using System;

namespace EyewearStore_SWP391.Models.ViewModels.Shop;

/// <summary>
/// View model for a single product item in the catalog grid.
/// Represents either a Frame or Lens product.
/// </summary>
public class ProductCatalogItemViewModel
{
    /// <summary>
    /// The unique product identifier
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Product type: "Frame" or "Lens"
    /// </summary>
    public string ProductType { get; set; } = null!;

    /// <summary>
    /// Product SKU code
    /// </summary>
    public string Sku { get; set; } = null!;

    /// <summary>
    /// Product display name
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Product description (may be truncated for display)
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Product price
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Currency code (e.g., "USD", "VND")
    /// </summary>
    public string Currency { get; set; } = null!;

    /// <summary>
    /// URL to the primary product image
    /// </summary>
    public string? PrimaryImageUrl { get; set; }

    // Frame-specific properties (nullable for Lens products)

    /// <summary>
    /// Frame material (e.g., "Metal", "Plastic", "Titanium")
    /// </summary>
    public string? FrameMaterial { get; set; }

    /// <summary>
    /// Frame type (e.g., "Full-rim", "Semi-rimless", "Rimless")
    /// </summary>
    public string? FrameType { get; set; }

    // Lens-specific properties (nullable for Frame products)

    /// <summary>
    /// Lens type (e.g., "Single Vision", "Progressive", "Bifocal")
    /// </summary>
    public string? LensType { get; set; }

    /// <summary>
    /// Whether the lens is prescription
    /// </summary>
    public bool? IsPrescription { get; set; }

    /// <summary>
    /// Lens index value
    /// </summary>
    public decimal? LensIndex { get; set; }

    /// <summary>
    /// Product creation date for sorting
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
