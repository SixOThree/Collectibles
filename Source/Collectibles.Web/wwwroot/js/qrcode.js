window.downloadFile = function (dataUrl, filename) {
    const link = document.createElement('a');
    link.href = dataUrl;
    link.download = filename;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
};

window.printQRCode = function (dataUrl) {
    // Create a new window for printing
    const printWindow = window.open('', '_blank');
    
    // Write the HTML content with the QR code filling the page width
    printWindow.document.write(`
        <!DOCTYPE html>
        <html>
        <head>
            <title>Print QR Code</title>
            <style>
                @page {
                    size: 2.4in 2.4in;
                    margin: 0;
                }
                body {
                    margin: 0;
                    padding: 0;
                    width: 100%;
                    height: 100vh;
                    display: flex;
                    justify-content: center;
                    align-items: center;
                    background: white;
                }
                .qr-container {
                    width: 2.4in;
                    height: 2.4in;
                    display: flex;
                    justify-content: center;
                    align-items: center;
                }
                .qr-container img {
                    width: 100%;
                    height: 100%;
                    object-fit: contain;
                    image-rendering: crisp-edges;
                    image-rendering: pixelated;
                }
                @media print {
                    @page {
                        size: 2.4in 2.4in;
                        margin: 0;
                    }
                    body {
                        margin: 0;
                        padding: 0;
                    }
                    .qr-container {
                        width: 2.4in;
                        height: 2.4in;
                    }
                    .qr-container img {
                        width: 2.4in;
                        height: 2.4in;
                    }
                }
            </style>
        </head>
        <body>
            <div class="qr-container">
                <img src="${dataUrl}" alt="QR Code" />
            </div>
        </body>
        </html>
    `);
    
    // Close the document writing
    printWindow.document.close();
    
    // Wait for the image to load, then print
    printWindow.onload = function() {
        setTimeout(function() {
            printWindow.print();
            printWindow.close();
        }, 250);
    };
};

window.initQRScanner = function (dotNetRef, videoElementId) {
    // Add retry logic if element not found immediately
    let retries = 0;
    const maxRetries = 10;
    
    const tryInit = () => {
        const video = document.getElementById(videoElementId);
        
        if (!video) {
            retries++;
            if (retries < maxRetries) {
                console.log(`Video element not found, retrying... (${retries}/${maxRetries})`);
                setTimeout(tryInit, 100);
                return;
            }
            console.error('Video element not found after retries');
            dotNetRef.invokeMethodAsync('OnScanError', 'Video element could not be initialized');
            return;
        }

        console.log('Video element found, initializing camera...');
        
        // Request camera permission and start video stream
        navigator.mediaDevices.getUserMedia({ 
            video: { facingMode: 'environment' } 
        })
        .then(stream => {
            video.srcObject = stream;
            console.log('Camera stream started');
            
            // Use QR code scanning library if available
            if (typeof QrScanner !== 'undefined') {
                console.log('QrScanner library found, initializing scanner...');
                // Set the worker path to the absolute URL to avoid CORS issues
                const baseUrl = window.location.origin;
                QrScanner.WORKER_PATH = baseUrl + '/js/qr-scanner-worker.min.js';
                console.log('Worker path set to:', QrScanner.WORKER_PATH);
                
                // Try to create scanner with specific options to avoid worker issues
                try {
                    const qrScanner = new QrScanner(
                        video,
                        result => {
                            console.log('QR code detected:', result.data);
                            dotNetRef.invokeMethodAsync('OnQRCodeScannedCallback', result.data);
                        },
                        {
                            highlightScanRegion: true,
                            highlightCodeOutline: true,
                            preferredCamera: 'environment',
                            maxScansPerSecond: 5
                        }
                    );
                    
                    qrScanner.start().then(() => {
                        console.log('QR Scanner started successfully');
                    }).catch(err => {
                        console.error('Error starting QR scanner:', err);
                        // Try without worker if it fails
                        if (err.message && err.message.includes('worker')) {
                            console.log('Attempting to start scanner without worker...');
                            dotNetRef.invokeMethodAsync('OnScanError', 'QR scanner initialization failed. Please try using Firefox or refresh the page.');
                        } else {
                            dotNetRef.invokeMethodAsync('OnScanError', 'Failed to start QR scanner: ' + err.message);
                        }
                    });
                    
                    // Store scanner instance for cleanup
                    video.qrScanner = qrScanner;
                } catch (err) {
                    console.error('Error creating QR scanner:', err);
                    dotNetRef.invokeMethodAsync('OnScanError', 'Failed to initialize QR scanner. Please try using Firefox or refresh the page.');
                }
            } else {
                console.error('QrScanner library not loaded');
                dotNetRef.invokeMethodAsync('OnScanError', 'QR Scanner library not loaded. Please refresh the page.');
            }
        })
        .catch(err => {
            console.error('Error accessing camera:', err);
            dotNetRef.invokeMethodAsync('OnScanError', err.message || err.name || 'Unknown error accessing camera');
        });
    };
    
    tryInit();
};

window.stopQRScanner = function (videoElementId) {
    const video = document.getElementById(videoElementId);
    
    if (video) {
        // Stop QR scanner if it exists
        if (video.qrScanner) {
            video.qrScanner.stop();
            video.qrScanner = null;
        }
        
        // Stop video stream
        if (video.srcObject) {
            const tracks = video.srcObject.getTracks();
            tracks.forEach(track => track.stop());
            video.srcObject = null;
        }
    }
};
