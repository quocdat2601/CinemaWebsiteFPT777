// ENHANCED MOVIE SECTIONS JAVASCRIPT
document.addEventListener("DOMContentLoaded", () => {
    // Check if Swiper is available
    const Swiper = window.Swiper // Declare Swiper variable
    if (typeof Swiper === "undefined") {
        console.warn("Swiper library not found. Please include Swiper.js")
        return
    }

    // Check if gtag is available
    const gtag = window.gtag // Declare gtag variable
    if (typeof gtag !== "undefined") {
        console.log("Google Analytics is available")
    } else {
        console.warn("Google Analytics is not available")
    }

    // Common Swiper configuration
    const swiperConfig = {
        // Core settings
        loop: true,
        autoplay: false, // Tắt autoplay
        centeredSlides: false,
        grabCursor: true,
        watchOverflow: true,

        // Navigation
        navigation: {
            nextEl: ".swiper-button-next",
            prevEl: ".swiper-button-prev",
        },

        // Effects
        effect: "slide",
        speed: 600,

        // Responsive breakpoints
        breakpoints: {
            // Mobile portrait
            320: {
                slidesPerView: 1,
                slidesPerGroup: 1,
                spaceBetween: 16,
            },
            // Mobile landscape
            480: {
                slidesPerView: 2,
                slidesPerGroup: 1,
                spaceBetween: 18,
            },
            // Tablet portrait
            640: {
                slidesPerView: 2,
                slidesPerGroup: 2,
                spaceBetween: 20,
            },
            // Tablet landscape
            768: {
                slidesPerView: 3,
                slidesPerGroup: 2,
                spaceBetween: 22,
            },
            // Desktop small
            900: {
                slidesPerView: 3,
                slidesPerGroup: 3,
                spaceBetween: 24,
            },
            // Desktop medium
            1200: {
                slidesPerView: 4,
                slidesPerGroup: 4,
                spaceBetween: 26,
            },
            // Desktop large
            1400: {
                slidesPerView: 4,
                slidesPerGroup: 4,
                spaceBetween: 28,
            },
        },

        // Events
        on: {
            init: () => {
                console.log("Swiper initialized")
                // Add loading state removal
                setTimeout(() => {
                    document.querySelectorAll(".movie-card-enhanced.loading").forEach((card) => {
                        card.classList.remove("loading")
                    })
                }, 500)
            },
            slideChange: function () {
                // Optional: Add analytics tracking here
                console.log("Slide changed to:", this.activeIndex)
            },
            resize: function () {
                this.update()
            },
        },
    }

    // Initialize Now Showing Swiper
    try {
        const nowShowingSwiper = new Swiper(".nowshowing-swiper", {
            ...swiperConfig,
            // Specific settings for now showing
            autoplay: false, // Tắt autoplay
        })

        console.log("Now Showing Swiper initialized successfully")
    } catch (error) {
        console.error("Error initializing Now Showing Swiper:", error)
    }

    // Initialize Coming Soon Swiper
    try {
        const comingSoonSwiper = new Swiper(".comingsoon-swiper", {
            ...swiperConfig,
            // Specific settings for coming soon
            autoplay: false, // Tắt autoplay
        })

        console.log("Coming Soon Swiper initialized successfully")
    } catch (error) {
        console.error("Error initializing Coming Soon Swiper:", error)
    }

    // Enhanced button interactions
    initializeButtonInteractions()

    // Initialize lazy loading for images
    initializeLazyLoading()

    // Initialize accessibility features
    initializeAccessibility()
})

