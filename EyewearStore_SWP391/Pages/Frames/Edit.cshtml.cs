using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using EyewearStore_SWP391.Models;
using EyewearStore_SWP391.Models.ViewModels.Frame;
using EyewearStore_SWP391.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;

namespace EyewearStore_SWP391.Pages.Frames
{
    /// <summary>
    /// Page model for editing an existing frame product.
    /// Handles form display with pre-populated data, update with concurrency handling, image upload,
    /// and triggers wishlist restock notifications when inventory moves from 0 -> >0.
    /// </summary>
    public class EditModel : PageModel
    {
        private readonly EyewearStoreContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly IWishlistService _wishlistService;           // optional direct service for sync use
        private readonly IConfiguration _configuration;
        private readonly IServiceScopeFactory _scopeFactory;         // to create scope inside Task.Run

        // Allowed file extensions
        private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };

        // Maximum file size (5 MB)
        private const long MaxFileSize = 5 * 1024 * 1024;

        public EditModel(
            EyewearStoreContext context,
            IWebHostEnvironment environment,
            IWishlistService wishlistService,
            IConfiguration configuration,
            IServiceScopeFactory scopeFactory)
        {
            _context = context;
            _environment = environment;
            _wishlistService = wishlistService;
            _configuration = configuration;
            _scopeFactory = scopeFactory;
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
            if (id == null) return NotFound();

            var frame = await _context.Frames
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.ProductId == id);

            if (frame == null) return NotFound();

            var primaryImage = await _context.ProductImages
                .Where(pi => pi.ProductId == id && pi.IsPrimary && pi.IsActive)
                .Select(pi => pi.ImageUrl)
                .FirstOrDefaultAsync();

