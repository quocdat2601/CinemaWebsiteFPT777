// Text Overflow Handler for Ticket Booking Confirmed Page
document.addEventListener('DOMContentLoaded', function() {
    
    // Function to check if text is overflowing
    function isTextOverflowing(element) {
        return element.scrollWidth > element.clientWidth || 
               element.scrollHeight > element.clientHeight;
    }
    
    // Function to add tooltip if text is overflowing
    function addTooltipIfNeeded(element) {
        const text = element.textContent.trim();
        if (text && isTextOverflowing(element)) {
            element.setAttribute('title', text);
            element.classList.add('has-tooltip');
        }
    }
    
    // Apply to all info-value elements
    const infoValues = document.querySelectorAll('.info-value');
    infoValues.forEach(addTooltipIfNeeded);
    
    // Enhanced tooltip functionality
    const tooltipElements = document.querySelectorAll('.info-value[title]');
    
    tooltipElements.forEach(element => {
        element.addEventListener('mouseenter', function(e) {
            const title = this.getAttribute('title');
            if (!title) return;
            
            // Remove existing tooltip
            const existingTooltip = document.querySelector('.custom-tooltip');
            if (existingTooltip) {
                existingTooltip.remove();
            }
            
            // Create custom tooltip
            const tooltip = document.createElement('div');
            tooltip.className = 'custom-tooltip';
            tooltip.textContent = title;
            tooltip.style.cssText = `
                position: fixed;
                background: rgba(0, 0, 0, 0.9);
                color: white;
                padding: 8px 12px;
                border-radius: 6px;
                font-size: 14px;
                z-index: 10000;
                max-width: 300px;
                word-wrap: break-word;
                white-space: normal;
                box-shadow: 0 4px 12px rgba(0, 0, 0, 0.3);
                pointer-events: none;
                opacity: 0;
                transition: opacity 0.2s ease;
            `;
            
            document.body.appendChild(tooltip);
            
            // Position tooltip
            const rect = this.getBoundingClientRect();
            const tooltipRect = tooltip.getBoundingClientRect();
            
            let left = rect.left + (rect.width / 2) - (tooltipRect.width / 2);
            let top = rect.top - tooltipRect.height - 8;
            
            // Adjust if tooltip goes off screen
            if (left < 10) left = 10;
            if (left + tooltipRect.width > window.innerWidth - 10) {
                left = window.innerWidth - tooltipRect.width - 10;
            }
            if (top < 10) {
                top = rect.bottom + 8;
            }
            
            tooltip.style.left = left + 'px';
            tooltip.style.top = top + 'px';
            
            // Show tooltip
            setTimeout(() => {
                tooltip.style.opacity = '1';
            }, 10);
        });
        
        element.addEventListener('mouseleave', function() {
            const tooltip = document.querySelector('.custom-tooltip');
            if (tooltip) {
                tooltip.style.opacity = '0';
                setTimeout(() => {
                    tooltip.remove();
                }, 200);
            }
        });
    });
    
    // Handle window resize
    window.addEventListener('resize', function() {
        // Re-check overflow after resize
        const infoValues = document.querySelectorAll('.info-value');
        infoValues.forEach(addTooltipIfNeeded);
    });
}); 