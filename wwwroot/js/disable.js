$(document).ready(function() {
    var cinemaRoomId = $('#cinemaRoomId').val();
    var detailedShows = [];
    var conflictedShows = [];

    // Load detailed movie shows
    $.get('/Cinema/GetDetailedMovieShowsByCinemaRoom', {cinemaRoomId: cinemaRoomId }, function(data) {
        detailedShows = data;
        renderAllMovieShows();
        checkForConflicts();
    }).fail(function() {
        $('#movieShowsByDate').html('<div class="text-danger">Error loading movie shows.</div>');
    });

    // Load basic movie shows (existing functionality)
    $.get('/Cinema/GetMovieShowsByCinemaRoomGrouped', {cinemaRoomId: cinemaRoomId }, function(data) {
        var container = $('#movieShowsByDate');
        if (!data || data.length === 0) {
            container.html('<div class="text-muted">No shows found for this room.</div>');
            return;
        }
        var html = '';
        data.forEach(function(group) {
            html += '<div class="mb-2">';
            html += '<strong>' + group.date + '</strong><br />';
            group.times.forEach(function(time) {
                html += '<span class="badge bg-primary me-1">' + time + '</span>';
            });
            html += '</div>';
        });
        container.html(html);
    }).fail(function () {
        $('#movieShowsByDate').html('<div class="text-danger">Error loading movie shows.</div>');
    });

    function renderAllMovieShows() {
        var container = $('#movieShowsByDate');
        if (!detailedShows || detailedShows.length === 0) {
            container.html('<div class="text-muted">No shows found for this room.</div>');
            return;
        }

        // Group by date
        var groupedByDate = {};
        detailedShows.forEach(function (show) {
            if (!groupedByDate[show.showDate]) {
                groupedByDate[show.showDate] = [];
            }
            groupedByDate[show.showDate].push(show);
        });

        var html = '';
        // Sort dates properly by converting dd/MM/yyyy to Date objects
        var sortedDates = Object.keys(groupedByDate).sort(function (a, b) {
            var dateA = new Date(a.split('/').reverse().join('-'));
            var dateB = new Date(b.split('/').reverse().join('-'));
            return dateA - dateB;
        });

        sortedDates.forEach(function (date) {
            html += '<div class="mb-3 p-2 border rounded">';
            html += '<strong class="text-primary">' + date + '</strong><br/>';
            groupedByDate[date].forEach(function (show) {
                var endTime = '';
                if (show.startTime && show.duration) {
                    var startTime = new Date('2000-01-01T' + show.scheduleTime);
                    var endTimeDate = new Date(startTime.getTime() + show.duration * 60000);
                    endTime = ' ~ ' + endTimeDate.getHours().toString().padStart(2, '0') + ':' +
                        endTimeDate.getMinutes().toString().padStart(2, '0');
                }
                html += '<div class="mt-1">';
                html += '<span class="badge bg-info me-1">' + show.scheduleTime + endTime + '</span>';
                html += '<small class="text-muted" style="color: white !important">' + show.movieName + '</small>';
                html += '</div>';
            });
            html += '</div>';
        });
        container.html(html);
    }

    function checkForConflicts() {
        var startDateStr = $('#unavailableStartDate').val();
        var endDateStr = $('#unavailableEndDate').val();

        if (!startDateStr || !endDateStr) {
            $('#conflictedShows').html('<div class="text-muted">Select start and end dates to see conflicts.</div>');
            return;
        }

        var startDate = new Date(startDateStr);
        var endDate = new Date(endDateStr);

        if (endDate <= startDate) {
            $('#conflictedShows').html('<div class="text-danger">End date must be after start date.</div>');
            return;
        }

        // Find conflicting shows
        var conflicts = [];
        detailedShows.forEach(function (show) {
            // Parse the show date (format: dd/MM/yyyy)
            var dateParts = show.showDate.split('/');
            var showDate = new Date(dateParts[2], dateParts[1] - 1, dateParts[0]); // year, month-1, day
            showDate.setHours(0, 0, 0, 0);

            // Check if the show date falls within the unavailable period
            var unavailableStartDate = new Date(startDate);
            unavailableStartDate.setHours(0, 0, 0, 0);
            var unavailableEndDate = new Date(endDate);
            unavailableEndDate.setHours(0, 0, 0, 0);

            if (showDate >= unavailableStartDate && showDate <= unavailableEndDate) {
                // Show date is within unavailable period, check time conflicts
                if (show.scheduleTime && show.scheduleTime !== 'N/A') {
                    var timeParts = show.scheduleTime.split(':');
                    var showStartTime = new Date(showDate);
                    showStartTime.setHours(parseInt(timeParts[0]), parseInt(timeParts[1]), 0, 0);

                    var showEndTime = new Date(showStartTime.getTime() + show.duration * 60000);

                    // Check if show overlaps with unavailable period
                    if (showStartTime < endDate && showEndTime > startDate) {
                        conflicts.push(show);
                    }
                }
            }
        });

        renderConflicts(conflicts);
    }

    function renderConflicts(conflicts) {
        var container = $('#conflictedShows');

        if (conflicts.length === 0) {
            container.html('<div class="text-success">No conflicts found for the selected period. You can disable this room.</div>');
            return;
        }

        // Group conflicts by date
        var groupedConflicts = {};
        conflicts.forEach(function (show) {
            if (!groupedConflicts[show.showDate]) {
                groupedConflicts[show.showDate] = [];
            }
            groupedConflicts[show.showDate].push(show);
        });

        var html = '';
        // Sort dates properly by converting dd/MM/yyyy to Date objects
        var sortedDates = Object.keys(groupedConflicts).sort(function (a, b) {
            var dateA = new Date(a.split('/').reverse().join('-'));
            var dateB = new Date(b.split('/').reverse().join('-'));
            return dateA - dateB;
        });

        sortedDates.forEach(function (date) {
            html += '<div class="mb-3 p-2 border rounded">';
            html += '<strong class="text-danger">' + date + '</strong><br/>';
            groupedConflicts[date].forEach(function (show) {
                var endTime = '';
                if (show.startTime && show.duration) {
                    var startTime = new Date('2000-01-01T' + show.scheduleTime);
                    var endTimeDate = new Date(startTime.getTime() + show.duration * 60000);
                    endTime = ' ~ ' + endTimeDate.getHours().toString().padStart(2, '0') + ':' +
                        endTimeDate.getMinutes().toString().padStart(2, '0');
                }
                html += '<div class="mt-1">';
                html += '<span class="badge bg-danger me-1">' + show.scheduleTime + endTime + '</span>';
                html += '<small class="text-muted" style="color: white !important">' + show.movieName + '</small>';
                if (show.bookingCount > 0) {
                    html += ' <button type="button" class="btn btn-sm btn-warning ms-2 view-books-btn" data-movieshowid="' + show.movieShowId + '" data-accountid="' + show.accountId + '">View Books</button>';
                } else {
                    html += ' <span class="badge bg-secondary ms-2" data-movieshowid="' + show.movieShowId + '">No Book</span>';
                    html += ' <button type="button" class="btn btn-outline-danger btn-sm ms-2 remove-show-btn" data-movieshowid="' + show.movieShowId + '">Remove</button>';
                }
                html += '</div>';
            });
            html += '</div>';
        });

        container.html(html);
    }

    // Client-side date validation for better UX
    $('#unavailableStartDate, #unavailableEndDate').on('change', function () {
        var startDateStr = $('#unavailableStartDate').val();
        var endDateStr = $('#unavailableEndDate').val();

        if (startDateStr && endDateStr) {
            var startDate = new Date(startDateStr);
            var endDate = new Date(endDateStr);

            if (endDate <= startDate) {
                alert('End Date must be after Start Date.');
                $('#unavailableEndDate').val(''); // Clear end date if invalid
            } else {
                // Check for conflicts when both dates are valid
                checkForConflicts();
            }
        }
    });

    $(document).on('click', '.view-books-btn', function (e) {
        e.preventDefault(); // Prevent form submission
        var movieShowId = $(this).data('movieshowid');
        var accountId = $(this).data('accountid'); // Get accountId from data attribute
        $.get('/Cinema/GetInvoicesByMovieShow', { movieShowId: movieShowId }, function (data) {
            var html = '<ul class="list-group">';
            if (data.length === 0) {
                html += '<li class="list-group-item">No bookings found.</li>';
            } else {
                data.forEach(function (inv) {
                    var statusClass = inv.status === 'Cancelled' ? 'text-danger' : 'text-success';
                    var statusIcon = inv.status === 'Cancelled' ? '<i class="fas fa-times-circle"></i>' : '<i class="fas fa-check-circle"></i>';
                    html += '<li class="list-group-item">';
                    html += 'Ticket ID: ' + inv.invoiceId + ' | Account: ' + (inv.accountName || inv.accountId) + ' | Seats: ' + inv.seat;
                    html += ' <span class="' + statusClass + '">' + statusIcon + ' ' + inv.status + '</span>';
                    if (inv.status === 'Cancelled' && inv.totalMoney > 0) {
                        html += ' <small class="text-muted">(Refund voucher created)</small>';
                    }
                    html += '</li>';
                });
            }
            html += '</ul>';
            $('#booksModalBody').html(html);
            $('#booksModal').modal('show');
        });
    });

    // Remove show from conflictedShows UI
    $(document).on('click', '.remove-show-btn', function (e) {
        e.preventDefault();
        var movieShowId = $(this).data('movieshowid');
        // Remove from detailedShows array
        detailedShows = detailedShows.filter(function (show) {
            return show.movieShowId != movieShowId;
        });
        // Track removed show IDs
        var removedIds = $('#removedShowIds').val();
        var removedArr = removedIds ? removedIds.split(',').filter(Boolean) : [];
        if (!removedArr.includes(String(movieShowId))) {
            removedArr.push(String(movieShowId));
        }
        $('#removedShowIds').val(removedArr.join(','));
        // Re-check for conflicts and re-render
        checkForConflicts();
    });

    function getDisableForm() {
        return $('#disableRoomForm');
    }
    var confirmInProgress = false;

    // Only show modal when the button is clicked
    $('#disableRoomBtn').on('click', function (e) {
        e.preventDefault();
        var noBookShows = [];
        $('#conflictedShows .badge.bg-secondary').each(function () {
            var parent = $(this).closest('.mt-1');
            var showText = parent.text().trim();
            noBookShows.push(showText);
        });
        if (noBookShows.length > 0) {
            // Populate the modal list
            var listHtml = '';
            noBookShows.forEach(function (show) {
                listHtml += '<li>' + show + '</li>';
            });
            $('#noBookShowList').html(listHtml);
            $('#noBookDisableModal').modal('show');
            return;
        }
        // Check for bookings
        var movieShowInvoices = [];
        $('#conflictedShows .view-books-btn').each(function () {
            var id = $(this).data('movieshowid');
            var accountId = $(this).data('accountid');
            if (id && accountId) movieShowInvoices.push({ movieShowId: id, accountId: accountId });
        });
        if (movieShowInvoices.length === 0) {
            confirmInProgress = true;
            var form = getDisableForm();
            form.off('submit');
            if (form.length > 0 && form[0]) {
                form[0].submit();
            } else {
                alert('Form not found. Please reload the page and try again.');
            }
            return;
        }
        $('#confirmDisableModal').modal('show');
    });

    // Prevent form submit by Enter key or other means
    $(document).on('submit', 'form[action="/Cinema/Disable"]', function (e) {
        if (!confirmInProgress) {
            e.preventDefault();
            return false;
        }
        confirmInProgress = false; // reset for next time
        return true;
    });

    $('#confirmDisableBtn').on('click', function () {
        confirmInProgress = true;
        $('#confirmDisableModal').modal('hide');
        var form = getDisableForm();
        form.off('submit'); // Detach handler
        if (form.length > 0 && form[0]) {
            form[0].submit();   // Native submit
        } else {
            alert('Form not found. Please reload the page and try again.');
        }
    });

    $('#confirmNoBookDisableBtn').on('click', function () {
        confirmInProgress = true;
        $('#noBookDisableModal').modal('hide');
        var form = getDisableForm();
        form.off('submit');
        if (form.length > 0 && form[0]) {
            form[0].submit();
        } else {
            alert('Form not found. Please reload the page and try again.');
        }
    });

    // Countdown timer for auto-enable
    function updateCountdown() {
        var endDateStr = $('#unavailableEndDate').val();
        if (!endDateStr) return;
        
        var endDate = new Date(endDateStr);
        var now = new Date();
        var timeLeft = endDate - now;

        if (timeLeft <= 0) {
            $('#countdownTimer').text('Room will be enabled automatically soon...');
            $('#autoEnableCountdown').removeClass('alert-info').addClass('alert-warning');
            return;
        }

        var hours = Math.floor(timeLeft / (1000 * 60 * 60));
        var minutes = Math.floor((timeLeft % (1000 * 60 * 60)) / (1000 * 60));
        var seconds = Math.floor((timeLeft % (1000 * 60)) / 1000);

        var countdownText = '';
        if (hours > 0) countdownText += hours + 'h ';
        if (minutes > 0) countdownText += minutes + 'm ';
        countdownText += seconds + 's';

        $('#countdownTimer').text(countdownText);
    }

    // Update countdown every second
    if ($('#countdownTimer').length > 0) {
        updateCountdown();
        setInterval(updateCountdown, 1000);
    }

    // SignalR connection for room auto-enable notifications
    var cinemaConnection = new signalR.HubConnectionBuilder()
        .withUrl("/cinemahub")
        .build();

    cinemaConnection.on("RoomAutoEnabled", function (cinemaRoomId, roomName) {
        if (cinemaRoomId == $('#cinemaRoomId').val()) {
            // Show success message and redirect
            alert(`Room "${roomName}" has been automatically enabled!`);
            window.location.href = '/Admin/MainPage?tab=ShowroomMg';
        }
    });

    cinemaConnection.start().catch(function (err) {
        console.error("CinemaHub connection failed: " + err.toString());
    });
});