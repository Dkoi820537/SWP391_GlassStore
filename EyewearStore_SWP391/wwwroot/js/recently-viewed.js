document.addEventListener("DOMContentLoaded", function () {
    const STORAGE_KEY = 'optiplus_recently_viewed';
    const MAX_ITEMS = 5;

    // 1. Record the view if we are on a product page
    if (window.recentlyViewedProduct) {
        let history = [];
        try {
            const stored = localStorage.getItem(STORAGE_KEY);
            if (stored) {
                history = JSON.parse(stored);
            }
        } catch (e) {
            console.error('Error reading localStorage', e);
        }

        // Remove if it already exists to move it to the front
        history = history.filter(p => p.id !== window.recentlyViewedProduct.id);

        // Add to front
        history.unshift(window.recentlyViewedProduct);

        // Cap at MAX_ITEMS
        if (history.length > MAX_ITEMS) {
            history = history.slice(0, MAX_ITEMS);
        }

        try {
            localStorage.setItem(STORAGE_KEY, JSON.stringify(history));
        } catch (e) {
            console.error('Error writing to localStorage', e);
        }
    }

    // 2. Render the UI if there is a container
    const container = document.getElementById('recently-viewed-container');
    const section = document.getElementById('recently-viewed-section') || document.getElementById('recently-viewed');

    if (container) {
        let history = [];
        try {
            const stored = localStorage.getItem(STORAGE_KEY);
            if (stored) {
                history = JSON.parse(stored);
            }
        } catch (e) {
            console.error('Error reading localStorage for rendering', e);
        }

        if (history && history.length > 0) {
            // Force flex layout onto the root container
            container.style.cssText = 'display: flex; flex-direction: row; gap: 20px; overflow-x: auto; padding-bottom: 16px;';

            // Build the HTML
            let html = '';
            history.forEach(item => {
                let imgHtml = '';
                if (item.imageUrl) {
                    imgHtml = `<img src="${item.imageUrl}" alt="${item.name}" style="width: 100%; height: 100%; object-fit: contain; background: transparent; border: none;" />`;
                } else {
                    imgHtml = `<svg width="40" height="40" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1"><rect x="2" y="8" width="8" height="6" rx="2"/><rect x="14" y="8" width="8" height="6" rx="2"/><path d="M10 11h4"/></svg>`;
                }

                // White box sized up proportionally for vertical card layout
                const innerWhiteBox = `
                    <div style="width: 200px; height: 240px; background-color: #ffffff; border-radius: 8px; display: flex; align-items: center; justify-content: center; overflow: hidden; padding: 10px;">
                        ${imgHtml}
                    </div>
                `;

                // Render price and currency if available
                let priceHtml = '';
                if (item.price && item.currency) {
                    priceHtml = `<p style="font-size: 16px; font-weight: 700; color: var(--shop-primary-teal, #4a7c7e); margin: 0 0 12px;">${item.currency} ${item.price}</p>`;
                }

                // Dynamically build proper route linking to handle old products in array + new
                let detailLink = item.productUrl || '/Products/Details/' + item.id;
                if (!item.productUrl) {
                    if (item.categoryLink && item.categoryLink.includes('Frame')) {
                        detailLink = '/Products/FrameDetails/' + item.id;
                    } else if (item.categoryLink && item.categoryLink.includes('Lenses')) {
                        detailLink = '/Products/LensDetails/' + item.id;
                    }
                }

                html += `
                <div class="favorite-card" style="width: 220px; flex-shrink: 0; display: flex; flex-direction: column; text-align: center;">
                    <div class="favorite-image" style="width: 220px; height: 260px; background-color: var(--shop-primary-teal, #4a7c7e); border-radius: 12px; display: flex; align-items: center; justify-content: center; overflow: hidden; margin-bottom: 16px; padding: 10px; box-sizing: border-box;">
                        ${innerWhiteBox}
                    </div>
                    <h4 style="font-size: 16px; font-weight: bold; margin: 0 0 8px; white-space: nowrap; overflow: hidden; text-overflow: ellipsis;" title="${item.name}">${item.name}</h4>
                    ${priceHtml}
                    <a href="${detailLink}" style="display: inline-block; align-self: center; margin: 0 auto; font-size: 14px; font-weight: 600; color: white; background-color: var(--shop-primary-teal, #4a7c7e); padding: 8px 24px; border-radius: 6px; text-decoration: none; transition: opacity 0.2s;">View Detail</a>
                </div>`;
            });

            container.innerHTML = html;

            if (section) {
                section.style.display = 'block';
            }
        } else {
            if (section) {
                section.style.display = 'none';
            }
        }
    }
});
