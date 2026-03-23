function initEditChapter() {
    const fileInput = document.getElementById('fileInput');
    if (fileInput) {
        fileInput.addEventListener('change', function (e) {
            const preview = document.getElementById('fileListPreview');
            if (!preview) return;
            preview.innerHTML = '';

            if (this.files && this.files.length > 0) {
                const fileCount = this.files.length;
                preview.innerHTML = `<div class="file-preview-header">New selection of ${fileCount} files:</div>`;

                const maxDisplay = 5;
                const ul = document.createElement('ul');
                ul.className = 'file-preview-list';

                for (let i = 0; i < Math.min(fileCount, maxDisplay); i++) {
                    const li = document.createElement('li');
                    li.className = 'file-preview-item';
                    li.innerHTML = `<i class="fa-solid fa-image file-preview-icon"></i> ${this.files[i].name}`;
                    ul.appendChild(li);
                }

                if (fileCount > maxDisplay) {
                    const li = document.createElement('li');
                    li.className = 'file-preview-more';
                    li.innerHTML = `...and ${fileCount - maxDisplay} other files.`;
                    ul.appendChild(li);
                }

                preview.appendChild(ul);
            }
        });
    }
}

// Global exposure
window.initEditChapter = initEditChapter;
