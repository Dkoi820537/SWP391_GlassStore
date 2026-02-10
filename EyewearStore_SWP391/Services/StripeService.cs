using EyewearStore_SWP391.DTOs;
using EyewearStore_SWP391.Models;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;

namespace EyewearStore_SWP391.Services;

/// <summary>
/// Handles Stripe Checkout Session creation.
/// VND is a zero-decimal currency, so amounts are in whole đồng (no cent conversion).
/// </summary>
public class StripeService : IStripeService
{
    private readonly EyewearStoreContext _context;
    private readonly ILogger<StripeService> _logger;

    public StripeService(EyewearStoreContext context, ILogger<StripeService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<string> CreateCheckoutSessionAsync(
        int orderId,
        List<StripeLineItemDto> lineItems,
        string successUrl,
        string cancelUrl,
        string customerEmail)
    {
        // Build Stripe line items
        var stripeLineItems = lineItems.Select(item =>
        {
            var li = new SessionLineItemOptions
            {
                PriceData = new SessionLineItemPriceDataOptions
                {
                    // VND is a zero-decimal currency — amount is in whole đồng
                    UnitAmount = item.UnitAmountInSmallestUnit,
                    Currency = "vnd",
                    ProductData = new SessionLineItemPriceDataProductDataOptions
                    {
                        Name = item.ProductName,
                    }
                },
                Quantity = item.Quantity
            };

            // Add product image if available
            if (!string.IsNullOrEmpty(item.ImageUrl))
            {
                li.PriceData.ProductData.Images = new List<string> { item.ImageUrl };
            }

            return li;
        }).ToList();

        var options = new SessionCreateOptions
        {
            PaymentMethodTypes = new List<string> { "card" },
            LineItems = stripeLineItems,
            Mode = "payment",
            SuccessUrl = successUrl + "?session_id={CHECKOUT_SESSION_ID}",
            CancelUrl = cancelUrl,
            CustomerEmail = customerEmail,
            ClientReferenceId = orderId.ToString(),
            Metadata = new Dictionary<string, string>
            {
                { "order_id", orderId.ToString() }
            }
        };

        var service = new SessionService();
        var session = await service.CreateAsync(options);

        // Store the session ID on the order
        var order = await _context.Orders.FindAsync(orderId);
        if (order != null)
        {
            order.StripeSessionId = session.Id;
            await _context.SaveChangesAsync();
        }

        _logger.LogInformation("Created Stripe Checkout Session {SessionId} for Order {OrderId}",
            session.Id, orderId);

        return session.Url;
    }
}
