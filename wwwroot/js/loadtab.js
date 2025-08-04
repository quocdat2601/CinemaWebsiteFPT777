// Global variables
let dashboardConnection = null;
let cinemaConnection = null;

// Main tab loading function
window.loadTab = function(tabName, params = {}) {
    $('.admin-tab').removeClass('active');
    $(`.admin-tab[data-tab="${tabName}"]`).addClass('active');

    const urlParams = new URLSearchParams({ tab: tabName });
    for (const key in params) {
        if (params[key]) urlParams.append(key, params[key]);
    }
    
    $('#tabContent').load(`/Admin/LoadTab?${urlParams.toString()}`, function(response, status) {
        if (status === 'error') {
            $('#tabContent').html('<div class="alert alert-danger">Failed to load content</div>');
        } else {
            // Trigger tabContentLoaded event for all tabs
            $(document).trigger('tabContentLoaded', [tabName]);
            
            // Trigger appropriate event based on the loaded tab
            switch(tabName) {
                case 'BookingMg':
                    $(document).trigger('bookingTabLoaded');
                    break;
                case 'VoucherMg':
                    $(document).trigger('voucherTabLoaded');
                    break;
                case 'Dashboard':
                    initializeDashboard();
                    break;
            }
        }
    });
}

// Dashboard initialization
window.initDashboardCharts = function(model) {
    const revCtx = document.getElementById('revenueChart')?.getContext('2d');
    if (revCtx && model.revenueDates) {
        window.revenueChart = new Chart(revCtx, {
            type: 'line',
            data: {
                labels: model.revenueDates,
                datasets: [{
                    label: 'Revenue',
                    data: model.revenueValues,
                    fill: true,
                    tension: 0.4
                }]
            }
        });
    }
    
    const bookCtx = document.getElementById('bookingChart')?.getContext('2d');
    if (bookCtx && model.bookingDates) {
        window.bookingChart = new Chart(bookCtx, {
            type: 'line',
            data: {
                labels: model.bookingDates,
                datasets: [{
                    label: 'Bookings',
                    data: model.bookingValues,
                    fill: true,
                    tension: 0.4
                }]
            }
        });
    }
};

// Dashboard initialization function
function initializeDashboard() {
    // Stop existing dashboard connection
    if (window.dashboardConnection) {
        window.dashboardConnection.stop();
    }
    
    // Initialize SignalR for dashboard
    window.dashboardConnection = new signalR.HubConnectionBuilder()
        .withUrl("/dashboardhub")
        .build();

    window.dashboardConnection.on("DashboardUpdated", function () {
        loadTab('Dashboard'); // reload tab dashboard
    });

    window.dashboardConnection.start().catch(function (err) {
        return console.error(err.toString());
    });

    const el = document.getElementById('dashboard-data');
    if (el) {
        const model = JSON.parse(el.textContent);
        window.initDashboardCharts(model);
        showTab('movie');
    }
}

// Search functions
function searchEmployees() {
    const keyword = document.getElementById('searchKeyword')?.value ?? '';
    loadTab('EmployeeMg', keyword);
}

function searchBooking() {
    const keyword = document.getElementById('searchKeyword')?.value ?? '';
    loadTab('BookingMg', keyword);
}

function searchFood() {
    const keyword = document.getElementById('searchKeyword').value;
    const categoryFilter = document.getElementById('categoryFilter').value;
    const statusFilter = document.querySelector('input[name="statusFilter"]:checked')?.value || 'true';
    
    const params = {};
    if (keyword) params.keyword = keyword;
    if (categoryFilter) params.categoryFilter = categoryFilter;
    // Always include statusFilter, default to 'true' (Active)
    params.statusFilter = statusFilter;
    
    // Check if there's an active sort
    if (window.currentSortFood && window.currentSortFood.param) {
        params.sortBy = window.currentSortFood.param;
    }
    
    loadTab('FoodMg', params);
}