// Button interaction handlers
function initializeButtonInteractions() {
    // Trailer button handlers
    document.addEventListener("click", (e) => {
        const trailerBtn = e.target.closest("[data-trailer-url]")
        if (trailerBtn) {
            e.preventDefault()
            const trailerUrl = trailerBtn.getAttribute("data-trailer-url")

            if (trailerUrl && trailerUrl.trim() !== "") {
                handleTrailerClick(trailerUrl, trailerBtn)
            } else {
                showNotification("Trailer not available", "warning")
            }
        }
    })

    // Book now button handlers
    document.addEventListener("click", (e) => {
        const bookBtn = e.target.closest("[data-movie-id][data-movie-name]")
        if (bookBtn && bookBtn.querySelector(".btn-text")?.textContent.includes("Book")) {
            e.preventDefault()
            const movieId = bookBtn.getAttribute("data-movie-id")
            const movieName = bookBtn.getAttribute("data-movie-name")

            handleBookingClick(movieId, movieName, bookBtn)
        }
    })

    
}

// Trailer click handler
function handleTrailerClick(trailerUrl, button) {
    // Add loading state
    button.classList.add("loading")
    button.disabled = true

    try {
        // Create modal or open in new window
        if (trailerUrl.includes("youtube.com") || trailerUrl.includes("youtu.be")) {
            openYouTubeModal(trailerUrl)
        } else {
            window.open(trailerUrl, "_blank", "width=800,height=600")
        }

        // Track event (if analytics is available)
        const gtag = window.gtag // Declare gtag variable within function scope
        if (typeof gtag !== "undefined") {
            gtag("event", "trailer_view", {
                movie_trailer_url: trailerUrl,
            })
        }
    } catch (error) {
        console.error("Error opening trailer:", error)
        showNotification("Error opening trailer", "error")
    } finally {
        // Remove loading state
        setTimeout(() => {
            button.classList.remove("loading")
            button.disabled = false
        }, 1000)
    }
}

// Booking click handler
function handleBookingClick(movieId, movieName, button) {
    // Add loading state
    button.classList.add("loading")
    button.disabled = true

    try {
        console.log("Opening booking modal for:", movieName, "ID:", movieId)

        // Call the global booking modal function
        if (typeof window.openBookingModal === 'function') {
            window.openBookingModal(movieId, movieName);
        } else {
            console.error("openBookingModal function not found");
            showNotification("Booking system not available", "error");
        }

        // Track event
        const gtag = window.gtag
        if (typeof gtag !== "undefined") {
            gtag("event", "booking_initiated", {
                movie_id: movieId,
                movie_name: movieName,
            })
        }
    } catch (error) {
        console.error("Error opening booking modal:", error)
        showNotification("Error opening booking modal", "error")
    } finally {
        // Remove loading state
        setTimeout(() => {
            button.classList.remove("loading")
            button.disabled = false
        }, 1000)
    }
}

// Notify click handler


// YouTube modal handler
function openYouTubeModal(url) {
    // Extract video ID from YouTube URL
    const videoId = extractYouTubeVideoId(url)
    if (!videoId) {
        window.open(url, "_blank")
        return
    }

    // Create modal
    const modal = document.createElement("div")
    modal.className = "trailer-modal"
    modal.innerHTML = `
        <div class="trailer-modal-content">
            <button class="trailer-modal-close">&times;</button>
            <iframe 
                src="https://www.youtube.com/embed/${videoId}?autoplay=1&rel=0" 
                frameborder="0" 
                allowfullscreen
                allow="autoplay; encrypted-media">
            </iframe>
        </div>
    `

    // Add modal styles
    const style = document.createElement("style")
    style.textContent = `
        .trailer-modal {
            position: fixed;
            top: 0; left: 0; right: 0; bottom: 0;
            background: rgba(0,0,0,0.9);
            display: flex;
            align-items: center;
            justify-content: center;
            z-index: 10000;
            animation: fadeIn 0.3s ease;
        }
        .trailer-modal-content {
            position: relative;
            width: 90%;
            max-width: 800px;
            aspect-ratio: 16/9;
        }
        .trailer-modal-content iframe {
            width: 100%;
            height: 100%;
            border-radius: 12px;
        }
        .trailer-modal-close {
            position: absolute;
            top: -40px;
            right: 0;
            background: none;
            border: none;
            color: white;
            font-size: 2rem;
            cursor: pointer;
            z-index: 1;
        }
        @keyframes fadeIn {
            from { opacity: 0; }
            to { opacity: 1; }
        }
    `

    document.head.appendChild(style)
    document.body.appendChild(modal)

    // Close modal handlers
    modal.querySelector(".trailer-modal-close").onclick = () => {
        document.body.removeChild(modal)
        document.head.removeChild(style)
    }

    modal.onclick = (e) => {
        if (e.target === modal) {
            document.body.removeChild(modal)
            document.head.removeChild(style)
        }
    }

    // ESC key handler
    const escHandler = (e) => {
        if (e.key === "Escape") {
            document.body.removeChild(modal)
            document.head.removeChild(style)
            document.removeEventListener("keydown", escHandler)
        }
    }
    document.addEventListener("keydown", escHandler)
}

