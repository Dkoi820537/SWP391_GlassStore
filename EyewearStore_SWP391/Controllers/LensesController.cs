using EyewearStore_SWP391.DTOs.Lens;
using EyewearStore_SWP391.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EyewearStore_SWP391.Controllers;

/// <summary>
/// API Controller for managing lens products
/// </summary>
[Route("api/[controller]")]
[ApiController]
[Produces("application/json")]
public class LensesController : ControllerBase
{
    private readonly EyewearStoreContext _context;

    /// <summary>
    /// Initializes a new instance of the LensesController
    /// </summary>
    /// <param name="context">The database context</param>
    public LensesController(EyewearStoreContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Creates a new lens product
    /// </summary>
    /// <param name="createDto">The lens data to create</param>
    /// <returns>The created lens product</returns>
    /// <response code="201">Returns the newly created lens</response>
    /// <response code="400">If the lens data is invalid</response>
    /// <response code="500">If there was an internal server error</response>
    [HttpPost]
    [ProducesResponseType(typeof(LensResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateLens([FromBody] CreateLensDto createDto)
    {
        try
        {
            if (createDto == null)
            {
                return BadRequest("Lens data is required");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

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

            var responseDto = MapToResponseDto(lens);

            return CreatedAtAction(
                nameof(GetLensById),
                new { id = lens.LensId },
                responseDto);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                $"An error occurred while creating the lens: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets all lenses with optional filtering, searching, sorting, and pagination
    /// </summary>
    /// <param name="search">Search term for lens type or coating</param>
    /// <param name="lensType">Filter by lens type</param>
    /// <param name="coating">Filter by coating</param>
    /// <param name="priceMin">Minimum price filter</param>
    /// <param name="priceMax">Maximum price filter</param>
    /// <param name="stockStatus">Filter by stock status</param>
    /// <param name="sortBy">Sort field (price, lensType, createdAt)</param>
    /// <param name="sortOrder">Sort order (asc, desc)</param>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 10, max: 100)</param>
    /// <returns>A paginated list of lenses</returns>
    /// <response code="200">Returns the list of lenses</response>
    /// <response code="500">If there was an internal server error</response>
    [HttpGet]
    [ProducesResponseType(typeof(LensListResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAllLenses(
        [FromQuery] string? search = null,
        [FromQuery] string? lensType = null,
        [FromQuery] string? coating = null,
        [FromQuery] decimal? priceMin = null,
        [FromQuery] decimal? priceMax = null,
        [FromQuery] string? stockStatus = null,
        [FromQuery] string sortBy = "createdAt",
        [FromQuery] string sortOrder = "desc",
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        try
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

            var response = new LensListResponseDto
            {
                Items = lenses.Select(MapToResponseDto).ToList(),
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                $"An error occurred while retrieving lenses: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets a single lens by ID
    /// </summary>
    /// <param name="id">The lens ID</param>
    /// <returns>The lens product</returns>
    /// <response code="200">Returns the lens</response>
    /// <response code="404">If the lens is not found</response>
    /// <response code="500">If there was an internal server error</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(LensResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetLensById(int id)
    {
        try
        {
            var lens = await _context.Lenses.FindAsync(id);

            if (lens == null)
            {
                return NotFound($"Lens with ID {id} not found");
            }

            var responseDto = MapToResponseDto(lens);

            return Ok(responseDto);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                $"An error occurred while retrieving the lens: {ex.Message}");
        }
    }

    /// <summary>
    /// Updates an existing lens product
    /// </summary>
    /// <param name="id">The lens ID to update</param>
    /// <param name="updateDto">The updated lens data</param>
    /// <returns>The updated lens product</returns>
    /// <response code="200">Returns the updated lens</response>
    /// <response code="400">If the lens data is invalid</response>
    /// <response code="404">If the lens is not found</response>
    /// <response code="500">If there was an internal server error</response>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(LensResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateLens(int id, [FromBody] UpdateLensDto updateDto)
    {
        try
        {
            if (updateDto == null)
            {
                return BadRequest("Lens data is required");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var lens = await _context.Lenses.FindAsync(id);

            if (lens == null)
            {
                return NotFound($"Lens with ID {id} not found");
            }

            // Update lens properties
            lens.LensType = updateDto.LensType;
            lens.IndexValue = updateDto.IndexValue;
            lens.Coating = updateDto.Coating;
            lens.Price = updateDto.Price;
            lens.StockStatus = updateDto.StockStatus;

            _context.Lenses.Update(lens);
            await _context.SaveChangesAsync();

            var responseDto = MapToResponseDto(lens);

            return Ok(responseDto);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                $"An error occurred while updating the lens: {ex.Message}");
        }
    }

    /// <summary>
    /// Soft deletes a lens by setting StockStatus to "out-of-stock"
    /// </summary>
    /// <param name="id">The lens ID to delete</param>
    /// <returns>No content on success</returns>
    /// <response code="204">The lens was successfully soft deleted</response>
    /// <response code="404">If the lens is not found</response>
    /// <response code="500">If there was an internal server error</response>
    /// <remarks>
    /// Note: The Lense model does not have an IsActive property.
    /// Soft delete is implemented by setting StockStatus to "out-of-stock".
    /// Valid StockStatus values per DB constraint: "in-stock", "low-stock", "out-of-stock".
    /// </remarks>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteLens(int id)
    {
        try
        {
            var lens = await _context.Lenses.FindAsync(id);

            if (lens == null)
            {
                return NotFound($"Lens with ID {id} not found");
            }

            // Soft delete by setting stock status to "out-of-stock"
            // Note: Lense model doesn't have IsActive property, using StockStatus instead
            // Valid values per DB constraint: 'in-stock', 'low-stock', 'out-of-stock'
            lens.StockStatus = "out-of-stock";

            _context.Lenses.Update(lens);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                $"An error occurred while deleting the lens: {ex.Message}");
        }
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
