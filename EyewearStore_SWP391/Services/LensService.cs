using EyewearStore_SWP391.DTOs.Lens;
using EyewearStore_SWP391.Models;
using Microsoft.EntityFrameworkCore;

namespace EyewearStore_SWP391.Services;

/// <summary>
/// Service implementation for lens product operations
/// </summary>
public class LensService : ILensService
{
    private readonly EyewearStoreContext _context;

    /// <summary>
    /// Initializes a new instance of the LensService
    /// </summary>
    /// <param name="context">The database context</param>
    public LensService(EyewearStoreContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<LensResponseDto> CreateLensAsync(CreateLensDto createDto)
    {
        var lens = new Lense
        {
            LensType = createDto.LensType,
            IndexValue = createDto.IndexValue,
            Coating = createDto.Coating,
            Price = createDto.Price,
            StockStatus = createDto.StockStatus ?? "in-stock",
            CreatedAt = DateTime.UtcNow
        };

        await _context.Lenses.AddAsync(lens);
        await _context.SaveChangesAsync();

        return MapToResponseDto(lens);
    }

    /// <inheritdoc />
    public async Task<LensListResponseDto> GetAllLensesAsync(
        string? search = null,
        string? lensType = null,
        string? coating = null,
        decimal? priceMin = null,
        decimal? priceMax = null,
        string? stockStatus = null,
        string sortBy = "createdAt",
        string sortOrder = "desc",
        int pageNumber = 1,
        int pageSize = 10)
    {
        // Validate pagination parameters
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var query = _context.Lenses.AsQueryable();

        // Search filter (lens type or coating)
        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(l =>
                l.LensType.ToLower().Contains(searchLower) ||
                (l.Coating != null && l.Coating.ToLower().Contains(searchLower)));
        }

        // Filter by lens type
        if (!string.IsNullOrWhiteSpace(lensType))
        {
            query = query.Where(l => l.LensType.ToLower() == lensType.ToLower());
        }

        // Filter by coating
        if (!string.IsNullOrWhiteSpace(coating))
        {
            query = query.Where(l => l.Coating != null && l.Coating.ToLower() == coating.ToLower());
        }

        // Filter by price range
        if (priceMin.HasValue)
        {
            query = query.Where(l => l.Price >= priceMin.Value);
        }

        if (priceMax.HasValue)
        {
            query = query.Where(l => l.Price <= priceMax.Value);
        }

        // Filter by stock status
        if (!string.IsNullOrWhiteSpace(stockStatus))
        {
            query = query.Where(l => l.StockStatus != null && l.StockStatus.ToLower() == stockStatus.ToLower());
        }

        // Apply sorting
        query = ApplySorting(query, sortBy, sortOrder);

        // Get total count before pagination
        var totalCount = await query.CountAsync();

        // Calculate pagination values
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        // Apply pagination
        var lenses = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new LensListResponseDto
        {
            Items = lenses.Select(MapToResponseDto).ToList(),
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        };
    }

    /// <inheritdoc />
    public async Task<LensResponseDto?> GetLensByIdAsync(int id)
    {
        var lens = await _context.Lenses.FindAsync(id);

        if (lens == null)
        {
            return null;
        }

        return MapToResponseDto(lens);
    }

    /// <inheritdoc />
    public async Task<LensResponseDto?> UpdateLensAsync(int id, UpdateLensDto updateDto)
    {
        var lens = await _context.Lenses.FindAsync(id);

        if (lens == null)
        {
            return null;
        }

        // Update lens properties
        lens.LensType = updateDto.LensType;
        lens.IndexValue = updateDto.IndexValue;
        lens.Coating = updateDto.Coating;
        lens.Price = updateDto.Price;
        lens.StockStatus = updateDto.StockStatus;

        _context.Lenses.Update(lens);
        await _context.SaveChangesAsync();

        return MapToResponseDto(lens);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteLensAsync(int id)
    {
        var lens = await _context.Lenses.FindAsync(id);

        if (lens == null)
        {
            return false;
        }

        // Soft delete by setting stock status to "out-of-stock"
        // Valid values per DB constraint: 'in-stock', 'low-stock', 'out-of-stock'
        lens.StockStatus = "out-of-stock";

        _context.Lenses.Update(lens);
        await _context.SaveChangesAsync();

        return true;
    }

    #region Private Helper Methods

    /// <summary>
    /// Maps a Lense entity to a LensResponseDto
    /// </summary>
    private static LensResponseDto MapToResponseDto(Lense lens)
    {
        return new LensResponseDto
        {
            LensId = lens.LensId,
            LensType = lens.LensType,
            IndexValue = lens.IndexValue,
            Coating = lens.Coating,
            Price = lens.Price,
            StockStatus = lens.StockStatus,
            CreatedAt = lens.CreatedAt
        };
    }

    /// <summary>
    /// Applies sorting to the query based on the specified field and order
    /// </summary>
    private static IQueryable<Lense> ApplySorting(IQueryable<Lense> query, string sortBy, string sortOrder)
    {
        var isDescending = sortOrder.ToLower() == "desc";

        return sortBy.ToLower() switch
        {
            "price" => isDescending
                ? query.OrderByDescending(l => l.Price)
                : query.OrderBy(l => l.Price),
            "lenstype" => isDescending
                ? query.OrderByDescending(l => l.LensType)
                : query.OrderBy(l => l.LensType),
            "indexvalue" => isDescending
                ? query.OrderByDescending(l => l.IndexValue)
                : query.OrderBy(l => l.IndexValue),
            "createdat" or _ => isDescending
                ? query.OrderByDescending(l => l.CreatedAt)
                : query.OrderBy(l => l.CreatedAt)
        };
    }

    #endregion
}
