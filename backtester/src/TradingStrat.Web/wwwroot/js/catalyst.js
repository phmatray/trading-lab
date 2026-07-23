// Catalyst UI Kit - JavaScript Interop for Blazor
// Provides dropdown positioning, focus management, and keyboard navigation

window.catalyst = {
    /**
     * Initialize a dropdown with positioning and keyboard navigation
     * @param {string} dropdownId - Unique ID of the dropdown container
     * @param {string} buttonId - ID of the trigger button
     * @param {string} menuId - ID of the dropdown menu
     * @param {string} anchor - Anchor position ('bottom', 'top', 'bottom start', 'bottom end', etc.)
     * @param {object} dotNetRef - .NET reference for callbacks
     */
    initializeDropdown(dropdownId, buttonId, menuId, anchor, dotNetRef) {
        const button = document.getElementById(buttonId);
        const menu = document.getElementById(menuId);

        if (!button || !menu) {
            console.warn('Dropdown elements not found:', { buttonId, menuId });
            return;
        }

        // Store dropdown state
        const dropdown = {
            id: dropdownId,
            button,
            menu,
            anchor,
            dotNetRef,
            isOpen: false,
            clickOutsideHandler: null,
            keyDownHandler: null
        };

        // Store in global map for cleanup
        if (!window.catalyst._dropdowns) {
            window.catalyst._dropdowns = new Map();
        }
        window.catalyst._dropdowns.set(dropdownId, dropdown);

        return dropdownId;
    },

    /**
     * Open the dropdown menu
     * @param {string} dropdownId - Dropdown ID
     */
    openDropdown(dropdownId) {
        const dropdown = window.catalyst._dropdowns?.get(dropdownId);
        if (!dropdown || dropdown.isOpen) return;

        dropdown.isOpen = true;
        dropdown.menu.style.display = 'block';

        // Position the menu
        this.positionDropdownMenu(dropdown);

        // Add event listeners
        setTimeout(() => {
            dropdown.clickOutsideHandler = (e) => {
                if (!dropdown.button.contains(e.target) && !dropdown.menu.contains(e.target)) {
                    dropdown.dotNetRef.invokeMethodAsync('CloseDropdown');
                }
            };
            document.addEventListener('click', dropdown.clickOutsideHandler);

            dropdown.keyDownHandler = (e) => this.handleDropdownKeyboard(e, dropdown);
            dropdown.menu.addEventListener('keydown', dropdown.keyDownHandler);

            // Focus first item
            const firstItem = dropdown.menu.querySelector('[role="menuitem"]:not([disabled])');
            if (firstItem) {
                firstItem.focus();
            }
        }, 10);
    },

    /**
     * Close the dropdown menu
     * @param {string} dropdownId - Dropdown ID
     */
    closeDropdown(dropdownId) {
        const dropdown = window.catalyst._dropdowns?.get(dropdownId);
        if (!dropdown || !dropdown.isOpen) return;

        dropdown.isOpen = false;
        dropdown.menu.style.display = 'none';

        // Remove event listeners
        if (dropdown.clickOutsideHandler) {
            document.removeEventListener('click', dropdown.clickOutsideHandler);
            dropdown.clickOutsideHandler = null;
        }
        if (dropdown.keyDownHandler) {
            dropdown.menu.removeEventListener('keydown', dropdown.keyDownHandler);
            dropdown.keyDownHandler = null;
        }

        // Return focus to button
        dropdown.button.focus();
    },

    /**
     * Position the dropdown menu relative to the button
     * @param {object} dropdown - Dropdown state object
     */
    positionDropdownMenu(dropdown) {
        const { button, menu, anchor } = dropdown;
        const buttonRect = button.getBoundingClientRect();
        const menuRect = menu.getBoundingClientRect();
        const viewportHeight = window.innerHeight;
        const viewportWidth = window.innerWidth;

        // Parse anchor string (e.g., 'bottom', 'bottom start', 'top end')
        const anchorParts = anchor.split(' ');
        const verticalAnchor = anchorParts[0]; // 'top' or 'bottom'
        const horizontalAnchor = anchorParts[1] || 'start'; // 'start', 'end', or undefined

        let top, left;

        // Vertical positioning
        if (verticalAnchor === 'bottom') {
            top = buttonRect.bottom + 8; // 8px gap
            // Check if menu would overflow viewport bottom
            if (top + menuRect.height > viewportHeight) {
                top = buttonRect.top - menuRect.height - 8; // Position above
            }
        } else {
            top = buttonRect.top - menuRect.height - 8;
            // Check if menu would overflow viewport top
            if (top < 0) {
                top = buttonRect.bottom + 8; // Position below
            }
        }

        // Horizontal positioning
        if (horizontalAnchor === 'end') {
            left = buttonRect.right - menuRect.width;
            // Adjust if overflows left edge
            if (left < 0) {
                left = buttonRect.left;
            }
        } else {
            left = buttonRect.left;
            // Adjust if overflows right edge
            if (left + menuRect.width > viewportWidth) {
                left = buttonRect.right - menuRect.width;
            }
        }

        // Apply positioning
        menu.style.position = 'fixed';
        menu.style.top = `${top}px`;
        menu.style.left = `${left}px`;
        menu.style.zIndex = '50';
    },

    /**
     * Handle keyboard navigation within dropdown
     * @param {KeyboardEvent} e - Keyboard event
     * @param {object} dropdown - Dropdown state object
     */
    handleDropdownKeyboard(e, dropdown) {
        const items = Array.from(dropdown.menu.querySelectorAll('[role="menuitem"]:not([disabled])'));
        const currentIndex = items.indexOf(document.activeElement);

        switch (e.key) {
            case 'ArrowDown':
                e.preventDefault();
                const nextIndex = (currentIndex + 1) % items.length;
                items[nextIndex]?.focus();
                break;

            case 'ArrowUp':
                e.preventDefault();
                const prevIndex = currentIndex <= 0 ? items.length - 1 : currentIndex - 1;
                items[prevIndex]?.focus();
                break;

            case 'Home':
                e.preventDefault();
                items[0]?.focus();
                break;

            case 'End':
                e.preventDefault();
                items[items.length - 1]?.focus();
                break;

            case 'Escape':
                e.preventDefault();
                dropdown.dotNetRef.invokeMethodAsync('CloseDropdown');
                break;

            case 'Tab':
                e.preventDefault();
                dropdown.dotNetRef.invokeMethodAsync('CloseDropdown');
                break;
        }
    },

    /**
     * Cleanup dropdown resources
     * @param {string} dropdownId - Dropdown ID
     */
    disposeDropdown(dropdownId) {
        const dropdown = window.catalyst._dropdowns?.get(dropdownId);
        if (!dropdown) return;

        this.closeDropdown(dropdownId);
        window.catalyst._dropdowns.delete(dropdownId);
    },

    // ========== Dialog Functions ==========

    /**
     * Initialize a dialog with Escape key handling
     * @param {object} dotNetRef - .NET reference for callbacks
     */
    initializeDialog(dotNetRef) {
        if (!window.catalyst._dialog) {
            window.catalyst._dialog = {
                dotNetRef,
                escapeHandler: null
            };
        } else {
            window.catalyst._dialog.dotNetRef = dotNetRef;
        }

        // Add Escape key handler
        if (!window.catalyst._dialog.escapeHandler) {
            window.catalyst._dialog.escapeHandler = (e) => {
                if (e.key === 'Escape') {
                    window.catalyst._dialog.dotNetRef?.invokeMethodAsync('CloseDialog');
                }
            };
            document.addEventListener('keydown', window.catalyst._dialog.escapeHandler);
        }
    },

    /**
     * Focus the dialog panel
     * @param {Element} element - Dialog panel element
     */
    focusDialog(element) {
        if (element) {
            element.focus();
        }
    },

    /**
     * Cleanup dialog resources
     */
    disposeDialog() {
        if (window.catalyst._dialog?.escapeHandler) {
            document.removeEventListener('keydown', window.catalyst._dialog.escapeHandler);
            window.catalyst._dialog.escapeHandler = null;
        }
        window.catalyst._dialog = null;
    }
};
