using EyewearStore_SWP391.DTOs.Frame;
using EyewearStore_SWP391.Services;
using Microsoft.AspNetCore.Mvc;

namespace EyewearStore_SWP391.Controllers;

/// <summary>
/// API Controller for managing frame products
/// </summary>
[Route("api/[controller]")]
[ApiController]
[Produces("application/json")]
public class FramesController : ControllerBase
{
    private readonly IFrameService _frameService;

    /// <summary>
    /// Initializes a new instance of the FramesController
    /// </summary>
    /// <param name="frameService">The frame service</param>
    public FramesController(IFrameService frameService)
    {
        _frameService = frameService;
    }

    /// <summary>
    /// Creates a new frame product
    /// </summary>
    /// <param name="createDto">The frame data to create</param>
    /// <returns>The created frame product</returns>
    /// <response code="201">Returns the newly created frame</response>
    /// <response code="400">If the frame data is invalid</response>
    /// <response code="500">If there was an internal server error</response>
    [HttpPost]
    [ProducesResponseType(typeof(FrameResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateFrame([FromBody] CreateFrameDto createDto)
    {
        try
        {
            if (createDto == null)
            {
                return BadRequest("Frame data is required");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var responseDto = await _frameService.CreateFrameAsync(createDto);

            return CreatedAtAction(
                nameof(GetFrameById),
                new { id = responseDto.ProductId },
                responseDto);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                $"An error occurred while creating the frame: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets all frames with optional filtering, searching, sorting, and pagination
    /// </summary>
    /// <param name="search">Search term for name, description, frame type, or material</param>
    /// <param name="frameType">Filter by frame type</param>
    /// <param name="frameMaterial">Filter by frame material</param>
    /// <param name="priceMin">Minimum price filter</param>
    /// <param name="priceMax">Maximum price filter</param>
    /// <param name="isActive">Filter by active status</param>
    /// <param name="sortBy">Sort field (price, name, frameType, frameMaterial, createdAt)</param>
    /// <param name="sortOrder">Sort order (asc, desc)</param>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 10, max: 100)</param>
    /// <returns>A paginated list of frames</returns>
    /// <response code="200">Returns the list of frames</response>
    /// <response code="500">If there was an internal server error</response>
    [HttpGet]
    [ProducesResponseType(typeof(FrameListResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAllFrames(
        [FromQuery] string? search = null,
        [FromQuery] string? frameType = null,
        [FromQuery] string? frameMaterial = null,
        [FromQuery] decimal? priceMin = null,
        [FromQuery] decimal? priceMax = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] string sortBy = "createdAt",
        [FromQuery] string sortOrder = "desc",
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            var response = await _frameService.GetAllFramesAsync(
                search, frameType, frameMaterial, priceMin, priceMax,
                isActive, sortBy, sortOrder, pageNumber, pageSize);

            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                $"An error occurred while retrieving frames: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets a single frame by ID (ProductId)
    /// </summary>
    /// <param name="id">The product ID</param>
    /// <returns>The frame product</returns>
    /// <response code="200">Returns the frame</response>
    /// <response code="404">If the frame is not found</response>
    /// <response code="500">If there was an internal server error</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(FrameResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetFrameById(int id)
    {
        try
        {
            var responseDto = await _frameService.GetFrameByIdAsync(id);

            if (responseDto == null)
            {
                return NotFound($"Frame with ID {id} not found");
            }

            return Ok(responseDto);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                $"An error occurred while retrieving the frame: {ex.Message}");
        }
    }

    /// <summary>
    /// Updates an existing frame product
    /// </summary>
    /// <param name="id">The product ID to update</param>
    /// <param name="updateDto">The updated frame data</param>
    /// <returns>The updated frame product</returns>
    /// <response code="200">Returns the updated frame</response>
    /// <response code="400">If the frame data is invalid</response>
    /// <response code="404">If the frame is not found</response>
    /// <response code="500">If there was an internal server error</response>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(FrameResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateFrame(int id, [FromBody] UpdateFrameDto updateDto)
    {
        try
        {
            if (updateDto == null)
            {
                return BadRequest("Frame data is required");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var responseDto = await _frameService.UpdateFrameAsync(id, updateDto);

            if (responseDto == null)
            {
                return NotFound($"Frame with ID {id} not found");
            }

            return Ok(responseDto);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                $"An error occurred while updating the frame: {ex.Message}");
        }
    }

    /// <summary>
    /// Soft deletes a frame by setting IsActive to false
    /// </summary>
    /// <param name="id">The product ID to delete</param>
    /// <returns>No content on success</returns>
    /// <response code="204">The frame was successfully soft deleted</response>
    /// <response code="404">If the frame is not found</response>
    /// <response code="500">If there was an internal server error</response>
    /// <remarks>
    /// Soft delete is implemented by setting IsActive to false.
    /// The product will no longer appear in active product listings.
    /// </remarks>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteFrame(int id)
    {
        try
        {
            var deleted = await _frameService.DeleteFrameAsync(id);

            if (!deleted)
            {
                return NotFound($"Frame with ID {id} not found");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                $"An error occurred while deleting the frame: {ex.Message}");
        }
    }
}
