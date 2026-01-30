using EyewearStore_SWP391.DTOs.Lens;

namespace EyewearStore_SWP391.Services;

/// <summary>
/// Interface for lens product operations
/// </summary>
public interface ILensService
{
    /// <summary>
    /// Creates a new lens product
    /// </summary>
    /// <param name="createDto">The lens data to create</param>
    /// <returns>The created lens as a response DTO</returns>
    Task<LensResponseDto> CreateLensAsync(CreateLensDto createDto);

    /// <summary>
    /// Gets all lenses with optional filtering, searching, sorting, and pagination
    /// </summary>
    Task<LensListResponseDto> GetAllLensesAsync(
        string? search = null,
        string? lensType = null,
        string? coating = null,
        decimal? priceMin = null,
        decimal? priceMax = null,
        string? stockStatus = null,
        string sortBy = "createdAt",
        string sortOrder = "desc",
        int pageNumber = 1,
        int pageSize = 10);

    /// <summary>
    /// Gets a single lens by ID
    /// </summary>
    /// <param name="id">The lens ID</param>
    /// <returns>The lens if found, null otherwise</returns>
    Task<LensResponseDto?> GetLensByIdAsync(int id);

    /// <summary>
    /// Updates an existing lens product
    /// </summary>
    /// <param name="id">The lens ID to update</param>
    /// <param name="updateDto">The updated lens data</param>
    /// <returns>The updated lens if found, null otherwise</returns>
    Task<LensResponseDto?> UpdateLensAsync(int id, UpdateLensDto updateDto);

    /// <summary>
    /// Soft deletes a lens by setting StockStatus to "out-of-stock"
    /// </summary>
    /// <param name="id">The lens ID to delete</param>
    /// <returns>True if the lens was found and deleted, false otherwise</returns>
    Task<bool> DeleteLensAsync(int id);
}
