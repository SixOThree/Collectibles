// Persist dark/light mode across Blazor Interactive Server navigation.
// Blazor Bootstrap's ThemeSwitcher stores the user's preference in localStorage
// under 'blazorbootstrap-theme', but Blazor's server-side DOM patching strips the
// data-bs-theme attribute from <html> on every navigation because the server renders
// it without the attribute. We use a MutationObserver to re-apply it immediately.
(function () {
    var STORAGE_KEY = 'blazorbootstrap-theme';

    function getResolvedTheme() {
        var stored = localStorage.getItem(STORAGE_KEY);
        if (!stored) return null;
        if (stored === 'dark') return 'dark';
        if (stored === 'light') return 'light';
        if (stored === 'system') {
            return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
        }
        return null;
    }

    function applyStoredTheme() {
        var theme = getResolvedTheme();
        if (theme) {
            document.documentElement.setAttribute('data-bs-theme', theme);
        }
    }

    // Apply immediately on script load (blocks rendering, prevents flash)
    applyStoredTheme();

    // Watch for Blazor removing data-bs-theme during navigation DOM patches
    var observer = new MutationObserver(function (mutations) {
        for (var i = 0; i < mutations.length; i++) {
            if (mutations[i].attributeName === 'data-bs-theme') {
                var current = document.documentElement.getAttribute('data-bs-theme');
                var expected = getResolvedTheme();
                if (expected && current !== expected) {
                    applyStoredTheme();
                }
            }
        }
    });

    observer.observe(document.documentElement, { attributes: true });
})();
