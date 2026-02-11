// Event Delegation for Dropdown (to support dynamic content)
document.addEventListener('click', (e) => {
    // Check if clicked element is a dropdown trigger
    const trigger = e.target.closest('.dropdown-trigger');

    if (trigger) {
        e.stopPropagation();
        e.preventDefault();

        const currentDropdown = trigger.closest('.dropdown');

        // Close other open dropdowns first
        document.querySelectorAll('.dropdown.active').forEach(activeItem => {
            if (activeItem !== currentDropdown) {
                activeItem.classList.remove('active');
            }
        });

        // Toggle current dropdown
        if (currentDropdown) {
            currentDropdown.classList.toggle('active');
        }
        return;
    }

    // Close when clicking outside (if not clicking inside a dropdown content)
    if (!e.target.closest('.dropdown-content')) {
        document.querySelectorAll('.dropdown.active').forEach(dropdown => {
            dropdown.classList.remove('active');
        });
    }
});
