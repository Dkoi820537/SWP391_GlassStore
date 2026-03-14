using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace EyewearStore_SWP391.Models.ViewModels.Lens;

public class EditLensViewModel
{
    [Required]
    public int ProductId { get; set; }

    // Image
    [Display(Name = "Product Image")]
    public IFormFile? ImageFile { get; set; }
    [StringLength(255)]
    public string? ImageAltText { get; set; }
    public string? ExistingImageUrl { get; set; }

    // Basic
    [Required(ErrorMessage = "SKU is required")]
    [StringLength(50)]
    public string Sku { get; set; } = null!;

    [Required(ErrorMessage = "Name is required")]
    [StringLength(255)]
    [Display(Name = "Product Name")]
    public string Name { get; set; } = null!;

    [StringLength(2000)]
    public string? Description { get; set; }

    // Brand & identity
    [StringLength(100)]
    public string? Brand { get; set; }

    [StringLength(100)]
    public string? Origin { get; set; }

    // Lens specs
    [StringLength(100)]
    [Display(Name = "Lens Type")]
    public string? LensType { get; set; }

    [Range(0.01, 10)]
    [Display(Name = "Lens Index")]
    public decimal? LensIndex { get; set; }

    [StringLength(100)]
    [Display(Name = "Lens Material")]
    public string? LensMaterial { get; set; }

    [StringLength(100)]
    [Display(Name = "Thickness")]
    public string? LensThickness { get; set; }

    // Coatings & UV
    [StringLength(500)]
    [Display(Name = "Coatings")]
    public string? LensCoating { get; set; }

    [StringLength(50)]
    [Display(Name = "UV Protection")]
    public string? UVProtection { get; set; }

    // Prescription
    [Display(Name = "Requires Prescription")]
    public bool IsPrescription { get; set; }

    // Pricing & inventory
    [Required(ErrorMessage = "Price is required")]
    [Range(0.01, 999999999999.99)]
    [DataType(DataType.Currency)]
    public decimal Price { get; set; }

    [Required]
    [StringLength(3, MinimumLength = 3)]
    public string Currency { get; set; } = "VND";

    [Range(0, int.MaxValue)]
    [Display(Name = "Inventory Quantity")]
    public int? InventoryQty { get; set; }

    public string? Attributes { get; set; }
    public bool IsActive { get; set; } = true;
}