            ExistingImages = await _context.ProductImages
                .Where(pi => pi.ProductId == id && pi.IsActive)
                .OrderByDescending(pi => pi.IsPrimary)
                .ThenBy(pi => pi.SortOrder)
                .ToListAsync();

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
                FrameMaterial = frame.FrameMaterial,
                FrameType = frame.FrameType,
                BridgeWidth = frame.BridgeWidth,
                TempleLength = frame.TempleLength,
                ExistingImageUrl = primaryImage
            };

            return Page();
        }

        /// <summary>
        /// Handles POST request - validates and updates the frame with optional new image
        /// and triggers restock notifications if needed.
        /// </summary>
        public async Task<IActionResult> OnPostAsync()
        {
            // Validate image file if provided
            if (Input.ImageFile != null)
            {
                if (Input.ImageFile.Length > MaxFileSize)
                {
                    ModelState.AddModelError("Input.ImageFile",
                        $"Image file size exceeds maximum allowed size of {MaxFileSize / (1024 * 1024)} MB");
                }

                var extension = Path.GetExtension(Input.ImageFile.FileName).ToLowerInvariant();
                if (!_allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError("Input.ImageFile",
                        $"Invalid file type. Allowed types: {string.Join(", ", _allowedExtensions)}");
                }
            }

            if (!ModelState.IsValid)
            {
                ExistingImages = await _context.ProductImages
                    .Where(pi => pi.ProductId == Input.ProductId && pi.IsActive)
                    .OrderByDescending(pi => pi.IsPrimary)
                    .ThenBy(pi => pi.SortOrder)
                    .ToListAsync();
                return Page();
            }

            var frame = await _context.Frames
                .FirstOrDefaultAsync(f => f.ProductId == Input.ProductId);

            if (frame == null) return NotFound();

            // If SKU changed, check uniqueness
            if (frame.Sku != Input.Sku)
            {
                var skuExists = await _context.Products
                    .AnyAsync(p => p.Sku == Input.Sku && p.ProductId != Input.ProductId);

                if (skuExists)
                {
                    ModelState.AddModelError("Input.Sku", "This SKU already exists. Please use a different SKU.");
                    ExistingImages = await _context.ProductImages
                        .Where(pi => pi.ProductId == Input.ProductId && pi.IsActive)
                        .OrderByDescending(pi => pi.IsPrimary)
                        .ThenBy(pi => pi.SortOrder)
                        .ToListAsync();
                    return Page();
                }
            }

            // ── SNAPSHOT before update (detect 0 -> >0 transition)
            var previousQty = frame.InventoryQty ?? 0;
            var wasOutOfStock = previousQty <= 0;
            var newQty = Input.InventoryQty ?? 0;
            var isNowInStock = newQty > 0;

            Console.WriteLine("======================================");
            Console.WriteLine($"[RESTOCK DEBUG] FrameId={frame.ProductId}");
            Console.WriteLine($"[RESTOCK DEBUG] previousQty={previousQty}, wasOutOfStock={wasOutOfStock}");
            Console.WriteLine($"[RESTOCK DEBUG] newQty={newQty}, isNowInStock={isNowInStock}");
            Console.WriteLine($"[RESTOCK DEBUG] Will notify: {wasOutOfStock && isNowInStock}");
            Console.WriteLine("======================================");

            // Update properties
            frame.Sku = Input.Sku;
            frame.Name = Input.Name;
            frame.Description = Input.Description;
            frame.Price = Input.Price;
            frame.Currency = Input.Currency;
            frame.InventoryQty = Input.InventoryQty;
            frame.Attributes = Input.Attributes;
            frame.IsActive = Input.IsActive;

            frame.FrameMaterial = Input.FrameMaterial;
            frame.FrameType = Input.FrameType;
            frame.BridgeWidth = Input.BridgeWidth;
            frame.TempleLength = Input.TempleLength;

            frame.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();

                if (Input.ImageFile != null && Input.ImageFile.Length > 0)
                {
                    await SaveProductImageAsync(frame.ProductId, Input.ImageFile, Input.ImageAltText);
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!FrameExists(Input.ProductId)) return NotFound();
                else throw;
            }

            // ── Trigger restock notification (background scope safe)
            if (wasOutOfStock && isNowInStock)
            {
                var baseUrl = _configuration["BaseUrl"] ?? "https://localhost:7234";
                var productUrl = $"{baseUrl}/Products/FrameDetails/{frame.ProductId}";

                Console.WriteLine($"[RESTOCK EMAIL] Triggering notify for FrameId={frame.ProductId}");
                Console.WriteLine($"[RESTOCK EMAIL] productUrl={productUrl}");

                // Fire-and-forget but create a new DI scope inside background task
                _ = Task.Run(async () =>
                {
                    using var scope = _scopeFactory.CreateScope();
                    try
                    {
                        var wishlistService = scope.ServiceProvider.GetRequiredService<IWishlistService>();
                        Console.WriteLine($"[RESTOCK EMAIL] Calling NotifyRestockAsync (background scope)...");
                        await wishlistService.NotifyRestockAsync(frame.ProductId, productUrl);
                        Console.WriteLine($"[RESTOCK EMAIL] SUCCESS — emails processed for FrameId={frame.ProductId}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[RESTOCK EMAIL ERROR] {ex.GetType().Name}: {ex.Message}");
                        if (ex.InnerException != null)
                            Console.WriteLine($"[RESTOCK EMAIL INNER] {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
                        Console.WriteLine($"[RESTOCK EMAIL STACK] {ex.StackTrace}");
                    }
                });
            }

            return RedirectToPage("./Index");
        }

        /// <summary>
        /// Handles GET request to delete a specific image
        /// </summary>
        public async Task<IActionResult> OnGetDeleteImageAsync(int imageId, int productId)
        {
            var image = await _context.ProductImages.FindAsync(imageId);
            if (image != null && image.ProductId == productId)
            {
                image.IsActive = false;
                await _context.SaveChangesAsync();
            }

            return RedirectToPage(new { id = productId });
        }

        /// <summary>
        /// Handles GET request to set an image as primary
        /// </summary>
        public async Task<IActionResult> OnGetSetPrimaryImageAsync(int imageId, int productId)
        {
            var existingPrimary = await _context.ProductImages
                .Where(pi => pi.ProductId == productId && pi.IsPrimary)
                .ToListAsync();

            foreach (var img in existingPrimary)
                img.IsPrimary = false;

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

            var imageUrl = $"/uploads/products/{productId}/{fileName}";

            var hasPrimaryImage = await _context.ProductImages
                .AnyAsync(pi => pi.ProductId == productId && pi.IsPrimary && pi.IsActive);

            var productImage = new ProductImage
            {
                ProductId = productId,
                ImageUrl = imageUrl,
                AltText = altText ?? Input.Name,
                IsPrimary = !hasPrimaryImage,
                SortOrder = 0,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _context.ProductImages.AddAsync(productImage);
            await _context.SaveChangesAsync();
        }

        private bool FrameExists(int id) => _context.Frames.Any(f => f.ProductId == id);
    }
}