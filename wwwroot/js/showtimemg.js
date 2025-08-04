
function pad(n) { return n < 10 ? '0' + n : n; }

// Month names for display
const monthNames = [
    'January', 'February', 'March', 'April', 'May', 'June',
    'July', 'August', 'September', 'October', 'November', 'December'
];

// Get data from global variables set by the view
let selected = window.selectedDate || new Date().toISOString().split('T')[0];
let selectedDate = selected ? new Date(selected) : new Date();
let currentYear = selectedDate.getFullYear();
let currentMonth = selectedDate.getMonth();

// Object to store fetched movie data
var movieShowSummary = window.movieShowSummary || {};
// Keep track of which months have been fetched
let fetchedMonths = window.fetchedMonths || {};

function updateTimeGrid() {
    const timeMarkers = document.querySelectorAll('.time-marker');
    timeMarkers.forEach(marker => {
        const hour = parseInt(marker.getAttribute('data-hour'));
        const timelineEvents = marker.querySelector('.timeline-events');
        
        // Clear existing content
        timelineEvents.innerHTML = '';
    });

    // Fetch movie shows for the selected date
    const selectedDateStr = document.getElementById('calendarInput').value;
    if (selectedDateStr) {
        fetch(`/Admin/GetMovieShowsByDate?date=${selectedDateStr}`)
            .then(response => response.json())
            .then(movieShows => {
                displayMoviesOnTimeline(movieShows);
            })
            .catch(error => {
                console.error('Error fetching movie shows:', error);
            });
    }
}

function displayMoviesOnTimeline(movieShows) {
    const timeMarkers = document.querySelectorAll('.time-marker');
    
    // Clear all timeline events first
    timeMarkers.forEach(marker => {
        const timelineEvents = marker.querySelector('.timeline-events');
        timelineEvents.innerHTML = '';
    });

    // Create a single flexible column for each time marker
    timeMarkers.forEach(marker => {
        const timelineEvents = marker.querySelector('.timeline-events');
        
        // Create a single flexible column
        const flexColumn = document.createElement('div');
        flexColumn.className = 'flex-column';
        flexColumn.style.cssText = 'flex: 1; position: relative; height: 100%;';
        
        timelineEvents.appendChild(flexColumn);
    });

    // Display movie shows with better positioning
    movieShows.forEach((movieShow, index) => {
        const startHour = movieShow.startHour;
        const endHour = movieShow.endHour;
        
        // Parse start time to get minutes for precise positioning
        const startTimeParts = movieShow.startTime.split(':');
        const startMinutes = parseInt(startTimeParts[1]);
        
        // Create main movie element
        const movieElement = createTimelineMovieElement(movieShow);
        
        // Find the starting time marker
        const startMarker = document.querySelector(`[data-hour="${startHour}"]`);
        if (startMarker) {
            const flexColumn = startMarker.querySelector('.flex-column');
            if (flexColumn) {
                // Set the movie element to span multiple hours
                const totalHours = endHour - startHour + 1;
                movieElement.style.height = `${totalHours * 80}px`; // 80px per hour
                movieElement.style.position = 'absolute';
                movieElement.style.top = `${(startMinutes / 60) * 80}px`; // Position based on minutes within the hour
                movieElement.style.left = `${index * 15}%`; // Position blocks side by side
                movieElement.style.width = '10%';
                movieElement.style.zIndex = '10';
                
                flexColumn.style.position = 'relative';
                flexColumn.appendChild(movieElement);
            }
        }
    });
    
    // Update summary statistics
    updateSummaryStats(movieShows);
}

function createTimelineMovieElement(movieShow) {
    const movieDiv = document.createElement('div');
    movieDiv.className = 'timeline-movie';
    
    movieDiv.innerHTML = `
        <div class="timeline-movie-title">${movieShow.movieName}</div>
        <div class="timeline-movie-time">${movieShow.startTime} - ${movieShow.endTime}</div>
        <div class="timeline-movie-screen">${movieShow.cinemaRoom}</div>
        <div class="timeline-movie-version">${movieShow.version}</div>
    `;
    
    // Add click event for potential future functionality
    movieDiv.addEventListener('click', () => {
        console.log('Timeline movie clicked:', movieShow);
        // You can add functionality here like editing, deleting, etc.
    });
    
    return movieDiv;
}

