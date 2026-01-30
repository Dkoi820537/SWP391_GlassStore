using EyewearStore_SWP391.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EyewearStore_SWP391.Controllers;

/// <summary>
/// API Controller for managing image uploads
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
    /// Uploads an image file and creates an Image record
    /// </summary>
    /// <param name="file">The image file to upload</param>
    /// <param name="imageType">Type of image (e.g., "product", "lens")</param>
    /// <param name="context">Context identifier (e.g., "lens:123")</param>
    /// <param name="altText">Alternative text for the image</param>
    /// <returns>The created image record with URL</returns>
    [HttpPost("upload")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UploadImage(
        IFormFile file,
        [FromForm] string imageType = "product",
        [FromForm] string? context = null,
        [FromForm] string? altText = null)
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

            // Create upload directory if it doesn't exist
            var uploadFolder = Path.Combine(_environment.WebRootPath, "uploads", imageType);
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
            var imageUrl = $"/uploads/{imageType}/{fileName}";

            // Create Image record in database
            var image = new Image
            {
                ImageUrl = imageUrl,
                AltText = altText,
                ImageType = imageType,
                Context = context,
                DisplayOrder = 0,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _context.Images.AddAsync(image);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetImageById), new { id = image.ImageId }, new
            {
                imageId = image.ImageId,
                imageUrl = image.ImageUrl,
                altText = image.AltText,
                imageType = image.ImageType,
                context = image.Context,
                createdAt = image.CreatedAt
            });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                $"An error occurred while uploading the image: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets images filtered by context
    /// </summary>
    /// <param name="context">Filter by context (e.g., "lens:123")</param>
    /// <param name="imageType">Filter by image type</param>
    /// <returns>List of images</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetImages(
        [FromQuery] string? context = null,
        [FromQuery] string? imageType = null)
    {
        try
        {
            var query = _context.Images.Where(i => i.IsActive);

            if (!string.IsNullOrWhiteSpace(context))
            {
                query = query.Where(i => i.Context == context);
            }

            if (!string.IsNullOrWhiteSpace(imageType))
            {
                query = query.Where(i => i.ImageType == imageType);
            }

            var images = await query
                .OrderBy(i => i.DisplayOrder)
                .ThenByDescending(i => i.CreatedAt)
                .Select(i => new
                {
                    imageId = i.ImageId,
                    imageUrl = i.ImageUrl,
                    altText = i.AltText,
                    imageType = i.ImageType,
                    context = i.Context,
                    displayOrder = i.DisplayOrder,
                    createdAt = i.CreatedAt
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
            var image = await _context.Images.FindAsync(id);

            if (image == null)
            {
                return NotFound($"Image with ID {id} not found");
            }

            return Ok(new
            {
                imageId = image.ImageId,
                imageUrl = image.ImageUrl,
                altText = image.AltText,
                imageType = image.ImageType,
                context = image.Context,
                displayOrder = image.DisplayOrder,
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
            var image = await _context.Images.FindAsync(id);

            if (image == null)
            {
                return NotFound($"Image with ID {id} not found");
            }

            // Soft delete
            image.IsActive = false;
            image.UpdatedAt = DateTime.UtcNow;

            _context.Images.Update(image);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                $"An error occurred while deleting the image: {ex.Message}");
        }
    }

    /// <summary>
    /// Updates an image's context (for linking to a lens after creation)
    /// </summary>
    /// <param name="id">The image ID</param>
    /// <param name="context">The new context value</param>
    /// <returns>The updated image</returns>
    [HttpPatch("{id:int}/context")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateImageContext(int id, [FromBody] string context)
    {
        try
        {
            var image = await _context.Images.FindAsync(id);

            if (image == null)
            {
                return NotFound($"Image with ID {id} not found");
            }

            image.Context = context;
            image.UpdatedAt = DateTime.UtcNow;

            _context.Images.Update(image);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                imageId = image.ImageId,
                imageUrl = image.ImageUrl,
                context = image.Context
            });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                $"An error occurred while updating the image: {ex.Message}");
        }
    }
}
