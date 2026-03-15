using System;
using System.Collections.Generic;

namespace EyewearStore_SWP391.Models.ViewModels.Shop;

public class LensDetailsViewModel
{
    // ── Core ──────────────────────────────────────────────────────────────────
    public int ProductId { get; set; }
    public string Sku { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = null!;
    public int? InventoryQty { get; set; }
    public DateTime CreatedAt { get; set; }

    // ── Brand & identity ──────────────────────────────────────────────────────
    public string? Brand { get; set; }
    public string? Origin { get; set; }

    // ── Lens specs ────────────────────────────────────────────────────────────
    public string? LensType { get; set; }
    public decimal? LensIndex { get; set; }
    public bool IsPrescription { get; set; }
    public decimal PrescriptionFee { get; set; }
    public string? LensMaterial { get; set; }
    public string? LensThickness { get; set; }

    // ── Coatings & protection ─────────────────────────────────────────────────
    public string? LensCoating { get; set; }
    public string? UVProtection { get; set; }

    // ── Sold count ────────────────────────────────────────────────────────────
    public int SoldCount { get; set; }

    // ── Images ────────────────────────────────────────────────────────────────
    public List<ProductImageViewModel> Images { get; set; } = new();

    public string? PrimaryImageUrl =>
        Images.FirstOrDefault(i => i.IsPrimary)?.ImageUrl
        ?? Images.FirstOrDefault()?.ImageUrl;

    // ── Related ───────────────────────────────────────────────────────────────
    public List<ProductCatalogItemViewModel> RelatedProducts { get; set; } = new();

    // ── Care instructions ─────────────────────────────────────────────────────
    public string? CareInstructions { get; set; }

    // ── Computed stock ────────────────────────────────────────────────────────
    public bool IsInStock => InventoryQty.HasValue && InventoryQty.Value > 0;

    public string StockStatus
    {
        get
        {
            if (!InventoryQty.HasValue || InventoryQty.Value <= 0) return "OutOfStock";
            if (InventoryQty.Value <= 10) return "LowStock";
            return "InStock";
        }
    }

    public string StockMessage => StockStatus switch
    {
        "OutOfStock" => "Out of Stock",
        "LowStock" => $"Only {InventoryQty} left",
        _ => $"In Stock ({InventoryQty} available)"
    };
}
