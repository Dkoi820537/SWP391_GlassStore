using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using EyewearStore_SWP391.Models;
using EyewearStore_SWP391.Models.ViewModels.Frame;

namespace EyewearStore_SWP391.Pages.Frames;

/// <summary>
/// Page model for listing and filtering frame products.
/// Provides server-side search, filtering, and pagination.
/// </summary>
public class IndexModel : PageModel
{
    private readonly EyewearStoreContext _context;
    private const int DefaultPageSize = 10;

    public IndexModel(EyewearStoreContext context)
    {
        _context = context;
    }

    /// <summary>
    /// The view model containing frame list data and pagination info
    /// </summary>
    public FrameListViewModel FrameList { get; set; } = new();

    // Filter properties with SupportsGet = true for query string binding

    /// <summary>
    /// Search term to filter by name, SKU, or description
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    /// <summary>
    /// Filter by frame material
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string? FilterMaterial { get; set; }

    /// <summary>
    /// Filter by frame type
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string? FilterType { get; set; }

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
    /// Current page number (1-indexed)
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public int CurrentPage { get; set; } = 1;

    /// <summary>
    /// Handles GET request - loads frames with filtering and pagination
    /// </summary>
    public async Task<IActionResult> OnGetAsync()
    {
        // Ensure current page is at least 1
        if (CurrentPage < 1) CurrentPage = 1;

        // Start query with all frames (including inactive for admin visibility)
        IQueryable<Frame> query = _context.Frames
            .Include(f => f.ProductImages)
            .AsNoTracking();

        // Apply search filter (search in Name, Sku, Description)
        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            var searchLower = SearchTerm.ToLower();
            query = query.Where(f =>
                f.Name.ToLower().Contains(searchLower) ||
                f.Sku.ToLower().Contains(searchLower) ||
                (f.Description != null && f.Description.ToLower().Contains(searchLower)));
        }

        // Apply material filter
        if (!string.IsNullOrWhiteSpace(FilterMaterial))
        {
            query = query.Where(f => f.FrameMaterial == FilterMaterial);
        }

        // Apply type filter
        if (!string.IsNullOrWhiteSpace(FilterType))
        {
            query = query.Where(f => f.FrameType == FilterType);
        }

        // Apply price range filters
        if (MinPrice.HasValue)
        {
            query = query.Where(f => f.Price >= MinPrice.Value);
        }

        if (MaxPrice.HasValue)
        {
            query = query.Where(f => f.Price <= MaxPrice.Value);
        }

        // Calculate total count for pagination (before skip/take)
        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)DefaultPageSize);

        // Ensure current page doesn't exceed total pages
        if (CurrentPage > totalPages && totalPages > 0)
        {
            CurrentPage = totalPages;
        }

        // Apply ordering by Name
        query = query.OrderBy(f => f.Name);

        // Apply pagination (skip/take)
        var frames = await query
            .Skip((CurrentPage - 1) * DefaultPageSize)
            .Take(DefaultPageSize)
            .ToListAsync();

        // Map to FrameItemViewModel list
        var frameItems = frames.Select(f => new FrameItemViewModel
        {
            ProductId = f.ProductId,
            Sku = f.Sku,
            Name = f.Name,
            Price = f.Price,
            Currency = f.Currency,
            InventoryQty = f.InventoryQty,
            FrameMaterial = f.FrameMaterial,
            FrameType = f.FrameType,
            IsActive = f.IsActive,
            PrimaryImageUrl = f.ProductImages?.FirstOrDefault(i => i.IsPrimary && i.IsActive)?.ImageUrl
                ?? f.ProductImages?.FirstOrDefault(i => i.IsActive)?.ImageUrl
        }).ToList();

        // Get available materials and types for filter dropdowns
        var availableMaterials = await _context.Frames
            .Where(f => f.FrameMaterial != null)
            .Select(f => f.FrameMaterial!)
            .Distinct()
            .OrderBy(m => m)
            .ToListAsync();

        var availableTypes = await _context.Frames
            .Where(f => f.FrameType != null)
            .Select(f => f.FrameType!)
            .Distinct()
            .OrderBy(t => t)
            .ToListAsync();

        // Populate the view model
        FrameList = new FrameListViewModel
        {
            Items = frameItems,
            SearchTerm = SearchTerm,
            FilterMaterial = FilterMaterial,
            FilterType = FilterType,
            MinPrice = MinPrice,
            MaxPrice = MaxPrice,
            CurrentPage = CurrentPage,
            TotalPages = totalPages,
            PageSize = DefaultPageSize,
            TotalCount = totalCount,
            AvailableMaterials = availableMaterials,
            AvailableTypes = availableTypes
        };

        return Page();
    }
}
