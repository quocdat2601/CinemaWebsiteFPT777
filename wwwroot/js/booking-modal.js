// BOOKING MODAL JAVASCRIPT
import bootstrap from "bootstrap"

class BookingModalManager {
  constructor() {
    this.currentMovieShows = []
    this.movieShowsCache = new Map()
    this.availableDates = new Set()
    this.selectedMovie = null
    this.init()
  }

  init() {
    this.bindEvents()
    this.initializeModal()
  }

  bindEvents() {
    // Modal event listeners
    const bookingModal = document.getElementById("addBookingModal")
    if (bookingModal) {
      bookingModal.addEventListener("shown.bs.modal", () => {
        this.onModalShown()
      })

      bookingModal.addEventListener("hidden.bs.modal", () => {
        this.onModalHidden()
      })
    }

    // Form element event listeners
    const dateSelect = document.getElementById("dateSelect")
    const versionSelect = document.getElementById("versionSelect")
    const timeSelect = document.getElementById("timeSelect")
    const continueBtn = document.getElementById("bookBtn")

    if (dateSelect) {
      dateSelect.addEventListener("change", () => {
        this.updateVersions()
        this.updateStepUI()
      })
    }

    if (versionSelect) {
      versionSelect.addEventListener("change", () => {
        this.updateTimes()
        this.updateStepUI()
      })
    }

    if (timeSelect) {
      timeSelect.addEventListener("change", () => {
        this.validateForm()
        this.updateStepUI()
      })
    }

    if (continueBtn) {
      continueBtn.addEventListener("click", () => this.continueToSeats())
    }
  }

  initializeModal() {
    // Set up initial modal state
    this.resetForm()
  }

  openBookingModal(movieId, movieName) {
    console.log("Opening booking modal for movie ID:", movieId)

    this.selectedMovie = { id: movieId, name: movieName }

    // Set the movie ID in the hidden input
    const movieIdInput = document.getElementById("movieId")
    if (movieIdInput) {
      movieIdInput.value = movieId
    }

    // Update modal title with movie name
    const modalTitle = document.getElementById("addBookingModalLabel")
    if (modalTitle && movieName) {
      modalTitle.innerHTML = `<i class="fas fa-ticket-alt"></i> Book Tickets for "${movieName}"`
    }

    // Update the movie name in the modal body
    const movieNameElem = document.getElementById("selectedMovieName")
    if (movieNameElem && movieName) {
      movieNameElem.textContent = movieName
    }

    // Reset form and load data
    this.resetForm()
    this.loadMovieShowsForMovie(movieId, movieName)

    // Show modal
    const bookingModal = new bootstrap.Modal(document.getElementById("addBookingModal"))
    bookingModal.show()
  }

  loadMovieShowsForMovie(movieId, movieName) {
    console.log("Loading movie shows for movie ID:", movieId)

    // Show loading state
    this.setLoadingState(true)

    // Check cache first
    if (this.movieShowsCache.has(movieId)) {
      console.log("Using cached movie shows for movie ID:", movieId)
      this.currentMovieShows = this.movieShowsCache.get(movieId)
      this.populateDateDropdown()
      this.setLoadingState(false)
      return
    }

    // Fetch movie shows
    fetch(`/Movie/GetMovieShows?movieId=${movieId}`, {
      headers: {
        "X-Requested-With": "XMLHttpRequest",
      },
    })
      .then((response) => {
        if (!response.ok) throw new Error("Failed to load movie shows")
        return response.json()
      })
      .then((shows) => {
        console.log("Movie shows loaded for movie ID:", movieId, shows)

        // Cache the results
        this.movieShowsCache.set(movieId, shows)
        this.currentMovieShows = shows
        this.populateDateDropdown()
        this.setLoadingState(false)
      })
      .catch((error) => {
        console.error("Error loading movie shows:", error)
        this.setErrorState("Failed to load showtimes. Please try again.")
      })
  }

