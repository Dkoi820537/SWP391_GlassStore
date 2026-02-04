using EyewearStore_SWP391.DTOs.Frame;

namespace EyewearStore_SWP391.Services;

/// <summary>
/// Interface for frame product operations
/// </summary>
public interface IFrameService
{
    /// <summary>
    /// Creates a new frame product
    /// </summary>
    /// <param name="createDto">The frame data to create</param>
    /// <returns>The created frame as a response DTO</returns>
    Task<FrameResponseDto> CreateFrameAsync(CreateFrameDto createDto);

    /// <summary>
    /// Gets all frames with optional filtering, searching, sorting, and pagination
    /// </summary>
    Task<FrameListResponseDto> GetAllFramesAsync(
        string? search = null,
        string? frameType = null,
        string? frameMaterial = null,
        decimal? priceMin = null,
        decimal? priceMax = null,
        bool? isActive = null,
        string sortBy = "createdAt",
        string sortOrder = "desc",
        int pageNumber = 1,
        int pageSize = 10);

    /// <summary>
    /// Gets a single frame by ID
    /// </summary>
    /// <param name="id">The product ID</param>
    /// <returns>The frame if found, null otherwise</returns>
    Task<FrameResponseDto?> GetFrameByIdAsync(int id);

    /// <summary>
    /// Updates an existing frame product
    /// </summary>
    /// <param name="id">The product ID to update</param>
    /// <param name="updateDto">The updated frame data</param>
    /// <returns>The updated frame if found, null otherwise</returns>
    Task<FrameResponseDto?> UpdateFrameAsync(int id, UpdateFrameDto updateDto);

    /// <summary>
    /// Soft deletes a frame by setting IsActive to false
    /// </summary>
    /// <param name="id">The product ID to delete</param>
    /// <returns>True if the frame was found and deleted, false otherwise</returns>
    Task<bool> DeleteFrameAsync(int id);
}
