function initChapterManagement() {
    const fileInput = document.getElementById('fileInput');
    if (fileInput) {
        fileInput.addEventListener('change', function (e) {
            const preview = document.getElementById('fileListPreview');
            if (!preview) return;
            preview.innerHTML = '';

            if (this.files && this.files.length > 0) {
                const filesArray = Array.from(this.files).sort((a, b) =>
                    a.name.localeCompare(b.name, undefined, { numeric: true, sensitivity: 'base' })
                );

                const fileCount = filesArray.length;
                preview.innerHTML = `<div class="file-preview-header">Selected ${fileCount} files in order:</div>`;

                const maxDisplay = 5;
                const ul = document.createElement('ul');
                ul.className = 'file-preview-list';

                for (let i = 0; i < Math.min(fileCount, maxDisplay); i++) {
                    const li = document.createElement('li');
                    li.className = 'file-preview-item';
                    li.innerHTML = `<span class="file-preview-number">${String(i + 1).padStart(2, '0')}.</span> <i class="fa-solid fa-image file-preview-icon"></i> ${filesArray[i].name}`;
                    ul.appendChild(li);
                }

                if (fileCount > maxDisplay) {
                    const li = document.createElement('li');
                    li.className = 'file-preview-more';
                    li.innerHTML = `...and ${fileCount - maxDisplay} more files.`;
                    ul.appendChild(li);
                }

                preview.appendChild(ul);
            }
        });
    }
}

// Global exposure
window.initChapterManagement = initChapterManagement;