  populateDateDropdown() {
    const dateSelect = document.getElementById("dateSelect")
    if (!dateSelect) return

    dateSelect.innerHTML = '<option value="">Choose your preferred date</option>'
    dateSelect.disabled = true

    if (!this.currentMovieShows || this.currentMovieShows.length === 0) {
      dateSelect.innerHTML = '<option value="">No showtimes available</option>'
      return
    }

    const today = new Date()
    today.setHours(0, 0, 0, 0)

    const uniqueDates = [...new Set(this.currentMovieShows.map((show) => show.showDate))]
    const validDates = uniqueDates.filter((dateStr) => {
      const showDate = new Date(dateStr)
      showDate.setHours(0, 0, 0, 0)
      return showDate >= today
    })

    if (validDates.length === 0) {
      dateSelect.innerHTML = '<option value="">No upcoming showtimes</option>'
      return
    }

    validDates.forEach((dateStr) => {
      const [year, month, day] = dateStr.split("-")
      const displayDate = this.formatDate(new Date(dateStr))
      const option = document.createElement("option")
      option.value = dateStr
      option.textContent = displayDate
      dateSelect.appendChild(option)
    })

    dateSelect.disabled = false
    this.addSelectAnimation(dateSelect)
  }

  updateVersions() {
    const date = document.getElementById("dateSelect").value
    const versionSelect = document.getElementById("versionSelect")
    const timeSelect = document.getElementById("timeSelect")

    if (!versionSelect || !timeSelect) return

    // Reset dependent dropdowns
    versionSelect.innerHTML = '<option value="">Choose movie version</option>'
    versionSelect.disabled = true
    timeSelect.innerHTML = '<option value="">Choose showtime</option>'
    timeSelect.disabled = true

    if (!date) return

    const filtered = this.currentMovieShows.filter((show) => show.showDate === date)

    // Get unique versions
    const versionMap = new Map()
    filtered.forEach((show) => {
      if (show.versionId && show.versionName && !versionMap.has(show.versionId)) {
        versionMap.set(show.versionId, show.versionName)
      }
    })

    if (versionMap.size === 0) {
      versionSelect.innerHTML = '<option value="">No versions available</option>'
      return
    }

    versionMap.forEach((name, id) => {
      const option = document.createElement("option")
      option.value = id
      option.textContent = name
      versionSelect.appendChild(option)
    })

    versionSelect.disabled = false
    this.addSelectAnimation(versionSelect)
  }

  updateTimes() {
    const date = document.getElementById("dateSelect").value
    const versionId = document.getElementById("versionSelect").value
    const timeSelect = document.getElementById("timeSelect")

    if (!timeSelect) return

    timeSelect.innerHTML = '<option value="">Choose showtime</option>'
    timeSelect.disabled = true

    if (!date || !versionId) return

    const filtered = this.currentMovieShows.filter(
      (show) => show.showDate === date && String(show.versionId) === String(versionId),
    )

    // Get unique times
    const timeSet = new Set()
    filtered.forEach((show) => {
      if (show.scheduleTime && !timeSet.has(show.scheduleTime)) {
        timeSet.add(show.scheduleTime)
      }
    })

    if (timeSet.size === 0) {
      timeSelect.innerHTML = '<option value="">No showtimes available</option>'
      return
    }

    // Sort times
    const sortedTimes = Array.from(timeSet).sort()

    sortedTimes.forEach((time) => {
      const option = document.createElement("option")
      option.value = time
      option.textContent = this.formatTime(time)
      timeSelect.appendChild(option)
    })

    timeSelect.disabled = false
    this.addSelectAnimation(timeSelect)
    this.validateForm()
  }

