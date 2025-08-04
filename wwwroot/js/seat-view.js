
let selectedSeats = new Set();
let countdownTimer = null;
let secondsLeft = 60;
const MAX_SEATS = 8;
let selectedFoods = {};
let foodPrices = {};
let seatHubConnection = null;
let currentSeatTotal = 0; // Store raw seat total for calculations

// Get data from global variables set by the view
const coupleSeatPairs = window.coupleSeatPairs || {};
const movieShowId = window.movieShowId || 0;
const isAdminSell = window.isAdminSell || false;

// Initialize food prices from global variable
if (window.foodPrices) {
    foodPrices = window.foodPrices;
}

// Initialize SignalR connection for real-time seat selection
function initializeSignalR() {
    seatHubConnection = new signalR.HubConnectionBuilder()
        .withUrl("/seathub")
        .withAutomaticReconnect()
        .build();

    seatHubConnection.on("SeatSelected", function (seatId) {
        const seatElement = document.querySelector(`[data-seat-id="${seatId}"]`);
        if (seatElement && !seatElement.classList.contains('selected')) {
            seatElement.classList.add('being-held');
        }
    });

    seatHubConnection.on("SeatDeselected", function (seatId) {
        const seatElement = document.querySelector(`[data-seat-id="${seatId}"]`);
        if (seatElement) {
            seatElement.classList.remove('being-held');
        }
    });

    seatHubConnection.on("SeatStatusChanged", function (seatId, newStatusId) {
        console.log("SeatStatusChanged", seatId, newStatusId);
        const seatElement = document.querySelector(`[data-seat-id="${seatId}"]`);
        if (!seatElement) return;

        if (newStatusId === 2) { // 2 = Booked
            seatElement.classList.add("booked");
            seatElement.classList.remove("selected", "being-held");
            seatElement.setAttribute("disabled", "disabled");
            // Keep the original seat type color from database, CSS will handle the darkening
            seatElement.style.backgroundColor = seatElement.getAttribute("data-seat-color") || "#cccbc8";
            selectedSeats.delete(String(seatId));
        } else {
            seatElement.classList.remove("booked");
            seatElement.removeAttribute("disabled");
            // Set lại màu gốc loại ghế
            const color = seatElement.getAttribute("data-seat-color") || "#cccbc8";
            seatElement.style.backgroundColor = color;
            // Remove from selectedSeats and UI
            selectedSeats.delete(String(seatId));
            seatElement.classList.remove("selected");
        }
        updateSelectionSummary();
        updateBookButtonState();
        updateFoodSectionVisibility();
    });

    seatHubConnection.on("AccountInUse", function () {
        // Hiển thị modal, disable UI chọn ghế
        document.getElementById('accountInUseModal').style.display = 'flex';
        document.getElementById('seatSelectionArea').style.pointerEvents = 'none';
        document.getElementById('seatSelectionArea').style.opacity = '0.4';
        document.getElementById('summarySeatBlock').style.opacity = '0.4';
        var bookBtn = document.getElementById('bookButton');
        if (bookBtn) bookBtn.disabled = true;
    });

    // Nhận 2 danh sách: ghế của mình (heldByMe), ghế của người khác (heldByOthers)
    seatHubConnection.on("HeldSeats", function (heldByMe, heldByOthers) {
        // Reset trạng thái
        document.querySelectorAll('.selected').forEach(seat => {
            seat.classList.remove("selected");
        });
        document.querySelectorAll('.being-held').forEach(seat => {
            seat.classList.remove("being-held");
            seat.removeAttribute("title");
        });
        selectedSeats.clear();

        // Đánh dấu ghế của chính mình
        heldByMe.forEach(seatId => {
            const seat = document.querySelector(`[data-seat-id='${seatId}']`);
            if (seat) {
                seat.classList.add("selected");
                selectedSeats.add(String(seatId));
            }
        });

        // Đánh dấu ghế của người khác
        heldByOthers.forEach(seatId => {
            const seat = document.querySelector(`[data-seat-id='${seatId}']`);
            if (seat && !seat.classList.contains("selected")) {
                seat.classList.add("being-held");
                seat.setAttribute("title", "Ghế đang được người khác giữ");
            }
        });

        updateSelectionSummary();
        updateBookButtonState();
        updateFoodSectionVisibility();
    });

    seatHubConnection.on("SeatsReleased", function (seatIds) {
        seatIds.forEach(seatId => {
            const seat = document.querySelector(`[data-seat-id='${seatId}']`);
            seat?.classList.remove("being-held");
            seat?.removeAttribute("title");
        });
    });

    seatHubConnection.start()
        .then(function () {
            console.log("SignalR Connected");
            // Join the showtime group
            if (movieShowId && movieShowId !== 0) {
                seatHubConnection.invoke("JoinShowtime", parseInt(movieShowId));
            }
        })
        .catch(function (err) {
            console.error("SignalR Connection Error: ", err);
        });
}

