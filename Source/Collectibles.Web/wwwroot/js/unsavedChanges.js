// Unsaved changes warning handler
window.unsavedChangesHandler = {
    _hasUnsavedChanges: false,

    setUnsavedChanges: function (hasChanges) {
        this._hasUnsavedChanges = hasChanges;
    },

    _beforeUnloadHandler: function (e) {
        if (window.unsavedChangesHandler._hasUnsavedChanges) {
            e.preventDefault();
            e.returnValue = '';
            return '';
        }
    },

    _clickHandler: function (e) {
        if (!window.unsavedChangesHandler._hasUnsavedChanges) {
            return;
        }

        // Find the closest anchor tag
        const link = e.target.closest('a[href]');
        if (!link) {
            return;
        }

        // Ignore links that open in new tabs or have no href
        const href = link.getAttribute('href');
        if (!href || href === '#' || link.target === '_blank') {
            return;
        }

        // Ignore links within the current edit form (like accordion toggles)
        if (link.closest('.edit-item-container') && (href.startsWith('#') || link.getAttribute('data-bs-toggle'))) {
            return;
        }

        // Show confirmation dialog
        if (!confirm('You have unsaved changes. Are you sure you want to leave this page?')) {
            e.preventDefault();
            e.stopPropagation();
        }
    },

    enable: function () {
        window.addEventListener('beforeunload', this._beforeUnloadHandler);
        document.addEventListener('click', this._clickHandler, true);
    },

    disable: function () {
        window.removeEventListener('beforeunload', this._beforeUnloadHandler);
        document.removeEventListener('click', this._clickHandler, true);
        this._hasUnsavedChanges = false;
    }
};
