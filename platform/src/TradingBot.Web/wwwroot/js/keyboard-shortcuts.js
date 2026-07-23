// Global keyboard shortcuts for navigation
(function () {
    let dotNetHelper = null;
    let boundHandler = null;

    function handleKeyDown(event) {
        console.log('Key pressed - key:', event.key, 'code:', event.code, 'Alt:', event.altKey, 'Ctrl:', event.ctrlKey, 'Meta:', event.metaKey);

        // Only handle Alt key combinations (and not Ctrl or Meta/Command)
        // Allow Shift with Alt for uppercase letters
        if (!event.altKey || event.ctrlKey || event.metaKey) {
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

        // Use event.code to get physical key, not the character produced
        // KeyD, KeyP, KeyR, KeyS, KeyG, KeyB, KeyH
        let route = null;

        switch (event.code) {
            case 'KeyD':
                route = '/';
                console.log('Navigating to Dashboard');
                break;
            case 'KeyP':
                route = '/portfolio';
                console.log('Navigating to Portfolio');
                break;
            case 'KeyR':
                route = '/performance';
                console.log('Navigating to Performance');
                break;
            case 'KeyS':
                route = '/strategies';
                console.log('Navigating to Strategies');
                break;
            case 'KeyG':
                route = '/settings';
                console.log('Navigating to Settings');
                break;
            case 'KeyB':
                route = '/backtest';
                console.log('Navigating to Backtest');
                break;
            case 'KeyH':
                route = '/help';
                console.log('Navigating to Help');
                break;
        }

        if (route && dotNetHelper) {
            event.preventDefault();
            event.stopPropagation();
            console.log('Invoking navigation to:', route);
            dotNetHelper.invokeMethodAsync('NavigateToRoute', route)
                .then(() => console.log('Navigation successful'))
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
