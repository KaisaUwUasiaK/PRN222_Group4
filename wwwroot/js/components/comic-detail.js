document.addEventListener('DOMContentLoaded', function () {
    const modal = document.getElementById("chaptersModal");
    const btn = document.getElementById("openChaptersModal");
    const span = document.getElementById("closeChaptersModal");

    if (btn && modal && span) {
        btn.onclick = function () {
            modal.classList.add("show");
            document.body.style.overflow = "hidden";
        }

        span.onclick = function () {
            modal.classList.remove("show");
            document.body.style.overflow = "auto";
        }

        window.onclick = function (event) {
            if (event.target == modal) {
                modal.classList.remove("show");
                document.body.style.overflow = "auto";
            }
        }
    }
});
