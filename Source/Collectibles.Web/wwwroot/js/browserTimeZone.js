window.getBrowserTimeZone = function () {
    return Intl.DateTimeFormat().resolvedOptions().timeZone;
};
