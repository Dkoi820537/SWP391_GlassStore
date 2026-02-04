namespace EyewearStore_SWP391.DTOs.Frame;

/// <summary>
/// Data Transfer Object for paginated frame list responses
/// </summary>
public class FrameListResponseDto
{
    /// <summary>
    /// The list of frame items for the current page
    /// </summary>
    public List<FrameResponseDto> Items { get; set; } = new();

    /// <summary>
    /// The current page number (1-indexed)
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// The number of items per page
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// The total number of items across all pages
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// The total number of pages
    /// </summary>
    public int TotalPages { get; set; }

    /// <summary>
    /// Indicates whether there is a previous page
    /// </summary>
    public bool HasPreviousPage => PageNumber > 1;

    /// <summary>
    /// Indicates whether there is a next page
    /// </summary>
    public bool HasNextPage => PageNumber < TotalPages;
}
