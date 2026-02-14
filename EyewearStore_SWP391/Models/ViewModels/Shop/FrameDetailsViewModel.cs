using System;
using System.Collections.Generic;

namespace EyewearStore_SWP391.Models.ViewModels.Shop;

/// <summary>
/// View model for displaying detailed frame information on the product details page.
/// </summary>
public class FrameDetailsViewModel
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

    // Frame-specific properties

    /// <summary>
    /// Frame material (e.g., "Metal", "Plastic", "Titanium")
    /// </summary>
    public string? FrameMaterial { get; set; }

    /// <summary>
    /// Frame type (e.g., "Full-rim", "Semi-rimless", "Rimless")
    /// </summary>
    public string? FrameType { get; set; }

    /// <summary>
    /// Bridge width in mm
    /// </summary>
    public decimal? BridgeWidth { get; set; }

    /// <summary>
    /// Temple length in mm
    /// </summary>
    public decimal? TempleLength { get; set; }

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
    /// Related frames (same material or type)
    /// </summary>
    public List<ProductCatalogItemViewModel> RelatedProducts { get; set; } = new();

    /// <summary>
    /// Care instructions for the frame
    /// </summary>
    public string? CareInstructions { get; set; }

    /// <summary>
    /// Product creation date
    /// </summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// View model for a product image.
/// </summary>
public class ProductImageViewModel
{
    /// <summary>
    /// Image identifier
    /// </summary>
    public int ImageId { get; set; }

    /// <summary>
    /// URL to the image
    /// </summary>
    public string ImageUrl { get; set; } = null!;

    /// <summary>
    /// Alternative text for accessibility
    /// </summary>
    public string? AltText { get; set; }

    /// <summary>
    /// Whether this is the primary/featured image
    /// </summary>
    public bool IsPrimary { get; set; }

    /// <summary>
    /// Display order for the image
    /// </summary>
    public int SortOrder { get; set; }
}

/// <summary>
/// Input model for adding a product to cart.
/// </summary>
public class AddToCartInputModel
{
    /// <summary>
    /// The product ID to add to cart
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Quantity to add (default: 1)
    /// </summary>
    public int Quantity { get; set; } = 1;

    /// <summary>
    /// Optional saved prescription profile ID (for lenses). Null = no prescription.
    /// </summary>
    public int? PrescriptionId { get; set; }
}
