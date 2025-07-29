/* ===== ESSENTIAL GLOBAL FUNCTIONALITY ===== */
$(document).ready(() => {
    console.log('app.js loaded');
    
    // Hamburger menu toggle
    $('#hamburger-menu').click(() => {
        $('#hamburger-menu').toggleClass('active')
        $('.nav-menu').toggleClass('active')
    })

    // Set body padding for fixed navbar
    $('body').css('padding-top', $('.nav-wrapper').innerHeight())

    // Update padding on resize
    window.addEventListener('resize', () => {
        $('body').css('padding-top', $('.nav-wrapper').innerHeight())
    })

    // Ripple effect for .btn-hover buttons
    $(document).on('click', '.btn-hover', function(e) {
        const btn = $(this);
        // Remove old ripple if exists
        btn.find('.ripple').remove();
        // Calculate click position
        const offset = btn.offset();
        const x = e.pageX - offset.left;
        const y = e.pageY - offset.top;
        // Create ripple
        const ripple = $('<span class="ripple"></span>');
        ripple.css({
            left: x + 'px',
            top: y + 'px',
            width: btn.outerWidth(),
            height: btn.outerWidth()
        });
        btn.append(ripple);
        // Remove ripple after animation
        setTimeout(() => ripple.remove(), 500);
    });
});

/* ===== GLOBAL NAVBAR FUNCTIONALITY ===== */
document.addEventListener('DOMContentLoaded', function() {
    // Update navbar style based on scroll position
    function updateNavbarStyle() {
        const navbar = document.querySelector('.app-navbar');
        const heroSection = document.querySelector('.hero-section');
        
        if (navbar && heroSection) {
            const heroBottom = heroSection.offsetTop + heroSection.offsetHeight;
            const scrollTop = window.pageYOffset;
            
            if (scrollTop < heroBottom) {
                navbar.classList.add('hero-transparent');
            } else {
                navbar.classList.remove('hero-transparent');
            }
        }
    }

    // Set active navigation items
    function setActiveNavItems() {
        const currentPath = window.location.pathname;
        const navLinks = document.querySelectorAll('.nav-link');
        const logo = document.querySelector('.navbar-brand');
        
        navLinks.forEach(link => {
            if (link.getAttribute('href') === currentPath) {
                link.classList.add('active');
            } else {
                link.classList.remove('active');
            }
        });
        
        // Highlight logo if on home page
        if (currentPath === '/' || currentPath === '/Home' || currentPath === '/Home/Index') {
            logo?.classList.add('home-active');
        } else {
            logo?.classList.remove('home-active');
        }
    }

    // Initialize navbar functionality
    updateNavbarStyle();
    setActiveNavItems();
    
    // Update on scroll
    window.addEventListener('scroll', updateNavbarStyle);
    
    // Update on page load
    window.addEventListener('load', () => {
        updateNavbarStyle();
        setActiveNavItems();
    });
});