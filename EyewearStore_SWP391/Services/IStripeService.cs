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
}
