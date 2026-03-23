// Custom Dropdown Logic
function toggleDropdown(dropdownId) {
    // Close other dropdowns first
    document.querySelectorAll('.custom-select-wrapper.open').forEach(wrapper => {
        if (wrapper.id !== dropdownId.replace('Dropdown', 'Wrapper')) {
            wrapper.classList.remove('open');
        }
    });

    const wrapper = document.getElementById(dropdownId.replace('Dropdown', 'Wrapper'));
    wrapper.classList.toggle('open');
}

function selectOption(filterName, value, text) {
    const wrapper = document.getElementById(filterName + 'Wrapper');
    const input = document.getElementById(filterName + 'Input');
    const displayText = document.getElementById(filterName + 'Text');
    const options = wrapper.querySelectorAll('.custom-option');

    input.value = value;
    displayText.textContent = text;

    options.forEach(opt => opt.classList.remove('active'));
    event.currentTarget.classList.add('active');

    wrapper.classList.remove('open');
}

// Close dropdowns when clicking outside
document.addEventListener('click', function (e) {
    if (!e.target.closest('.custom-select-wrapper')) {
        document.querySelectorAll('.custom-select-wrapper.open').forEach(wrapper => {
            wrapper.classList.remove('open');
        });
    }
});

function toggleFilters(isInitialization = false) {
    const wrapper = document.getElementById('filtersWrapper');
    const btnIcon = document.getElementById('toggleFiltersIcon');
    const btnText = document.getElementById('toggleFiltersText');

    if (!wrapper || !btnIcon) return;

    const isCurrentlyActive = wrapper.classList.contains('active');

    if (isInitialization) {
        if (!isCurrentlyActive) {
            wrapper.classList.add('active');
            wrapper.style.maxHeight = 'none';
            wrapper.style.overflow = 'visible';
            btnIcon.classList.replace('ph-caret-down', 'ph-caret-up');
            if (btnText) btnText.textContent = 'Hide filters';
        }
        return;
    }

    wrapper.classList.toggle('active');

    if (wrapper.classList.contains('active')) {
        wrapper.style.overflow = 'hidden';
        // Calculate exact height needed for perfect transition speed
        const scrollHeight = wrapper.scrollHeight;
        wrapper.style.maxHeight = '0px';
        wrapper.offsetHeight; // Force reflow
        wrapper.style.maxHeight = scrollHeight + 'px';

        btnIcon.classList.replace('ph-caret-down', 'ph-caret-up');
        localStorage.setItem('filtersExpanded', 'true');
        setTimeout(() => {
            if (wrapper.classList.contains('active')) {
                wrapper.style.overflow = 'visible';
                wrapper.style.maxHeight = 'none';
            }
        }, 400);
    } else {
        wrapper.style.maxHeight = wrapper.scrollHeight + 'px';
        wrapper.style.overflow = 'hidden';
        wrapper.offsetHeight; // Force reflow
        wrapper.style.maxHeight = '0px';

        btnIcon.classList.replace('ph-caret-up', 'ph-caret-down');
        localStorage.setItem('filtersExpanded', 'false');
    }
}

function clearFilters() {
    // Clear custom selects
    selectOption('sortBy', '', 'None');
    selectOption('status', '', 'Any');

    // Clear all checkboxes
    document.querySelectorAll('input[type="checkbox"][name="tagIds"]').forEach(cb => {
        cb.checked = false;
        cb.parentElement.classList.remove('selected');
    });

    // Clear search text
    const searchInput = document.querySelector('.search-input');
    if (searchInput) searchInput.value = '';
}

function feelLucky(randomUrl) {
    const form = document.getElementById('searchForm');
    if (form && randomUrl) {
        form.action = randomUrl;
        form.submit();
    }
}

// Initialization function to be called from the View
function initComicIndex(config) {
    const { hasFilters, infoMsg } = config;
    const isExpanded = localStorage.getItem('filtersExpanded') === 'true';

    if (hasFilters || isExpanded) {
        toggleFilters(true);
    }

    if (infoMsg) {
        alert(infoMsg);
    }
}
