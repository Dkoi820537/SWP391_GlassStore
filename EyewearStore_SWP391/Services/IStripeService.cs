namespace EyewearStore_SWP391.Services;

/// <summary>
/// Wraps Stripe API calls for creating Checkout Sessions.
/// </summary>
public interface IStripeService
{
    /// <summary>
    /// Create a Stripe Checkout Session for the given order.
    /// Returns the URL to redirect the user to.
    /// </summary>
    Task<string> CreateCheckoutSessionAsync(
        int orderId,
        List<DTOs.StripeLineItemDto> lineItems,
        string successUrl,
        string cancelUrl,
        string customerEmail);
}
