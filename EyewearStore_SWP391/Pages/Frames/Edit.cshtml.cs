using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using EyewearStore_SWP391.Models;
using EyewearStore_SWP391.Models.ViewModels.Frame;

namespace EyewearStore_SWP391.Pages.Frames;

/// <summary>
/// Page model for editing an existing frame product.
/// Handles form display with pre-populated data, update with concurrency handling, and image upload.
/// </summary>
public class EditModel : PageModel
{
    private readonly EyewearStoreContext _context;
    private readonly IWebHostEnvironment _environment;

    // Allowed file extensions
    private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };

    // Maximum file size (5 MB)
    private const long MaxFileSize = 5 * 1024 * 1024;

    public EditModel(EyewearStoreContext context, IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    /// <summary>
    /// The input view model bound to the form
    /// </summary>
    [BindProperty]
    public EditFrameViewModel Input { get; set; } = new();

    /// <summary>
    /// List of existing images for this product
    /// </summary>
    public List<ProductImage> ExistingImages { get; set; } = new();

    /// <summary>
    /// Handles GET request - loads frame data for editing
    /// </summary>
    /// <param name="id">The ProductId of the frame to edit</param>
    public async Task<IActionResult> OnGetAsync(int? id)
    {
        // Return NotFound if id is null
        if (id == null)
        {
            return NotFound();
        }

        // Query frame from database by ProductId
        var frame = await _context.Frames
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.ProductId == id);

        // Return NotFound if frame doesn't exist
        if (frame == null)
        {
            return NotFound();
        }

        // Get primary image URL
        var primaryImage = await _context.ProductImages
            .Where(pi => pi.ProductId == id && pi.IsPrimary && pi.IsActive)
            .Select(pi => pi.ImageUrl)
            .FirstOrDefaultAsync();

        // Get all existing images for this product
        ExistingImages = await _context.ProductImages
            .Where(pi => pi.ProductId == id && pi.IsActive)
            .OrderByDescending(pi => pi.IsPrimary)
            .ThenBy(pi => pi.SortOrder)
            .ToListAsync();

        // Map frame entity to EditFrameViewModel Input
        Input = new EditFrameViewModel
        {
            ProductId = frame.ProductId,
            Sku = frame.Sku,
            Name = frame.Name,
            Description = frame.Description,
            Price = frame.Price,
            Currency = frame.Currency,
            InventoryQty = frame.InventoryQty,
            Attributes = frame.Attributes,
            IsActive = frame.IsActive,
            // Frame-specific properties
            FrameMaterial = frame.FrameMaterial,
            FrameType = frame.FrameType,
            BridgeWidth = frame.BridgeWidth,
            TempleLength = frame.TempleLength,
            // Existing image
            ExistingImageUrl = primaryImage
        };

        return Page();
    }

    /// <summary>
    /// Handles POST request - validates and updates the frame with optional new image
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
            // Reload existing images for display
            ExistingImages = await _context.ProductImages
                .Where(pi => pi.ProductId == Input.ProductId && pi.IsActive)
                .OrderByDescending(pi => pi.IsPrimary)
                .ThenBy(pi => pi.SortOrder)
                .ToListAsync();
            return Page();
        }

        // Find existing frame in database by Input.ProductId
        var frame = await _context.Frames
            .FirstOrDefaultAsync(f => f.ProductId == Input.ProductId);

        // Return NotFound if frame doesn't exist
        if (frame == null)
        {
            return NotFound();
        }

        // If SKU changed, check if new SKU already exists (excluding current frame)
        if (frame.Sku != Input.Sku)
        {
            var skuExists = await _context.Products
                .AnyAsync(p => p.Sku == Input.Sku && p.ProductId != Input.ProductId);

            if (skuExists)
            {
                ModelState.AddModelError("Input.Sku", "This SKU already exists. Please use a different SKU.");
                // Reload existing images for display
                ExistingImages = await _context.ProductImages
                    .Where(pi => pi.ProductId == Input.ProductId && pi.IsActive)
                    .OrderByDescending(pi => pi.IsPrimary)
                    .ThenBy(pi => pi.SortOrder)
                    .ToListAsync();
                return Page();
            }
        }

        // Update frame properties from Input
        frame.Sku = Input.Sku;
        frame.Name = Input.Name;
        frame.Description = Input.Description;
        frame.Price = Input.Price;
        frame.Currency = Input.Currency;
        frame.InventoryQty = Input.InventoryQty;
        frame.Attributes = Input.Attributes;
        frame.IsActive = Input.IsActive;
        // Frame-specific properties
        frame.FrameMaterial = Input.FrameMaterial;
        frame.FrameType = Input.FrameType;
        frame.BridgeWidth = Input.BridgeWidth;
        frame.TempleLength = Input.TempleLength;
        // Set UpdatedAt to current UTC time
        frame.UpdatedAt = DateTime.UtcNow;

        try
        {
            // Save changes
            await _context.SaveChangesAsync();

            // Handle image upload if provided
            if (Input.ImageFile != null && Input.ImageFile.Length > 0)
            {
                await SaveProductImageAsync(frame.ProductId, Input.ImageFile, Input.ImageAltText);
            }
        }
        catch (DbUpdateConcurrencyException)
        {
            // Handle concurrency exception - check if frame still exists
            if (!FrameExists(Input.ProductId))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        // Redirect to Index page on success
        return RedirectToPage("./Index");
    }

    /// <summary>
    /// Handles POST request to delete a specific image
    /// </summary>
    public async Task<IActionResult> OnPostDeleteImageAsync(int imageId, int productId)
    {
        var image = await _context.ProductImages.FindAsync(imageId);
        if (image != null && image.ProductId == productId)
        {
            // Soft delete the image
            image.IsActive = false;
            await _context.SaveChangesAsync();
        }

        return RedirectToPage(new { id = productId });
    }

    /// <summary>
    /// Handles POST request to set an image as primary
    /// </summary>
    public async Task<IActionResult> OnPostSetPrimaryImageAsync(int imageId, int productId)
    {
        // Unset all primary images for this product
        var existingPrimary = await _context.ProductImages
            .Where(pi => pi.ProductId == productId && pi.IsPrimary)
            .ToListAsync();

        foreach (var img in existingPrimary)
        {
            img.IsPrimary = false;
        }

        // Set the new primary
        var image = await _context.ProductImages.FindAsync(imageId);
        if (image != null && image.ProductId == productId)
        {
            image.IsPrimary = true;
            await _context.SaveChangesAsync();
        }

        return RedirectToPage(new { id = productId });
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

        // Check if there are any existing primary images
        var hasPrimaryImage = await _context.ProductImages
            .AnyAsync(pi => pi.ProductId == productId && pi.IsPrimary && pi.IsActive);

        // Create ProductImage record in database
        var productImage = new ProductImage
        {
            ProductId = productId,
            ImageUrl = imageUrl,
            AltText = altText ?? Input.Name, // Use product name as default alt text
            IsPrimary = !hasPrimaryImage, // Only set as primary if no existing primary
            SortOrder = 0,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _context.ProductImages.AddAsync(productImage);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Helper method to check if a frame exists
    /// </summary>
    /// <param name="id">The ProductId to check</param>
    /// <returns>True if the frame exists, false otherwise</returns>
    private bool FrameExists(int id)
    {
        return _context.Frames.Any(f => f.ProductId == id);
    }
}

