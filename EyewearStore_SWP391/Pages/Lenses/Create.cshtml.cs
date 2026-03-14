using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using EyewearStore_SWP391.Models;
using EyewearStore_SWP391.Models.ViewModels.Lens;

namespace EyewearStore_SWP391.Pages.Lenses;

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
    public CreateLensViewModel Input { get; set; } = new();

    public IActionResult OnGet()
    {
        Input = new CreateLensViewModel { Currency = "VND", IsActive = true };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var files = new List<IFormFile>();
        if (Input.ImageFiles != null && Input.ImageFiles.Any(f => f?.Length > 0))
            files.AddRange(Input.ImageFiles.Where(f => f != null && f.Length > 0));
        else if (Input.ImageFile != null && Input.ImageFile.Length > 0)
            files.Add(Input.ImageFile);

        foreach (var file in files)
        {
            if (file.Length > MaxFileSize)
                ModelState.AddModelError("Input.ImageFiles", $"'{file.FileName}' exceeds 5 MB limit.");
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_allowedExtensions.Contains(ext))
                ModelState.AddModelError("Input.ImageFiles", $"'{file.FileName}' is not a valid image type.");
        }

        if (!ModelState.IsValid) return Page();

        if (await _context.Products.AnyAsync(p => p.Sku == Input.Sku))
        {
            ModelState.AddModelError("Input.Sku", "This SKU already exists.");
            return Page();
        }

        var lens = new Lens
        {
            Sku = Input.Sku,
            Name = Input.Name,
            Description = Input.Description,
            Price = Input.Price,
            Currency = Input.Currency,
            InventoryQty = Input.InventoryQty,
            Attributes = Input.Attributes,
            ProductType = "Lens",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            LensType = Input.LensType,
            LensIndex = Input.LensIndex,
            IsPrescription = Input.IsPrescription,
            Brand = Input.Brand,
            Origin = Input.Origin,
            LensMaterial = Input.LensMaterial,
            LensThickness = Input.LensThickness,
            LensCoating = Input.LensCoating,
            UVProtection = Input.UVProtection,
        };

        _context.Lenses.Add(lens);
        await _context.SaveChangesAsync();

        if (files.Any())
            await SaveProductImagesAsync(lens.ProductId, files, Input.ImageAltText);

        TempData["Success"] = $"Lens '{lens.Name}' created successfully.";
        return RedirectToPage("./Index");
    }

    private async Task SaveProductImagesAsync(int productId, List<IFormFile> files, string? altText)
    {
        var folder = Path.Combine(_environment.WebRootPath, "uploads", "products", productId.ToString());
        Directory.CreateDirectory(folder);

        for (int i = 0; i < files.Count; i++)
        {
            var file = files[i];
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var name = $"{DateTime.UtcNow:yyyyMMddHHmmss}_{i}_{Guid.NewGuid().ToString("N")[..8]}{ext}";
            var path = Path.Combine(folder, name);
            using var stream = new FileStream(path, FileMode.Create);
            await file.CopyToAsync(stream);

            _context.ProductImages.Add(new ProductImage
            {
                ProductId = productId,
                ImageUrl = $"/uploads/products/{productId}/{name}",
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
