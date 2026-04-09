using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using EyewearStore_SWP391.Models;
using EyewearStore_SWP391.Models.ViewModels.Lens;

namespace EyewearStore_SWP391.Pages.Lenses;

public class IndexModel : PageModel
{
    private readonly EyewearStoreContext _context;
    private const int DefaultPageSize = 10;

    public IndexModel(EyewearStoreContext context) => _context = context;

    public LensListViewModel LensList { get; set; } = new();

    [BindProperty(SupportsGet = true)] public string? SearchTerm { get; set; }
    [BindProperty(SupportsGet = true)] public string? FilterLensType { get; set; }
    [BindProperty(SupportsGet = true)] public string? FilterBrand { get; set; }
    [BindProperty(SupportsGet = true)] public bool? FilterIsPrescription { get; set; }
    [BindProperty(SupportsGet = true)] public decimal? MinPrice { get; set; }
    [BindProperty(SupportsGet = true)] public decimal? MaxPrice { get; set; }
    [BindProperty(SupportsGet = true)] public int CurrentPage { get; set; } = 1;

    public async Task<IActionResult> OnGetAsync()
    {
        if (CurrentPage < 1) CurrentPage = 1;

        IQueryable<Lens> query = _context.Lenses.Include(l => l.ProductImages).AsNoTracking();

        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            var s = SearchTerm.ToLower();
            query = query.Where(l =>
                l.Name.ToLower().Contains(s) ||
                l.Sku.ToLower().Contains(s) ||
                (l.Brand != null && l.Brand.ToLower().Contains(s)) ||
                (l.Description != null && l.Description.ToLower().Contains(s)));
        }

        if (!string.IsNullOrWhiteSpace(FilterLensType))
            query = query.Where(l => l.LensType == FilterLensType);

        if (!string.IsNullOrWhiteSpace(FilterBrand))
            query = query.Where(l => l.Brand == FilterBrand);

        if (FilterIsPrescription.HasValue)
            query = query.Where(l => l.IsPrescription == FilterIsPrescription.Value);

        if (MinPrice.HasValue) query = query.Where(l => l.Price >= MinPrice.Value);
        if (MaxPrice.HasValue) query = query.Where(l => l.Price <= MaxPrice.Value);

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)DefaultPageSize);
        if (CurrentPage > totalPages && totalPages > 0) CurrentPage = totalPages;

        var lenses = await query
            .OrderBy(l => l.Name)
            .Skip((CurrentPage - 1) * DefaultPageSize)
            .Take(DefaultPageSize)
            .ToListAsync();

        var lensItems = lenses.Select(l => new LensItemViewModel
        {
            ProductId = l.ProductId,
            Sku = l.Sku,
            Name = l.Name,
            Price = l.Price,
            Currency = l.Currency,
            QuantityOnHand = l.QuantityOnHand,
            Brand = l.Brand,
            Origin = l.Origin,
            LensType = l.LensType,
            LensIndex = l.LensIndex,
            LensMaterial = l.LensMaterial,
            LensThickness = l.LensThickness,
            LensCoating = l.LensCoating,
            UVProtection = l.UVProtection,
            IsPrescription = l.IsPrescription,
            IsActive = l.IsActive,
            PrimaryImageUrl = l.ProductImages?.FirstOrDefault(i => i.IsPrimary && i.IsActive)?.ImageUrl
                           ?? l.ProductImages?.FirstOrDefault(i => i.IsActive)?.ImageUrl
        }).ToList();

        var availableLensTypes = await _context.Lenses
            .Where(l => l.LensType != null)
            .Select(l => l.LensType!).Distinct().OrderBy(t => t).ToListAsync();

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
