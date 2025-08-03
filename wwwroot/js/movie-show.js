

document.addEventListener('DOMContentLoaded', function () {
    const cinemaRoomSelect = document.getElementById('cinemaRoomSelect');
    const showDateSelect = document.getElementById('showDateSelect');
    const scheduleSelect = document.getElementById('scheduleSelect');
    const addMovieShowBtn = document.getElementById('addMovieShowBtn');
    const movieShowsContainer = document.getElementById('movieShowsContainer');
    const saveChangesBtn = document.getElementById('saveChangesBtn');
    const movieId = document.getElementById('movieId').value;
    const movieDuration = parseInt(document.getElementById('movieDuration').value, 10);
    const cleaningTime = 15; // You can change this value as needed
    const lastShowEndTimeEl = document.getElementById('lastShowEndTime');

    // Get data from global variables set by the view
    let movieShowItems = window.movieShowItems || [];
    const toDate = window.toDate;
    const availableShowDates = window.availableShowDates || [];

    async function updateAvailableSchedules() {
        const selectedRoom = cinemaRoomSelect.value;
        const selectedDate = showDateSelect.value;

        if (!selectedRoom || !selectedDate) {
            scheduleSelect.innerHTML = '<option value="">-- Select a Schedule --</option>';
            return;
        }

        // 1. Fetch backend schedules and shows
        const response = await fetch(`/Movie/GetAvailableScheduleTimes?cinemaRoomId=${selectedRoom}&showDate=${selectedDate}&movieDurationMinutes=${movieDuration}&cleaningTimeMinutes=${cleaningTime}`);
        const result = await response.json();
        const availableSchedules = result.schedules;

        // 2. Fetch backend shows for this room/date
        let backendShows = [];
        try {
            const resp = await fetch(`/Movie/GetMovieShowsByRoomAndDate?cinemaRoomId=${selectedRoom}&showDate=${selectedDate}`);
            if (resp.ok) {
                backendShows = await resp.json();
            }
        } catch (e) {
            console.error('Failed to fetch backend shows', e);
        }

        // 3. Get UI shows for this room/date
        const uiShows = movieShowItems.filter(item => item.roomId === selectedRoom && item.dateId === selectedDate);

        // 4. Merge shows
        const allShows = [...backendShows, ...uiShows];

        // 5. Filter available schedules to hide conflicts
        const filteredSchedules = availableSchedules.filter(schedule => {
            return !isScheduleConflicting(schedule.scheduleTime, allShows, movieDuration, cleaningTime);
        });

        // 6. Update dropdown
        scheduleSelect.innerHTML = '<option value="">-- Select a Schedule --</option>';
        filteredSchedules.forEach(schedule => {
            const option = document.createElement('option');
            option.value = schedule.scheduleId;
            option.textContent = schedule.scheduleTime;
            scheduleSelect.appendChild(option);
        });

        // 7. Update next available time hint
        if (allShows.length > 0) {
            // Find latest end time
            let latestEnd = null;
            for (const show of allShows) {
                if (!show.scheduleText) continue;
                const [startHour, startMin] = show.scheduleText.split(':').map(Number);
                const start = new Date(0, 0, 0, startHour, startMin);
                const end = new Date(start.getTime() + (movieDuration + cleaningTime) * 60000);
                if (!latestEnd || end > latestEnd) latestEnd = end;
            }
            if (latestEnd) {
                const h = latestEnd.getHours().toString().padStart(2, '0');
                const m = latestEnd.getMinutes().toString().padStart(2, '0');
                lastShowEndTimeEl.textContent = `This cinema is available at: ${h}:${m} (+ ${cleaningTime} mins cleaning)`;
                if (filteredSchedules.length > 0) {
                    lastShowEndTimeEl.textContent += ` - Next available time: ${filteredSchedules[0].scheduleTime}`;
                }
            }
        } else {
            lastShowEndTimeEl.textContent = 'No shows scheduled for this room and date yet.';
        }
    }

    function updateAddButtonState() {
        const hasRoom = cinemaRoomSelect.value !== '';
        const hasDate = showDateSelect.value !== '';
        const hasSchedule = scheduleSelect.value !== '';
        addMovieShowBtn.disabled = !(hasRoom && hasDate && hasSchedule);
    }

    // Helper: Populate Cinema Room options
    function populateCinemaRooms(rooms) {
        cinemaRoomSelect.innerHTML = '<option value="">-- Select a Cinema Room --</option>';
        rooms.forEach(room => {
            const option = document.createElement('option');
            option.value = room.cinemaRoomId;
            option.textContent = room.cinemaRoomName;
            cinemaRoomSelect.appendChild(option);
        });
    }

    // Helper: Populate Show Dates
    function populateShowDates(showDates) {
        showDateSelect.innerHTML = '<option value="">-- Select a Show Date --</option>';
        showDates.forEach(date => {
            const option = document.createElement('option');
            option.value = date.value;
            option.textContent = date.text;
            showDateSelect.appendChild(option);
        });
    }

    // Helper: Reset and disable dependent selects
    function resetAndDisable(select) {
        select.innerHTML = '<option value="">-- Select --</option>';
        select.disabled = true;
    }

    // 1. Version selection enables Cinema Room
    const versionRadios = document.querySelectorAll('.version-radio');
    versionRadios.forEach(radio => {
        radio.addEventListener('change', async function () {
            const versionId = this.value;
            // Reset and disable all dependent selects
            resetAndDisable(cinemaRoomSelect);
            resetAndDisable(showDateSelect);
            resetAndDisable(scheduleSelect);
            addMovieShowBtn.disabled = true;

            // Fetch rooms for this version
            const response = await fetch(`/Cinema/GetRoomsByVersion?versionId=${versionId}`);
            if (response.ok) {
                const rooms = await response.json();
                populateCinemaRooms(rooms);
                cinemaRoomSelect.disabled = false;
            } else {
                cinemaRoomSelect.innerHTML = '<option value="">-- No rooms available --</option>';
            }
        });
    });

    // 2. Cinema Room selection enables Show Date
    cinemaRoomSelect.addEventListener('change', function () {
        resetAndDisable(showDateSelect);
        resetAndDisable(scheduleSelect);
        addMovieShowBtn.disabled = true;
        const roomId = cinemaRoomSelect.value;
        if (!roomId) return;

        // Populate show dates from global variable
        const today = new Date();
        today.setHours(0, 0, 0, 0); // Set to midnight

        const filteredDates = availableShowDates.filter(d => {
            const dateObj = new Date(d.value);
            dateObj.setHours(0, 0, 0, 0); // Set to midnight
            return dateObj >= today && (!toDate || dateObj <= new Date(toDate));
        });
        populateShowDates(filteredDates);
        showDateSelect.disabled = false;
    });

    // 3. Show Date selection enables Schedule
    showDateSelect.addEventListener('change', async function () {
        resetAndDisable(scheduleSelect);
        addMovieShowBtn.disabled = true;
        const roomId = cinemaRoomSelect.value;
        const selectedDate = showDateSelect.value;
        if (!roomId || !selectedDate) return;
        await updateAvailableSchedules();
        scheduleSelect.disabled = false;
    });

    // 4. Schedule selection enables Add button
    scheduleSelect.addEventListener('change', function () {
        updateAddButtonState();
    });

    // Initial state: all selects except version are disabled
    resetAndDisable(cinemaRoomSelect);
    resetAndDisable(showDateSelect);
    resetAndDisable(scheduleSelect);
    addMovieShowBtn.disabled = true;

    addMovieShowBtn.addEventListener('click', addMovieShow);

    // Force the ongoing tab to be active on first load
    var ongoingTab = document.getElementById('ongoing-tab');
    if (ongoingTab) {
        var tab = new bootstrap.Tab(ongoingTab);
        tab.show();
    }

    // On initial load
    renderMovieShows();

    // On tab switch
    ['show.bs.tab', 'shown.bs.tab'].forEach(function (eventName) {
        document.getElementById('ongoing-tab').addEventListener(eventName, function (event) {
            renderMovieShows();
        });
        document.getElementById('old-tab').addEventListener(eventName, function (event) {
            renderMovieShows();
        });
    });

    function renderMovieShows() {
        const ongoingContainer = document.getElementById('movieShowsOngoingContainer');
        const oldContainer = document.getElementById('movieShowsOldContainer');
        ongoingContainer.innerHTML = '';
        oldContainer.innerHTML = '';

        // Get today's and tomorrow's date in yyyy-MM-dd
        const today = new Date();
        today.setHours(0, 0, 0, 0);

        // Helper to parse yyyy-MM-dd
        function parseDate(dateStr) {
            const [year, month, day] = dateStr.split('-').map(Number);
            return new Date(year, month - 1, day, 0, 0, 0, 0);
        }

        function isSameDay(d1, d2) {
            return d1.getFullYear() === d2.getFullYear() &&
                d1.getMonth() === d2.getMonth() &&
                d1.getDate() === d2.getDate();
        }

        // Split shows
        const ongoingShows = [];
        const oldShows = [];

        movieShowItems.forEach(item => {
            const showDate = parseDate(item.dateId); // parseDate returns a Date object, time at midnight
            if (showDate >= today) {
                ongoingShows.push(item);
            } else {
                oldShows.push(item);
            }
        });

        // Helper to render shows into a container
        function renderShows(shows, container) {
            if (shows.length === 0) {
                container.innerHTML = '<h3 class="text-muted mt-3 mb-3 text-center">No shows found.</h3>';
                return;
            }

            // Sort and index
            shows.sort((a, b) => {
                const dateComparison = a.dateId.localeCompare(b.dateId);
                if (dateComparison !== 0) return dateComparison;
                const roomComparison = a.roomName.localeCompare(b.roomName);
                if (roomComparison !== 0) return roomComparison;
                return a.scheduleText.localeCompare(b.scheduleText);
            });

            const indexedShows = shows.map((item, index) => ({ ...item, originalIndex: movieShowItems.indexOf(item) }));

            // Group by date
            const groupedByDate = indexedShows.reduce((acc, show) => {
                if (!acc[show.dateId]) {
                    acc[show.dateId] = [];
                }
                acc[show.dateId].push(show);
                return acc;
            }, {});

            Object.keys(groupedByDate).sort((a, b) => a.localeCompare(b)).forEach(dateId => {
                const showsForDate = groupedByDate[dateId];
                const dateText = new Date(dateId).toLocaleDateString('en-GB');

                const dateContainer = document.createElement('div');
                dateContainer.className = 'date-group mb-4 p-3 border rounded';

                const dateHeader = document.createElement('h5');
                dateHeader.className = 'date-header mb-3 border-bottom pb-2 text-black';
                dateHeader.textContent = `Date: ${dateText}`;
                dateContainer.appendChild(dateHeader);

                showsForDate.forEach(show => {
                    const showDiv = document.createElement('div');
                    showDiv.className = 'movie-show-entry alert alert-light d-flex justify-content-between align-items-center mb-2';

                    let endTimeText = '';
                    if (show.scheduleText && show.scheduleText !== 'N/A' && movieDuration) {
                        const [hours, minutes] = show.scheduleText.split(':').map(Number);
                        const startTime = new Date();
                        startTime.setHours(hours, minutes, 0, 0);

                        const endTime = new Date(startTime.getTime() + movieDuration * 60000);

                        const endHours = endTime.getHours().toString().padStart(2, '0');
                        const endMinutes = endTime.getMinutes().toString().padStart(2, '0');
                        endTimeText = ` ~ ${endHours}:${endMinutes}`;
                    }

                    const contentDiv = document.createElement('div');
                    contentDiv.innerHTML = `
                            <span class="fw-bold me-2">Room:</span><span>${show.roomName}</span>
                            <span class="fw-bold ms-3 me-2">Time:</span><span>[${show.scheduleText}${endTimeText}]</span>
                            <span class="fw-bold ms-3 me-2">Version:</span><span>${show.version}</span>
                        `;

                    const removeButton = document.createElement('button');
                    removeButton.type = 'button';
                    removeButton.className = 'btn btn-outline-danger btn-sm';
                    removeButton.innerHTML = 'Remove';
                    removeButton.onclick = function () {
                        removeMovieShow(show.originalIndex);
                    };

                    showDiv.appendChild(contentDiv);
                    showDiv.appendChild(removeButton);
                    dateContainer.appendChild(showDiv);
                });

                container.appendChild(dateContainer);
            });
        }

        renderShows(ongoingShows, ongoingContainer);
        renderShows(oldShows, oldContainer);
    }

    async function addMovieShow() {
        const selectedDateId = showDateSelect.value;
        const selectedDateText = showDateSelect.options[showDateSelect.selectedIndex].text;
        const selectedScheduleId = scheduleSelect.value;
        const selectedScheduleText = scheduleSelect.options[scheduleSelect.selectedIndex].text;

        const roomId = cinemaRoomSelect.value;
        const roomName = cinemaRoomSelect.options[cinemaRoomSelect.selectedIndex].text;

        // Get the selected version ID from the radio button
        const selectedVersionRadio = document.querySelector('input[name="SelectedVersionIds"]:checked');
        const versionId = selectedVersionRadio ? parseInt(selectedVersionRadio.value) : null;
        const versionName = selectedVersionRadio ? selectedVersionRadio.parentElement.textContent.trim() : "N/A";

        // Build the new show object
        const movieShowItem = {
            dateId: selectedDateId,
            dateText: selectedDateText,
            scheduleId: selectedScheduleId,
            scheduleText: selectedScheduleText,
            roomId: roomId,
            roomName: roomName,
            versionId: versionId,
            version: versionName
        };

        // --- Call checkAndSuggest before adding ---
        const allShowsForRoomDate = await getMergedShowsForRoomDate(roomId, selectedDateId);

        if (isTimeConflict(movieShowItem, allShowsForRoomDate, movieDuration, cleaningTime)) {
            alert('This schedule conflicts with another show in this room and date.');
            return;
        }

        movieShowItems.push(movieShowItem);
        renderMovieShows();
        scheduleSelect.innerHTML = '<option value="">-- Select a Schedule --</option>';

        await updateAvailableSchedules();

        updateAddButtonState();
    }

    function removeMovieShow(index) {
        movieShowItems.splice(index, 1);
        renderMovieShows();
    }

    saveChangesBtn.addEventListener('click', async function () {
        try {
            if (movieShowItems.length === 0) {
                // Call backend to delete all shows for this movie
                await fetch('/Movie/DeleteAllMovieShows', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ movieId: movieId })
                });
                alert('All movie shows deleted.');
                window.location.reload();
                return;
            }

            // 1. Fetch current shows from backend (get all for this movie)
            const resp = await fetch(`/Movie/ViewShow/${movieId}`, {
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            });
            const backendShows = await resp.json();

            // 2. Find shows to delete (in backend but not in UI)
            const showsToDelete = backendShows.filter(b =>
                !movieShowItems.some(u =>
                    u.dateId === b.showDate.split('/').reverse().join('-') &&
                    u.roomId == b.cinemaRoomId &&
                    u.scheduleId == b.scheduleId &&
                    u.versionId == b.versionId
                )
            );

            // 3. Find shows to add (in UI but not in backend)
            const showsToAdd = movieShowItems.filter(u =>
                !backendShows.some(b =>
                    u.dateId === b.showDate.split('/').reverse().join('-') &&
                    u.roomId == b.cinemaRoomId &&
                    u.scheduleId == b.scheduleId &&
                    u.versionId == b.versionId
                )
            );

            // 4. Try to delete shows (one by one, safely)
            let undeletableShows = [];
            for (const show of showsToDelete) {
                const delResp = await fetch('/Movie/DeleteMovieShowIfNotReferenced', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(show.movieShowId)
                });
                const delResult = await delResp.json();
                if (!delResult.success) {
                    undeletableShows.push(show);
                }
            }

            // 5. Add new shows
            for (const showItem of showsToAdd) {
                const response = await fetch('/Movie/AddMovieShow', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({
                        movieId: movieId,
                        showDate: showItem.dateId,
                        scheduleId: showItem.scheduleId,
                        cinemaRoomId: showItem.roomId,
                        versionId: showItem.versionId
                    })
                });
                if (!response.ok) {
                    throw new Error(`Failed to add movie show for schedule ${showItem.scheduleText}`);
                }
            }

            if (undeletableShows.length > 0) {
                alert('Some shows could not be deleted because they are referenced by invoices.');
            } else {
                alert('Movie shows saved successfully!');
            }
            window.location.reload();
        } catch (error) {
            console.error('Error saving movie shows:', error);
            alert('An error occurred while saving the movie shows. Please try again.');
        }
    });

    async function getMergedShowsForRoomDate(roomId, dateId) {
        // 1. Fetch backend shows
        let backendShows = [];
        try {
            const resp = await fetch(`/Movie/GetMovieShowsByRoomAndDate?cinemaRoomId=${roomId}&showDate=${dateId}`);
            if (resp.ok) {
                backendShows = await resp.json();
            }
        } catch (e) {
            console.error('Failed to fetch backend shows', e);
        }

        // 2. Get UI shows
        const uiShows = movieShowItems.filter(item => item.roomId === roomId && item.dateId === dateId);

        // 3. Merge (avoid duplicates if needed)
        // If you want to avoid duplicate scheduleText, you can filter here
        const allShows = [...backendShows, ...uiShows];

        return allShows;
    }

    function getNextAvailableTime(allShows, movieDuration, cleaningTime) {
        // allShows: array of shows for the room/date, each with a scheduleText (e.g., "14:00")
        // Returns a string like "15:30" or null if no shows

        if (!allShows || allShows.length === 0) return null;

        // Sort by scheduleText (time)
        allShows.sort((a, b) => a.scheduleText.localeCompare(b.scheduleText));

        // Find the latest end time
        let latestEnd = null;
        for (const show of allShows) {
            if (!show.scheduleText) continue;
            const [startHour, startMin] = show.scheduleText.split(':').map(Number);
            const start = new Date(0, 0, 0, startHour, startMin);
            const end = new Date(start.getTime() + (movieDuration + cleaningTime) * 60000);
            if (!latestEnd || end > latestEnd) latestEnd = end;
        }

        // Format as HH:mm
        if (latestEnd) {
            const h = latestEnd.getHours().toString().padStart(2, '0');
            const m = latestEnd.getMinutes().toString().padStart(2, '0');
            return `${h}:${m}`;
        }
        return null;
    }

    async function checkAndSuggest(roomId, dateId, newScheduleText) {
        const allShows = await getMergedShowsForRoomDate(roomId, dateId);

        // Conflict check
        if (allShows.some(show => show.scheduleText === newScheduleText)) {
            alert('This schedule conflicts with another show in this room and date.');
            return false;
        }

        // Next available time
        const nextTime = getNextAvailableTime(allShows, movieDuration, cleaningTime);
        if (nextTime) {
            lastShowEndTimeEl.textContent = `Next available time: ${nextTime}`;
        } else {
            lastShowEndTimeEl.textContent = 'No shows scheduled for this room and date yet.';
        }
        return true;
    }

    function isTimeConflict(newShow, allShows, movieDuration, cleaningTime) {
        // Parse new show's start and end time
        const [newStartHour, newStartMin] = newShow.scheduleText.split(':').map(Number);
        const newStart = new Date(0, 0, 0, newStartHour, newStartMin);
        const newEnd = new Date(newStart.getTime() + (movieDuration + cleaningTime) * 60000);

        for (const show of allShows) {
            if (!show.scheduleText) continue;
            const [showStartHour, showStartMin] = show.scheduleText.split(':').map(Number);
            const showStart = new Date(0, 0, 0, showStartHour, showStartMin);
            const showEnd = new Date(showStart.getTime() + (movieDuration + cleaningTime) * 60000);

            // Check for overlap
            if (newStart < showEnd && newEnd > showStart) {
                return true; // There is a conflict
            }
        }
        return false; // No conflict
    }

    function isScheduleConflicting(scheduleTime, allShows, movieDuration, cleaningTime) {
        const [newStartHour, newStartMin] = scheduleTime.split(':').map(Number);
        const newStart = new Date(0, 0, 0, newStartHour, newStartMin);
        const newEnd = new Date(newStart.getTime() + (movieDuration + cleaningTime) * 60000);

        for (const show of allShows) {
            if (!show.scheduleText) continue;
            const [showStartHour, showStartMin] = show.scheduleText.split(':').map(Number);
            const showStart = new Date(0, 0, 0, showStartHour, showStartMin);
            const showEnd = new Date(showStart.getTime() + (movieDuration + cleaningTime) * 60000);

            if (newStart < showEnd && newEnd > showStart) {
                return true;
            }
        }
        return false;
    }
});
