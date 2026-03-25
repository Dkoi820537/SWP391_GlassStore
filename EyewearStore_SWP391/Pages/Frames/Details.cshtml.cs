using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using EyewearStore_SWP391.Models;
using EyewearStore_SWP391.Models.ViewModels.Frame;

namespace EyewearStore_SWP391.Pages.Frames;

public class DetailsModel : PageModel
{
    private readonly EyewearStoreContext _context;

    public DetailsModel(EyewearStoreContext context)
    {
        _context = context;
    }

    public FrameViewModel Frame { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null) return NotFound();

        var frame = await _context.Frames
            .Include(f => f.ProductImages)
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.ProductId == id);

        if (frame == null) return NotFound();

        Frame = new FrameViewModel
        {
            ProductId = frame.ProductId,
            Sku = frame.Sku,
            Name = frame.Name,
            Description = frame.Description,
            ProductType = frame.ProductType,
            Price = frame.Price,
            Currency = frame.Currency,
            InventoryQty = frame.InventoryQty,
            Attributes = frame.Attributes,
            IsActive = frame.IsActive,
            CreatedAt = frame.CreatedAt,
            UpdatedAt = frame.UpdatedAt,
            // Frame specs
            FrameMaterial = frame.FrameMaterial,
            FrameType = frame.FrameType,
            BridgeWidth = frame.BridgeWidth,
            TempleLength = frame.TempleLength,
            // v2
            Brand = frame.Brand,
            Color = frame.Color,
            Gender = frame.Gender,
            FrameShape = frame.FrameShape,
            // v3
            LensWidth = frame.LensWidth,
            Origin = frame.Origin,
            // v4
            FrameColor = frame.FrameColor,
            LensMaterial = frame.LensMaterial,
            LensColor = frame.LensColor,
            SuitableFaceShapes = frame.SuitableFaceShapes,
            IsPolarized = frame.IsPolarized,
            HasUvProtection = frame.HasUvProtection,
            StyleTags = frame.StyleTags,
            // Images
            PrimaryImageUrl = frame.ProductImages?
                .FirstOrDefault(i => i.IsPrimary && i.IsActive)?.ImageUrl
                ?? frame.ProductImages?.FirstOrDefault(i => i.IsActive)?.ImageUrl,
            ImageUrls = frame.ProductImages?
                .Where(i => i.IsActive)
                .OrderByDescending(i => i.IsPrimary)
                .ThenBy(i => i.SortOrder)
                .Select(i => i.ImageUrl)
                .ToList() ?? new List<string>()
        };

        return Page();
    }
}