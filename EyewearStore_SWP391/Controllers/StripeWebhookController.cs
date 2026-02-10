using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;
using EyewearStore_SWP391.Services;

namespace EyewearStore_SWP391.Controllers;

/// <summary>
/// Handles Stripe webhook events. This endpoint receives POST requests from
/// Stripe when payment events occur (e.g. checkout.session.completed).
/// </summary>
[ApiController]
[Route("api/stripe")]
public class StripeWebhookController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<StripeWebhookController> _logger;

    public StripeWebhookController(
        IOrderService orderService,
        IConfiguration configuration,
        ILogger<StripeWebhookController> logger)
    {
        _orderService = orderService;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// POST /api/stripe/webhook
    /// Stripe sends events here. We verify the signature and handle checkout.session.completed.
    /// </summary>
    [HttpPost("webhook")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> HandleWebhook()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        var webhookSecret = _configuration["Stripe:WebhookSecret"];

        Event stripeEvent;

        try
        {
            stripeEvent = EventUtility.ConstructEvent(
                json,
                Request.Headers["Stripe-Signature"],
                webhookSecret);
        }
        catch (StripeException ex)
        {
            _logger.LogWarning("Stripe webhook signature verification failed: {Message}", ex.Message);
            return BadRequest(new { error = "Webhook signature verification failed" });
        }

        _logger.LogInformation("Received Stripe event: {EventType} ({EventId})",
            stripeEvent.Type, stripeEvent.Id);

        // Handle checkout.session.completed
        if (stripeEvent.Type == EventTypes.CheckoutSessionCompleted)
        {
            var session = stripeEvent.Data.Object as Session;
            if (session != null)
            {
                _logger.LogInformation(
                    "Checkout session completed: {SessionId}, PaymentIntent: {PaymentIntentId}, OrderId: {OrderId}",
                    session.Id, session.PaymentIntentId, session.ClientReferenceId);

                // Find the order by Stripe Session ID
                var order = await _orderService.GetOrderByStripeSessionIdAsync(session.Id);
                if (order != null)
                {
                    await _orderService.MarkOrderPaidAsync(order.OrderId, session.PaymentIntentId);
                }
                else
                {
                    _logger.LogWarning("No order found for Stripe session {SessionId}", session.Id);
                }
            }
        }

        // Return 200 to acknowledge receipt (Stripe expects this)
        return Ok();
    }
}
