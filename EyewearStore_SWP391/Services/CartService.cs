using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using EyewearStore_SWP391.Models;

namespace EyewearStore_SWP391.Services;

public class CartService : ICartService
{
    private const string DebugLogPath = @"d:\SWP\SWP391_GlassStore\.cursor\debug.log";

    private readonly EyewearStoreContext _context;

    public CartService(EyewearStoreContext context) => _context = context;

    private const decimal DefaultPrescriptionFeeVnd = 500_000m;

    public async Task<Cart?> GetCartByUserIdAsync(int userId)
    {
        // #region agent log
        try
        {
            var entryLog = System.Text.Json.JsonSerializer.Serialize(new { hypothesisId = "A,D", location = "CartService.cs:GetCartByUserIdAsync", message = "Entry", data = new { userId }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n";
            await System.IO.File.AppendAllTextAsync(DebugLogPath, entryLog);
        }
        catch { }
        // #endregion

        try
        {
            return await _context.Carts
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                        .ThenInclude(p => p.ProductImages)
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Service)
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Prescription)
                .FirstOrDefaultAsync(c => c.UserId == userId);
        }
        catch (SqlException ex)
        {
            // #region agent log
            try
            {
                var errLog = System.Text.Json.JsonSerializer.Serialize(new { hypothesisId = "A,B,C", location = "CartService.cs:GetCartByUserIdAsync", message = "SqlException", data = new { ex.Message, number = ex.Number }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n";
                await System.IO.File.AppendAllTextAsync(DebugLogPath, errLog);
            }
            catch { }
            // #endregion
            throw;
        }
    }

    public async Task AddToCartAsync(int userId, int productId, int quantity = 1, int? serviceId = null, string? tempPrescriptionJson = null, int? prescriptionId = null)
    {
        if (quantity <= 0) throw new ArgumentException("Quantity must be greater than 0");

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
            if (product == null) throw new InvalidOperationException("Product does not exist");
            if (!product.IsActive) throw new InvalidOperationException("Product is no longer active");

            // Check inventory if applicable
            if (product.InventoryQty.HasValue && product.InventoryQty < quantity)
            {
                throw new InvalidOperationException("Quantity exceeds available stock");
            }

            // Verify service if provided
            if (serviceId.HasValue)
            {
                var service = await _context.Services.FindAsync(serviceId.Value);
                if (service == null) throw new InvalidOperationException("Service does not exist");
            }

            // Resolve prescription fee when prescription is selected (for lenses)
            decimal prescriptionFee = 0m;
            if (prescriptionId.HasValue && prescriptionId.Value > 0)
            {
                var prescription = await _context.PrescriptionProfiles
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.PrescriptionId == prescriptionId.Value && p.UserId == userId && p.IsActive);
                if (prescription == null)
                    throw new InvalidOperationException("Invalid or inactive prescription selected.");

                var lens = await _context.Lenses.FindAsync(productId);
                prescriptionFee = (lens != null && lens.IsPrescription) ? (lens.PrescriptionFee ?? DefaultPrescriptionFeeVnd) : 0m;
            }

            // Match existing by productId, serviceId, and prescriptionId (so same prescription = same line)
            var existing = cart.CartItems.FirstOrDefault(ci =>
                ci.ProductId == productId &&
                ci.ServiceId == serviceId &&
                ci.PrescriptionId == prescriptionId);

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
                    TempPrescriptionJson = tempPrescriptionJson,
                    PrescriptionId = prescriptionId,
                    PrescriptionFee = prescriptionFee
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
                .ThenInclude(p => p.ProductImages)
            .FirstOrDefaultAsync(ci => ci.CartItemId == cartItemId);
            
        if (item == null) throw new InvalidOperationException("Cart item does not exist");

        if (newQuantity <= 0)
        {
            _context.CartItems.Remove(item);
        }
        else
        {
            // Check inventory if applicable
            if (item.Product.InventoryQty.HasValue && item.Product.InventoryQty < newQuantity)
            {
                throw new InvalidOperationException("Quantity exceeds available stock");
            }
            item.Quantity = newQuantity;
            _context.CartItems.Update(item);
        }
        await _context.SaveChangesAsync();
    }

    public async Task UpdateItemPrescriptionAsync(int cartItemId, string? tempPrescriptionJson)
    {
        var item = await _context.CartItems.FindAsync(cartItemId);
        if (item == null) throw new InvalidOperationException("Cart item does not exist");
        item.TempPrescriptionJson = tempPrescriptionJson;
        _context.CartItems.Update(item);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateItemPrescriptionByIdAsync(int cartItemId, int? prescriptionId, int userId)
    {
        var item = await _context.CartItems
            .Include(ci => ci.Cart)
            .FirstOrDefaultAsync(ci => ci.CartItemId == cartItemId);
        if (item == null) throw new InvalidOperationException("Cart item does not exist");
        if (item.Cart.UserId != userId) throw new InvalidOperationException("Cart item does not belong to you.");

        decimal prescriptionFee = 0m;
        if (prescriptionId.HasValue && prescriptionId.Value > 0)
        {
            var prescription = await _context.PrescriptionProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.PrescriptionId == prescriptionId.Value && p.UserId == userId && p.IsActive);
            if (prescription == null)
                throw new InvalidOperationException("Invalid or inactive prescription selected.");

            var lens = await _context.Lenses.FindAsync(item.ProductId);
            
            prescriptionFee = (lens != null && lens.IsPrescription) ? (lens.PrescriptionFee ?? DefaultPrescriptionFeeVnd) : 0m;
        }

        item.PrescriptionId = prescriptionId;
        item.PrescriptionFee = prescriptionFee;
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
        var (_, _, grandTotal) = await GetCartTotalsBreakdownAsync(userId);
        return grandTotal;
    }

    public async Task<(decimal SubtotalBase, decimal PrescriptionFeesTotal, decimal GrandTotal)> GetCartTotalsBreakdownAsync(int userId)
    {
        var cart = await GetCartByUserIdAsync(userId);
        if (cart == null) return (0m, 0m, 0m);

        decimal subtotalBase = 0m;
        decimal prescriptionFeesTotal = 0m;
        foreach (var ci in cart.CartItems)
        {
            decimal baseUnit = ci.Product?.Price ?? 0m;
            if (ci.Service != null) baseUnit += ci.Service.Price;
            subtotalBase += baseUnit * ci.Quantity;
            prescriptionFeesTotal += ci.PrescriptionFee * ci.Quantity;
        }
        return (subtotalBase, prescriptionFeesTotal, subtotalBase + prescriptionFeesTotal);
    }
}