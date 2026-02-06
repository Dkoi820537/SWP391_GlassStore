using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using EyewearStore_SWP391.Models;
using EyewearStore_SWP391.Models.ViewModels.Frame;

namespace EyewearStore_SWP391.Pages.Frames;

/// <summary>
/// Page model for creating a new frame product.
/// Handles form display and submission with SKU uniqueness validation and image upload.
/// </summary>
public class CreateModel : PageModel
{
    private readonly EyewearStoreContext _context;
    private readonly IWebHostEnvironment _environment;

    // Allowed file extensions
    private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };

    // Maximum file size (5 MB)
    private const long MaxFileSize = 5 * 1024 * 1024;

    public CreateModel(EyewearStoreContext context, IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    /// <summary>
    /// The input view model bound to the form
    /// </summary>
    [BindProperty]
    public CreateFrameViewModel Input { get; set; } = new();

    /// <summary>
    /// Handles GET request - initializes the form with default values
    /// </summary>
    public IActionResult OnGet()
    {
        // Initialize Input with default values
        Input = new CreateFrameViewModel
        {
            Currency = "USD",
            IsActive = true
        };

        return Page();
    }

    /// <summary>
    /// Handles POST request - validates and saves the new frame with optional image
    /// </summary>
    public async Task<IActionResult> OnPostAsync()
    {
        // Validate image file if provided
        if (Input.ImageFile != null)
        {
            // Validate file size
            if (Input.ImageFile.Length > MaxFileSize)
            {
                ModelState.AddModelError("Input.ImageFile", 
                    $"Image file size exceeds maximum allowed size of {MaxFileSize / (1024 * 1024)} MB");
            }

            // Validate file extension
            var extension = Path.GetExtension(Input.ImageFile.FileName).ToLowerInvariant();
            if (!_allowedExtensions.Contains(extension))
            {
                ModelState.AddModelError("Input.ImageFile", 
                    $"Invalid file type. Allowed types: {string.Join(", ", _allowedExtensions)}");
            }
        }

        // Check ModelState validity
        if (!ModelState.IsValid)
        {
            return Page();
        }

        // Check if SKU already exists in database
        var skuExists = await _context.Products
            .AnyAsync(p => p.Sku == Input.Sku);

        if (skuExists)
        {
            ModelState.AddModelError("Input.Sku", "This SKU already exists. Please use a different SKU.");
            return Page();
        }

        // Create new Frame entity from Input
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
            // Frame-specific properties
            FrameMaterial = Input.FrameMaterial,
            FrameType = Input.FrameType,
            BridgeWidth = Input.BridgeWidth,
            TempleLength = Input.TempleLength
        };

        // Add frame to DbContext
        _context.Frames.Add(frame);

        // Save changes to get the ProductId
        await _context.SaveChangesAsync();

        // Handle image upload if provided
        if (Input.ImageFile != null && Input.ImageFile.Length > 0)
        {
            await SaveProductImageAsync(frame.ProductId, Input.ImageFile, Input.ImageAltText);
        }

        // Redirect to Index page
        return RedirectToPage("./Index");
    }

    /// <summary>
    /// Saves the uploaded image to disk and creates a ProductImage record
    /// </summary>
    private async Task SaveProductImageAsync(int productId, IFormFile file, string? altText)
    {
        // Create upload directory if it doesn't exist
        var uploadFolder = Path.Combine(_environment.WebRootPath, "uploads", "products", productId.ToString());
        if (!Directory.Exists(uploadFolder))
        {
            Directory.CreateDirectory(uploadFolder);
        }

        // Generate unique filename
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var fileName = $"{timestamp}_{uniqueId}{extension}";
        var filePath = Path.Combine(uploadFolder, fileName);

        // Save file to disk
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        // Create image URL (relative path)
        var imageUrl = $"/uploads/products/{productId}/{fileName}";

        // Create ProductImage record in database
        var productImage = new ProductImage
        {
            ProductId = productId,
            ImageUrl = imageUrl,
            AltText = altText ?? Input.Name, // Use product name as default alt text
            IsPrimary = true, // First image is primary
            SortOrder = 0,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _context.ProductImages.AddAsync(productImage);
        await _context.SaveChangesAsync();
    }
}

