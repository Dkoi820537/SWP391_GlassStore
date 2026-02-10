using EyewearStore_SWP391.Models;

namespace EyewearStore_SWP391.Services;

/// <summary>
/// Handles order creation, retrieval, status updates, and inventory management.
/// </summary>
public interface IOrderService
{
    /// <summary>Create a pending order from the user's current cart.</summary>
    Task<Order> CreatePendingOrderAsync(int userId, int addressId);

    /// <summary>Get a single order by its primary key.</summary>
    Task<Order?> GetOrderByIdAsync(int orderId);

    /// <summary>Find an order by its Stripe Checkout Session ID.</summary>
    Task<Order?> GetOrderByStripeSessionIdAsync(string sessionId);

    /// <summary>Get all orders for a user, newest first.</summary>
    Task<List<Order>> GetOrdersByUserIdAsync(int userId);

    /// <summary>
    /// Mark order as Paid, reduce product inventory, clear the user's cart.
    /// Idempotent — safe to call multiple times for the same order.
    /// </summary>
    Task MarkOrderPaidAsync(int orderId, string paymentIntentId);

    /// <summary>Cancel a pending order (e.g. user abandoned Stripe Checkout).</summary>
    Task CancelOrderAsync(int orderId);
}
