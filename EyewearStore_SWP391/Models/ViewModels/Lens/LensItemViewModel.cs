namespace EyewearStore_SWP391.Models.ViewModels.Lens;

/// <summary>
/// Lightweight view model for lens list items.
/// Contains only essential fields for displaying in lists/tables.
/// </summary>
public class LensItemViewModel
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
    /// The name of the lens product
    /// </summary>
    public string Name { get; set; } = null!;

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
    /// The type of lens (e.g., Single Vision, Bifocal, Progressive)
    /// </summary>
    public string? LensType { get; set; }

    /// <summary>
    /// Whether this lens requires a prescription
    /// </summary>
    public bool IsPrescription { get; set; }

    /// <summary>
    /// Whether the product is active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Primary image URL for the product thumbnail
    /// </summary>
    public string? PrimaryImageUrl { get; set; }
}
