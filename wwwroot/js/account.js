/* Account Management JavaScript */

// Sidebar toggle functionality
document.addEventListener('DOMContentLoaded', function() {
    const sidebarToggle = document.getElementById('sidebarToggle');
    if (sidebarToggle) {
        sidebarToggle.addEventListener('click', function () {
            const sidebar = document.getElementById('modernSidebar');
            sidebar.classList.toggle('collapsed');

            // Change icon
            const icon = this.querySelector('.material-icons');
            if (sidebar.classList.contains('collapsed')) {
                icon.textContent = 'menu_open';
            } else {
                icon.textContent = 'menu';
            }
        });
    }
});

// Tab loading functionality
$(document).ready(function() {
    $('.sidebar-link').on('click', function(e) {
        e.preventDefault();
        
        // Remove active class from all links
        $('.sidebar-link').removeClass('active');
        
        // Add active class to clicked link
        $(this).addClass('active');
        
        const tabName = $(this).data('tab');
        
        // Show loading indicator
        $('#tabContent').html('<div class="text-center"><div class="spinner-border" role="status"><span class="visually-hidden">Loading...</span></div></div>');
        
        // Load tab content via AJAX
        $.get('/MyAccount/LoadTab', { tab: tabName }, function(data) {
            $('#tabContent').html(data);
        }).fail(function() {
            $('#tabContent').html('<div class="alert alert-danger">Failed to load tab content. Please try again.</div>');
        });
    });
    
    // Load initial tab
    const urlParams = new URLSearchParams(window.location.search);
    const initialTab = urlParams.get('tab') || 'Profile';
    $(`.sidebar-link[data-tab="${initialTab}"]`).trigger('click');
});

// Profile avatar upload functionality
function initProfileAvatarUpload() {
    const imageInput = document.getElementById('profileImageInput');
    const imagePreview = document.getElementById('profileImagePreview');
    const avatarBtns = document.getElementById('avatarBtns');
    const cancelBtn = document.getElementById('cancelAvatarBtn');
    const updateImageBtn = document.getElementById('updateImageBtn');
    const profileForm = document.getElementById('profileForm');
    const originalSrc = imagePreview ? imagePreview.src : '';
    
    if (!imageInput || !imagePreview) return;
    
    imageInput.addEventListener('change', function(event) {
        if (event.target.files && event.target.files[0]) {
            const reader = new FileReader();
            reader.onload = function(e) {
                imagePreview.src = e.target.result;
                if (avatarBtns) avatarBtns.classList.add('is-visible');
            }
            reader.readAsDataURL(event.target.files[0]);
        }
    });
    
    if (cancelBtn && avatarBtns) {
        cancelBtn.addEventListener('click', function() {
            imagePreview.src = originalSrc;
            imageInput.value = '';
            avatarBtns.classList.remove('is-visible');
        });
    }
    
    // Change form action when Update button is clicked
    if (updateImageBtn && profileForm) {
        updateImageBtn.addEventListener('click', function(e) {
            profileForm.action = '/MyAccount/UpdateImage';
        });
    }
}

// Initialize profile avatar upload when DOM is ready
document.addEventListener('DOMContentLoaded', function() {
    initProfileAvatarUpload();
});

