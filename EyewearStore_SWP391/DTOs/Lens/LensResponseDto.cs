namespace EyewearStore_SWP391.DTOs.Lens;

/// <summary>
/// Data Transfer Object for returning lens product data
/// </summary>
public class LensResponseDto
{
    /// <summary>
    /// The unique identifier of the lens
    /// </summary>
    public int LensId { get; set; }

    /// <summary>
    /// The type of lens (e.g., Single Vision, Bifocal, Progressive)
    /// </summary>
    public string LensType { get; set; } = null!;

    /// <summary>
    /// The refractive index value of the lens
    /// </summary>
    public decimal? IndexValue { get; set; }

    /// <summary>
    /// The coating applied to the lens
    /// </summary>
    public string? Coating { get; set; }

    /// <summary>
    /// The price of the lens product
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// The stock status of the lens
    /// </summary>
    public string? StockStatus { get; set; }

    /// <summary>
    /// The date and time when the lens was created
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
