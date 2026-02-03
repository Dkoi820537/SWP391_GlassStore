using System.ComponentModel.DataAnnotations;

namespace EyewearStore_SWP391.DTOs.Lens;

/// <summary>
/// Data Transfer Object for updating an existing lens product
/// </summary>
public class UpdateLensDto
{
    /// <summary>
    /// The SKU (Stock Keeping Unit) for the product
    /// </summary>
    [Required(ErrorMessage = "SKU is required")]
    [StringLength(50, ErrorMessage = "SKU cannot exceed 50 characters")]
    public string Sku { get; set; } = null!;

    /// <summary>
    /// The name of the lens product
    /// </summary>
    [Required(ErrorMessage = "Name is required")]
    [StringLength(255, ErrorMessage = "Name cannot exceed 255 characters")]
    public string Name { get; set; } = null!;

    /// <summary>
    /// The description of the lens product
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The price of the lens product
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

    // Lens-specific properties

    /// <summary>
    /// The type of lens (e.g., Single Vision, Bifocal, Progressive)
    /// </summary>
    [StringLength(50, ErrorMessage = "Lens type cannot exceed 50 characters")]
    public string? LensType { get; set; }

    /// <summary>
    /// The refractive index value of the lens (e.g., 1.50, 1.60, 1.67, 1.74)
    /// </summary>
    [Range(1.0, 2.0, ErrorMessage = "Lens index must be between 1.0 and 2.0")]
    public decimal? LensIndex { get; set; }

    /// <summary>
    /// Whether this lens requires a prescription
    /// </summary>
    public bool IsPrescription { get; set; }
}