function selectSeat(seatElement) {
    const seatId = String(seatElement.getAttribute('data-seat-id'));
    if (seatElement.classList.contains('disabled') ||
        seatElement.classList.contains('being-held') ||
        seatElement.classList.contains('booked')) return;

    // Couple seat auto-select logic
    if (coupleSeatPairs && coupleSeatPairs[seatId]) {
        const otherSeatId = coupleSeatPairs[seatId].toString();
        const otherSeatElement = document.querySelector(`[data-seat-id='${otherSeatId}']`);
        const isSelected = seatElement.classList.contains('selected');

        if (isSelected) {
            // Deselect both seats
            seatElement.classList.remove('selected');
            selectedSeats.delete(seatId);
            if (otherSeatElement) {
                otherSeatElement.classList.remove('selected');
                selectedSeats.delete(otherSeatId);
            }

            // Notify server about deselection
            if (seatHubConnection && seatHubConnection.state === signalR.HubConnectionState.Connected) {
                seatHubConnection.invoke("DeselectSeat", parseInt(movieShowId), parseInt(seatId));
                if (otherSeatElement) seatHubConnection.invoke("DeselectSeat", parseInt(movieShowId), parseInt(otherSeatId));
            }
        } else {
            // Check seat limit for couple seats (need 2 seats)
            if (selectedSeats.size + 2 > MAX_SEATS) {
                alert('Bạn đã chọn tối đa ' + MAX_SEATS + ' ghế. Vui lòng bỏ chọn một ghế khác trước khi chọn ghế mới.');
                return;
            }

            // Select both seats
            seatElement.classList.add('selected');
            selectedSeats.add(seatId);
            if (otherSeatElement && !otherSeatElement.classList.contains('disabled') &&
                !otherSeatElement.classList.contains('being-held') &&
                !otherSeatElement.classList.contains('booked')) {
                otherSeatElement.classList.add('selected');
                selectedSeats.add(otherSeatId);
            } else {
                alert('Ghế cặp không khả dụng. Không thể chọn couple seat.');
                seatElement.classList.remove('selected');
                selectedSeats.delete(seatId);
                return;
            }

            // Notify server about both selections
            if (seatHubConnection && seatHubConnection.state === signalR.HubConnectionState.Connected) {
                seatHubConnection.invoke("SelectSeat", parseInt(movieShowId), parseInt(seatId));
                if (otherSeatElement) seatHubConnection.invoke("SelectSeat", parseInt(movieShowId), parseInt(otherSeatId));
            }

            if (selectedSeats.size === 2) startCountdown();
        }
        updateSelectionSummary();
        updateBookButtonState();
        updateFoodSectionVisibility();
    } else {
        // Normal seat logic
        if (selectedSeats.has(seatId)) {
            seatElement.classList.remove('selected');
            selectedSeats.delete(seatId);

            // Notify server about deselection
            if (seatHubConnection && seatHubConnection.state === signalR.HubConnectionState.Connected) {
                seatHubConnection.invoke("DeselectSeat", parseInt(movieShowId), parseInt(seatId));
            }
        } else {
            if (selectedSeats.size >= MAX_SEATS) {
                alert('Bạn đã chọn tối đa ' + MAX_SEATS + ' ghế. Vui lòng bỏ chọn một ghế khác trước khi chọn ghế mới.');
                return;
            }
            seatElement.classList.add('selected');
            selectedSeats.add(seatId);

            // Notify server about selection
            if (seatHubConnection && seatHubConnection.state === signalR.HubConnectionState.Connected) {
                seatHubConnection.invoke("SelectSeat", parseInt(movieShowId), parseInt(seatId));
            }

            if (selectedSeats.size === 1) startCountdown();
        }
        updateSelectionSummary();
        updateBookButtonState();
        updateFoodSectionVisibility();
    }

    // Stop countdown if no seats are selected
    if (selectedSeats.size === 0 && countdownTimer) {
        clearInterval(countdownTimer);
        countdownTimer = null;
        const timerEl = document.getElementById("countdownTimer");
        if (timerEl) {
            timerEl.textContent = '';
        }
    }
}

