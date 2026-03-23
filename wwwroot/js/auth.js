/**
 * Auth Pages Logic (Login, Register, ForgotPassword)
 */

function initAuth(config) {
    // 1. Notyf Initialization
    const notyf = new Notyf({
        duration: 4000,
        position: { x: 'right', y: 'top' },
        dismissible: true
    });

    // 2. TempData Notifications
    if (config.notifications) {
        if (config.notifications.success) notyf.success(config.notifications.success);
        if (config.notifications.error) notyf.error(config.notifications.error);
        if (config.notifications.warning) notyf.open({ type: 'warning', message: config.notifications.warning });
        if (config.notifications.info) notyf.open({ type: 'info', message: config.notifications.info });
    }

    // 3. ASP.NET Validation Summary to Notyf
    const errorItems = document.querySelectorAll('.validation-summary-errors li');
    if (errorItems.length) {
        errorItems.forEach(item => {
            const msg = item.textContent.trim();
            if (msg && msg !== "") {
                notyf.error(msg);
            }
        });
    }

    // 4. Password Visibility Toggles
    document.querySelectorAll('.pwd-toggle').forEach(btn => {
        btn.addEventListener('click', function() {
            const inputId = this.getAttribute('data-toggle-for') || (this.previousElementSibling ? this.previousElementSibling.id : null);
            if (!inputId) return;
            
            const input = document.getElementById(inputId);
            const icon = this.querySelector('i');
            
            if (input && icon) {
                const isHidden = input.type === 'password';
                input.type = isHidden ? 'text' : 'password';
                icon.className = isHidden ? 'ph ph-eye-slash' : 'ph ph-eye';
            }
        });
    });
}
