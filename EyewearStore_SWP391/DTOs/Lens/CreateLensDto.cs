using System.ComponentModel.DataAnnotations;

namespace EyewearStore_SWP391.DTOs.Lens;

/// <summary>
/// Data Transfer Object for creating a new lens product
/// </summary>
public class CreateLensDto
{
    /// <summary>
    /// The type of lens (e.g., Single Vision, Bifocal, Progressive)
    /// </summary>
    [Required(ErrorMessage = "Lens type is required")]
    [StringLength(50, ErrorMessage = "Lens type cannot exceed 50 characters")]
    public string LensType { get; set; } = null!;

    /// <summary>
    /// The refractive index value of the lens (e.g., 1.50, 1.60, 1.67, 1.74)
    /// </summary>
    [Range(1.0, 2.0, ErrorMessage = "Index value must be between 1.0 and 2.0")]
    public decimal? IndexValue { get; set; }

    /// <summary>
    /// The coating applied to the lens (e.g., Anti-Reflective, Blue Light Filter)
    /// </summary>
    [StringLength(100, ErrorMessage = "Coating cannot exceed 100 characters")]
    public string? Coating { get; set; }

    /// <summary>
    /// The price of the lens product
    /// </summary>
    [Required(ErrorMessage = "Price is required")]
    [Range(0.01, 999999999999, ErrorMessage = "Price must be greater than 0")]
    public decimal Price { get; set; }

    /// <summary>
    /// The stock status of the lens. Valid values: "in-stock", "low-stock", "out-of-stock"
    /// </summary>
    [StringLength(20, ErrorMessage = "Stock status cannot exceed 20 characters")]
    [RegularExpression("^(in-stock|low-stock|out-of-stock)$", ErrorMessage = "Invalid stock status. Valid values are: in-stock, low-stock, out-of-stock")]
    public string? StockStatus { get; set; }
}
