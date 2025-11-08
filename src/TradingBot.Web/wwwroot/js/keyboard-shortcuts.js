// Global keyboard shortcuts for navigation
window.keyboardShortcuts = {
    dotNetHelper: null,

    initialize: function (dotNetHelper) {
        this.dotNetHelper = dotNetHelper;

        // Add document-level keydown listener
        document.addEventListener('keydown', this.handleKeyDown.bind(this));
    },

    handleKeyDown: function (event) {
        // Only handle Alt key combinations (and not Ctrl or Shift)
        if (!event.altKey || event.ctrlKey || event.shiftKey || event.metaKey) {
            return;
        }

        // Check if user is typing in an input/textarea
        const activeElement = document.activeElement;
        if (activeElement && (
            activeElement.tagName === 'INPUT' ||
            activeElement.tagName === 'TEXTAREA' ||
            activeElement.isContentEditable
        )) {
            return;
        }

        const key = event.key.toLowerCase();
        let route = null;

        switch (key) {
            case 'd':
                route = '/';
                break;
            case 'p':
                route = '/portfolio';
                break;
            case 'r':
                route = '/performance';
                break;
            case 's':
                route = '/strategies';
                break;
            case 'g':
                route = '/settings';
                break;
            case 'b':
                route = '/backtest';
                break;
        }

        if (route && this.dotNetHelper) {
            event.preventDefault();
            this.dotNetHelper.invokeMethodAsync('NavigateToRoute', route);
        }
    },

    dispose: function () {
        document.removeEventListener('keydown', this.handleKeyDown.bind(this));
        this.dotNetHelper = null;
    }
};
