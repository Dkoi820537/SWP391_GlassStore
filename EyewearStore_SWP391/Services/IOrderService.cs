using EyewearStore_SWP391.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EyewearStore_SWP391.Services
{
    /// <summary>
    /// Handles order creation, retrieval, status updates, and inventory management.
    /// </summary>
    public interface IOrderService
    {
        /// <summary>Create a pending order from the user's current cart (optional prescription).</summary>
        Task<Order> CreatePendingOrderAsync(int userId, int addressId, int? prescriptionId = null);

        /// <summary>Get a single order by its primary key.</summary>
        Task<Order?> GetOrderByIdAsync(int orderId);

        /// <summary>Find an order by its Stripe Checkout Session ID.</summary>
        Task<Order?> GetOrderByStripeSessionIdAsync(string sessionId);

        /// <summary>Find all orders sharing the same Stripe Checkout Session ID (split-order support).</summary>
        Task<List<Order>> GetOrdersByStripeSessionIdAsync(string sessionId);

        /// <summary>Get all orders for a user, newest first.</summary>
        Task<List<Order>> GetOrdersByUserIdAsync(int userId);

        /// <summary>
        /// Mark order as Paid, reduce product inventory, clear the user's cart.
        /// Idempotent — safe to call multiple times for the same order.
        /// </summary>
        Task MarkOrderPaidAsync(int orderId, string paymentIntentId);

        /// <summary>Cancel a pending order (e.g. user abandoned Stripe Checkout).</summary>
        Task CancelOrderAsync(int orderId);

        /// <summary>
        /// Customer-initiated cancellation with automated refund.
        /// Validates order-type/status rules, issues Stripe refund, restores inventory.
        /// </summary>
        Task<DTOs.CancellationResult> RequestCancellationAsync(int orderId, int userId);

        /// <summary>Confirms an order after successful payment</summary>
        Task<Order> ConfirmOrderAsync(int orderId, string stripeSessionId);

        /// <summary>Get orders for a user (with items & product snapshots).</summary>
        Task<List<Order>> GetUserOrdersAsync(int userId);

        /// <summary>Get all orders sharing the same OrderGroupId (split-order siblings).</summary>
        Task<List<Order>> GetOrdersByGroupIdAsync(string orderGroupId);
    }
}