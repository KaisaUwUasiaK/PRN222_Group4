/**
 * Tag Management Logic
 */
function initTagManagement() {
    const modal = document.getElementById('tagConfirmModal');
    const titleEl = document.getElementById('tagConfirmTitle');
    const messageEl = document.getElementById('tagConfirmMessage');
    const cancelBtn = document.getElementById('tagConfirmCancelBtn');
    const closeBtn = document.getElementById('tagConfirmCloseBtn');
    const okBtn = document.getElementById('tagConfirmOkBtn');

    if (!modal) return;

    let onConfirm = null;

    function openModal(options) {
        if (titleEl) titleEl.textContent = options.title;
        if (messageEl) messageEl.textContent = options.message;
        if (okBtn) {
            okBtn.textContent = options.okText || 'Confirm';
            okBtn.className = options.okClass || 'btn btn-primary';
        }
        if (cancelBtn) cancelBtn.style.display = options.showCancel === false ? 'none' : 'inline-flex';
        onConfirm = options.onConfirm || null;
        modal.classList.add('active');
    }

    function closeModal() {
        modal.classList.remove('active');
        onConfirm = null;
    }

    if (okBtn) {
        okBtn.addEventListener('click', function () {
            if (onConfirm) {
                onConfirm();
            }
            closeModal();
        });
    }

    if (cancelBtn) cancelBtn.addEventListener('click', closeModal);
    if (closeBtn) closeBtn.addEventListener('click', closeModal);

    modal.addEventListener('click', function (e) {
        if (e.target === modal) {
            closeModal();
        }
    });

    document.querySelectorAll('.js-confirm-edit').forEach(function (btn) {
        btn.addEventListener('click', function (e) {
            e.preventDefault();

            const tagName = btn.dataset.tagName || 'this tag';
            const targetUrl = btn.getAttribute('href');

            openModal({
                title: 'Open Edit Form',
                message: `You are about to edit tag "${tagName}". Please review changes carefully before saving.`,
                okText: 'Continue',
                okClass: 'btn btn-primary',
                onConfirm: function () {
                    window.location.href = targetUrl;
                }
            });
        });
    });

    document.querySelectorAll('.js-confirm-delete').forEach(function (btn) {
        btn.addEventListener('click', function () {
            const tagName = btn.dataset.tagName || 'this tag';
            const usageCount = parseInt(btn.dataset.usageCount || '0', 10);
            const formId = btn.dataset.formId;

            if (usageCount > 0) {
                openModal({
                    title: 'Delete Blocked',
                    message: `Tag "${tagName}" is currently used by ${usageCount} comic(s). Remove this tag from related comics before deleting.`,
                    okText: 'Understood',
                    okClass: 'btn btn-ghost',
                    showCancel: false
                });
                return;
            }

            openModal({
                title: 'Confirm Delete',
                message: `Delete tag "${tagName}"? This action cannot be undone.`,
                okText: 'Delete Tag',
                okClass: 'btn btn-primary',
                onConfirm: function () {
                    const form = document.getElementById(formId);
                    if (form) {
                        form.submit();
                    }
                }
            });
        });
    });
}
