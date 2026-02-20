using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using EyewearStore_SWP391.Services;
using System.Threading.Tasks;

namespace EyewearStore_SWP391.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WishlistController : ControllerBase
    {
        private readonly IWishlistService _wishlistService;

        public WishlistController(IWishlistService wishlistService)
        {
            _wishlistService = wishlistService;
        }

        // POST: api/wishlist/add
        [HttpPost("add")]
        [Authorize] // require authenticated user
        public async Task<IActionResult> Add([FromBody] AddWishlistDto dto)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

            var (success, message) = await _wishlistService.AddToWishlistAsync(userId, dto.ProductId);
            if (!success) return BadRequest(new { message });
            return Ok(new { message });
        }

        // POST: api/wishlist/remove
        [HttpPost("remove")]
        [Authorize]
        public async Task<IActionResult> Remove([FromBody] AddWishlistDto dto)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

            var ok = await _wishlistService.RemoveFromWishlistAsync(userId, dto.ProductId);
            if (!ok) return NotFound(new { message = "Not found in wishlist" });
            return Ok(new { message = "Removed" });
        }

        // GET: api/wishlist/my
        [HttpGet("my")]
        [Authorize]
        public async Task<IActionResult> My()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

            var items = await _wishlistService.GetUserWishlistAsync(userId);
            return Ok(items);
        }
    }

    public class AddWishlistDto
    {
        public int ProductId { get; set; }
    }
}