// Initialize SignalR for cinema room notifications
function initializeCinemaConnection() {
    if (window.cinemaConnection) {
        window.cinemaConnection.stop();
    }
    
    window.cinemaConnection = new signalR.HubConnectionBuilder()
        .withUrl("/cinemahub")
        .build();

    // Handle room auto-enabled notifications
    window.cinemaConnection.on("RoomAutoEnabled", function (cinemaRoomId, roomName) {
        // Show notification
        showToast(`Room "${roomName}" has been automatically enabled!`, 'success');
        
        // If currently on ShowroomMg tab, refresh it
        if ($('.admin-tab.active').data('tab') === 'ShowroomMg') {
            loadTab('ShowroomMg');
        }
    });

    // Handle admin notifications
    window.cinemaConnection.on("AdminNotification", function (message, roomName) {
        showToast(message, 'info');
    });

    window.cinemaConnection.start().catch(function (err) {
        console.error("CinemaHub connection failed: " + err.toString());
    });
}

// Helper function to show toast notifications
function showToast(message, type = 'info') {
    const toastHtml = `
        <div class="toast align-items-center text-white bg-${type === 'success' ? 'success' : type === 'error' ? 'danger' : 'info'} border-0" role="alert" aria-live="assertive" aria-atomic="true">
            <div class="d-flex">
                <div class="toast-body">
                    ${message}
                </div>
                <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>
            </div>
        </div>
    `;
    
    const toastContainer = $('#toast-container');
    if (toastContainer.length === 0) {
        $('body').append('<div id="toast-container" class="toast-container position-fixed top-0 end-0 p-3"></div>');
    }
    
    $('#toast-container').append(toastHtml);
    const toastElement = $('#toast-container .toast').last();
    const toast = new bootstrap.Toast(toastElement);
    toast.show();
    
    // Auto-remove after 5 seconds
    setTimeout(() => {
        toastElement.remove();
    }, 5000);
}

// Version management functions
function handleEditVersion() {
    var id = $(this).data('id');
    $.get('/Version/Get/' + id, function (version) {
        // Set the form fields with the fetched values
        $('#inlineEditVersionId').val(version.versionId);
        $('#inlineEditVersionName').val(version.versionName);
        $('#inlineEditMulti').val(version.multi);
        // Calculate and update prices immediately
        updateSeatTypePrices(version.multi);
        // Show the edit area
        $('#inlineEditArea').slideDown();
        // Scroll to the edit area
        $('html, body').animate({ scrollTop: $('#inlineEditArea').offset().top - 80 }, 400);
    });
}

function handleCancelInlineEdit() {
    $('#inlineEditArea').slideUp();
}

function handleInlineEditSubmit(e) {
    e.preventDefault();
    var data = {
        VersionId: $('#inlineEditVersionId').val(),
        VersionName: $('#inlineEditVersionName').val(),
        Multi: parseFloat($('#inlineEditMulti').val())
    };
    $.ajax({
        url: '/Version/Edit',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(data),
        success: function (res) {
            if (res.success) {
                loadTab('VersionMg');
            } else {
                alert(res.error || 'Failed to update version.');
            }
        }
    });
}

function handleMultiplierInput() {
    var multi = parseFloat($(this).val()) || 1;
    updateSeatTypePrices(multi);
}

function updateSeatTypePrices(multi) {
    $('#seatTypePriceTable tr').each(function () {
        var base = parseFloat($(this).attr('data-base-price')) || 0;
        var price = Math.round(base * multi);
        $(this).find('.calculated-price').text(price.toLocaleString('en-US'));
    });
}

// Initialize the page
$(document).ready(function () {
    // Get initial tab from ViewData or default to Dashboard
    var tabToLoad = window.initialTab || 'Dashboard';
    loadTab(tabToLoad);

    // Add click handlers for admin tabs
    $('.admin-tab').on('click', function(e) {
        e.preventDefault();
        const tabName = $(this).data('tab');
        loadTab(tabName);
    });

    // Initialize SignalR connections
    initializeCinemaConnection();

    // Delegated events for version management
    $(document).on('click', '.edit-version-btn', handleEditVersion);
    $(document).on('click', '#cancelInlineEdit', handleCancelInlineEdit);
    $(document).on('submit', '#inlineEditVersionForm', handleInlineEditSubmit);
    $(document).on('input', '#inlineEditMulti', handleMultiplierInput);

    // Event handlers for tab content loaded
    $(document).on('bookingTabLoaded', function() {
        console.log('BookingMg tab loaded, initializing pagination...');
        initializeBookingPagination();
    });

    $(document).on('voucherTabLoaded', function() {
        console.log('VoucherMg tab loaded, initializing pagination...');
        initializeVoucherPagination();
    });
});

