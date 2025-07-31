document.addEventListener('DOMContentLoaded', function() {
    // Get data from global variables set by the view
    let selectedTypeIds = window.selectedTypeIds || [];
    let selectedVersionIds = window.selectedVersionIds || [];

    function bindFilterEvents() {
        // Chips
        document.getElementById('typeChips').addEventListener('click', function (e) {
            const chip = e.target.closest('.chip');
            if (!chip) return;
            const id = parseInt(chip.getAttribute('data-typeid'));
            if (isNaN(id)) return;
            
            if (chip.classList.contains('selected')) {
                // If already selected, deselect it
                selectedTypeIds = selectedTypeIds.filter(tid => tid !== id);
            } else {
                // If not selected, select it
                selectedTypeIds.push(id);
            }
            updateFilter();
        });
        
        document.getElementById('versionChips').addEventListener('click', function (e) {
            const chip = e.target.closest('.chip');
            if (!chip) return;
            const id = parseInt(chip.getAttribute('data-versionid'));
            if (isNaN(id)) return;
            
            if (chip.classList.contains('selected')) {
                // If already selected, deselect it
                selectedVersionIds = selectedVersionIds.filter(vid => vid !== id);
            } else {
                // If not selected, select it
                selectedVersionIds.push(id);
            }
            updateFilter();
        });
        
        // Search
        const searchInput = document.getElementById('searchInput');
        let timeoutId;
        searchInput.addEventListener('input', function () {
            clearTimeout(timeoutId);
            timeoutId = setTimeout(() => {
                updateFilter();
            }, 300);
        });
    }

    function updateFilter() {
        const params = new URLSearchParams();
        if (selectedTypeIds.length > 0) params.append('typeIds', selectedTypeIds.join(','));
        if (selectedVersionIds.length > 0) params.append('versionIds', selectedVersionIds.join(','));
        const searchInput = document.getElementById('searchInput');
        const searchValue = searchInput ? searchInput.value : '';
        const searchFocused = searchInput === document.activeElement;
        const searchCursorPosition = searchInput ? searchInput.selectionStart : 0;
        
        if (searchValue) params.append('searchTerm', searchValue);

        const newUrl = window.location.pathname + '?' + params.toString();
        window.history.pushState({ path: newUrl }, '', newUrl);

        fetch(`/Movie/MovieList?${params.toString()}`, {
            headers: { "X-Requested-With": "XMLHttpRequest" }
        })
        .then(response => response.text())
        .then(html => {
            document.getElementById("movieFilterAndGrid").innerHTML = html;
            
            // Restore search input state
            const newSearchInput = document.getElementById('searchInput');
            if (newSearchInput && searchValue) {
                newSearchInput.value = searchValue;
                if (searchFocused) {
                    newSearchInput.focus();
                    newSearchInput.setSelectionRange(searchCursorPosition, searchCursorPosition);
                }
            }
            
            bindFilterEvents(); // Re-bind after AJAX update
        });
    }

    // Initial binding
    bindFilterEvents();
});
