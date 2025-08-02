// Booking Widget JavaScript
(function() {
    'use strict';

    // Global variables for booking widget
    let currentMovieShows = [];
    let movieShowsCache = new Map();

    // Initialize booking widget
    function initBookingWidget() {
        console.log('Initializing booking widget...');
        
        // Set up event listeners
        setupEventListeners();
    }

    // Set up event listeners
    function setupEventListeners() {
        const movieSelect = document.getElementById('movieSelect');
        const dateSelect = document.getElementById('widgetDateSelect');
        const versionSelect = document.getElementById('widgetVersionSelect');
        const timeSelect = document.getElementById('widgetTimeSelect');
        const bookNowBtn = document.getElementById('bookNowBtn');

        if (movieSelect) {
            movieSelect.addEventListener('change', updateBookingDates);
        }

        if (dateSelect) {
            dateSelect.addEventListener('change', updateBookingVersions);
        }

        if (versionSelect) {
            versionSelect.addEventListener('change', updateBookingTimes);
        }

        if (timeSelect) {
            timeSelect.addEventListener('change', updateWidgetButtonState);
        }

        if (bookNowBtn) {
            bookNowBtn.addEventListener('click', continueToSeatSelection);
        }
    }

    // Update dates when movie is selected
    window.updateBookingDates = function() {
        const movieSelect = document.getElementById('movieSelect');
        const dateSelect = document.getElementById('widgetDateSelect');
        const versionSelect = document.getElementById('widgetVersionSelect');
        const timeSelect = document.getElementById('widgetTimeSelect');
        const bookNowBtn = document.getElementById('bookNowBtn');

        if (!movieSelect || !dateSelect) return;

        const movieId = movieSelect.value;
        
        // Reset dependent dropdowns
        dateSelect.innerHTML = '<option value="">2. Select Date</option>';
        dateSelect.disabled = true;
        versionSelect.innerHTML = '<option value="">3. Select Version</option>';
        versionSelect.disabled = true;
        timeSelect.innerHTML = '<option value="">4. Select Time</option>';
        timeSelect.disabled = true;
        bookNowBtn.disabled = true;

        if (!movieId) return;

        // Show loading state
        dateSelect.innerHTML = '<option value="">Loading dates...</option>';
        dateSelect.disabled = true;

        // Check cache first
        if (movieShowsCache.has(movieId)) {
            currentMovieShows = movieShowsCache.get(movieId);
            populateDateDropdown();
            return;
        }

        // Fetch movie shows
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
            populateDateDropdown();
        })
        .catch(error => {
            console.error('Error loading movie shows:', error);
            dateSelect.innerHTML = '<option value="">Error loading dates</option>';
            dateSelect.disabled = true;
        });
    };

    // Populate date dropdown
    function populateDateDropdown() {
        const dateSelect = document.getElementById('widgetDateSelect');
        if (!dateSelect) return;

        dateSelect.innerHTML = '<option value="">2. Select Date</option>';
        dateSelect.disabled = true;

        if (!currentMovieShows || currentMovieShows.length === 0) return;

        const today = new Date();
        today.setHours(0, 0, 0, 0);

        const uniqueDates = [...new Set(currentMovieShows.map(show => show.showDate))];

        uniqueDates.forEach(dateStr => {
            const showDate = new Date(dateStr);
            showDate.setHours(0, 0, 0, 0);

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
    }

    // Update versions when date is selected
    window.updateWidgetVersions = function() {
        const dateSelect = document.getElementById('widgetDateSelect');
        const versionSelect = document.getElementById('widgetVersionSelect');
        const timeSelect = document.getElementById('widgetTimeSelect');
        const bookNowBtn = document.getElementById('bookNowBtn');

        if (!dateSelect || !versionSelect || !timeSelect) return;

        const date = dateSelect.value;
        
        // Reset dependent dropdowns
        versionSelect.innerHTML = '<option value="">3. Select Version</option>';
        versionSelect.disabled = true;
        timeSelect.innerHTML = '<option value="">4. Select Time</option>';
        timeSelect.disabled = true;
        bookNowBtn.disabled = true;

        if (!date) return;

        const filtered = currentMovieShows.filter(show => show.showDate === date);
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

        if (versionSelect.options.length > 1) {
            versionSelect.disabled = false;
        }
    };

    // Populate time dropdown
    function populateTimeDropdown(selectedDate, selectedVersionId) {
        const timeSelect = document.getElementById('widgetTimeSelect');
        const bookNowBtn = document.getElementById('bookNowBtn');

        if (!timeSelect) return;

        timeSelect.innerHTML = '<option value="">4. Select Time</option>';
        timeSelect.disabled = true;

        if (!selectedDate) return;

        // Filter shows for the selected date and version
        const filtered = currentMovieShows.filter(show => 
            show.showDate === selectedDate && String(show.versionId) === String(selectedVersionId)
        );
        
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

        if (timeSelect.options.length > 1) {
            timeSelect.disabled = false;
        }
    }

    // Update times when version is selected
    window.updateWidgetTimes = function() {
        const dateSelect = document.getElementById('widgetDateSelect');
        const versionSelect = document.getElementById('widgetVersionSelect');
        const timeSelect = document.getElementById('widgetTimeSelect');
        const bookNowBtn = document.getElementById('bookNowBtn');

        if (!dateSelect || !versionSelect || !timeSelect || !bookNowBtn) return;

        const date = dateSelect.value;
        const versionId = versionSelect.value;

        // Reset time dropdown
        timeSelect.innerHTML = '<option value="">4. Select Time</option>';
        timeSelect.disabled = true;
        bookNowBtn.disabled = true;

        if (!date || !versionId) return;

        // Populate times based on selected date and version
        populateTimeDropdown(date, versionId);
    };

    // Update button state when time is selected
    window.updateWidgetButtonState = function() {
        const timeSelect = document.getElementById('widgetTimeSelect');
        const bookNowBtn = document.getElementById('bookNowBtn');

        if (!timeSelect || !bookNowBtn) return;

        if (timeSelect.value) {
            bookNowBtn.disabled = false;
        } else {
            bookNowBtn.disabled = true;
        }
    };

    // Continue to seat selection
    window.continueToSeatSelection = function() {
        const movieSelect = document.getElementById('movieSelect');
        const dateSelect = document.getElementById('widgetDateSelect');
        const versionSelect = document.getElementById('widgetVersionSelect');
        const timeSelect = document.getElementById('widgetTimeSelect');

        if (!movieSelect || !dateSelect || !versionSelect || !timeSelect) return;

        const movieId = movieSelect.value;
        const date = dateSelect.value;
        const versionId = versionSelect.value;
        const time = timeSelect.value;

        if (!movieId || !date || !versionId || !time) {
            showError('Please select all required information');
            return;
        }

        const [year, month, day] = date.split('-');
        const formattedDate = `${day}/${month}/${year}`;

        // Show loading state
        const bookNowBtn = document.getElementById('bookNowBtn');
        if (bookNowBtn) {
            const originalText = bookNowBtn.textContent;
            bookNowBtn.textContent = 'Redirecting...';
            bookNowBtn.disabled = true;
        }

        // Redirect to seat selection with versionId
        window.location.href = `/Seat/Select?movieId=${movieId}&date=${formattedDate}&versionId=${encodeURIComponent(versionId)}&time=${encodeURIComponent(time)}`;
    };

    // Show error message
    function showError(message) {
        // Create temporary error message
        const errorDiv = document.createElement('div');
        errorDiv.className = 'booking-widget-error-message';
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

        const bookingWidget = document.querySelector('.booking-widget');
        if (bookingWidget) {
            bookingWidget.appendChild(errorDiv);
            
            // Remove error message after 3 seconds
            setTimeout(() => {
                if (errorDiv.parentNode) {
                    errorDiv.remove();
                }
            }, 3000);
        }
    }

    // Initialize when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initBookingWidget);
    } else {
        initBookingWidget();
    }

})(); 