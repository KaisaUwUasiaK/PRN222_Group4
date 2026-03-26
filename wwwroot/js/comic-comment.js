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
        // new comment
        const html = `
            <div class="flex gap-4 group">
                <a href="/User/PublicProfile/${comment.userId}" class="w-10 h-10 md:w-12 md:h-12 shrink-0 rounded-full bg-gray-800 flex items-center justify-center text-gray-400 font-bold overflow-hidden border border-white/10">
                    <img src="${comment.avatarUrl}" class="w-full h-full object-cover"/>
                </a>
                <div class="flex-1 w-full bg-[#161616] rounded-2xl rounded-tl-none p-4 border border-white/5">
                    <div class="flex justify-between items-start w-full mb-2">
                        <div class="flex items-center gap-2 flex-wrap">
                            <a href="/User/PublicProfile/${comment.userId}" class="font-bold text-blue-400 text-sm md:text-base">${comment.userName}</a>
                            <span class="text-xs text-gray-500"><i class="ph ph-clock"></i> Vừa xong</span>
                        </div>
                    </div>
                    <p class="text-gray-300 text-sm md:text-base leading-relaxed break-words whitespace-pre-wrap">${comment.content}</p>
                </div>
            </div>`;

        //insert in top of list
        const container = document.getElementById('comments-container');
        if (container) container.insertAdjacentHTML('afterbegin', html);

        // Delete comment message
        const noCmt = document.querySelector('.ph-chat-teardrop-slash');
        if (noCmt) noCmt.parentElement.remove();
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
                    alert("Có lỗi xảy ra hoặc bạn bình luận quá nhanh.");
                }
            }).finally(() => {
                submitBtn.disabled = false;
            });
        });
    }
});