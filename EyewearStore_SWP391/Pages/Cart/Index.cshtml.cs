using Microsoft.AspNetCore.Mvc.RazorPages;
using EyewearStore_SWP391.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace EyewearStore_SWP391.Pages.Cart
{
    public class IndexModel : PageModel
    {
        private readonly ICartService _cartService;
        public IndexModel(ICartService cartService) => _cartService = cartService;

        public Models.Cart? Cart { get; set; }
        public decimal Total { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (!User.Identity.IsAuthenticated) return Challenge();
            var uid = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            Cart = await _cartService.GetCartByUserIdAsync(uid);
            Total = await _cartService.CalculateCartTotalAsync(uid);
            return Page();
        }

        public async Task<IActionResult> OnPostUpdateQuantityAsync(int cartItemId, int quantity)
        {
            try
            {
                await _cartService.UpdateQuantityAsync(cartItemId, quantity);
                TempData["SuccessMessage"] = "Cart updated successfully";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRemoveAsync(int cartItemId)
        {
            await _cartService.RemoveItemAsync(cartItemId);
            TempData["SuccessMessage"] = "Item removed from cart";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostClearAsync()
        {
            var uid = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            await _cartService.ClearCartAsync(uid);
            TempData["SuccessMessage"] = "Cart has been cleared";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostUpdatePrescriptionAsync(int cartItemId, string tempPrescriptionJson)
        {
            try
            {
                await _cartService.UpdateItemPrescriptionAsync(cartItemId, tempPrescriptionJson);
                TempData["SuccessMessage"] = "Prescription updated successfully";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            return RedirectToPage();
        }
    }
}