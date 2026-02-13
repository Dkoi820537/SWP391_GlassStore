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

                // Rebuild cart dropdown content from response data
                rebuildCartDropdown(result.cartItems, result.cartCount, result.subtotal);

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

    /**
     * Rebuilds the cart dropdown HTML using cart item data from the server response.
     * This avoids a second AJAX call and ensures the dropdown always reflects
     * the latest cart state immediately after add-to-cart.
     */
    function rebuildCartDropdown(cartItems, itemCount, subtotal) {
        // Find the dropdown menu in the current page
        const dropdownMenu = document.querySelector('#cartDropdown')
            ?.closest('.dropdown')
            ?.querySelector('.dropdown-menu');

        if (!dropdownMenu) return;

        // Format number with thousands separator (e.g., 1,200,000)
        const formatPrice = (num) => Math.round(num).toLocaleString('en-US');

        let html = '';

        // Header row
        html += `<li>
            <div class="d-flex justify-content-between align-items-center mb-2">
                <strong>Cart</strong>
                <small class="text-muted">${itemCount} item(s)</small>
            </div>
        </li>`;

        if (!cartItems || cartItems.length === 0) {
            // Empty cart state
            html += `<li class="py-3 text-center text-muted">
                <div>Your cart is empty</div>
                <div class="small">Add products to get started</div>
            </li>`;
        } else {
            // Cart items list
            html += `<li><div style="max-height:240px; overflow:auto;">`;
            for (const item of cartItems) {
                html += `<div class="d-flex align-items-center mb-2">
                    <div style="width:56px; height:56px; background:#f5f5f5; border-radius:6px; display:flex; align-items:center; justify-content:center; font-size:12px; color:#777; margin-right:10px;">
                        IMG
                    </div>
                    <div class="flex-grow-1">
                        <div class="fw-semibold">${item.name}</div>
                        <div class="small text-muted">x ${item.quantity} • ${formatPrice(item.unitPrice)} VND</div>
                    </div>
                    <div class="ms-2 text-end">
                        <div class="fw-bold">${formatPrice(item.lineTotal)} VND</div>
                    </div>
                </div>`;
            }
            html += `</div></li>`;

            // Divider + total + action buttons
            html += `<li><hr class="dropdown-divider" /></li>`;
            html += `<li class="d-flex justify-content-between align-items-center mb-2">
                <div><small class="text-muted">Total</small></div>
                <div class="fw-bold">${formatPrice(subtotal)} VND</div>
            </li>`;
            html += `<li class="d-flex gap-2">
                <a class="btn btn-sm btn-outline-secondary flex-grow-1" href="/Cart/Index">View Cart</a>
                <a class="btn btn-sm btn-primary flex-grow-1" href="/Checkout/Index">Checkout</a>
            </li>`;
        }

        dropdownMenu.innerHTML = html;
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
