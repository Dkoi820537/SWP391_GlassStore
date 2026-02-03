using EyewearStore_SWP391.DTOs.Lens;
using EyewearStore_SWP391.Models;
using Microsoft.EntityFrameworkCore;

namespace EyewearStore_SWP391.Services;

/// <summary>
/// Service implementation for lens product operations using TPT inheritance
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
        var lens = new Lens
        {
            // Base Product properties
            Sku = createDto.Sku,
            Name = createDto.Name,
            Description = createDto.Description,
            ProductType = "Lens",
            Price = createDto.Price,
            Currency = createDto.Currency,
            InventoryQty = createDto.InventoryQty,
            IsActive = createDto.IsActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,

            // Lens-specific properties
            LensType = createDto.LensType,
            LensIndex = createDto.LensIndex,
            IsPrescription = createDto.IsPrescription
        };

        await _context.Lenses.AddAsync(lens);
        await _context.SaveChangesAsync();

        return await MapToResponseDtoAsync(lens);
    }

    /// <inheritdoc />
    public async Task<LensListResponseDto> GetAllLensesAsync(
        string? search = null,
        string? lensType = null,
        decimal? priceMin = null,
        decimal? priceMax = null,
        bool? isActive = null,
        bool? isPrescription = null,
        string sortBy = "createdAt",
        string sortOrder = "desc",
        int pageNumber = 1,
        int pageSize = 10)
    {
        // Validate pagination parameters
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        // Use OfType<Lens>() to query TPT inheritance
        var query = _context.Products.OfType<Lens>().AsQueryable();

        // Search filter (name or description)
        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(l =>
                l.Name.ToLower().Contains(searchLower) ||
                (l.Description != null && l.Description.ToLower().Contains(searchLower)) ||
                (l.LensType != null && l.LensType.ToLower().Contains(searchLower)));
        }

        // Filter by lens type
        if (!string.IsNullOrWhiteSpace(lensType))
        {
            query = query.Where(l => l.LensType != null && l.LensType.ToLower() == lensType.ToLower());
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

        // Filter by is_active
        if (isActive.HasValue)
        {
            query = query.Where(l => l.IsActive == isActive.Value);
        }

        // Filter by is_prescription
        if (isPrescription.HasValue)
        {
            query = query.Where(l => l.IsPrescription == isPrescription.Value);
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

        // Map to response DTOs (with primary image)
        var items = new List<LensResponseDto>();
        foreach (var lens in lenses)
        {
            items.Add(await MapToResponseDtoAsync(lens));
        }

        return new LensListResponseDto
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        };
    }

    /// <inheritdoc />
    public async Task<LensResponseDto?> GetLensByIdAsync(int id)
    {
        var lens = await _context.Products
            .OfType<Lens>()
            .FirstOrDefaultAsync(l => l.ProductId == id);

        if (lens == null)
        {
            return null;
        }

        return await MapToResponseDtoAsync(lens);
    }

    /// <inheritdoc />
    public async Task<LensResponseDto?> UpdateLensAsync(int id, UpdateLensDto updateDto)
    {
        var lens = await _context.Products
            .OfType<Lens>()
            .FirstOrDefaultAsync(l => l.ProductId == id);

        if (lens == null)
        {
            return null;
        }

        // Update base Product properties
        lens.Sku = updateDto.Sku;
        lens.Name = updateDto.Name;
        lens.Description = updateDto.Description;
        lens.Price = updateDto.Price;
        lens.Currency = updateDto.Currency;
        lens.InventoryQty = updateDto.InventoryQty;
        lens.IsActive = updateDto.IsActive;
        lens.UpdatedAt = DateTime.UtcNow;

        // Update Lens-specific properties
        lens.LensType = updateDto.LensType;
        lens.LensIndex = updateDto.LensIndex;
        lens.IsPrescription = updateDto.IsPrescription;

        _context.Products.Update(lens);
        await _context.SaveChangesAsync();

        return await MapToResponseDtoAsync(lens);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteLensAsync(int id)
    {
        var lens = await _context.Products
            .OfType<Lens>()
            .FirstOrDefaultAsync(l => l.ProductId == id);

        if (lens == null)
        {
            return false;
        }

        // Soft delete by setting IsActive to false
        lens.IsActive = false;
        lens.UpdatedAt = DateTime.UtcNow;

        _context.Products.Update(lens);
        await _context.SaveChangesAsync();

        return true;
    }

    #region Private Helper Methods

    /// <summary>
    /// Maps a Lens entity to a LensResponseDto
    /// </summary>
    private async Task<LensResponseDto> MapToResponseDtoAsync(Lens lens)
    {
        // Get primary image URL
        var primaryImage = await _context.ProductImages
            .Where(pi => pi.ProductId == lens.ProductId && pi.IsPrimary && pi.IsActive)
            .Select(pi => pi.ImageUrl)
            .FirstOrDefaultAsync();

        return new LensResponseDto
        {
            ProductId = lens.ProductId,
            Sku = lens.Sku,
            Name = lens.Name,
            Description = lens.Description,
            Price = lens.Price,
            Currency = lens.Currency,
            InventoryQty = lens.InventoryQty,
            IsActive = lens.IsActive,
            CreatedAt = lens.CreatedAt,
            UpdatedAt = lens.UpdatedAt,
            LensType = lens.LensType,
            LensIndex = lens.LensIndex,
            IsPrescription = lens.IsPrescription,
            PrimaryImageUrl = primaryImage
        };
    }

    /// <summary>
    /// Applies sorting to the query based on the specified field and order
    /// </summary>
    private static IQueryable<Lens> ApplySorting(IQueryable<Lens> query, string sortBy, string sortOrder)
    {
        var isDescending = sortOrder.ToLower() == "desc";

        return sortBy.ToLower() switch
        {
            "price" => isDescending
                ? query.OrderByDescending(l => l.Price)
                : query.OrderBy(l => l.Price),
            "name" => isDescending
                ? query.OrderByDescending(l => l.Name)
                : query.OrderBy(l => l.Name),
            "lenstype" => isDescending
                ? query.OrderByDescending(l => l.LensType)
                : query.OrderBy(l => l.LensType),
            "lensindex" => isDescending
                ? query.OrderByDescending(l => l.LensIndex)
                : query.OrderBy(l => l.LensIndex),
            "createdat" or _ => isDescending
                ? query.OrderByDescending(l => l.CreatedAt)
                : query.OrderBy(l => l.CreatedAt)
        };
    }

    #endregion
}
