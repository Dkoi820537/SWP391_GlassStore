using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;
using EyewearStore_SWP391.Models;
using EyewearStore_SWP391.Services;

namespace EyewearStore_SWP391.Controllers;

/// <summary>
/// Handles Stripe webhook events. This endpoint receives POST requests from
/// Stripe when payment events occur (e.g. checkout.session.completed, charge.refunded).
/// Supports split-order checkout: a single payment session may cover multiple orders.
/// </summary>
[ApiController]
[Route("api/stripe")]
public class StripeWebhookController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly EyewearStoreContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<StripeWebhookController> _logger;

    public StripeWebhookController(
        IOrderService orderService,
        EyewearStoreContext context,
        IConfiguration configuration,
        ILogger<StripeWebhookController> logger)
    {
        _orderService = orderService;
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// POST /api/stripe/webhook
    /// Stripe sends events here. We verify the signature and handle
    /// checkout.session.completed and charge.refunded.
    /// </summary>
    [HttpPost("webhook")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> HandleWebhook()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        var webhookSecret = _configuration["Stripe:WebhookSecret"];
        
        if (string.IsNullOrEmpty(webhookSecret))
        {
            _logger.LogError("Stripe webhook secret is not configured.");
            return StatusCode(500, new { error = "Server improperly configured" });
        }

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

        // ── Handle checkout.session.completed ─────────────────────────────
        if (stripeEvent.Type == EventTypes.CheckoutSessionCompleted)
        {
            var session = stripeEvent.Data.Object as Session;
            if (session != null)
            {
                _logger.LogInformation(
                    "Checkout session completed: {SessionId}, PaymentIntent: {PaymentIntentId}, OrderIds: {OrderIds}",
                    session.Id, session.PaymentIntentId, session.ClientReferenceId);

                var orderIdStrings = (session.ClientReferenceId ?? "")
                    .Split(',', StringSplitOptions.RemoveEmptyEntries);

                if (orderIdStrings.Any())
                {
                    foreach (var idStr in orderIdStrings)
                    {
                        if (int.TryParse(idStr.Trim(), out var orderId))
                        {
                            var order = await _orderService.GetOrderByIdAsync(orderId);
                            if (order != null)
                            {
                                await _orderService.MarkOrderPaidAsync(order.OrderId, session.PaymentIntentId);
                                _logger.LogInformation(
                                    "Marked order {OrderId} as paid (split-order group)",
                                    order.OrderId);
                            }
                            else
                            {
                                _logger.LogWarning(
                                    "No order found for ID {OrderId} from session {SessionId}",
                                    orderId, session.Id);
                            }
                        }
                    }
                }
                else
                {
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
        }

        // ── Handle charge.refunded ────────────────────────────────────────
        // Safety-net: when Stripe confirms a refund asynchronously, update
        // the order's payment status to "Refunded" if not already done.
        if (stripeEvent.Type == EventTypes.ChargeRefunded)
        {
            var charge = stripeEvent.Data.Object as Charge;
            if (charge != null && !string.IsNullOrEmpty(charge.PaymentIntentId))
            {
                _logger.LogInformation(
                    "Charge refunded for PaymentIntent {PI}, amount refunded: {Amount}",
                    charge.PaymentIntentId, charge.AmountRefunded);

                var orders = await _context.Orders
                    .Where(o => o.StripePaymentIntentId == charge.PaymentIntentId)
                    .ToListAsync();

                foreach (var order in orders)
                {
                    if (order.PaymentStatus != "Refunded")
                    {
                        order.PaymentStatus = "Refunded";
                        order.RefundAmount = charge.AmountRefunded;
                        if (order.Status == "Cancellation_Pending")
                        {
                            order.Status = "Cancelled";
                            order.CancelledAt = DateTime.UtcNow;
                        }

                        _logger.LogInformation(
                            "Order {OrderId} payment status updated to Refunded via webhook",
                            order.OrderId);
                    }
                }

                await _context.SaveChangesAsync();
            }
        }

        // Return 200 to acknowledge receipt (Stripe expects this)
        return Ok();
    }
}
