document.addEventListener("DOMContentLoaded", function () {
  // Khởi tạo Swiper cho hero section
  var heroSwiper = new Swiper('.hero-swiper', {
    effect: 'slide',
    grabCursor: true,
    centeredSlides: true,
    slidesPerView: 5,
    spaceBetween: 20,
    loop: true,
    speed: 400, // đồng bộ với background
    // Không dùng navigation Swiper ở dưới nữa
    autoplay: {
      delay: 8000,
      disableOnInteraction: false,
    },
    breakpoints: {
      0: { slidesPerView: 1 },
      576: { slidesPerView: 3 },
      992: { slidesPerView: 5 }
    }
  });

  // Gán sự kiện cho nút prev/next lớn trên hero content
  document.getElementById('hero-prev').addEventListener('click', function() {
    heroSwiper.slidePrev();
  });
  document.getElementById('hero-next').addEventListener('click', function() {
    heroSwiper.slideNext();
  });
  // Gán sự kiện cho side bar navigation
  document.querySelector('.hero-nav-side-left').addEventListener('click', function() {
    heroSwiper.slidePrev();
  });
  document.querySelector('.hero-nav-side-right').addEventListener('click', function() {
    heroSwiper.slideNext();
  });

  // Progress bar logic
  var progressBar = document.getElementById('hero-progress-bar');
  var progressInterval;
  var slideDuration = 8000;
  var progressStep = 10;
  var progress = 0;

  function startProgressBar() {
    progress = 0;
    progressBar.style.width = '0%';
    clearInterval(progressInterval);
    progressInterval = setInterval(function () {
      progress += (progressStep / slideDuration) * 100;
      progressBar.style.width = progress + '%';
      if (progress >= 100) {
        heroSwiper.slideNext();
      }
    }, progressStep);
  }

  function resetProgressBar() {
    clearInterval(progressInterval);
    startProgressBar();
  }

  // Khi Swiper bắt đầu chuyển slide, update ngay background track để chạy song song
  heroSwiper.on('slideChangeTransitionStart', function () {
    updateHeroBgTrack(heroSwiper.realIndex);
  });

  // Khi click vào slide, chuyển đến slide đó
  document.querySelectorAll('.hero-swiper .swiper-slide').forEach(function (slide) {
    slide.addEventListener('click', function () {
      var realIndex = parseInt(this.getAttribute('data-swiper-slide-index'));
      heroSwiper.slideToLoop(realIndex);
    });
  });

  // Lần đầu load cũng cập nhật đúng info
  updateHeroContentByIndex(heroSwiper.realIndex);
  startProgressBar();

  // Pause auto slide on hover
  document.querySelector('.hero-section').addEventListener('mouseenter', function () {
    heroSwiper.autoplay.stop();
    clearInterval(progressInterval);
  });
  document.querySelector('.hero-section').addEventListener('mouseleave', function () {
    heroSwiper.autoplay.start();
    startProgressBar();
  });

  // === HERO BG TRACK LOGIC ===
  var heroBg = document.getElementById('hero-bg');
  var heroBgTrack = heroBg.querySelector('.hero-bg-track');
  var slides = document.querySelectorAll('.hero-swiper .swiper-slide');
  var slideCount = slides.length;
  var currentIndex = heroSwiper.realIndex;

  function updateHeroBgTrack(index, peek, animate = true) {
    var bgWidth = heroBg.offsetWidth;
    var offset = -(index + 1) * bgWidth; // +1 vì có 1 clone đầu
    if (peek === 'left') offset += bgWidth * 0.1;
    if (peek === 'right') offset -= bgWidth * 0.1;
    if (!animate) heroBgTrack.style.transition = 'none';
    else heroBgTrack.style.transition = '';
    heroBgTrack.style.transform = 'translateX(' + offset + 'px)';
  }

  // Lần đầu load
  updateHeroBgTrack(heroSwiper.realIndex);

  // Khi Swiper chuyển slide xong
  heroSwiper.on('slideChangeTransitionEnd', function () {
    currentIndex = heroSwiper.realIndex;
    // Xử lý loop: nếu activeIndex là 0 (clone cuối), reset về cuối thực; nếu là slideCount+1 (clone đầu), reset về đầu thực
    if (heroSwiper.activeIndex === 0) {
      // Đang ở clone cuối, reset về cuối thực (không animate)
      updateHeroBgTrack(slideCount - 1, undefined, false);
    } else if (heroSwiper.activeIndex === slideCount + 1) {
      // Đang ở clone đầu, reset về đầu thực (không animate)
      updateHeroBgTrack(0, undefined, false);
    } else {
      updateHeroBgTrack(currentIndex);
    }
    heroBg.classList.remove('peek-left','peek-right','slide-left','slide-right');
    updateHeroContentByIndex(currentIndex);
    resetProgressBar();
  });

  // Hiệu ứng peek động cho hero-bg-track khi hover vào 10% trái/phải
  var heroSection = document.querySelector('.hero-section');
  heroSection.addEventListener('mousemove', function(e) {
    var rect = heroSection.getBoundingClientRect();
    var x = e.clientX - rect.left;
    var width = rect.width;
    if (x < width * 0.1) {
      heroBg.classList.add('peek-left');
      heroBg.classList.remove('peek-right');
      updateHeroBgTrack(currentIndex, 'left');
    } else if (x > width * 0.9) {
      heroBg.classList.add('peek-right');
      heroBg.classList.remove('peek-left');
      updateHeroBgTrack(currentIndex, 'right');
    } else {
      heroBg.classList.remove('peek-left');
      heroBg.classList.remove('peek-right');
      updateHeroBgTrack(currentIndex);
    }
  });
  heroSection.addEventListener('mouseleave', function() {
    heroBg.classList.remove('peek-left');
    heroBg.classList.remove('peek-right');
    updateHeroBgTrack(currentIndex);
  });
  // Khi click vào vùng 10% trái/phải, animate background và chuyển slide tương ứng
  heroSection.addEventListener('click', function(e) {
    var rect = heroSection.getBoundingClientRect();
    var x = e.clientX - rect.left;
    var width = rect.width;
    if (x < width * 0.1) {
      heroSwiper.slidePrev();
    } else if (x > width * 0.9) {
      heroSwiper.slideNext();
    }
  });

  // Hàm cập nhật hero-info và background
  function updateHeroContentByIndex(index) {
    var slide = document.querySelector('.hero-swiper .swiper-slide[data-swiper-slide-index="' + index + '"]');
    var heroInfo = document.getElementById('hero-info');
    var heroBg = document.getElementById('hero-bg');
    if (!slide) return;
    // Lấy data từ slide
    var data = {
      name: slide.getAttribute('data-movie-name'),
      duration: slide.getAttribute('data-movie-duration'),
      versions: slide.getAttribute('data-movie-versions'),
      content: slide.getAttribute('data-movie-content'),
      actor: slide.getAttribute('data-movie-actor'),
      types: slide.getAttribute('data-movie-types'),
      director: slide.getAttribute('data-movie-director'),
      largeimage: slide.getAttribute('data-movie-largeimage'),
      logoimage: slide.getAttribute('data-movie-logoimage'),
      id: slide.getAttribute('data-movie-id'),
    };
    // Update background
    var bgImg = heroBg.querySelector('img');
    if (bgImg) {
      bgImg.src = data.largeimage;
      bgImg.alt = data.name;
    }
    // Update info
    setTimeout(function () {
      // Logo hoặc title
      var logo = heroInfo.querySelector('.hero-movie-logo');
      var title = heroInfo.querySelector('.hero-title');
      if (data.logoimage && data.logoimage.length > 0) {
        if (logo) {
          logo.src = data.logoimage;
        } else {
          if (title) title.remove();
          var img = document.createElement('img');
          img.className = 'hero-movie-logo';
          img.src = data.logoimage;
          img.alt = data.name + ' Logo';
          heroInfo.prepend(img);
        }
      } else {
        if (title) {
          title.textContent = data.name;
        } else {
          if (logo) logo.remove();
          var h2 = document.createElement('h2');
          h2.className = 'hero-title';
          h2.textContent = data.name;
          heroInfo.prepend(h2);
        }
      }
      // Meta
      var metaSpans = heroInfo.querySelectorAll('.hero-meta span');
      if (metaSpans.length > 0) metaSpans[0].innerHTML = '<i class="bx bxs-time"></i> ' + data.duration + "'";
      if (metaSpans.length > 1) metaSpans[1].textContent = data.versions || 'HD';
      // Desc
      var desc = heroInfo.querySelector('.hero-desc');
      if (desc) desc.textContent = data.content;
      // Extra info
      var extra = heroInfo.querySelector('.hero-extra-info');
      if (extra) extra.innerHTML =
        '<div><b>Starring:</b> ' + data.actor + '</div>' +
        '<div><b>This show is:</b> ' + data.types + '</div>' +
        '<div><b>Director:</b> ' + data.director + '</div>';
      // Book now
      var bookBtn = heroInfo.querySelector('.book-now-btn');
      if (bookBtn) bookBtn.setAttribute('href', '/Movie/Detail/' + data.id);
      // Fade in
      setTimeout(function () {
        heroInfo.classList.add('active');
        heroBg.classList.add('active');
      }, 100);
    }, 300);
  }
});
