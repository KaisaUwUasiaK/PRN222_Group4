/**
 * Global Layout Logic
 */

function initLayout(config) {
    // 1. Notyf Initialization
    const notyf = new Notyf({
        duration: 4000,
        position: { x: 'right', y: 'top' },
        dismissible: true,
        types: [
            { type: 'success', background: '#10B981', icon: { className: 'fa-solid fa-circle-check', tagName: 'i', color: 'white' } },
            { type: 'error',   background: '#EF4444', icon: { className: 'fa-solid fa-circle-xmark', tagName: 'i', color: 'white' } },
            { type: 'info',    background: '#6366F1', icon: { className: 'fa-solid fa-circle-info',  tagName: 'i', color: 'white' } },
            { type: 'warning', background: '#F59E0B', icon: { className: 'fa-solid fa-triangle-exclamation', tagName: 'i', color: 'white' } }
        ]
    });

    // 2. Handle TempData Notifications
    if (config.notifications) {
        if (config.notifications.success) notyf.success(config.notifications.success);
        if (config.notifications.error) notyf.error(config.notifications.error);
        if (config.notifications.warning) notyf.open({ type: 'warning', message: config.notifications.warning });
        if (config.notifications.info) notyf.open({ type: 'info', message: config.notifications.info });
    }

    // 3. UserStatus SignalR (if authenticated)
    if (config.isAuthenticated && config.currentUserId) {
        const statusConnection = new signalR.HubConnectionBuilder()
            .withUrl("/hubs/userStatus")
            .withAutomaticReconnect()
            .build();

        statusConnection.on("UserStatusChanged", function (userId, status) {
            if (userId === config.currentUserId.toString() && status === "Banned") {
                window.location.href = "/Authentication/AccountLocked";
            }
        });

        statusConnection.start().catch(err => console.error("SignalR connection error:", err));
    }

    // 4. Dropdown Toggles (Generic support for various layouts)
    document.querySelectorAll('.dropdown-trigger, .user-trigger').forEach(trigger => {
        trigger.addEventListener('click', function(e) {
            e.stopPropagation();
            const dropdown = this.closest('.dropdown, .nav-user');
            if (dropdown) {
                dropdown.classList.toggle('active');
                // For layouts that use 'open' instead of 'active'
                dropdown.classList.toggle('open');
            }
        });
    });

    document.addEventListener('click', function() {
        document.querySelectorAll('.dropdown.active, .dropdown.open, .nav-user.active, .nav-user.open').forEach(d => {
            d.classList.remove('active');
            d.classList.remove('open');
        });
    });
}

/**
 * Global Search Handler
 */
function handleSearchKeyPress(event) {
    if (event.key === 'Enter') {
        const query = event.target.value.trim();
        if (query) {
            window.location.href = '/Comic/Index?search=' + encodeURIComponent(query);
        }
    }
}
