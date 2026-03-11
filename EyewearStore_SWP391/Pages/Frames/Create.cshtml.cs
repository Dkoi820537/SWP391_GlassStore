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
        Input = new CreateFrameViewModel
        {
            Currency = "VND",
            IsActive = true
        };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (Input.ImageFile != null)
        {
            if (Input.ImageFile.Length > MaxFileSize)
                ModelState.AddModelError("Input.ImageFile",
                    $"Image file size exceeds maximum allowed size of {MaxFileSize / (1024 * 1024)} MB");

            var extension = Path.GetExtension(Input.ImageFile.FileName).ToLowerInvariant();
            if (!_allowedExtensions.Contains(extension))
                ModelState.AddModelError("Input.ImageFile",
                    $"Invalid file type. Allowed types: {string.Join(", ", _allowedExtensions)}");
        }

        if (!ModelState.IsValid)
            return Page();

        var skuExists = await _context.Products.AnyAsync(p => p.Sku == Input.Sku);
        if (skuExists)
        {
            ModelState.AddModelError("Input.Sku", "This SKU already exists. Please use a different SKU.");
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
            IsActive = true,
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
        };

        _context.Frames.Add(frame);
        await _context.SaveChangesAsync();

        if (Input.ImageFile != null && Input.ImageFile.Length > 0)
            await SaveProductImageAsync(frame.ProductId, Input.ImageFile, Input.ImageAltText);

        return RedirectToPage("./Index");
    }

    private async Task SaveProductImageAsync(int productId, IFormFile file, string? altText)
    {
        var uploadFolder = Path.Combine(_environment.WebRootPath, "uploads", "products", productId.ToString());
        if (!Directory.Exists(uploadFolder))
            Directory.CreateDirectory(uploadFolder);

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var fileName = $"{timestamp}_{uniqueId}{extension}";
        var filePath = Path.Combine(uploadFolder, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
            await file.CopyToAsync(stream);

        var productImage = new ProductImage
        {
            ProductId = productId,
            ImageUrl = $"/uploads/products/{productId}/{fileName}",
            AltText = altText ?? Input.Name,
            IsPrimary = true,
            SortOrder = 0,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _context.ProductImages.AddAsync(productImage);
        await _context.SaveChangesAsync();
    }
}