// Initialize the page
document.addEventListener('DOMContentLoaded', function() {
    // Initialize Flatpickr - calendar only
    const calendarContainer = document.getElementById('calendarContainer');
    if (calendarContainer) {
        flatpickr(calendarContainer, {
            dateFormat: "d/m/Y",
            defaultDate: selectedDate,
            inline: true, // Make calendar always visible
            showMonths: 1,
            enableTime: false,
            disableMobile: true,
            onChange: function(selectedDates, dateStr, instance) {
                // Update hidden input and selected date display
                document.getElementById('calendarInput').value = dateStr;
                document.getElementById('selectedDateDisplay').textContent = dateStr;
                // Update the timeline
                updateTimeGrid();
            },
            onReady: function(selectedDates, dateStr, instance) {
                // Calendar is ready
                console.log('Flatpickr calendar initialized');
            }
        });
    }

    // Initialize timeline
    updateTimeGrid();
});

function updateRightSection() {
    const rightSection = document.querySelector('.col-lg-6:last-child .showtime-list');
    if (rightSection) {
        rightSection.innerHTML = `
            <div class="alert alert-info text-center">
                <i class="fa fa-info-circle"></i>
                Select a date from the calendar to view showtimes
            </div>
        `;
    }
}

// Quick action functions
function addNewShowtime() {
    // Load available movies and show modal
    loadAvailableMovies();
    
    // Initialize version selection with disabled dropdown
    const versionContainer = document.getElementById('versionSelectionContainer');
    if (versionContainer) {
        versionContainer.innerHTML = `
            <select class="form-select" id="versionSelect" disabled>
                <option value="">-- Select a Version --</option>
            </select>
        `;
    }
    
    const modal = new bootstrap.Modal(document.getElementById('quickAddShowtimeModal'));
    modal.show();
    
    // Initialize event listeners after modal is shown
    modal._element.addEventListener('shown.bs.modal', function() {
        setupEventListeners();
    });
}

function loadAvailableMovies() {
    fetch('/Movie/GetAvailableMovies')
        .then(response => response.json())
        .then(movies => {
            const container = document.getElementById('movieSelectionContainer');
            container.innerHTML = `
                <select class="form-select" id="movieSelect">
                    <option value="">-- Select a Movie --</option>
                </select>
            `;
            
            const movieSelect = document.getElementById('movieSelect');
            movies.forEach(movie => {
                const option = document.createElement('option');
                option.value = movie.movieId;
                option.textContent = `${movie.movieNameEnglish} (${movie.duration} min)`;
                option.setAttribute('data-duration', movie.duration);
                movieSelect.appendChild(option);
            });
            
            // Add change event listener
            movieSelect.addEventListener('change', function() {
                const selectedOption = this.options[this.selectedIndex];
                if (this.value) {
                    const duration = selectedOption.getAttribute('data-duration');
                    selectMovie(this.value, duration);
                } else {
                    // Reset version selection
                    document.getElementById('versionSelectionContainer').innerHTML = `
                        <select class="form-select" id="versionSelect" disabled>
                            <option value="">-- Select a Version --</option>
                        </select>
                    `;
                }
            });
        })
        .catch(error => {
            console.error('Error loading movies:', error);
        });
}

function selectMovie(movieId, duration) {
    // Set the movie data for movie-show.js to use
    document.getElementById('movieId').value = movieId;
    document.getElementById('movieDuration').value = duration;
    
    // Set up global variables that movie-show.js expects
    window.movieShowItems = [];
    window.toDate = null; // This will be set by the movie data
    window.availableShowDates = [];
    
    // Load versions for this movie
    loadVersionsForMovie(movieId);
    
    // Enable version selection
    document.getElementById('versionSelectionContainer').style.display = 'block';
}

