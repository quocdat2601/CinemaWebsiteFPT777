
const selectedSeats = new Set();

// Get data from global variables set by the view
let coupleSeatPairs = {};

let batchCouplePairs = []; // Store pairs for batch creation

function updateSelectionUI() {
	const modeDesc = document.getElementById('modeDescription');
	const selectedCount = document.getElementById('selectedSeatCount');
	const deleteBtn = document.getElementById('deleteCoupleSeatBtn');
	const saveBtn = document.getElementById("saveChangesBtn");
	let coupleSeatSelected = false;
	let coupleSeatIds = [];

	selectedSeats.forEach(seat => {
		if (seat.hasAttribute('data-couple-seat')) {
			coupleSeatSelected = true;
			coupleSeatIds.push(seat.getAttribute('data-seat-id'));
		}
	});

	selectedCount.textContent = selectedSeats.size;

	// Check if there are any seats with data-selected-type attribute
	const hasUpdates = document.querySelectorAll('[data-selected-type]').length > 0;

	if (selectedSeats.size > 0) {
		modeDesc.textContent = `${selectedSeats.size} seats selected. Choose a type to apply.`;
		saveBtn.disabled = false; // Enable if seats are selected
	} else if (hasUpdates) {
		modeDesc.textContent = 'Seats updated. Click Save Changes to apply.';
		saveBtn.disabled = false; // Enable if there are updates
	} else {
		modeDesc.textContent = 'Select seats and choose a type';
		saveBtn.disabled = true; // Disable if no selection and no updates
	}

	// Show delete couple seat button only if exactly 2 couple seats are selected
	if (coupleSeatSelected && coupleSeatIds.length === 2) {
		deleteBtn.style.display = '';
		deleteBtn.disabled = false;
		deleteBtn.dataset.coupleSeatIds = JSON.stringify(coupleSeatIds);
	} else {
		deleteBtn.style.display = 'none';
		deleteBtn.disabled = true;
		deleteBtn.dataset.coupleSeatIds = '';
	}
}

function toggleSelectAll(checkbox) {
	const isChecked = checkbox.checked;

	// Select all seats including disabled seats
	document.querySelectorAll('.seat').forEach(seat => {
		if (isChecked) {
			seat.classList.add('selected');
			selectedSeats.add(seat);
		} else {
			seat.classList.remove('selected');
			selectedSeats.delete(seat);
		}
	});

	// Sync both select all checkboxes
	document.getElementById('selectAll').checked = isChecked;
	document.getElementById('selectAllFooter').checked = isChecked;

	// Update UI
	updateSelectionUI();
	updateCheckboxStates();
}

function toggleRow(checkbox) {
	const row = checkbox.getAttribute('data-row');
	const isChecked = checkbox.checked;

	// Select all seats in row including disabled seats
	document.querySelectorAll(`.seat[data-seat-row="${row}"]`).forEach(seat => {
		if (isChecked) {
			seat.classList.add('selected');
			selectedSeats.add(seat);
		} else {
			seat.classList.remove('selected');
			selectedSeats.delete(seat);
		}
	});

	updateSelectionUI();
	updateCheckboxStates();
}

function toggleColumn(checkbox) {
	const column = checkbox.getAttribute('data-column');
	const isChecked = checkbox.checked;

	// Select all seats in column including disabled seats
	document.querySelectorAll(`.seat[data-seat-col="${column}"]`).forEach(seat => {
		if (isChecked) {
			seat.classList.add('selected');
			selectedSeats.add(seat);
		} else {
			seat.classList.remove('selected');
			selectedSeats.delete(seat);
		}
	});

	updateSelectionUI();
	updateCheckboxStates();
}

