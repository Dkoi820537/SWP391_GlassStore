namespace EyewearStore_SWP391.DTOs;

/// <summary>
/// Result of an order cancellation request.
/// </summary>
public class CancellationResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public decimal RefundAmount { get; set; }
    public int OrderId { get; set; }
    public string? PaymentStatus { get; set; }

    public static CancellationResult Ok(int orderId, decimal refundAmount, string paymentStatus)
        => new() { Success = true, OrderId = orderId, RefundAmount = refundAmount, PaymentStatus = paymentStatus };

    public static CancellationResult Fail(int orderId, string error)
        => new() { Success = false, OrderId = orderId, ErrorMessage = error };
}