function updateSelectionSummary() {
    const selectedSeatNames = document.getElementById('selectedSeatNames');
    const totalPrice = document.getElementById('totalPrice');
    const seatInfo = document.getElementById('seatInfo');
    const noSeatAlert = document.getElementById('noSeatAlert');

    if (selectedSeats.size > 0) {
        let total = 0;
        let seatNames = [];
        document.querySelectorAll('.seat.selected').forEach(seat => {
            const seatName = seat.getAttribute('data-seat-name');
            const calculatedPrice = parseFloat(seat.getAttribute('data-seat-price')) || 0;
            seatNames.push(seatName);
            total += calculatedPrice;
        });

        selectedSeatNames.textContent = seatNames.join(', ');
        currentSeatTotal = total; // Store raw value
        totalPrice.textContent = total.toLocaleString();
        seatInfo.style.display = 'block';
        noSeatAlert.style.display = 'none';
    } else {
        currentSeatTotal = 0; // Reset when no seats selected
        seatInfo.style.display = 'none';
        noSeatAlert.style.display = 'block';
    }
    updateFoodTotal();
}

function updateBookButtonState() {
    document.getElementById('bookButton').disabled = (selectedSeats.size === 0);
}

function updateFoodSectionVisibility() {
    const foodSection = document.getElementById('foodSelectionArea');
    const seatSection = document.getElementById('seatSelectionArea');
    const bookButton = document.getElementById('bookButton');
    const btnText = bookButton.querySelector('.btn-text');

    if (selectedSeats.size > 0) {
        // Show food section if seats are selected
        if (seatSection.style.display !== 'none') {
            // We're in seat selection mode, button should say "Continue"
            btnText.textContent = 'Continue';
        } else {
            // We're in food selection mode, button should say "Continue to Payment"
            btnText.textContent = 'Continue to Payment';
        }
    } else {
        // No seats selected, hide food section
        foodSection.style.display = 'none';
        seatSection.style.display = 'block';
        btnText.textContent = 'Continue';
    }
}

function startCountdown() {
    const timerEl = document.getElementById("countdownTimer");
    if (countdownTimer) clearInterval(countdownTimer);
    secondsLeft = 60;

    countdownTimer = setInterval(() => {
        if (secondsLeft <= 0) {
            clearInterval(countdownTimer);

            // Notify server to release all held seats
            if (seatHubConnection && seatHubConnection.state === signalR.HubConnectionState.Connected) {
                selectedSeats.forEach(seatId => {
                    seatHubConnection.invoke("DeselectSeat", parseInt(movieShowId), parseInt(seatId));
                });
            }

            // Clear all selected seats
            selectedSeats.clear();
            document.querySelectorAll('.seat.selected').forEach(seat => {
                seat.classList.remove('selected');
            });

            // Update UI
            updateSelectionSummary();
            updateBookButtonState();

            // Show full-screen timeout notification
            showTimeoutNotification();
        } else {
            const min = Math.floor(secondsLeft / 60);
            const sec = secondsLeft % 60;
            timerEl.textContent = `Time left: ${min}:${sec.toString().padStart(2, '0')}`;

            // Add urgent class when time is running low (less than 10 seconds)
            if (secondsLeft <= 10) {
                timerEl.classList.add('urgent');
            } else {
                timerEl.classList.remove('urgent');
            }

            secondsLeft--;
        }
    }, 1000);
}

