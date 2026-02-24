using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using EyewearStore_SWP391.Services;

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
        [Authorize]
        public async Task<IActionResult> Add([FromBody] WishlistProductDto dto)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            (bool success, string message) r = await _wishlistService.AddToWishlistAsync(userId.Value, dto.ProductId);
            if (!r.success) return BadRequest(new { message = r.message });
            return Ok(new { message = r.message });
        }

        // POST: api/wishlist/remove
        [HttpPost("remove")]
        [Authorize]
        public async Task<IActionResult> Remove([FromBody] WishlistProductDto dto)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var ok = await _wishlistService.RemoveFromWishlistAsync(userId.Value, dto.ProductId);
            if (!ok) return NotFound(new { message = "Not found in wishlist" });
            return Ok(new { message = "Removed" });
        }

        // GET: api/wishlist/my
        [HttpGet("my")]
        [Authorize]
        public async Task<IActionResult> My()
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var items = await _wishlistService.GetUserWishlistAsync(userId.Value);
            return Ok(items);
        }

        // POST: api/wishlist/subscribe
        [HttpPost("subscribe")]
        [Authorize]
        public async Task<IActionResult> Subscribe([FromBody] WishlistProductDto dto)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            (bool success, string message) r = await _wishlistService.SetNotifyAsync(userId.Value, dto.ProductId, true);
            if (!r.success) return BadRequest(new { message = r.message });
            return Ok(new { message = "Subscribed! You'll receive an email when this item is back in stock." });
        }

        // POST: api/wishlist/unsubscribe
        [HttpPost("unsubscribe")]
        [Authorize]
        public async Task<IActionResult> Unsubscribe([FromBody] WishlistProductDto dto)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            (bool success, string message) r = await _wishlistService.SetNotifyAsync(userId.Value, dto.ProductId, false);
            if (!r.success) return BadRequest(new { message = r.message });
            return Ok(new { message = "Unsubscribed successfully." });
        }

        private int? GetUserId()
        {
            var str = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(str, out int id) ? id : null;
        }
    }

    public class WishlistProductDto
    {
        public int ProductId { get; set; }
    }
}
