// PROMOTIONS SECTION JAVASCRIPT
// Import Swiper and gtag if they are not already defined
const Swiper = window.Swiper // Assuming Swiper is available globally
const gtag = window.gtag // Assuming gtag is available globally

class PromotionsManager {
  constructor() {
    this.swiper = null
    this.activePromotions = new Map()
    this.init()
  }

  init() {
    this.initSwiper()
    this.bindEvents()
    this.initScrollAnimations()
    this.initPromotionEffects()
  }

  initSwiper() {
    if (typeof Swiper !== "undefined") {
      this.swiper = new Swiper(".promotions-swiper-modern", {
        slidesPerView: 1,
        slidesPerGroup: 1,
        spaceBetween: 30,
        loop: true,
        autoplay: {
          delay: 6000,
          disableOnInteraction: false,
          pauseOnMouseEnter: true,
        },
        navigation: {
          nextEl: ".promotions-swiper-modern .swiper-button-next",
          prevEl: ".promotions-swiper-modern .swiper-button-prev",
        },
        effect: "slide",
        speed: 1200,
        breakpoints: {
          480: {
            slidesPerView: 1,
            slidesPerGroup: 1,
            spaceBetween: 20,
          },
          768: {
            slidesPerView: 2,
            slidesPerGroup: 2,
            spaceBetween: 25,
          },
          1024: {
            slidesPerView: 2,
            slidesPerGroup: 2,
            spaceBetween: 30,
          },
          1200: {
            slidesPerView: 3,
            slidesPerGroup: 3,
            spaceBetween: 35,
          },
        },
        on: {
          slideChange: () => {
            this.onSlideChange()
          },
        },
      })
    }
  }

  bindEvents() {
    // Learn more buttons
    document.querySelectorAll(".btn-learn-more").forEach((btn) => {
      btn.addEventListener("click", (e) => {
        e.preventDefault()
        const promoCard = btn.closest(".promotion-card")
        const promoId = promoCard.getAttribute("data-promo-id")
        this.showPromotionDetails(promoId)
      })
    })

    // Card hover effects
    document.querySelectorAll(".promotion-card").forEach((card) => {
      card.addEventListener("mouseenter", () => {
        this.onCardHover(card, true)
      })

      card.addEventListener("mouseleave", () => {
        this.onCardHover(card, false)
      })
    })
  }

  initScrollAnimations() {
    const observerOptions = {
      threshold: 0.1,
      rootMargin: "0px 0px -50px 0px",
    }

    const observer = new IntersectionObserver((entries) => {
      entries.forEach((entry) => {
        if (entry.isIntersecting) {
          entry.target.classList.add("animate")
        }
      })
    }, observerOptions)

    // Observe section elements
    const section = document.querySelector(".promotions-section")
    if (section) {
      section.classList.add("fade-in-up")
      observer.observe(section)
    }

    // Observe promotion cards with staggered animation
    document.querySelectorAll(".promotion-card").forEach((card, index) => {
      card.style.animationDelay = `${index * 0.2}s`
      card.classList.add("fade-in-left")
      observer.observe(card)
    })
  }

  initPromotionEffects() {
    // Animate discount badges
    document.querySelectorAll(".discount-badge").forEach((badge) => {
      this.animateDiscountBadge(badge)
    })

    // Add sparkle effects to promotion cards
    document.querySelectorAll(".promotion-card").forEach((card) => {
      this.addSparkleEffect(card)
    })

    // Initialize countdown timers if needed
    this.initCountdownTimers()
  }

  animateDiscountBadge(badge) {
    // Add floating animation
    let direction = 1
    setInterval(() => {
      badge.style.transform = `translateY(${direction * 3}px) rotate(${direction * 2}deg)`
      direction *= -1
    }, 2000)
  }

