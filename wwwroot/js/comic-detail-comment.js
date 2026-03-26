document.addEventListener('DOMContentLoaded', function () {
    const config = window.detailConfig;
    if (!config || !config.comicId) return;

    const currentComicId = config.comicId;
    const currentUserId = config.currentUserId;

    // 1. Initiate SignalR
    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/commentHub")
        .build();

    // 2. join group
    connection.start().then(() => {
        console.log("SignalR Connected for Comic: " + currentComicId);
        connection.invoke("JoinComic", currentComicId);
    }).catch(err => console.error(err));

    // 3. Listen "ReceiveComicComment" from Controller
    connection.on("ReceiveComicComment", (comment) => {
        const isOwnComment = currentUserId == comment.userId;

        // Generate delete button if it is from own comment
        const deleteHtml = isOwnComment ? `
            <form action="/Comic/DeleteComment" method="post" style="margin-left: auto;">
                <input type="hidden" name="commentId" value="${comment.commentId}" />
                <input type="hidden" name="source" value="Detail" />
                <input type="hidden" name="comicId" value="${currentComicId}" />
                <button type="submit" style="background:none; border:none; color: #F87171; cursor:pointer;" title="Delete comment">
                    <i class="fa-solid fa-trash"></i>
                </button>
            </form>` : '';

        // Generate HTML comment block
        const html = `
            <div class="comment-card" id="detail-comment-${comment.commentId}">
                <div class="comment-header">
                    <a href="/User/PublicProfile/${comment.userId}" style="display: flex;">
                        <img src="${comment.avatarUrl}" class="comment-avatar" alt="Avatar" onerror="this.style.display='none'">
                    </a>
                    <div class="comment-author-info">
                        <a href="/User/PublicProfile/${comment.userId}" class="comment-author" style="text-decoration: none; color: inherit; transition: color 0.2s;">
                            ${comment.userName}
                        </a>
                        <span class="comment-meta">
                            Chapter ${comment.chapterNumber} • Vừa xong
                        </span>
                    </div>
                    ${deleteHtml}
                </div>
                <p class="comment-text">${comment.content}</p>
            </div>`;

        // Push comment to the top
        const container = document.getElementById('detail-comments-container');
        if (container) {
            container.insertAdjacentHTML('afterbegin', html);
        }

        // Delete  "No comments yet"
        const noCmt = document.querySelector('.comments-list p');
        if (noCmt && noCmt.textContent.includes('No comments yet')) {
            noCmt.parentElement.remove();
        }
    });

    // 4. Catch RemoveComment 
    connection.on("RemoveComment", (commentId) => {
        const cmtBox = document.getElementById(`detail-comment-${commentId}`);
        if (cmtBox) cmtBox.remove();
    });
});