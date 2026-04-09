using System.ComponentModel.DataAnnotations;

namespace EyewearStore_SWP391.DTOs.Frame;

public class CreateFrameDto
{
    [Required(ErrorMessage = "SKU is required")]
    [StringLength(50)]
    public string Sku { get; set; } = null!;

    [Required(ErrorMessage = "Name is required")]
    [StringLength(255)]
    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    [Required]
    [Range(0.01, 999999999999.99)]
    public decimal Price { get; set; }

    [Required]
    [StringLength(3, MinimumLength = 3)]
    public string Currency { get; set; } = "VND";

    [Range(0, int.MaxValue)]
    public int? QuantityOnHand { get; set; }

    public bool IsActive { get; set; } = true;

    // ── Frame specs ──────────────────────────────────────────────────────────
    [StringLength(100)]
    public string? FrameMaterial { get; set; }

    [StringLength(50)]
    public string? FrameType { get; set; }

    [Range(0, 100)]
    public decimal? BridgeWidth { get; set; }

    [Range(0, 200)]
    public decimal? TempleLength { get; set; }

    // ── v2 ──────────────────────────────────────────────────────────────────
    [StringLength(100)]
    public string? Brand { get; set; }

    [StringLength(200)]
    public string? Color { get; set; }

    [StringLength(20)]
    public string? Gender { get; set; }

    [StringLength(50)]
    public string? FrameShape { get; set; }

    // ── v3 ──────────────────────────────────────────────────────────────────
    [Range(0, 100)]
    public decimal? LensWidth { get; set; }

    [StringLength(100)]
    public string? Origin { get; set; }

    // ── v4 ──────────────────────────────────────────────────────────────────
    [StringLength(200)]
    public string? FrameColor { get; set; }

    [StringLength(100)]
    public string? LensMaterial { get; set; }

    [StringLength(100)]
    public string? LensColor { get; set; }

    [StringLength(500)]
    public string? SuitableFaceShapes { get; set; }

    public bool? IsPolarized { get; set; }

    public bool? HasUvProtection { get; set; }

    [StringLength(200)]
    public string? StyleTags { get; set; }
}