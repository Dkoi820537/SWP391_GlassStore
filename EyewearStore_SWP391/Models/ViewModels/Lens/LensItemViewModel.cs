namespace EyewearStore_SWP391.Models.ViewModels.Lens;

public class LensItemViewModel
{
    public int ProductId { get; set; }
    public string Sku { get; set; } = null!;
    public string Name { get; set; } = null!;
    public decimal Price { get; set; }
    public string Currency { get; set; } = null!;
    public int? QuantityOnHand { get; set; }
    public string? Brand { get; set; }
    public string? Origin { get; set; }
    public string? LensType { get; set; }
    public decimal? LensIndex { get; set; }
    public string? LensMaterial { get; set; }
    public string? LensThickness { get; set; }
    public string? LensCoating { get; set; }
    public string? UVProtection { get; set; }
    public bool IsPrescription { get; set; }
    public bool IsActive { get; set; }
    public string? PrimaryImageUrl { get; set; }
}