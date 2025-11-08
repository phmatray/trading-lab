// Global keyboard shortcuts for navigation
(function () {
    let dotNetHelper = null;
    let boundHandler = null;

    function handleKeyDown(event) {
        console.log('Key pressed:', event.key, 'Alt:', event.altKey, 'Ctrl:', event.ctrlKey);

        // Only handle Alt key combinations (and not Ctrl or Shift)
        if (!event.altKey || event.ctrlKey || event.shiftKey || event.metaKey) {
            return;
        }

        // Check if user is typing in an input/textarea
        const activeElement = document.activeElement;
        if (activeElement && (
            activeElement.tagName === 'INPUT' ||
            activeElement.tagName === 'TEXTAREA' ||
            activeElement.tagName === 'SELECT' ||
            activeElement.isContentEditable
        )) {
            return;
        }

        const key = event.key.toLowerCase();
        let route = null;

        switch (key) {
            case 'd':
                route = '/';
                console.log('Navigating to Dashboard');
                break;
            case 'p':
                route = '/portfolio';
                console.log('Navigating to Portfolio');
                break;
            case 'r':
                route = '/performance';
                console.log('Navigating to Performance');
                break;
            case 's':
                route = '/strategies';
                console.log('Navigating to Strategies');
                break;
            case 'g':
                route = '/settings';
                console.log('Navigating to Settings');
                break;
            case 'b':
                route = '/backtest';
                console.log('Navigating to Backtest');
                break;
        }

        if (route && dotNetHelper) {
            event.preventDefault();
            event.stopPropagation();
            dotNetHelper.invokeMethodAsync('NavigateToRoute', route)
                .catch(err => console.error('Navigation error:', err));
        }
    }

    window.keyboardShortcuts = {
        initialize: function (helper) {
            console.log('Initializing keyboard shortcuts');
            dotNetHelper = helper;

            // Remove any existing listener first
            if (boundHandler) {
                document.removeEventListener('keydown', boundHandler);
            }

            // Create and store bound handler
            boundHandler = handleKeyDown;

            // Add listener with capture phase to ensure it fires
            document.addEventListener('keydown', boundHandler, true);
            console.log('Keyboard shortcuts initialized');
        },

        dispose: function () {
            console.log('Disposing keyboard shortcuts');
            if (boundHandler) {
                document.removeEventListener('keydown', boundHandler, true);
                boundHandler = null;
            }
            dotNetHelper = null;
        }
    };
})();
