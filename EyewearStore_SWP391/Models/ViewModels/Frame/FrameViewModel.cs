using System;
using System.Collections.Generic;

namespace EyewearStore_SWP391.Models.ViewModels.Frame;

public class FrameViewModel
{
    // ── Product base ─────────────────────────────────────────────────────────
    public int ProductId { get; set; }
    public string Sku { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string ProductType { get; set; } = "Frame";
    public decimal Price { get; set; }
    public string Currency { get; set; } = null!;
    public int? QuantityOnHand { get; set; }
    public string? Attributes { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // ── Frame specs ──────────────────────────────────────────────────────────
    public string? FrameMaterial { get; set; }
    public string? FrameType { get; set; }
    public decimal? BridgeWidth { get; set; }
    public decimal? TempleLength { get; set; }

    // ── v2 ──────────────────────────────────────────────────────────────────
    public string? Brand { get; set; }
    public string? Color { get; set; }
    public string? Gender { get; set; }
    public string? FrameShape { get; set; }

    // ── v3 ──────────────────────────────────────────────────────────────────
    public decimal? LensWidth { get; set; }
    public string? Origin { get; set; }

    // ── v4 ──────────────────────────────────────────────────────────────────
    public string? FrameColor { get; set; }
    public string? LensMaterial { get; set; }
    public string? LensColor { get; set; }
    public string? SuitableFaceShapes { get; set; }
    public bool? IsPolarized { get; set; }
    public bool? HasUvProtection { get; set; }
    public string? StyleTags { get; set; }

    // ── Images ───────────────────────────────────────────────────────────────
    public string? PrimaryImageUrl { get; set; }
    public List<string> ImageUrls { get; set; } = new();
}