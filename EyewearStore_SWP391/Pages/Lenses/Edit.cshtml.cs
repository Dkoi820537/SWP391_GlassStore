using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using EyewearStore_SWP391.Models;
using EyewearStore_SWP391.Models.ViewModels.Lens;
using EyewearStore_SWP391.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EyewearStore_SWP391.Pages.Lenses;

public class EditModel : PageModel
{
    private readonly EyewearStoreContext _context;
    private readonly IWebHostEnvironment _environment;
    private readonly IWishlistService _wishlistService;
    private readonly IConfiguration _configuration;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
    private const long MaxFileSize = 5 * 1024 * 1024;

    public EditModel(EyewearStoreContext context, IWebHostEnvironment environment,
        IWishlistService wishlistService, IConfiguration configuration, IServiceScopeFactory scopeFactory)
    {
        _context = context; _environment = environment;
        _wishlistService = wishlistService; _configuration = configuration; _scopeFactory = scopeFactory;
    }

    [BindProperty] public EditLensViewModel Input { get; set; } = new();
    public List<ProductImage> ExistingImages { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null) return NotFound();
        var lens = await _context.Lenses.AsNoTracking().FirstOrDefaultAsync(l => l.ProductId == id);
        if (lens == null) return NotFound();

        ExistingImages = await _context.ProductImages
            .Where(pi => pi.ProductId == id && pi.IsActive)
            .OrderByDescending(pi => pi.IsPrimary).ThenBy(pi => pi.SortOrder).ToListAsync();

        var primaryImage = ExistingImages.FirstOrDefault(i => i.IsPrimary)?.ImageUrl
                        ?? ExistingImages.FirstOrDefault()?.ImageUrl;

        Input = new EditLensViewModel
        {
            ProductId = lens.ProductId,
            Sku = lens.Sku,
            Name = lens.Name,
            Description = lens.Description,
            Price = lens.Price,
            Currency = lens.Currency,
            QuantityOnHand = lens.QuantityOnHand,
            Attributes = lens.Attributes,
            IsActive = lens.IsActive,
            LensType = lens.LensType,
            LensIndex = lens.LensIndex,
            IsPrescription = lens.IsPrescription,
            Brand = lens.Brand,
            Origin = lens.Origin,
            LensMaterial = lens.LensMaterial,
            LensThickness = lens.LensThickness,
            LensCoating = lens.LensCoating,
            UVProtection = lens.UVProtection,
            ExistingImageUrl = primaryImage
        };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (Input.ImageFile != null)
        {
            if (Input.ImageFile.Length > MaxFileSize)
                ModelState.AddModelError("Input.ImageFile", "Image exceeds 5 MB limit.");
            var ext = Path.GetExtension(Input.ImageFile.FileName).ToLowerInvariant();
            if (!_allowedExtensions.Contains(ext))
                ModelState.AddModelError("Input.ImageFile", "Invalid file type.");
        }

        if (!ModelState.IsValid)
        {
            ExistingImages = await _context.ProductImages
                .Where(pi => pi.ProductId == Input.ProductId && pi.IsActive)
                .OrderByDescending(pi => pi.IsPrimary).ThenBy(pi => pi.SortOrder).ToListAsync();
            return Page();
        }

        var lens = await _context.Lenses.FirstOrDefaultAsync(l => l.ProductId == Input.ProductId);
        if (lens == null) return NotFound();

        if (lens.Sku != Input.Sku)
        {
            var skuExists = await _context.Products.AnyAsync(p => p.Sku == Input.Sku && p.ProductId != Input.ProductId);
            if (skuExists)
            {
                ModelState.AddModelError("Input.Sku", "This SKU already exists.");
                ExistingImages = await _context.ProductImages
                    .Where(pi => pi.ProductId == Input.ProductId && pi.IsActive)
                    .OrderByDescending(pi => pi.IsPrimary).ThenBy(pi => pi.SortOrder).ToListAsync();
                return Page();
            }
        }

