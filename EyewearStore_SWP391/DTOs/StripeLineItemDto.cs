namespace EyewearStore_SWP391.DTOs;

/// <summary>
/// Data transfer object representing a single line item for a Stripe Checkout Session.
/// Prices are in the smallest currency unit (đồng for VND).
/// </summary>
public class StripeLineItemDto
{
    public string ProductName { get; set; } = null!;

    /// <summary>Unit price in the smallest currency unit (VND đồng = whole number).</summary>
    public long UnitAmountInSmallestUnit { get; set; }

    public int Quantity { get; set; }

    /// <summary>Optional product image URL (absolute URL).</summary>
    public string? ImageUrl { get; set; }
}
