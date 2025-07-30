// TRAILER MODAL JAVASCRIPT
// Use global Bootstrap Modal if available
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
    if (typeof bootstrap !== 'undefined') {
      const bsModal = new bootstrap.Modal(this.modal)
      bsModal.show()
    }

    // Track trailer view
    this.trackTrailerView(trailerUrl)
  }

  closeTrailer() {
    if (!this.modal) return

    if (typeof bootstrap !== 'undefined') {
      const bsModal = bootstrap.Modal.getInstance(this.modal)
      if (bsModal) {
        bsModal.hide()
      }
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
    const vimeoPatterns = [
      /(?:https?:\/\/)?(?:www\.)?vimeo\.com\/([0-9]+)/,
      /(?:https?:\/\/)?(?:www\.)?vimeo\.com\/groups\/[^\/]+\/videos\/([0-9]+)/,
    ]

    for (const pattern of vimeoPatterns) {
      const match = url.match(pattern)
      if (match) {
        return `https://player.vimeo.com/video/${match[1]}?autoplay=1&title=0&byline=0&portrait=0`
      }
    }

    // Return original URL if no patterns match
    return url
  }

  updateModalTitle() {
    const modalTitle = this.modal.querySelector(".modal-title")
    if (modalTitle) {
      modalTitle.textContent = "Movie Trailer"
    }
  }

  onModalShown() {
    // Pause any background videos or carousels
    this.pausePageVideos()
    this.pauseCarousels()

    // Focus management for accessibility
    const closeBtn = this.modal.querySelector(".btn-close")
    if (closeBtn) {
      closeBtn.focus()
    }
  }

  onModalHidden() {
    // Clear iframe source to stop video
    if (this.iframe) {
      this.iframe.src = ""
    }

    // Resume carousels
    this.resumeCarousels()
  }

  pausePageVideos() {
    // Pause any HTML5 videos on the page
    const videos = document.querySelectorAll("video")
    videos.forEach(video => {
      if (!video.paused) {
        video.pause()
      }
    })
  }

  pauseCarousels() {
    // Pause any autoplay carousels
    const swipers = document.querySelectorAll(".swiper")
    swipers.forEach(swiper => {
      if (swiper.swiper && swiper.swiper.autoplay) {
        swiper.swiper.autoplay.stop()
      }
    })
  }

  resumeCarousels() {
    // Resume autoplay carousels
    const swipers = document.querySelectorAll(".swiper")
    swipers.forEach(swiper => {
      if (swiper.swiper && swiper.swiper.autoplay) {
        swiper.swiper.autoplay.start()
      }
    })
  }

  trackTrailerView(trailerUrl) {
    // Track trailer view with Google Analytics if available
    if (typeof window.gtag === "function") {
      window.gtag("event", "trailer_view", {
        event_category: "engagement",
        event_label: trailerUrl,
      })
    }
  }

  isOpen() {
    return this.modal && this.modal.classList.contains("show")
  }

  getCurrentTrailerUrl() {
    return this.iframe ? this.iframe.src : ""
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