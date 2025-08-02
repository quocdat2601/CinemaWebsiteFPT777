/* Admin Table Sorting Functionality */

// Global sort state for booking management
let currentSort = { by: '', dir: 'asc', param: '' };

// Immediate fallback functions to ensure they're available
window.searchBookings = window.searchBookings || function() {
    console.log('Immediate fallback searchBookings called');
    const keyword = document.getElementById('searchKeyword')?.value || '';
    const status = document.getElementById('statusFilter')?.value || '';
    let bookingType = localStorage.getItem('bookingTypeFilter') || 'all';
    console.log('Calling loadTab with params:', { keyword, statusFilter: status, bookingTypeFilter: bookingType });
    if (typeof window.loadTab === 'function') {
        window.loadTab('BookingMg', { keyword, statusFilter: status, bookingTypeFilter: bookingType });
    }
};

window.resetFilters = window.resetFilters || function() {
    console.log('Immediate fallback resetFilters called');
    localStorage.removeItem('bookingSort');
    localStorage.removeItem('bookingTypeFilter');
    if (typeof window.loadTab === 'function') {
        window.loadTab('BookingMg', { bookingTypeFilter: 'all' });
    }
};

// Booking table sorting
$(document).on('click', '#booking-table .sortable', function() {
    const sortBy = $(this).data('sort');
    if (currentSort.by === sortBy) {
        currentSort.dir = currentSort.dir === 'asc' ? 'desc' : 'asc';
    } else {
        currentSort.by = sortBy;
        currentSort.dir = 'asc';
    }
    let sortParam = '';
    if (sortBy === 'id') sortParam = currentSort.dir === 'asc' ? 'id_asc' : 'id_desc';
    if (sortBy === 'movie') sortParam = currentSort.dir === 'asc' ? 'movie_az' : 'movie_za';
    if (sortBy === 'account') sortParam = currentSort.dir === 'asc' ? 'account_az' : 'account_za';
    if (sortBy === 'identity') sortParam = currentSort.dir === 'asc' ? 'identity_az' : 'identity_za';
    if (sortBy === 'phone') sortParam = currentSort.dir === 'asc' ? 'phone_az' : 'phone_za';
    if (sortBy === 'time') sortParam = currentSort.dir === 'asc' ? 'time_asc' : 'time_desc';
    currentSort.param = sortParam;
    saveSortState();
    searchBookings();
    updateSortIcons();
});

window.searchBookings = function() {
    console.log('searchBookings called');
    const keyword = document.getElementById('searchKeyword')?.value || '';
    const status = document.getElementById('statusFilter')?.value || '';

    // Get booking type from localStorage if available, otherwise from DOM
    let bookingType = localStorage.getItem('bookingTypeFilter');
    if (!bookingType) {
        bookingType = document.querySelector('input[name="bookingTypeFilter"]:checked')?.value || 'all';
    }

    console.log('Calling loadTab with params:', { keyword, statusFilter: status, bookingTypeFilter: bookingType, sortBy: currentSort.param });
    
    if (typeof window.loadTab === 'function') {
        window.loadTab('BookingMg', { keyword, statusFilter: status, bookingTypeFilter: bookingType, sortBy: currentSort.param });
    } else {
        console.error('loadTab function not found');
    }
}

function updateSortIcons() {
    $('#sortIconId, #sortIconMovie, #sortIconAccount, #sortIconIdentity, #sortIconPhone, #sortIconTime').html('');
    if (currentSort.by === 'id') {
        $('#sortIconId').html(currentSort.dir === 'asc' ? '▲' : '▼');
    }
    if (currentSort.by === 'movie') {
        $('#sortIconMovie').html(currentSort.dir === 'asc' ? '▲' : '▼');
    }
    if (currentSort.by === 'account') {
        $('#sortIconAccount').html(currentSort.dir === 'asc' ? '▲' : '▼');
    }
    if (currentSort.by === 'identity') {
        $('#sortIconIdentity').html(currentSort.dir === 'asc' ? '▲' : '▼');
    }
    if (currentSort.by === 'phone') {
        $('#sortIconPhone').html(currentSort.dir === 'asc' ? '▲' : '▼');
    }
    if (currentSort.by === 'time') {
        $('#sortIconTime').html(currentSort.dir === 'asc' ? '▲' : '▼');
    }
}

// Save sort state to localStorage
function saveSortState() {
    localStorage.setItem('bookingSort', JSON.stringify(currentSort));
}

