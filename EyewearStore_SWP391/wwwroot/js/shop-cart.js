/**
 * EyewearStore - Shop Cart Actions
 * Handles AJAX Add to Cart, Buy Now, and User Feedback
 */

document.addEventListener('DOMContentLoaded', () => {
    // Initialize verification token for AJAX requests
    const csrfToken = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

    window.addToCart = async function (button, productId, isBuyNow = false) {
        if (!button) return;

        // Prevent double clicks
        if (button.disabled) return;

        // Save original button state
        const originalContent = button.innerHTML;
        const originalWidth = button.offsetWidth;

        // Set loading state
        button.disabled = true;
        button.innerHTML = '<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Adding...';
        button.style.width = `${originalWidth}px`; // Prevent layout shift

        try {
            const response = await fetch('/Products/Index?handler=AddToCart', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': csrfToken
                },
                body: JSON.stringify({
                    productId: productId,
                    quantity: 1
                })
            });

            if (response.status === 401) {
                // User not logged in
                window.location.href = `/Account/Login?returnUrl=${encodeURIComponent(window.location.pathname)}`;
                return;
            }

            const result = await response.json();

            if (result.success) {
                // Update Cart Badge
                updateCartBadge(result.cartCount);

                // Show Success Toast
                showToast(result.message || 'Item added to cart!', 'success');

                // Handle Buy Now redirect
                if (isBuyNow) {
                    window.location.href = '/Cart/Index'; // Or direct to checkout
                }
            } else {
                // Show Error Toast
                showToast(result.message || 'Failed to add item.', 'error');
            }
        } catch (error) {
            console.error('Error adding to cart:', error);
            showToast('Something went wrong. Please try again.', 'error');
        } finally {
            // Restore button state
            if (!isBuyNow) {
                setTimeout(() => {
                    button.disabled = false;
                    button.innerHTML = originalContent;
                }, 500);
            }
        }
    };

    function updateCartBadge(count) {
        const badges = document.querySelectorAll('.cart-badge');
        badges.forEach(badge => {
            badge.innerText = count;
            badge.classList.remove('d-none');
            // Add pulse animation class
            badge.classList.add('pulse-animation');
            setTimeout(() => badge.classList.remove('pulse-animation'), 1000);
        });
    }

    // Toast Notification System
    function showToast(message, type = 'success') {
        // Remove existing toast
        const existingToast = document.getElementById('shop-toast');
        if (existingToast) existingToast.remove();

        // Create new toast container
        const toast = document.createElement('div');
        toast.id = 'shop-toast';
        toast.className = `toast-notification ${type}`;

        const icon = type === 'success' ? 'bi-check-circle-fill' : 'bi-exclamation-circle-fill';

        toast.innerHTML = `
            <div class="toast-content">
                <i class="bi ${icon} toast-icon"></i>
                <span class="toast-message">${message}</span>
            </div>
            ${type === 'success' ? `
            <div class="toast-actions">
                <a href="/Cart/Index" class="btn-toast-action">View Cart</a>
            </div>` : ''}
            <button class="toast-close" onclick="this.parentElement.remove()">&times;</button>
        `;

        document.body.appendChild(toast);

        // Trigger animation
        requestAnimationFrame(() => {
            toast.classList.add('show');
        });

        // Auto dismiss
        setTimeout(() => {
            if (toast && toast.parentElement) {
                toast.classList.remove('show');
                setTimeout(() => toast.remove(), 300);
            }
        }, 5000);
    }
});
