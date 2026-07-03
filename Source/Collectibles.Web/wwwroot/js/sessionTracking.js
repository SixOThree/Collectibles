// Session tracking cookie refresh for Blazor Server.
// The TrackingCookieMiddleware only runs on HTTP requests, but Blazor Server
// uses a persistent SignalR WebSocket. This script refreshes the cookie
// expiration on user interactions to maintain the sliding window behavior.
(function () {
    var COOKIE_NAME = 'CollectiblesTrackingId';
    var EXPIRATION_MINUTES = 1440; // 24 hours

    function getCookie(name) {
        var match = document.cookie.match(new RegExp('(^| )' + name + '=([^;]+)'));
        return match ? match[2] : null;
    }

    function refreshCookie() {
        var trackingId = getCookie(COOKIE_NAME);
        if (!trackingId) return;

        var expires = new Date(Date.now() + EXPIRATION_MINUTES * 60 * 1000).toUTCString();
        var isSecure = window.location.protocol === 'https:';
        var cookieString = COOKIE_NAME + '=' + trackingId + ';expires=' + expires + ';path=/;SameSite=Strict';

        if (isSecure) {
            cookieString += ';Secure';
        }

        document.cookie = cookieString;
    }

    // Refresh on user interactions
    var events = ['click', 'keydown', 'scroll', 'mousemove'];
    var lastRefresh = 0;
    var REFRESH_INTERVAL = 5 * 60 * 1000; // Throttle: at most every 5 minutes

    function throttledRefresh() {
        var now = Date.now();
        if (now - lastRefresh > REFRESH_INTERVAL) {
            refreshCookie();
            lastRefresh = now;
        }
    }

    events.forEach(function (event) {
        document.addEventListener(event, throttledRefresh, { passive: true });
    });

    // Also refresh periodically as a backup
    setInterval(refreshCookie, 10 * 60 * 1000); // Every 10 minutes
})();
