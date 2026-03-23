/**
 * Moderation Review Logic
 */
function initModerationReview() {
    const rejectModal = document.getElementById('rejectModal');
    const hideModal = document.getElementById('hideModal');
    const modalOverlay = document.getElementById('modalOverlay');

    window.showRejectModal = function() {
        if (rejectModal) rejectModal.style.display = 'block';
        if (modalOverlay) modalOverlay.style.display = 'block';
    };

    window.closeRejectModal = function() {
        if (rejectModal) rejectModal.style.display = 'none';
        if (modalOverlay) modalOverlay.style.display = 'none';
    };

    window.showHideModal = function() {
        if (hideModal) hideModal.style.display = 'block';
        if (modalOverlay) modalOverlay.style.display = 'block';
    };

    window.closeHideModal = function() {
        if (hideModal) hideModal.style.display = 'none';
        if (modalOverlay) modalOverlay.style.display = 'none';
    };

    window.closeModals = function() {
        closeRejectModal();
        closeHideModal();
    };

    if (modalOverlay) {
        modalOverlay.addEventListener('click', closeModals);
    }
}
