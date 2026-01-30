/**
 * Lens Admin Module
 * Handles CRUD operations, filtering, pagination, and image uploads
 */
const LensAdmin = (function () {
    'use strict';

    // API endpoints
    const API = {
        lenses: '/api/lenses',
        images: '/api/images'
    };

    // State
    let currentPage = 1;
    let pageSize = 12;
    let totalPages = 1;
    let filters = {
        search: '',
        lensType: '',
        stockStatus: '',
        coating: '',
        priceMin: null,
        priceMax: null,
        sortBy: 'createdAt',
        sortOrder: 'desc'
    };
    let showInactive = false;
    let uploadedImages = [];

    // DOM Elements cache
    let elements = {};

    // ==========================================
    // INITIALIZATION
    // ==========================================

    function init(options = {}) {
        cacheElements();
        bindEvents();

        if (options.page === 'index') {
            loadLenses();
        } else if (options.page === 'create') {
            initCreateForm();
        } else if (options.page === 'edit' && options.lensId) {
            initEditForm(options.lensId);
        } else if (options.page === 'details' && options.lensId) {
            loadLensDetails(options.lensId);
        }
    }

    function cacheElements() {
        elements = {
            lensGrid: document.getElementById('lensGrid'),
            searchInput: document.getElementById('searchInput'),
            lensTypeFilter: document.getElementById('lensTypeFilter'),
            stockStatusFilter: document.getElementById('stockStatusFilter'),
            sortBySelect: document.getElementById('sortBySelect'),
            showInactiveToggle: document.getElementById('showInactiveToggle'),
            pagination: document.getElementById('pagination'),
            paginationInfo: document.getElementById('paginationInfo'),
            lensForm: document.getElementById('lensForm'),
            imageUploadZone: document.getElementById('imageUploadZone'),
            imageInput: document.getElementById('imageInput'),
            imagePreviewGrid: document.getElementById('imagePreviewGrid'),
            loadingOverlay: document.getElementById('loadingOverlay'),
            toastContainer: document.getElementById('toastContainer'),
            deleteModal: document.getElementById('deleteModal')
        };
    }

    function bindEvents() {
        // Search with debounce
        if (elements.searchInput) {
            elements.searchInput.addEventListener('input', debounce(handleSearch, 300));
        }

        // Filters
        if (elements.lensTypeFilter) {
            elements.lensTypeFilter.addEventListener('change', handleFilterChange);
        }
        if (elements.stockStatusFilter) {
            elements.stockStatusFilter.addEventListener('change', handleFilterChange);
        }
        if (elements.sortBySelect) {
            elements.sortBySelect.addEventListener('change', handleSortChange);
        }
        if (elements.showInactiveToggle) {
            elements.showInactiveToggle.addEventListener('change', handleInactiveToggle);
        }

        // Form submission
        if (elements.lensForm) {
            elements.lensForm.addEventListener('submit', handleFormSubmit);
        }

        // Image upload
        if (elements.imageUploadZone) {
            setupImageUpload();
        }
    }

    // ==========================================
    // API FUNCTIONS
    // ==========================================

    async function apiRequest(url, options = {}) {
        try {
            showLoading();
            const response = await fetch(url, {
                headers: {
                    'Content-Type': 'application/json',
                    ...options.headers
                },
                ...options
            });

            if (!response.ok) {
                const errorData = await response.text();
                throw new Error(errorData || `HTTP ${response.status}`);
            }

            // Handle 204 No Content
            if (response.status === 204) {
                return null;
            }

            return await response.json();
        } catch (error) {
            console.error('API Error:', error);
            showToast('error', error.message || 'An error occurred');
            throw error;
        } finally {
            hideLoading();
        }
    }

    async function fetchLenses() {
        const params = new URLSearchParams({
            pageNumber: currentPage,
            pageSize: pageSize,
            sortBy: filters.sortBy,
            sortOrder: filters.sortOrder
        });

        if (filters.search) params.append('search', filters.search);
        if (filters.lensType) params.append('lensType', filters.lensType);
        if (filters.stockStatus) params.append('stockStatus', filters.stockStatus);
        if (filters.coating) params.append('coating', filters.coating);
        if (filters.priceMin) params.append('priceMin', filters.priceMin);
        if (filters.priceMax) params.append('priceMax', filters.priceMax);

        return await apiRequest(`${API.lenses}?${params}`);
    }

    async function fetchLensById(id) {
        return await apiRequest(`${API.lenses}/${id}`);
    }

    async function createLens(data) {
        return await apiRequest(API.lenses, {
            method: 'POST',
            body: JSON.stringify(data)
        });
    }

    async function updateLens(id, data) {
        return await apiRequest(`${API.lenses}/${id}`, {
            method: 'PUT',
            body: JSON.stringify(data)
        });
    }

    async function deleteLens(id) {
        return await apiRequest(`${API.lenses}/${id}`, {
            method: 'DELETE'
        });
    }

    async function restoreLens(id) {
        return await apiRequest(`${API.lenses}/${id}`, {
            method: 'PUT',
            body: JSON.stringify({ stockStatus: 'in-stock' })
        });
    }

    async function fetchLensImages(lensId) {
        return await apiRequest(`${API.images}?context=lens:${lensId}`);
    }

    async function uploadImage(file, lensId = null) {
        const formData = new FormData();
        formData.append('file', file);
        formData.append('imageType', 'lens');
        if (lensId) {
            formData.append('context', `lens:${lensId}`);
        }

        showLoading();
        try {
            const response = await fetch(`${API.images}/upload`, {
                method: 'POST',
                body: formData
            });

            if (!response.ok) {
                const errorData = await response.text();
                throw new Error(errorData || 'Upload failed');
            }

            return await response.json();
        } catch (error) {
            console.error('Upload Error:', error);
            showToast('error', error.message || 'Upload failed');
            throw error;
        } finally {
            hideLoading();
        }
    }

    async function updateImageContext(imageId, context) {
        return await fetch(`${API.images}/${imageId}/context`, {
            method: 'PATCH',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(context)
        });
    }

    async function deleteImage(imageId) {
        return await apiRequest(`${API.images}/${imageId}`, {
            method: 'DELETE'
        });
    }

    // ==========================================
    // INDEX PAGE - LIST LENSES
    // ==========================================

    async function loadLenses() {
        try {
            const data = await fetchLenses();

            // Fetch images for each lens
            const lensesWithImages = await Promise.all(
                data.items.map(async (lens) => {
                    try {
                        const images = await fetch(`${API.images}?context=lens:${lens.lensId}`).then(r => r.ok ? r.json() : []);
                        lens.imageUrl = images && images.length > 0 ? images[0].imageUrl : null;
                    } catch {
                        lens.imageUrl = null;
                    }
                    return lens;
                })
            );

            renderLensGrid(lensesWithImages);
            renderPagination(data);
        } catch (error) {
            if (elements.lensGrid) {
                elements.lensGrid.innerHTML = renderEmptyState('Error loading lenses', error.message);
            }
        }
    }

    function renderLensGrid(lenses) {
        if (!elements.lensGrid) return;

        if (!lenses || lenses.length === 0) {
            elements.lensGrid.innerHTML = renderEmptyState('No lenses found', 'Try adjusting your filters or add a new lens.');
            return;
        }

        elements.lensGrid.innerHTML = lenses.map(lens => renderLensCard(lens)).join('');

        // Bind card action events
        elements.lensGrid.querySelectorAll('[data-action]').forEach(btn => {
            btn.addEventListener('click', handleCardAction);
        });
    }

    function renderLensCard(lens) {
        const isInactive = lens.stockStatus === 'out-of-stock';
        const statusClass = lens.stockStatus ? lens.stockStatus.replace(' ', '-') : '';

        return `
            <div class="lens-card ${isInactive ? 'inactive' : ''}" data-id="${lens.lensId}">
                ${lens.stockStatus ? `<span class="status-badge ${statusClass}">${formatStockStatus(lens.stockStatus)}</span>` : ''}
                <div class="lens-card-image">
                    ${lens.imageUrl
                ? `<img src="${lens.imageUrl}" alt="${escapeHtml(lens.lensType)}" onerror="this.parentElement.innerHTML='<span class=\\'placeholder-icon\\'>🔍</span>'">`
                : '<span class="placeholder-icon">🔍</span>'
            }
                </div>
                <div class="lens-card-body">
                    <h3 class="lens-card-title">${escapeHtml(lens.lensType)}</h3>
                    <p class="lens-card-subtitle">${lens.coating ? escapeHtml(lens.coating) : 'No coating'}</p>
                    <div class="lens-card-specs">
                        ${lens.indexValue ? `<span class="lens-card-spec">Index: ${lens.indexValue}</span>` : ''}
                    </div>
                    <div class="lens-card-price">$${formatPrice(lens.price)}</div>
                    <div class="lens-card-actions">
                        <a href="/Admin/Lenses/Details?id=${lens.lensId}" class="btn btn-outline-primary">View</a>
                        <a href="/Admin/Lenses/Edit?id=${lens.lensId}" class="btn btn-primary">Edit</a>
                        ${isInactive
                ? `<button class="btn btn-secondary" data-action="restore" data-id="${lens.lensId}">Restore</button>`
                : `<button class="btn btn-outline-primary" data-action="delete" data-id="${lens.lensId}">Delete</button>`
            }
                    </div>
                </div>
            </div>
        `;
    }

    function renderEmptyState(title, description) {
        return `
            <div class="empty-state">
                <div class="empty-state-icon">📦</div>
                <h3 class="empty-state-title">${title}</h3>
                <p class="empty-state-description">${description}</p>
                <a href="/Admin/Lenses/Create" class="btn btn-primary">Add New Lens</a>
            </div>
        `;
    }

    function renderPagination(data) {
        if (!elements.pagination) return;

        totalPages = data.totalPages;
        currentPage = data.pageNumber;

        if (totalPages <= 1) {
            elements.pagination.innerHTML = '';
            if (elements.paginationInfo) {
                elements.paginationInfo.textContent = `Showing ${data.items.length} of ${data.totalCount} items`;
            }
            return;
        }

        let paginationHtml = `
            <button class="pagination-btn" onclick="LensAdmin.goToPage(${currentPage - 1})" ${currentPage <= 1 ? 'disabled' : ''}>
                ← Previous
            </button>
        `;

        // Page numbers
        const startPage = Math.max(1, currentPage - 2);
        const endPage = Math.min(totalPages, currentPage + 2);

        if (startPage > 1) {
            paginationHtml += `<button class="pagination-btn" onclick="LensAdmin.goToPage(1)">1</button>`;
            if (startPage > 2) paginationHtml += `<span class="pagination-info">...</span>`;
        }

        for (let i = startPage; i <= endPage; i++) {
            paginationHtml += `
                <button class="pagination-btn ${i === currentPage ? 'active' : ''}" onclick="LensAdmin.goToPage(${i})">
                    ${i}
                </button>
            `;
        }

        if (endPage < totalPages) {
            if (endPage < totalPages - 1) paginationHtml += `<span class="pagination-info">...</span>`;
            paginationHtml += `<button class="pagination-btn" onclick="LensAdmin.goToPage(${totalPages})">${totalPages}</button>`;
        }

        paginationHtml += `
            <button class="pagination-btn" onclick="LensAdmin.goToPage(${currentPage + 1})" ${currentPage >= totalPages ? 'disabled' : ''}>
                Next →
            </button>
        `;

        elements.pagination.innerHTML = paginationHtml;

        if (elements.paginationInfo) {
            const start = (currentPage - 1) * pageSize + 1;
            const end = Math.min(currentPage * pageSize, data.totalCount);
            elements.paginationInfo.textContent = `Showing ${start}-${end} of ${data.totalCount} items`;
        }
    }

    function goToPage(page) {
        if (page < 1 || page > totalPages) return;
        currentPage = page;
        loadLenses();
        window.scrollTo({ top: 0, behavior: 'smooth' });
    }

    // ==========================================
    // EVENT HANDLERS
    // ==========================================

    function handleSearch(event) {
        filters.search = event.target.value;
        currentPage = 1;
        loadLenses();
    }

    function handleFilterChange(event) {
        const filterName = event.target.dataset.filter || event.target.id.replace('Filter', '');
        filters[filterName] = event.target.value;
        currentPage = 1;
        loadLenses();
    }

    function handleSortChange(event) {
        const value = event.target.value;
        const [sortBy, sortOrder] = value.split('-');
        filters.sortBy = sortBy;
        filters.sortOrder = sortOrder || 'asc';
        currentPage = 1;
        loadLenses();
    }

    function handleInactiveToggle(event) {
        showInactive = event.target.checked;
        if (showInactive) {
            filters.stockStatus = 'out-of-stock';
        } else {
            filters.stockStatus = '';
        }
        currentPage = 1;
        loadLenses();
    }

    async function handleCardAction(event) {
        const action = event.target.dataset.action;
        const id = event.target.dataset.id;

        if (action === 'delete') {
            showDeleteModal(id);
        } else if (action === 'restore') {
            try {
                const lens = await fetchLensById(id);
                await updateLens(id, {
                    lensType: lens.lensType,
                    indexValue: lens.indexValue,
                    coating: lens.coating,
                    price: lens.price,
                    stockStatus: 'in-stock'
                });
                showToast('success', 'Lens restored successfully');
                loadLenses();
            } catch (error) {
                showToast('error', 'Failed to restore lens');
            }
        }
    }

    async function handleFormSubmit(event) {
        event.preventDefault();

        if (!validateForm()) {
            return;
        }

        const formData = new FormData(event.target);
        const lensData = {
            lensType: formData.get('lensType'),
            indexValue: formData.get('indexValue') ? parseFloat(formData.get('indexValue')) : null,
            coating: formData.get('coating') || null,
            price: parseFloat(formData.get('price')),
            stockStatus: formData.get('stockStatus') || 'in-stock'
        };

        const lensId = formData.get('lensId');

        try {
            let savedLens;
            if (lensId) {
                savedLens = await updateLens(lensId, lensData);
                showToast('success', 'Lens updated successfully');
            } else {
                savedLens = await createLens(lensData);
                showToast('success', 'Lens created successfully');

                // Link uploaded images to the new lens
                if (uploadedImages.length > 0) {
                    for (const img of uploadedImages) {
                        await updateImageContext(img.imageId, `lens:${savedLens.lensId}`);
                    }
                }
            }

            // Redirect to list after short delay
            setTimeout(() => {
                window.location.href = '/Admin/Lenses';
            }, 1000);
        } catch (error) {
            // Error already shown by apiRequest
        }
    }

    // ==========================================
    // FORM HANDLING
    // ==========================================

    function initCreateForm() {
        uploadedImages = [];
    }

    async function initEditForm(lensId) {
        try {
            const lens = await fetchLensById(lensId);
            populateForm(lens);

            // Load existing images
            const images = await fetchLensImages(lensId);
            if (images && images.length > 0) {
                uploadedImages = images;
                renderImagePreviews();
            }
        } catch (error) {
            showToast('error', 'Failed to load lens data');
        }
    }

    function populateForm(lens) {
        const form = elements.lensForm;
        if (!form) return;

        form.querySelector('[name="lensId"]').value = lens.lensId;
        form.querySelector('[name="lensType"]').value = lens.lensType || '';
        form.querySelector('[name="indexValue"]').value = lens.indexValue || '';
        form.querySelector('[name="coating"]').value = lens.coating || '';
        form.querySelector('[name="price"]').value = lens.price || '';
        form.querySelector('[name="stockStatus"]').value = lens.stockStatus || 'in-stock';
    }

    function validateForm() {
        const form = elements.lensForm;
        if (!form) return false;

        let isValid = true;

        // Clear previous errors
        form.querySelectorAll('.form-group').forEach(group => {
            group.classList.remove('has-error');
        });

        // Lens Type - required
        const lensType = form.querySelector('[name="lensType"]');
        if (!lensType.value.trim()) {
            showFieldError(lensType, 'Lens type is required');
            isValid = false;
        }

        // Index Value - optional but must be between 1.0 and 2.0
        const indexValue = form.querySelector('[name="indexValue"]');
        if (indexValue.value) {
            const val = parseFloat(indexValue.value);
            if (isNaN(val) || val < 1.0 || val > 2.0) {
                showFieldError(indexValue, 'Index value must be between 1.0 and 2.0');
                isValid = false;
            }
        }

        // Price - required and > 0
        const price = form.querySelector('[name="price"]');
        if (!price.value || parseFloat(price.value) <= 0) {
            showFieldError(price, 'Price must be greater than 0');
            isValid = false;
        }

        return isValid;
    }

    function showFieldError(input, message) {
        const group = input.closest('.form-group');
        group.classList.add('has-error');
        const errorEl = group.querySelector('.error-message');
        if (errorEl) {
            errorEl.textContent = message;
        }
    }

    // ==========================================
    // IMAGE UPLOAD
    // ==========================================

    function setupImageUpload() {
        const zone = elements.imageUploadZone;
        const input = elements.imageInput;

        // Click to upload
        zone.addEventListener('click', () => input.click());

        // Drag and drop
        zone.addEventListener('dragover', (e) => {
            e.preventDefault();
            zone.classList.add('drag-over');
        });

        zone.addEventListener('dragleave', () => {
            zone.classList.remove('drag-over');
        });

        zone.addEventListener('drop', async (e) => {
            e.preventDefault();
            zone.classList.remove('drag-over');
            const files = e.dataTransfer.files;
            await handleFileUpload(files);
        });

        // File input change
        input.addEventListener('change', async (e) => {
            await handleFileUpload(e.target.files);
            input.value = ''; // Reset input
        });
    }

    async function handleFileUpload(files) {
        for (const file of files) {
            // Validate file type
            const validTypes = ['image/jpeg', 'image/png', 'image/webp'];
            if (!validTypes.includes(file.type)) {
                showToast('error', `Invalid file type: ${file.name}`);
                continue;
            }

            // Validate file size (5MB)
            if (file.size > 5 * 1024 * 1024) {
                showToast('error', `File too large: ${file.name}`);
                continue;
            }

            try {
                const result = await uploadImage(file);
                uploadedImages.push(result);
                renderImagePreviews();
                showToast('success', 'Image uploaded successfully');
            } catch (error) {
                // Error already shown
            }
        }
    }

    function renderImagePreviews() {
        if (!elements.imagePreviewGrid) return;

        elements.imagePreviewGrid.innerHTML = uploadedImages.map((img, index) => `
            <div class="image-preview-item" data-index="${index}">
                <img src="${img.imageUrl}" alt="${img.altText || 'Lens image'}">
                <button type="button" class="remove-btn" onclick="LensAdmin.removeImage(${index})">×</button>
                ${index === 0 ? '<span class="primary-badge">Primary</span>' : ''}
            </div>
        `).join('');
    }

    async function removeImage(index) {
        const image = uploadedImages[index];
        try {
            await deleteImage(image.imageId);
            uploadedImages.splice(index, 1);
            renderImagePreviews();
            showToast('success', 'Image removed');
        } catch (error) {
            showToast('error', 'Failed to remove image');
        }
    }

    // ==========================================
    // DETAILS PAGE
    // ==========================================

    async function loadLensDetails(lensId) {
        try {
            const lens = await fetchLensById(lensId);
            renderLensDetails(lens);

            const images = await fetchLensImages(lensId);
            renderDetailsGallery(images);
        } catch (error) {
            showToast('error', 'Failed to load lens details');
        }
    }

    function renderLensDetails(lens) {
        const detailsInfo = document.getElementById('detailsInfo');
        if (!detailsInfo) return;

        detailsInfo.innerHTML = `
            <h1 class="details-title">${escapeHtml(lens.lensType)}</h1>
            <div class="details-price">$${formatPrice(lens.price)}</div>
            <div class="details-specs">
                <div class="details-spec-row">
                    <span class="details-spec-label">Lens Type</span>
                    <span class="details-spec-value">${escapeHtml(lens.lensType)}</span>
                </div>
                <div class="details-spec-row">
                    <span class="details-spec-label">Index Value</span>
                    <span class="details-spec-value">${lens.indexValue || 'N/A'}</span>
                </div>
                <div class="details-spec-row">
                    <span class="details-spec-label">Coating</span>
                    <span class="details-spec-value">${lens.coating ? escapeHtml(lens.coating) : 'None'}</span>
                </div>
                <div class="details-spec-row">
                    <span class="details-spec-label">Stock Status</span>
                    <span class="details-spec-value">
                        <span class="status-badge ${lens.stockStatus || ''}">${formatStockStatus(lens.stockStatus)}</span>
                    </span>
                </div>
                <div class="details-spec-row">
                    <span class="details-spec-label">Created</span>
                    <span class="details-spec-value">${formatDate(lens.createdAt)}</span>
                </div>
            </div>
            <div class="form-actions">
                <a href="/Admin/Lenses/Edit?id=${lens.lensId}" class="btn btn-primary">Edit Lens</a>
                <button class="btn btn-outline-primary" onclick="LensAdmin.confirmDelete(${lens.lensId})">Delete</button>
                <a href="/Admin/Lenses" class="btn btn-secondary">Back to List</a>
            </div>
        `;
    }

    function renderDetailsGallery(images) {
        const gallery = document.getElementById('detailsGallery');
        const mainImage = document.getElementById('detailsMainImage');
        const thumbnails = document.getElementById('detailsThumbnails');

        if (!gallery || !mainImage) return;

        if (!images || images.length === 0) {
            mainImage.innerHTML = '<span class="placeholder-icon" style="font-size: 5rem;">🔍</span>';
            if (thumbnails) thumbnails.style.display = 'none';
            return;
        }

        // Set main image
        mainImage.innerHTML = `<img src="${images[0].imageUrl}" alt="${images[0].altText || 'Lens image'}">`;

        // Render thumbnails
        if (thumbnails && images.length > 1) {
            thumbnails.innerHTML = images.map((img, index) => `
                <div class="details-thumbnail ${index === 0 ? 'active' : ''}" onclick="LensAdmin.selectImage('${img.imageUrl}', this)">
                    <img src="${img.imageUrl}" alt="${img.altText || ''}">
                </div>
            `).join('');
        }
    }

    function selectImage(imageUrl, thumbnail) {
        const mainImage = document.getElementById('detailsMainImage');
        if (mainImage) {
            mainImage.innerHTML = `<img src="${imageUrl}" alt="Lens image">`;
        }

        // Update active thumbnail
        document.querySelectorAll('.details-thumbnail').forEach(t => t.classList.remove('active'));
        thumbnail.classList.add('active');
    }

    // ==========================================
    // DELETE MODAL
    // ==========================================

    let deleteTargetId = null;

    function showDeleteModal(id) {
        deleteTargetId = id;
        const modal = elements.deleteModal || document.getElementById('deleteModal');
        if (modal) {
            modal.classList.add('active');
        }
    }

    function hideDeleteModal() {
        deleteTargetId = null;
        const modal = elements.deleteModal || document.getElementById('deleteModal');
        if (modal) {
            modal.classList.remove('active');
        }
    }

    function confirmDelete(id = null) {
        const targetId = id || deleteTargetId;
        showDeleteModal(targetId);
    }

    async function executeDelete() {
        if (!deleteTargetId) return;

        try {
            await deleteLens(deleteTargetId);
            showToast('success', 'Lens deleted successfully');
            hideDeleteModal();

            // Redirect or refresh
            if (window.location.pathname.includes('/Details') || window.location.pathname.includes('/Edit')) {
                setTimeout(() => {
                    window.location.href = '/Admin/Lenses';
                }, 1000);
            } else {
                loadLenses();
            }
        } catch (error) {
            showToast('error', 'Failed to delete lens');
        }
    }

    // ==========================================
    // UI UTILITIES
    // ==========================================

    function showLoading() {
        if (elements.loadingOverlay) {
            elements.loadingOverlay.style.display = 'flex';
        }
    }

    function hideLoading() {
        if (elements.loadingOverlay) {
            elements.loadingOverlay.style.display = 'none';
        }
    }

    function showToast(type, message) {
        let container = elements.toastContainer || document.getElementById('toastContainer');

        if (!container) {
            container = document.createElement('div');
            container.id = 'toastContainer';
            container.className = 'toast-container';
            document.body.appendChild(container);
            elements.toastContainer = container;
        }

        const toast = document.createElement('div');
        toast.className = `toast ${type}`;
        toast.innerHTML = `
            <span>${message}</span>
            <button class="close-btn" onclick="this.parentElement.remove()">×</button>
        `;

        container.appendChild(toast);

        // Auto-remove after 5 seconds
        setTimeout(() => {
            toast.remove();
        }, 5000);
    }

    // ==========================================
    // HELPER FUNCTIONS
    // ==========================================

    function debounce(func, wait) {
        let timeout;
        return function executedFunction(...args) {
            const later = () => {
                clearTimeout(timeout);
                func(...args);
            };
            clearTimeout(timeout);
            timeout = setTimeout(later, wait);
        };
    }

    function escapeHtml(text) {
        if (!text) return '';
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    function formatPrice(price) {
        return parseFloat(price).toLocaleString('en-US', {
            minimumFractionDigits: 2,
            maximumFractionDigits: 2
        });
    }

    function formatStockStatus(status) {
        if (!status) return 'Unknown';
        return status.split('-').map(word =>
            word.charAt(0).toUpperCase() + word.slice(1)
        ).join(' ');
    }

    function formatDate(dateString) {
        if (!dateString) return 'N/A';
        return new Date(dateString).toLocaleDateString('en-US', {
            year: 'numeric',
            month: 'long',
            day: 'numeric',
            hour: '2-digit',
            minute: '2-digit'
        });
    }

    // ==========================================
    // PUBLIC API
    // ==========================================

    return {
        init,
        loadLenses,
        goToPage,
        removeImage,
        selectImage,
        confirmDelete,
        executeDelete,
        hideDeleteModal,
        showToast
    };
})();

// Export for global access
window.LensAdmin = LensAdmin;
