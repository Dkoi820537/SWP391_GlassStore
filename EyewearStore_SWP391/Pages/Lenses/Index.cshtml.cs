using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using EyewearStore_SWP391.Models;
using EyewearStore_SWP391.Models.ViewModels.Lens;

namespace EyewearStore_SWP391.Pages.Lenses;

/// <summary>
/// Page model for listing and filtering lens products.
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
    /// The view model containing lens list data and pagination info
    /// </summary>
    public LensListViewModel LensList { get; set; } = new();

    // Filter properties with SupportsGet = true for query string binding

    /// <summary>
    /// Search term to filter by name, SKU, or description
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    /// <summary>
    /// Filter by lens type
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string? FilterLensType { get; set; }

    /// <summary>
    /// Filter by prescription requirement
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public bool? FilterIsPrescription { get; set; }

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
    /// Handles GET request - loads lenses with filtering and pagination
    /// </summary>
    public async Task<IActionResult> OnGetAsync()
    {
        // Ensure current page is at least 1
        if (CurrentPage < 1) CurrentPage = 1;

        // Start query with all lenses (including inactive for admin visibility)
        IQueryable<Lens> query = _context.Lenses
            .Include(l => l.ProductImages)
            .AsNoTracking();

        // Apply search filter (search in Name, Sku, Description)
        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            var searchLower = SearchTerm.ToLower();
            query = query.Where(l =>
                l.Name.ToLower().Contains(searchLower) ||
                l.Sku.ToLower().Contains(searchLower) ||
                (l.Description != null && l.Description.ToLower().Contains(searchLower)));
        }

        // Apply lens type filter
        if (!string.IsNullOrWhiteSpace(FilterLensType))
        {
            query = query.Where(l => l.LensType == FilterLensType);
        }

        // Apply prescription filter
        if (FilterIsPrescription.HasValue)
        {
            query = query.Where(l => l.IsPrescription == FilterIsPrescription.Value);
        }

        // Apply price range filters
        if (MinPrice.HasValue)
        {
            query = query.Where(l => l.Price >= MinPrice.Value);
        }

        if (MaxPrice.HasValue)
        {
            query = query.Where(l => l.Price <= MaxPrice.Value);
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
        query = query.OrderBy(l => l.Name);

        // Apply pagination (skip/take)
        var lenses = await query
            .Skip((CurrentPage - 1) * DefaultPageSize)
            .Take(DefaultPageSize)
            .ToListAsync();

        // Map to LensItemViewModel list
        var lensItems = lenses.Select(l => new LensItemViewModel
        {
            ProductId = l.ProductId,
            Sku = l.Sku,
            Name = l.Name,
            Price = l.Price,
            Currency = l.Currency,
            InventoryQty = l.InventoryQty,
            LensType = l.LensType,
            IsPrescription = l.IsPrescription,
            IsActive = l.IsActive,
            PrimaryImageUrl = l.ProductImages?.FirstOrDefault(i => i.IsPrimary && i.IsActive)?.ImageUrl
                ?? l.ProductImages?.FirstOrDefault(i => i.IsActive)?.ImageUrl
        }).ToList();

        // Get available lens types for filter dropdown
        var availableLensTypes = await _context.Lenses
            .Where(l => l.LensType != null)
            .Select(l => l.LensType!)
            .Distinct()
            .OrderBy(t => t)
            .ToListAsync();

        // Populate the view model
        LensList = new LensListViewModel
        {
            Items = lensItems,
            SearchTerm = SearchTerm,
            FilterLensType = FilterLensType,
            FilterIsPrescription = FilterIsPrescription,
            MinPrice = MinPrice,
            MaxPrice = MaxPrice,
            CurrentPage = CurrentPage,
            TotalPages = totalPages,
            PageSize = DefaultPageSize,
            TotalCount = totalCount,
            AvailableLensTypes = availableLensTypes
        };

        return Page();
    }
}
