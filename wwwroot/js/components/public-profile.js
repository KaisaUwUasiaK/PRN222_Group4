/**
 * Public Profile Logic
 */
function initPublicProfile(config) {
    // Check if SignalR is available for real-time status
    if (typeof statusConnection !== 'undefined') {
        const profileUserId = config.userId;
        const statusIndicator = document.querySelector('.status-indicator');
        const statusText = document.querySelector('.meta-item:nth-child(2) span');
        const statusIcon = document.querySelector('.meta-item:nth-child(2) i');

        statusConnection.on("UserStatusChanged", function(userId, status) {
            if (userId === profileUserId) {
                if (statusIndicator) {
                    statusIndicator.classList.remove('online', 'offline', 'banned');
                    statusIndicator.classList.add(status.toLowerCase());
                    statusIndicator.title = status;
                }
                
                if (statusText) {
                    statusText.textContent = (status === 'Online') ? 'Online Now' : status;
                }
                
                if (statusIcon) {
                    if (status === 'Online') {
                        statusIcon.className = "fa-solid fa-circle text-accent";
                    } else if (status === 'Banned') {
                        statusIcon.className = "fa-solid fa-ban text-danger";
                    } else {
                        statusIcon.className = "fa-solid fa-circle";
                    }
                }
            }
        });
    }
}
