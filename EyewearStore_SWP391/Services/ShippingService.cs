namespace EyewearStore_SWP391.Services;

public static class ShippingService
{
    public const decimal FreeShippingThreshold = 10_000_000m;  // 10 triệu
    public const decimal StandardShippingFee = 100_000m;  // 100k
    public const decimal CustomShippingFee = 150_000m;  // 150k
    public static decimal Calculate(decimal subtotal, string orderType = "Standard")
    {
        if (subtotal >= FreeShippingThreshold)
            return 0m;

        return string.Equals(orderType, "Custom", StringComparison.OrdinalIgnoreCase)
            ? CustomShippingFee
            : StandardShippingFee;
    }

    public static decimal CalculateForMixedCart(decimal subtotal, bool hasCustomItems)
    {
        if (subtotal >= FreeShippingThreshold)
            return 0m;

        return hasCustomItems ? CustomShippingFee : StandardShippingFee;
    }
}