  validateForm() {
    const movieId = document.getElementById("movieId").value
    const date = document.getElementById("dateSelect").value
    const versionId = document.getElementById("versionSelect").value
    const time = document.getElementById("timeSelect").value
    const continueBtn = document.getElementById("bookBtn")

    // Step UI update
    this.updateStepUI()

    if (!continueBtn) return

    const isValid = movieId && date && versionId && time

    continueBtn.disabled = !isValid
    continueBtn.classList.toggle("disabled", !isValid)

    if (isValid) {
      continueBtn.classList.add("ready")
      this.addButtonPulse(continueBtn)
    } else {
      continueBtn.classList.remove("ready")
    }
  }

  updateStepUI() {
    // Highlight/select step, show helper text
    const dateSelect = document.getElementById("dateSelect")
    const versionSelect = document.getElementById("versionSelect")
    const timeSelect = document.getElementById("timeSelect")
    
    // Get labels and containers
    const dateLabel = dateSelect?.parentNode?.querySelector('.form-label')
    const versionLabel = versionSelect?.parentNode?.querySelector('.form-label')
    const timeLabel = timeSelect?.parentNode?.querySelector('.form-label')
    
    const dateContainer = dateSelect?.parentNode
    const versionContainer = versionSelect?.parentNode
    const timeContainer = timeSelect?.parentNode
    
    // Helper elements
    let dateHelper = document.getElementById("dateStepHelper")
    let versionHelper = document.getElementById("versionStepHelper")
    let timeHelper = document.getElementById("timeStepHelper")
    
    if (!dateHelper) {
      dateHelper = document.createElement("div")
      dateHelper.id = "dateStepHelper"
      dateHelper.className = "step-helper-text"
      dateSelect.parentNode.appendChild(dateHelper)
    }
    if (!versionHelper) {
      versionHelper = document.createElement("div")
      versionHelper.id = "versionStepHelper"
      versionHelper.className = "step-helper-text"
      versionSelect.parentNode.appendChild(versionHelper)
    }
    if (!timeHelper) {
      timeHelper = document.createElement("div")
      timeHelper.id = "timeStepHelper"
      timeHelper.className = "step-helper-text"
      timeSelect.parentNode.appendChild(timeHelper)
    }
    
    // Reset all states first
    [dateSelect, versionSelect, timeSelect].forEach(select => {
      if (select) {
        select.classList.remove("step-active")
      }
    })
    
    [dateLabel, versionLabel, timeLabel].forEach(label => {
      if (label) {
        label.classList.remove("active-step", "inactive-step")
      }
    })
    
    [dateContainer, versionContainer, timeContainer].forEach(container => {
      if (container) {
        container.classList.remove("active", "inactive")
      }
    })
    
    // Step 1: Date
    if (!dateSelect.value) {
      dateSelect.classList.add("step-active")
      dateLabel?.classList.add("active-step")
      dateContainer?.classList.add("active")
      versionLabel?.classList.add("inactive-step")
      timeLabel?.classList.add("inactive-step")
      versionContainer?.classList.add("inactive")
      timeContainer?.classList.add("inactive")
      
      dateHelper.textContent = "Please select a date to continue."
      versionHelper.textContent = ""
      timeHelper.textContent = ""
      
      versionSelect.disabled = true
      timeSelect.disabled = true
    } else if (!versionSelect.value) {
      // Step 2: Version
      versionSelect.classList.add("step-active")
      versionLabel?.classList.add("active-step")
      versionContainer?.classList.add("active")
      timeLabel?.classList.add("inactive-step")
      timeContainer?.classList.add("inactive")
      
      dateHelper.textContent = ""
      versionHelper.textContent = "Please select a version to continue."
      timeHelper.textContent = ""
      
      versionSelect.disabled = false
      timeSelect.disabled = true
    } else if (!timeSelect.value) {
      // Step 3: Time
      timeSelect.classList.add("step-active")
      timeLabel?.classList.add("active-step")
      timeContainer?.classList.add("active")
      
      dateHelper.textContent = ""
      versionHelper.textContent = ""
      timeHelper.textContent = "Please select a showtime to continue."
      
      versionSelect.disabled = false
      timeSelect.disabled = false
    } else {
      // All selected - success state
      dateHelper.textContent = ""
      versionHelper.textContent = ""
      timeHelper.textContent = "All selections complete! You can now continue."
      timeHelper.classList.add("success")
      
      versionSelect.disabled = false
      timeSelect.disabled = false
    }
  }