// Initialize BookingMg pagination
function initializeBookingPagination() {
    // Check if pagination-common.js is loaded
    if (typeof createPaginationManager === 'undefined') {
        console.error('createPaginationManager function not found! Loading pagination-common.js...');
        // Dynamically load the script
        const script = document.createElement('script');
        script.src = '/js/pagination-common.js';
        script.onload = function() {
            initializeBookingPagination();
        };
        script.onerror = function() {
            console.error('Failed to load pagination-common.js');
            $('#bookingResult').html('<div class="alert alert-danger"><i class="fas fa-exclamation-triangle"></i> Failed to load pagination script.</div>');
        };
        document.head.appendChild(script);
        return;
    }

    // Booking-specific functions
    function renderBookingTable(bookingData) {
        if (bookingData.length > 0) {
            let html = '<div class="table-responsive"><table id="booking-table" class="table table-hover">';
            html += '<thead class="table-light"><tr>';
            html += '<th class="text-center sortable" data-sort="id">Booking ID <span id="sortIconId"></span></th>';
            html += '<th class="text-start sortable" data-sort="movie">Movie Name <span id="sortIconMovie"></span></th>';
            html += '<th class="text-center sortable" data-sort="account">Account ID <span id="sortIconAccount"></span></th>';
            html += '<th class="text-end sortable" data-sort="identity">Identity Card <span id="sortIconIdentity"></span></th>';
            html += '<th class="text-end sortable" data-sort="phone">Phone Number <span id="sortIconPhone"></span></th>';
            html += '<th class="text-end sortable" data-sort="time">Schedule Time <span id="sortIconTime"></span></th>';
            html += '<th class="text-center">Status</th>';
            html += '<th class="text-center">Actions</th>';
            html += '</tr></thead><tbody>';

            bookingData.forEach(function(booking) {
                let statusBadge = '';
                if (booking.status === 'Completed' && !booking.cancel) {
                    statusBadge = '<span class="badge bg-success"><i class="bi bi-check-circle me-1"></i>Paid</span>';
                } else if (booking.status === 'Completed' && booking.cancel) {
                    statusBadge = '<span class="badge bg-danger"><i class="bi bi-x-circle me-1"></i>Cancelled</span>';
                } else if (booking.status === null || booking.status === 'null') {
                    statusBadge = '<span class="badge bg-danger"><i class="bi bi-x-circle me-1"></i>Cancelled</span>';
                } else if (booking.status === 'Incomplete') {
                    statusBadge = '<span class="badge bg-secondary"><i class="bi bi-hourglass-split me-1"></i>Unpaid</span>';
                } else {
                    statusBadge = '<span class="badge bg-secondary"><i class="bi bi-hourglass-split me-1"></i>Unpaid</span>';
                }

                html += '<tr class="text-center booking-row">';
                html += '<td class="text-center">' + booking.invoiceId + '</td>';
                html += '<td class="text-start">' + booking.movieName + '</td>';
                html += '<td class="text-center">' + booking.accountId + '</td>';
                html += '<td class="text-end">' + booking.identityCard + '</td>';
                html += '<td class="text-end">' + booking.phoneNumber + '</td>';
                html += '<td class="text-end">' + booking.scheduleTime + '</td>';
                html += '<td class="text-center">' + statusBadge + '</td>';
                html += '<td class="text-center">';
                html += '<a href="/Booking/TicketInfo?invoiceId=' + booking.invoiceId + '" class="btn btn-sm btn-info">';
                html += '<i class="fas fa-info-circle"></i> Details';
                html += '</a>';
                html += '</td>';
                html += '</tr>';
            });

            html += '</tbody></table></div>';
            $('#bookingResult').html(html);
            
            // Restore sort icons after rendering table
            if (typeof window.updateSortIcons === 'function') {
                setTimeout(function() {
                    window.updateSortIcons();
                }, 100);
            }
        } else {
            $('#bookingResult').html('<div class="text-center py-4"><i class="bi bi-emoji-frown display-1 text-muted"></i><h4 class="text-muted mt-3">No bookings found</h4><p class="text-muted">Try adjusting your search criteria or add a new booking.</p></div>');
        }
    }

    function updateBookingStatistics(statistics) {
        $('#totalBookingsCount').text(statistics.totalBookings);
        $('#paidCount').text(statistics.paid);
        $('#cancelledCount').text(statistics.cancelled);
        $('#unpaidCount').text(statistics.unpaid);
    }

    // Initialize booking pagination
    window.bookingPagination = createPaginationManager({
        containerId: 'bookingSearchForm',
        resultId: 'bookingResult',
        paginationId: 'bookingPagination',
        pageSize: 10,
        loadFunction: {
            url: '/Admin/BookingMgPartial'
        },
        renderFunction: renderBookingTable,
        updateStatsFunction: updateBookingStatistics
    });

    // Handle reset filters
    window.resetFilters = function() {
        $('#searchKeyword').val('');
        $('#statusFilter').val('');
        $('input[name="bookingTypeFilter"][value="all"]').prop('checked', true);
        window.bookingPagination.loadData({}, 1);
    };

    // Handle search bookings
    window.searchBookingsFromForm = function() {
        window.bookingPagination.loadData(window.bookingPagination.getCurrentParams(), 1);
    };
}

