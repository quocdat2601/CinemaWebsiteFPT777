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
        new Chart(revCtx, {
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
        new Chart(bookCtx, {
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
    const category = document.getElementById('categoryFilter').value;
    const status = document.getElementById('statusFilter').value;
    loadTab('FoodMg', keyword, category, status);
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
});

// Make functions globally available
window.searchEmployees = searchEmployees;
window.searchBooking = searchBooking;
window.searchFood = searchFood;
window.showToast = showToast;
window.updateSeatTypePrices = updateSeatTypePrices;