// Restore sort state when tab is loaded
function restoreSortState() {
    const saved = localStorage.getItem('bookingSort');
    if (saved) {
        const s = JSON.parse(saved);
        if (s && s.by) {
            currentSort.by = s.by;
            currentSort.dir = s.dir;
            currentSort.param = s.param;
            updateSortIcons();
        }
    }
}

// Initialize when BookingMg tab is loaded
$(document).on('tabContentLoaded', function(e, tabName) {
    console.log('tabContentLoaded triggered for:', tabName);
    if (tabName === 'BookingMg') {
        console.log('Initializing BookingMg tab');
        restoreSortState();
        // Use a longer delay to ensure content is fully rendered
        setTimeout(function() {
            restoreRadioButtonState();
        }, 200);
        if (currentSort.param) {
            searchBookings();
        }
    }
});

// Initialize radio button state on page load
$(document).ready(function() {
    console.log('admin-sorting.js loaded');
    console.log('searchBookings function available:', typeof window.searchBookings);
    console.log('resetFilters function available:', typeof window.resetFilters);
    console.log('loadTab function available:', typeof window.loadTab);
    
    // Ensure functions are available globally
    if (typeof window.searchBookings === 'undefined') {
        console.error('searchBookings function not found, creating fallback');
        window.searchBookings = function() {
            console.log('Fallback searchBookings called');
            const keyword = document.getElementById('searchKeyword')?.value || '';
            const status = document.getElementById('statusFilter')?.value || '';
            let bookingType = localStorage.getItem('bookingTypeFilter') || 'all';
            console.log('Calling loadTab with params:', { keyword, statusFilter: status, bookingTypeFilter: bookingType });
            if (typeof window.loadTab === 'function') {
                window.loadTab('BookingMg', { keyword, statusFilter: status, bookingTypeFilter: bookingType });
            }
        };
    }
    
    if (typeof window.resetFilters === 'undefined') {
        console.error('resetFilters function not found, creating fallback');
        window.resetFilters = function() {
            console.log('Fallback resetFilters called');
            localStorage.removeItem('bookingSort');
            localStorage.removeItem('bookingTypeFilter');
            if (typeof window.loadTab === 'function') {
                window.loadTab('BookingMg', { bookingTypeFilter: 'all' });
            }
        };
    }
    
    restoreRadioButtonState();
});

// Add event handler for radio button changes
$(document).on('change', 'input[name="bookingTypeFilter"]', function() {
    console.log('Radio button changed:', $(this).val());
    const selectedValue = $(this).val();
    // Save the selected value before reloading
    localStorage.setItem('bookingTypeFilter', selectedValue);
    searchBookings();
});

// Add event handler for search input changes
$(document).on('input', '#searchKeyword', function() {
    console.log('Search input changed:', $(this).val());
    searchBookings();
});

// Add event handler for status filter changes
$(document).on('change', '#statusFilter', function() {
    console.log('Status filter changed:', $(this).val());
    searchBookings();
});

// Function to restore radio button state
function restoreRadioButtonState() {
    const savedFilter = localStorage.getItem('bookingTypeFilter');
    if (savedFilter) {
        // Uncheck all radio buttons first
        $('input[name="bookingTypeFilter"]').prop('checked', false);
        // Check the saved one
        $(`input[name="bookingTypeFilter"][value="${savedFilter}"]`).prop('checked', true);
    } else {
        // If no saved filter, default to "all"
        $('input[name="bookingTypeFilter"]').prop('checked', false);
        $('input[name="bookingTypeFilter"][value="all"]').prop('checked', true);
    }
}

// Function to reset all filters
window.resetFilters = function() {
    // Clear localStorage
    localStorage.removeItem('bookingSort');
    localStorage.removeItem('bookingTypeFilter');

    // Reset current sort state
    currentSort = { by: '', dir: 'asc', param: '' };

    // Load tab with default "all" filter
    if (typeof window.loadTab === 'function') {
        window.loadTab('BookingMg', { bookingTypeFilter: 'all' });
    } else {
        console.error('loadTab function not found');
    }
}

