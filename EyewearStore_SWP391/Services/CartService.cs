using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EyewearStore_SWP391.Models;

namespace EyewearStore_SWP391.Services
{
    public class CartService : ICartService
    {
        private readonly EyewearStoreContext _context;
        public CartService(EyewearStoreContext context) => _context = context;

        public async Task<Cart?> GetCartByUserIdAsync(int userId)
        {
            return await _context.Carts
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Frame)
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Lens)
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Bundle)
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Service)
                .FirstOrDefaultAsync(c => c.UserId == userId);
        }

        public async Task AddToCartAsync(int userId, string productType, int productId, int quantity = 1, string? tempPrescriptionJson = null)
        {
            if (quantity <= 0) throw new ArgumentException("Quantity phải lớn hơn 0");

            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (cart == null)
                {
                    cart = new Cart { UserId = userId, CreatedAt = DateTime.UtcNow };
                    _context.Carts.Add(cart);
                    await _context.SaveChangesAsync();
                }

                Frame? frame = null; Lense? lense = null; Bundle? bundle = null; Models.Service? service = null;
                decimal unitPrice = 0m;

                if (string.Equals(productType, "Frame", StringComparison.OrdinalIgnoreCase))
                {
                    frame = await _context.Frames.FindAsync(productId);
                    if (frame == null) throw new InvalidOperationException("Frame không tồn tại");
                    if (frame.StockQuantity < quantity) throw new InvalidOperationException("Số lượng vượt quá tồn kho");
                    unitPrice = frame.Price;
                }
                else if (string.Equals(productType, "Lens", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(productType, "Lense", StringComparison.OrdinalIgnoreCase))
                {
                    lense = await _context.Lenses.FindAsync(productId);
                    if (lense == null) throw new InvalidOperationException("Lens không tồn tại");
                    unitPrice = lense.Price;
                }
                else if (string.Equals(productType, "Bundle", StringComparison.OrdinalIgnoreCase))
                {
                    bundle = await _context.Bundles.FindAsync(productId);
                    if (bundle == null || !bundle.IsActive) throw new InvalidOperationException("Bundle không hợp lệ");
                    unitPrice = bundle.BundlePrice;
                }
                else if (string.Equals(productType, "Service", StringComparison.OrdinalIgnoreCase))
                {
                    service = await _context.Services.FindAsync(productId);
                    if (service == null) throw new InvalidOperationException("Service không tồn tại");
                    unitPrice = service.Price;
                }
                else throw new InvalidOperationException("ProductType không hợp lệ");

                var existing = cart.CartItems.FirstOrDefault(ci =>
                    ci.FrameId == frame?.FrameId &&
                    ci.LensId == lense?.LensId &&
                    ci.BundleId == bundle?.BundleId &&
                    ci.ServiceId == service?.ServiceId &&
                    (ci.TempPrescriptionJson ?? "") == (tempPrescriptionJson ?? ""));

                if (existing != null)
                {
                    existing.Quantity += quantity;
                    _context.CartItems.Update(existing);
                }
                else
                {
                    var item = new CartItem
                    {
                        CartId = cart.CartId,
                        FrameId = frame?.FrameId,
                        LensId = lense?.LensId,
                        BundleId = bundle?.BundleId,
                        ServiceId = service?.ServiceId,
                        Quantity = quantity,
                        IsBundle = bundle != null,
                        TempPrescriptionJson = tempPrescriptionJson
                    };
                    _context.CartItems.Add(item);
                }

                await _context.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task UpdateQuantityAsync(int cartItemId, int newQuantity)
        {
            var item = await _context.CartItems.FindAsync(cartItemId);
            if (item == null) throw new InvalidOperationException("Cart item không tồn tại");

            if (newQuantity <= 0)
            {
                _context.CartItems.Remove(item);
            }
            else
            {
                if (item.FrameId.HasValue)
                {
                    var frame = await _context.Frames.FindAsync(item.FrameId.Value);
                    if (frame != null && frame.StockQuantity < newQuantity)
                        throw new InvalidOperationException("Số lượng vượt quá tồn kho");
                }
                item.Quantity = newQuantity;
                _context.CartItems.Update(item);
            }
            await _context.SaveChangesAsync();
        }

        public async Task UpdateItemPrescriptionAsync(int cartItemId, string? tempPrescriptionJson)
        {
            var item = await _context.CartItems.FindAsync(cartItemId);
            if (item == null) throw new InvalidOperationException("Cart item không tồn tại");
            item.TempPrescriptionJson = tempPrescriptionJson;
            _context.CartItems.Update(item);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveItemAsync(int cartItemId)
        {
            var item = await _context.CartItems.FindAsync(cartItemId);
            if (item == null) return;
            _context.CartItems.Remove(item);
            await _context.SaveChangesAsync();
        }

        public async Task ClearCartAsync(int userId)
        {
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null) return;
            _context.CartItems.RemoveRange(cart.CartItems);
            await _context.SaveChangesAsync();
        }

        public async Task<decimal> CalculateCartTotalAsync(int userId)
        {
            var cart = await GetCartByUserIdAsync(userId);
            if (cart == null) return 0m;
            decimal total = 0m;
            foreach (var ci in cart.CartItems)
            {
                decimal unit = 0m;
                if (ci.Frame != null) unit = ci.Frame.Price;
                else if (ci.Lens != null) unit = ci.Lens.Price;
                else if (ci.Bundle != null) unit = ci.Bundle.BundlePrice;
                else if (ci.Service != null) unit = ci.Service.Price;
                total += unit * ci.Quantity;
            }
            return total;
        }
    }
}