  continueToSeats() {
    const movieId = document.getElementById("movieId").value
    const date = document.getElementById("dateSelect").value
    const versionId = document.getElementById("versionSelect").value
    const time = document.getElementById("timeSelect").value

    if (!movieId || !date || !versionId || !time) {
      this.showAlert("Please select all options", "warning")
      return
    }

    // Show loading state on button
    const continueBtn = document.getElementById("bookBtn")
    const originalText = continueBtn.innerHTML
    continueBtn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Loading...'
    continueBtn.disabled = true

    // Format date for URL
    const [year, month, day] = date.split("-")
    const formattedDate = `${day}/${month}/${year}`

    // Navigate to seat selection
    setTimeout(() => {
      window.location.href = `/Seat/Select?movieId=${movieId}&date=${formattedDate}&versionId=${encodeURIComponent(versionId)}&time=${encodeURIComponent(time)}`
    }, 1000)
  }

  resetForm() {
    const dateSelect = document.getElementById("dateSelect")
    const versionSelect = document.getElementById("versionSelect")
    const timeSelect = document.getElementById("timeSelect")
    const continueBtn = document.getElementById("bookBtn")

    if (dateSelect) {
      dateSelect.innerHTML = '<option value="">Loading dates...</option>'
      dateSelect.disabled = true
    }

    if (versionSelect) {
      versionSelect.innerHTML = '<option value="">Choose movie version</option>'
      versionSelect.disabled = true
    }

    if (timeSelect) {
      timeSelect.innerHTML = '<option value="">Choose showtime</option>'
      timeSelect.disabled = true
    }

    if (continueBtn) {
      continueBtn.innerHTML = '<span>Continue to Seat Selection</span><i class="fas fa-arrow-right"></i>'
      continueBtn.disabled = true
      continueBtn.classList.remove("ready", "disabled")
    }
  }

  setLoadingState(isLoading) {
    const dateSelect = document.getElementById("dateSelect")
    if (!dateSelect) return

    if (isLoading) {
      dateSelect.innerHTML = '<option value="">Loading dates...</option>'
      dateSelect.disabled = true
      dateSelect.classList.add("loading")
    } else {
      dateSelect.classList.remove("loading")
    }
  }

  setErrorState(message) {
    const dateSelect = document.getElementById("dateSelect")
    if (dateSelect) {
      dateSelect.innerHTML = `<option value="">${message}</option>`
      dateSelect.disabled = true
      dateSelect.classList.add("error")
    }
    this.showAlert(message, "error")
  }

  onModalShown() {
    // Focus on date select when modal opens
    const dateSelect = document.getElementById("dateSelect")
    if (dateSelect && !dateSelect.disabled) {
      dateSelect.focus()
    }

    // Add modal entrance animation
    const modalContent = document.querySelector(".modern-booking-modal")
    if (modalContent) {
      modalContent.style.transform = "scale(0.9)"
      modalContent.style.opacity = "0"
      setTimeout(() => {
        modalContent.style.transform = "scale(1)"
        modalContent.style.opacity = "1"
        modalContent.style.transition = "all 0.3s ease"
      }, 50)
    }

    // Initialize step UI after a short delay
    setTimeout(() => {
      this.updateStepUI()
    }, 200)
  }

  onModalHidden() {
    // Clean up when modal is closed
    this.resetForm()
    this.selectedMovie = null

    // Clear any alerts
    this.clearAlerts()
  }

  // Utility methods
  formatDate(date) {
    const today = new Date()
    const tomorrow = new Date(today)
    tomorrow.setDate(tomorrow.getDate() + 1)

    if (date.toDateString() === today.toDateString()) {
      return "Today"
    } else if (date.toDateString() === tomorrow.toDateString()) {
      return "Tomorrow"
    } else {
      return date.toLocaleDateString("en-US", {
        weekday: "short",
        month: "short",
        day: "numeric",
      })
    }
  }

