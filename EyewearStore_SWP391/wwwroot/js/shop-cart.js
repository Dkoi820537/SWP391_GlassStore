/**
 * EyewearStore - Shop Cart Actions
 * Handles AJAX Add to Cart, Remove Item, Clear Cart, Update Quantity, and User Feedback
 */

document.addEventListener('DOMContentLoaded', () => {
    // Initialize verification token for AJAX requests
    const csrfToken = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

    // ─── Add to Cart ────────────────────────────────────────────────────────────

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

    // ─── Remove Single Cart Item ─────────────────────────────────────────────────

    async function removeCartItem(cartItemId, btn) {
        if (!cartItemId || btn.disabled) return;

        btn.disabled = true;

        // Dim item row as visual feedback
        const row = btn.closest('.cart-item-row');
        if (row) row.style.opacity = '0.45';

        try {
            const response = await fetch('/Products/Index?handler=RemoveCartItem', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': csrfToken
                },
                body: JSON.stringify({ cartItemId })
            });

            if (response.status === 401) {
                window.location.href = `/Account/Login?returnUrl=${encodeURIComponent(window.location.pathname)}`;
                return;
            }

            const result = await response.json();

            if (result.success) {
                // Animate row out
                if (row) {
                    row.style.transition = 'opacity 0.25s ease, max-height 0.3s ease, margin 0.3s ease';
                    row.style.overflow = 'hidden';
                    row.style.maxHeight = row.offsetHeight + 'px';
                    requestAnimationFrame(() => {
                        row.style.opacity = '0';
                        row.style.maxHeight = '0';
                        row.style.marginBottom = '0';
                        row.style.paddingTop = '0';
                        row.style.paddingBottom = '0';
                    });
                    setTimeout(() => row.remove(), 300);
                }

                updateCartBadge(result.cartCount);

                // Give animation time to start before rebuilding
                setTimeout(() => {
                    rebuildCartDropdown(result.cartItems, result.cartCount, result.subtotal);
                }, 310);
            } else {
                // Restore row state on error
                if (row) row.style.opacity = '1';
                btn.disabled = false;
                showToast(result.message || 'Failed to remove item.', 'error');
            }
        } catch (error) {
            console.error('Error removing cart item:', error);
            if (row) row.style.opacity = '1';
            btn.disabled = false;
            showToast('Something went wrong. Please try again.', 'error');
        }
    }

    // ─── Update Cart Item Quantity ───────────────────────────────────────────────

    let _qtyDebounce = {};

    async function updateCartItemQty(cartItemId, newQty, btn) {
        if (!cartItemId || newQty < 1) return;

        // Debounce rapid clicks per item (150ms)
        if (_qtyDebounce[cartItemId]) {
            clearTimeout(_qtyDebounce[cartItemId]);
        }

        // Disable both +/- buttons for this item during request
        const row = btn?.closest('.cart-item-row');
        const minusBtn = row?.querySelector('.btn-cart-qty-minus');
        const plusBtn = row?.querySelector('.btn-cart-qty-plus');
        const qtyDisplay = row?.querySelector('.cart-qty-display');

        // Optimistic UI update: show new quantity immediately
        if (qtyDisplay) {
            qtyDisplay.textContent = newQty;
            qtyDisplay.style.transition = 'transform 0.15s ease';
            qtyDisplay.style.transform = 'scale(1.15)';
            setTimeout(() => { qtyDisplay.style.transform = 'scale(1)'; }, 150);
        }

        // Update minus button disabled state optimistically
        if (minusBtn) {
            minusBtn.disabled = newQty <= 1;
            minusBtn.style.opacity = newQty <= 1 ? '0.4' : '1';
            minusBtn.style.cursor = newQty <= 1 ? 'not-allowed' : 'pointer';
        }

        _qtyDebounce[cartItemId] = setTimeout(async () => {
            // Disable buttons during server call
            if (minusBtn) minusBtn.disabled = true;
            if (plusBtn) plusBtn.disabled = true;

            try {
                const response = await fetch('/Products/Index?handler=UpdateCartItemQty', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'RequestVerificationToken': csrfToken
                    },
                    body: JSON.stringify({ cartItemId, newQuantity: newQty })
                });

                if (response.status === 401) {
                    window.location.href = `/Account/Login?returnUrl=${encodeURIComponent(window.location.pathname)}`;
                    return;
                }

                const result = await response.json();

                if (result.success) {
                    updateCartBadge(result.cartCount);
                    rebuildCartDropdown(result.cartItems, result.cartCount, result.subtotal);
                } else {
                    showToast(result.message || 'Failed to update quantity.', 'error');
                    // Re-enable buttons on error
                    if (minusBtn) { minusBtn.disabled = false; minusBtn.style.opacity = '1'; }
                    if (plusBtn) plusBtn.disabled = false;
                }
            } catch (error) {
                console.error('Error updating cart item qty:', error);
                showToast('Something went wrong. Please try again.', 'error');
                if (minusBtn) { minusBtn.disabled = false; minusBtn.style.opacity = '1'; }
                if (plusBtn) plusBtn.disabled = false;
            }
        }, 200);
    }

    // ─── Clear All Cart Items ────────────────────────────────────────────────────

    async function clearCart(btn) {
        if (!confirm('Are you sure you want to remove all items from your cart?')) return;

        if (btn) btn.disabled = true;

        try {
            const response = await fetch('/Products/Index?handler=ClearCart', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': csrfToken
                }
            });

            if (response.status === 401) {
                window.location.href = `/Account/Login?returnUrl=${encodeURIComponent(window.location.pathname)}`;
                return;
            }

            const result = await response.json();

            if (result.success) {
                updateCartBadge(0);
                rebuildCartDropdown([], 0, 0);
            } else {
                if (btn) btn.disabled = false;
                showToast(result.message || 'Failed to clear cart.', 'error');
            }
        } catch (error) {
            console.error('Error clearing cart:', error);
            if (btn) btn.disabled = false;
            showToast('Something went wrong. Please try again.', 'error');
        }
    }

    // ─── Cart Badge ──────────────────────────────────────────────────────────────

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

    // ─── Rebuild Cart Dropdown ───────────────────────────────────────────────────

    /**
     * Rebuilds the cart dropdown HTML using cart item data from the server response.
     * Avoids a second AJAX call and ensures the dropdown always reflects
     * the latest cart state immediately.
     */
    function rebuildCartDropdown(cartItems, itemCount, subtotal) {
        // Find the dropdown menu in the current page
        const dropdownMenu = document.querySelector('#cartDropdown')
            ?.closest('.dropdown')
            ?.querySelector('.dropdown-menu');

        if (!dropdownMenu) return;

        // Format number with thousands separator (e.g., 1,200,000)
        const formatPrice = (num) => Math.round(num).toLocaleString('en-US');

        // Trash SVG icon (inline, no external dependency)
        const trashIcon = `<svg xmlns="http://www.w3.org/2000/svg" width="13" height="13" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
            <polyline points="3 6 5 6 21 6"></polyline>
            <path d="M19 6l-1 14H6L5 6"></path>
            <path d="M10 11v6M14 11v6"></path>
            <path d="M9 6V4h6v2"></path>
        </svg>`;

        let html = '';

        // Header row
        const clearAllBtn = itemCount > 0
            ? `<button type="button" class="btn-cart-clear btn btn-sm btn-outline-danger py-0 px-2" style="font-size:11px; border-radius:4px;" aria-label="Clear all items from cart">Clear All</button>`
            : '';

        html += `<li>
            <div class="d-flex justify-content-between align-items-center mb-2">
                <strong>Cart</strong>
                <div class="d-flex align-items-center gap-2">
                    ${clearAllBtn}
                    <small class="text-muted">${itemCount} item(s)</small>
                </div>
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
            html += `<li><div style="max-height:300px; overflow-y:auto; overflow-x:visible; padding-right:4px;">`;
            for (const item of cartItems) {
                const imgHtml = item.imageUrl
                    ? `<img src="${item.imageUrl}" alt="Product" style="width:100%; height:100%; object-fit:cover;" />`
                    : `<span>IMG</span>`;

                const minusDisabled = item.quantity <= 1;
                const minusStyle = minusDisabled ? 'opacity:0.4; cursor:not-allowed;' : '';

                html += `<div class="cart-item-row mb-2"
                              data-cart-item-id="${item.cartItemId}"
                              data-unit-price="${item.unitPrice}"
                              style="transition: opacity 0.25s ease, max-height 0.3s ease; display:flex; gap:10px; align-items:flex-start; padding:6px 0; border-bottom:1px solid #f0f0f0;">
                    <div style="width:50px; height:50px; background:#f5f5f5; border-radius:6px; display:flex; align-items:center; justify-content:center; font-size:11px; color:#999; overflow:hidden; flex-shrink:0;">
                        ${imgHtml}
                    </div>
                    <div class="flex-grow-1 min-w-0" style="display:flex; flex-direction:column; gap:2px;">
                        <div style="display:flex; align-items:center; gap:6px;">
                            <div class="fw-semibold text-truncate" style="flex:1; font-size:13px;">${item.name}</div>
                            <div class="fw-bold flex-shrink-0 cart-item-line-total" style="font-size:13px; white-space:nowrap;">${formatPrice(item.lineTotal)} VND</div>
                            <button type="button"
                                    class="btn-cart-remove btn btn-sm p-0"
                                    style="width:24px; height:24px; display:flex; align-items:center; justify-content:center; border-radius:4px; color:#9CA3AF; border:1px solid #e5e7eb; background:transparent; outline:none; box-shadow:none; flex-shrink:0;"
                                    data-cart-item-id="${item.cartItemId}"
                                    aria-label="Remove ${item.name} from cart"
                                    title="Remove">
                                ${trashIcon}
                            </button>
                        </div>
                        <div class="small text-muted" style="font-size:11px;">${formatPrice(item.unitPrice)} VND each</div>
                        <div style="display:flex; align-items:center; margin-top:2px;">
                            <div class="cart-qty-group" style="display:inline-flex; border:1px solid #D1D5DB; border-radius:6px; overflow:hidden; height:26px;">
                                <button type="button"
                                        class="btn-cart-qty-minus"
                                        style="width:26px; height:100%; display:flex; align-items:center; justify-content:center; border:none; background:#fff; color:#6B7280; cursor:pointer; font-size:14px; font-weight:600; transition:background .15s; outline:none; ${minusStyle}"
                                        data-cart-item-id="${item.cartItemId}"
                                        ${minusDisabled ? 'disabled' : ''}
                                        aria-label="Decrease quantity of ${item.name}">
                                    &minus;
                                </button>
                                <span class="cart-qty-display"
                                      style="width:34px; height:100%; display:flex; align-items:center; justify-content:center; background:#F3F4F6; font-size:13px; font-weight:700; color:#374151; border-left:1px solid #D1D5DB; border-right:1px solid #D1D5DB; user-select:none;"
                                      aria-label="Quantity: ${item.quantity}">${item.quantity}</span>
                                <button type="button"
                                        class="btn-cart-qty-plus"
                                        style="width:26px; height:100%; display:flex; align-items:center; justify-content:center; border:none; background:#fff; color:#6B7280; cursor:pointer; font-size:14px; font-weight:600; transition:background .15s; outline:none;"
                                        data-cart-item-id="${item.cartItemId}"
                                        aria-label="Increase quantity of ${item.name}">
                                    +
                                </button>
                            </div>
                        </div>
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

        // Re-attach event listeners after rebuilding the DOM
        attachCartDropdownListeners(dropdownMenu);
    }

    // ─── Event Delegation ────────────────────────────────────────────────────────

    /**
     * Attaches click listeners on the dropdown for remove, clear, and quantity buttons.
     * Called once on page load and again after every rebuildCartDropdown().
     */
    function attachCartDropdownListeners(menu) {
        if (!menu) return;

        // Remove individual item
        menu.querySelectorAll('.btn-cart-remove').forEach(btn => {
            btn.addEventListener('click', function (e) {
                e.preventDefault();
                e.stopPropagation(); // keep Bootstrap dropdown open
                const cartItemId = parseInt(this.dataset.cartItemId, 10);
                removeCartItem(cartItemId, this);
            });
        });

        // Decrease quantity
        menu.querySelectorAll('.btn-cart-qty-minus').forEach(btn => {
            btn.addEventListener('click', function (e) {
                e.preventDefault();
                e.stopPropagation();
                const cartItemId = parseInt(this.dataset.cartItemId, 10);
                const row = this.closest('.cart-item-row');
                const qtyDisplay = row?.querySelector('.cart-qty-display');
                const currentQty = parseInt(qtyDisplay?.textContent, 10) || 1;
                if (currentQty > 1) {
                    updateCartItemQty(cartItemId, currentQty - 1, this);
                }
            });
        });

        // Increase quantity
        menu.querySelectorAll('.btn-cart-qty-plus').forEach(btn => {
            btn.addEventListener('click', function (e) {
                e.preventDefault();
                e.stopPropagation();
                const cartItemId = parseInt(this.dataset.cartItemId, 10);
                const row = this.closest('.cart-item-row');
                const qtyDisplay = row?.querySelector('.cart-qty-display');
                const currentQty = parseInt(qtyDisplay?.textContent, 10) || 1;
                if (currentQty < 99) {
                    updateCartItemQty(cartItemId, currentQty + 1, this);
                }
            });
        });

        // Clear all
        const clearBtn = menu.querySelector('.btn-cart-clear');
        if (clearBtn) {
            clearBtn.addEventListener('click', function (e) {
                e.preventDefault();
                e.stopPropagation(); // keep Bootstrap dropdown open
                clearCart(this);
            });
        }
    }

    // Attach on initial page load
    const initialMenu = document.querySelector('#cartDropdown')
        ?.closest('.dropdown')
        ?.querySelector('.dropdown-menu');
    attachCartDropdownListeners(initialMenu);

    // ─── Hover styles ────────────────────────────────────────────────────────────

    // Remove button hover: turn red
    document.addEventListener('mouseover', function (e) {
        const btn = e.target.closest('.btn-cart-remove');
        if (btn) {
            btn.style.color = '#EF4444';
            btn.style.borderColor = '#EF4444';
        }
    });
    document.addEventListener('mouseout', function (e) {
        const btn = e.target.closest('.btn-cart-remove');
        if (btn && !btn.disabled) {
            btn.style.color = '#9CA3AF';
            btn.style.borderColor = '#e5e7eb';
        }
    });

    // Quantity +/- button hover: darken background
    document.addEventListener('mouseover', function (e) {
        const btn = e.target.closest('.btn-cart-qty-minus, .btn-cart-qty-plus');
        if (btn && !btn.disabled) {
            btn.style.background = '#E5E7EB';
        }
    });
    document.addEventListener('mouseout', function (e) {
        const btn = e.target.closest('.btn-cart-qty-minus, .btn-cart-qty-plus');
        if (btn) {
            btn.style.background = '#fff';
        }
    });

    // ─── Toast Notification System ───────────────────────────────────────────────
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
