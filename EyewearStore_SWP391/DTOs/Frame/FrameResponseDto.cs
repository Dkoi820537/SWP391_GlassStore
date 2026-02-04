namespace EyewearStore_SWP391.DTOs.Frame;

/// <summary>
/// Data Transfer Object for returning frame product data
/// </summary>
public class FrameResponseDto
{
    /// <summary>
    /// The unique identifier of the product (ProductId)
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
    /// The description of the frame product
    /// </summary>
    public string? Description { get; set; }

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
    /// Whether the product is active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// The date and time when the frame was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// The date and time when the frame was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    // Frame-specific properties

    /// <summary>
    /// The material of the frame (e.g., Titanium, Acetate, Metal)
    /// </summary>
    public string? FrameMaterial { get; set; }

    /// <summary>
    /// The type of frame (e.g., Full-Rim, Half-Rim, Rimless)
    /// </summary>
    public string? FrameType { get; set; }

    /// <summary>
    /// The bridge width in millimeters
    /// </summary>
    public decimal? BridgeWidth { get; set; }

    /// <summary>
    /// The temple length in millimeters
    /// </summary>
    public decimal? TempleLength { get; set; }

    /// <summary>
    /// Primary image URL for the product
    /// </summary>
    public string? PrimaryImageUrl { get; set; }
}
