
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

function renderCalendar(year, month, selectedDate) {
    const calendar = document.getElementById('customCalendar');
    calendar.innerHTML = '';
    const today = new Date();
    const firstDay = new Date(year, month, 1);
    const lastDay = new Date(year, month + 1, 0);
    const startDay = firstDay.getDay() === 0 ? 6 : firstDay.getDay() - 1; // Monday start
    let html = '<table class="table table-bordered text-center"><thead><tr>';
    ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'].forEach(d => html += `<th>${d}</th>`);
    html += '</tr></thead><tbody><tr>';
    for (let i = 0; i < startDay; i++) html += '<td></td>';
    for (let d = 1; d <= lastDay.getDate(); d++) {
        const dateObj = new Date(year, month, d);
        const isToday = dateObj.toDateString() === today.toDateString();
        const isSelected = selectedDate && dateObj.toDateString() === selectedDate.toDateString();
        let classes = '';
        if (isToday) classes += ' bg-info';
        if (isSelected) classes += ' bg-primary text-white';

        // Format date key as yyyy-MM-dd
        const key = `${year}-${pad(month + 1)}-${pad(d)}`;
        let movieInfo = '';
        let uniqueMovieNames = movieShowSummary[key];
        if (typeof uniqueMovieNames === 'string') {
            try { uniqueMovieNames = JSON.parse(uniqueMovieNames); } catch (e) { uniqueMovieNames = []; }
        }
        if (!Array.isArray(uniqueMovieNames)) uniqueMovieNames = [];

        if (uniqueMovieNames.length > 0) {
            let movieList = uniqueMovieNames.slice(0, 2).map(name => `<div class='calendar-movie-title'>${name}</div>`).join('');
            if (uniqueMovieNames.length > 2) {
                movieList += `<div class='calendar-movie-more'>+${uniqueMovieNames.length - 2}</div>`;
            }
            movieInfo = `<div class='calendar-movies'>${movieList}</div>`;
        }

        html += `<td class="calendar-day${classes}" data-date="${pad(d)}/${pad(month + 1)}/${year}">` +
            `<div class='calendar-date-number'>${d}</div>${movieInfo}</td>`;
        if ((startDay + d) % 7 === 0) html += '</tr><tr>';
    }
    html += '</tr></tbody></table>';
    calendar.innerHTML = html;

    // Add click listeners
    document.querySelectorAll('.calendar-day').forEach(cell => {
        cell.onclick = function () {
            document.getElementById('calendarInput').value = this.getAttribute('data-date');
            document.getElementById('dateNavForm').submit();
        };
    });

    // Update month/year display
    document.getElementById('calendarMonthYear').textContent = `${monthNames[month]} ${year}`;
}

function navigateToMonth(year, month) {
    const monthKey = `${year}-${month}`;

    // If we already have the data for this month, just render the calendar
    if (fetchedMonths[monthKey]) {
        renderCalendar(year, month, selectedDate);
        return;
    }

    // Otherwise, fetch the data from the server
    fetch(`/Admin/GetMovieShowSummary?year=${year}&month=${month + 1}`) // JS month is 0-11, server expects 1-12
        .then(response => {
            if (!response.ok) {
                throw new Error('Network response was not ok');
            }
            return response.json();
        })
        .then(data => {
            // Add the new data to our summary object and mark the month as fetched
            Object.assign(movieShowSummary, data);
            fetchedMonths[monthKey] = true;
            // Now render the calendar with the new data
            renderCalendar(year, month, selectedDate);
        })
        .catch(error => {
            console.error('Error fetching movie summary:', error);
            // Even if fetching fails, render the calendar so the UI doesn't get stuck
            renderCalendar(year, month, selectedDate);
        });
}

// Initialize the page
document.addEventListener('DOMContentLoaded', function() {
    // Initial render
    renderCalendar(currentYear, currentMonth, selectedDate);

    // Update click handlers to use the new navigation function
    document.getElementById('prevMonth').onclick = function () {
        currentMonth--;
        if (currentMonth < 0) {
            currentMonth = 11;
            currentYear--;
        }
        navigateToMonth(currentYear, currentMonth);
    };

    document.getElementById('nextMonth').onclick = function () {
        currentMonth++;
        if (currentMonth > 11) {
            currentMonth = 0;
            currentYear++;
        }
        navigateToMonth(currentYear, currentMonth);
    };

    // Initialize pagination for showtime table
    initializePagination();
});

function initializePagination() {
    const tableBody = document.getElementById('showtimeTableBody');
    if (!tableBody) return;

    const rows = Array.from(tableBody.querySelectorAll('tr'));
    const rowsPerPage = 7;
    const pageCount = Math.ceil(rows.length / rowsPerPage);
    const paginationContainer = document.getElementById('showtime-pagination');

    if (pageCount <= 1) {
        if (paginationContainer) {
            paginationContainer.style.display = 'none';
        }
        return;
    }

    let currentPage = 1;

    function displayPage(page) {
        currentPage = page;
        tableBody.innerHTML = '';

        const start = (page - 1) * rowsPerPage;
        const end = start + rowsPerPage;
        const paginatedRows = rows.slice(start, end);

        paginatedRows.forEach(row => {
            tableBody.appendChild(row);
        });

        updatePaginationControls();
    }

    function updatePaginationControls() {
        if (!paginationContainer) return;
        paginationContainer.innerHTML = '';

        // Previous button
        const prevLi = document.createElement('li');
        prevLi.className = 'page-item';
        if (currentPage === 1) prevLi.classList.add('disabled');
        const prevLink = document.createElement('a');
        prevLink.className = 'page-link';
        prevLink.href = '#';
        prevLink.innerHTML = '&laquo;';
        prevLink.onclick = (e) => {
            e.preventDefault();
            if (currentPage > 1) displayPage(currentPage - 1);
        };
        prevLi.appendChild(prevLink);
        paginationContainer.appendChild(prevLi);

        // Page number buttons
        for (let i = 1; i <= pageCount; i++) {
            const pageLi = document.createElement('li');
            pageLi.className = 'page-item';
            if (i === currentPage) pageLi.classList.add('active');

            const pageLink = document.createElement('a');
            pageLink.className = 'page-link';
            pageLink.href = '#';
            pageLink.innerText = i;
            pageLink.onclick = (e) => {
                e.preventDefault();
                displayPage(i);
            };
            pageLi.appendChild(pageLink);
            paginationContainer.appendChild(pageLi);
        }

        // Next button
        const nextLi = document.createElement('li');
        nextLi.className = 'page-item';
        if (currentPage === pageCount) nextLi.classList.add('disabled');
        const nextLink = document.createElement('a');
        nextLink.className = 'page-link';
        nextLink.href = '#';
        nextLink.innerHTML = '&raquo;';
        nextLink.onclick = (e) => {
            e.preventDefault();
            if (currentPage < pageCount) displayPage(currentPage + 1);
        };
        nextLi.appendChild(nextLink);
        paginationContainer.appendChild(nextLi);
    }

    displayPage(1);
}