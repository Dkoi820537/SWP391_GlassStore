using System;
using System.Collections.Generic;

namespace EyewearStore_SWP391.Models.ViewModels.Shop;

/// <summary>
/// View model for displaying detailed lens information on the product details page.
/// Mirrors the FrameDetailsViewModel architecture with lens-specific properties.
/// </summary>
public class LensDetailsViewModel
{
    /// <summary>
    /// The unique product identifier
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Product SKU code
    /// </summary>
    public string Sku { get; set; } = null!;

    /// <summary>
    /// Product display name
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Full product description
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

    // Lens-specific properties

    /// <summary>
    /// Lens type (e.g., "Single Vision", "Bifocal", "Progressive")
    /// </summary>
    public string? LensType { get; set; }

    /// <summary>
    /// Refractive index of the lens (e.g., 1.50, 1.67, 1.74)
    /// </summary>
    public decimal? LensIndex { get; set; }

    /// <summary>
    /// Whether this lens requires a prescription
    /// </summary>
    public bool IsPrescription { get; set; }

    // Inventory and stock properties

    /// <summary>
    /// Current inventory quantity
    /// </summary>
    public int? InventoryQty { get; set; }

    /// <summary>
    /// Whether the product is currently in stock
    /// </summary>
    public bool IsInStock => InventoryQty.HasValue && InventoryQty.Value > 0;

    /// <summary>
    /// Stock status: "InStock", "LowStock", "OutOfStock"
    /// </summary>
    public string StockStatus
    {
        get
        {
            if (!InventoryQty.HasValue || InventoryQty.Value <= 0)
                return "OutOfStock";
            if (InventoryQty.Value <= 10)
                return "LowStock";
            return "InStock";
        }
    }

    /// <summary>
    /// Human-readable stock message
    /// </summary>
    public string StockMessage
    {
        get
        {
            return StockStatus switch
            {
                "OutOfStock" => "Out of Stock",
                "LowStock" => $"Only {InventoryQty} left",
                _ => "In Stock"
            };
        }
    }

    // Image properties

    /// <summary>
    /// List of product images
    /// </summary>
    public List<ProductImageViewModel> Images { get; set; } = new();

    /// <summary>
    /// URL to the primary product image
    /// </summary>
    public string? PrimaryImageUrl => Images.FirstOrDefault(i => i.IsPrimary)?.ImageUrl
        ?? Images.FirstOrDefault()?.ImageUrl;

    // Related products

    /// <summary>
    /// Related lenses (same type or prescription)
    /// </summary>
    public List<ProductCatalogItemViewModel> RelatedProducts { get; set; } = new();

    /// <summary>
    /// Care instructions for the lens
    /// </summary>
    public string? CareInstructions { get; set; }

    /// <summary>
    /// Product creation date
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
