// Booking Modal JavaScript - Separate from booking-widget.js to avoid conflicts
(function() {
    'use strict';

    // Global variables for modal
    let currentMovieShows = [];
    let movieShowsCache = new Map();

    // Modal-specific booking functions
    window.modalUpdateBookingVersions = function() {
        const date = document.getElementById('dateSelect').value;
        const versionSelect = document.getElementById('versionSelect');
        const timeSelect = document.getElementById('timeSelect');
        
        // Clear and disable dropdowns
        versionSelect.innerHTML = '<option value="">— Select Version —</option>';
        versionSelect.disabled = true;
        timeSelect.innerHTML = '<option value="">— Select Time —</option>';
        timeSelect.disabled = true;
        
        if (!date) {
            return;
        }
        
        if (!currentMovieShows || currentMovieShows.length === 0) {
            return;
        }
        
        // Filter shows for the selected date
        const filtered = currentMovieShows.filter(show => show.showDate === date);
        
        if (filtered.length === 0) {
            return;
        }
        
        // Use a Map to ensure unique versionId-versionName pairs
        const versionMap = new Map();
        filtered.forEach(show => {
            if (show.versionId && show.versionName && !versionMap.has(show.versionId)) {
                versionMap.set(show.versionId, show.versionName);
            }
        });
        
        // Add version options
        versionMap.forEach((name, id) => {
            const option = document.createElement('option');
            option.value = id;
            option.textContent = name;
            versionSelect.appendChild(option);
        });
        
        // Enable version dropdown
        versionSelect.disabled = false;
        
        // Update step status
        window.updateStepStatus(1, true);
        window.updateStepStatus(2, false);
        window.updateStepStatus(3, false);
        
        // Update booking summary - reset version and time
        window.updateBookingSummary();
        
        // Update button state
        window.modalUpdateBookingButton();
    };

    // Populate time dropdown for modal
    window.modalUpdateBookingTimes = function() {
        const date = document.getElementById('dateSelect').value;
        const versionId = document.getElementById('versionSelect').value;
        const timeSelect = document.getElementById('timeSelect');
        
        timeSelect.innerHTML = '<option value="">— Select Time —</option>';
        timeSelect.disabled = true;
        
        if (!date || !versionId) {
            return;
        }
        
        if (!currentMovieShows || currentMovieShows.length === 0) {
            return;
        }
        
        // Filter shows for the selected date and version
        const filtered = currentMovieShows.filter(show => 
            show.showDate === date && String(show.versionId) === String(versionId)
        );
        
        if (filtered.length === 0) {
            return;
        }
        
        // Use a Set to ensure unique times
        const timeSet = new Set();
        filtered.forEach(show => {
            if (show.scheduleTime && !timeSet.has(show.scheduleTime)) {
                const option = document.createElement('option');
                option.value = show.scheduleTime;
                option.textContent = show.scheduleTime;
                timeSelect.appendChild(option);
                timeSet.add(show.scheduleTime);
            }
        });
        
        timeSelect.disabled = false;
        
        // Update step status
        window.updateStepStatus(2, true);
        window.updateStepStatus(3, false);
        
        // Update booking summary - reset time
        window.updateBookingSummary();
        
        // Update button state
        window.modalUpdateBookingButton();
    };

    // Enable/disable the continue button based on selections
    window.modalUpdateBookingButton = function() {
        const dateSelect = document.getElementById('dateSelect');
        const versionSelect = document.getElementById('versionSelect');
        const timeSelect = document.getElementById('timeSelect');
        const bookBtn = document.getElementById('bookBtn');
        
        if (!bookBtn) return;
        
        const hasDate = dateSelect && dateSelect.value;
        const hasVersion = versionSelect && versionSelect.value;
        const hasTime = timeSelect && timeSelect.value;
        
        console.log('modalUpdateBookingButton - hasDate:', hasDate, 'hasVersion:', hasVersion, 'hasTime:', hasTime);
        
        if (hasDate && hasVersion && hasTime) {
            bookBtn.disabled = false;
            console.log('Button enabled');
        } else {
            bookBtn.disabled = true;
            console.log('Button disabled');
        }
        
        // Update step status
        if (hasTime) {
            window.updateStepStatus(3, true);
        } else {
            window.updateStepStatus(3, false);
        }
        
        // Update booking summary
        window.updateBookingSummary();
    };

    // Modal-specific continue to seat selection
    window.modalContinueToSeatSelection = function() {
        console.log('modalContinueToSeatSelection function called!');

        
        try {
            const modalMovieId = document.getElementById('movieId');
            const modalDateSelect = document.getElementById('dateSelect');
            const modalVersionSelect = document.getElementById('versionSelect');
            const modalTimeSelect = document.getElementById('timeSelect');
            const bookBtn = document.getElementById('bookBtn');

            console.log('Modal elements found:', {
                modalMovieId: !!modalMovieId,
                modalDateSelect: !!modalDateSelect,
                modalVersionSelect: !!modalVersionSelect,
                modalTimeSelect: !!modalTimeSelect,
                bookBtn: !!bookBtn
            });

    
            console.log('Modal actual values:', {
                modalMovieIdValue: modalMovieId?.value,
                modalDateSelectValue: modalDateSelect?.value,
                modalVersionSelectValue: modalVersionSelect?.value,
                modalTimeSelectValue: modalTimeSelect?.value
            });

            if (!modalMovieId || !modalDateSelect || !modalVersionSelect || !modalTimeSelect) {
                console.error('Modal elements not found');
                return;
            }

            const movieId = modalMovieId.value;
            const date = modalDateSelect.value;
            const versionId = modalVersionSelect.value;
            const time = modalTimeSelect.value;

            console.log('Modal values:', {
                movieId: movieId,
                date: date,
                versionId: versionId,
                time: time
            });

            if (!movieId || !date || !versionId || !time) {
                console.error('Missing required modal information');
                showModalError('Please select all required information');
                return;
            }

            const [year, month, day] = date.split('-');
            const formattedDate = `${day}/${month}/${year}`;
            console.log('Formatted date:', formattedDate);

            // Show loading state
            if (bookBtn) {
                const originalText = bookBtn.innerHTML;
                bookBtn.innerHTML = '<span>Redirecting...</span> <i class="fas fa-spinner fa-spin"></i>';
                bookBtn.disabled = true;
                console.log('Modal button loading state applied');
            }

            // Redirect to seat selection with versionId
            const url = `/Seat/Select?movieId=${movieId}&date=${formattedDate}&versionId=${encodeURIComponent(versionId)}&time=${encodeURIComponent(time)}`;
            console.log('Modal redirect URL:', url);
            console.log('URL components:', {
                movieId: movieId,
                formattedDate: formattedDate,
                versionId: versionId,
                encodedVersionId: encodeURIComponent(versionId),
                time: time,
                encodedTime: encodeURIComponent(time)
            });
            console.log('About to set window.location.href...');
            
            // Try multiple redirection methods
            try {
                window.location.href = url;
                console.log('window.location.href set successfully');
            } catch (e) {
                console.error('Error with window.location.href:', e);
                try {
                    window.location.assign(url);
                    console.log('window.location.assign set successfully');
                } catch (e2) {
                    console.error('Error with window.location.assign:', e2);
                    try {
                        window.location.replace(url);
                        console.log('window.location.replace set successfully');
                    } catch (e3) {
                        console.error('Error with window.location.replace:', e3);
                        // Fallback: try to open in new window
                        window.open(url, '_self');
                        console.log('window.open called as fallback');
                    }
                }
            }
        } catch (error) {
            console.error('Error in modalContinueToSeatSelection:', error);
            showModalError('An error occurred while processing your request');
        }
    };

    // Show error message for modal
    function showModalError(message) {
        // Create temporary error message
        const errorDiv = document.createElement('div');
        errorDiv.className = 'modal-error-message';
        errorDiv.textContent = message;
        errorDiv.style.cssText = `
            position: absolute;
            top: 100%;
            left: 0;
            right: 0;
            background: #ff6b6b;
            color: white;
            padding: 8px 12px;
            border-radius: 4px;
            font-size: 12px;
            margin-top: 8px;
            z-index: 1000;
        `;

        const modalBody = document.querySelector('#addBookingModal .modal-body');
        if (modalBody) {
            modalBody.appendChild(errorDiv);
            
            // Remove error message after 3 seconds
            setTimeout(() => {
                if (errorDiv.parentNode) {
                    errorDiv.remove();
                }
            }, 3000);
        }
    }

    // Function to open booking modal for selected movie
    window.openBookingModal = function(movieId, movieName, movieLogo) {
        // Set the movie ID in the hidden input
        const movieIdInput = document.getElementById('movieId');
        if (movieIdInput) {
            movieIdInput.value = movieId;
        }

        // Update the movie name in the summary
        const movieNameElem = document.getElementById('summaryMovieName');
        if (movieNameElem && movieName) {
            movieNameElem.textContent = movieName;
        }

        // Update the movie logo
        const movieLogoElem = document.getElementById('selectedMovieLogo');
        if (movieLogoElem && movieLogo) {
            movieLogoElem.src = movieLogo;
            movieLogoElem.style.display = 'block';
        } else if (movieLogoElem) {
            movieLogoElem.style.display = 'none';
        }

        // Reset form selections
        const dateSelect = document.getElementById('dateSelect');
        const versionSelect = document.getElementById('versionSelect');
        const timeSelect = document.getElementById('timeSelect');

        if (dateSelect) {
            dateSelect.innerHTML = '<option value="">Loading dates...</option>';
            dateSelect.disabled = true;
        }
        if (versionSelect) {
            versionSelect.innerHTML = '<option value="">— Select Version —</option>';
            versionSelect.disabled = true;
        }
        if (timeSelect) {
            timeSelect.innerHTML = '<option value="">— Select Time —</option>';
            timeSelect.disabled = true;
        }

        // Reset summary
        resetBookingSummary();

        // Load movie shows for this specific movie
        window.loadMovieShowsForMovie(movieId, movieName);
    };

    // Function to load movie shows for a specific movie
    window.loadMovieShowsForMovie = function(movieId, movieName) {
        // Check if we already have this movie's shows cached
        if (movieShowsCache.has(movieId)) {
            currentMovieShows = movieShowsCache.get(movieId);
            window.populateDateDropdown();
            window.showBookingModal();
            return;
        }

        // Fetch movie shows for this specific movie
        fetch(`/Movie/GetMovieShows?movieId=${movieId}`, {
            headers: {
                'X-Requested-With': 'XMLHttpRequest'
            }
        })
        .then(response => {
            if (!response.ok) throw new Error('Failed to load movie shows');
            return response.json();
        })
        .then(shows => {
            // Cache the results
            movieShowsCache.set(movieId, shows);
            currentMovieShows = shows;
            window.populateDateDropdown();
            window.showBookingModal();
        })
        .catch(error => {
            console.error('Error loading movie shows:', error);
            // Show error state
            const dateSelect = document.getElementById('dateSelect');
            if (dateSelect) {
                dateSelect.innerHTML = '<option value="">Error loading dates</option>';
                dateSelect.disabled = true;
            }
            
            // Show user-friendly error message
            const modalBody = document.querySelector('#addBookingModal .modal-body');
            if (modalBody) {
                const errorDiv = document.createElement('div');
                errorDiv.className = 'alert alert-danger mt-3';
                errorDiv.innerHTML = '<i class="fas fa-exclamation-triangle"></i> Unable to load showtimes. Please try again later.';
                modalBody.appendChild(errorDiv);
            }
        });
    };

    // Function to show the booking modal
    window.showBookingModal = function() {
        const modalElement = document.getElementById('addBookingModal');
        if (modalElement && typeof bootstrap !== 'undefined') {
            const bookingModal = new bootstrap.Modal(modalElement);
            bookingModal.show();
            
            // Reset modal state
            resetModalState();
            
            // Focus management for accessibility
            modalElement.addEventListener('shown.bs.modal', function() {
                const dateSelect = document.getElementById('dateSelect');
                if (dateSelect) {
                    dateSelect.focus();
                }
            });
        }
    };

    // Function to reset modal state
    window.resetModalState = function() {
        // Reset all steps
        window.updateStepStatus(1, false);
        window.updateStepStatus(2, false);
        window.updateStepStatus(3, false);
        
        // Set first step as active
        window.updateStepStatus(1, true);
    };

    // Function to update step status
    window.updateStepStatus = function(stepNumber, isActive) {
        const stepElement = document.querySelector(`.booking-step[data-step="${stepNumber}"]`);
        
        if (stepElement) {
            stepElement.classList.remove('active', 'completed');
            if (isActive) {
                stepElement.classList.add('active');
            } else if (stepNumber < window.getCurrentStep()) {
                stepElement.classList.add('completed');
            }
        }
    };

    // Function to get current step
    window.getCurrentStep = function() {
        const dateSelect = document.getElementById('dateSelect');
        const versionSelect = document.getElementById('versionSelect');
        const timeSelect = document.getElementById('timeSelect');
        
        if (dateSelect.value && versionSelect.value && timeSelect.value) {
            return 4; // All steps completed
        } else if (dateSelect.value && versionSelect.value) {
            return 3;
        } else if (dateSelect.value) {
            return 2;
        } else {
            return 1;
        }
    };

    // Function to reset booking summary
    window.resetBookingSummary = function() {
        document.getElementById('summaryDate').textContent = '-';
        document.getElementById('summaryVersion').textContent = '-';
        document.getElementById('summaryTime').textContent = '-';
    };

    // Function to update booking summary
    window.updateBookingSummary = function() {
        const dateSelect = document.getElementById('dateSelect');
        const versionSelect = document.getElementById('versionSelect');
        const timeSelect = document.getElementById('timeSelect');

        if (dateSelect.value) {
            const [year, month, day] = dateSelect.value.split('-');
            document.getElementById('summaryDate').textContent = `${day}/${month}/${year}`;
        } else {
            document.getElementById('summaryDate').textContent = '-';
        }

        if (versionSelect.value) {
            const selectedOption = versionSelect.options[versionSelect.selectedIndex];
            document.getElementById('summaryVersion').textContent = selectedOption.textContent;
        } else {
            document.getElementById('summaryVersion').textContent = '-';
        }

        if (timeSelect.value) {
            document.getElementById('summaryTime').textContent = timeSelect.value;
        } else {
            document.getElementById('summaryTime').textContent = '-';
        }
    };

    // Function to populate date dropdown
    window.populateDateDropdown = function() {
        const dateSelect = document.getElementById('dateSelect');
        if (!dateSelect) {
            return;
        }

        dateSelect.innerHTML = '<option value="">— Select Date —</option>';
        dateSelect.disabled = true;

        if (!currentMovieShows || currentMovieShows.length === 0) {
            return;
        }

        const today = new Date();
        today.setHours(0, 0, 0, 0); // normalize time

        // Since the API already filters shows, all returned shows are available
        const uniqueDates = [...new Set(currentMovieShows.map(show => show.showDate))];

        uniqueDates.forEach(dateStr => {
            const showDate = new Date(dateStr);
            showDate.setHours(0, 0, 0, 0); // normalize show date

            if (showDate >= today) {
                const [year, month, day] = dateStr.split('-');
                const displayDate = `${day}/${month}/${year}`;
                const option = document.createElement('option');
                option.value = dateStr;
                option.textContent = displayDate;
                dateSelect.appendChild(option);
            }
        });

        if (dateSelect.options.length > 1) {
            dateSelect.disabled = false;
        }
    };

})(); 