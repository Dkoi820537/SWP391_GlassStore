using EyewearStore_SWP391.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EyewearStore_SWP391.Services
{
    public interface IWishlistService
    {
        Task<(bool success, string message)> AddToWishlistAsync(int userId, int productId);
        Task<bool> RemoveFromWishlistAsync(int userId, int productId);
        Task<List<WishlistItemDto>> GetUserWishlistAsync(int userId);
        Task<bool> IsInWishlistAsync(int userId, int productId);
        Task NotifyRestockAsync(int productId, string productUrl);
    }

    public class WishlistItemDto
    {
        public int WishlistId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = "";
        public string Sku { get; set; } = "";
        public decimal Price { get; set; }
        public string? PrimaryImageUrl { get; set; }
        public bool IsInStock { get; set; }
        public int? InventoryQty { get; set; }
        public DateTime AddedAt { get; set; }
    }

    public class WishlistService : IWishlistService
    {
        private readonly EyewearStoreContext _context;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;

        public WishlistService(
            EyewearStoreContext context,
            IEmailService emailService,
            IConfiguration configuration)
        {
            _context = context;
            _emailService = emailService;
            _configuration = configuration;
        }

        public async Task<(bool success, string message)> AddToWishlistAsync(int userId, int productId)
        {
            // Check if product exists
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.ProductId == productId && p.IsActive);

            if (product == null)
            {
                return (false, "Product not found or inactive.");
            }

            // ✅ CRITICAL: Only allow adding to wishlist if OUT OF STOCK
            if (product.InventoryQty > 0)
            {
                return (false, "This product is currently in stock. You can add it to your cart!");
            }

            // Check if already in wishlist
            var exists = await _context.Wishlists
                .AnyAsync(w => w.UserId == userId && w.ProductId == productId);

            if (exists)
            {
                return (false, "Product is already in your wishlist.");
            }

            // Add to wishlist
            var wishlist = new Wishlist
            {
                UserId = userId,
                ProductId = productId,
                NotifyOnRestock = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Wishlists.Add(wishlist);
            await _context.SaveChangesAsync();

            return (true, "Added to wishlist! We'll notify you when it's back in stock.");
        }

        public async Task<bool> RemoveFromWishlistAsync(int userId, int productId)
        {
            var wishlist = await _context.Wishlists
                .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId);

            if (wishlist == null)
            {
                return false;
            }

            _context.Wishlists.Remove(wishlist);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<WishlistItemDto>> GetUserWishlistAsync(int userId)
        {
            var items = await _context.Wishlists
                .Where(w => w.UserId == userId)
                .Include(w => w.Product)
                    .ThenInclude(p => p.ProductImages)
                .OrderByDescending(w => w.CreatedAt)
                .Select(w => new WishlistItemDto
                {
                    WishlistId = w.WishlistId,
                    ProductId = w.ProductId,
                    ProductName = w.Product.Name,
                    Sku = w.Product.Sku,
                    Price = w.Product.Price,
                    PrimaryImageUrl = w.Product.ProductImages
                        .Where(i => i.IsPrimary && i.IsActive)
                        .Select(i => i.ImageUrl)
                        .FirstOrDefault(),
                    IsInStock = w.Product.InventoryQty > 0,
                    InventoryQty = w.Product.InventoryQty,
                    AddedAt = w.CreatedAt
                })
                .ToListAsync();

            return items;
        }

        public async Task<bool> IsInWishlistAsync(int userId, int productId)
        {
            return await _context.Wishlists
                .AnyAsync(w => w.UserId == userId && w.ProductId == productId);
        }

        public async Task NotifyRestockAsync(int productId, string productUrl)
        {
            // Get all users who wishlisted this product with notification enabled
            var wishlistUsers = await _context.Wishlists
                .Where(w => w.ProductId == productId && w.NotifyOnRestock)
                .Include(w => w.User)
                .Include(w => w.Product)
                .ToListAsync();

            if (!wishlistUsers.Any())
            {
                return;
            }

            var product = wishlistUsers.First().Product;

            // Send email to each user
            foreach (var wishlist in wishlistUsers)
            {
                try
                {
                    await _emailService.SendRestockNotificationAsync(
                        wishlist.User.Email,
                        wishlist.User.FullName ?? wishlist.User.Email,
                        product.Name,
                        productUrl
                    );
                }
                catch (Exception ex)
                {
                    // Log error but continue with other users
                    Console.WriteLine($"Failed to send restock email to {wishlist.User.Email}: {ex.Message}");
                }
            }
        }
    }
}






























































































































