function loadVersionsForMovie(movieId) {
    fetch(`/Movie/GetVersionsByMovie?movieId=${movieId}`)
        .then(response => response.json())
        .then(versions => {
            const container = document.getElementById('versionSelectionContainer');
            container.innerHTML = `
                <select class="form-select" id="versionSelect">
                    <option value="">-- Select a Version --</option>
                </select>
            `;
            
            const versionSelect = document.getElementById('versionSelect');
            versions.forEach(version => {
                const option = document.createElement('option');
                option.value = version.versionId;
                option.textContent = version.versionName;
                versionSelect.appendChild(option);
            });
            
            // Add change event listener to trigger cinema room loading
            versionSelect.addEventListener('change', async function() {
                if (this.value) {
                    // Reset and disable all dependent selects first
                    const cinemaRoomSelect = document.getElementById('cinemaRoomSelect');
                    const showDateSelect = document.getElementById('showDateSelect');
                    const scheduleSelect = document.getElementById('scheduleSelect');
                    const addMovieShowBtn = document.getElementById('addMovieShowBtn');
                    
                    if (cinemaRoomSelect) {
                        cinemaRoomSelect.innerHTML = '<option value="">-- Select a Cinema Room --</option>';
                        cinemaRoomSelect.disabled = true;
                    }
                    
                    if (showDateSelect) {
                        showDateSelect.innerHTML = '<option value="">-- Select a Show Date --</option>';
                        showDateSelect.disabled = true;
                    }
                    
                    if (scheduleSelect) {
                        scheduleSelect.innerHTML = '<option value="">-- Select a Schedule --</option>';
                        scheduleSelect.disabled = true;
                    }
                    
                    if (addMovieShowBtn) {
                        addMovieShowBtn.disabled = true;
                    }
                    
                    // Fetch rooms for this version
                    try {
                        const response = await fetch(`/Cinema/GetRoomsByVersion?versionId=${this.value}`);
                        if (response.ok) {
                            const rooms = await response.json();
                            
                            // Populate cinema rooms
                            cinemaRoomSelect.innerHTML = '<option value="">-- Select a Cinema Room --</option>';
                            rooms.forEach(room => {
                                const option = document.createElement('option');
                                option.value = room.cinemaRoomId;
                                option.textContent = room.cinemaRoomName;
                                cinemaRoomSelect.appendChild(option);
                            });
                            cinemaRoomSelect.disabled = false;
                        } else {
                            cinemaRoomSelect.innerHTML = '<option value="">-- No rooms available --</option>';
                        }
                    } catch (error) {
                        console.error('Error fetching rooms:', error);
                        cinemaRoomSelect.innerHTML = '<option value="">-- Error loading rooms --</option>';
                    }
                } else {
                    // If no version selected, disable cinema room
                    const cinemaRoomSelect = document.getElementById('cinemaRoomSelect');
                    if (cinemaRoomSelect) {
                        cinemaRoomSelect.innerHTML = '<option value="">-- Select a Cinema Room --</option>';
                        cinemaRoomSelect.disabled = true;
                    }
                }
            });
        })
        .catch(error => {
            console.error('Error loading versions:', error);
        });
}

