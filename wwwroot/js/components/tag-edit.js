function initTagEdit() {
    const form = document.getElementById('editTagForm');
    const saveBtn = document.getElementById('saveTagBtn');
    const modal = document.getElementById('editConfirmModal');
    const okBtn = document.getElementById('editConfirmOkBtn');
    const cancelBtn = document.getElementById('editConfirmCancelBtn');
    const closeBtn = document.getElementById('editConfirmCloseBtn');

    if (!saveBtn || !modal || !okBtn || !cancelBtn || !closeBtn) return;

    function openModal() {
        modal.classList.add('active');
    }

    function closeModal() {
        modal.classList.remove('active');
    }

    saveBtn.addEventListener('click', openModal);
    cancelBtn.addEventListener('click', closeModal);
    closeBtn.addEventListener('click', closeModal);

    modal.addEventListener('click', function (e) {
        if (e.target === modal) {
            closeModal();
        }
    });

    okBtn.addEventListener('click', function () {
        closeModal();
        if (form) {
            form.requestSubmit();
        }
    });
}

// Global exposure
window.initTagEdit = initTagEdit;
