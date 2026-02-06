using System;
using System.Collections.Generic;

namespace EyewearStore_SWP391.Models.ViewModels.Shop;

/// <summary>
/// View model for the product catalog page.
/// Contains the list of products, filter options, and pagination data.
/// </summary>
public class ProductCatalogViewModel
{
    /// <summary>
    /// List of products to display in the grid
    /// </summary>
    public List<ProductCatalogItemViewModel> Products { get; set; } = new();

    // Filter values

    /// <summary>
    /// Current product type filter: "All", "Frame", or "Lens"
    /// </summary>
    public string ProductTypeFilter { get; set; } = "All";

    /// <summary>
    /// Current search term for filtering by name, description, or SKU
    /// </summary>
    public string? SearchTerm { get; set; }

    /// <summary>
    /// Minimum price filter
    /// </summary>
    public decimal? MinPrice { get; set; }

    /// <summary>
    /// Maximum price filter
    /// </summary>
    public decimal? MaxPrice { get; set; }

    /// <summary>
    /// Frame material filter (for frames only)
    /// </summary>
    public string? FrameMaterialFilter { get; set; }

    /// <summary>
    /// Frame type filter (for frames only)
    /// </summary>
    public string? FrameTypeFilter { get; set; }

    /// <summary>
    /// Lens type filter (for lenses only)
    /// </summary>
    public string? LensTypeFilter { get; set; }

    /// <summary>
    /// Sort option: "name", "price-low", "price-high", "newest"
    /// </summary>
    public string SortBy { get; set; } = "name";

    // Pagination

    /// <summary>
    /// Current page number (1-indexed)
    /// </summary>
    public int CurrentPage { get; set; } = 1;

    /// <summary>
    /// Total number of pages
    /// </summary>
    public int TotalPages { get; set; }

    /// <summary>
    /// Number of items per page
    /// </summary>
    public int PageSize { get; set; } = 12;

    /// <summary>
    /// Total number of items matching the current filters
    /// </summary>
    public int TotalItems { get; set; }

    // Available filter options (for dropdowns)

    /// <summary>
    /// Available frame materials for filter dropdown
    /// </summary>
    public List<string> AvailableFrameMaterials { get; set; } = new();

    /// <summary>
    /// Available frame types for filter dropdown
    /// </summary>
    public List<string> AvailableFrameTypes { get; set; } = new();

    /// <summary>
    /// Available lens types for filter dropdown
    /// </summary>
    public List<string> AvailableLensTypes { get; set; } = new();

    // Helper properties

    /// <summary>
    /// Whether there is a previous page
    /// </summary>
    public bool HasPreviousPage => CurrentPage > 1;

    /// <summary>
    /// Whether there is a next page
    /// </summary>
    public bool HasNextPage => CurrentPage < TotalPages;

    /// <summary>
    /// Starting item number for current page (for display)
    /// </summary>
    public int StartItem => TotalItems == 0 ? 0 : ((CurrentPage - 1) * PageSize) + 1;

    /// <summary>
    /// Ending item number for current page (for display)
    /// </summary>
    public int EndItem => Math.Min(CurrentPage * PageSize, TotalItems);
}
