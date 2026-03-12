using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using EyewearStore_SWP391.Models;
using EyewearStore_SWP391.Models.ViewModels.Shop;
using System.Security.Claims;

namespace EyewearStore_SWP391.Pages.Products;

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

    public ProductCatalogViewModel Catalog { get; set; } = new();

    // Brand list for mega bar
    public List<BrandItem> AllBrands { get; set; } = new();
    public record BrandItem(string Brand, int Count);

    [BindProperty(SupportsGet = true)]
    public string ProductTypeFilter { get; set; } = "All";

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    [BindProperty(SupportsGet = true)]
    public decimal? MinPrice { get; set; }

    [BindProperty(SupportsGet = true)]
    public decimal? MaxPrice { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? FrameMaterialFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? FrameTypeFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? LensTypeFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? BrandFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? GenderFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? FrameShapeFilter { get; set; }

    [BindProperty(SupportsGet = true)]
    public string SortBy { get; set; } = "name";

    [BindProperty(SupportsGet = true)]
    public int CurrentPage { get; set; } = 1;

    [BindProperty(SupportsGet = true)]
    public int PageSize { get; set; } = DefaultPageSize;

    public async Task<IActionResult> OnGetAsync()
    {
        if (CurrentPage < 1) CurrentPage = 1;
        if (PageSize < 1 || PageSize > 48) PageSize = DefaultPageSize;

        if (!string.IsNullOrWhiteSpace(BrandFilter))
            ProductTypeFilter = "Frame";

        var productList = new List<ProductCatalogItemViewModel>();

        bool includeFrames = ProductTypeFilter == "All" || ProductTypeFilter == "Frame";
        bool includeLenses = ProductTypeFilter == "All" || ProductTypeFilter == "Lens";

        // ── Query Frames ──────────────────────────────────────────────────────
        if (includeFrames)
        {
            var framesQuery = _context.Frames
                .Include(f => f.ProductImages)
                .AsNoTracking()
                .Where(f => f.IsActive);

            if (!string.IsNullOrWhiteSpace(BrandFilter))
                framesQuery = framesQuery.Where(f => f.Brand != null && f.Brand.ToLower() == BrandFilter.ToLower());

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var searchLower = SearchTerm.ToLower();
                framesQuery = framesQuery.Where(f =>
                    f.Name.ToLower().Contains(searchLower) ||
                    f.Sku.ToLower().Contains(searchLower) ||
                    (f.Description != null && f.Description.ToLower().Contains(searchLower)));
            }

            if (MinPrice.HasValue) framesQuery = framesQuery.Where(f => f.Price >= MinPrice.Value);
            if (MaxPrice.HasValue) framesQuery = framesQuery.Where(f => f.Price <= MaxPrice.Value);

            if (!string.IsNullOrWhiteSpace(FrameMaterialFilter))
                framesQuery = framesQuery.Where(f => f.FrameMaterial == FrameMaterialFilter);
            if (!string.IsNullOrWhiteSpace(FrameTypeFilter))
                framesQuery = framesQuery.Where(f => f.FrameType == FrameTypeFilter);
            if (!string.IsNullOrWhiteSpace(GenderFilter))
                framesQuery = framesQuery.Where(f => f.Gender != null && f.Gender.ToLower() == GenderFilter.ToLower());
            if (!string.IsNullOrWhiteSpace(FrameShapeFilter))
                framesQuery = framesQuery.Where(f => f.FrameShape != null && f.FrameShape.ToLower() == FrameShapeFilter.ToLower());

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
                InventoryQty = f.InventoryQty,
                PrimaryImageUrl = f.ProductImages?.FirstOrDefault(i => i.IsPrimary && i.IsActive)?.ImageUrl
                               ?? f.ProductImages?.FirstOrDefault(i => i.IsActive)?.ImageUrl,
                FrameMaterial = f.FrameMaterial,
                FrameType = f.FrameType,
                CreatedAt = f.CreatedAt
            }));
        }

        // ── Query Lenses ──────────────────────────────────────────────────────
        if (includeLenses)
        {
            var lensesQuery = _context.Lenses
                .Include(l => l.ProductImages)
                .AsNoTracking()
                .Where(l => l.IsActive);

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var searchLower = SearchTerm.ToLower();
                lensesQuery = lensesQuery.Where(l =>
                    l.Name.ToLower().Contains(searchLower) ||
                    l.Sku.ToLower().Contains(searchLower) ||
                    (l.Description != null && l.Description.ToLower().Contains(searchLower)));
            }

            if (MinPrice.HasValue) lensesQuery = lensesQuery.Where(l => l.Price >= MinPrice.Value);
            if (MaxPrice.HasValue) lensesQuery = lensesQuery.Where(l => l.Price <= MaxPrice.Value);

            if (!string.IsNullOrWhiteSpace(LensTypeFilter))
                lensesQuery = lensesQuery.Where(l => l.LensType == LensTypeFilter);

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
                InventoryQty = l.InventoryQty,
                PrimaryImageUrl = l.ProductImages?.FirstOrDefault(i => i.IsPrimary && i.IsActive)?.ImageUrl
                               ?? l.ProductImages?.FirstOrDefault(i => i.IsActive)?.ImageUrl,
                LensType = l.LensType,
                LensIndex = l.LensIndex,
                IsPrescription = l.IsPrescription,
                CreatedAt = l.CreatedAt
            }));
        }

        // ── Sort ──────────────────────────────────────────────────────────────
        productList = SortBy switch
        {
            "price-low" => productList.OrderBy(p => p.Price).ToList(),
            "price-high" => productList.OrderByDescending(p => p.Price).ToList(),
            "newest" => productList.OrderByDescending(p => p.CreatedAt).ToList(),
            _ => productList.OrderBy(p => p.Name).ToList()
        };

        // ── Pagination ────────────────────────────────────────────────────────
        var totalCount = productList.Count;
        var totalPages = (int)Math.Ceiling(totalCount / (double)PageSize);
        if (CurrentPage > totalPages && totalPages > 0) CurrentPage = totalPages;

        var pagedProducts = productList
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize)
            .ToList();

        // ── Filter options ────────────────────────────────────────────────────
        var availableFrameMaterials = await _context.Frames
            .Where(f => f.IsActive && f.FrameMaterial != null)
            .Select(f => f.FrameMaterial!)
            .Distinct().OrderBy(m => m).ToListAsync();

        var availableFrameTypes = await _context.Frames
            .Where(f => f.IsActive && f.FrameType != null)
            .Select(f => f.FrameType!)
            .Distinct().OrderBy(t => t).ToListAsync();

        var availableLensTypes = await _context.Lenses
            .Where(l => l.IsActive && l.LensType != null)
            .Select(l => l.LensType!)
            .Distinct().OrderBy(t => t).ToListAsync();

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

        // ── Brand mega bar data ───────────────────────────────────────────────
        AllBrands = await _context.Frames
            .Where(f => f.IsActive && f.Brand != null)
            .GroupBy(f => f.Brand)
            .Select(g => new { Brand = g.Key!, Count = g.Count() })
            .OrderBy(b => b.Brand)
            .AsNoTracking()
            .ToListAsync()
            .ContinueWith(t => t.Result.Select(x => new BrandItem(x.Brand, x.Count)).ToList());

        return Page();
    }

    // ── Smart Search API ──────────────────────────────────────────────────────
    // GET /Products?handler=Search&q=molsion&limit=6
    public async Task<IActionResult> OnGetSearchAsync(string? q, int limit = 8)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 1)
            return new JsonResult(new { frames = Array.Empty<object>(), brands = Array.Empty<object>() });

        var term = q.Trim().ToLower();

        var frames = await _context.Frames
            .Where(f => f.IsActive && (
                f.Name.ToLower().Contains(term) ||
                f.Sku.ToLower().Contains(term) ||
                (f.Brand != null && f.Brand.ToLower().Contains(term))))
            .OrderBy(f => f.Name)
            .Take(limit)
            .Select(f => new
            {
                productId = f.ProductId,
                name = f.Name,
                sku = f.Sku,
                price = f.Price,
                currency = f.Currency,
                brand = f.Brand,
                imageUrl = f.ProductImages
                             .Where(i => i.IsActive)
                             .OrderByDescending(i => i.IsPrimary)
                             .ThenBy(i => i.SortOrder)
                             .Select(i => i.ImageUrl)
                             .FirstOrDefault()
            })
            .AsNoTracking()
            .ToListAsync();

        var brands = await _context.Frames
            .Where(f => f.IsActive && f.Brand != null && f.Brand.ToLower().Contains(term))
            .GroupBy(f => f.Brand)
            .Select(g => new { brand = g.Key, count = g.Count() })
            .OrderBy(b => b.brand)
            .Take(5)
            .AsNoTracking()
            .ToListAsync();

        return new JsonResult(new { frames, brands });
    }

    // ── Cart handlers ─────────────────────────────────────────────────────────
    public async Task<IActionResult> OnPostAddToCartAsync([FromBody] AddToCartRequest request)
    {
        if (request == null || request.ProductId <= 0 || request.Quantity <= 0)
            return new JsonResult(new { success = false, message = "Invalid request data." });

        if (!User.Identity?.IsAuthenticated ?? true)
            return new JsonResult(new { success = false, message = "Please login to add items to cart." }) { StatusCode = 401 };

        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId))
            return new JsonResult(new { success = false, message = "User identity not found." }) { StatusCode = 401 };

        try
        {
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
                return new JsonResult(new { success = false, message = $"Only {inventoryQty} items left in stock." });

            await _cartService.AddToCartAsync(userId, request.ProductId, request.Quantity, prescriptionId: request.PrescriptionId);

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
                    string? imageUrl = ci.Product?.ProductImages?.OrderByDescending(x => x.IsPrimary).FirstOrDefault()?.ImageUrl;
                    string? prescriptionName = ci.Prescription?.ProfileName;
                    string productType = ci.Product?.ProductType ?? "Frame";
                    cartItemsList.Add(new { cartItemId = ci.CartItemId, name, unitPrice, quantity = ci.Quantity, lineTotal, imageUrl, prescriptionName, productType });
                }
            }

            return new JsonResult(new { success = true, message = "Added to cart successfully!", cartCount = newCount, cartItems = cartItemsList, subtotal });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { success = false, message = "Error adding to cart: " + ex.Message });
        }
    }

    public IActionResult OnGetCartSummaryPartial() => Partial("_CartSummary");

    public async Task<IActionResult> OnPostRemoveCartItemAsync([FromBody] RemoveCartItemRequest request)
    {
        if (request == null || request.CartItemId <= 0)
            return new JsonResult(new { success = false, message = "Invalid request." });
        if (!User.Identity?.IsAuthenticated ?? true)
            return new JsonResult(new { success = false, message = "Please login." }) { StatusCode = 401 };
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId))
            return new JsonResult(new { success = false, message = "User identity not found." }) { StatusCode = 401 };
        try
        {
            await _cartService.RemoveItemAsync(request.CartItemId);
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
                    string? imageUrl = ci.Product?.ProductImages?.OrderByDescending(x => x.IsPrimary).FirstOrDefault()?.ImageUrl;
                    string? prescriptionName = ci.Prescription?.ProfileName;
                    string productType = ci.Product?.ProductType ?? "Frame";
                    cartItemsList.Add(new { cartItemId = ci.CartItemId, name, unitPrice, quantity = ci.Quantity, lineTotal, imageUrl, prescriptionName, productType });
                }
            }
            return new JsonResult(new { success = true, message = "Item removed.", cartCount = newCount, cartItems = cartItemsList, subtotal });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { success = false, message = "Error: " + ex.Message });
        }
    }

    public async Task<IActionResult> OnPostClearCartAsync()
    {
        if (!User.Identity?.IsAuthenticated ?? true)
            return new JsonResult(new { success = false, message = "Please login." }) { StatusCode = 401 };
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId))
            return new JsonResult(new { success = false, message = "User identity not found." }) { StatusCode = 401 };
        try
        {
            await _cartService.ClearCartAsync(userId);
            return new JsonResult(new { success = true, message = "Cart cleared.", cartCount = 0, cartItems = new List<object>(), subtotal = 0m });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { success = false, message = "Error: " + ex.Message });
        }
    }

    public async Task<IActionResult> OnPostUpdateCartItemQtyAsync([FromBody] UpdateCartItemQtyRequest request)
    {
        if (request == null || request.CartItemId <= 0)
            return new JsonResult(new { success = false, message = "Invalid request." });
        if (!User.Identity?.IsAuthenticated ?? true)
            return new JsonResult(new { success = false, message = "Please login." }) { StatusCode = 401 };
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId))
            return new JsonResult(new { success = false, message = "User identity not found." }) { StatusCode = 401 };
        try
        {
            if (request.NewQuantity <= 0)
                await _cartService.RemoveItemAsync(request.CartItemId);
            else
                await _cartService.UpdateQuantityAsync(request.CartItemId, request.NewQuantity);
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
                    string? imageUrl = ci.Product?.ProductImages?.OrderByDescending(x => x.IsPrimary).FirstOrDefault()?.ImageUrl;
                    string? prescriptionName = ci.Prescription?.ProfileName;
                    string productType = ci.Product?.ProductType ?? "Frame";
                    cartItemsList.Add(new { cartItemId = ci.CartItemId, name, unitPrice, quantity = ci.Quantity, lineTotal, imageUrl, prescriptionName, productType });
                }
            }
            return new JsonResult(new { success = true, message = "Quantity updated.", cartCount = newCount, cartItems = cartItemsList, subtotal });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { success = false, message = "Error: " + ex.Message });
        }
    }

    public async Task<IActionResult> OnGetPrescriptionsAsync()
    {
        if (!User.Identity?.IsAuthenticated ?? true)
            return new JsonResult(new { prescriptions = Array.Empty<object>() }) { StatusCode = 401 };
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId))
            return new JsonResult(new { prescriptions = Array.Empty<object>() }) { StatusCode = 401 };
        var prescriptions = await _context.PrescriptionProfiles
            .Where(p => p.UserId == userId && p.IsActive)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new { p.PrescriptionId, name = p.ProfileName ?? "Prescription", leftSph = p.LeftSph, leftCyl = p.LeftCyl, rightSph = p.RightSph, rightCyl = p.RightCyl })
            .ToListAsync();
        return new JsonResult(new { prescriptions });
    }

    // ── Request DTOs ──────────────────────────────────────────────────────────
    public class AddToCartRequest { public int ProductId { get; set; } public int Quantity { get; set; } public int? PrescriptionId { get; set; } }
    public class RemoveCartItemRequest { public int CartItemId { get; set; } }
    public class UpdateCartItemQtyRequest { public int CartItemId { get; set; } public int NewQuantity { get; set; } }
}
