// ImageZoomPan class definition
class ImageZoomPan {
    constructor(container, image, options = {}) {
        this.container = container;
        this.image = image;
        this.options = {
            minZoom: 0.5,
            maxZoom: 5,
            zoomStep: 0.1,
            smoothTime: 300,
            ...options
        };

        this.state = {
            zoom: 1,
            x: 0,
            y: 0,
            isDragging: false,
            startX: 0,
            startY: 0,
            lastX: 0,
            lastY: 0
        };

        this.boundHandlers = {
            wheel: this.handleWheel.bind(this),
            mousedown: this.handleMouseDown.bind(this),
            mousemove: this.handleMouseMove.bind(this),
            mouseup: this.handleMouseUp.bind(this),
            mouseleave: this.handleMouseLeave.bind(this),
            dblclick: this.handleDoubleClick.bind(this),
            touchstart: this.handleTouchStart.bind(this),
            touchmove: this.handleTouchMove.bind(this),
            touchend: this.handleTouchEnd.bind(this)
        };

        this.touches = [];
        this.lastTouchDistance = 0;

        this.init();
    }

    init() {
        this.container.style.position = 'relative';
        this.container.style.overflow = 'hidden';
        this.container.style.cursor = 'grab';
        this.container.style.userSelect = 'none';
        this.container.style.touchAction = 'none';

        this.image.style.transformOrigin = 'center center';
        this.image.style.transition = `transform ${this.options.smoothTime}ms ease-out`;
        this.image.draggable = false;

        this.attachEvents();
        this.updateTransform();
    }

    attachEvents() {
        this.container.addEventListener('wheel', this.boundHandlers.wheel, { passive: false });
        this.container.addEventListener('mousedown', this.boundHandlers.mousedown);
        this.container.addEventListener('mousemove', this.boundHandlers.mousemove);
        this.container.addEventListener('mouseup', this.boundHandlers.mouseup);
        this.container.addEventListener('mouseleave', this.boundHandlers.mouseleave);
        this.container.addEventListener('dblclick', this.boundHandlers.dblclick);
        this.container.addEventListener('touchstart', this.boundHandlers.touchstart, { passive: false });
        this.container.addEventListener('touchmove', this.boundHandlers.touchmove, { passive: false });
        this.container.addEventListener('touchend', this.boundHandlers.touchend);
    }

    detachEvents() {
        Object.entries(this.boundHandlers).forEach(([event, handler]) => {
            this.container.removeEventListener(event, handler);
        });
    }

    handleWheel(e) {
        e.preventDefault();
        
        const rect = this.container.getBoundingClientRect();
        const x = e.clientX - rect.left;
        const y = e.clientY - rect.top;
        
        const delta = e.deltaY > 0 ? -this.options.zoomStep : this.options.zoomStep;
        this.zoomTo(this.state.zoom + delta, x, y);
    }

    handleMouseDown(e) {
        if (e.button !== 0) return;
        
        this.state.isDragging = true;
        this.state.startX = e.clientX - this.state.x;
        this.state.startY = e.clientY - this.state.y;
        this.container.style.cursor = 'grabbing';
        this.image.style.transition = 'none';
    }

    handleMouseMove(e) {
        if (!this.state.isDragging) return;
        
        e.preventDefault();
        this.state.x = e.clientX - this.state.startX;
        this.state.y = e.clientY - this.state.startY;
        this.updateTransform();
    }

    handleMouseUp() {
        this.endDrag();
    }

    handleMouseLeave() {
        this.endDrag();
    }

    handleDoubleClick(e) {
        const rect = this.container.getBoundingClientRect();
        const x = e.clientX - rect.left;
        const y = e.clientY - rect.top;
        
        if (this.state.zoom > 1) {
            this.reset();
        } else {
            this.zoomTo(2, x, y);
        }
    }

    handleTouchStart(e) {
        e.preventDefault();
        this.touches = Array.from(e.touches);
        
        if (this.touches.length === 1) {
            const touch = this.touches[0];
            this.state.isDragging = true;
            this.state.startX = touch.clientX - this.state.x;
            this.state.startY = touch.clientY - this.state.y;
            this.image.style.transition = 'none';
        } else if (this.touches.length === 2) {
            this.state.isDragging = false;
            this.lastTouchDistance = this.getTouchDistance();
        }
    }

