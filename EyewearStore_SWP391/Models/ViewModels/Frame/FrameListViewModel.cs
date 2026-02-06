using System.ComponentModel.DataAnnotations;

namespace EyewearStore_SWP391.Models.ViewModels.Frame;

/// <summary>
/// View model for the frame index/list page.
/// Contains list of frame items, search/filter properties, pagination, and sorting.
/// </summary>
public class FrameListViewModel
{
    /// <summary>
    /// The list of frame items for the current page
    /// </summary>
    public List<FrameItemViewModel> Items { get; set; } = new();

    // Search and filter properties

    /// <summary>
    /// Search term to filter by name, description, or SKU
    /// </summary>
    [Display(Name = "Search")]
    public string? SearchTerm { get; set; }

    /// <summary>
    /// Filter by frame material (e.g., Titanium, Acetate, Metal)
    /// </summary>
    [Display(Name = "Material")]
    public string? FilterMaterial { get; set; }

    /// <summary>
    /// Filter by frame type (e.g., Full-Rim, Half-Rim, Rimless)
    /// </summary>
    [Display(Name = "Type")]
    public string? FilterType { get; set; }

    /// <summary>
    /// Minimum price filter
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "Minimum price must be non-negative")]
    [Display(Name = "Min Price")]
    public decimal? MinPrice { get; set; }

    /// <summary>
    /// Maximum price filter
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "Maximum price must be non-negative")]
    [Display(Name = "Max Price")]
    public decimal? MaxPrice { get; set; }

    /// <summary>
    /// Filter by active status
    /// </summary>
    [Display(Name = "Active Status")]
    public bool? IsActive { get; set; }

    // Pagination properties

    /// <summary>
    /// The current page number (1-indexed)
    /// </summary>
    public int CurrentPage { get; set; } = 1;

    /// <summary>
    /// The total number of pages
    /// </summary>
    public int TotalPages { get; set; }

    /// <summary>
    /// The number of items per page
    /// </summary>
    public int PageSize { get; set; } = 10;

    /// <summary>
    /// The total number of items across all pages
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Indicates whether there is a previous page
    /// </summary>
    public bool HasPreviousPage => CurrentPage > 1;

    /// <summary>
    /// Indicates whether there is a next page
    /// </summary>
    public bool HasNextPage => CurrentPage < TotalPages;

    // Sorting properties

    /// <summary>
    /// Field to sort by (e.g., Name, Price, CreatedAt, FrameMaterial, FrameType)
    /// </summary>
    [Display(Name = "Sort By")]
    public string? SortBy { get; set; }

    /// <summary>
    /// Sort order (asc or desc)
    /// </summary>
    [Display(Name = "Sort Order")]
    public string SortOrder { get; set; } = "asc";

    // Available filter options (for dropdowns)

    /// <summary>
    /// Available frame materials for filter dropdown
    /// </summary>
    public List<string> AvailableMaterials { get; set; } = new();

    /// <summary>
    /// Available frame types for filter dropdown
    /// </summary>
    public List<string> AvailableTypes { get; set; } = new();

    /// <summary>
    /// Available sort options
    /// </summary>
    public List<string> AvailableSortOptions { get; set; } = new()
    {
        "Name",
        "Price",
        "CreatedAt",
        "FrameMaterial",
        "FrameType",
        "InventoryQty"
    };
}
