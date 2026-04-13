// Global error handler for JavaScript errors
window.addEventListener('error', function(event) {
    console.error('JavaScript Error:', {
        message: event.message,
        source: event.filename,
        line: event.lineno,
        column: event.colno,
        error: event.error
    });
});

// Handle unhandled promise rejections
window.addEventListener('unhandledrejection', function(event) {
    console.error('Unhandled Promise Rejection:', {
        reason: event.reason,
        promise: event.promise
    });
});

// Log when Blazor connection is lost
if (window.Blazor) {
    window.Blazor.defaultReconnectionHandler._reconnectionDisplay.show = function() {
        console.error('Blazor connection lost');
    };
}

// Add debugging for Blazor initialization
console.log('Error handler script loaded');