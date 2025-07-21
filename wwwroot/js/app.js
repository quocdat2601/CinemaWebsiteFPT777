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

    // set owl carousel
    let navText = ["<i class='bx bx-chevron-left'></i>", "<i class='bx bx-chevron-right'></i>"]

    $('#hero-carousel').owlCarousel({
        items: 1,
        dots: false,
        loop: true,
        nav: true,
        navText: navText,
        autoplay: true,
        autoplayHoverPause: true
    })

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
        nav:true,
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

    // XÓA PHẦN NÀY VÌ ĐÃ CHUYỂN SANG SWIPER
    // $('#nowshowing-carousel').owlCarousel({
    //     items: 4,
    //     dots: false,
    //     nav: true,
    //     navText: navText,
    //     margin: 15,
    //     loop: true,
    //     autoplay: true,
    //     autoplayHoverPause: true,
    //     responsive: {
    //         0: { items: 1 },
    //         600: { items: 2 },
    //         1000: { items: 4 }
    //     }
    // });
})