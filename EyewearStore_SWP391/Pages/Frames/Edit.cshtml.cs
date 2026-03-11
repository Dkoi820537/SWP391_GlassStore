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
        public EditFrameViewModel Input { get; set; } = new();

        public List<ProductImage> ExistingImages { get; set; } = new();

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
                // Frame specs
                FrameMaterial = frame.FrameMaterial,
                FrameType = frame.FrameType,
                BridgeWidth = frame.BridgeWidth,
                TempleLength = frame.TempleLength,
                // v2
                Brand = frame.Brand,
                Color = frame.Color,
                Gender = frame.Gender,
                FrameShape = frame.FrameShape,
                // v3
                LensWidth = frame.LensWidth,
                Origin = frame.Origin,
                ExistingImageUrl = primaryImage
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

            // Snapshot for restock detection
            var previousQty = frame.InventoryQty ?? 0;
            var wasOutOfStock = previousQty <= 0;
            var newQty = Input.InventoryQty ?? 0;
            var isNowInStock = newQty > 0;

            // Update all fields
            frame.Sku = Input.Sku;
            frame.Name = Input.Name;
            frame.Description = Input.Description;
            frame.Price = Input.Price;
            frame.Currency = Input.Currency;
            frame.InventoryQty = Input.InventoryQty;
            frame.Attributes = Input.Attributes;
            frame.IsActive = Input.IsActive;
            // Frame specs
            frame.FrameMaterial = Input.FrameMaterial;
            frame.FrameType = Input.FrameType;
            frame.BridgeWidth = Input.BridgeWidth;
            frame.TempleLength = Input.TempleLength;
            // v2
            frame.Brand = Input.Brand;
            frame.Color = Input.Color;
            frame.Gender = Input.Gender;
            frame.FrameShape = Input.FrameShape;
            // v3
            frame.LensWidth = Input.LensWidth;
            frame.Origin = Input.Origin;
            frame.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();

                if (Input.ImageFile != null && Input.ImageFile.Length > 0)
                    await SaveProductImageAsync(frame.ProductId, Input.ImageFile, Input.ImageAltText);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!FrameExists(Input.ProductId)) return NotFound();
                else throw;
            }

            // Restock notification
            if (wasOutOfStock && isNowInStock)
            {
                var baseUrl = _configuration["BaseUrl"] ?? "https://localhost:7234";
                var productUrl = $"{baseUrl}/Products/FrameDetails/{frame.ProductId}";

                _ = Task.Run(async () =>
                {
                    using var scope = _scopeFactory.CreateScope();
                    try
                    {
                        var wishlistService = scope.ServiceProvider.GetRequiredService<IWishlistService>();
                        await wishlistService.NotifyRestockAsync(frame.ProductId, productUrl);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[RESTOCK EMAIL ERROR] {ex.GetType().Name}: {ex.Message}");
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

            var hasPrimaryImage = await _context.ProductImages
                .AnyAsync(pi => pi.ProductId == productId && pi.IsPrimary && pi.IsActive);

            var productImage = new ProductImage
            {
                ProductId = productId,
                ImageUrl = $"/uploads/products/{productId}/{fileName}",
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