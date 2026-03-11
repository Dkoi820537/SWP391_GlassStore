using System;
using System.Collections.Generic;

namespace EyewearStore_SWP391.Models.ViewModels.Shop;

public class FrameDetailsViewModel
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

    // ── Frame specs ───────────────────────────────────────────────────────────
    public string? FrameMaterial { get; set; }
    public string? FrameType { get; set; }
    public decimal? BridgeWidth { get; set; }
    public decimal? TempleLength { get; set; }
    public string? CareInstructions { get; set; }

    // ── v2 fields ─────────────────────────────────────────────────────────────
    public string? Brand { get; set; }
    public string? Color { get; set; }
    public string? Gender { get; set; }
    public string? FrameShape { get; set; }

    // ── v3 fields ─────────────────────────────────────────────────────────────
    public decimal? LensWidth { get; set; }
    public string? Origin { get; set; }

    /// <summary>Số lượng đã bán — tính từ order_items</summary>
    public int SoldCount { get; set; }

    // ── Images ────────────────────────────────────────────────────────────────
    public List<ProductImageViewModel> Images { get; set; } = new();

    public string? PrimaryImageUrl =>
        Images.FirstOrDefault(i => i.IsPrimary)?.ImageUrl
        ?? Images.FirstOrDefault()?.ImageUrl;

    // ── Related ───────────────────────────────────────────────────────────────
    public List<ProductCatalogItemViewModel> RelatedProducts { get; set; } = new();

    // ── Computed ──────────────────────────────────────────────────────────────
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

    public string StockMessage
    {
        get
        {
            return StockStatus switch
            {
                "OutOfStock" => "Hết hàng",
                "LowStock" => $"Còn ít ({InventoryQty} sản phẩm)",
                _ => $"Còn hàng ({InventoryQty} sản phẩm)"
            };
        }
    }

    /// <summary>Formatted size string: LensWidth-Bridge-Temple</summary>
    public string SizeLabel
    {
        get
        {
            var parts = new[]
            {
                LensWidth.HasValue    ? LensWidth.Value.ToString("0")    : "—",
                BridgeWidth.HasValue  ? BridgeWidth.Value.ToString("0")  : null,
                TempleLength.HasValue ? TempleLength.Value.ToString("0") : null,
            };
            return string.Join(" - ", parts.Where(p => p != null));
        }
    }
}

public class ProductImageViewModel
{
    public int ImageId { get; set; }
    public string ImageUrl { get; set; } = null!;
    public string? AltText { get; set; }
    public bool IsPrimary { get; set; }
    public int SortOrder { get; set; }
}

public class AddToCartInputModel
{
    public int ProductId { get; set; }
    public int Quantity { get; set; } = 1;
    public int? PrescriptionId { get; set; }
}