  addSparkleEffect(card) {
    const sparkleContainer = document.createElement("div")
    sparkleContainer.className = "sparkle-container"
    sparkleContainer.style.cssText = `
            position: absolute;
            top: 0;
            left: 0;
            right: 0;
            bottom: 0;
            pointer-events: none;
            overflow: hidden;
            border-radius: var(--border-radius-lg);
        `

    card.style.position = "relative"
    card.appendChild(sparkleContainer)

    // Create sparkles on hover
    card.addEventListener("mouseenter", () => {
      this.createSparkles(sparkleContainer)
    })
  }

  createSparkles(container) {
    for (let i = 0; i < 6; i++) {
      setTimeout(() => {
        const sparkle = document.createElement("div")
        sparkle.className = "sparkle"
        sparkle.style.cssText = `
                    position: absolute;
                    width: 4px;
                    height: 4px;
                    background: #fff;
                    border-radius: 50%;
                    animation: sparkle-float 1.5s ease-out forwards;
                    left: ${Math.random() * 100}%;
                    top: ${Math.random() * 100}%;
                `

        container.appendChild(sparkle)

        setTimeout(() => {
          if (sparkle.parentNode) {
            sparkle.parentNode.removeChild(sparkle)
          }
        }, 1500)
      }, i * 100)
    }
  }

  initCountdownTimers() {
    // Add countdown timers for limited-time offers
    document.querySelectorAll(".promotion-card").forEach((card) => {
      const promoId = card.getAttribute("data-promo-id")
      // You can add specific end dates for promotions here
      // this.addCountdownTimer(card, endDate);
    })
  }

  onSlideChange() {
    // Add slide change effects
    const activeSlides = document.querySelectorAll(".promotions-swiper-modern .swiper-slide-active .promotion-card")
    activeSlides.forEach((card) => {
      card.classList.add("slide-active")
      setTimeout(() => {
        card.classList.remove("slide-active")
      }, 1200)
    })
  }

  onCardHover(card, isHovering) {
    const overlay = card.querySelector(".promotion-overlay")
    const image = card.querySelector(".promotion-image img")
    const discountBadge = card.querySelector(".discount-badge")

    if (isHovering) {
      card.style.transform = "translateY(-12px) scale(1.02)"
      if (overlay) overlay.style.opacity = "1"
      if (image) image.style.transform = "scale(1.1)"
      if (discountBadge) {
        discountBadge.style.animation = "none"
        discountBadge.style.transform = "scale(1.2) rotate(5deg)"
      }
    } else {
      card.style.transform = ""
      if (overlay) overlay.style.opacity = "0"
      if (image) image.style.transform = ""
      if (discountBadge) {
        discountBadge.style.animation = "bounce 2s infinite"
        discountBadge.style.transform = ""
      }
    }
  }



  showPromotionDetails(promoId) {
    // Create and show promotion details modal
    const modal = this.createPromotionModal(promoId)
    document.body.appendChild(modal)

    // Show modal
    setTimeout(() => {
      modal.classList.add("show")
    }, 100)

    // Track promotion interaction
    this.trackPromotionClick(promoId, "learn_more")
  }



  createPromotionModal(promoId) {
    const modal = document.createElement("div")
    modal.className = "promotion-details-modal"
    // Add modal content based on promoId
    // This would typically fetch details from your backend
    return modal
  }

  trackPromotionClick(promoId, action) {
    // Analytics tracking
    if (typeof gtag !== "undefined") {
      gtag("event", "promotion_click", {
        promotion_id: promoId,
        action: action,
      })
    }

    
  }

  // Public methods
  nextSlide() {
    if (this.swiper) {
      this.swiper.slideNext()
    }
  }

  prevSlide() {
    if (this.swiper) {
      this.swiper.slidePrev()
    }
  }

  pauseAutoplay() {
    if (this.swiper && this.swiper.autoplay) {
      this.swiper.autoplay.stop()
    }
  }

  resumeAutoplay() {
    if (this.swiper && this.swiper.autoplay) {
      this.swiper.autoplay.start()
    }
  }

  getActivePromotions() {
    return Array.from(this.activePromotions.keys())
  }

  destroy() {
    if (this.swiper) {
      this.swiper.destroy(true, true)
    }
  }
}

