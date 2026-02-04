using System.ComponentModel.DataAnnotations;

namespace EyewearStore_SWP391.DTOs.Frame;

/// <summary>
/// Data Transfer Object for updating an existing frame product
/// </summary>
public class UpdateFrameDto
{
    /// <summary>
    /// The SKU (Stock Keeping Unit) for the product
    /// </summary>
    [Required(ErrorMessage = "SKU is required")]
    [StringLength(50, ErrorMessage = "SKU cannot exceed 50 characters")]
    public string Sku { get; set; } = null!;

    /// <summary>
    /// The name of the frame product
    /// </summary>
    [Required(ErrorMessage = "Name is required")]
    [StringLength(255, ErrorMessage = "Name cannot exceed 255 characters")]
    public string Name { get; set; } = null!;

    /// <summary>
    /// The description of the frame product
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The price of the frame product
    /// </summary>
    [Required(ErrorMessage = "Price is required")]
    [Range(0.01, 999999999999.99, ErrorMessage = "Price must be greater than 0")]
    public decimal Price { get; set; }

    /// <summary>
    /// The currency code (e.g., VND, USD)
    /// </summary>
    [Required(ErrorMessage = "Currency is required")]
    [StringLength(3, MinimumLength = 3, ErrorMessage = "Currency must be exactly 3 characters")]
    public string Currency { get; set; } = "VND";

    /// <summary>
    /// The inventory quantity (optional)
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Inventory quantity cannot be negative")]
    public int? InventoryQty { get; set; }

    /// <summary>
    /// Whether the product is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    // Frame-specific properties

    /// <summary>
    /// The material of the frame (e.g., Titanium, Acetate, Metal)
    /// </summary>
    [StringLength(100, ErrorMessage = "Frame material cannot exceed 100 characters")]
    public string? FrameMaterial { get; set; }

    /// <summary>
    /// The type of frame (e.g., Full-Rim, Half-Rim, Rimless)
    /// </summary>
    [StringLength(50, ErrorMessage = "Frame type cannot exceed 50 characters")]
    public string? FrameType { get; set; }

    /// <summary>
    /// The bridge width in millimeters
    /// </summary>
    [Range(0, 100, ErrorMessage = "Bridge width must be between 0 and 100mm")]
    public decimal? BridgeWidth { get; set; }

    /// <summary>
    /// The temple length in millimeters
    /// </summary>
    [Range(0, 200, ErrorMessage = "Temple length must be between 0 and 200mm")]
    public decimal? TempleLength { get; set; }
}