function updateCheckboxStates() {
	// Update row checkboxes
	document.querySelectorAll('.row-checkbox').forEach(checkbox => {
		const row = checkbox.getAttribute('data-row');
		const seatsInRow = document.querySelectorAll(`.seat[data-seat-row="${row}"]`);
		const selectedInRow = document.querySelectorAll(`.seat[data-seat-row="${row}"].selected`);
		checkbox.checked = selectedInRow.length > 0 && selectedInRow.length === seatsInRow.length;
	});

	// Update all column checkboxes (headers and footers)
	document.querySelectorAll('.column-checkbox').forEach(checkbox => {
		const col = checkbox.getAttribute('data-column');
		const seatsInColumn = document.querySelectorAll(`.seat[data-seat-col="${col}"]`);
		const selectedInColumn = document.querySelectorAll(`.seat[data-seat-col="${col}"].selected`);
		checkbox.checked = selectedInColumn.length > 0 && selectedInColumn.length === seatsInColumn.length;
	});

	// Update select all checkboxes (both header and footer)
	const totalSeats = document.querySelectorAll('.seat').length;
	const selectedSeatsCount = document.querySelectorAll('.seat.selected').length;
	const isAllSelected = selectedSeatsCount > 0 && selectedSeatsCount === totalSeats;
	document.getElementById('selectAll').checked = isAllSelected;
	document.getElementById('selectAllFooter').checked = isAllSelected;
}

function selectSeat(seatElement) {
	const seatId = String(seatElement.getAttribute('data-seat-id'));
	const isCoupleSeat = seatElement.hasAttribute('data-couple-seat');

	console.log('selectSeat called for seat:', seatId, 'isCoupleSeat:', isCoupleSeat, 'isDisabled:', seatElement.classList.contains('disabled'));

	if (isCoupleSeat && coupleSeatPairs[seatId]) {
		// Select/deselect both seats in the couple
		const otherSeatId = coupleSeatPairs[seatId].toString();
		const otherSeatElement = document.querySelector(`[data-seat-id='${otherSeatId}']`);
		const isSelected = seatElement.classList.contains('selected');

		if (isSelected) {
			// Deselect both seats
			seatElement.classList.remove('selected');
			selectedSeats.delete(seatElement);
			if (otherSeatElement) {
				otherSeatElement.classList.remove('selected');
				selectedSeats.delete(otherSeatElement);
			}
		} else {
			// Select both seats (including disabled seats)
			seatElement.classList.add('selected');
			selectedSeats.add(seatElement);
			if (otherSeatElement) {
				otherSeatElement.classList.add('selected');
				selectedSeats.add(otherSeatElement);
			}
		}
	} else {
		// Normal seat selection (works for all seats including disabled)
		if (selectedSeats.has(seatElement)) {
			seatElement.classList.remove('selected');
			selectedSeats.delete(seatElement);
		} else {
			seatElement.classList.add('selected');
			selectedSeats.add(seatElement);
		}
	}

	updateSelectionUI();
	updateCheckboxStates();
}

function updateDisabledSeatCount() {
	const disabledCount = document.querySelectorAll('.seat.disabled').length;
	document.getElementById('disabledSeatCount').textContent = disabledCount;
}

function isContiguousSelection(seatElements) {
	// Get seat positions from the selected elements
	const seatPositions = seatElements.map(seat => ({
		row: parseInt(seat.getAttribute('data-seat-row')),
		col: parseInt(seat.getAttribute('data-seat-col'))
	}));
	
	if (seatPositions.length < 2) return false;

	// Sort by row first, then by column
	seatPositions.sort((a, b) => {
		if (a.row !== b.row) return a.row - b.row;
		return a.col - b.col;
	});

	// Check if seats are in the same row and adjacent columns
	const sameRow = seatPositions.every((pos, index) => {
		if (index === 0) return true;
		return pos.row === seatPositions[index - 1].row && 
			   pos.col === seatPositions[index - 1].col + 1;
	});

	if (sameRow) return true;

	// Check if seats are in adjacent rows and same column
	const sameColumn = seatPositions.every((pos, index) => {
		if (index === 0) return true;
		return pos.col === seatPositions[index - 1].col && 
			   pos.row === seatPositions[index - 1].row + 1;
	});

	return sameColumn;
}

