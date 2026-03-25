using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace EyewearStore_SWP391.Models.ViewModels.Frame;

public class EditFrameViewModel
{
    public int ProductId { get; set; }

    // ── Image ────────────────────────────────────────────────────────────────
    [Display(Name = "New Image")]
    public IFormFile? ImageFile { get; set; }

    [StringLength(255)]
    [Display(Name = "Image Alt Text")]
    public string? ImageAltText { get; set; }

    public string? ExistingImageUrl { get; set; }

    // ── Basic Info ───────────────────────────────────────────────────────────
    [Required(ErrorMessage = "SKU is required")]
    [StringLength(50)]
    [Display(Name = "SKU")]
    public string Sku { get; set; } = null!;

    [Required(ErrorMessage = "Name is required")]
    [StringLength(255)]
    [Display(Name = "Product Name")]
    public string Name { get; set; } = null!;

    [StringLength(2000)]
    [Display(Name = "Description")]
    public string? Description { get; set; }

    // ── Pricing & Inventory ──────────────────────────────────────────────────
    [Required]
    [Range(0.01, 999999999999.99)]
    [DataType(DataType.Currency)]
    [Display(Name = "Price")]
    public decimal Price { get; set; }

    [Required]
    [StringLength(3, MinimumLength = 3)]
    [Display(Name = "Currency")]
    public string Currency { get; set; } = "VND";

    [Range(0, int.MaxValue)]
    [Display(Name = "Inventory Quantity")]
    public int? InventoryQty { get; set; }

    [Display(Name = "Additional Attributes (JSON)")]
    public string? Attributes { get; set; }

    [Display(Name = "Is Active")]
    public bool IsActive { get; set; } = true;

    // ── Brand & Identity ─────────────────────────────────────────────────────
    [StringLength(100)]
    [Display(Name = "Brand")]
    public string? Brand { get; set; }

    [StringLength(20)]
    [Display(Name = "Gender")]
    public string? Gender { get; set; }

    [StringLength(100)]
    [Display(Name = "Country of Origin")]
    public string? Origin { get; set; }

    [StringLength(200)]
    [Display(Name = "Style Tags (comma-separated)")]
    public string? StyleTags { get; set; }

    // ── Frame Specs ──────────────────────────────────────────────────────────
    [StringLength(100)]
    [Display(Name = "Frame Material")]
    public string? FrameMaterial { get; set; }

    [StringLength(50)]
    [Display(Name = "Frame Type")]
    public string? FrameType { get; set; }

    [StringLength(50)]
    [Display(Name = "Frame Shape")]
    public string? FrameShape { get; set; }

    [StringLength(200)]
    [Display(Name = "Frame Color (Màu gọng)")]
    public string? FrameColor { get; set; }

    [StringLength(200)]
    [Display(Name = "Color Swatches (comma-separated)")]
    public string? Color { get; set; }

    [Range(0.01, 100)]
    [Display(Name = "Lens Width (mm)")]
    public decimal? LensWidth { get; set; }

    [Range(0.01, 100)]
    [Display(Name = "Bridge Width (mm)")]
    public decimal? BridgeWidth { get; set; }

    [Range(0.01, 200)]
    [Display(Name = "Temple Length (mm)")]
    public decimal? TempleLength { get; set; }

    // ── Lens Details ─────────────────────────────────────────────────────────
    [StringLength(100)]
    [Display(Name = "Lens Material")]
    public string? LensMaterial { get; set; }

    [StringLength(100)]
    [Display(Name = "Lens Color")]
    public string? LensColor { get; set; }

    [Display(Name = "Polarized")]
    public bool? IsPolarized { get; set; }

    [Display(Name = "UV Protection")]
    public bool? HasUvProtection { get; set; }

    // ── Face Shape Compatibility ─────────────────────────────────────────────
    [StringLength(500)]
    [Display(Name = "Suitable Face Shapes (comma-separated)")]
    public string? SuitableFaceShapes { get; set; }
}