/* Admin Table Sorting Functionality */

// Global sort state for booking management
let currentSort = { by: '', dir: 'asc', param: '' };

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

function searchBookings() {
    const keyword = document.getElementById('searchKeyword').value;
    const status = document.getElementById('statusFilter').value;
    loadTab('BookingMg', { keyword, statusFilter: status, sortBy: currentSort.param });
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
    if (tabName === 'BookingMg') {
        restoreSortState();
        if (currentSort.param) {
            searchBookings();
        }
    }
});

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
        loadTab('FoodMg', { keyword, categoryFilter: category, statusFilter: status, sortBy: currentSortFood.param });
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
        loadTab('VoucherMg', { keyword, statusFilter: status, expiryFilter: expiry, sortBy: currentSortVoucher.param });
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