function updateFoodTotal() {
    let totalFood = 0;
    let foodListHtml = '';
    let hasFood = false;

    for (let foodId in selectedFoods) {
        if (selectedFoods[foodId] > 0) {
            totalFood += (foodPrices[foodId] || 0) * selectedFoods[foodId];
            hasFood = true;
        }
    }

    document.getElementById('totalFoodPrice').textContent = totalFood.toLocaleString();

    // Update the selected food list
    const selectedFoodList = document.getElementById('selectedFoodList');
    console.log('Selected foods:', selectedFoods, 'Has food:', hasFood);
    if (hasFood) {
        foodListHtml = '';
        for (let foodId in selectedFoods) {
            if (selectedFoods[foodId] > 0) {
                // Get food name from the food item element
                const foodItem = document.querySelector(`.food-item[data-food-id="${foodId}"]`);
                const foodName = foodItem ? foodItem.querySelector('.food-name').textContent : `Food ${foodId}`;
                foodListHtml += `<li>${foodName} x${selectedFoods[foodId]}</li>`;
                console.log('Adding food to list:', foodName, 'x', selectedFoods[foodId]);
            }
        }
        selectedFoodList.innerHTML = foodListHtml;
    } else {
        selectedFoodList.innerHTML = '<li class="no-food">No food selected</li>';
    }

    // Use the stored raw seat total for accurate calculation
    let grandTotal = currentSeatTotal + totalFood;
    document.getElementById('grandTotal').textContent = grandTotal.toLocaleString() + ' VND';
}

function setSeatColorByStatus(seatElement) {
    if (seatElement.classList.contains('booked')) {
        // Keep the original seat type color from database, CSS will handle the darkening
        seatElement.style.backgroundColor = seatElement.getAttribute('data-seat-color') || '#cccbc8';
    } else if (seatElement.classList.contains('selected')) {
        seatElement.style.backgroundColor = '#10b981';
    } else if (seatElement.classList.contains('being-held')) {
        seatElement.style.backgroundColor = '#f59e42';
    } else {
        // Mặc định lấy màu loại ghế từ data-seat-color
        seatElement.style.backgroundColor = seatElement.getAttribute('data-seat-color') || '#cccbc8';
    }
}

// Initialize
document.addEventListener('DOMContentLoaded', function () {
    // Initialize SignalR connection
    initializeSignalR();

    // Food quantity controls
    document.querySelectorAll('.qty-btn.plus').forEach(btn => {
        btn.addEventListener('click', function () {
            const foodId = this.getAttribute('data-food-id');
            let qtyInput = document.querySelector(`.qty-input[data-food-id='${foodId}']`);
            let qty = parseInt(qtyInput.value) || 0;
            qty++;
            qtyInput.value = qty;
            selectedFoods[foodId] = qty;
            console.log('Food selected:', foodId, 'Quantity:', qty);
            updateFoodTotal();
        });
    });

    document.querySelectorAll('.qty-btn.minus').forEach(btn => {
        btn.addEventListener('click', function () {
            const foodId = this.getAttribute('data-food-id');
            let qtyInput = document.querySelector(`.qty-input[data-food-id='${foodId}']`);
            let qty = parseInt(qtyInput.value) || 0;
            if (qty > 0) qty--;
            qtyInput.value = qty;
            if (qty === 0) delete selectedFoods[foodId];
            else selectedFoods[foodId] = qty;
            console.log('Food deselected:', foodId, 'Quantity:', qty);
            updateFoodTotal();
        });
    });

    // Back to seat button
    document.getElementById('backToSeatBtn')?.addEventListener('click', function () {
        document.getElementById('foodSelectionArea').style.display = 'none';
        document.getElementById('seatSelectionArea').style.display = 'block';
    });

    // Category filtering
    document.querySelectorAll('.category-tab').forEach(tab => {
        tab.addEventListener('click', function () {
            const selectedCategory = this.getAttribute('data-category');
            
            // Update active tab
            document.querySelectorAll('.category-tab').forEach(t => t.classList.remove('active'));
            this.classList.add('active');
            
            // Filter food items
            const foodItems = document.querySelectorAll('.food-item');
            foodItems.forEach(item => {
                const itemCategory = item.getAttribute('data-category');
                if (selectedCategory === 'all' || itemCategory === selectedCategory) {
                    item.style.display = 'block';
                } else {
                    item.style.display = 'none';
                }
            });
        });
    });

    // Continue button
    document.getElementById('bookButton').addEventListener('click', function () {
        if (this.disabled || selectedSeats.size === 0) return;

        const seatSelectionArea = document.getElementById('seatSelectionArea');
        const foodSelectionArea = document.getElementById('foodSelectionArea');

        if (seatSelectionArea.style.display !== 'none') {
            // Go to food selection
            seatSelectionArea.style.display = 'none';
            foodSelectionArea.style.display = 'block';
            this.querySelector('.btn-text').textContent = 'Continue to Payment';
        } else {
            // Go to payment
            const selectedSeatIds = Array.from(selectedSeats);
            const data = {
                movieId: window.movieId,
                showDate: window.showDate,
                showTime: window.showTime,
                selectedSeatIds: selectedSeatIds,
                movieShowId: movieShowId
            };

            const params = new URLSearchParams();
            for (const key in data) {
                if (Array.isArray(data[key])) {
                    data[key].forEach(item => params.append(key, item));
                } else {
                    params.append(key, data[key]);
                }
            }

            for (const foodId in selectedFoods) {
                if (selectedFoods[foodId] > 0) {
                    params.append('foodIds', foodId);
                    params.append('foodQtys', selectedFoods[foodId]);
                }
            }

            window.location.href = isAdminSell
                ? `/Booking/ConfirmTicketForAdmin?${params.toString()}`
                : `/Booking/Information?${params.toString()}`;
        }
    });

    updateBookButtonState();
    updateSelectionSummary();
    updateFoodTotal();
});

