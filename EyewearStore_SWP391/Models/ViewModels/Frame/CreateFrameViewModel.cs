using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace EyewearStore_SWP391.Models.ViewModels.Frame;

/// <summary>
/// View model for creating a new frame product.
/// Contains all required and optional fields with validation attributes.
/// </summary>
public class CreateFrameViewModel
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
    /// The name of the frame product
    /// </summary>
    [Required(ErrorMessage = "Name is required")]
    [StringLength(255, ErrorMessage = "Name cannot exceed 255 characters")]
    [Display(Name = "Product Name")]
    public string Name { get; set; } = null!;

    /// <summary>
    /// The description of the frame product
    /// </summary>
    [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
    [Display(Name = "Description")]
    public string? Description { get; set; }

    /// <summary>
    /// The price of the frame product
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

    // Frame-specific properties

    /// <summary>
    /// The material of the frame (e.g., Titanium, Acetate, Metal)
    /// </summary>
    [StringLength(100, ErrorMessage = "Frame material cannot exceed 100 characters")]
    [Display(Name = "Frame Material")]
    public string? FrameMaterial { get; set; }

    /// <summary>
    /// The type of frame (e.g., Full-Rim, Half-Rim, Rimless)
    /// </summary>
    [StringLength(50, ErrorMessage = "Frame type cannot exceed 50 characters")]
    [Display(Name = "Frame Type")]
    public string? FrameType { get; set; }

    /// <summary>
    /// The bridge width in millimeters
    /// </summary>
    [Range(0.01, 100, ErrorMessage = "Bridge width must be greater than 0 and not exceed 100mm")]
    [Display(Name = "Bridge Width (mm)")]
    public decimal? BridgeWidth { get; set; }

    /// <summary>
    /// The temple length in millimeters
    /// </summary>
    [Range(0.01, 200, ErrorMessage = "Temple length must be greater than 0 and not exceed 200mm")]
    [Display(Name = "Temple Length (mm)")]
    public decimal? TempleLength { get; set; }
}