// Food table sorting (from FoodMg.cshtml)
(function() {
    let currentSortFood = { by: '', dir: 'asc', param: '' };

    $(document).on('click', '#food-table .sortable', function() {
        const sortBy = $(this).data('sort');
        if (currentSortFood.by === sortBy) {
            currentSortFood.dir = currentSortFood.dir === 'asc' ? 'desc' : 'asc';
        } else {
            currentSortFood.by = sortBy;
            currentSortFood.dir = 'asc';
        }
        let sortParam = '';
        if (sortBy === 'name') sortParam = currentSortFood.dir === 'asc' ? 'name_az' : 'name_za';
        if (sortBy === 'category') sortParam = currentSortFood.dir === 'asc' ? 'category_az' : 'category_za';
        if (sortBy === 'price') sortParam = currentSortFood.dir === 'asc' ? 'price_asc' : 'price_desc';
        if (sortBy === 'created') sortParam = currentSortFood.dir === 'asc' ? 'created_asc' : 'created_desc';
        currentSortFood.param = sortParam;
        searchFood();
        updateSortIconsFood();
    });

    window.searchFood = function() {
        const keyword = document.getElementById('searchKeyword').value;
        const category = document.getElementById('categoryFilter').value;
        const status = document.getElementById('statusFilter').value;
        window.loadTab('FoodMg', { keyword, categoryFilter: category, statusFilter: status, sortBy: currentSortFood.param });
    }

    function updateSortIconsFood() {
        $('#sortIconName, #sortIconCategory, #sortIconPrice, #sortIconCreated').html('');
        if (currentSortFood.by === 'name') {
            $('#sortIconName').html(currentSortFood.dir === 'asc' ? '▲' : '▼');
        }
        if (currentSortFood.by === 'category') {
            $('#sortIconCategory').html(currentSortFood.dir === 'asc' ? '▲' : '▼');
        }
        if (currentSortFood.by === 'price') {
            $('#sortIconPrice').html(currentSortFood.dir === 'asc' ? '▲' : '▼');
        }
        if (currentSortFood.by === 'created') {
            $('#sortIconCreated').html(currentSortFood.dir === 'asc' ? '▲' : '▼');
        }
    }
})();

// Voucher table sorting (from VoucherMg.cshtml)
(function() {
    let currentSortVoucher = { by: '', dir: 'asc', param: '' };

    $(document).on('click', '#voucher-table .sortable', function() {
        const sortBy = $(this).data('sort');
        if (currentSortVoucher.by === sortBy) {
            currentSortVoucher.dir = currentSortVoucher.dir === 'asc' ? 'desc' : 'asc';
        } else {
            currentSortVoucher.by = sortBy;
            currentSortVoucher.dir = 'asc';
        }
        let sortParam = '';
        if (sortBy === 'voucherid') sortParam = currentSortVoucher.dir === 'asc' ? 'voucherid_asc' : 'voucherid_desc';
        if (sortBy === 'account') sortParam = currentSortVoucher.dir === 'asc' ? 'account_az' : 'account_za';
        if (sortBy === 'value') sortParam = currentSortVoucher.dir === 'asc' ? 'value_asc' : 'value_desc';
        if (sortBy === 'created') sortParam = currentSortVoucher.dir === 'asc' ? 'created_asc' : 'created_desc';
        if (sortBy === 'expiry') sortParam = currentSortVoucher.dir === 'asc' ? 'expiry_asc' : 'expiry_desc';
        currentSortVoucher.param = sortParam;
        searchVouchers();
        updateSortIconsVoucher();
    });

    window.searchVouchers = function() {
        const keyword = document.getElementById('searchKeyword').value;
        const status = document.getElementById('statusFilter').value;
        const expiry = document.getElementById('expiryFilter').value;
        window.loadTab('VoucherMg', { keyword, statusFilter: status, expiryFilter: expiry, sortBy: currentSortVoucher.param });
    }

    function updateSortIconsVoucher() {
        $('#sortIconVoucherId, #sortIconAccount, #sortIconValue, #sortIconCreated, #sortIconExpiry').html('');
        if (currentSortVoucher.by === 'voucherid') {
            $('#sortIconVoucherId').html(currentSortVoucher.dir === 'asc' ? '▲' : '▼');
        }
        if (currentSortVoucher.by === 'account') {
            $('#sortIconAccount').html(currentSortVoucher.dir === 'asc' ? '▲' : '▼');
        }
        if (currentSortVoucher.by === 'value') {
            $('#sortIconValue').html(currentSortVoucher.dir === 'asc' ? '▲' : '▼');
        }
        if (currentSortVoucher.by === 'created') {
            $('#sortIconCreated').html(currentSortVoucher.dir === 'asc' ? '▲' : '▼');
        }
        if (currentSortVoucher.by === 'expiry') {
            $('#sortIconExpiry').html(currentSortVoucher.dir === 'asc' ? '▲' : '▼');
        }
    }
})(); 