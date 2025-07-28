$(document).ready(() => {
    console.log('app.js loaded');
    $('#hamburger-menu').click(() => {
        $('#hamburger-menu').toggleClass('active')
        $('.nav-menu').toggleClass('active')
    })

    $('body').css('padding-top', $('.nav-wrapper').innerHeight())

    window.addEventListener('resize', () => {
        $('body').css('padding-top', $('.nav-wrapper').innerHeight())
    })

    // set owl carousel cho các phần khác ngoài hero section
    let navText = ["<i class='bx bx-chevron-left'></i>", "<i class='bx bx-chevron-right'></i>"]

    $('#top-movies-slide').owlCarousel({
        items: 2,
        dots: false,
        loop: true,
        autoplay: true,
        autoplayHoverPause: true,
        margin: 15,
        responsive: {
            500: {
                items: 3
            },
            1280: {
                items: 4
            },
            1600: {
                items: 6
            }
        }
    })

    $('.movies-slide').owlCarousel({
        items: 2,
        dots: false,
        nav: true,
        navText: navText,
        margin: 15,
        responsive: {
            500: {
                items: 2
            },
            1280: {
                items: 4
            },
            1600: {
                items: 6
            }
        }
    })

    // Hiệu ứng ripple cho nút .btn-hover
    $(document).on('click', '.btn-hover', function(e) {
        const btn = $(this);
        // Xóa ripple cũ nếu có
        btn.find('.ripple').remove();
        // Tính vị trí click
        const offset = btn.offset();
        const x = e.pageX - offset.left;
        const y = e.pageY - offset.top;
        // Tạo ripple
        const ripple = $('<span class="ripple"></span>');
        ripple.css({
            left: x + 'px',
            top: y + 'px',
            width: btn.outerWidth(),
            height: btn.outerWidth()
        });
        btn.append(ripple);
        // Xóa ripple sau khi animation xong
        setTimeout(() => ripple.remove(), 500);
    });
});

// Khởi tạo Swiper cho slide 2 (Now Showing)
document.addEventListener('DOMContentLoaded', function() {
    if (typeof Swiper !== 'undefined') {
        new Swiper('.nowshowing-swiper', {
            slidesPerView: 4,
            slidesPerGroup: 4,
            spaceBetween: 16,
            loop: true,
            autoplay: false,
            navigation: {
                nextEl: '.swiper-button-next',
                prevEl: '.swiper-button-prev',
            },
            breakpoints: {
                480: {
                    slidesPerView: 1,
                    slidesPerGroup: 1,
                    spaceBetween: 10,
                },
                640: {
                    slidesPerView: 2,
                    slidesPerGroup: 2,
                    spaceBetween: 16,
                },
                900: {
                    slidesPerView: 3,
                    slidesPerGroup: 3,
                    spaceBetween: 18,
                },
                1200: {
                    slidesPerView: 4,
                    slidesPerGroup: 4,
                    spaceBetween: 20,
                },
            }
        });
    }
});