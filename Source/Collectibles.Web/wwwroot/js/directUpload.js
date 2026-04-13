/**
 * Direct Upload Module
 * Handles uploading files directly to Azure Blob Storage using SAS URLs.
 * This bypasses the server (and Cloudflare) for large file uploads.
 *
 * For files <= 4 MB, uses a single PUT request.
 * For files > 4 MB, uses Azure Block Blob upload (Put Block + Put Block List).
 */
window.directUpload = {
    // Files larger than this threshold use block upload (4 MB).
    // Keep this low: single PUT requests can be rejected by Azure (depending on API version),
    // proxies, or firewalls with body size limits. Block upload sends 8 MB chunks and is reliable
    // for any file size with negligible overhead.
    BLOCK_UPLOAD_THRESHOLD: 4 * 1024 * 1024,

    // Size of each block for block uploads (8 MB)
    BLOCK_SIZE: 8 * 1024 * 1024,

    /**
     * Uploads a file directly to Azure Blob Storage using a SAS URL.
     * Automatically chooses single PUT or block upload based on file size.
     *
     * @param {string} sasUrl - The SAS URL with write permissions
     * @param {File} file - The file to upload (from input element)
     * @param {object} dotNetRef - DotNet object reference for progress callbacks
     * @param {string} contentType - The MIME type of the file
     * @returns {Promise<{success: boolean, error?: string}>}
     */
    uploadToAzure: function (sasUrl, file, dotNetRef, contentType) {
        if (file.size > this.BLOCK_UPLOAD_THRESHOLD) {
            return this.uploadToAzureBlocks(sasUrl, file, dotNetRef, contentType);
        }
        return this.uploadToAzureSinglePut(sasUrl, file, dotNetRef, contentType);
    },

    /**
     * Uploads a file using a single PUT request.
     * Suitable for files up to ~4 MB.
     */
    uploadToAzureSinglePut: function (sasUrl, file, dotNetRef, contentType) {
        return new Promise((resolve) => {
            const xhr = new XMLHttpRequest();

            // Track upload progress
            xhr.upload.addEventListener('progress', function (e) {
                if (e.lengthComputable) {
                    const percentComplete = Math.round((e.loaded / e.total) * 100);
                    // Call back to .NET to update progress
                    if (dotNetRef) {
                        dotNetRef.invokeMethodAsync('OnDirectUploadProgress', percentComplete)
                            .catch(err => console.warn('Failed to report progress:', err));
                    }
                }
            });

            // Handle completion
            xhr.addEventListener('load', function () {
                if (xhr.status >= 200 && xhr.status < 300) {
                    resolve({ success: true });
                } else {
                    const errorMessage = `Upload failed with status ${xhr.status}: ${xhr.statusText}`;
                    console.error(errorMessage);
                    resolve({ success: false, error: errorMessage });
                }
            });

            // Handle errors
            xhr.addEventListener('error', function (e) {
                // Log detailed error info for debugging
                console.error('XHR error event:', e);
                console.error('XHR status:', xhr.status);
                console.error('XHR statusText:', xhr.statusText);
                console.error('XHR readyState:', xhr.readyState);
                console.error('SAS URL domain:', new URL(sasUrl).hostname);

                // This is usually a CORS error - browsers don't expose details for security
                const errorMessage = `Network error during upload (likely CORS). Status: ${xhr.status}, ReadyState: ${xhr.readyState}. Check browser console for details.`;
                console.error(errorMessage);
                resolve({ success: false, error: errorMessage });
            });

            xhr.addEventListener('abort', function () {
                resolve({ success: false, error: 'Upload was aborted' });
            });

            // Open connection and set headers
            xhr.open('PUT', sasUrl, true);

            // Required headers for Azure Blob Storage
            xhr.setRequestHeader('x-ms-blob-type', 'BlockBlob');
            xhr.setRequestHeader('Content-Type', contentType);

            // Send the file
            xhr.send(file);
        });
    },

    /**
     * Uploads a large file using Azure Block Blob upload.
     * Splits the file into blocks, uploads each with Put Block,
     * then commits with Put Block List.
     *
     * @param {string} sasUrl - The SAS URL with write permissions
     * @param {File} file - The file to upload
     * @param {object} dotNetRef - DotNet object reference for progress callbacks
     * @param {string} contentType - The MIME type of the file
     * @returns {Promise<{success: boolean, error?: string}>}
     */
    uploadToAzureBlocks: async function (sasUrl, file, dotNetRef, contentType) {
        const blockSize = this.BLOCK_SIZE;
        const totalBlocks = Math.ceil(file.size / blockSize);
        const blockIds = [];
        let totalBytesUploaded = 0;

        console.log(`Starting block upload: ${file.size} bytes, ${totalBlocks} blocks of ${blockSize} bytes`);

        try {
            // Upload each block
            for (let i = 0; i < totalBlocks; i++) {
                const start = i * blockSize;
                const end = Math.min(start + blockSize, file.size);
                const chunk = file.slice(start, end);

                // Block ID must be base64-encoded and all IDs must be the same length
                const blockId = btoa(String(i).padStart(6, '0'));
                blockIds.push(blockId);

                // Append block parameters to SAS URL
                const separator = sasUrl.includes('?') ? '&' : '?';
                const blockUrl = `${sasUrl}${separator}comp=block&blockid=${encodeURIComponent(blockId)}`;

                const response = await fetch(blockUrl, {
                    method: 'PUT',
                    headers: {
                        'Content-Length': String(end - start),
                    },
                    body: chunk,
                });

                if (!response.ok) {
                    const errorText = await response.text();
                    const errorMessage = `Block ${i + 1}/${totalBlocks} upload failed with status ${response.status}: ${errorText}`;
                    console.error(errorMessage);
                    return { success: false, error: errorMessage };
                }

                totalBytesUploaded += (end - start);
                const percentComplete = Math.round((totalBytesUploaded / file.size) * 100);

                if (dotNetRef) {
                    try {
                        await dotNetRef.invokeMethodAsync('OnDirectUploadProgress', percentComplete);
                    } catch (err) {
                        console.warn('Failed to report progress:', err);
                    }
                }
            }

            // Commit the block list
            const blockListXml = '<?xml version="1.0" encoding="utf-8"?>\n<BlockList>' +
                blockIds.map(id => `<Latest>${id}</Latest>`).join('') +
                '</BlockList>';

            const separator = sasUrl.includes('?') ? '&' : '?';
            const commitUrl = `${sasUrl}${separator}comp=blocklist`;

            const commitResponse = await fetch(commitUrl, {
                method: 'PUT',
                headers: {
                    'Content-Type': 'application/xml',
                    'x-ms-blob-content-type': contentType,
                },
                body: blockListXml,
            });

            if (!commitResponse.ok) {
                const errorText = await commitResponse.text();
                const errorMessage = `Block list commit failed with status ${commitResponse.status}: ${errorText}`;
                console.error(errorMessage);
                return { success: false, error: errorMessage };
            }

            console.log('Block upload completed successfully');
            return { success: true };
        } catch (err) {
            const errorMessage = `Block upload error: ${err.message}`;
            console.error(errorMessage, err);
            return { success: false, error: errorMessage };
        }
    },

    /**
     * Gets a File object from an InputFile element by index.
     * This is needed because Blazor's InputFile doesn't expose the raw File object.
     *
     * @param {string} inputId - The ID of the input element
     * @param {number} fileIndex - The index of the file in the FileList
     * @returns {File|null}
     */
    getFileFromInput: function (inputId, fileIndex) {
        const input = document.getElementById(inputId);
        if (input && input.files && input.files.length > fileIndex) {
            return input.files[fileIndex];
        }
        return null;
    },

    /**
     * Uploads a file from an input element to Azure.
     * This is a convenience method that combines getFileFromInput and uploadToAzure.
     *
     * @param {string} inputId - The ID of the input element
     * @param {number} fileIndex - The index of the file in the FileList
     * @param {string} sasUrl - The SAS URL with write permissions
     * @param {object} dotNetRef - DotNet object reference for progress callbacks
     * @param {string} contentType - The MIME type of the file
     * @returns {Promise<{success: boolean, error?: string}>}
     */
    uploadFromInput: async function (inputId, fileIndex, sasUrl, dotNetRef, contentType) {
        const file = this.getFileFromInput(inputId, fileIndex);
        if (!file) {
            return { success: false, error: 'File not found in input element' };
        }
        return await this.uploadToAzure(sasUrl, file, dotNetRef, contentType);
    }
};