// Initialize when DOM is ready
document.addEventListener("DOMContentLoaded", () => {
  window.promotionsManager = new PromotionsManager()
})

// Export for module systems
if (typeof module !== "undefined" && module.exports) {
  module.exports = PromotionsManager
}

// Add CSS for sparkle animation and modals
const promotionStyles = `
@keyframes sparkle-float {
    0% {
        opacity: 0;
        transform: translateY(0) scale(0);
    }
    50% {
        opacity: 1;
        transform: translateY(-20px) scale(1);
    }
    100% {
        opacity: 0;
        transform: translateY(-40px) scale(0);
    }
}

.promotion-offer-modal {
    position: fixed;
    top: 0;
    left: 0;
    right: 0;
    bottom: 0;
    z-index: 10000;
    display: flex;
    align-items: center;
    justify-content: center;
    opacity: 0;
    transition: opacity 0.3s ease;
}

.promotion-offer-modal.show {
    opacity: 1;
}

.promotion-offer-modal .modal-backdrop {
    position: absolute;
    top: 0;
    left: 0;
    right: 0;
    bottom: 0;
    background: rgba(0, 0, 0, 0.8);
    backdrop-filter: blur(5px);
}

.promotion-offer-modal .modal-content {
    background: var(--bg-secondary);
    border-radius: var(--border-radius-lg);
    padding: 0;
    max-width: 500px;
    width: 90%;
    position: relative;
    box-shadow: var(--shadow-xl);
    border: 1px solid rgba(255, 255, 255, 0.1);
}

.promotion-offer-modal .modal-header {
    background: var(--success-gradient);
    padding: var(--spacing-lg);
    border-radius: var(--border-radius-lg) var(--border-radius-lg) 0 0;
    display: flex;
    justify-content: space-between;
    align-items: center;
}

.promotion-offer-modal .modal-header h3 {
    color: white;
    margin: 0;
    display: flex;
    align-items: center;
    gap: var(--spacing-sm);
}

.promotion-offer-modal .close-btn {
    background: rgba(255, 255, 255, 0.2);
    border: none;
    border-radius: 50%;
    width: 35px;
    height: 35px;
    color: white;
    cursor: pointer;
    transition: background 0.3s ease;
}

.promotion-offer-modal .close-btn:hover {
    background: rgba(255, 255, 255, 0.3);
}

.promotion-offer-modal .modal-body {
    padding: var(--spacing-xl);
    text-align: center;
}

.success-animation {
    font-size: 4rem;
    color: #4facfe;
    margin-bottom: var(--spacing-lg);
    animation: success-bounce 0.6s ease;
}

@keyframes success-bounce {
    0% { transform: scale(0); }
    50% { transform: scale(1.2); }
    100% { transform: scale(1); }
}

.promo-code {
    background: var(--bg-card);
    border: 2px dashed var(--text-accent);
    border-radius: var(--border-radius);
    padding: var(--spacing-md);
    margin: var(--spacing-lg) 0;
    display: flex;
    align-items: center;
    justify-content: space-between;
}

.copy-btn {
    background: var(--text-accent);
    border: none;
    border-radius: var(--border-radius-sm);
    padding: var(--spacing-xs);
    color: white;
    cursor: pointer;
    transition: background 0.3s ease;
}

.copy-btn:hover {
    background: var(--primary-gradient);
}

.promotion-offer-modal .modal-footer {
    padding: var(--spacing-lg);
    text-align: center;
}

.btn-primary {
    background: var(--primary-gradient);
    border: none;
    border-radius: var(--border-radius);
    padding: var(--spacing-md) var(--spacing-xl);
    color: white;
    font-weight: 600;
    cursor: pointer;
    transition: transform 0.3s ease;
}

.btn-primary:hover {
    transform: translateY(-2px);
}
`

// Inject promotion styles
const promotionStyleSheet = document.createElement("style")
promotionStyleSheet.textContent = promotionStyles
document.head.appendChild(promotionStyleSheet)