// Add event listeners for the complete flow
function setupEventListeners() {
    const cinemaRoomSelect = document.getElementById('cinemaRoomSelect');
    const showDateSelect = document.getElementById('showDateSelect');
    const scheduleSelect = document.getElementById('scheduleSelect');
    const addMovieShowBtn = document.getElementById('addMovieShowBtn');
    
    // Cinema Room selection enables Show Date - reuse movie-show.js logic
    if (cinemaRoomSelect) {
        cinemaRoomSelect.addEventListener('change', function() {
            if (showDateSelect) {
                showDateSelect.innerHTML = '<option value="">-- Select a Show Date --</option>';
                showDateSelect.disabled = true;
            }
            if (scheduleSelect) {
                scheduleSelect.innerHTML = '<option value="">-- Select a Schedule --</option>';
                scheduleSelect.disabled = true;
            }
            if (addMovieShowBtn) {
                addMovieShowBtn.disabled = true;
            }
            
            const roomId = cinemaRoomSelect.value;
            if (!roomId) return;
            
            // Get movie data to set up proper date range
            const movieId = document.getElementById('movieId').value;
            fetch(`/Movie/GetAvailableMovies`)
                .then(response => response.json())
                .then(movies => {
                    const selectedMovie = movies.find(m => m.movieId === movieId);
                    if (selectedMovie && selectedMovie.toDate) {
                        // Set up global variables for movie-show.js
                        window.toDate = selectedMovie.toDate;
                        
                        // Generate available show dates from movie's fromDate to toDate
                        window.availableShowDates = [];
                        const fromDate = new Date(selectedMovie.fromDate);
                        const toDate = new Date(selectedMovie.toDate);
                        
                        for (let d = new Date(fromDate); d <= toDate; d.setDate(d.getDate() + 1)) {
                            window.availableShowDates.push({
                                value: d.toISOString().split('T')[0],
                                text: d.toLocaleDateString('en-GB')
                            });
                        }
                        
                        // Now use movie-show.js logic to populate dates
                        const today = new Date();
                        today.setHours(0, 0, 0, 0);
                        
                        const filteredDates = window.availableShowDates.filter(d => {
                            const dateObj = new Date(d.value);
                            dateObj.setHours(0, 0, 0, 0);
                            return dateObj >= today && (!window.toDate || dateObj <= new Date(window.toDate));
                        });
                        
                        showDateSelect.innerHTML = '<option value="">-- Select a Show Date --</option>';
                        filteredDates.forEach(date => {
                            const option = document.createElement('option');
                            option.value = date.value;
                            option.textContent = date.text;
                            showDateSelect.appendChild(option);
                        });
                        showDateSelect.disabled = false;
                    }
                })
                .catch(error => {
                    console.error('Error fetching movie data:', error);
                });
        });
    }
    
    // Show Date selection enables Schedule - reuse movie-show.js logic
    if (showDateSelect) {
        showDateSelect.addEventListener('change', async function() {
            if (scheduleSelect) {
                scheduleSelect.innerHTML = '<option value="">-- Select a Schedule --</option>';
                scheduleSelect.disabled = true;
            }
            if (addMovieShowBtn) {
                addMovieShowBtn.disabled = true;
            }
            
            const roomId = cinemaRoomSelect?.value;
            const selectedDate = showDateSelect.value;
            const movieDuration = parseInt(document.getElementById('movieDuration').value, 10);
            const cleaningTime = 15;
            
            if (!roomId || !selectedDate) return;
            
            // Fetch available schedules using movie-show.js logic
            try {
                const response = await fetch(`/Movie/GetAvailableScheduleTimes?cinemaRoomId=${roomId}&showDate=${selectedDate}&movieDurationMinutes=${movieDuration}&cleaningTimeMinutes=${cleaningTime}`);
                const result = await response.json();
                const availableSchedules = result.schedules;
                
                scheduleSelect.innerHTML = '<option value="">-- Select a Schedule --</option>';
                availableSchedules.forEach(schedule => {
                    const option = document.createElement('option');
                    option.value = schedule.scheduleId;
                    option.textContent = schedule.scheduleTime;
                    scheduleSelect.appendChild(option);
                });
                scheduleSelect.disabled = false;
            } catch (error) {
                console.error('Error fetching schedules:', error);
                scheduleSelect.innerHTML = '<option value="">-- Error loading schedules --</option>';
            }
        });
    }
    
    // Schedule selection enables Add button
    if (scheduleSelect) {
        scheduleSelect.addEventListener('change', function() {
            if (addMovieShowBtn) {
                addMovieShowBtn.disabled = !this.value;
            }
        });
    }
    
    // Add button click handler
    if (addMovieShowBtn) {
        addMovieShowBtn.addEventListener('click', async function() {
            const movieId = document.getElementById('movieId').value;
            const versionId = document.getElementById('versionSelect').value;
            const roomId = cinemaRoomSelect?.value;
            const showDate = showDateSelect?.value;
            const scheduleId = scheduleSelect?.value;
            
            if (!movieId || !versionId || !roomId || !showDate || !scheduleId) {
                alert('Please fill in all fields');
                return;
            }
            
            try {
                const response = await fetch('/Movie/AddMovieShow', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({
                        movieId: movieId,
                        showDate: showDate,
                        scheduleId: scheduleId,
                        cinemaRoomId: roomId,
                        versionId: versionId
                    })
                });
                
                if (response.ok) {
                    alert('Movie show added successfully!');
                    
                    // Reset the modal
                    resetModal();
                    
                    // Close modal and refresh timeline
                    const modal = bootstrap.Modal.getInstance(document.getElementById('quickAddShowtimeModal'));
                    modal.hide();
                    updateTimeGrid();
                } else {
                    alert('Failed to add movie show. Please try again.');
                }
            } catch (error) {
                console.error('Error adding movie show:', error);
                alert('An error occurred while adding the movie show.');
            }
        });
    }
}