function applySeatType(clickedTypeDiv) {
	const color = clickedTypeDiv.getAttribute('data-color');
	const typeName = clickedTypeDiv.getAttribute('data-type-name');
	const typeId = clickedTypeDiv.getAttribute('data-type');

	console.log('Applying seat type:', typeName, 'to', selectedSeats.size, 'seats');

	if (selectedSeats.size === 0) {
		alert('Please select at least one seat first.');
		return;
	}

	if (typeName === 'Couple') {
		const seatArray = Array.from(selectedSeats);
		if (seatArray.length % 2 !== 0) {
			alert('Please select an even number of seats to create couple seats.');
			return;
		}
		if (!isContiguousSelection(seatArray)) {
			alert('Please select seats in a contiguous order (e.g., 1 2 3 4 or 4 3 2 1).');
			return;
		}
		// Pair up seats
		batchCouplePairs = [];
		for (let i = 0; i < seatArray.length; i += 2) {
			const firstId = parseInt(seatArray[i].getAttribute('data-seat-id'));
			const secondId = parseInt(seatArray[i + 1].getAttribute('data-seat-id'));
			batchCouplePairs.push({ FirstSeatId: firstId, SecondSeatId: secondId });
		}
		// Set type visually
		selectedSeats.forEach(seat => {
			seat.style.backgroundColor = color;
			seat.setAttribute('data-selected-type', typeId);
			seat.classList.remove('selected');
		});

		selectedSeats.clear();
		document.querySelectorAll('.seat.selected').forEach(seat => seat.classList.remove('selected'));

		// Enable save button
		document.getElementById("saveChangesBtn").disabled = false;
		document.getElementById('modeDescription').textContent = 'Seats updated. Click Save Changes to apply.';
		updateSelectionUI();
		return;
	}

	// Apply type to selected seats (including disabled seats)
	selectedSeats.forEach(seat => {
		seat.style.backgroundColor = color;
		seat.setAttribute('data-selected-type', typeId);
		seat.classList.remove('selected');
	});

	selectedSeats.clear();
	document.querySelectorAll('.seat.selected').forEach(seat => seat.classList.remove('selected'));

	// Enable save button
	document.getElementById("saveChangesBtn").disabled = false;
	document.getElementById('modeDescription').textContent = 'Seats updated. Click Save Changes to apply.';
	updateSelectionUI();
}

// Function to clear all updates (for testing)
function clearAllUpdates() {
	document.querySelectorAll('[data-selected-type]').forEach(seat => {
		seat.removeAttribute('data-selected-type');
		// Reset to original color based on current type
		const currentType = seat.getAttribute('data-seat-type');
		const typeElement = document.querySelector(`[data-type-name="${currentType}"]`);
		if (typeElement) {
			seat.style.backgroundColor = typeElement.getAttribute('data-color');
		}
	});
	document.getElementById("saveChangesBtn").disabled = true;
	document.getElementById('modeDescription').textContent = 'Select seats and choose a type';
	updateSelectionUI();
}

