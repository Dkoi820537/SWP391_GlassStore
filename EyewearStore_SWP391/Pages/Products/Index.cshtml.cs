using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using EyewearStore_SWP391.Models;
using EyewearStore_SWP391.Models.ViewModels.Shop;

using System.Security.Claims;

namespace EyewearStore_SWP391.Pages.Products;

/// <summary>
/// Page model for the customer-facing product catalog.
/// Allows browsing all available frames and lenses with filtering, sorting, and pagination.
/// </summary>
public class IndexModel : PageModel
{
    private readonly EyewearStoreContext _context;
    private readonly Services.ICartService _cartService;
    private const int DefaultPageSize = 12;

    public IndexModel(EyewearStoreContext context, Services.ICartService cartService)
    {
        _context = context;
        _cartService = cartService;
    }

    /// <summary>
    /// The view model containing product catalog data
    /// </summary>
    public ProductCatalogViewModel Catalog { get; set; } = new();

    // Filter properties with SupportsGet = true for query string binding

    /// <summary>
    /// Product type filter: "All", "Frame", or "Lens"
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string ProductTypeFilter { get; set; } = "All";

    /// <summary>
    /// Search term to filter by name, description, or SKU
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    /// <summary>
    /// Minimum price filter
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public decimal? MinPrice { get; set; }

    /// <summary>
    /// Maximum price filter
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public decimal? MaxPrice { get; set; }

    /// <summary>
    /// Frame material filter
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string? FrameMaterialFilter { get; set; }

    /// <summary>
    /// Frame type filter
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string? FrameTypeFilter { get; set; }

    /// <summary>
    /// Lens type filter
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string? LensTypeFilter { get; set; }

    /// <summary>
    /// Sort option: "name", "price-low", "price-high", "newest"
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string SortBy { get; set; } = "name";

    /// <summary>
    /// Current page number (1-indexed)
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public int CurrentPage { get; set; } = 1;

    /// <summary>
    /// Number of items per page
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public int PageSize { get; set; } = DefaultPageSize;

    /// <summary>
    /// Handles GET request - loads products with filtering, sorting, and pagination
    /// </summary>
    public async Task<IActionResult> OnGetAsync()
    {
        // Ensure page values are valid
        if (CurrentPage < 1) CurrentPage = 1;
        if (PageSize < 1 || PageSize > 48) PageSize = DefaultPageSize;

        // Build combined query for both Frames and Lenses
        var productList = new List<ProductCatalogItemViewModel>();

        // Determine which product types to query based on filter
        bool includeFrames = ProductTypeFilter == "All" || ProductTypeFilter == "Frame";
        bool includeLenses = ProductTypeFilter == "All" || ProductTypeFilter == "Lens";

        // Query Frames if applicable
        if (includeFrames)
        {
            var framesQuery = _context.Frames
                .Include(f => f.ProductImages)
                .AsNoTracking()
                .Where(f => f.IsActive); // Only show active products

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var searchLower = SearchTerm.ToLower();
                framesQuery = framesQuery.Where(f =>
                    f.Name.ToLower().Contains(searchLower) ||
                    f.Sku.ToLower().Contains(searchLower) ||
                    (f.Description != null && f.Description.ToLower().Contains(searchLower)));
            }

            // Apply price range filters
            if (MinPrice.HasValue)
            {
                framesQuery = framesQuery.Where(f => f.Price >= MinPrice.Value);
            }
            if (MaxPrice.HasValue)
            {
                framesQuery = framesQuery.Where(f => f.Price <= MaxPrice.Value);
            }

            // Apply frame-specific filters
            if (!string.IsNullOrWhiteSpace(FrameMaterialFilter))
            {
                framesQuery = framesQuery.Where(f => f.FrameMaterial == FrameMaterialFilter);
            }
            if (!string.IsNullOrWhiteSpace(FrameTypeFilter))
            {
                framesQuery = framesQuery.Where(f => f.FrameType == FrameTypeFilter);
            }

            var frames = await framesQuery.ToListAsync();
            productList.AddRange(frames.Select(f => new ProductCatalogItemViewModel
            {
                ProductId = f.ProductId,
                ProductType = "Frame",
                Sku = f.Sku,
                Name = f.Name,
                Description = f.Description,
                Price = f.Price,
                Currency = f.Currency,
                PrimaryImageUrl = f.ProductImages?.FirstOrDefault(i => i.IsPrimary && i.IsActive)?.ImageUrl
                    ?? f.ProductImages?.FirstOrDefault(i => i.IsActive)?.ImageUrl,
                FrameMaterial = f.FrameMaterial,
                FrameType = f.FrameType,
                CreatedAt = f.CreatedAt
            }));
        }

