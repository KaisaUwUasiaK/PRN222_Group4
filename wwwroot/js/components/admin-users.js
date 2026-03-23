/**
 * Admin Users Management
 */
function initAdminUsers(config) {
    const modal = document.getElementById('createModModal');
    const form = document.getElementById('createModForm');
    const hiddenForm = document.getElementById('hiddenCreateForm');
    const createBtn = document.getElementById('createModBtn');

    if (modal) {
        // Close on overlay click
        modal.addEventListener('click', function (e) {
            if (e.target === this) this.classList.remove('active');
        });
        
        const card = modal.querySelector('.modal-card');
        if (card) {
            card.addEventListener('click', function (e) {
                e.stopPropagation();
            });
        }
    }

    if (createBtn) {
        createBtn.addEventListener('click', function () {
            submitCreateMod();
        });
    }

    function submitCreateMod() {
        // Clear errors
        ['username', 'email', 'password', 'confirm'].forEach(function (f) {
            const errEl = document.getElementById('err-' + f);
            const inputEl = document.getElementById('mod-' + f);
            if (errEl) errEl.textContent = '';
            if (inputEl) inputEl.style.borderColor = '';
        });

        const username = document.getElementById('mod-username').value.trim();
        const email    = document.getElementById('mod-email').value.trim();
        const password = document.getElementById('mod-password').value;
        const confirm  = document.getElementById('mod-confirm').value;

        // Validate
        let hasError = false;
        if (!username || username.length < 3) {
            showFieldError('username', 'Username must be at least 3 characters.');
            hasError = true;
        }
        if (!email || email.indexOf('@') < 0) {
            showFieldError('email', 'Please enter a valid email address.');
            hasError = true;
        }
        if (!password || password.length < 6) {
            showFieldError('password', 'Password must be at least 6 characters.');
            hasError = true;
        }
        if (password !== confirm) {
            showFieldError('confirm', 'Passwords do not match.');
            hasError = true;
        }
        if (hasError) return;

        // Fill hidden form and submit
        document.getElementById('hf-username').value = username;
        document.getElementById('hf-email').value    = email;
        document.getElementById('hf-password').value = password;
        document.getElementById('hf-confirm').value  = confirm;
        if (hiddenForm) hiddenForm.submit();
    }

    function showFieldError(field, msg) {
        const errEl = document.getElementById('err-' + field);
        const inputEl = document.getElementById('mod-' + field);
        if (errEl) errEl.textContent = msg;
        if (inputEl) inputEl.style.borderColor = 'var(--color-danger)';
    }

    // SignalR realtime status
    if (typeof statusConnection !== 'undefined') {
        statusConnection.on("UserOnline",  function (userId) { updateStatus(userId, 'Online'); });
        statusConnection.on("UserOffline", function (userId) { updateStatus(userId, 'Offline'); });
        statusConnection.on("UserBanned",  function (userId) { updateStatus(userId, 'Banned'); });
    }

    function updateStatus(userId, status) {
        const dot      = document.getElementById('status-dot-' + userId);
        const badge    = document.getElementById('status-badge-' + userId);
        const colorDot = document.getElementById('status-color-dot-' + userId);
        const text     = document.getElementById('status-text-' + userId);
        if (!dot || !badge || !text) return;

        const cfgMap = {
            'Online':  { color: 'var(--color-accent)', cls: 'badge badge-online'  },
            'Offline': { color: 'var(--text-muted)',   cls: 'badge badge-offline' },
            'Banned':  { color: 'var(--color-danger)', cls: 'badge badge-banned'  }
        };
        const cfg = cfgMap[status] || cfgMap['Offline'];

        dot.style.background = cfg.color;
        if (colorDot) colorDot.style.background = cfg.color;
        badge.className = cfg.cls;
        text.textContent = status;

        let count = 0;
        document.querySelectorAll('[id^="status-text-"]').forEach(function (el) {
            if (el.textContent === 'Online') count++;
        });
        const onlineCountEl = document.getElementById('onlineCount');
        if (onlineCountEl) onlineCountEl.textContent = count;
    }
}
