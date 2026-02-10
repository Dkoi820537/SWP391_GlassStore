using EyewearStore_SWP391.Models;
using Microsoft.EntityFrameworkCore;

namespace EyewearStore_SWP391.Services;

/// <summary>
/// Handles order lifecycle: creation from cart, payment confirmation,
/// inventory reduction, and cancellation.
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

    /// <inheritdoc />
    public async Task<Order> CreatePendingOrderAsync(int userId, int addressId)
    {
        using var tx = await _context.Database.BeginTransactionAsync();
        try
        {
            // Load the user's cart with product details
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                        .ThenInclude(p => p.ProductImages)
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Service)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null || !cart.CartItems.Any())
                throw new InvalidOperationException("Cart is empty.");

            // Validate stock availability for every item
            foreach (var ci in cart.CartItems)
            {
                if (ci.Product == null) continue;
                if (!ci.Product.IsActive)
                    throw new InvalidOperationException($"Product \"{ci.Product.Name}\" is no longer available.");
                if (ci.Product.InventoryQty.HasValue && ci.Product.InventoryQty < ci.Quantity)
                    throw new InvalidOperationException(
                        $"Insufficient stock for \"{ci.Product.Name}\". Available: {ci.Product.InventoryQty}, Requested: {ci.Quantity}");
            }

            // Calculate total
            decimal totalAmount = 0m;
            var orderItems = new List<OrderItem>();

            foreach (var ci in cart.CartItems)
            {
                decimal unitPrice = ci.Product?.Price ?? 0m;
                if (ci.Service != null)
                    unitPrice += ci.Service.Price;

                orderItems.Add(new OrderItem
                {
                    ProductId = ci.ProductId,
                    UnitPrice = unitPrice,
                    Quantity = ci.Quantity,
                    IsBundle = false,
                    SnapshotJson = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        ProductName = ci.Product?.Name,
                        ProductType = ci.Product?.ProductType,
                        ServiceName = ci.Service?.Name,
                        ImageUrl = ci.Product?.ProductImages
                            .Where(img => img.IsPrimary && img.IsActive)
                            .Select(img => img.ImageUrl)
                            .FirstOrDefault()
                    })
                });

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
            await _context.SaveChangesAsync();

            // Assign order ID to items and save
            foreach (var oi in orderItems)
                oi.OrderId = order.OrderId;

            _context.OrderItems.AddRange(orderItems);
            await _context.SaveChangesAsync();

            await tx.CommitAsync();

            _logger.LogInformation("Created pending order {OrderId} for user {UserId}, total {Total}",
                order.OrderId, userId, totalAmount);

            return order;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public async Task<Order?> GetOrderByStripeSessionIdAsync(string sessionId)
    {
        return await _context.Orders
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .FirstOrDefaultAsync(o => o.StripeSessionId == sessionId);
    }

    /// <inheritdoc />
    public async Task<List<Order>> GetOrdersByUserIdAsync(int userId)
    {
        return await _context.Orders
            .Where(o => o.UserId == userId)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
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
                _logger.LogWarning("MarkOrderPaidAsync: Order {OrderId} not found", orderId);
                return;
            }

            // Idempotency check — already paid
            if (order.Status == "Paid")
            {
                _logger.LogInformation("Order {OrderId} is already marked as Paid, skipping", orderId);
                return;
            }

            // Update order status
            order.Status = "Paid";
            order.StripePaymentIntentId = paymentIntentId;

            // Reduce inventory for each order item
            foreach (var oi in order.OrderItems)
            {
                var product = await _context.Products.FindAsync(oi.ProductId);
                if (product != null && product.InventoryQty.HasValue)
                {
                    product.InventoryQty -= oi.Quantity;
                    if (product.InventoryQty < 0)
                        product.InventoryQty = 0; // Safety guard
                    product.UpdatedAt = DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync();

            // Clear the user's cart
            await _cartService.ClearCartAsync(order.UserId);

            await tx.CommitAsync();

            _logger.LogInformation("Order {OrderId} marked as Paid. Inventory reduced, cart cleared for user {UserId}",
                orderId, order.UserId);
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    /// <inheritdoc />
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
}
