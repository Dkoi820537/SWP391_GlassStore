using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using EyewearStore_SWP391.Models;
using EyewearStore_SWP391.Models.ViewModels.Lens;

namespace EyewearStore_SWP391.Pages.Lenses;

public class DetailsModel : PageModel
{
    private readonly EyewearStoreContext _context;
    public DetailsModel(EyewearStoreContext context) => _context = context;

    public LensViewModel Lens { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null) return NotFound();

        var lens = await _context.Lenses
            .Include(l => l.ProductImages)
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.ProductId == id);

        if (lens == null) return NotFound();

        Lens = new LensViewModel
        {
            ProductId = lens.ProductId,
            Sku = lens.Sku,
            Name = lens.Name,
            Description = lens.Description,
            ProductType = lens.ProductType,
            Price = lens.Price,
            Currency = lens.Currency,
            QuantityOnHand = lens.QuantityOnHand,
            Attributes = lens.Attributes,
            IsActive = lens.IsActive,
            CreatedAt = lens.CreatedAt,
            UpdatedAt = lens.UpdatedAt,
            LensType = lens.LensType,
            LensIndex = lens.LensIndex,
            IsPrescription = lens.IsPrescription,
            Brand = lens.Brand,
            Origin = lens.Origin,
            LensMaterial = lens.LensMaterial,
            LensThickness = lens.LensThickness,
            LensCoating = lens.LensCoating,
            UVProtection = lens.UVProtection,
            PrimaryImageUrl = lens.ProductImages?
                .FirstOrDefault(i => i.IsPrimary && i.IsActive)?.ImageUrl
                ?? lens.ProductImages?.FirstOrDefault(i => i.IsActive)?.ImageUrl,
            ImageUrls = lens.ProductImages?
                .Where(i => i.IsActive)
                .OrderByDescending(i => i.IsPrimary).ThenBy(i => i.SortOrder)
                .Select(i => i.ImageUrl).ToList() ?? new List<string>()
        };
        return Page();
    }
}
