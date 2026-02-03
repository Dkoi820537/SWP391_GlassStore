using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EyewearStore_SWP391.Models;

namespace EyewearStore_SWP391.Services;

public class CartService : ICartService
{
    private readonly EyewearStoreContext _context;
    
    public CartService(EyewearStoreContext context) => _context = context;

    public async Task<Cart?> GetCartByUserIdAsync(int userId)
    {
        return await _context.Carts
            .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
            .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Service)
            .FirstOrDefaultAsync(c => c.UserId == userId);
    }

    public async Task AddToCartAsync(int userId, int productId, int quantity = 1, int? serviceId = null, string? tempPrescriptionJson = null)
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

            // Verify product exists and is active
            var product = await _context.Products.FindAsync(productId);
            if (product == null) throw new InvalidOperationException("Product không tồn tại");
            if (!product.IsActive) throw new InvalidOperationException("Product không còn hoạt động");

            // Check inventory if applicable
            if (product.InventoryQty.HasValue && product.InventoryQty < quantity)
            {
                throw new InvalidOperationException("Số lượng vượt quá tồn kho");
            }

            // Verify service if provided
            if (serviceId.HasValue)
            {
                var service = await _context.Services.FindAsync(serviceId.Value);
                if (service == null) throw new InvalidOperationException("Service không tồn tại");
            }

            // Check if item already exists in cart
            var existing = cart.CartItems.FirstOrDefault(ci =>
                ci.ProductId == productId &&
                ci.ServiceId == serviceId &&
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
                    ProductId = productId,
                    ServiceId = serviceId,
                    Quantity = quantity,
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
        var item = await _context.CartItems
            .Include(ci => ci.Product)
            .FirstOrDefaultAsync(ci => ci.CartItemId == cartItemId);
            
        if (item == null) throw new InvalidOperationException("Cart item không tồn tại");

        if (newQuantity <= 0)
        {
            _context.CartItems.Remove(item);
        }
        else
        {
            // Check inventory if applicable
            if (item.Product.InventoryQty.HasValue && item.Product.InventoryQty < newQuantity)
            {
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
            decimal unit = ci.Product?.Price ?? 0m;
            // Add service price if applicable
            if (ci.Service != null)
            {
                unit += ci.Service.Price;
            }
            total += unit * ci.Quantity;
        }
        return total;
    }
}