    handleTouchMove(e) {
        e.preventDefault();
        this.touches = Array.from(e.touches);
        
        if (this.touches.length === 1 && this.state.isDragging) {
            const touch = this.touches[0];
            this.state.x = touch.clientX - this.state.startX;
            this.state.y = touch.clientY - this.state.startY;
            this.updateTransform();
        } else if (this.touches.length === 2) {
            const currentDistance = this.getTouchDistance();
            const delta = currentDistance - this.lastTouchDistance;
            const zoomDelta = delta * 0.01;
            
            const centerX = (this.touches[0].clientX + this.touches[1].clientX) / 2;
            const centerY = (this.touches[0].clientY + this.touches[1].clientY) / 2;
            const rect = this.container.getBoundingClientRect();
            
            this.zoomTo(this.state.zoom + zoomDelta, centerX - rect.left, centerY - rect.top);
            this.lastTouchDistance = currentDistance;
        }
    }

    handleTouchEnd() {
        this.touches = [];
        this.endDrag();
    }

    getTouchDistance() {
        if (this.touches.length < 2) return 0;
        const dx = this.touches[0].clientX - this.touches[1].clientX;
        const dy = this.touches[0].clientY - this.touches[1].clientY;
        return Math.sqrt(dx * dx + dy * dy);
    }

    endDrag() {
        this.state.isDragging = false;
        this.container.style.cursor = 'grab';
        this.image.style.transition = `transform ${this.options.smoothTime}ms ease-out`;
        this.constrainPan();
    }

    zoomTo(newZoom, centerX, centerY) {
        const prevZoom = this.state.zoom;
        this.state.zoom = Math.max(this.options.minZoom, Math.min(this.options.maxZoom, newZoom));
        
        if (this.state.zoom !== prevZoom) {
            const zoomRatio = this.state.zoom / prevZoom;
            const rect = this.container.getBoundingClientRect();
            const offsetX = centerX - rect.width / 2;
            const offsetY = centerY - rect.height / 2;
            
            this.state.x = (this.state.x - offsetX) * zoomRatio + offsetX;
            this.state.y = (this.state.y - offsetY) * zoomRatio + offsetY;
            
            this.updateTransform();
            this.constrainPan();
        }
    }

    constrainPan() {
        const rect = this.container.getBoundingClientRect();
        const imgRect = this.image.getBoundingClientRect();
        
        const scaledWidth = this.image.naturalWidth * this.state.zoom;
        const scaledHeight = this.image.naturalHeight * this.state.zoom;
        
        const maxX = Math.max(0, (scaledWidth - rect.width) / 2);
        const maxY = Math.max(0, (scaledHeight - rect.height) / 2);
        
        this.state.x = Math.max(-maxX, Math.min(maxX, this.state.x));
        this.state.y = Math.max(-maxY, Math.min(maxY, this.state.y));
        
        this.updateTransform();
    }

    updateTransform() {
        this.image.style.transform = `translate(${this.state.x}px, ${this.state.y}px) scale(${this.state.zoom})`;
    }

    zoomIn() {
        const rect = this.container.getBoundingClientRect();
        this.zoomTo(this.state.zoom + this.options.zoomStep, rect.width / 2, rect.height / 2);
    }

    zoomOut() {
        const rect = this.container.getBoundingClientRect();
        this.zoomTo(this.state.zoom - this.options.zoomStep, rect.width / 2, rect.height / 2);
    }

    reset() {
        this.state.zoom = 1;
        this.state.x = 0;
        this.state.y = 0;
        this.updateTransform();
    }

    fitToContainer() {
        const containerRect = this.container.getBoundingClientRect();
        const imgAspect = this.image.naturalWidth / this.image.naturalHeight;
        const containerAspect = containerRect.width / containerRect.height;
        
        let zoom;
        if (imgAspect > containerAspect) {
            zoom = containerRect.width / this.image.naturalWidth;
        } else {
            zoom = containerRect.height / this.image.naturalHeight;
        }
        
        this.state.zoom = zoom;
        this.state.x = 0;
        this.state.y = 0;
        this.updateTransform();
    }

    getState() {
        return { ...this.state };
    }

    setState(newState) {
        Object.assign(this.state, newState);
        this.updateTransform();
    }

    destroy() {
        this.detachEvents();
        this.image.style.transform = '';
        this.image.style.transition = '';
        this.image.style.transformOrigin = '';
        this.container.style.cursor = '';
        this.container.style.overflow = '';
        this.container.style.position = '';
        this.container.style.userSelect = '';
        this.container.style.touchAction = '';
    }
}

// Modal functionality
let modalInstance = null;
let zoomPanInstance = null;

