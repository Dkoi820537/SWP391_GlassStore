using EyewearStore_SWP391.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EyewearStore_SWP391.Controllers;

/// <summary>
/// API Controller for managing product images
/// </summary>
[Route("api/[controller]")]
[ApiController]
[Produces("application/json")]
public class ImagesController : ControllerBase
{
    private readonly EyewearStoreContext _context;
    private readonly IWebHostEnvironment _environment;
    
    // Allowed file extensions
    private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
    
    // Maximum file size (5 MB)
    private const long MaxFileSize = 5 * 1024 * 1024;

    public ImagesController(EyewearStoreContext context, IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    /// <summary>
    /// Uploads an image file and creates a ProductImage record
    /// </summary>
    /// <param name="file">The image file to upload</param>
    /// <param name="productId">The product ID to associate with the image</param>
    /// <param name="altText">Alternative text for the image</param>
    /// <param name="isPrimary">Whether this is the primary image for the product</param>
    /// <param name="sortOrder">Display order for the image</param>
    /// <returns>The created image record with URL</returns>
    [HttpPost("upload")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UploadImage(
        IFormFile file,
        [FromForm] int productId,
        [FromForm] string? altText = null,
        [FromForm] bool isPrimary = false,
        [FromForm] int sortOrder = 0)
    {
        try
        {
            // Validate file exists
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded");
            }

            // Validate file size
            if (file.Length > MaxFileSize)
            {
                return BadRequest($"File size exceeds maximum allowed size of {MaxFileSize / (1024 * 1024)} MB");
            }

            // Validate file extension
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_allowedExtensions.Contains(extension))
            {
                return BadRequest($"Invalid file type. Allowed types: {string.Join(", ", _allowedExtensions)}");
            }

            // Verify product exists
            var product = await _context.Products.FindAsync(productId);
            if (product == null)
            {
                return BadRequest($"Product with ID {productId} not found");
            }

            // If this is set as primary, unset other primary images for this product
            if (isPrimary)
            {
                var existingPrimary = await _context.ProductImages
                    .Where(pi => pi.ProductId == productId && pi.IsPrimary)
                    .ToListAsync();
                foreach (var img in existingPrimary)
                {
                    img.IsPrimary = false;
                }
            }

            // Create upload directory if it doesn't exist
            var uploadFolder = Path.Combine(_environment.WebRootPath, "uploads", "products", productId.ToString());
            if (!Directory.Exists(uploadFolder))
            {
                Directory.CreateDirectory(uploadFolder);
            }

            // Generate unique filename
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
                AltText = altText,
                IsPrimary = isPrimary,
                SortOrder = sortOrder,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _context.ProductImages.AddAsync(productImage);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetImageById), new { id = productImage.ImageId }, new
            {
                imageId = productImage.ImageId,
                productId = productImage.ProductId,
                imageUrl = productImage.ImageUrl,
                altText = productImage.AltText,
                isPrimary = productImage.IsPrimary,
                sortOrder = productImage.SortOrder,
                isActive = productImage.IsActive,
                createdAt = productImage.CreatedAt
            });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                $"An error occurred while uploading the image: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets images for a specific product
    /// </summary>
    /// <param name="productId">The product ID</param>
    /// <param name="activeOnly">Only return active images</param>
    /// <returns>List of product images</returns>
    [HttpGet("product/{productId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetImagesByProductId(int productId, [FromQuery] bool activeOnly = true)
    {
        try
        {
            var query = _context.ProductImages.Where(pi => pi.ProductId == productId);

            if (activeOnly)
            {
                query = query.Where(pi => pi.IsActive);
            }

            var images = await query
                .OrderByDescending(pi => pi.IsPrimary)
                .ThenBy(pi => pi.SortOrder)
                .Select(pi => new
                {
                    imageId = pi.ImageId,
                    productId = pi.ProductId,
                    imageUrl = pi.ImageUrl,
                    altText = pi.AltText,
                    isPrimary = pi.IsPrimary,
                    sortOrder = pi.SortOrder,
                    isActive = pi.IsActive,
                    createdAt = pi.CreatedAt
                })
                .ToListAsync();

            return Ok(images);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                $"An error occurred while retrieving images: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets an image by ID
    /// </summary>
    /// <param name="id">The image ID</param>
    /// <returns>The image record</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetImageById(int id)
    {
        try
        {
            var image = await _context.ProductImages.FindAsync(id);

            if (image == null)
            {
                return NotFound($"Image with ID {id} not found");
            }

            return Ok(new
            {
                imageId = image.ImageId,
                productId = image.ProductId,
                imageUrl = image.ImageUrl,
                altText = image.AltText,
                isPrimary = image.IsPrimary,
                sortOrder = image.SortOrder,
                isActive = image.IsActive,
                createdAt = image.CreatedAt
            });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                $"An error occurred while retrieving the image: {ex.Message}");
        }
    }

    /// <summary>
    /// Sets an image as the primary image for its product
    /// </summary>
    /// <param name="id">The image ID</param>
    /// <returns>The updated image</returns>
    [HttpPatch("{id:int}/set-primary")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetImageAsPrimary(int id)
    {
        try
        {
            var image = await _context.ProductImages.FindAsync(id);

            if (image == null)
            {
                return NotFound($"Image with ID {id} not found");
            }

            // Unset other primary images for this product
            var existingPrimary = await _context.ProductImages
                .Where(pi => pi.ProductId == image.ProductId && pi.IsPrimary && pi.ImageId != id)
                .ToListAsync();
            foreach (var img in existingPrimary)
            {
                img.IsPrimary = false;
            }

            image.IsPrimary = true;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                imageId = image.ImageId,
                productId = image.ProductId,
                imageUrl = image.ImageUrl,
                isPrimary = image.IsPrimary
            });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                $"An error occurred while updating the image: {ex.Message}");
        }
    }

    /// <summary>
    /// Deletes an image (soft delete by setting IsActive to false)
    /// </summary>
    /// <param name="id">The image ID to delete</param>
    /// <returns>No content on success</returns>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteImage(int id)
    {
        try
        {
            var image = await _context.ProductImages.FindAsync(id);

            if (image == null)
            {
                return NotFound($"Image with ID {id} not found");
            }

            // Soft delete
            image.IsActive = false;

            _context.ProductImages.Update(image);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                $"An error occurred while deleting the image: {ex.Message}");
        }
    }
}
