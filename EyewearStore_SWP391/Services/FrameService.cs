using EyewearStore_SWP391.DTOs.Frame;
using EyewearStore_SWP391.Models;
using Microsoft.EntityFrameworkCore;

namespace EyewearStore_SWP391.Services;

/// <summary>
/// Service implementation for frame product operations using TPT inheritance
/// </summary>
public class FrameService : IFrameService
{
    private readonly EyewearStoreContext _context;

    /// <summary>
    /// Initializes a new instance of the FrameService
    /// </summary>
    /// <param name="context">The database context</param>
    public FrameService(EyewearStoreContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<FrameResponseDto> CreateFrameAsync(CreateFrameDto createDto)
    {
        var frame = new Frame
        {
            // Base Product properties
            Sku = createDto.Sku,
            Name = createDto.Name,
            Description = createDto.Description,
            ProductType = "Frame",
            Price = createDto.Price,
            Currency = createDto.Currency,
            InventoryQty = createDto.InventoryQty,
            IsActive = createDto.IsActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,

            // Frame-specific properties
            FrameMaterial = createDto.FrameMaterial,
            FrameType = createDto.FrameType,
            BridgeWidth = createDto.BridgeWidth,
            TempleLength = createDto.TempleLength
        };

        await _context.Frames.AddAsync(frame);
        await _context.SaveChangesAsync();

        return await MapToResponseDtoAsync(frame);
    }

    /// <inheritdoc />
    public async Task<FrameListResponseDto> GetAllFramesAsync(
        string? search = null,
        string? frameType = null,
        string? frameMaterial = null,
        decimal? priceMin = null,
        decimal? priceMax = null,
        bool? isActive = null,
        string sortBy = "createdAt",
        string sortOrder = "desc",
        int pageNumber = 1,
        int pageSize = 10)
    {
        // Validate pagination parameters
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        // Use OfType<Frame>() to query TPT inheritance
        var query = _context.Products.OfType<Frame>().AsQueryable();

        // Search filter (name or description)
        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(f =>
                f.Name.ToLower().Contains(searchLower) ||
                (f.Description != null && f.Description.ToLower().Contains(searchLower)) ||
                (f.FrameType != null && f.FrameType.ToLower().Contains(searchLower)) ||
                (f.FrameMaterial != null && f.FrameMaterial.ToLower().Contains(searchLower)));
        }

        // Filter by frame type
        if (!string.IsNullOrWhiteSpace(frameType))
        {
            query = query.Where(f => f.FrameType != null && f.FrameType.ToLower() == frameType.ToLower());
        }

        // Filter by frame material
        if (!string.IsNullOrWhiteSpace(frameMaterial))
        {
            query = query.Where(f => f.FrameMaterial != null && f.FrameMaterial.ToLower() == frameMaterial.ToLower());
        }

        // Filter by price range
        if (priceMin.HasValue)
        {
            query = query.Where(f => f.Price >= priceMin.Value);
        }

        if (priceMax.HasValue)
        {
            query = query.Where(f => f.Price <= priceMax.Value);
        }

        // Filter by is_active
        if (isActive.HasValue)
        {
            query = query.Where(f => f.IsActive == isActive.Value);
        }

        // Apply sorting
        query = ApplySorting(query, sortBy, sortOrder);

        // Get total count before pagination
        var totalCount = await query.CountAsync();

        // Calculate pagination values
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        // Apply pagination
        var frames = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Map to response DTOs (with primary image)
        var items = new List<FrameResponseDto>();
        foreach (var frame in frames)
        {
            items.Add(await MapToResponseDtoAsync(frame));
        }

        return new FrameListResponseDto
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        };
    }

    /// <inheritdoc />
    public async Task<FrameResponseDto?> GetFrameByIdAsync(int id)
    {
        var frame = await _context.Products
            .OfType<Frame>()
            .FirstOrDefaultAsync(f => f.ProductId == id);

        if (frame == null)
        {
            return null;
        }

        return await MapToResponseDtoAsync(frame);
    }

    /// <inheritdoc />
    public async Task<FrameResponseDto?> UpdateFrameAsync(int id, UpdateFrameDto updateDto)
    {
        var frame = await _context.Products
            .OfType<Frame>()
            .FirstOrDefaultAsync(f => f.ProductId == id);

        if (frame == null)
        {
            return null;
        }

        // Update base Product properties
        frame.Sku = updateDto.Sku;
        frame.Name = updateDto.Name;
        frame.Description = updateDto.Description;
        frame.Price = updateDto.Price;
        frame.Currency = updateDto.Currency;
        frame.InventoryQty = updateDto.InventoryQty;
        frame.IsActive = updateDto.IsActive;
        frame.UpdatedAt = DateTime.UtcNow;

        // Update Frame-specific properties
        frame.FrameMaterial = updateDto.FrameMaterial;
        frame.FrameType = updateDto.FrameType;
        frame.BridgeWidth = updateDto.BridgeWidth;
        frame.TempleLength = updateDto.TempleLength;

        _context.Products.Update(frame);
        await _context.SaveChangesAsync();

        return await MapToResponseDtoAsync(frame);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteFrameAsync(int id)
    {
        var frame = await _context.Products
            .OfType<Frame>()
            .FirstOrDefaultAsync(f => f.ProductId == id);

        if (frame == null)
        {
            return false;
        }

        // Soft delete by setting IsActive to false
        frame.IsActive = false;
        frame.UpdatedAt = DateTime.UtcNow;

        _context.Products.Update(frame);
        await _context.SaveChangesAsync();

        return true;
    }

    #region Private Helper Methods

    /// <summary>
    /// Maps a Frame entity to a FrameResponseDto
    /// </summary>
    private async Task<FrameResponseDto> MapToResponseDtoAsync(Frame frame)
    {
        // Get primary image URL
        var primaryImage = await _context.ProductImages
            .Where(pi => pi.ProductId == frame.ProductId && pi.IsPrimary && pi.IsActive)
            .Select(pi => pi.ImageUrl)
            .FirstOrDefaultAsync();

        return new FrameResponseDto
        {
            ProductId = frame.ProductId,
            Sku = frame.Sku,
            Name = frame.Name,
            Description = frame.Description,
            Price = frame.Price,
            Currency = frame.Currency,
            InventoryQty = frame.InventoryQty,
            IsActive = frame.IsActive,
            CreatedAt = frame.CreatedAt,
            UpdatedAt = frame.UpdatedAt,
            FrameMaterial = frame.FrameMaterial,
            FrameType = frame.FrameType,
            BridgeWidth = frame.BridgeWidth,
            TempleLength = frame.TempleLength,
            PrimaryImageUrl = primaryImage
        };
    }

    /// <summary>
    /// Applies sorting to the query based on the specified field and order
    /// </summary>
    private static IQueryable<Frame> ApplySorting(IQueryable<Frame> query, string sortBy, string sortOrder)
    {
        var isDescending = sortOrder.ToLower() == "desc";

        return sortBy.ToLower() switch
        {
            "price" => isDescending
                ? query.OrderByDescending(f => f.Price)
                : query.OrderBy(f => f.Price),
            "name" => isDescending
                ? query.OrderByDescending(f => f.Name)
                : query.OrderBy(f => f.Name),
            "frametype" => isDescending
                ? query.OrderByDescending(f => f.FrameType)
                : query.OrderBy(f => f.FrameType),
            "framematerial" => isDescending
                ? query.OrderByDescending(f => f.FrameMaterial)
                : query.OrderBy(f => f.FrameMaterial),
            "createdat" or _ => isDescending
                ? query.OrderByDescending(f => f.CreatedAt)
                : query.OrderBy(f => f.CreatedAt)
        };
    }

    #endregion
}
