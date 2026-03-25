using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using EyewearStore_SWP391.Models;
using EyewearStore_SWP391.Models.ViewModels.Frame;

namespace EyewearStore_SWP391.Pages.Frames;

public class CreateModel : PageModel
{
    private readonly EyewearStoreContext _context;
    private readonly IWebHostEnvironment _environment;

    private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
    private const long MaxFileSize = 5 * 1024 * 1024;

    public CreateModel(EyewearStoreContext context, IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    [BindProperty]
    public CreateFrameViewModel Input { get; set; } = new();

    public IActionResult OnGet()
    {
        Input = new CreateFrameViewModel { Currency = "VND", IsActive = true };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var files = new List<IFormFile>();
        if (Input.ImageFiles != null && Input.ImageFiles.Any(f => f.Length > 0))
            files.AddRange(Input.ImageFiles.Where(f => f != null && f.Length > 0));
        else if (Input.ImageFile != null && Input.ImageFile.Length > 0)
            files.Add(Input.ImageFile);

        foreach (var file in files)
        {
            if (file.Length > MaxFileSize)
                ModelState.AddModelError("Input.ImageFiles",
                    $"'{file.FileName}' vuot qua {MaxFileSize / (1024 * 1024)} MB.");
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_allowedExtensions.Contains(ext))
                ModelState.AddModelError("Input.ImageFiles",
                    $"'{file.FileName}' khong hop le. Chi chap nhan: jpg, jpeg, png, webp.");
        }

        if (!ModelState.IsValid) return Page();

        if (await _context.Products.AnyAsync(p => p.Sku == Input.Sku))
        {
            ModelState.AddModelError("Input.Sku", "SKU nay da ton tai.");
            return Page();
        }

        var frame = new Frame
        {
            Sku = Input.Sku,
            Name = Input.Name,
            Description = Input.Description,
            Price = Input.Price,
            Currency = Input.Currency,
            InventoryQty = Input.InventoryQty,
            Attributes = Input.Attributes,
            ProductType = "Frame",
            // FIX: lấy từ Input thay vì hardcode true
            IsActive = Input.IsActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,

            // Frame specs
            FrameMaterial = Input.FrameMaterial,
            FrameType = Input.FrameType,
            BridgeWidth = Input.BridgeWidth,
            TempleLength = Input.TempleLength,

            // v2
            Brand = Input.Brand,
            Color = Input.Color,
            Gender = Input.Gender,
            FrameShape = Input.FrameShape,

            // v3
            LensWidth = Input.LensWidth,
            Origin = Input.Origin,

            // v4 — các field này cần cột trong bảng frames (chạy migration)
            FrameColor = Input.FrameColor,
            LensMaterial = Input.LensMaterial,
            LensColor = Input.LensColor,
            SuitableFaceShapes = Input.SuitableFaceShapes,
            IsPolarized = Input.IsPolarized,
            HasUvProtection = Input.HasUvProtection,
            StyleTags = Input.StyleTags,
        };

        _context.Frames.Add(frame);
        await _context.SaveChangesAsync();

        if (files.Any())
            await SaveProductImagesAsync(frame.ProductId, files, Input.ImageAltText);

        return RedirectToPage("./Index");
    }

    private async Task SaveProductImagesAsync(int productId, List<IFormFile> files, string? altText)
    {
        var uploadFolder = Path.Combine(
            _environment.WebRootPath, "uploads", "products", productId.ToString());
        Directory.CreateDirectory(uploadFolder);

        for (int i = 0; i < files.Count; i++)
        {
            var file = files[i];
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var fileName = $"{DateTime.UtcNow:yyyyMMddHHmmss}_{i}_{Guid.NewGuid().ToString("N")[..8]}{ext}";
            var filePath = Path.Combine(uploadFolder, fileName);

            using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);

            _context.ProductImages.Add(new ProductImage
            {
                ProductId = productId,
                ImageUrl = $"/uploads/products/{productId}/{fileName}",
                AltText = altText ?? Input.Name,
                IsPrimary = i == 0,
                SortOrder = i,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            });
        }

        await _context.SaveChangesAsync();
    }
}
