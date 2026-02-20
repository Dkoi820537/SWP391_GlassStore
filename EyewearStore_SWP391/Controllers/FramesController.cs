using EyewearStore_SWP391.DTOs.Frame;
using EyewearStore_SWP391.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EyewearStore_SWP391.Models;

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
    private readonly IWishlistService _wishlistService;
    private readonly EyewearStoreContext _context;
    private readonly IConfiguration _configuration;

    public FramesController(
        IFrameService frameService,
        IWishlistService wishlistService,
        EyewearStoreContext context,
        IConfiguration configuration)
    {
        _frameService = frameService;
        _wishlistService = wishlistService;
        _context = context;
        _configuration = configuration;
    }

    /// <summary>
    /// Creates a new frame product
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(FrameResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateFrame([FromBody] CreateFrameDto createDto)
    {
        try
        {
            if (createDto == null)
                return BadRequest("Frame data is required");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

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
    /// Gets a single frame by ID
    /// </summary>
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
                return NotFound($"Frame with ID {id} not found");

            return Ok(responseDto);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                $"An error occurred while retrieving the frame: {ex.Message}");
        }
    }

    /// <summary>
    /// Updates an existing frame product.
    /// ✅ If InventoryQty changes from 0 → positive, automatically sends
    /// restock email notifications to all users who wishlisted this frame.
    /// </summary>
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
                return BadRequest("Frame data is required");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // ── Snapshot inventory BEFORE update ──────────────────────────────
            var currentFrame = await _context.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ProductId == id);

            var wasOutOfStock = currentFrame != null && currentFrame.InventoryQty <= 0;

            // ── Perform the actual update ──────────────────────────────────────
            var responseDto = await _frameService.UpdateFrameAsync(id, updateDto);

            if (responseDto == null)
                return NotFound($"Frame with ID {id} not found");

            // ── Check if restocked → trigger email notifications ───────────────
            var isNowInStock = responseDto.InventoryQty > 0;

            if (wasOutOfStock && isNowInStock)
            {
                var baseUrl = _configuration["BaseUrl"] ?? "https://localhost:7001";
                var productUrl = $"{baseUrl}/Products/Details/{id}";

                // Fire-and-forget — don't block the HTTP response for email sending
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _wishlistService.NotifyRestockAsync(id, productUrl);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Restock Email Error] FrameId={id}: {ex.Message}");
                    }
                });
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
                return NotFound($"Frame with ID {id} not found");

            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                $"An error occurred while deleting the frame: {ex.Message}");
        }
    }
}