// Extract YouTube video ID
function extractYouTubeVideoId(url) {
    const regExp = /^.*(youtu.be\/|v\/|u\/\w\/|embed\/|watch\?v=|&v=)([^#&?]*).*/
    const match = url.match(regExp)
    return match && match[2].length === 11 ? match[2] : null
}

// Notification system
function showNotification(message, type = "info") {
    const notification = document.createElement("div")
    notification.className = `notification notification-${type}`
    notification.textContent = message

    // Add notification styles
    const style = document.createElement("style")
    style.textContent = `
        .notification {
            position: fixed;
            top: 20px;
            right: 20px;
            padding: 1rem 1.5rem;
            border-radius: 8px;
            color: white;
            font-weight: 600;
            z-index: 10001;
            animation: slideIn 0.3s ease;
        }
        .notification-success { background: #10B981; }
        .notification-error { background: #EF4444; }
        .notification-warning { background: #F59E0B; }
        .notification-info { background: #3B82F6; }
        @keyframes slideIn {
            from { transform: translateX(100%); opacity: 0; }
            to { transform: translateX(0); opacity: 1; }
        }
    `

    if (!document.querySelector("#notification-styles")) {
        style.id = "notification-styles"
        document.head.appendChild(style)
    }

    document.body.appendChild(notification)

    setTimeout(() => {
        if (notification.parentNode) {
            notification.parentNode.removeChild(notification)
        }
    }, 4000)
}

// Lazy loading for images
function initializeLazyLoading() {
    if ("IntersectionObserver" in window) {
        const imageObserver = new IntersectionObserver((entries, observer) => {
            entries.forEach((entry) => {
                if (entry.isIntersecting) {
                    const img = entry.target
                    img.src = img.dataset.src || img.src
                    img.classList.remove("lazy")
                    observer.unobserve(img)
                }
            })
        })

        document.querySelectorAll('img[loading="lazy"]').forEach((img) => {
            imageObserver.observe(img)
        })
    }
}

// Accessibility features
function initializeAccessibility() {
    // Add keyboard navigation for cards
    document.querySelectorAll(".movie-card-enhanced").forEach((card) => {
        card.setAttribute("tabindex", "0")
        card.setAttribute("role", "article")
        card.setAttribute("aria-label", `Movie: ${card.querySelector(".movie-title")?.textContent || "Unknown"}`)

        card.addEventListener("keydown", (e) => {
            if (e.key === "Enter" || e.key === " ") {
                e.preventDefault()
                const firstButton = card.querySelector(".action-btn")
                if (firstButton) firstButton.click()
            }
        })
    })

    // Add ARIA labels to buttons
    document.querySelectorAll(".action-btn").forEach((btn) => {
        const text = btn.querySelector(".btn-text")?.textContent
        if (text) {
            btn.setAttribute("aria-label", text)
        }
    })
}

// Performance monitoring
function logPerformance() {
    if ("performance" in window) {
        window.addEventListener("load", () => {
            setTimeout(() => {
                const perfData = performance.getEntriesByType("navigation")[0]
                console.log("Page load time:", perfData.loadEventEnd - perfData.loadEventStart, "ms")
            }, 0)
        })
    }
}

// Initialize performance monitoring
logPerformance()

// Export functions for external use
window.MovieSections = {
    handleTrailerClick,
    handleBookingClick,
    showNotification,
}