// Function to show timeout notification
function showTimeoutNotification() {
    // Create full-screen overlay
    const overlay = document.createElement('div');
    overlay.style.cssText = `
        position: fixed;
        top: 0;
        left: 0;
        width: 100%;
        height: 100%;
        background: rgba(0, 0, 0, 0.9);
        display: flex;
        align-items: center;
        justify-content: center;
        z-index: 9999;
        animation: fadeIn 0.3s ease-out;
    `;
    
    // Create modal content
    const modal = document.createElement('div');
    modal.style.cssText = `
        background: var(--bg-secondary);
        border: 1px solid var(--border-color);
        border-radius: var(--radius-xl);
        padding: 3rem;
        text-align: center;
        max-width: 500px;
        width: 90%;
        box-shadow: var(--shadow-xl);
        animation: scaleIn 0.3s ease-out;
    `;
    
    modal.innerHTML = `
        <div style="margin-bottom: 2rem;">
            <div style="font-size: 3rem; margin-bottom: 1rem;">⏰</div>
            <h2 style="color: var(--text-primary); font-size: 1.5rem; font-weight: 700; margin-bottom: 1rem;">Time Expired!</h2>
            <p style="color: var(--text-secondary); font-size: 1rem; line-height: 1.6;">
                Your seat selection time has expired. Please click the button below to return to the home page.
            </p>
        </div>
        <button id="goHomeBtn" style="
            background: var(--primary-color);
            color: white;
            border: none;
            padding: 0.875rem 1.75rem;
            border-radius: var(--radius-md);
            font-size: 1rem;
            font-weight: 600;
            cursor: pointer;
            transition: all 0.2s ease;
            display: flex;
            align-items: center;
            gap: 0.5rem;
            margin: 0 auto;
        " onmouseover="this.style.background='var(--primary-dark)'" onmouseout="this.style.background='var(--primary-color)'">
            <span>🏠</span>
            Go to Home
        </button>
    `;
    
    // Add CSS animations
    const style = document.createElement('style');
    style.textContent = `
        @keyframes fadeIn {
            from { opacity: 0; }
            to { opacity: 1; }
        }
        @keyframes scaleIn {
            from { 
                transform: scale(0.8);
                opacity: 0;
            }
            to { 
                transform: scale(1);
                opacity: 1;
            }
        }
    `;
    document.head.appendChild(style);
    
    // Add to page
    overlay.appendChild(modal);
    document.body.appendChild(overlay);
    
    // Add click event to button
    document.getElementById('goHomeBtn').addEventListener('click', function() {
        window.location.href = '/';
    });
    
    // Prevent scrolling on body
    document.body.style.overflow = 'hidden';
}