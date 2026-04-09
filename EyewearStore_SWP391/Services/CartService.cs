using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using EyewearStore_SWP391.Models;

namespace EyewearStore_SWP391.Services;

public class CartService : ICartService
{
    private const string DebugLogPath = @"d:\SWP\SWP391_GlassStore\.cursor\debug.log";
    private readonly EyewearStoreContext _context;
    private const decimal DefaultPrescriptionFeeVnd = 500_000m;

    // Key dùng để nhúng lensProductId vào TempPrescriptionJson
    private const string LensIdKey = "lensProductId";

    public CartService(EyewearStoreContext context) => _context = context;

    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>Đọc lensProductId đã nhúng trong TempPrescriptionJson (nếu có)</summary>
    public static int? ExtractLensProductId(string? json)
    {
        if (string.IsNullOrEmpty(json)) return null;
        try
        {
            var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty(LensIdKey, out var el)
                && el.TryGetInt32(out var id))
                return id;
        }
        catch { }
        return null;
    }

    /// <summary>Tạo JSON chỉ chứa lensProductId để lưu vào TempPrescriptionJson</summary>
    private static string EncodeLensId(int lensProductId)
        => JsonSerializer.Serialize(new { lensProductId });

    // ── GetCartByUserIdAsync ─────────────────────────────────────────────────
    // ── Lightweight summary for navbar (1 query, no includes) ─────────────
    public async Task<(int ItemCount, decimal Subtotal)> GetCartSummaryAsync(int userId)
    {
        var summary = await _context.Carts
            .Where(c => c.UserId == userId)
            .Select(c => new
            {
                ItemCount = c.CartItems.Sum(ci => ci.Quantity),
                Subtotal = c.CartItems.Sum(ci =>
                    (ci.Product != null ? ci.Product.Price : 0m) * ci.Quantity)
            })
            .FirstOrDefaultAsync();

        return summary == null ? (0, 0m) : (summary.ItemCount, summary.Subtotal);
    }

    // ── Full cart with all navigation properties ─────────────────────────────

    public async Task<Cart?> GetCartByUserIdAsync(int userId)
    {
        return await _context.Carts
            .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
                    .ThenInclude(p => p.ProductImages)
            .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Service)
            .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Prescription)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == userId);
    }

    // ── AddToCartAsync (đơn thường — giữ nguyên) ────────────────────────────

    public async Task AddToCartAsync(int userId, int productId, int quantity = 1,
        int? serviceId = null, string? tempPrescriptionJson = null,
        int? prescriptionId = null)
    {
        if (quantity <= 0) throw new ArgumentException("Quantity must be greater than 0");

        using var tx = await _context.Database.BeginTransactionAsync();
        try
        {
            var cart = await GetOrCreateCartAsync(userId);

            var product = await _context.Products.FindAsync(productId);
            if (product == null) throw new InvalidOperationException("Product does not exist");
            if (!product.IsActive) throw new InvalidOperationException("Product is no longer active");
            if (product.AvailableStock < quantity)
                throw new InvalidOperationException("Quantity exceeds available stock");

            if (serviceId.HasValue)
            {
                var svc = await _context.Services.FindAsync(serviceId.Value);
                if (svc == null) throw new InvalidOperationException("Service does not exist");
            }

            decimal prescriptionFee = 0m;
            if (prescriptionId.HasValue && prescriptionId.Value > 0)
            {
                var rx = await _context.PrescriptionProfiles.AsNoTracking()
                    .FirstOrDefaultAsync(p => p.PrescriptionId == prescriptionId.Value
                                           && p.UserId == userId && p.IsActive);
                if (rx == null)
                    throw new InvalidOperationException("Invalid or inactive prescription selected.");

                var lens = await _context.Lenses.FindAsync(productId);
                prescriptionFee = (lens != null && lens.IsPrescription)
                    ? (lens.PrescriptionFee ?? DefaultPrescriptionFeeVnd) : 0m;
            }

            var existing = cart.CartItems.FirstOrDefault(ci =>
                ci.ProductId == productId &&
                ci.ServiceId == serviceId &&
                ci.PrescriptionId == prescriptionId &&
                ExtractLensProductId(ci.TempPrescriptionJson) == null); // bỏ qua đơn gia công

            if (existing != null)
            {
                existing.Quantity += quantity;
                _context.CartItems.Update(existing);
            }
            else
            {
                _context.CartItems.Add(new CartItem
                {
                    CartId = cart.CartId,
                    ProductId = productId,
                    ServiceId = serviceId,
                    Quantity = quantity,
                    TempPrescriptionJson = tempPrescriptionJson,
                    PrescriptionId = prescriptionId,
                    PrescriptionFee = prescriptionFee
                });
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

    // ── AddServiceOrderAsync (đơn gia công: Frame + Lens + Service) ──────────

    /// <summary>
    /// Tạo 1 CartItem đặc biệt:
    ///   ProductId            = frameProductId
    ///   ServiceId            = serviceId
    ///   TempPrescriptionJson = {"lensProductId": lensProductId}
    ///
    /// Không merge với item cũ — mỗi đơn gia công là 1 dòng riêng.
    /// </summary>
    public async Task AddServiceOrderAsync(int userId, int frameProductId, int lensProductId,
        int serviceId, int quantity = 1, int? prescriptionId = null)
    {
        if (quantity <= 0) throw new ArgumentException("Quantity must be greater than 0");

        using var tx = await _context.Database.BeginTransactionAsync();
        try
        {
            var cart = await GetOrCreateCartAsync(userId);

            // Validate Frame
            var frame = await _context.Products.FindAsync(frameProductId);
            if (frame == null || !frame.IsActive)
                throw new InvalidOperationException("Frame product is unavailable");
            if (frame.AvailableStock < quantity)
                throw new InvalidOperationException("Frame quantity exceeds available stock");

            // Validate Lens
            var lens = await _context.Products.FindAsync(lensProductId);
            if (lens == null || !lens.IsActive)
                throw new InvalidOperationException("Lens product is unavailable");
            if (lens.AvailableStock < quantity)
                throw new InvalidOperationException("Lens quantity exceeds available stock");

            // Validate Frame-Lens Compatibility (Category-based)
            var allowedLensTypes = await _context.FrameCompatibleLensTypes
                .Where(c => c.FrameProductId == frameProductId)
                .Select(c => c.LensType)
                .ToListAsync();

            if (allowedLensTypes.Any())
            {
                // If the frame has restricted compatibility, the lens must match one of the allowed types.
                // We need to fetch the LensType from the Lens table specifically (since we only have the Product base entity above).
                var lensDetails = await _context.Lenses.FindAsync(lensProductId);
                if (lensDetails == null || string.IsNullOrEmpty(lensDetails.LensType) || !allowedLensTypes.Contains(lensDetails.LensType))
                {
                    throw new InvalidOperationException("The selected lens is not compatible with this frame.");
                }
            }

            // Validate Service
            var svc = await _context.Services.FindAsync(serviceId);
            if (svc == null || !svc.IsActive)
                throw new InvalidOperationException("Service is unavailable");

            // Validate Prescription (optional)
            decimal prescriptionFee = 0m;
            if (prescriptionId.HasValue && prescriptionId.Value > 0)
            {
                var rx = await _context.PrescriptionProfiles.AsNoTracking()
                    .FirstOrDefaultAsync(p => p.PrescriptionId == prescriptionId.Value
                                           && p.UserId == userId && p.IsActive);
                if (rx == null)
                    throw new InvalidOperationException("Invalid or inactive prescription selected.");

                // Use the lens-specific prescription fee
                var lensEntity = await _context.Lenses.FindAsync(lensProductId);
                if (lensEntity != null && lensEntity.IsPrescription)
                    prescriptionFee = lensEntity.PrescriptionFee ?? DefaultPrescriptionFeeVnd;
            }

            _context.CartItems.Add(new CartItem
            {
                CartId = cart.CartId,
                ProductId = frameProductId,
                ServiceId = serviceId,
                Quantity = quantity,
                TempPrescriptionJson = EncodeLensId(lensProductId),  // lưu LensId ở đây
                PrescriptionId = prescriptionId,
                PrescriptionFee = prescriptionFee
            });

            await _context.SaveChangesAsync();
            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    // ── GetCartTotalsBreakdownAsync ──────────────────────────────────────────
    public async Task<(decimal SubtotalBase, decimal PrescriptionFeesTotal, decimal ShippingFee, decimal GrandTotal)>
        GetCartTotalsBreakdownAsync(int userId)
    {
        var cart = await GetCartByUserIdAsync(userId);
        return await GetCartTotalsBreakdownAsync(cart);
    }

    public async Task<(decimal SubtotalBase, decimal PrescriptionFeesTotal, decimal ShippingFee, decimal GrandTotal)>
        GetCartTotalsBreakdownAsync(Cart? cart)
    {
        if (cart == null) return (0m, 0m, 0m, 0m);

        // Lấy tất cả lensProductId cần load thêm giá
        var lensIds = cart.CartItems
            .Select(ci => ExtractLensProductId(ci.TempPrescriptionJson))
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        // Load giá các Lens 1 lần duy nhất
        var lensPrices = lensIds.Any()
            ? await _context.Products
                .Where(p => lensIds.Contains(p.ProductId))
                .ToDictionaryAsync(p => p.ProductId, p => p.Price)
            : new Dictionary<int, decimal>();

        decimal subtotalBase = 0m;
        decimal prescriptionFeesTotal = 0m;
        bool hasCustomItems = false;

        foreach (var ci in cart.CartItems)
        {
            decimal baseUnit = ci.Product?.Price ?? 0m;

            // Nếu là đơn gia công, cộng thêm giá Lens
            var lensId = ExtractLensProductId(ci.TempPrescriptionJson);
            if (lensId.HasValue && lensPrices.TryGetValue(lensId.Value, out var lensPrice))
            {
                baseUnit += lensPrice;
                hasCustomItems = true;
            }

            // Cộng phí gia công / service
            if (ci.Service != null)
                baseUnit += ci.Service.Price;

            subtotalBase += baseUnit * ci.Quantity;
            prescriptionFeesTotal += ci.PrescriptionFee * ci.Quantity;
        }

        // Tính phí vận chuyển
        var shippingFee = ShippingService.CalculateForMixedCart(subtotalBase, hasCustomItems);

        return (subtotalBase, prescriptionFeesTotal, shippingFee, subtotalBase + prescriptionFeesTotal + shippingFee);
    }

    // ── Các method còn lại giữ nguyên ────────────────────────────────────────

    public async Task UpdateQuantityAsync(int cartItemId, int newQuantity)
    {
        var item = await _context.CartItems
            .Include(ci => ci.Product).ThenInclude(p => p.ProductImages)
            .FirstOrDefaultAsync(ci => ci.CartItemId == cartItemId);
        if (item == null) throw new InvalidOperationException("Cart item does not exist");

        if (newQuantity <= 0)
            _context.CartItems.Remove(item);
        else
        {
            if (item.Product.AvailableStock < newQuantity)
                throw new InvalidOperationException("Quantity exceeds available stock");
            item.Quantity = newQuantity;
            _context.CartItems.Update(item);
        }
        await _context.SaveChangesAsync();
    }

    public async Task UpdateItemPrescriptionAsync(int cartItemId, string? tempPrescriptionJson)
    {
        var item = await _context.CartItems.FindAsync(cartItemId);
        if (item == null) throw new InvalidOperationException("Cart item does not exist");
        // Không ghi đè nếu item là đơn gia công
        if (ExtractLensProductId(item.TempPrescriptionJson).HasValue)
            throw new InvalidOperationException("Cannot update prescription on a service order item.");
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
            var rx = await _context.PrescriptionProfiles.AsNoTracking()
                .FirstOrDefaultAsync(p => p.PrescriptionId == prescriptionId.Value
                                       && p.UserId == userId && p.IsActive);
            if (rx == null)
                throw new InvalidOperationException("Invalid or inactive prescription selected.");

            var lens = await _context.Lenses.FindAsync(item.ProductId);
            prescriptionFee = (lens != null && lens.IsPrescription)
                ? (lens.PrescriptionFee ?? DefaultPrescriptionFeeVnd) : 0m;
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
        var (_, _, _, grandTotal) = await GetCartTotalsBreakdownAsync(userId);
        return grandTotal;
    }
    // ── Private helpers ──────────────────────────────────────────────────────

    private async Task<Cart> GetOrCreateCartAsync(int userId)
    {
        var cart = await _context.Carts
            .Include(c => c.CartItems)
            .FirstOrDefaultAsync(c => c.UserId == userId);
        if (cart != null) return cart;

        cart = new Cart { UserId = userId, CreatedAt = DateTime.UtcNow };
        _context.Carts.Add(cart);
        await _context.SaveChangesAsync();
        return cart;
    }
}