        // Query Lenses if applicable
        if (includeLenses)
        {
            var lensesQuery = _context.Lenses
                .Include(l => l.ProductImages)
                .AsNoTracking()
                .Where(l => l.IsActive); // Only show active products

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var searchLower = SearchTerm.ToLower();
                lensesQuery = lensesQuery.Where(l =>
                    l.Name.ToLower().Contains(searchLower) ||
                    l.Sku.ToLower().Contains(searchLower) ||
                    (l.Description != null && l.Description.ToLower().Contains(searchLower)));
            }

            // Apply price range filters
            if (MinPrice.HasValue)
            {
                lensesQuery = lensesQuery.Where(l => l.Price >= MinPrice.Value);
            }
            if (MaxPrice.HasValue)
            {
                lensesQuery = lensesQuery.Where(l => l.Price <= MaxPrice.Value);
            }

            // Apply lens-specific filters
            if (!string.IsNullOrWhiteSpace(LensTypeFilter))
            {
                lensesQuery = lensesQuery.Where(l => l.LensType == LensTypeFilter);
            }

            var lenses = await lensesQuery.ToListAsync();
            productList.AddRange(lenses.Select(l => new ProductCatalogItemViewModel
            {
                ProductId = l.ProductId,
                ProductType = "Lens",
                Sku = l.Sku,
                Name = l.Name,
                Description = l.Description,
                Price = l.Price,
                Currency = l.Currency,
                PrimaryImageUrl = l.ProductImages?.FirstOrDefault(i => i.IsPrimary && i.IsActive)?.ImageUrl
                    ?? l.ProductImages?.FirstOrDefault(i => i.IsActive)?.ImageUrl,
                LensType = l.LensType,
                LensIndex = l.LensIndex,
                IsPrescription = l.IsPrescription,
                CreatedAt = l.CreatedAt
            }));
        }

        // Apply sorting
        productList = SortBy switch
        {
            "price-low" => productList.OrderBy(p => p.Price).ToList(),
            "price-high" => productList.OrderByDescending(p => p.Price).ToList(),
            "newest" => productList.OrderByDescending(p => p.CreatedAt).ToList(),
            _ => productList.OrderBy(p => p.Name).ToList() // default: name
        };

        // Calculate total count for pagination
        var totalCount = productList.Count;
        var totalPages = (int)Math.Ceiling(totalCount / (double)PageSize);

        // Ensure current page doesn't exceed total pages
        if (CurrentPage > totalPages && totalPages > 0)
        {
            CurrentPage = totalPages;
        }

        // Apply pagination
        var pagedProducts = productList
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize)
            .ToList();

        // Get available filter options
        var availableFrameMaterials = await _context.Frames
            .Where(f => f.IsActive && f.FrameMaterial != null)
            .Select(f => f.FrameMaterial!)
            .Distinct()
            .OrderBy(m => m)
            .ToListAsync();

        var availableFrameTypes = await _context.Frames
            .Where(f => f.IsActive && f.FrameType != null)
            .Select(f => f.FrameType!)
            .Distinct()
            .OrderBy(t => t)
            .ToListAsync();

        var availableLensTypes = await _context.Lenses
            .Where(l => l.IsActive && l.LensType != null)
            .Select(l => l.LensType!)
            .Distinct()
            .OrderBy(t => t)
            .ToListAsync();

        // Populate the view model
        Catalog = new ProductCatalogViewModel
        {
            Products = pagedProducts,
            ProductTypeFilter = ProductTypeFilter,
            SearchTerm = SearchTerm,
            MinPrice = MinPrice,
            MaxPrice = MaxPrice,
            FrameMaterialFilter = FrameMaterialFilter,
            FrameTypeFilter = FrameTypeFilter,
            LensTypeFilter = LensTypeFilter,
            SortBy = SortBy,
            CurrentPage = CurrentPage,
            TotalPages = totalPages,
            PageSize = PageSize,
            TotalItems = totalCount,
            AvailableFrameMaterials = availableFrameMaterials,
            AvailableFrameTypes = availableFrameTypes,
            AvailableLensTypes = availableLensTypes
        };

        return Page();
    }

    /// <summary>
    /// Handles AJAX Add to Cart request
    /// </summary>
    public async Task<IActionResult> OnPostAddToCartAsync([FromBody] AddToCartRequest request)
    {
        if (request == null || request.ProductId <= 0 || request.Quantity <= 0)
        {
            return new JsonResult(new { success = false, message = "Invalid request data." });
        }

        // Check authentication
        if (!User.Identity?.IsAuthenticated ?? true)
        {
            return new JsonResult(new { success = false, message = "Please login to add items to cart." }) { StatusCode = 401 };
        }

        var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId))
        {
            return new JsonResult(new { success = false, message = "User identity not found." }) { StatusCode = 401 };
        }

        try
        {
            // Verify product (check both Frames and Lenses)
            // Note: In a real scenario, we might want a unified Product table,
            // but here we check both. We assume ProductId is unique across both tables or logic handles it.
            // Based on models, they are separate tables but might share ID space if inherited, 
            // but looking at Context they seems separate.
            // Let's check Frame first, then Lens.
            
            int? inventoryQty = 0;
            bool isStockManaged = false;

            var frame = await _context.Frames.FindAsync(request.ProductId);
            if (frame != null)
            {
                inventoryQty = frame.InventoryQty;
                isStockManaged = true;
            }
            else
            {
                var lens = await _context.Lenses.FindAsync(request.ProductId);
                if (lens != null)
                {
                   inventoryQty = lens.InventoryQty;
                   isStockManaged = true;
                }
                else
                {
                    return new JsonResult(new { success = false, message = "Product not found." });
                }
            }

            if (isStockManaged && inventoryQty.HasValue && inventoryQty.Value < request.Quantity)
            {
                 return new JsonResult(new { success = false, message = $"Only {inventoryQty} items left in stock." });
            }

            await _cartService.AddToCartAsync(userId, request.ProductId, request.Quantity);

            // Get updated cart with items for dropdown refresh
            var cart = await _cartService.GetCartByUserIdAsync(userId);
            var newCount = cart?.CartItems.Sum(i => i.Quantity) ?? 0;
            decimal subtotal = 0m;
            var cartItemsList = new List<object>();

            if (cart?.CartItems != null)
            {
                foreach (var ci in cart.CartItems)
                {
                    decimal unitPrice = ci.Product != null ? ci.Product.Price : ci.Service != null ? ci.Service.Price : 0m;
                    string name = ci.Product != null ? ci.Product.Name : ci.Service != null ? ci.Service.Name : "Product";
                    decimal lineTotal = unitPrice * ci.Quantity;
                    subtotal += lineTotal;
                    cartItemsList.Add(new { name, unitPrice, quantity = ci.Quantity, lineTotal });
                }
            }

            return new JsonResult(new { success = true, message = "Added to cart successfully!", cartCount = newCount, cartItems = cartItemsList, subtotal });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { success = false, message = "Error adding to cart: " + ex.Message });
        }
    }

    /// <summary>
    /// Returns the _CartSummary partial view HTML for AJAX cart dropdown refresh
    /// </summary>
    public IActionResult OnGetCartSummaryPartial()
    {
        return Partial("_CartSummary");
    }

    public class AddToCartRequest
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }
}
