document.addEventListener('DOMContentLoaded', function() {
    // Get data from global variables set by the view
    const selectedDate = window.selectedDate;
    const isAtToday = window.isAtToday;
    
    // Initialize flatpickr with dd/MM/yyyy format
    flatpickr("#calendarInput", {
        dateFormat: "d/m/Y",
        defaultDate: selectedDate,
        allowInput: true,
        minDate: "today",
        onChange: function(selectedDates, dateStr, instance) {
            document.getElementById('dateNavForm').submit();
        }
    });

    function getSelectedDate() {
        // Get value in dd/MM/yyyy and parse
        const val = document.getElementById('calendarInput').value;
        const [d, m, y] = val.split('/');
        return new Date(`${y}-${m}-${d}`);
    }
    
    function setSelectedDate(date) {
        // Format as dd/MM/yyyy
        const d = date.getDate().toString().padStart(2, '0');
        const m = (date.getMonth() + 1).toString().padStart(2, '0');
        const y = date.getFullYear();
        const formatted = `${d}/${m}/${y}`;
        document.getElementById('calendarInput')._flatpickr.setDate(formatted, true, "d/m/Y");
        document.getElementById('dateNavForm').submit();
    }

    // Previous day button event listener
    var prevDayBtn = document.getElementById('prevDayBtn');
    if (prevDayBtn) {
        prevDayBtn.addEventListener('click', function() {
            const date = getSelectedDate();
            date.setDate(date.getDate() - 1);
            // Prevent going before today
            const today = new Date();
            today.setHours(0,0,0,0);
            if (date >= today) {
                setSelectedDate(date);
            }
        });
    }

    // Next day button event listener
    document.getElementById('nextDayBtn').addEventListener('click', function() {
        const date = getSelectedDate();
        date.setDate(date.getDate() + 1);
        setSelectedDate(date);
    });
});
