document.addEventListener('DOMContentLoaded', function() {
    // Get data from global variables set by the view
    const movieId = window.movieId;
    const isAuthenticated = window.isAuthenticated;
    const showsByDate = window.showsByDate || {};
    
    // Global variable to store available dates from movie shows
    let availableDates = new Set();

    // Initialize everything when DOM is loaded
    function initializePage() {
        // Initialize book button event listener
        const bookBtn = document.getElementById('bookBtn');
        if (bookBtn) {
            bookBtn.addEventListener('click', function(event) {
                if (!isAuthenticated) {
                    event.preventDefault();
                    window.location.href = '/Account/Login';
                }
            });
        }

        // Add event listeners for person links (actors/directors)
        document.querySelectorAll('.person-link').forEach(function(link) {
            link.addEventListener('click', function(e) {
                e.preventDefault();
                const person = {
                    id: this.dataset.id,
                    image: this.dataset.image || '',
                    name: this.dataset.name || 'Unknown',
                    dateOfBirth: this.dataset.dob || '',
                    nationality: this.dataset.nationality || '',
                    gender: this.dataset.gender === 'True' ? true : this.dataset.gender === 'False' ? false : null,
                    description: this.dataset.description || ''
                };
                showActorModal(person);
            });
        });

        // Load movie shows after DOM is ready
        loadMovieShows();
        
        // Initialize booking modal event listener
        const bookingModal = document.getElementById('addBookingModal');
        if (bookingModal) {
            bookingModal.addEventListener('shown.bs.modal', function () {
                // Populate date picker when modal opens
                populateDatePicker();
                
                // Focus on date select when modal opens
                const dateSelect = document.getElementById('dateSelect');
                if (dateSelect) {
                    dateSelect.focus();
                }
            });
        }
    }

    function showActorModal(actor) {
        const modalBody = document.getElementById('actorModalBody');
        console.log(actor); // Inspect the whole object
        let bodyHtml = `
        <div class="container-fluid text-black" style="font-size: 20px">
                <div class="row g-3 align-items-center" style="margin-top: 0.1rem">
                  <!-- Left Column -->
              <div class="col-md-6 text-center">
                ${actor.image ? `
                  <img src="${actor.image}" alt="${actor.name}" class="rounded-circle shadow-sm" style="width: 200px; height: 200px; object-fit: cover;">
                ` : `
                  <div class="rounded-circle bg-light d-flex align-items-center justify-content-center shadow-sm" style="width: 200px; height: 200px;">
                    <i class="fas fa-user text-muted" style="font-size: 2rem;"></i>
                  </div>
                `}
              </div>

              <!-- Right Column -->
                <div class="col-md-6 d-flex flex-column justify-content-center">
                ${actor.name ? `<p><strong>Name:</strong> ${actor.name}</p>` : ''}
                ${actor.dateOfBirth ? `<p><strong>Born:</strong> ${actor.dateOfBirth}</p>` : ''}
                ${actor.nationality ? `<p><strong>Nationality:</strong> ${actor.nationality}</p>` : ''}
                ${actor.gender !== null ? `<p><strong>Gender:</strong> ${actor.gender ? 'Female' : 'Male'}</p>` : ''}
              </div>
            </div>
            <div class="text-center">
                <a href="/Cast/Detail/${actor.id}" class="btn btn-md btn-primary" style="border-radius: 20px; margin-top: 20px">
                    View Details
                </a>
            </div>
        </div>
    `   ;

        if (bodyHtml === '') bodyHtml = '<p class="text-muted">No information available.</p>';

        modalBody.innerHTML = bodyHtml;

        // Show the modal using Bootstrap
        const modal = new bootstrap.Modal(document.getElementById('actorModal'));
        modal.show();
    }

    async function loadMovieShows() {
        try {
            const response = await fetch(`/Movie/MovieShow/${movieId}`, {
                headers: {
                    'X-Requested-With': 'XMLHttpRequest'
                }
            });

            if (!response.ok) throw new Error('Failed to fetch movie shows');

            const shows = await response.json();
            const container = document.getElementById('movieShowsContainer');
            container.innerHTML = ''; // Clear container
            
            // Clear and populate available dates for booking modal
            availableDates.clear();

            // Group shows by date and version (API already filters out disabled rooms)
            const groupedShows = shows.reduce((acc, show) => {
                const dateKey = show.showDate;
                
                // Add date to available dates set for booking modal
                availableDates.add(dateKey);
                
                if (!acc[dateKey]) {
                    acc[dateKey] = {
                        dateText: dateKey,
                        versions: {}
                    };
                }

                const versionKey = show.versionName || 'Unknown';
                if (!acc[dateKey].versions[versionKey]) {
                    acc[dateKey].versions[versionKey] = new Set();
                }
                acc[dateKey].versions[versionKey].add(show.scheduleTime);
                return acc;
            }, {});

            // Filter out past dates - only show today and future dates
            const today = new Date();
            today.setHours(0, 0, 0, 0); // Reset time to start of day for comparison
            
            const filteredGroups = Object.values(groupedShows).filter(group => {
                const [day, month, year] = group.dateText.split('/');
                const dateObj = new Date(year, month - 1, day);
                return dateObj >= today; // Only include today and future dates
            });
            
            const sortedGroups = filteredGroups.sort((a, b) =>
                new Date(a.dateText.split('/').reverse().join('-')) - new Date(b.dateText.split('/').reverse().join('-'))
            );
            const scheduleSection = document.getElementById('scheduleSection');
            const bookSection = document.getElementById('bookSection');
            
            if (sortedGroups.length === 0) {
                container.innerHTML = '<div class="text-muted">No shows scheduled yet.</div>';
                if (scheduleSection) {
                    scheduleSection.style.display = 'none';
                }
                if (bookSection) {
                    bookSection.style.display = 'none';
                }
                return;
            }
            
            if (scheduleSection) {
                scheduleSection.style.display = 'block';
            }
            if (bookSection) {
                bookSection.style.display = 'block';
            }

            sortedGroups.forEach(group => {
                const groupDiv = document.createElement('div');
                groupDiv.className = 'schedule-date mb-4';

                const heading = document.createElement('h4');
                heading.className = 'text-muted mb-3 bg bg-info text-center';
                heading.innerHTML = group.dateText;
                heading.style = 'width: 150px; border-radius: 10px';
                groupDiv.appendChild(heading);

                // Sort versions alphabetically
                const sortedVersions = Object.keys(group.versions).sort((a, b) => a.localeCompare(b));

                sortedVersions.forEach(versionName => {
                    const versionDiv = document.createElement('div');
                    versionDiv.className = 'mb-3';

                    const versionLabel = document.createElement('div');
                    versionLabel.className = 'medium text-white mb-2';
                    versionLabel.innerHTML = versionName;
                    versionDiv.appendChild(versionLabel);

                    const timeContainer = document.createElement('div');
                    timeContainer.className = 'd-flex flex-wrap gap-2';

                    // Convert Set to Array and sort times
                    const sortedTimes = Array.from(group.versions[versionName]).sort((a, b) => a.localeCompare(b));
                    sortedTimes.forEach(time => {
                        const timeBtn = document.createElement('button');
                        timeBtn.className = 'btn btn-outline-light btn-sm';
                        timeBtn.innerText = time;
                        timeContainer.appendChild(timeBtn);
                    });

                    versionDiv.appendChild(timeContainer);
                    groupDiv.appendChild(versionDiv);
                });

                container.appendChild(groupDiv);
            });
        } catch (error) {
            console.error('Error loading movie shows:', error);
            document.getElementById('movieShowsContainer').innerHTML =
                '<div class="text-danger">Error loading movie shows</div>';
        }
    }

    // Function to populate date picker with available dates
    function populateDatePicker() {
        const dateSelect = document.getElementById('dateSelect');
        if (!dateSelect) return;
        
        dateSelect.innerHTML = '<option value="">— Select Date —</option>';
        
        if (availableDates.size > 0) {
            // Convert Set to Array and sort dates chronologically
            const sortedDates = Array.from(availableDates).sort((a, b) => {
                const [dayA, monthA, yearA] = a.split('/');
                const [dayB, monthB, yearB] = b.split('/');
                const dateA = new Date(yearA, monthA - 1, dayA);
                const dateB = new Date(yearB, monthB - 1, dayB);
                return dateA - dateB;
            });
            
            const today = new Date();
            today.setHours(0, 0, 0, 0); // Reset time to start of day for comparison
            
            sortedDates.forEach(date => {
                // Parse the date to check if it's today or future
                const [day, month, year] = date.split('/');
                const dateObj = new Date(year, month - 1, day);
                
                if (dateObj >= today) {
                    const option = document.createElement('option');
                    // Convert DD/MM/YYYY to YYYY-MM-DD for the value
                    option.value = `${year}-${month.padStart(2, '0')}-${day.padStart(2, '0')}`;
                    option.textContent = date; // Display as DD/MM/YYYY
                    dateSelect.appendChild(option);
                }
            });
        }
    }

    function updateVersions() {
        const date = document.getElementById('dateSelect').value;
        const versionSelect = document.getElementById('versionSelect');
        const timeSelect = document.getElementById('timeSelect');
        versionSelect.innerHTML = '<option value="">— Select Version —</option>';
        versionSelect.disabled = true;
        timeSelect.innerHTML = '<option value="">— Select Time —</option>';
        timeSelect.disabled = true;

        if (movieId && date) {
            fetch(`/Booking/GetVersions?movieId=${movieId}&date=${date}`)
                .then(response => response.json())
                .then(versions => {
                    if (versions.length > 0) {
                        versions.forEach(v => {
                            const option = document.createElement('option');
                            option.value = v.versionId;
                            option.textContent = v.versionName;
                            versionSelect.appendChild(option);
                        });
                        versionSelect.disabled = false;
                    }
                });
        }
    }

    function updateTimes() {
        const date = document.getElementById('dateSelect').value;
        const versionId = document.getElementById('versionSelect').value;
        const timeSelect = document.getElementById('timeSelect');
        timeSelect.innerHTML = '<option value="">— Select Time —</option>';
        timeSelect.disabled = true;

        if (movieId && date && versionId) {
            fetch(`/Booking/GetTimes?movieId=${movieId}&date=${date}&versionId=${versionId}`)
                .then(response => response.json())
                .then(times => {
                    if (times.length > 0) {
                        times.forEach(time => {
                            const option = document.createElement('option');
                            option.value = time;
                            option.textContent = time;
                            timeSelect.appendChild(option);
                        });
                        timeSelect.disabled = false;
                    }
                });
        }
    }

    function continueToSeats() {
        const date = document.getElementById('dateSelect').value;
        const versionId = document.getElementById('versionSelect').value;
        const time = document.getElementById('timeSelect').value;

        if (!movieId || !date || !versionId || !time) {
            alert('Please select all options');
            return;
        }

        const [year, month, day] = date.split('-');
        const formattedDate = `${day}/${month}/${year}`;

        window.location.href = `/Seat/Select?movieId=${movieId}&date=${formattedDate}&versionId=${versionId}&time=${time}`;
    }

    // Make functions globally available
    window.showActorModal = showActorModal;
    window.loadMovieShows = loadMovieShows;
    window.populateDatePicker = populateDatePicker;
    window.updateVersions = updateVersions;
    window.updateTimes = updateTimes;
    window.continueToSeats = continueToSeats;

    // Initialize the page
    initializePage();
});
