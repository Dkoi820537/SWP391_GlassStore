using EyewearStore_SWP391.Models;
using Microsoft.EntityFrameworkCore;

namespace EyewearStore_SWP391.Services
{
    public class OrderService : IOrderService
    {
        private readonly EyewearStoreContext _context;
        private readonly ICartService _cartService;
        private readonly ILogger<OrderService> _logger;

        public OrderService(EyewearStoreContext context,
                            ICartService cartService,
                            ILogger<OrderService> logger)
        {
            _context = context;
            _cartService = cartService;
            _logger = logger;
        }

        // ── CreatePendingOrderAsync ──────────────────────────────────────────

        public async Task<Order> CreatePendingOrderAsync(
            int userId, int addressId, int? prescriptionId = null)
        {
            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                // Load cart with all navigations we need
                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                        .ThenInclude(ci => ci.Product)
                            .ThenInclude(p => p.ProductImages)
                    .Include(c => c.CartItems)
                        .ThenInclude(ci => ci.Service)
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (cart == null || !cart.CartItems.Any())
                    throw new InvalidOperationException("Cart is empty");

                var address = await _context.Addresses
                    .FirstOrDefaultAsync(a => a.AddressId == addressId && a.UserId == userId);
                if (address == null)
                    throw new InvalidOperationException("Invalid address");

                // Optional global prescription
                PrescriptionProfile? prescription = null;
                if (prescriptionId.HasValue && prescriptionId.Value > 0)
                {
                    prescription = await _context.PrescriptionProfiles
                        .AsNoTracking()
                        .FirstOrDefaultAsync(p =>
                            p.PrescriptionId == prescriptionId.Value &&
                            p.UserId == userId && p.IsActive);
                    if (prescription == null)
                        throw new InvalidOperationException("Invalid or inactive prescription selected.");
                }

                // Validate stock
                foreach (var ci in cart.CartItems)
                {
                    if (ci.Product == null) continue;
                    if (!ci.Product.IsActive)
                        throw new InvalidOperationException($"Product \"{ci.Product.Name}\" is no longer available.");
                    if (ci.Product.InventoryQty.HasValue && ci.Product.InventoryQty < ci.Quantity)
                        throw new InvalidOperationException($"Insufficient stock for \"{ci.Product.Name}\".");
                }

                // ── Collect all lensProductIds in ONE DB query ───────────────
                var lensIds = cart.CartItems
                    .Select(ci => CartService.ExtractLensProductId(ci.TempPrescriptionJson))
                    .Where(id => id.HasValue)
                    .Select(id => id!.Value)
                    .Distinct()
                    .ToList();

                var lensProducts = lensIds.Any()
                    ? await _context.Products
                        .Where(p => lensIds.Contains(p.ProductId))
                        .ToDictionaryAsync(p => p.ProductId)
                    : new Dictionary<int, Product>();

                // ── Build OrderItems ─────────────────────────────────────────
                var orderItems = new List<OrderItem>();
                decimal totalAmount = 0m;

                foreach (var ci in cart.CartItems)
                {
                    var lensId = CartService.ExtractLensProductId(ci.TempPrescriptionJson);
                    bool isServiceOrder = lensId.HasValue && ci.ServiceId.HasValue;

                    Product? lensProduct = lensId.HasValue
                        ? lensProducts.GetValueOrDefault(lensId.Value)
                        : null;

                    decimal framePrice = ci.Product?.Price ?? 0m;
                    decimal lensPrice = lensProduct?.Price ?? 0m;
                    decimal servicePrice = ci.Service?.Price ?? 0m;
                    decimal unitPrice = framePrice + lensPrice + servicePrice;

                    // ── Build rich SnapshotJson ──────────────────────────────
                    object snapshotObj;

                    if (isServiceOrder)
                    {
                        snapshotObj = new
                        {
                            // Flag that admin pages use to detect service orders
                            isServiceOrder = true,

                            // Frame
                            frameName = ci.Product?.Name,
                            framePrice,
                            productName = ci.Product?.Name,   // backward compat
                            productType = ci.Product?.ProductType,

                            // Lens
                            lensProductId = lensId,
                            lensProductName = lensProduct?.Name,
                            lensPrice,

                            // Service
                            serviceId = ci.ServiceId,
                            serviceName = ci.Service?.Name,
                            servicePrice,

                            // Image (frame's primary image)
                            imageUrl = ci.Product?.ProductImages?
                                                 .Where(img => img.IsPrimary && img.IsActive)
                                                 .Select(img => img.ImageUrl)
                                                 .FirstOrDefault(),

                            // Admin workflow fields (written as null initially)
                            serviceStatus = "Pending",
                            assignedTo = (string?)null,
                            internalNote = (string?)null
                        };
                    }
                    else
                    {
                        // ── Regular order (unchanged logic) ─────────────────
                        snapshotObj = new
                        {
                            isServiceOrder = false,
                            productName = ci.Product?.Name,
                            productType = ci.Product?.ProductType,
                            serviceName = ci.Service?.Name,
                            imageUrl = ci.Product?.ProductImages?
                                                .Where(img => img.IsPrimary && img.IsActive)
                                                .Select(img => img.ImageUrl)
                                                .FirstOrDefault(),
                            prescription = prescription == null ? null : new
                            {
                                id = prescription.PrescriptionId,
                                name = prescription.ProfileName,
                                rightSph = prescription.RightSph,
                                rightCyl = prescription.RightCyl,
                                rightAxis = prescription.RightAxis,
                                leftSph = prescription.LeftSph,
                                leftCyl = prescription.LeftCyl,
                                leftAxis = prescription.LeftAxis
                            }
                        };
                    }

                    var oi = new OrderItem
                    {
                        ProductId = ci.ProductId,
                        PrescriptionId = prescription?.PrescriptionId,
                        UnitPrice = unitPrice,
                        Quantity = ci.Quantity,
                        IsBundle = false,
                        SnapshotJson = System.Text.Json.JsonSerializer.Serialize(snapshotObj,
                                             new System.Text.Json.JsonSerializerOptions
                                             {
                                                 PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                                             })
                    };

                    orderItems.Add(oi);
                    totalAmount += unitPrice * ci.Quantity;
                }

                // ── Create Order ─────────────────────────────────────────────
                var order = new Order
                {
                    UserId = userId,
                    AddressId = addressId,
                    Status = "Pending",
                    TotalAmount = totalAmount,
                    PaymentMethod = "Stripe",
                    CreatedAt = DateTime.UtcNow
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                foreach (var oi in orderItems) oi.OrderId = order.OrderId;
                _context.OrderItems.AddRange(orderItems);
                await _context.SaveChangesAsync();

                await tx.CommitAsync();

                _logger.LogInformation(
                    "Created pending order {OrderId} for user {UserId} total {Total}",
                    order.OrderId, userId, totalAmount);

                return order;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        // ── Remaining methods unchanged ───────────────────────────────────────

        public async Task<Order?> GetOrderByIdAsync(int orderId)
            => await _context.Orders
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
                    .ThenInclude(p => p.ProductImages)
                .Include(o => o.Address)
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

        public async Task<Order?> GetOrderByStripeSessionIdAsync(string sessionId)
            => await _context.Orders
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.StripeSessionId == sessionId);

        public async Task<List<Order>> GetOrdersByUserIdAsync(int userId)
            => await _context.Orders
                .Where(o => o.UserId == userId)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

        public async Task MarkOrderPaidAsync(int orderId, string paymentIntentId)
        {
            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var order = await _context.Orders
                    .Include(o => o.OrderItems)
                    .FirstOrDefaultAsync(o => o.OrderId == orderId);

                if (order == null)
                {
                    _logger.LogWarning("MarkOrderPaidAsync: order {OrderId} not found", orderId);
                    return;
                }

                if (order.Status == "Pending Confirmation") return;

                order.Status = "Pending Confirmation";
                order.StripePaymentIntentId = paymentIntentId;

                foreach (var oi in order.OrderItems)
                {
                    var product = await _context.Products.FindAsync(oi.ProductId);
                    if (product?.InventoryQty != null)
                    {
                        product.InventoryQty -= oi.Quantity;
                        if (product.InventoryQty < 0) product.InventoryQty = 0;
                        var prop = product.GetType().GetProperty("UpdatedAt");
                        prop?.SetValue(product, DateTime.UtcNow);
                    }
                }

                await _context.SaveChangesAsync();
                await _cartService.ClearCartAsync(order.UserId);
                await tx.CommitAsync();

                _logger.LogInformation(
                    "Order {OrderId} marked Pending Confirmation for user {UserId}",
                    orderId, order.UserId);
            }
            catch { await tx.RollbackAsync(); throw; }
        }

        public async Task CancelOrderAsync(int orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return;
            if (order.Status == "Pending")
            {
                order.Status = "Cancelled";
                await _context.SaveChangesAsync();
            }
        }

        public async Task<Order> ConfirmOrderAsync(int orderId, string stripeSessionId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.OrderId == orderId)
                ?? throw new InvalidOperationException("Order not found");

            order.Status = "Confirmed";
            order.StripeSessionId = stripeSessionId;

            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.UserId == order.UserId);
            if (cart != null)
                _context.CartItems.RemoveRange(cart.CartItems);

            await _context.SaveChangesAsync();
            return order;
        }

        public async Task<List<Order>> GetUserOrdersAsync(int userId)
            => await _context.Orders
                .Where(o => o.UserId == userId)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Prescription)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
    }
}