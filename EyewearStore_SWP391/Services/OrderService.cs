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

        // ── Helper: write a status history row ──────────────────────────────
        private void RecordStatusHistory(int orderId, string status,
                                         string actor = "System", string? note = null)
        {
            _context.OrderStatusHistories.Add(new OrderStatusHistory
            {
                OrderId = orderId,
                Status = status,
                Actor = actor,
                Note = note,
                CreatedAt = DateTime.UtcNow
            });
        }

        // ── CreatePendingOrderAsync ──────────────────────────────────────────

        public async Task<Order> CreatePendingOrderAsync(
            int userId, int addressId, int? prescriptionId = null)
        {
            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
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

                foreach (var ci in cart.CartItems)
                {
                    if (ci.Product == null) continue;
                    if (!ci.Product.IsActive)
                        throw new InvalidOperationException($"Product \"{ci.Product.Name}\" is no longer available.");
                    if (ci.Product.InventoryQty.HasValue && ci.Product.InventoryQty < ci.Quantity)
                        throw new InvalidOperationException($"Insufficient stock for \"{ci.Product.Name}\".");
                }

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

                    object snapshotObj;
                    if (isServiceOrder)
                    {
                        snapshotObj = new
                        {
                            isServiceOrder = true,
                            frameName = ci.Product?.Name,
                            framePrice,
                            productName = ci.Product?.Name,
                            productType = ci.Product?.ProductType,
                            lensProductId = lensId,
                            lensProductName = lensProduct?.Name,
                            lensPrice,
                            serviceId = ci.ServiceId,
                            serviceName = ci.Service?.Name,
                            servicePrice,
                            imageUrl = ci.Product?.ProductImages?
                                               .Where(img => img.IsPrimary && img.IsActive)
                                               .Select(img => img.ImageUrl)
                                               .FirstOrDefault(),
                            serviceStatus = "Pending",
                            assignedTo = (string?)null,
                            internalNote = (string?)null
                        };
                    }
                    else
                    {
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
                await _context.SaveChangesAsync(); // get OrderId

                foreach (var oi in orderItems) oi.OrderId = order.OrderId;
                _context.OrderItems.AddRange(orderItems);

                // ── Record initial status history ────────────────────────────
                RecordStatusHistory(order.OrderId, "Pending", "Customer",
                    "Order placed successfully");

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

        // ── MarkOrderPaidAsync ───────────────────────────────────────────────

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

                // ── Set PaymentStatus based on payment method ────────────────
                if (order.PaymentMethod == "COD")
                {
                    order.PaymentStatus = "DepositPaid_AwaitingCOD";
                }
                else
                {
                    order.PaymentStatus = "FullyPaid";
                    order.DepositAmount = order.TotalAmount;
                    order.PendingBalance = 0;
                }

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

                // ── Record status history ────────────────────────────────────
                var historyNote = order.PaymentMethod == "COD"
                    ? $"Deposit of {order.DepositAmount:N0} VND received via Stripe (COD order)"
                    : "Payment received via Stripe";
                RecordStatusHistory(orderId, "Pending Confirmation", "System", historyNote);

                await _context.SaveChangesAsync();
                await _cartService.ClearCartAsync(order.UserId);
                await tx.CommitAsync();

                _logger.LogInformation(
                    "Order {OrderId} marked Pending Confirmation for user {UserId} (PaymentStatus: {PaymentStatus})",
                    orderId, order.UserId, order.PaymentStatus);
            }
            catch { await tx.RollbackAsync(); throw; }
        }

        // ── CancelOrderAsync ─────────────────────────────────────────────────

        public async Task CancelOrderAsync(int orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return;
            if (order.Status == "Pending")
            {
                order.Status = "Cancelled";

                RecordStatusHistory(orderId, "Cancelled", "Customer",
                    "Order cancelled by customer");

                await _context.SaveChangesAsync();
            }
        }

        // ── ConfirmOrderAsync ────────────────────────────────────────────────

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

            RecordStatusHistory(orderId, "Confirmed", "System",
                "Payment confirmed by Stripe");

            await _context.SaveChangesAsync();
            return order;
        }

        // ── Retrieval methods ─────────────────────────────────────────────────

        public async Task<Order?> GetOrderByIdAsync(int orderId)
            => await _context.Orders
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
                    .ThenInclude(p => p.ProductImages)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Returns)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Prescription)
                .Include(o => o.Address)
                .Include(o => o.User)
                .Include(o => o.Shipments)
                .Include(o => o.StatusHistories)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

        public async Task<Order?> GetOrderByStripeSessionIdAsync(string sessionId)
            => await _context.Orders
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.StripeSessionId == sessionId);

        /// <summary>
        /// Returns ALL orders sharing the same Stripe session ID (split-order support).
        /// </summary>
        public async Task<List<Order>> GetOrdersByStripeSessionIdAsync(string sessionId)
            => await _context.Orders
                .Where(o => o.StripeSessionId == sessionId)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
                    .ThenInclude(p => p.ProductImages)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Prescription)
                .Include(o => o.Address)
                .Include(o => o.StatusHistories)
                .OrderBy(o => o.OrderType) // Custom first, then Standard
                .ToListAsync();

        /// <summary>
        /// Returns all orders sharing the same OrderGroupId (split-order siblings).
        /// </summary>
        public async Task<List<Order>> GetOrdersByGroupIdAsync(string orderGroupId)
            => await _context.Orders
                .Where(o => o.OrderGroupId == orderGroupId)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
                    .ThenInclude(p => p.ProductImages)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Prescription)
                .Include(o => o.Address)
                .Include(o => o.StatusHistories)
                .OrderBy(o => o.OrderType)
                .ToListAsync();

        public async Task<List<Order>> GetOrdersByUserIdAsync(int userId)
            => await _context.Orders
                .Where(o => o.UserId == userId)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Returns)
                .Include(o => o.Shipments)
                .Include(o => o.StatusHistories)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

        public async Task<List<Order>> GetUserOrdersAsync(int userId)
            => await _context.Orders
                .Where(o => o.UserId == userId)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Prescription)
                .Include(o => o.StatusHistories)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
    }
}