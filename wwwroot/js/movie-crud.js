document.addEventListener('DOMContentLoaded', function() {
    // Get data from global variables set by the view
    let selectedDirectors = window.selectedDirectors || [];
    let selectedActors = window.selectedActors || [];
    let allDirectors = [];
    let allActors = [];

    // Initialize selections from model (for Edit page)
    function initializeSelections() {
        const directorIds = window.initialDirectorIds || '';
        const actorIds = window.initialActorIds || '';
        
        if (directorIds) {
            selectedDirectors = directorIds.split(',').filter(id => id.trim() !== '');
        }
        
        if (actorIds) {
            selectedActors = actorIds.split(',').filter(id => id.trim() !== '');
        }
        
        // Ensure they are arrays
        if (!Array.isArray(selectedDirectors)) {
            selectedDirectors = [];
        }
        if (!Array.isArray(selectedActors)) {
            selectedActors = [];
        }
        
        updateDirectorSelection();
        updateActorSelection();
    }

    // Image preview function
    function previewImage(input, previewId) {
        const preview = document.getElementById(previewId);
        if (input.files && input.files[0]) {
            const reader = new FileReader();
            reader.onload = function (e) {
                preview.src = e.target.result;
                preview.style.display = "block";
            }
            reader.readAsDataURL(input.files[0]);
        } else {
            preview.src = "#";
            preview.style.display = "none";
        }
    }

    // Director/Actor modal functions
    function openDirectorPopup() {
        const modalElement = document.getElementById('directorModal');
        if (!modalElement) {
            console.error('Director modal element not found');
            alert('Director modal not found. Please refresh the page.');
            return;
        }
        
        fetch('/Movie/GetDirectors')
            .then(response => {
                if (!response.ok) {
                    throw new Error(`HTTP error! status: ${response.status}`);
                }
                return response.json();
            })
            .then(data => {
                allDirectors = data;
                renderDirectors(data);
                const modal = new bootstrap.Modal(modalElement);
                modal.show();
            })
            .catch(error => {
                console.error('Error loading directors:', error);
                alert('Error loading directors. Please try again.');
            });
    }

    function openActorPopup() {
        const modalElement = document.getElementById('actorModal');
        if (!modalElement) {
            console.error('Actor modal element not found');
            alert('Actor modal not found. Please refresh the page.');
            return;
        }
        
        fetch('/Movie/GetActors')
            .then(response => {
                if (!response.ok) {
                    throw new Error(`HTTP error! status: ${response.status}`);
                }
                return response.json();
            })
            .then(data => {
                allActors = data;
                renderActors(data);
                const modal = new bootstrap.Modal(modalElement);
                modal.show();
            })
            .catch(error => {
                console.error('Error loading actors:', error);
                alert('Error loading actors. Please try again.');
            });
    }

    function renderDirectors(directors) {
        const directorsList = document.getElementById('directorsList');
        if (!directorsList) {
            console.error('Directors list element not found');
            return;
        }
        
        directorsList.innerHTML = '';
        
        directors.forEach(director => {
            const directorCard = `
                <div class="col-md-3 mb-3">
                    <div class="card director-card" data-id="${director.id}" data-name="${director.name.toLowerCase()}" onclick="toggleDirector(${director.id}, '${director.name}')">
                        <div class="selection-overlay">
                            <i class="fas fa-check-circle"></i>
                        </div>
                        <img src="${director.image || '/images/movies/default-movie.jpg'}" class="card-img-top" alt="${director.name}">
                        <div class="card-body">
                            <h6 class="card-title">${director.name}</h6>
                        </div>
                    </div>
                </div>
            `;
            directorsList.innerHTML += directorCard;
        });

        // Restore previously selected states
        if (Array.isArray(selectedDirectors)) {
            selectedDirectors.forEach(id => {
                const card = document.querySelector(`.director-card[data-id="${id}"]`);
                if (card) card.classList.add('selected');
            });
        }
    }

    function renderActors(actors) {
        const actorsList = document.getElementById('actorsList');
        if (!actorsList) {
            console.error('Actors list element not found');
            return;
        }
        
        actorsList.innerHTML = '';
        
        actors.forEach(actor => {
            const actorCard = `
                <div class="col-md-3 mb-3">
                    <div class="card actor-card" data-id="${actor.id}" data-name="${actor.name.toLowerCase()}" onclick="toggleActor(${actor.id}, '${actor.name}')">
                        <div class="selection-overlay">
                            <i class="fas fa-check-circle"></i>
                        </div>
                        <img src="${actor.image || '/images/movies/default-movie.jpg'}" class="card-img-top" alt="${actor.name}">
                        <div class="card-body">
                            <h6 class="card-title">${actor.name}</h6>
                        </div>
                    </div>
                </div>
            `;
            actorsList.innerHTML += actorCard;
        });

        // Restore previously selected states
        if (Array.isArray(selectedActors)) {
            selectedActors.forEach(id => {
                const card = document.querySelector(`.actor-card[data-id="${id}"]`);
                if (card) card.classList.add('selected');
            });
        }
    }

    function toggleDirector(id, name) {
        const index = selectedDirectors.indexOf(id.toString());
        const card = document.querySelector(`.director-card[data-id="${id}"]`);
        
        if (index === -1) {
            selectedDirectors.push(id.toString());
            card.classList.add('selected');
        } else {
            selectedDirectors.splice(index, 1);
            card.classList.remove('selected');
        }
        
        updateDirectorSelection();
    }

    function toggleActor(id, name) {
        const index = selectedActors.indexOf(id.toString());
        const card = document.querySelector(`.actor-card[data-id="${id}"]`);
        
        if (index === -1) {
            selectedActors.push(id.toString());
            card.classList.add('selected');
        } else {
            selectedActors.splice(index, 1);
            card.classList.remove('selected');
        }
        
        updateActorSelection();
    }

    function updateDirectorSelection() {
        const directorIdsInput = document.getElementById('selectedDirectorIds');
        const directorsDisplay = document.getElementById('selectedDirectors');
        
        if (directorIdsInput) directorIdsInput.value = selectedDirectors.join(',');
        if (directorsDisplay) directorsDisplay.value = selectedDirectors.length + ' director(s) selected';
    }

    function updateActorSelection() {
        const actorIdsInput = document.getElementById('selectedActorIds');
        const actorsDisplay = document.getElementById('selectedActors');
        
        if (actorIdsInput) actorIdsInput.value = selectedActors.join(',');
        if (actorsDisplay) actorsDisplay.value = selectedActors.length + ' actor(s) selected';
    }

    function filterDirectors() {
        const searchTerm = document.getElementById('directorSearch').value.toLowerCase();
        const filteredDirectors = allDirectors.filter(director => 
            director.name.toLowerCase().includes(searchTerm)
        );
        renderDirectors(filteredDirectors);
    }

    function filterActors() {
        const searchTerm = document.getElementById('actorSearch').value.toLowerCase();
        const filteredActors = allActors.filter(actor => 
            actor.name.toLowerCase().includes(searchTerm)
        );
        renderActors(filteredActors);
    }

    function confirmDirectorSelection() {
        const modal = bootstrap.Modal.getInstance(document.getElementById('directorModal'));
        modal.hide();
    }

    function confirmActorSelection() {
        const modal = bootstrap.Modal.getInstance(document.getElementById('actorModal'));
        modal.hide();
    }

    // Make functions globally available
    window.previewImage = previewImage;
    window.openDirectorPopup = openDirectorPopup;
    window.openActorPopup = openActorPopup;
    window.toggleDirector = toggleDirector;
    window.toggleActor = toggleActor;
    window.filterDirectors = filterDirectors;
    window.filterActors = filterActors;
    window.confirmDirectorSelection = confirmDirectorSelection;
    window.confirmActorSelection = confirmActorSelection;

    // Initialize selections
    initializeSelections();
});