// Booking History Cards AJAX
$(function() {
    let historyDataCache = [];
    let currentPage = 1;
    const pageSize = 6;
    
    function renderHistoryCards(historyData) {
        // Filter only completed bookings
        historyDataCache = (historyData || []).filter(function(booking) {
            return booking.status === 1;
        });
        renderHistoryPage(currentPage);
        renderHistoryPagination();
    }
    
    function renderHistoryPage(page) {
        let html = '';
        const start = (page - 1) * pageSize;
        const end = start + pageSize;
        const pageData = historyDataCache.slice(start, end);
        
        if (pageData.length > 0) {
            html += '<div class="booking-card-list">';
            pageData.forEach(function(booking) {
                var movieName = booking.movieShow && booking.movieShow.movie ? booking.movieShow.movie.movieNameEnglish : 'N/A';
                var showDate = booking.movieShow && booking.movieShow.showDate ? new Date(booking.movieShow.showDate).toLocaleDateString('en-GB') : 'N/A';
                var showTime = booking.movieShow && booking.movieShow.schedule && booking.movieShow.schedule.scheduleTime ? booking.movieShow.schedule.scheduleTime.substring(0, 5) : 'N/A';
                var seats = booking.seat || '';
                var seatCount = seats ? seats.split(',').length : 0;
                var statusBadge = (booking.status === 1 && booking.cancel === false) ? '<span class="badge bg-success">Booked</span>' : (booking.status === 1 && booking.cancel === true) ? '<span class="badge bg-danger">Canceled</span>' : '<span class="badge bg-secondary">Not Paid</span>';
                var totalMoney = booking.totalMoney === 0 ? '0 VND' : (booking.totalMoney ? new Intl.NumberFormat('en-US').format(booking.totalMoney) + ' VND' : 'N/A');
                var totalAmountHtml = `<span id="booking-total-${booking.invoiceId}" class="booking-total-amount">${totalMoney}</span>`;
                
                if (booking.status === 1 && booking.invoiceId) {
                    $.ajax({
                        url: '/api/foodinvoice/gettotalfoodprice',
                        type: 'GET',
                        data: { invoiceId: booking.invoiceId },
                        success: function(foodRes) {
                            var foodPrice = (typeof foodRes === 'number') ? foodRes : (foodRes.totalFoodPrice || 0);
                            var seatPrice = booking.totalMoney || 0;
                            var total = seatPrice + (foodPrice || 0);
                            var totalText = new Intl.NumberFormat('en-US').format(total) + ' VND';
                            if (total === 0) totalText = '0 VND';
                            $("#booking-total-" + booking.invoiceId)
                                .text(totalText)
                                .addClass('booking-total-amount-highlight');
                        },
                        error: function() {
                            $("#booking-total-" + booking.invoiceId).text(totalMoney);
                        }
                    });
                }
                
                html += `<div class="booking-card">
                    <div class="booking-card-row booking-card-row-top">
                        <div class="booking-card-title">${movieName}</div>
                        <div class="booking-card-date" style="flex:1.5 1 0%;min-width:90px;max-width:160px;"><i class=\"fas fa-calendar-alt\"></i> ${showDate}</div>
                        <div class="booking-card-time"><i class=\"fas fa-clock\"></i> ${showTime}</div>
                        <div class="booking-card-actions dropdown">
                            <button class="btn btn-light btn-sm rounded-circle" type="button" data-bs-toggle="dropdown" aria-expanded="false"><i class="fas fa-ellipsis-v"></i></button>
                            <ul class="dropdown-menu dropdown-menu-end">
                                <li><a class="dropdown-item" href="/Ticket/Details/${booking.invoiceId}"><i class="fas fa-info-circle me-2"></i>Detail</a></li>
                            </ul>
                        </div>
                    </div>
                    <div class="booking-card-row booking-card-row-bottom">
                        <div class="booking-card-seats"><i class=\"fas fa-chair\"></i> ${seatCount} seat(s): ${seats}</div>
                        <div class="booking-card-status">${statusBadge}</div>
                        <div class="booking-card-total">${totalAmountHtml}</div>
                    </div>
                </div>`;
            });
            html += '</div>';
        } else {
            html = '<div class="text-center py-4"><p class="text-muted">No booking history found.</p></div>';
        }
        
        $('#bookingHistoryCards').html(html);
    }
    
    function renderHistoryPagination() {
        const totalPages = Math.ceil(historyDataCache.length / pageSize);
        let paginationHtml = '';
        
        if (totalPages > 1) {
            paginationHtml = '<nav aria-label="Booking history pagination"><ul class="pagination justify-content-center">';
            
            // Previous button
            paginationHtml += `<li class="page-item ${currentPage === 1 ? 'disabled' : ''}">
                <a class="page-link" href="#" onclick="changeHistoryPage(${currentPage - 1}); return false;">Previous</a>
            </li>`;
            
            // Page numbers
            for (let i = 1; i <= totalPages; i++) {
                paginationHtml += `<li class="page-item ${currentPage === i ? 'active' : ''}">
                    <a class="page-link" href="#" onclick="changeHistoryPage(${i}); return false;">${i}</a>
                </li>`;
            }
            
            // Next button
            paginationHtml += `<li class="page-item ${currentPage === totalPages ? 'disabled' : ''}">
                <a class="page-link" href="#" onclick="changeHistoryPage(${currentPage + 1}); return false;">Next</a>
            </li>`;
            
            paginationHtml += '</ul></nav>';
        }
        
        $('#bookingHistoryPagination').html(paginationHtml);
    }
    
    window.changeHistoryPage = function(page) {
        if (page >= 1 && page <= Math.ceil(historyDataCache.length / pageSize)) {
            currentPage = page;
            renderHistoryPage(currentPage);
            renderHistoryPagination();
        }
    };
    
    // Load booking history data
    $.get('/MyAccount/GetBookingHistory', function(data) {
        renderHistoryCards(data);
    });
}); 