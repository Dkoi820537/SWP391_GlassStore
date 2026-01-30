using EyewearStore_SWP391.DTOs.Lens;
using EyewearStore_SWP391.Services;
using Microsoft.AspNetCore.Mvc;

namespace EyewearStore_SWP391.Controllers;

/// <summary>
/// API Controller for managing lens products
/// </summary>
[Route("api/[controller]")]
[ApiController]
[Produces("application/json")]
public class LensesController : ControllerBase
{
    private readonly ILensService _lensService;

    /// <summary>
    /// Initializes a new instance of the LensesController
    /// </summary>
    /// <param name="lensService">The lens service</param>
    public LensesController(ILensService lensService)
    {
        _lensService = lensService;
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

            var responseDto = await _lensService.CreateLensAsync(createDto);

            return CreatedAtAction(
                nameof(GetLensById),
                new { id = responseDto.LensId },
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
            var response = await _lensService.GetAllLensesAsync(
                search, lensType, coating, priceMin, priceMax,
                stockStatus, sortBy, sortOrder, pageNumber, pageSize);

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
            var responseDto = await _lensService.GetLensByIdAsync(id);

            if (responseDto == null)
            {
                return NotFound($"Lens with ID {id} not found");
            }

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

            var responseDto = await _lensService.UpdateLensAsync(id, updateDto);

            if (responseDto == null)
            {
                return NotFound($"Lens with ID {id} not found");
            }

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
            var deleted = await _lensService.DeleteLensAsync(id);

            if (!deleted)
            {
                return NotFound($"Lens with ID {id} not found");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                $"An error occurred while deleting the lens: {ex.Message}");
        }
    }
}
