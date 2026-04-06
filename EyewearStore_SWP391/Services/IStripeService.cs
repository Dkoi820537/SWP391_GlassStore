namespace EyewearStore_SWP391.Services;

/// <summary>
/// Wraps Stripe API calls for creating Checkout Sessions.
/// </summary>
public interface IStripeService
{
    /// <summary>
    /// Create a Stripe Checkout Session for the given order(s).
    /// Supports split-order checkout: multiple order IDs share one payment session.
    /// Returns the URL to redirect the user to.
    /// </summary>
    Task<string> CreateCheckoutSessionAsync(
        List<int> orderIds,
        List<DTOs.StripeLineItemDto> lineItems,
        string successUrl,
        string cancelUrl,
        string customerEmail);

    /// <summary>
    /// Issue a refund against a Stripe PaymentIntent.
    /// Uses an idempotency key to prevent duplicate refunds.
    /// </summary>
    /// <param name="paymentIntentId">The Stripe PaymentIntent ID (pi_xxx).</param>
    /// <param name="amountInSmallestUnit">Refund amount in smallest currency unit (VND = whole đồng).</param>
    /// <param name="idempotencyKey">Unique key to guarantee at-most-once processing.</param>
    Task<DTOs.StripeRefundResult> RefundPaymentAsync(
        string paymentIntentId,
        long amountInSmallestUnit,
        string idempotencyKey);
}
