document.addEventListener("DOMContentLoaded", () => {
  // Initialize hero section
  initHeroSection()

  // Add active class to hero elements with delay for animation
  setTimeout(() => {
    document.getElementById("hero-bg").classList.add("active")
    document.getElementById("hero-info").classList.add("active")
  }, 100)
})

function initHeroSection() {
  // Initialize Owl Carousel
  const heroCarousel = window.$("#hero-carousel").owlCarousel({
    items: 5,
    loop: true,
    margin: 20,
    nav: false,
    dots: false,
    center: true,
    autoWidth: false, // Đã tắt autoWidth để tránh lỗi owl-stage quá rộng
    responsive: {
      0: { items: 1 },
      576: { items: 3 },
      992: { items: 5 },
    },
  })

  // Navigation buttons
  window.$("#hero-prev").on("click", () => {
    heroCarousel.trigger("prev.owl.carousel")
    resetProgressBar()
  })

  window.$("#hero-next").on("click", () => {
    heroCarousel.trigger("next.owl.carousel")
    resetProgressBar()
  })

  // Click on carousel item
  window.$(document).on("click", ".hero-carousel-item", function () {
    const index = window.$(this).parent().index()
    heroCarousel.trigger("to.owl.carousel", [index, 300])
    resetProgressBar()
  })

  // When carousel changes
  heroCarousel.on("changed.owl.carousel", (event) => {
    updateHeroContent(event.item.index)
  })

  // Auto slide with progress bar
  let progressInterval
  const slideDuration = 8000 // 8 seconds per slide
  const progressStep = 10 // Update progress every 10ms
  let progress = 0

  function startProgressBar() {
    const progressBar = document.getElementById("hero-progress-bar")
    progress = 0
    progressBar.style.width = "0%"

    progressInterval = setInterval(() => {
      progress += (progressStep / slideDuration) * 100
      progressBar.style.width = `${progress}%`

      if (progress >= 100) {
        window.$("#hero-next").click()
      }
    }, progressStep)
  }

  function resetProgressBar() {
    clearInterval(progressInterval)
    startProgressBar()
  }

  // Start auto slide
  startProgressBar()

  // Pause auto slide on hover
  window.$(".hero-section").hover(
    () => {
      clearInterval(progressInterval)
    },
    () => {
      startProgressBar()
    },
  )
}

function updateHeroContent(index) {
  // Find the active item
  const $item = window.$(".owl-item").eq(index).find(".hero-carousel-item")
  if (!$item.length) return

  // Get movie data
  const data = {
    id: $item.data("movie-id"),
    name: $item.data("movie-name"),
    duration: $item.data("movie-duration"),
    versions: $item.data("movie-versions"),
    content: $item.data("movie-content"),
    actor: $item.data("movie-actor"),
    types: $item.data("movie-types"),
    director: $item.data("movie-director"),
    largeimage: $item.data("movie-largeimage"),
    trailerurl: $item.data("movie-trailerurl"),
    logoimage: $item.data("movie-logoimage"),
  }

  // Update active state in carousel
  window.$(".hero-carousel-item").removeClass("active")
  $item.addClass("active")

  // Animate content change
  const $heroInfo = window.$("#hero-info")
  const $heroBg = window.$("#hero-bg")

  // Fade out
  $heroInfo.removeClass("active")

  // Change background with crossfade
  const $newBg = window.$('<div class="hero-bg"><img src="' + data.largeimage + '" alt="' + data.name + '"></div>')
  $newBg.css("opacity", 0)
  $heroBg.after($newBg)

  setTimeout(() => {
    $newBg.css("opacity", 1)
    setTimeout(() => {
      $heroBg.remove()
      $newBg.attr("id", "hero-bg")
      $newBg.addClass("active")
    }, 500)
  }, 100)

  // Update content after short delay
  setTimeout(() => {
    // Update logo or title
    if (data.logoimage && data.logoimage.length > 0) {
      if ($heroInfo.find(".hero-movie-logo").length) {
        $heroInfo.find(".hero-movie-logo").attr("src", data.logoimage)
      } else {
        $heroInfo.find(".hero-title").remove()
        $heroInfo.prepend('<img class="hero-movie-logo" src="' + data.logoimage + '" alt="' + data.name + ' Logo">')
      }
    } else {
      if ($heroInfo.find(".hero-title").length) {
        $heroInfo.find(".hero-title").text(data.name)
      } else {
        $heroInfo.find(".hero-movie-logo").remove()
        $heroInfo.prepend('<h2 class="hero-title">' + data.name + "</h2>")
      }
    }

    // Update meta info
    $heroInfo
      .find(".hero-meta span")
      .eq(0)
      .html('<i class="bx bxs-time"></i> ' + data.duration + "'")
    $heroInfo
      .find(".hero-meta span")
      .eq(1)
      .text(data.versions || "HD")

    // Update description
    $heroInfo.find(".hero-desc").text(data.content)

    // Update extra info
    $heroInfo
      .find(".hero-extra-info")
      .html(
        "<div><b>Starring:</b> " +
          data.actor +
          "</div>" +
          "<div><b>This show is:</b> " +
          data.types +
          "</div>" +
          "<div><b>Director:</b> " +
          data.director +
          "</div>",
      )

    // Update book now button link
    $heroInfo.find(".book-now-btn").attr("href", "/Movie/Detail/" + data.id)

    // Fade in
    setTimeout(() => {
      $heroInfo.addClass("active")
    }, 100)
  }, 300)
}