  formatTime(timeString) {
    // Convert 24-hour format to 12-hour format
    const [hours, minutes] = timeString.split(":")
    const hour = Number.parseInt(hours)
    const ampm = hour >= 12 ? "PM" : "AM"
    const displayHour = hour % 12 || 12
    return `${displayHour}:${minutes} ${ampm}`
  }

  addSelectAnimation(selectElement) {
    selectElement.style.transform = "scale(1.02)"
    selectElement.style.boxShadow = "0 0 0 3px rgba(102, 126, 234, 0.2)"
    setTimeout(() => {
      selectElement.style.transform = ""
      selectElement.style.boxShadow = ""
      selectElement.style.transition = "all 0.3s ease"
    }, 200)
  }

  addButtonPulse(button) {
    button.classList.add("pulse")
    setTimeout(() => {
      button.classList.remove("pulse")
    }, 1000)
  }

  showAlert(message, type = "info") {
    // Remove existing alerts
    this.clearAlerts()

    const alert = document.createElement("div")
    alert.className = `booking-alert booking-alert-${type}`
    alert.innerHTML = `
            <div class="alert-content">
                <i class="fas fa-${this.getAlertIcon(type)}"></i>
                <span>${message}</span>
            </div>
            <button class="alert-close" onclick="this.parentNode.remove()">
                <i class="fas fa-times"></i>
            </button>
        `

    const modalBody = document.querySelector(".modern-booking-body")
    if (modalBody) {
      modalBody.insertBefore(alert, modalBody.firstChild)

      // Auto-remove after 5 seconds
      setTimeout(() => {
        if (alert.parentNode) {
          alert.remove()
        }
      }, 5000)
    }
  }

  clearAlerts() {
    document.querySelectorAll(".booking-alert").forEach((alert) => {
      alert.remove()
    })
  }

  getAlertIcon(type) {
    const icons = {
      info: "info-circle",
      success: "check-circle",
      warning: "exclamation-triangle",
      error: "exclamation-circle",
    }
    return icons[type] || "info-circle"
  }

  // Public methods
  getSelectedBookingData() {
    return {
      movieId: document.getElementById("movieId").value,
      date: document.getElementById("dateSelect").value,
      versionId: document.getElementById("versionSelect").value,
      time: document.getElementById("timeSelect").value,
      movieName: this.selectedMovie?.name,
    }
  }

  isFormValid() {
    const data = this.getSelectedBookingData()
    return data.movieId && data.date && data.versionId && data.time
  }
}

// Global functions for backward compatibility
window.openBookingModal = (movieId, movieName) => {
  if (window.bookingModalManager) {
    window.bookingModalManager.openBookingModal(movieId, movieName)
  }
}

window.openBookingModalFromHero = () => {
  // Get current hero movie data
  const heroInfo = document.getElementById("hero-info")
  if (heroInfo) {
    const movieId = document.getElementById("movieId")?.value
    const movieName =
      heroInfo.querySelector(".hero-title")?.textContent || heroInfo.querySelector(".hero-movie-logo")?.alt

    if (movieId && movieName) {
      window.openBookingModal(movieId, movieName)
    }
  }
}

window.updateVersions = () => {
  if (window.bookingModalManager) {
    window.bookingModalManager.updateVersions()
  }
}

window.updateTimes = () => {
  if (window.bookingModalManager) {
    window.bookingModalManager.updateTimes()
  }
}

window.continueToSeats = () => {
  if (window.bookingModalManager) {
    window.bookingModalManager.continueToSeats()
  }
}

// Initialize when DOM is ready
document.addEventListener("DOMContentLoaded", () => {
  window.bookingModalManager = new BookingModalManager()
})

// Export for module systems
if (typeof module !== "undefined" && module.exports) {
  module.exports = BookingModalManager
}
