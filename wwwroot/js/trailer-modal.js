// TRAILER MODAL JAVASCRIPT
// Import Bootstrap and gtag if they are not already imported
import { Modal } from "bootstrap"
window.gtag =
  window.gtag ||
  (() => {
    /* gtag placeholder */
  })

class TrailerModalManager {
  constructor() {
    this.modal = null
    this.iframe = null
    this.init()
  }

  init() {
    this.modal = document.getElementById("trailerModal")
    this.iframe = document.getElementById("trailerFrame")
    this.bindEvents()
  }

  bindEvents() {
    if (!this.modal) return

    // Modal events
    this.modal.addEventListener("shown.bs.modal", () => {
      this.onModalShown()
    })

    this.modal.addEventListener("hidden.bs.modal", () => {
      this.onModalHidden()
    })

    // Trailer button events
    document.addEventListener("click", (e) => {
      if (e.target.matches(".watch-trailer-btn, .btn-trailer, .play-trailer-btn, .coming-soon-trailer")) {
        e.preventDefault()
        const trailerUrl =
          e.target.getAttribute("data-trailer-url") ||
          e.target.closest("[data-trailer-url]")?.getAttribute("data-trailer-url")
        if (trailerUrl) {
          this.openTrailer(trailerUrl)
        }
      }
    })

    // Keyboard events
    document.addEventListener("keydown", (e) => {
      if (e.key === "Escape" && this.modal.classList.contains("show")) {
        this.closeTrailer()
      }
    })
  }

  openTrailer(trailerUrl) {
    if (!this.modal || !this.iframe) return

    // Convert YouTube watch URLs to embed URLs
    const embedUrl = this.convertToEmbedUrl(trailerUrl)

    // Set iframe source
    this.iframe.src = embedUrl

    // Update modal title if possible
    this.updateModalTitle()

    // Show modal
    const bsModal = new Modal(this.modal)
    bsModal.show()

    // Track trailer view
    this.trackTrailerView(trailerUrl)
  }

  closeTrailer() {
    if (!this.modal) return

    const bsModal = Modal.getInstance(this.modal)
    if (bsModal) {
      bsModal.hide()
    }
  }

  convertToEmbedUrl(url) {
    if (!url) return ""

    // YouTube URL patterns
    const youtubePatterns = [
      /(?:https?:\/\/)?(?:www\.)?youtube\.com\/watch\?v=([^&\n?#]+)/,
      /(?:https?:\/\/)?(?:www\.)?youtube\.com\/embed\/([^&\n?#]+)/,
      /(?:https?:\/\/)?youtu\.be\/([^&\n?#]+)/,
    ]

    for (const pattern of youtubePatterns) {
      const match = url.match(pattern)
      if (match) {
        return `https://www.youtube.com/embed/${match[1]}?autoplay=1&rel=0&modestbranding=1`
      }
    }

    // Vimeo URL patterns
    const vimeoPattern = /(?:https?:\/\/)?(?:www\.)?vimeo\.com\/(\d+)/
    const vimeoMatch = url.match(vimeoPattern)
    if (vimeoMatch) {
      return `https://player.vimeo.com/video/${vimeoMatch[1]}?autoplay=1`
    }

    // Return original URL if no pattern matches
    return url
  }

  updateModalTitle() {
    const modalTitle = this.modal.querySelector(".modal-title")
    if (modalTitle) {
      // Try to get movie title from context
      const activeCard = document.querySelector(".modern-movie-card:hover, .coming-soon-card:hover")
      if (activeCard) {
        const movieTitle = activeCard.querySelector(".movie-title")?.textContent
        if (movieTitle) {
          modalTitle.innerHTML = `<i class="fas fa-play-circle"></i> ${movieTitle} - Trailer`
          return
        }
      }

      // Default title
      modalTitle.innerHTML = '<i class="fas fa-play-circle"></i> Movie Trailer'
    }
  }

  onModalShown() {
    // Add modal entrance animation
    const modalContent = this.modal.querySelector(".modal-content")
    if (modalContent) {
      modalContent.style.transform = "scale(0.9)"
      modalContent.style.opacity = "0"
      setTimeout(() => {
        modalContent.style.transform = "scale(1)"
        modalContent.style.opacity = "1"
        modalContent.style.transition = "all 0.3s ease"
      }, 50)
    }

    // Pause any playing videos on the page
    this.pausePageVideos()

    // Pause carousel autoplay
    this.pauseCarousels()
  }

  onModalHidden() {
    // Clear iframe source to stop video
    if (this.iframe) {
      this.iframe.src = ""
    }

    // Resume carousel autoplay
    this.resumeCarousels()
  }

  pausePageVideos() {
    // Pause any HTML5 videos
    document.querySelectorAll("video").forEach((video) => {
      if (!video.paused) {
        video.pause()
        video.setAttribute("data-was-playing", "true")
      }
    })
  }

  pauseCarousels() {
    // Pause Swiper autoplay
    if (window.nowShowingManager) {
      window.nowShowingManager.pauseAutoplay()
    }
    if (window.comingSoonManager) {
      window.comingSoonManager.pauseAutoplay()
    }
    if (window.promotionsManager) {
      window.promotionsManager.pauseAutoplay()
    }
  }

  resumeCarousels() {
    // Resume Swiper autoplay
    setTimeout(() => {
      if (window.nowShowingManager) {
        window.nowShowingManager.resumeAutoplay()
      }
      if (window.comingSoonManager) {
        window.comingSoonManager.resumeAutoplay()
      }
      if (window.promotionsManager) {
        window.promotionsManager.resumeAutoplay()
      }
    }, 500)
  }

  trackTrailerView(trailerUrl) {
    // Analytics tracking
    if (typeof window.gtag !== "undefined") {
      window.gtag("event", "trailer_view", {
        trailer_url: trailerUrl,
      })
    }

    console.log("Trailer viewed:", trailerUrl)
  }

  // Public methods
  isOpen() {
    return this.modal && this.modal.classList.contains("show")
  }

  getCurrentTrailerUrl() {
    return this.iframe ? this.iframe.src : null
  }
}

// Global function for backward compatibility
window.openTrailer = (trailerUrl) => {
  if (window.trailerModalManager) {
    window.trailerModalManager.openTrailer(trailerUrl)
  }
}

// Initialize when DOM is ready
document.addEventListener("DOMContentLoaded", () => {
  window.trailerModalManager = new TrailerModalManager()
})

// Export for module systems
if (typeof module !== "undefined" && module.exports) {
  module.exports = TrailerModalManager
}