function submitUpdatedSeats() {
	const updatedSeats = [];
	let isCoupleSeat = false;
	let coupleTypeId = null;
	let wasCoupleSeat = false;
	let coupleSeatIds = [];

	document.querySelectorAll('[data-selected-type]').forEach(seat => {
		const typeName = document.querySelector(`[data-type="${seat.getAttribute('data-selected-type')}"]`).getAttribute('data-type-name');
		const seatId = seat.getAttribute('data-seat-id');

		// Check if this seat was part of a couple seat
		if (seat.hasAttribute('data-couple-seat')) {
			wasCoupleSeat = true;
			coupleSeatIds.push(parseInt(seatId));
		}

		if (typeName === 'Couple') {
			isCoupleSeat = true;
			coupleTypeId = seat.getAttribute('data-selected-type');
		}
		updatedSeats.push({
			SeatId: seatId,
			NewSeatTypeId: seat.getAttribute('data-selected-type')
		});
	});

	// Batch couple seat creation logic
	if (batchCouplePairs.length > 0) {
		fetch('/Seat/CreateCoupleSeatsBatch', {
			method: 'POST',
			headers: {
				'Content-Type': 'application/json',
				'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
			},
			body: JSON.stringify(batchCouplePairs)
		})
			.then(response => {
				if (!response.ok) throw new Error('Failed to create couple seats');
				// After couple seats are created, update seat types
				return fetch('/Seat/UpdateSeatTypes', {
					method: 'POST',
					headers: {
						'Content-Type': 'application/json',
						'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
					},
					body: JSON.stringify(updatedSeats)
				});
			})
			.then(response => {
				if (response.ok) {
					alert('Couple seats created and seats updated successfully.');
					location.reload();
				} else {
					throw new Error('Failed to update seat types');
				}
			})
			.catch(error => {
				alert('Error: ' + error.message);
			});
		return;
	}

	// If seats were part of a couple seat but are being changed to a different type
	if (wasCoupleSeat && !isCoupleSeat) {
		// First delete the couple seat relationship
		fetch('/Seat/DeleteCoupleSeat', {
			method: 'POST',
			headers: {
				'Content-Type': 'application/json',
				'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
			},
			body: JSON.stringify({ seatIds: coupleSeatIds })
		})
			.then(response => {
				if (response.ok) {
					// After couple seat is deleted, update the seat types
					return fetch('/Seat/UpdateSeatTypes', {
						method: 'POST',
						headers: {
							'Content-Type': 'application/json',
							'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
						},
						body: JSON.stringify(updatedSeats)
					});
				} else {
					throw new Error('Failed to delete couple seat');
				}
			})
			.then(response => {
				if (response.ok) {
					alert('Couple seat removed and seats updated successfully.');
					location.reload();
				} else {
					throw new Error('Failed to update seat types');
				}
			})
			.catch(error => {
				alert('Error: ' + error.message);
			});
	}
	else if (isCoupleSeat && updatedSeats.length === 2) {
		// First create the couple seat
		const coupleSeat = {
			FirstSeatId: parseInt(updatedSeats[0].SeatId),
			SecondSeatId: parseInt(updatedSeats[1].SeatId)
		};

		fetch('/Seat/CreateCoupleSeat', {
			method: 'POST',
			headers: {
				'Content-Type': 'application/json',
				'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
			},
			body: JSON.stringify(coupleSeat)
		})
			.then(response => {
				if (response.ok) {
					// After couple seat is created, update the seat types
					return fetch('/Seat/UpdateSeatTypes', {
						method: 'POST',
						headers: {
							'Content-Type': 'application/json',
							'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
						},
						body: JSON.stringify(updatedSeats)
					});
				} else {
					throw new Error('Failed to create couple seat');
				}
			})
			.then(response => {
				if (response.ok) {
					alert('Couple seat created and seats updated successfully.');
					location.reload();
				} else {
					throw new Error('Failed to update seat types');
				}
			})
			.catch(error => {
				alert('Error: ' + error.message);
			});
	} else {
		// Handle regular seat type updates
		fetch('/Seat/UpdateSeatTypes', {
			method: 'POST',
			headers: {
				'Content-Type': 'application/json',
				'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
			},
			body: JSON.stringify(updatedSeats)
		})
			.then(response => {
				if (response.ok) {
					alert('Seat types updated successfully.');
					location.reload();
				} else {
					alert('Failed to update seat types.');
				}
			});
	}
}

function deleteSelectedCoupleSeat(event) {
	event.preventDefault();
	const deleteBtn = document.getElementById('deleteCoupleSeatBtn');
	const coupleSeatIds = JSON.parse(deleteBtn.dataset.coupleSeatIds || '[]');
	if (coupleSeatIds.length !== 2) {
		alert('Please select exactly two couple seats to delete.');
		return;
	}

	// Step 1: Delete the couple seat relationship
	fetch('/Seat/DeleteCoupleSeat', {
		method: 'POST',
		headers: {
			'Content-Type': 'application/json',
			'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
		},
		body: JSON.stringify({ seatIds: coupleSeatIds })
	})
		.then(response => {
			if (!response.ok) throw new Error('Failed to delete couple seat');
			// Step 2: Set both seats to type "Normal"
			// Find the typeId for "Normal"
			const normalTypeDiv = document.querySelector('.seat-type-option[data-type-name="Normal"]');
			if (!normalTypeDiv) throw new Error('Normal seat type not found');
			const normalTypeId = normalTypeDiv.getAttribute('data-type');
			const updates = coupleSeatIds.map(seatId => ({
				SeatId: seatId,
				NewSeatTypeId: normalTypeId
			}));
			return fetch('/Seat/UpdateSeatTypes', {
				method: 'POST',
				headers: {
					'Content-Type': 'application/json',
					'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
				},
				body: JSON.stringify(updates)
			});
		})
		.then(response => {
			if (!response.ok) throw new Error('Failed to update seat types');
			alert('Couple seat deleted and seats reverted to Normal.');
			location.reload();
		})
		.catch(error => {
			alert('Error: ' + error.message);
		});
}

// Initialize on page load
document.addEventListener('DOMContentLoaded', function () {
	// Initialize coupleSeatPairs from global variable
	coupleSeatPairs = window.coupleSeatPairs || {};
	updateDisabledSeatCount();
	updateSelectionUI();
});