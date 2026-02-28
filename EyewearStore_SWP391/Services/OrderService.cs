    using EyewearStore_SWP391.Models;
    using Microsoft.EntityFrameworkCore;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    namespace EyewearStore_SWP391.Services
    {
        /// <summary>
        /// Handles order lifecycle: creation from cart, payment confirmation,
        /// inventory reduction, and cancellation.
        /// 
        /// IMPORTANT: This implementation does NOT change DB schema.
        /// Prescription selection is persisted inside OrderItem.SnapshotJson.
        /// </summary>
        public class OrderService : IOrderService
        {
            private readonly EyewearStoreContext _context;
            private readonly ICartService _cartService;
            private readonly ILogger<OrderService> _logger;

            public OrderService(EyewearStoreContext context, ICartService cartService, ILogger<OrderService> logger)
            {
                _context = context;
                _cartService = cartService;
                _logger = logger;
            }

            public async Task<Order> CreatePendingOrderAsync(int userId, int addressId, int? prescriptionId = null)
            {
                using var tx = await _context.Database.BeginTransactionAsync();
                try
                {
                    var cart = await _context.Carts
                        .Include(c => c.CartItems)
                            .ThenInclude(ci => ci.Product)
                                .ThenInclude(p => p.ProductImages)
                        .Include(c => c.CartItems)
                            .ThenInclude(ci => ci.Service) // OK if CartItem has Service nav
                        .FirstOrDefaultAsync(c => c.UserId == userId);

                    if (cart == null || !cart.CartItems.Any())
                        throw new InvalidOperationException("Cart is empty");

                    // Validate address ownership
                    var address = await _context.Addresses
                        .FirstOrDefaultAsync(a => a.AddressId == addressId && a.UserId == userId);
                    if (address == null)
                        throw new InvalidOperationException("Invalid address");

                    // Validate prescription if provided (must belong to user and active)
                    PrescriptionProfile? prescription = null;
                    if (prescriptionId.HasValue && prescriptionId.Value > 0)
                    {
                        prescription = await _context.PrescriptionProfiles
                            .AsNoTracking()
                            .FirstOrDefaultAsync(p => p.PrescriptionId == prescriptionId.Value && p.UserId == userId && p.IsActive);

                        if (prescription == null)
                            throw new InvalidOperationException("Invalid or inactive prescription selected.");
                    }

                    // Validate stock availability
                    foreach (var ci in cart.CartItems)
                    {
                        if (ci.Product == null) continue;
                        if (!ci.Product.IsActive)
                            throw new InvalidOperationException($"Product \"{ci.Product.Name}\" is no longer available.");
                        if (ci.Product.InventoryQty.HasValue && ci.Product.InventoryQty < ci.Quantity)
                            throw new InvalidOperationException($"Insufficient stock for \"{ci.Product.Name}\".");
                    }

                    // Build OrderItems (store snapshot JSON with product/service/prescription)
                    var orderItems = new List<OrderItem>();
                    decimal totalAmount = 0m;

                    foreach (var ci in cart.CartItems)
                    {
                        decimal unitPrice = ci.Product?.Price ?? 0m;
                        string? serviceName = null;
                        if (ci.Service != null)
                        {
                            unitPrice += ci.Service.Price;
                            serviceName = ci.Service.Name;
                        }

                        var snapshotObj = new
                        {
                            ProductName = ci.Product?.Name,
                            ProductType = ci.Product?.ProductType,
                            ServiceName = serviceName,
                            ImageUrl = ci.Product?.ProductImages?
                                        .Where(img => img.IsPrimary && img.IsActive)
                                        .Select(img => img.ImageUrl)
                                        .FirstOrDefault(),
                            Prescription = prescription == null ? null : new
                            {
                                Id = prescription.PrescriptionId,
                                Name = prescription.ProfileName,
                                RightSph = prescription.RightSph,
                                RightCyl = prescription.RightCyl,
                                RightAxis = prescription.RightAxis,
                                LeftSph = prescription.LeftSph,
                                LeftCyl = prescription.LeftCyl,
                                LeftAxis = prescription.LeftAxis
                            }
                        };

                        var oi = new OrderItem
                        {
                            ProductId = ci.ProductId,
                            PrescriptionId = prescription?.PrescriptionId, // works if OrderItem has this property (ok)
                            UnitPrice = unitPrice,
                            Quantity = ci.Quantity,
                            IsBundle = false,
                            SnapshotJson = System.Text.Json.JsonSerializer.Serialize(snapshotObj)
                        };

                        orderItems.Add(oi);
                        totalAmount += unitPrice * ci.Quantity;
                    }

                    // Create Order (ONLY use fields present in the Order model)
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

                    // Attach OrderId to items and save
                    foreach (var oi in orderItems) oi.OrderId = order.OrderId;
                    _context.OrderItems.AddRange(orderItems);
                    await _context.SaveChangesAsync();

                    await tx.CommitAsync();

                    _logger.LogInformation("Created pending order {OrderId} for user {UserId} total {Total}",
                        order.OrderId, userId, totalAmount);

                    return order;
                }
                catch
                {
                    await tx.RollbackAsync();
                    throw;
                }
            }

            public async Task<Order?> GetOrderByIdAsync(int orderId)
            {
                return await _context.Orders
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Product)
                            .ThenInclude(p => p.ProductImages)
                    .Include(o => o.Address)
                    .Include(o => o.User)
                    .FirstOrDefaultAsync(o => o.OrderId == orderId);
            }

            public async Task<Order?> GetOrderByStripeSessionIdAsync(string sessionId)
            {
                return await _context.Orders
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Product)
                    .FirstOrDefaultAsync(o => o.StripeSessionId == sessionId);
            }

            public async Task<List<Order>> GetOrdersByUserIdAsync(int userId)
            {
                return await _context.Orders
                    .Where(o => o.UserId == userId)
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Product)
                    .OrderByDescending(o => o.CreatedAt)
                    .ToListAsync();
            }

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

                    if (order.Status == "Pending Confirmation")
                    {
                        _logger.LogInformation("Order {OrderId} already Pending Confirmation", orderId);
                        return;
                    }

                    order.Status = "Pending Confirmation";
                    order.StripePaymentIntentId = paymentIntentId;

                    // Reduce inventory
                    foreach (var oi in order.OrderItems)
                    {
                        var product = await _context.Products.FindAsync(oi.ProductId);
                        if (product != null && product.InventoryQty.HasValue)
                        {
                            product.InventoryQty -= oi.Quantity;
                            if (product.InventoryQty < 0) product.InventoryQty = 0;

                            // If Product has UpdatedAt, try to set it via reflection (safe-guard)
                            var prop = product.GetType().GetProperty("UpdatedAt");
                            if (prop != null && prop.CanWrite)
                            {
                                prop.SetValue(product, DateTime.UtcNow);
                            }
                        }
                    }

                    await _context.SaveChangesAsync();

                    // Clear cart
                    await _cartService.ClearCartAsync(order.UserId);

                    await tx.CommitAsync();

                    _logger.LogInformation("Order {OrderId} marked Pending Confirmation, inventory reduced, cart cleared for user {UserId}",
                        orderId, order.UserId);
                }
                catch
                {
                    await tx.RollbackAsync();
                    throw;
                }
            }

            public async Task CancelOrderAsync(int orderId)
            {
                var order = await _context.Orders.FindAsync(orderId);
                if (order == null) return;

                if (order.Status == "Pending")
                {
                    order.Status = "Cancelled";
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Order {OrderId} cancelled", orderId);
                }
            }

            public async Task<Order> ConfirmOrderAsync(int orderId, string stripeSessionId)
            {
                var order = await _context.Orders
                    .Include(o => o.OrderItems)
                    .FirstOrDefaultAsync(o => o.OrderId == orderId);

                if (order == null) throw new InvalidOperationException("Order not found");

                order.Status = "Confirmed";
                order.StripeSessionId = stripeSessionId;
                // Do not set UpdatedAt if Order model doesn't have it

                // Clear user's cart items
                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                    .FirstOrDefaultAsync(c => c.UserId == order.UserId);
                if (cart != null)
                {
                    _context.CartItems.RemoveRange(cart.CartItems);
                }

                await _context.SaveChangesAsync();
                return order;
            }

            public async Task<List<Order>> GetUserOrdersAsync(int userId)
            {
                return await _context.Orders
                    .Where(o => o.UserId == userId)
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Product)
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Prescription) // If model has nav; otherwise SnapshotJson stores prescription
                    .OrderByDescending(o => o.CreatedAt)
                    .ToListAsync();
            }
        }
    }