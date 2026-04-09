document.addEventListener('DOMContentLoaded', () => {
    const cartContainer = document.querySelector('.cart-container');
    if (!cartContainer) return;
    
    // Add CSRF token for fetch requests
    const getToken = () => {
        const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
        return tokenInput ? tokenInput.value : '';
    };

    cartContainer.addEventListener('click', async (e) => {
        // Increment / Decrement
        const qtyBtn = e.target.closest('[data-action="increment"]') || e.target.closest('[data-action="decrement"]');
        if (qtyBtn) {
            e.preventDefault();
            const action = qtyBtn.dataset.action;
            const itemId = qtyBtn.dataset.itemId;
            const input = document.querySelector(`input[data-item-id="${itemId}"]`);
            
            let currentVal = parseInt(input.value) || 0;
            if (action === 'increment') {
                currentVal += 1;
            } else if (action === 'decrement' && currentVal > 1) {
                currentVal -= 1;
            } else {
                return; // don't drop below 1
            }
            
            input.value = currentVal;
            await updateQuantity(itemId, currentVal);
            return;
        }

        // Remove
        const removeBtn = e.target.closest('[data-action="remove"]');
        if (removeBtn) {
            e.preventDefault();
            const itemId = removeBtn.dataset.itemId;
            if (confirm("Are you sure you want to remove this item?")) {
                await removeItem(itemId);
            }
        }
    });
    
    // Support typing into the quantity input directly
    cartContainer.addEventListener('change', async (e) => {
        if (e.target.classList.contains('qty-input')) {
            const input = e.target;
            const itemId = input.dataset.itemId;
            let currentVal = parseInt(input.value);
            
            if (isNaN(currentVal) || currentVal < 1) {
                currentVal = 1;
                input.value = 1;
            }
            
            await updateQuantity(itemId, currentVal);
        }
    });

    async function updateQuantity(itemId, quantity) {
        try {
            const response = await fetch('?handler=UpdateQuantityAjax', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': getToken()
                },
                body: JSON.stringify({ cartItemId: parseInt(itemId), quantity: parseInt(quantity) })
            });
            
            if (response.ok) {
                const data = await response.json();
                if (data.success) {
                    updateSummaryDOM(data);
                    
                    const itemTotalEl = document.querySelector(`.item-total[data-item-id="${itemId}"]`);
                    if (itemTotalEl && data.itemTotal !== undefined) {
                        itemTotalEl.innerText = formatVND(data.itemTotal);
                    }
                } else {
                    alert(data.message || 'Error updating quantity');
                }
            } else {
                alert('Server connection failed.');
            }
        } catch (error) {
            console.error('Error:', error);
        }
    }

    async function removeItem(itemId) {
        try {
            const response = await fetch('?handler=RemoveItemAjax', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': getToken()
                },
                body: JSON.stringify({ cartItemId: parseInt(itemId) })
            });

            if (response.ok) {
                const data = await response.json();
                if (data.success) {
                    const itemCard = document.querySelector(`.cart-item-card[data-item-id="${itemId}"]`);
                    if (itemCard) itemCard.remove();
                    
                    updateSummaryDOM(data);
                    
                    const cartCountEl = document.getElementById('cart-items-count');
                    if (cartCountEl && data.itemCount !== undefined) {
                        cartCountEl.innerText = `${data.itemCount} items`;
                    }
                    
                    // If cart is empty, reload page to show empty state
                    if (data.itemCount === 0) {
                        window.location.reload();
                    }
                } else {
                    alert(data.message || 'Error removing item');
                }
            } else {
                 alert('Server connection failed.');
            }
        } catch (error) {
            console.error('Error:', error);
        }
    }

    function updateSummaryDOM(data) {
        const subtotalEl = document.getElementById('summary-subtotal');
        if (subtotalEl && data.subtotal !== undefined) subtotalEl.innerText = formatVND(data.subtotal);
        
        const prescriptionRow = document.getElementById('summary-prescription-row');
        const prescriptionVal = document.getElementById('summary-prescription-val');
        
        if (prescriptionRow && prescriptionVal && data.prescriptionFees !== undefined) {
            if (data.prescriptionFees > 0) {
                prescriptionRow.style.display = 'flex';
                prescriptionVal.innerText = formatVND(data.prescriptionFees);
            } else {
                prescriptionRow.style.display = 'none';
            }
        }
        
        const totalEl = document.getElementById('summary-total');
        if (totalEl && data.total !== undefined) totalEl.innerText = formatVND(data.total);
    }

    function formatVND(amount) {
        return amount.toLocaleString('vi-VN') + ' VND';
    }
});
