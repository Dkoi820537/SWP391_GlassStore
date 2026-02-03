/**
 * Lens Admin Module
 * Handles CRUD operations, filtering, pagination, and image uploads
 * Updated for new TPT schema with Product base entity
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
        isActive: null,
        isPrescription: null,
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
        } else if (options.page === 'edit' && options.productId) {
            initEditForm(options.productId);
        } else if (options.page === 'details' && options.productId) {
            loadLensDetails(options.productId);
        }
    }

    function cacheElements() {
        elements = {
            lensGrid: document.getElementById('lensGrid'),
            searchInput: document.getElementById('searchInput'),
            lensTypeFilter: document.getElementById('lensTypeFilter'),
            isActiveFilter: document.getElementById('isActiveFilter'),
            isPrescriptionFilter: document.getElementById('isPrescriptionFilter'),
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
        if (elements.isActiveFilter) {
            elements.isActiveFilter.addEventListener('change', handleFilterChange);
        }
        if (elements.isPrescriptionFilter) {
            elements.isPrescriptionFilter.addEventListener('change', handleFilterChange);
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
        if (filters.isActive !== null) params.append('isActive', filters.isActive);
        if (filters.isPrescription !== null) params.append('isPrescription', filters.isPrescription);
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
        const lens = await fetchLensById(id);
        return await apiRequest(`${API.lenses}/${id}`, {
            method: 'PUT',
            body: JSON.stringify({
                sku: lens.sku,
                name: lens.name,
                description: lens.description,
                price: lens.price,
                currency: lens.currency,
                inventoryQty: lens.inventoryQty,
                isActive: true,
                lensType: lens.lensType,
                lensIndex: lens.lensIndex,
                isPrescription: lens.isPrescription
            })
        });
    }

    async function fetchLensImages(productId) {
        return await apiRequest(`${API.images}/product/${productId}`);
    }

    async function uploadImage(file, productId = null, isPrimary = false) {
        const formData = new FormData();
        formData.append('file', file);
        if (productId) {
            formData.append('productId', productId);
        }
        formData.append('isPrimary', isPrimary);

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

    async function setImageAsPrimary(imageId) {
        return await fetch(`${API.images}/${imageId}/set-primary`, {
            method: 'PATCH'
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
            renderLensGrid(data.items);
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
        const isInactive = !lens.isActive;
        const statusClass = lens.isActive ? 'active' : 'inactive';

        return `
            <div class="lens-card ${isInactive ? 'inactive' : ''}" data-id="${lens.productId}">
                <span class="status-badge ${statusClass}">${lens.isActive ? 'Active' : 'Inactive'}</span>
                <div class="lens-card-image">
                    ${lens.primaryImageUrl
                ? `<img src="${lens.primaryImageUrl}" alt="${escapeHtml(lens.name)}" onerror="this.parentElement.innerHTML='<span class=\\'placeholder-icon\\'>🔍</span>'">`
                : '<span class="placeholder-icon">🔍</span>'
            }
                </div>
                <div class="lens-card-body">
                    <h3 class="lens-card-title">${escapeHtml(lens.name)}</h3>
                    <p class="lens-card-subtitle">${lens.lensType ? escapeHtml(lens.lensType) : 'No type specified'}</p>
                    <div class="lens-card-specs">
                        ${lens.lensIndex ? `<span class="lens-card-spec">Index: ${lens.lensIndex}</span>` : ''}
                        ${lens.isPrescription ? '<span class="lens-card-spec">Rx Required</span>' : ''}
                    </div>
                    <div class="lens-card-price">${formatPrice(lens.price)} ${lens.currency}</div>
                    <div class="lens-card-actions">
                        <a href="/Admin/Lenses/Details?id=${lens.productId}" class="btn btn-outline-primary">View</a>
                        <a href="/Admin/Lenses/Edit?id=${lens.productId}" class="btn btn-primary">Edit</a>
                        ${isInactive
                ? `<button class="btn btn-secondary" data-action="restore" data-id="${lens.productId}">Restore</button>`
                : `<button class="btn btn-outline-primary" data-action="delete" data-id="${lens.productId}">Delete</button>`
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
        const value = event.target.value;

        // Handle boolean filters
        if (filterName === 'isActive' || filterName === 'isPrescription') {
            filters[filterName] = value === '' ? null : value === 'true';
        } else {
            filters[filterName] = value;
        }

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
            filters.isActive = false;
        } else {
            filters.isActive = null;
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
                await restoreLens(id);
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
            sku: formData.get('sku'),
            name: formData.get('name'),
            description: formData.get('description') || null,
            price: parseFloat(formData.get('price')),
            currency: formData.get('currency') || 'VND',
            inventoryQty: formData.get('inventoryQty') ? parseInt(formData.get('inventoryQty')) : null,
            isActive: formData.get('isActive') === 'true',
            lensType: formData.get('lensType') || null,
            lensIndex: formData.get('lensIndex') ? parseFloat(formData.get('lensIndex')) : null,
            isPrescription: formData.get('isPrescription') === 'true'
        };

        const productId = formData.get('productId');

        try {
            let savedLens;
            if (productId) {
                savedLens = await updateLens(productId, lensData);
                showToast('success', 'Lens updated successfully');
            } else {
                savedLens = await createLens(lensData);
                showToast('success', 'Lens created successfully');

                // Upload pending images for the new lens
                if (uploadedImages.length > 0) {
                    for (let i = 0; i < uploadedImages.length; i++) {
                        const img = uploadedImages[i];
                        await uploadImage(img.file, savedLens.productId, i === 0);
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

    async function initEditForm(productId) {
        try {
            const lens = await fetchLensById(productId);
            populateForm(lens);

            // Load existing images
            const images = await fetchLensImages(productId);
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

        form.querySelector('[name="productId"]').value = lens.productId;
        form.querySelector('[name="sku"]').value = lens.sku || '';
        form.querySelector('[name="name"]').value = lens.name || '';
        form.querySelector('[name="description"]').value = lens.description || '';
        form.querySelector('[name="price"]').value = lens.price || '';
        form.querySelector('[name="currency"]').value = lens.currency || 'VND';
        form.querySelector('[name="inventoryQty"]').value = lens.inventoryQty || '';
        form.querySelector('[name="isActive"]').value = lens.isActive ? 'true' : 'false';
        form.querySelector('[name="lensType"]').value = lens.lensType || '';
        form.querySelector('[name="lensIndex"]').value = lens.lensIndex || '';
        form.querySelector('[name="isPrescription"]').value = lens.isPrescription ? 'true' : 'false';
    }

    function validateForm() {
        const form = elements.lensForm;
        if (!form) return false;

        let isValid = true;

        // Clear previous errors
        form.querySelectorAll('.form-group').forEach(group => {
            group.classList.remove('has-error');
        });

        // SKU - required
        const sku = form.querySelector('[name="sku"]');
        if (!sku.value.trim()) {
            showFieldError(sku, 'SKU is required');
            isValid = false;
        }

        // Name - required
        const name = form.querySelector('[name="name"]');
        if (!name.value.trim()) {
            showFieldError(name, 'Name is required');
            isValid = false;
        }

        // Price - required and > 0
        const price = form.querySelector('[name="price"]');
        if (!price.value || parseFloat(price.value) <= 0) {
            showFieldError(price, 'Price must be greater than 0');
            isValid = false;
        }

        // Lens Index - optional but must be between 1.0 and 2.0
        const lensIndex = form.querySelector('[name="lensIndex"]');
        if (lensIndex.value) {
            const val = parseFloat(lensIndex.value);
            if (isNaN(val) || val < 1.0 || val > 2.0) {
                showFieldError(lensIndex, 'Lens index must be between 1.0 and 2.0');
                isValid = false;
            }
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
        const formData = new FormData(elements.lensForm);
        const productId = formData.get('productId');

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
                if (productId) {
                    // If editing, upload immediately
                    const result = await uploadImage(file, productId, uploadedImages.length === 0);
                    uploadedImages.push(result);
                    renderImagePreviews();
                    showToast('success', 'Image uploaded successfully');
                } else {
                    // If creating, store file for later upload
                    const previewUrl = URL.createObjectURL(file);
                    uploadedImages.push({ file, imageUrl: previewUrl, isPending: true });
                    renderImagePreviews();
                }
            } catch (error) {
                // Error already shown
            }
        }
    }

    function renderImagePreviews() {
        if (!elements.imagePreviewGrid) return;

        elements.imagePreviewGrid.innerHTML = uploadedImages.map((img, index) => `
            <div class="image-preview-item ${img.isPending ? 'pending' : ''}" data-index="${index}">
                <img src="${img.imageUrl}" alt="${img.altText || 'Lens image'}">
                <button type="button" class="remove-btn" onclick="LensAdmin.removeImage(${index})">×</button>
                ${img.isPrimary || index === 0 ? '<span class="primary-badge">Primary</span>' : ''}
                ${img.isPending ? '<span class="pending-badge">Pending</span>' : ''}
            </div>
        `).join('');
    }

    async function removeImage(index) {
        const image = uploadedImages[index];
        try {
            if (!image.isPending && image.imageId) {
                await deleteImage(image.imageId);
            }
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

    async function loadLensDetails(productId) {
        try {
            const lens = await fetchLensById(productId);
            renderLensDetails(lens);

            const images = await fetchLensImages(productId);
            renderDetailsGallery(images);
        } catch (error) {
            showToast('error', 'Failed to load lens details');
        }
    }

    function renderLensDetails(lens) {
        const detailsInfo = document.getElementById('detailsInfo');
        if (!detailsInfo) return;

        detailsInfo.innerHTML = `
            <h1 class="details-title">${escapeHtml(lens.name)}</h1>
            <div class="details-price">${formatPrice(lens.price)} ${lens.currency}</div>
            <div class="details-specs">
                <div class="details-spec-row">
                    <span class="details-spec-label">SKU</span>
                    <span class="details-spec-value">${escapeHtml(lens.sku)}</span>
                </div>
                <div class="details-spec-row">
                    <span class="details-spec-label">Lens Type</span>
                    <span class="details-spec-value">${lens.lensType ? escapeHtml(lens.lensType) : 'N/A'}</span>
                </div>
                <div class="details-spec-row">
                    <span class="details-spec-label">Lens Index</span>
                    <span class="details-spec-value">${lens.lensIndex || 'N/A'}</span>
                </div>
                <div class="details-spec-row">
                    <span class="details-spec-label">Prescription Required</span>
                    <span class="details-spec-value">${lens.isPrescription ? 'Yes' : 'No'}</span>
                </div>
                <div class="details-spec-row">
                    <span class="details-spec-label">Status</span>
                    <span class="details-spec-value">
                        <span class="status-badge ${lens.isActive ? 'active' : 'inactive'}">${lens.isActive ? 'Active' : 'Inactive'}</span>
                    </span>
                </div>
                <div class="details-spec-row">
                    <span class="details-spec-label">Inventory</span>
                    <span class="details-spec-value">${lens.inventoryQty !== null ? lens.inventoryQty : 'N/A'}</span>
                </div>
                <div class="details-spec-row">
                    <span class="details-spec-label">Created</span>
                    <span class="details-spec-value">${formatDate(lens.createdAt)}</span>
                </div>
                <div class="details-spec-row">
                    <span class="details-spec-label">Updated</span>
                    <span class="details-spec-value">${formatDate(lens.updatedAt)}</span>
                </div>
            </div>
            ${lens.description ? `<div class="details-description"><h3>Description</h3><p>${escapeHtml(lens.description)}</p></div>` : ''}
            <div class="form-actions">
                <a href="/Admin/Lenses/Edit?id=${lens.productId}" class="btn btn-primary">Edit Lens</a>
                <button class="btn btn-outline-primary" onclick="LensAdmin.confirmDelete(${lens.productId})">Delete</button>
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

        // Sort by primary first
        images.sort((a, b) => (b.isPrimary ? 1 : 0) - (a.isPrimary ? 1 : 0));

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
        return parseFloat(price).toLocaleString('vi-VN', {
            minimumFractionDigits: 0,
            maximumFractionDigits: 2
        });
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
