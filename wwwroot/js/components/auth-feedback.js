/**
 * Auth Feedback (Notyf) initialization
 */
function initAuthFeedback(config) {
    const notyf = new Notyf({
        duration: 4000,
        position: { x: 'right', y: 'top' },
        dismissible: true
    });

    if (config.success) {
        notyf.success(config.success);
    }
    
    if (config.error) {
        notyf.error(config.error);
    }

    if (config.warning) {
        notyf.open({ type: 'warning', message: config.warning });
    }

    if (config.info) {
        notyf.open({ type: 'info', message: config.info });
    }

    // Handle validation summary errors
    document.querySelectorAll('.validation-summary-errors li').forEach(function (item) {
        const msg = item.textContent.trim();
        if (msg && msg !== "") {
            notyf.error(msg);
        }
    });
}
