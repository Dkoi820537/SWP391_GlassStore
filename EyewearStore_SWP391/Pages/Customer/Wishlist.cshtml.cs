using EyewearStore_SWP391.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace EyewearStore_SWP391.Pages.Customer
{
    [Authorize]
    public class WishlistModel : PageModel
    {
        private readonly IWishlistService _wishlistService;
        private const int PageSize = 12;

        public WishlistModel(IWishlistService wishlistService)
        {
            _wishlistService = wishlistService;
        }

        public List<WishlistItemDto> Items { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;

        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;

        public async Task<IActionResult> OnGetAsync()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdStr, out int userId))
                return RedirectToPage("/Account/Login");

            if (CurrentPage < 1) CurrentPage = 1;

            TotalItems = await _wishlistService.GetUserWishlistCountAsync(userId);
            TotalPages = (int)Math.Ceiling(TotalItems / (double)PageSize);
            if (CurrentPage > TotalPages && TotalPages > 0) CurrentPage = TotalPages;

            Items = await _wishlistService.GetUserWishlistAsync(userId, CurrentPage, PageSize);
            return Page();
        }
    }
}
