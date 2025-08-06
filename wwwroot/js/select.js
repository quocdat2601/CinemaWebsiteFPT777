document.addEventListener('DOMContentLoaded', function() {
    // Get data from global variables set by the view
    const selectedDate = window.selectedDate;
    const isAtToday = window.isAtToday;
    
    // Flag to prevent onChange from firing during initialization
    let isInitializing = true;
    
    // Initialize flatpickr with dd/MM/yyyy format
    // Remove minDate restriction to allow proper navigation
    flatpickr("#calendarInput", {
        dateFormat: "d/m/Y",
        defaultDate: selectedDate,
        allowInput: true,
        // Removed minDate: "today" to allow proper navigation
        onChange: function(selectedDates, dateStr, instance) {
            // Skip onChange during initialization
            if (isInitializing) {
                isInitializing = false;
                return;
            }
            
            // Only submit if the date is today or in the future
            const selectedDate = new Date(selectedDates[0]);
            const today = new Date();
            today.setHours(0, 0, 0, 0);
            
            if (selectedDate >= today) {
                // Submit form immediately when date is selected
                document.getElementById('dateNavForm').submit();
            } else {
                // Reset to today if user tries to select a past date
                instance.setDate(today, true, "d/m/Y");
            }
        },
        onClose: function(selectedDates, dateStr, instance) {
            // Also submit on close if a valid date was selected
            if (selectedDates.length > 0) {
                const selectedDate = new Date(selectedDates[0]);
                const today = new Date();
                today.setHours(0, 0, 0, 0);
                
                if (selectedDate >= today) {
                    document.getElementById('dateNavForm').submit();
                }
            }
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
        
        // Update flatpickr instance
        const flatpickrInstance = document.getElementById('calendarInput')._flatpickr;
        flatpickrInstance.setDate(formatted, true, "d/m/Y");
        
        // Submit form to navigate
        document.getElementById('dateNavForm').submit();
    }

    // Previous day button event listener
    var prevDayBtn = document.getElementById('prevDayBtn');
    if (prevDayBtn) {
        prevDayBtn.addEventListener('click', function() {
            const date = getSelectedDate();
            date.setDate(date.getDate() - 1);
            
            // Only allow navigation to today or future dates
            const today = new Date();
            today.setHours(0, 0, 0, 0);
            
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
