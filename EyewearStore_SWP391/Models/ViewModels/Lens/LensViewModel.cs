using System;
using System.Collections.Generic;

namespace EyewearStore_SWP391.Models.ViewModels.Lens;

public class LensViewModel
{
    public int ProductId { get; set; }
    public string Sku { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string ProductType { get; set; } = "Lens";
    public decimal Price { get; set; }
    public string Currency { get; set; } = null!;
    public int? InventoryQty { get; set; }
    public string? Attributes { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Brand & identity
    public string? Brand { get; set; }
    public string? Origin { get; set; }

    // Lens specs
    public string? LensType { get; set; }
    public decimal? LensIndex { get; set; }
    public bool IsPrescription { get; set; }
    public string? LensMaterial { get; set; }
    public string? LensThickness { get; set; }

    // Coatings & UV
    public string? LensCoating { get; set; }
    public string? UVProtection { get; set; }

    // Images
    public string? PrimaryImageUrl { get; set; }
    public List<string> ImageUrls { get; set; } = new();
}