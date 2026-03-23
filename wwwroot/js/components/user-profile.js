/**
 * User Profile Logic
 */
function initUserProfile(config) {
    const notyfInstance = typeof notyf !== 'undefined' ? notyf : new Notyf({
        duration: 4000,
        position: { x: 'right', y: 'top' },
        dismissible: true
    });

    const avatarFileInput = document.getElementById('avatarFile');
    const avatarImage = document.getElementById('avatarImage');

    if (avatarFileInput) {
        avatarFileInput.addEventListener('change', function(e) {
            const file = e.target.files[0];
            if (!file) return;

            const allowedTypes = ['image/jpeg', 'image/jpg', 'image/png', 'image/gif'];
            if (!allowedTypes.includes(file.type)) {
                notyfInstance.error('Invalid file type. Only JPG, PNG, and GIF are allowed.');
                return;
            }

            if (file.size > 5 * 1024 * 1024) {
                notyfInstance.error('File size exceeds 5MB limit.');
                return;
            }

            const reader = new FileReader();
            reader.onload = function(e) {
                if (avatarImage) avatarImage.src = e.target.result;
            };
            reader.readAsDataURL(file);

            const formData = new FormData();
            formData.append('avatarFile', file);
            const token = document.querySelector('input[name="__RequestVerificationToken"]');
            if (token) {
                formData.append('__RequestVerificationToken', token.value);
            }

            fetch(config.uploadAvatarUrl, {
                method: 'POST',
                body: formData
            })
            .then(response => response.json())
            .then(data => {
                if (data.success) {
                    if (avatarImage) avatarImage.src = data.avatarUrl;
                    notyfInstance.success('Avatar updated successfully!');
                    document.dispatchEvent(new CustomEvent('avatarUpdated', { detail: data.avatarUrl }));
                } else {
                    notyfInstance.error(data.message || 'Failed to upload avatar.');
                }
            })
            .catch(error => {
                console.error('Error:', error);
                notyfInstance.error('An error occurred while uploading the avatar.');
            });
        });
    }

    // Delete Account Modal
    const deleteModal = document.getElementById('deleteModal');
    const deleteBtn = document.getElementById('deleteAccountBtn');
    const deleteModalInput = document.getElementById('deleteModalInput');
    const deleteModalError = document.getElementById('deleteModalError');
    const deleteModalConfirm = document.getElementById('deleteModalConfirm');
    const deleteModalCancel = document.getElementById('deleteModalCancel');
    const deleteForm = document.getElementById('deleteForm');
    const deleteConfirmText = document.getElementById('deleteConfirmText');

    if (deleteBtn && deleteModal) {
        deleteBtn.addEventListener('click', function () {
            if (deleteModalInput) {
                deleteModalInput.value = '';
                deleteModalInput.classList.remove('input-error');
            }
            if (deleteModalError) deleteModalError.textContent = '';
            deleteModal.classList.add('active');
            setTimeout(() => { if (deleteModalInput) deleteModalInput.focus(); }, 150);
        });
    }

    if (deleteModalCancel) {
        deleteModalCancel.addEventListener('click', () => deleteModal.classList.remove('active'));
    }

    if (deleteModal) {
        deleteModal.addEventListener('click', function (e) {
            if (e.target === deleteModal) deleteModal.classList.remove('active');
        });
    }

    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape' && deleteModal && deleteModal.classList.contains('active')) {
            deleteModal.classList.remove('active');
        }
    });

    if (deleteModalConfirm) {
        deleteModalConfirm.addEventListener('click', function () {
            const val = deleteModalInput.value.trim();
            if (val !== 'DELETE') {
                if (deleteModalError) deleteModalError.textContent = 'Please type exactly "DELETE" to confirm.';
                if (deleteModalInput) {
                    deleteModalInput.classList.add('input-error');
                    deleteModalInput.addEventListener('animationend', function () {
                        deleteModalInput.classList.remove('input-error');
                    }, { once: true });
                }
                return;
            }
            if (deleteConfirmText) deleteConfirmText.value = val;
            if (deleteForm) deleteForm.submit();
        });
    }

    if (deleteModalInput) {
        deleteModalInput.addEventListener('input', function () {
            if (deleteModalError) deleteModalError.textContent = '';
            deleteModalInput.classList.remove('input-error');
        });
    }
}
