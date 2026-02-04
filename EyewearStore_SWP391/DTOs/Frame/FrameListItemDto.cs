namespace EyewearStore_SWP391.DTOs.Frame;

/// <summary>
/// Lightweight Data Transfer Object for frame list views
/// </summary>
public class FrameListItemDto
{
    /// <summary>
    /// The unique identifier of the product
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// The SKU (Stock Keeping Unit) for the product
    /// </summary>
    public string Sku { get; set; } = null!;

    /// <summary>
    /// The name of the frame product
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// The price of the frame product
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// The currency code
    /// </summary>
    public string Currency { get; set; } = null!;

    /// <summary>
    /// The inventory quantity
    /// </summary>
    public int? InventoryQty { get; set; }

    /// <summary>
    /// The material of the frame (e.g., Titanium, Acetate, Metal)
    /// </summary>
    public string? FrameMaterial { get; set; }

    /// <summary>
    /// The type of frame (e.g., Full-Rim, Half-Rim, Rimless)
    /// </summary>
    public string? FrameType { get; set; }

    /// <summary>
    /// Whether the product is active
    /// </summary>
    public bool IsActive { get; set; }
}
