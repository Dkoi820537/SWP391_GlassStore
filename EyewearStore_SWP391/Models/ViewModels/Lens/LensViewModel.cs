namespace EyewearStore_SWP391.Models.ViewModels.Lens;

/// <summary>
/// View model for displaying lens product details.
/// Contains all Product and Lens-specific properties.
/// </summary>
public class LensViewModel
{
    // Product properties (inherited from base)

    /// <summary>
    /// The unique identifier of the product
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// The SKU (Stock Keeping Unit) for the product
    /// </summary>
    public string Sku { get; set; } = null!;

    /// <summary>
    /// The name of the lens product
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// The description of the lens product
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The product type discriminator
    /// </summary>
    public string ProductType { get; set; } = "Lens";

    /// <summary>
    /// The price of the lens product
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// The currency code (e.g., VND, USD)
    /// </summary>
    public string Currency { get; set; } = null!;

    /// <summary>
    /// The inventory quantity
    /// </summary>
    public int? InventoryQty { get; set; }

    /// <summary>
    /// JSON attributes for additional product data
    /// </summary>
    public string? Attributes { get; set; }

    /// <summary>
    /// Whether the product is active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// The date and time when the lens was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// The date and time when the lens was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    // Lens-specific properties

    /// <summary>
    /// The type of lens (e.g., Single Vision, Bifocal, Progressive)
    /// </summary>
    public string? LensType { get; set; }

    /// <summary>
    /// The refractive index of the lens (e.g., 1.50, 1.67, 1.74)
    /// </summary>
    public decimal? LensIndex { get; set; }

    /// <summary>
    /// Whether this lens requires a prescription
    /// </summary>
    public bool IsPrescription { get; set; }

    /// <summary>
    /// Primary image URL for the product
    /// </summary>
    public string? PrimaryImageUrl { get; set; }

    /// <summary>
    /// List of all product image URLs
    /// </summary>
    public List<string> ImageUrls { get; set; } = new();
}
