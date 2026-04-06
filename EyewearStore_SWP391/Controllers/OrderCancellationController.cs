using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using EyewearStore_SWP391.Services;

namespace EyewearStore_SWP391.Controllers;

/// <summary>
/// Handles customer-initiated order cancellation with automated refund.
/// POST /api/orders/{orderId}/cancel
/// </summary>
[ApiController]
[Route("api/orders")]
public class OrderCancellationController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly ILogger<OrderCancellationController> _logger;

    public OrderCancellationController(
        IOrderService orderService,
        ILogger<OrderCancellationController> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }

    /// <summary>
    /// Cancel an order and issue an automated refund if applicable.
    /// Only the order owner can cancel. Re-validates status server-side.
    /// </summary>
    [HttpPost("{orderId}/cancel")]
    public async Task<IActionResult> CancelOrder(int orderId)
    {
        // ── Authentication check ─────────────────────────────────────
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
        {
            return Unauthorized(new { error = "Please log in to cancel an order." });
        }

        _logger.LogInformation(
            "Cancellation requested for Order {OrderId} by User {UserId}",
            orderId, userId);

        var result = await _orderService.RequestCancellationAsync(orderId, userId);

        if (!result.Success)
        {
            _logger.LogWarning(
                "Cancellation denied for Order {OrderId}: {Error}",
                orderId, result.ErrorMessage);
            return BadRequest(new
            {
                error = result.ErrorMessage,
                orderId = result.OrderId
            });
        }

        return Ok(new
        {
            message = "Order cancelled successfully.",
            orderId = result.OrderId,
            refundAmount = result.RefundAmount,
            paymentStatus = result.PaymentStatus
        });
    }
}
