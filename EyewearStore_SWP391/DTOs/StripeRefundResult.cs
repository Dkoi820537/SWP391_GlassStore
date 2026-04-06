namespace EyewearStore_SWP391.DTOs;

/// <summary>
/// Result of a Stripe refund operation.
/// </summary>
public class StripeRefundResult
{
    public bool Success { get; set; }
    public string? RefundId { get; set; }
    public long AmountRefunded { get; set; }
    public string? ErrorMessage { get; set; }

    public static StripeRefundResult Ok(string refundId, long amount)
        => new() { Success = true, RefundId = refundId, AmountRefunded = amount };

    public static StripeRefundResult Fail(string error)
        => new() { Success = false, ErrorMessage = error };
}
