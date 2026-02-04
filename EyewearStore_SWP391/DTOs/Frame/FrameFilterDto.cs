using System.ComponentModel.DataAnnotations;

namespace EyewearStore_SWP391.DTOs.Frame;

/// <summary>
/// Data Transfer Object for filtering and searching frames
/// </summary>
public class FrameFilterDto
{
    /// <summary>
    /// Search term to filter by name, description, or SKU
    /// </summary>
    public string? Search { get; set; }

    /// <summary>
    /// Minimum price filter
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "MinPrice must be non-negative")]
    public decimal? MinPrice { get; set; }

    /// <summary>
    /// Maximum price filter
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "MaxPrice must be non-negative")]
    public decimal? MaxPrice { get; set; }

    /// <summary>
    /// Filter by frame material (e.g., Titanium, Acetate, Metal)
    /// </summary>
    public string? FrameMaterial { get; set; }

    /// <summary>
    /// Filter by frame type (e.g., Full-Rim, Half-Rim, Rimless)
    /// </summary>
    public string? FrameType { get; set; }

    /// <summary>
    /// Minimum bridge width filter in millimeters
    /// </summary>
    [Range(0, 100, ErrorMessage = "MinBridgeWidth must be between 0 and 100mm")]
    public decimal? MinBridgeWidth { get; set; }

    /// <summary>
    /// Maximum bridge width filter in millimeters
    /// </summary>
    [Range(0, 100, ErrorMessage = "MaxBridgeWidth must be between 0 and 100mm")]
    public decimal? MaxBridgeWidth { get; set; }

    /// <summary>
    /// Minimum temple length filter in millimeters
    /// </summary>
    [Range(0, 200, ErrorMessage = "MinTempleLength must be between 0 and 200mm")]
    public decimal? MinTempleLength { get; set; }

    /// <summary>
    /// Maximum temple length filter in millimeters
    /// </summary>
    [Range(0, 200, ErrorMessage = "MaxTempleLength must be between 0 and 200mm")]
    public decimal? MaxTempleLength { get; set; }

    /// <summary>
    /// Filter by active status
    /// </summary>
    public bool? IsActive { get; set; }

    /// <summary>
    /// Page number for pagination (1-indexed)
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Page must be at least 1")]
    public int Page { get; set; } = 1;

    /// <summary>
    /// Number of items per page
    /// </summary>
    [Range(1, 100, ErrorMessage = "PageSize must be between 1 and 100")]
    public int PageSize { get; set; } = 10;

    /// <summary>
    /// Field to sort by (e.g., Name, Price, CreatedAt)
    /// </summary>
    public string? SortBy { get; set; }

    /// <summary>
    /// Sort order (asc or desc)
    /// </summary>
    public string? SortOrder { get; set; } = "asc";
}