        var previousQty = lens.QuantityOnHand ?? 0;
        var wasOutOfStock = previousQty <= 0;
        var newQty = Input.QuantityOnHand ?? 0;

        lens.Sku = Input.Sku;
        lens.Name = Input.Name;
        lens.Description = Input.Description;
        lens.Price = Input.Price;
        lens.Currency = Input.Currency;
        lens.QuantityOnHand = Input.QuantityOnHand;
        lens.Attributes = Input.Attributes;
        lens.IsActive = Input.IsActive;
        lens.UpdatedAt = DateTime.UtcNow;
        lens.LensType = Input.LensType;
        lens.LensIndex = Input.LensIndex;
        lens.IsPrescription = Input.IsPrescription;
        lens.Brand = Input.Brand;
        lens.Origin = Input.Origin;
        lens.LensMaterial = Input.LensMaterial;
        lens.LensThickness = Input.LensThickness;
        lens.LensCoating = Input.LensCoating;
        lens.UVProtection = Input.UVProtection;

        try
        {
            await _context.SaveChangesAsync();
            if (Input.ImageFile != null && Input.ImageFile.Length > 0)
                await SaveProductImageAsync(lens.ProductId, Input.ImageFile, Input.ImageAltText);
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!_context.Lenses.Any(l => l.ProductId == Input.ProductId)) return NotFound();
            else throw;
        }

        if (wasOutOfStock && newQty > 0)
        {
            var baseUrl = _configuration["BaseUrl"] ?? "https://localhost:7234";
            var productUrl = $"{baseUrl}/Products/LensDetails/{lens.ProductId}";
            _ = Task.Run(async () =>
            {
                using var scope = _scopeFactory.CreateScope();
                try
                {
                    var svc = scope.ServiceProvider.GetRequiredService<IWishlistService>();
                    await svc.NotifyRestockAsync(lens.ProductId, productUrl);
                }
                catch (Exception ex) { Console.WriteLine($"[RESTOCK ERROR] {ex.Message}"); }
            });
        }

        TempData["Success"] = $"Lens '{lens.Name}' updated successfully.";
        return RedirectToPage("./Index");
    }

    public async Task<IActionResult> OnGetDeleteImageAsync(int imageId, int productId)
    {
        var image = await _context.ProductImages.FindAsync(imageId);
        if (image != null && image.ProductId == productId) { image.IsActive = false; await _context.SaveChangesAsync(); }
        return RedirectToPage(new { id = productId });
    }

    public async Task<IActionResult> OnGetSetPrimaryImageAsync(int imageId, int productId)
    {
        var existing = await _context.ProductImages.Where(pi => pi.ProductId == productId && pi.IsPrimary).ToListAsync();
        foreach (var img in existing) img.IsPrimary = false;
        var image = await _context.ProductImages.FindAsync(imageId);
        if (image != null && image.ProductId == productId) { image.IsPrimary = true; await _context.SaveChangesAsync(); }
        return RedirectToPage(new { id = productId });
    }

    private async Task SaveProductImageAsync(int productId, IFormFile file, string? altText)
    {
        var folder = Path.Combine(_environment.WebRootPath, "uploads", "products", productId.ToString());
        Directory.CreateDirectory(folder);
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var fileName = $"{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid().ToString("N")[..8]}{ext}";
        using (var stream = new FileStream(Path.Combine(folder, fileName), FileMode.Create))
            await file.CopyToAsync(stream);
        var hasPrimary = await _context.ProductImages.AnyAsync(pi => pi.ProductId == productId && pi.IsPrimary && pi.IsActive);
        _context.ProductImages.Add(new ProductImage
        {
            ProductId = productId,
            ImageUrl = $"/uploads/products/{productId}/{fileName}",
            AltText = altText ?? Input.Name,
            IsPrimary = !hasPrimary,
            SortOrder = 0,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
    }

}