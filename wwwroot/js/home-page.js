
$(document).ready(function () {
    // Initialize cycle2 slideshow with explicit options
    $('#home_big_banner').cycle({
        fx: 'fade',
        timeout: 5000,
        slides: '> li',
        pager: '.home_big_banner_pager',
        pagerTemplate: '<span class="pager-bullet"></span>',
        prev: '.home_big_banner_prev',
        next: '.home_big_banner_next',
        swipe: true,
        swipeFx: 'fade',
        log: false,
        speed: 1000,
        pauseOnHover: true,
        allowWrap: true,
        manualSpeed: 1000,
        manualTrump: true
    });

    // Add click event handlers as backup
    $('.home_big_banner_prev').on('click', function () {
        $('#home_big_banner').cycle('prev');
    });

    $('.home_big_banner_next').on('click', function () {
        $('#home_big_banner').cycle('next');
    });
});

var movieSwiper = new Swiper('.movie-swiper', {
    effect: 'coverflow',
    grabCursor: true,
    centeredSlides: true,
    slidesPerView: 3,
    spaceBetween: 30,
    loop: true,
    loopAdditionalSlides: 2,
    watchSlidesProgress: true,
    speed: 800,
    coverflowEffect: {
        rotate: 0,
        stretch: 0,
        depth: 0,
        modifier: 1,
        slideShadows: false,
    },
    pagination: {
        el: '.movie-pagination',
        clickable: true,
    },
    breakpoints: {
        0: { slidesPerView: 1 },
        576: { slidesPerView: 2 },
        992: { slidesPerView: 3 }
    }
});

// Cập nhật hero-info và background khi slide thay đổi
function updateHeroContentByIndex(index) {
    const slide = document.querySelector(`.movie-swiper .swiper-slide[data-swiper-slide-index="${index}"]`);
    const heroInfo = document.getElementById('hero-info');
    const heroBg = document.getElementById('hero-bg');

    if (slide) {
        heroBg.style.transition = 'all 0.3s cubic-bezier(0.25, 0.46, 0.45, 0.94)';
        heroBg.style.backgroundImage = `url('${slide.dataset.large}')`;

        heroInfo.style.opacity = 0;
        setTimeout(() => {
            document.querySelector('.movie-title').textContent = slide.dataset.title;
            document.querySelector('.movie-duration').textContent = slide.dataset.duration;
            document.querySelector('.movie-director').textContent = slide.dataset.director;
            document.querySelector('.movie-actor').textContent = slide.dataset.actor;
            document.querySelector('.movie-version').textContent = slide.dataset.version;
            document.querySelector('.movie-desc').textContent = slide.dataset.desc;
            heroInfo.style.opacity = 1;
        }, 300);
    }

    heroBg.style.filter = 'brightness(0.7) blur(0.5px)';
}


// Khi Swiper bắt đầu chuyển slide
movieSwiper.on('slideChangeTransitionStart', function () {
    var heroInfo = document.getElementById('hero-info');
    var heroBg = document.getElementById('hero-bg');
    heroInfo.style.opacity = 0;
    heroBg.style.filter = 'brightness(0.5) blur(1px)';
});

// Khi Swiper chuyển slide xong
movieSwiper.on('slideChangeTransitionEnd', function () {
    updateHeroContentByIndex(movieSwiper.realIndex);
});

// Khi click vào slide, chuyển đến slide đó
document.querySelectorAll('.movie-swiper .swiper-slide').forEach((slide) => {
    slide.addEventListener('click', function () {
        const realIndex = parseInt(this.dataset.swiperSlideIndex);
        console.log('Clicked slide realIndex:', realIndex);
        movieSwiper.slideToLoop(realIndex);
    });
});

//DEBUG INDEX
//         movieSwiper.on('slideChangeTransitionStart', function () {
//     console.log('Before Slide Change – Active realIndex:', movieSwiper.realIndex);
// });

// movieSwiper.on('slideChangeTransitionEnd', function () {
//     console.log('After Slide Change – New realIndex:', movieSwiper.realIndex);
//     updateHeroContentByIndex(movieSwiper.realIndex);
// });

// Lần đầu load cũng cập nhật đúng info
updateHeroContentByIndex(movieSwiper.realIndex);

var promoSwiper = new Swiper('.promo-swiper', {
    slidesPerView: 4,
    spaceBetween: 30,
    navigation: {
        nextEl: '.promo-next',
        prevEl: '.promo-prev',
    },
    pagination: {
        el: '.promo-pagination',
        clickable: true,
    },
    breakpoints: {
        0: { slidesPerView: 1 },
        576: { slidesPerView: 2 },
        992: { slidesPerView: 4 }
    }
});

