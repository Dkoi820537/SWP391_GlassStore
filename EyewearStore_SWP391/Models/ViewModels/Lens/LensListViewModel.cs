using System.ComponentModel.DataAnnotations;

namespace EyewearStore_SWP391.Models.ViewModels.Lens;

/// <summary>
/// View model for the lens index/list page.
/// Contains list of lens items, search/filter properties, pagination, and sorting.
/// </summary>
public class LensListViewModel
{
    /// <summary>
    /// The list of lens items for the current page
    /// </summary>
    public List<LensItemViewModel> Items { get; set; } = new();

    // Search and filter properties

    /// <summary>
    /// Search term to filter by name, description, or SKU
    /// </summary>
    [Display(Name = "Search")]
    public string? SearchTerm { get; set; }

    /// <summary>
    /// Filter by lens type (e.g., Single Vision, Bifocal, Progressive)
    /// </summary>
    [Display(Name = "Lens Type")]
    public string? FilterLensType { get; set; }

    /// <summary>
    /// Filter by prescription requirement
    /// </summary>
    [Display(Name = "Prescription")]
    public bool? FilterIsPrescription { get; set; }

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
    /// Field to sort by (e.g., Name, Price, CreatedAt, LensType)
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
    /// Available lens types for filter dropdown
    /// </summary>
    public List<string> AvailableLensTypes { get; set; } = new();

    /// <summary>
    /// Available sort options
    /// </summary>
    public List<string> AvailableSortOptions { get; set; } = new()
    {
        "Name",
        "Price",
        "CreatedAt",
        "LensType",
        "InventoryQty"
    };
}
