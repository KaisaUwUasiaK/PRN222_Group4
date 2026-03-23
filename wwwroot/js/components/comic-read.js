function initComicRead() {
    window.onscroll = function() {
        let btn = document.getElementById("scrollTop");
        if (document.body.scrollTop > 500 || document.documentElement.scrollTop > 500) {
            btn.style.display = "grid";
        } else {
            btn.style.display = "none";
        }
    };
}

function openReportModal(commentId, targetUserId, targetUsername) {
    const commentInput = document.getElementById('reportCommentId');
    const targetUserIdInput = document.getElementById('reportTargetUserId');
    const targetNameSpan = document.getElementById('reportTargetName');
    const modal = document.getElementById('reportModal');

    if (commentInput) commentInput.value = commentId;
    if (targetUserIdInput) targetUserIdInput.value = targetUserId;
    if (targetNameSpan) targetNameSpan.textContent = targetUsername;
    if (modal) modal.classList.add('active');
}

function closeReportModal() {
    const modal = document.getElementById('reportModal');
    if (modal) {
        modal.classList.remove('active');
    }
}

// Global expose for onclick handlers in HTML
window.openReportModal = openReportModal;
window.closeReportModal = closeReportModal;
window.initComicRead = initComicRead;
