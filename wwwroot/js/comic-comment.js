function updateTotalCommentCount(change) {
    const countSpan = document.getElementById('comment-total-count');
    if (countSpan) {
        let currentCount = parseInt(countSpan.innerText) || 0;
        let newCount = currentCount + change;
        countSpan.innerText = newCount > 0 ? newCount : 0; 
    }
}
document.addEventListener('DOMContentLoaded', function () {
    const config = window.commentConfig;
    if (!config || !config.chapterId) return;

    const currentChapterId = config.chapterId;
    const currentUserId = config.currentUserId;

    // --- part 1: SIGNALR (send message) ---
    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/commentHub")
        .build();

    connection.start().then(() => {
        connection.invoke("JoinChapter", currentChapterId);
    }).catch(err => console.error(err));

    connection.on("ReceiveComment", (comment) => {
        // Check if the comment belongs to the current user
        const isOwnComment = currentUserId == comment.userId;

        // If it's the user's comment, generate the Delete form
        const deleteHtml = isOwnComment ? `
            <form action="/Comic/DeleteComment" method="post" class="delete-comment-form" onsubmit="return confirm('Are you sure you want to delete this comment?');">
                <input type="hidden" name="commentId" value="${comment.commentId}" />
                <input type="hidden" name="chapterId" value="${currentChapterId}" />
                <input type="hidden" name="source" value="Read" />
                <input type="hidden" name="comicId" value="${config.comicId}" />

                <button type="submit" class="text-gray-600 hover:text-red-500 transition-colors p-1" title="Delete">
                    <i class="ph ph-trash text-base md:text-lg"></i>
                </button>
            </form>` : '';

        // New comment (Add ID to the outer div for easy deletion)
        const html = `
            <div class="flex gap-4 group" id="read-comment-${comment.commentId}">
                <a href="/User/PublicProfile/${comment.userId}" class="w-10 h-10 md:w-12 md:h-12 shrink-0 rounded-full bg-gray-800 flex items-center justify-center text-gray-400 font-bold overflow-hidden border border-white/10">
                    <img src="${comment.avatarUrl}" class="w-full h-full object-cover" onerror="this.style.display='none'"/>
                </a>
                <div class="flex-1 w-full bg-[#161616] rounded-2xl rounded-tl-none p-4 border border-white/5">
                    <div class="flex justify-between items-start w-full mb-2">
                        <div class="flex items-center gap-2 flex-wrap">
                            <a href="/User/PublicProfile/${comment.userId}" class="font-bold text-blue-400 text-sm md:text-base">${comment.userName}</a>
                            <span class="text-xs text-gray-500"><i class="ph ph-clock"></i> Just now</span>
                        </div>
                        <div class="flex items-center gap-1">
                            ${deleteHtml}
                        </div>
                    </div>
                    <p class="text-gray-300 text-sm md:text-base leading-relaxed break-words whitespace-pre-wrap">${comment.content}</p>
                </div>
            </div>`;

        // Insert at the top of the list
        const container = document.getElementById('comments-container');
        if (container) container.insertAdjacentHTML('afterbegin', html);

        // Remove the "No comments yet" message if it exists
        const noCmt = document.querySelector('.ph-chat-teardrop-slash');
        if (noCmt) noCmt.parentElement.remove();
        updateTotalCommentCount(1);
    });

    // Catch the delete comment event from SignalR to hide the UI
    connection.on("RemoveComment", (commentId) => {
        const cmtBox = document.getElementById(`read-comment-${commentId}`);
        if (cmtBox) {
            cmtBox.remove();
            updateTotalCommentCount(-1); 
        }
    });

    // --- part 2: AJAX (Send form) ---
    const commentForm = document.querySelector('form[action="/Comic/AddComment"]');
    if (commentForm) {
        commentForm.addEventListener('submit', function (e) {
            e.preventDefault();

            const submitBtn = this.querySelector('button[type="submit"]');
            submitBtn.disabled = true;

            fetch(this.action, {
                method: 'POST',
                body: new FormData(this)
            }).then(res => {
                if (res.ok) {
                    this.querySelector('textarea').value = '';
                } else {
                    alert("An error occurred or you are commenting too fast.");
                }
            }).finally(() => {
                submitBtn.disabled = false;
            });
        });
    }
    document.addEventListener('submit', async function (e) {
        if (e.target && e.target.closest('form') && e.target.closest('form').action.includes('DeleteComment')) {
            e.preventDefault();

            const form = e.target.closest('form');

            try {
                const response = await fetch(form.action, {
                    method: 'POST',
                    body: new FormData(form)
                });

                if (response.ok) {
                    const commentBox = form.closest('.group');
                    if (commentBox) commentBox.remove();
                } else {
                    alert("Không thể xóa bình luận lúc này.");
                }
            } catch (error) {
                console.error("Lỗi khi xóa:", error);
            }
        }
    });
});