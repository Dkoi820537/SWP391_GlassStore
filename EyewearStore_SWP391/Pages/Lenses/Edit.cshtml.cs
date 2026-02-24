using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using EyewearStore_SWP391.Models;
using EyewearStore_SWP391.Models.ViewModels.Lens;
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

namespace EyewearStore_SWP391.Pages.Lenses
{
    public class EditModel : PageModel
    {
        private readonly EyewearStoreContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly IWishlistService _wishlistService;
        private readonly IConfiguration _configuration;
        private readonly IServiceScopeFactory _scopeFactory;

        private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
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

        [BindProperty]
        public EditLensViewModel Input { get; set; } = new();

        public List<ProductImage> ExistingImages { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            var lens = await _context.Lenses
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.ProductId == id);

            if (lens == null) return NotFound();

            var primaryImage = await _context.ProductImages
                .Where(pi => pi.ProductId == id && pi.IsPrimary && pi.IsActive)
                .Select(pi => pi.ImageUrl)
                .FirstOrDefaultAsync();

            ExistingImages = await _context.ProductImages
                .Where(pi => pi.ProductId == id && pi.IsActive)
                .OrderByDescending(pi => pi.IsPrimary)
                .ThenBy(pi => pi.SortOrder)
                .ToListAsync();

            Input = new EditLensViewModel
            {
                ProductId = lens.ProductId,
                Sku = lens.Sku,
                Name = lens.Name,
                Description = lens.Description,
                Price = lens.Price,
                Currency = lens.Currency,
                InventoryQty = lens.InventoryQty,
                Attributes = lens.Attributes,
                IsActive = lens.IsActive,
                LensType = lens.LensType,
                LensIndex = lens.LensIndex,
                IsPrescription = lens.IsPrescription,
                ExistingImageUrl = primaryImage
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Validate image
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
            {
                ExistingImages = await _context.ProductImages
                    .Where(pi => pi.ProductId == Input.ProductId && pi.IsActive)
                    .OrderByDescending(pi => pi.IsPrimary)
                    .ThenBy(pi => pi.SortOrder)
                    .ToListAsync();
                return Page();
            }

            var lens = await _context.Lenses
                .FirstOrDefaultAsync(l => l.ProductId == Input.ProductId);

            if (lens == null) return NotFound();

            // Check SKU uniqueness
            if (lens.Sku != Input.Sku)
            {
                var skuExists = await _context.Products
                    .AnyAsync(p => p.Sku == Input.Sku && p.ProductId != Input.ProductId);

                if (skuExists)
                {
                    ModelState.AddModelError("Input.Sku", "This SKU already exists.");
                    ExistingImages = await _context.ProductImages
                        .Where(pi => pi.ProductId == Input.ProductId && pi.IsActive)
                        .OrderByDescending(pi => pi.IsPrimary)
                        .ThenBy(pi => pi.SortOrder)
                        .ToListAsync();
                    return Page();
                }
            }

            // ── SNAPSHOT trước khi update ────────────────────────────────────────
            var previousQty = lens.InventoryQty ?? 0;
            var wasOutOfStock = previousQty <= 0;
            var newQty = Input.InventoryQty ?? 0;
            var isNowInStock = newQty > 0;

            Console.WriteLine($"======================================");
            Console.WriteLine($"[RESTOCK DEBUG] LensId={lens.ProductId}");
            Console.WriteLine($"[RESTOCK DEBUG] previousQty={previousQty}, wasOutOfStock={wasOutOfStock}");
            Console.WriteLine($"[RESTOCK DEBUG] newQty={newQty}, isNowInStock={isNowInStock}");
            Console.WriteLine($"[RESTOCK DEBUG] Will notify: {wasOutOfStock && isNowInStock}");
            Console.WriteLine($"======================================");

            // Update properties
            lens.Sku = Input.Sku;
            lens.Name = Input.Name;
            lens.Description = Input.Description;
            lens.Price = Input.Price;
            lens.Currency = Input.Currency;
            lens.InventoryQty = Input.InventoryQty;
            lens.Attributes = Input.Attributes;
            lens.IsActive = Input.IsActive;
            lens.LensType = Input.LensType;
            lens.LensIndex = Input.LensIndex;
            lens.IsPrescription = Input.IsPrescription;
            lens.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();

                if (Input.ImageFile != null && Input.ImageFile.Length > 0)
                    await SaveProductImageAsync(lens.ProductId, Input.ImageFile, Input.ImageAltText);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!LensExists(Input.ProductId)) return NotFound();
                else throw;
            }

            // ── Trigger restock notification ─────────────────────────────────────
            if (wasOutOfStock && isNowInStock)
            {
                var baseUrl = _configuration["BaseUrl"] ?? "https://localhost:7234";
                var productUrl = $"{baseUrl}/Products/LensDetails/{lens.ProductId}";

                Console.WriteLine($"[RESTOCK EMAIL] Triggering notify for LensId={lens.ProductId}");
                Console.WriteLine($"[RESTOCK EMAIL] productUrl={productUrl}");

                // Fire-and-forget but create a new DI scope inside the background task
                _ = Task.Run(async () =>
                {
                    using var scope = _scopeFactory.CreateScope();
                    try
                    {
                        var wishlistService = scope.ServiceProvider.GetRequiredService<IWishlistService>();
                        Console.WriteLine($"[RESTOCK EMAIL] Calling NotifyRestockAsync (background scope)...");
                        await wishlistService.NotifyRestockAsync(lens.ProductId, productUrl);
                        Console.WriteLine($"[RESTOCK EMAIL] SUCCESS — emails processed for LensId={lens.ProductId}");
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

        private bool LensExists(int id) => _context.Lenses.Any(l => l.ProductId == id);
    }
}