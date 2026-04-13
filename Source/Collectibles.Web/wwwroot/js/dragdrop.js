window.initializeFileDrop = (dotNetRef, dropZoneElement, inputElement) => {
  // Prevent default drag behaviors
  ['dragenter', 'dragover', 'dragleave', 'drop'].forEach(eventName => {
    dropZoneElement.addEventListener(eventName, preventDefaults, false);
    document.body.addEventListener(eventName, preventDefaults, false);
  });

  // Highlight drop zone when item is dragged over it
  ['dragenter', 'dragover'].forEach(eventName => {
    dropZoneElement.addEventListener(eventName, highlight, false);
  });

  ['dragleave', 'drop'].forEach(eventName => {
    dropZoneElement.addEventListener(eventName, unhighlight, false);
  });

  // Handle dropped files
  dropZoneElement.addEventListener('drop', handleDrop, false);

  function preventDefaults(e) {
    e.preventDefault();
    e.stopPropagation();
  }

  function highlight(e) {
    dropZoneElement.classList.add('drag-over');
  }

  function unhighlight(e) {
    dropZoneElement.classList.remove('drag-over');
  }

  function handleDrop(e) {
    const dt = e.dataTransfer;
    const files = dt.files;

    if (files && files.length > 0) {
      // Convert FileList to array and trigger the file input
      const dataTransfer = new DataTransfer();

      Array.from(files).forEach(file => {
        dataTransfer.items.add(file);
      });

      inputElement.files = dataTransfer.files;

      // Trigger the change event
      const event = new Event('change', { bubbles: true });
      inputElement.dispatchEvent(event);
    }
  }
};

window.disposeFileDrop = (dropZoneElement) => {
  // Clean up event listeners if needed
  if (dropZoneElement && dropZoneElement.parentNode) {
    dropZoneElement.replaceWith(dropZoneElement.cloneNode(true));
  }
};
