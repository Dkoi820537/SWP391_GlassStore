using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace EyewearStore_SWP391.Models.ViewModels.Lens;

/// <summary>
/// View model for creating a new lens product.
/// Contains all required and optional fields with validation attributes.
/// </summary>
public class CreateLensViewModel
{
    /// <summary>
    /// The image file to upload (optional)
    /// Allowed types: .jpg, .jpeg, .png, .webp
    /// Maximum size: 5 MB
    /// </summary>
    [Display(Name = "Product Image")]
    public IFormFile? ImageFile { get; set; }

    /// <summary>
    /// Alternative text for the image (optional)
    /// </summary>
    [StringLength(255, ErrorMessage = "Image alt text cannot exceed 255 characters")]
    [Display(Name = "Image Alt Text")]
    public string? ImageAltText { get; set; }

    /// <summary>
    /// The SKU (Stock Keeping Unit) for the product
    /// </summary>
    [Required(ErrorMessage = "SKU is required")]
    [StringLength(50, ErrorMessage = "SKU cannot exceed 50 characters")]
    [Display(Name = "SKU")]
    public string Sku { get; set; } = null!;

    /// <summary>
    /// The name of the lens product
    /// </summary>
    [Required(ErrorMessage = "Name is required")]
    [StringLength(255, ErrorMessage = "Name cannot exceed 255 characters")]
    [Display(Name = "Product Name")]
    public string Name { get; set; } = null!;

    /// <summary>
    /// The description of the lens product
    /// </summary>
    [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
    [Display(Name = "Description")]
    public string? Description { get; set; }

    /// <summary>
    /// The price of the lens product
    /// </summary>
    [Required(ErrorMessage = "Price is required")]
    [Range(0.01, 999999999999.99, ErrorMessage = "Price must be greater than 0")]
    [DataType(DataType.Currency)]
    [Display(Name = "Price")]
    public decimal Price { get; set; }

    /// <summary>
    /// The currency code (e.g., VND, USD)
    /// </summary>
    [Required(ErrorMessage = "Currency is required")]
    [StringLength(3, MinimumLength = 3, ErrorMessage = "Currency must be exactly 3 characters")]
    [Display(Name = "Currency")]
    public string Currency { get; set; } = "VND";

    /// <summary>
    /// The inventory quantity (optional)
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Inventory quantity cannot be negative")]
    [Display(Name = "Inventory Quantity")]
    public int? InventoryQty { get; set; }

    /// <summary>
    /// JSON attributes for additional product data (optional)
    /// </summary>
    [Display(Name = "Additional Attributes (JSON)")]
    public string? Attributes { get; set; }

    /// <summary>
    /// Whether the product is active
    /// </summary>
    [Display(Name = "Is Active")]
    public bool IsActive { get; set; } = true;

    // Lens-specific properties

    /// <summary>
    /// The type of lens (e.g., Single Vision, Bifocal, Progressive)
    /// </summary>
    [StringLength(100, ErrorMessage = "Lens type cannot exceed 100 characters")]
    [Display(Name = "Lens Type")]
    public string? LensType { get; set; }

    /// <summary>
    /// The refractive index of the lens (e.g., 1.50, 1.67, 1.74)
    /// </summary>
    [Range(0.01, 10, ErrorMessage = "Lens index must be greater than 0 and not exceed 10")]
    [Display(Name = "Lens Index")]
    public decimal? LensIndex { get; set; }

    /// <summary>
    /// Whether this lens requires a prescription
    /// </summary>
    [Display(Name = "Requires Prescription")]
    public bool IsPrescription { get; set; }
}