// Initialize VoucherMg pagination
function initializeVoucherPagination() {
    // Check if pagination-common.js is loaded
    if (typeof createPaginationManager === 'undefined') {
        console.error('createPaginationManager function not found! Loading pagination-common.js...');
        // Dynamically load the script
        const script = document.createElement('script');
        script.src = '/js/pagination-common.js';
        script.onload = function() {
            initializeVoucherPagination();
        };
        script.onerror = function() {
            console.error('Failed to load pagination-common.js');
            $('#voucherResult').html('<div class="alert alert-danger"><i class="fas fa-exclamation-triangle"></i> Failed to load pagination script.</div>');
        };
        document.head.appendChild(script);
        return;
    }

    // Voucher-specific functions
    function renderVoucherTable(voucherData) {
        if (voucherData.length > 0) {
            let html = '<div class="table-responsive"><table id="voucher-table" class="table table-hover">';
            html += '<thead class="table-light"><tr>';
            html += '<th>Image</th>';
            html += '<th class="text-center sortable" data-sort="voucherid">Voucher ID <span id="sortIconVoucherId"></span></th>';
            html += '<th>Code</th>';
            html += '<th class="text-center sortable" data-sort="account">Account ID <span id="sortIconVoucherAccount"></span></th>';
            html += '<th class="text-end sortable" data-sort="value">Value <span id="sortIconValue"></span></th>';
            html += '<th class="text-center sortable" data-sort="created">Created Date <span id="sortIconVoucherCreated"></span></th>';
            html += '<th class="text-center sortable" data-sort="expiry">Expiry Date <span id="sortIconExpiry"></span></th>';
            html += '<th class="text-center">Status</th>';
            html += '<th class="text-center">Actions</th>';
            html += '</tr></thead><tbody>';

            voucherData.forEach(function(voucher) {
                let statusBadge = '';
                if (voucher.isUsed) {
                    statusBadge = '<span class="badge bg-secondary"><i class="bi bi-check2-all me-1"></i>Used</span>';
                } else if (voucher.isExpired) {
                    statusBadge = '<span class="badge bg-danger"><i class="bi bi-x-circle me-1"></i>Expired</span>';
                } else {
                    statusBadge = '<span class="badge bg-success"><i class="bi bi-check-circle me-1"></i>Active</span>';
                }

                let imageCell = '';
                if (voucher.image) {
                    imageCell = `<img src="${voucher.image}" alt="Voucher" class="img-thumbnail voucher-image" style="width: 50px; height: 50px; object-fit: cover; cursor: pointer;" data-bs-toggle="modal" data-bs-target="#imageModal" data-image="${voucher.image}" data-code="${voucher.code}">`;
                } else {
                    imageCell = '<div class="bg-secondary text-white d-flex align-items-center justify-content-center" style="width: 50px; height: 50px; font-size: 12px;"><i class="bi bi-ticket-perforated"></i></div>';
                }

                let expiryDateCell = '';
                if (voucher.isExpiringSoon) {
                    expiryDateCell = `<span class="text-warning fw-bold">${voucher.expiryDate} <i class="bi bi-exclamation-triangle ms-1" title="Expires in ${voucher.daysUntilExpiry} day${voucher.daysUntilExpiry === 1 ? '' : 's'}"></i></span>`;
                } else if (voucher.isExpired) {
                    expiryDateCell = `<span class="text-danger fw-bold">${voucher.expiryDate} <i class="bi bi-x-circle ms-1"></i></span>`;
                } else {
                    expiryDateCell = `<span class="text-success">${voucher.expiryDate}</span>`;
                }

                let accountCell = voucher.accountId ? `<span class="text-muted">${voucher.accountId}</span>` : '<span class="text-muted fst-italic">Unassigned</span>';

                html += '<tr class="text-center voucher-row">';
                html += '<td>' + imageCell + '</td>';
                html += '<td class="text-center"><strong class="text-primary">' + voucher.voucherId + '</strong></td>';
                html += '<td><span class="badge bg-light text-dark font-monospace">' + voucher.code + '</span></td>';
                html += '<td class="text-center">' + accountCell + '</td>';
                html += '<td class="text-end"><strong class="text-success">' + voucher.value.toLocaleString() + ' VND</strong></td>';
                html += '<td class="text-center">' + voucher.createdDate + '</td>';
                html += '<td class="text-center">' + expiryDateCell + '</td>';
                html += '<td class="text-center">' + statusBadge + '</td>';
                html += '<td class="text-center">';
                html += '<div class="btn-group" role="group">';
                html += '<form action="/Voucher/AdminEdit" method="get" style="display:inline;">';
                html += '<input type="hidden" name="id" value="' + voucher.voucherId + '" />';
                html += '<button type="submit" class="btn btn-sm btn-outline-primary" ' + (voucher.isUsed || voucher.isExpired ? 'disabled' : '') + ' title="' + (voucher.isUsed || voucher.isExpired ? 'Cannot edit used or expired vouchers' : 'Edit voucher') + '">';
                html += '<i class="bi bi-pencil"></i>';
                html += '</button>';
                html += '</form>';
                html += '<button type="button" class="btn btn-sm btn-outline-info" onclick="viewVoucherDetails(\'' + voucher.voucherId + '\')" title="View details">';
                html += '<i class="bi bi-eye"></i>';
                html += '</button>';
                html += '<form action="/Voucher/AdminDelete" method="post" onsubmit="return confirm(\'Are you sure you want to delete this voucher? This action cannot be undone.\');" style="display: inline;">';
                html += '<input type="hidden" name="id" value="' + voucher.voucherId + '" />';
                html += '<button type="submit" class="btn btn-sm btn-outline-danger" title="Delete voucher">';
                html += '<i class="bi bi-trash"></i>';
                html += '</button>';
                html += '</form>';
                html += '</div>';
                html += '</td>';
                html += '</tr>';
            });

            html += '</tbody></table></div>';
            $('#voucherResult').html(html);
            
            // Restore sort icons after rendering table
            if (typeof window.updateSortIconsVoucher === 'function') {
                setTimeout(function() {
                    window.updateSortIconsVoucher();
                }, 100);
            }
        } else {
            $('#voucherResult').html('<div class="text-center py-4"><i class="bi bi-emoji-frown display-1 text-muted"></i><h4 class="text-muted mt-3">No vouchers found</h4><p class="text-muted">Try adjusting your search criteria or add a new voucher.</p></div>');
        }
    }

    function updateVoucherStatistics(statistics) {
        $('#totalVouchersCount').text(statistics.totalVouchers);
        $('#activeCount').text(statistics.active);
        $('#usedCount').text(statistics.used);
        $('#expiredCount').text(statistics.expired);
    }

    // Initialize voucher pagination
    const voucherPagination = createPaginationManager({
        containerId: 'voucherSearchForm',
        resultId: 'voucherResult',
        paginationId: 'voucherPagination',
        pageSize: 10,
        loadFunction: {
            url: '/Admin/VoucherMgPartial'
        },
        renderFunction: renderVoucherTable,
        updateStatsFunction: updateVoucherStatistics
    });

    // Handle reset filters
    window.resetVoucherFilters = function() {
        $('#searchKeyword').val('');
        $('#statusFilter').val('');
        $('#expiryFilter').val('');
        voucherPagination.loadData({}, 1);
    };

    // Handle search vouchers
    window.searchVouchers = function() {
        voucherPagination.loadData(voucherPagination.getCurrentParams(), 1);
    };

    // Image modal functionality
    $(document).on('show.bs.modal', '#imageModal', function (event) {
        var button = $(event.relatedTarget);
        var image = button.data('image');
        var code = button.data('code');
        var modal = $(this);
        modal.find('#modalImage').attr('src', image);
        modal.find('#modalImageCode').text('Code: ' + code);
    });

    // Initialize tooltips
    var tooltipTriggerList = [].slice.call(document.querySelectorAll('[title]'));
    var tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });

    // Voucher details function
    window.viewVoucherDetails = function(voucherId) {
        $('#voucherDetailsContent').html(`
            <div class="text-center">
                <i class="bi bi-arrow-clockwise fa-spin fa-2x text-primary mb-3"></i>
                <p>Loading voucher details...</p>
            </div>
        `);
        $('#voucherDetailsModal').modal('show');

        // Load voucher details via AJAX
        $.getJSON('/Voucher/GetVoucherDetails', { id: voucherId }, function(response) {
            if (response.success) {
                var voucher = response.voucher;
                var statusBadge = '';

                if (voucher.status === 'Active') {
                    statusBadge = '<span class="badge bg-success"><i class="bi bi-check-circle me-1"></i>Active</span>';
                } else if (voucher.status === 'Used') {
                    statusBadge = '<span class="badge bg-secondary"><i class="bi bi-check2-all me-1"></i>Used</span>';
                } else {
                    statusBadge = '<span class="badge bg-danger"><i class="bi bi-x-circle me-1"></i>Expired</span>';
                }

                var expiryWarning = '';
                if (voucher.isExpiringSoon) {
                    expiryWarning = `<div class="alert alert-warning">
                        <i class="bi bi-exclamation-triangle me-2"></i>
                        This voucher expires in ${voucher.daysUntilExpiry} day${voucher.daysUntilExpiry === 1 ? '' : 's'}!
                    </div>`;
                }

                var imageSection = '';
                if (voucher.image) {
                    imageSection = `
                        <div class="text-center mb-3">
                            <img src="${voucher.image}" alt="Voucher Image" class="img-fluid rounded" style="max-height: 200px;">
                        </div>
                    `;
                }

                $('#voucherDetailsContent').html(`
                    ${expiryWarning}
                    ${imageSection}
                    <div class="row">
                        <div class="col-md-6">
                            <h6><i class="bi bi-ticket-perforated me-2 text-primary"></i>Voucher Information</h6>
                            <table class="table table-sm">
                                <tr><td><strong>Voucher ID:</strong></td><td>${voucher.id}</td></tr>
                                <tr><td><strong>Code:</strong></td><td><span class="badge bg-light text-dark font-monospace">${voucher.code}</span></td></tr>
                                <tr><td><strong>Value:</strong></td><td><span class="fw-bold text-success">${voucher.value.toLocaleString()} VND</span></td></tr>
                                <tr><td><strong>Status:</strong></td><td>${statusBadge}</td></tr>
                            </table>
                        </div>
                        <div class="col-md-6">
                            <h6><i class="bi bi-calendar me-2 text-primary"></i>Date Information</h6>
                            <table class="table table-sm">
                                <tr><td><strong>Created:</strong></td><td>${voucher.createdDate}</td></tr>
                                <tr><td><strong>Expires:</strong></td><td>${voucher.expiryDate}</td></tr>
                                <tr><td><strong>Account ID:</strong></td><td>${voucher.accountId || '<span class="text-muted fst-italic">Unassigned</span>'}</td></tr>
                            </table>
                        </div>
                    </div>
                `);
            } else {
                $('#voucherDetailsContent').html(`
                    <div class="alert alert-danger">
                        <i class="bi bi-exclamation-triangle me-2"></i>
                        ${response.message}
                    </div>
                `);
            }
        }).fail(function() {
            $('#voucherDetailsContent').html(`
                <div class="alert alert-danger">
                    <i class="bi bi-exclamation-triangle me-2"></i>
                    Failed to load voucher details. Please try again.
                </div>
            `);
        });
    };
}

// Make functions globally available
window.searchEmployees = searchEmployees;
window.searchBooking = searchBooking;
window.searchFood = searchFood;
window.showToast = showToast;
window.updateSeatTypePrices = updateSeatTypePrices;