// Function to reset the modal to initial state
function resetModal() {
    // Reset movie selection
    const movieSelect = document.getElementById('movieSelect');
    if (movieSelect) {
        movieSelect.value = '';
    }
    
    // Reset version selection
    const versionContainer = document.getElementById('versionSelectionContainer');
    if (versionContainer) {
        versionContainer.innerHTML = `
            <select class="form-select" id="versionSelect" disabled>
                <option value="">-- Select a Version --</option>
            </select>
        `;
    }
    
    // Reset cinema room selection
    const cinemaRoomSelect = document.getElementById('cinemaRoomSelect');
    if (cinemaRoomSelect) {
        cinemaRoomSelect.innerHTML = '<option value="">-- Select a Cinema Room --</option>';
        cinemaRoomSelect.disabled = true;
    }
    
    // Reset show date selection
    const showDateSelect = document.getElementById('showDateSelect');
    if (showDateSelect) {
        showDateSelect.innerHTML = '<option value="">-- Select a Show Date --</option>';
        showDateSelect.disabled = true;
    }
    
    // Reset schedule selection
    const scheduleSelect = document.getElementById('scheduleSelect');
    if (scheduleSelect) {
        scheduleSelect.innerHTML = '<option value="">-- Select a Schedule --</option>';
        scheduleSelect.disabled = true;
    }
    
    // Reset add button
    const addMovieShowBtn = document.getElementById('addMovieShowBtn');
    if (addMovieShowBtn) {
        addMovieShowBtn.disabled = true;
    }
    
    // Reset hidden inputs
    document.getElementById('movieId').value = '';
    document.getElementById('movieDuration').value = '';
    
    // Clear last show end time info
    const lastShowEndTimeEl = document.getElementById('lastShowEndTime');
    if (lastShowEndTimeEl) {
        lastShowEndTimeEl.textContent = '';
    }
}

function updateSummaryStats(movieShows) {
    const totalShows = movieShows.length;
    const uniqueScreens = [...new Set(movieShows.map(ms => ms.cinemaRoom))].length;
    
    // Calculate total duration
    let totalDuration = 0;
    movieShows.forEach(show => {
        const startParts = show.startTime.split(':');
        const endParts = show.endTime.split(':');
        const startMinutes = parseInt(startParts[0]) * 60 + parseInt(startParts[1]);
        const endMinutes = parseInt(endParts[0]) * 60 + parseInt(endParts[1]);
        totalDuration += endMinutes - startMinutes;
    });
    
    const totalHours = Math.floor(totalDuration / 60);
    const remainingMinutes = totalDuration % 60;
    
    // Update the summary display
    document.getElementById('totalShows').textContent = totalShows;
    document.getElementById('screensUsed').textContent = uniqueScreens;
    document.getElementById('totalDuration').textContent = `${totalHours}h ${remainingMinutes}m`;
}