window.imagePreviewModal = {
    initializeModal: function() {
        modalInstance = {
            handleKeydown: (e) => {
                switch (e.key) {
                    case 'Escape':
                        closeModal();
                        break;
                    case 'ArrowLeft':
                        if (!e.ctrlKey && !e.shiftKey) {
                            navigatePrevious();
                        }
                        break;
                    case 'ArrowRight':
                        if (!e.ctrlKey && !e.shiftKey) {
                            navigateNext();
                        }
                        break;
                    case '+':
                    case '=':
                        if (e.ctrlKey || e.metaKey) {
                            e.preventDefault();
                            window.imagePreviewModal.zoomIn();
                        }
                        break;
                    case '-':
                    case '_':
                        if (e.ctrlKey || e.metaKey) {
                            e.preventDefault();
                            window.imagePreviewModal.zoomOut();
                        }
                        break;
                    case '0':
                        if (e.ctrlKey || e.metaKey) {
                            e.preventDefault();
                            window.imagePreviewModal.resetZoom();
                        }
                        break;
                }
            }
        };

        document.addEventListener('keydown', modalInstance.handleKeydown);
        document.body.style.overflow = 'hidden';
        
        window.imagePreviewModal.initializeZoomPan();
    },

    cleanup: function() {
        if (modalInstance) {
            document.removeEventListener('keydown', modalInstance.handleKeydown);
            modalInstance = null;
        }
        
        window.imagePreviewModal.cleanupZoomPan();
        document.body.style.overflow = '';
    },

    initializeZoomPan: function() {
        // Clean up any existing instance first
        window.imagePreviewModal.cleanupZoomPan();

        // Poll for the zoom container — Blazor's render batch may arrive after
        // the JS interop call, so the DOM element might not exist yet.
        let attempts = 0;
        const maxAttempts = 20; // 20 × 50ms = 1s max wait
        const tryInit = () => {
            const container = document.querySelector('.zoom-container');
            const image = container?.querySelector('.preview-image');

            if (container && image) {
                if (image.complete && image.naturalWidth > 0) {
                    zoomPanInstance = new ImageZoomPan(container, image, {
                        minZoom: 0.5,
                        maxZoom: 5,
                        zoomStep: 0.1,
                        smoothTime: 300
                    });
                } else {
                    // Image not loaded yet, wait for it
                    image.addEventListener('load', () => {
                        if (!zoomPanInstance) {
                            zoomPanInstance = new ImageZoomPan(container, image, {
                                minZoom: 0.5,
                                maxZoom: 5,
                                zoomStep: 0.1,
                                smoothTime: 300
                            });
                        }
                    }, { once: true });
                }
            } else if (++attempts < maxAttempts) {
                setTimeout(tryInit, 50);
            }
        };
        setTimeout(tryInit, 50);
    },

    cleanupZoomPan: function() {
        if (zoomPanInstance) {
            zoomPanInstance.destroy();
            zoomPanInstance = null;
        }
    },

    zoomIn: function() {
        if (zoomPanInstance) {
            zoomPanInstance.zoomIn();
        } else {
            console.warn('ZoomPan instance not initialized');
        }
    },

    zoomOut: function() {
        if (zoomPanInstance) {
            zoomPanInstance.zoomOut();
        } else {
            console.warn('ZoomPan instance not initialized');
        }
    },

    resetZoom: function() {
        if (zoomPanInstance) {
            zoomPanInstance.reset();
        } else {
            console.warn('ZoomPan instance not initialized');
        }
    },

    fitToContainer: function() {
        if (zoomPanInstance) {
            zoomPanInstance.fitToContainer();
        } else {
            console.warn('ZoomPan instance not initialized');
        }
    }
};

function closeModal() {
    const closeButton = document.querySelector('.modal-backdrop .btn[title="Close"]');
    if (closeButton) {
        closeButton.click();
    }
}

function navigatePrevious() {
    const prevButton = document.querySelector('.navigation-controls .btn:first-child:not([disabled])');
    if (prevButton) {
        prevButton.click();
    }
}

function navigateNext() {
    const nextButton = document.querySelector('.navigation-controls .btn:last-child:not([disabled])');
    if (nextButton) {
        nextButton.click();
    }
}

window.downloadFile = function(base64Content, mimeType, fileName) {
    const byteCharacters = atob(base64Content);
    const byteNumbers = new Array(byteCharacters.length);
    
    for (let i = 0; i < byteCharacters.length; i++) {
        byteNumbers[i] = byteCharacters.charCodeAt(i);
    }
    
    const byteArray = new Uint8Array(byteNumbers);
    const blob = new Blob([byteArray], { type: mimeType });
    const url = window.URL.createObjectURL(blob);
    
    const link = document.createElement('a');
    link.href = url;
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    
    window.URL.revokeObjectURL(url);
};