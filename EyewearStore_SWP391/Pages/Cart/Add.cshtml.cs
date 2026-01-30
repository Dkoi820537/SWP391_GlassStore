using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using EyewearStore_SWP391.Services;
using System.Security.Claims;

namespace EyewearStore_SWP391.Pages.Cart
{
    public class AddModel : PageModel
    {
        private readonly ICartService _cartService;
        public AddModel(ICartService cartService) => _cartService = cartService;

        [BindProperty] public string ProductType { get; set; }
        [BindProperty] public int ProductId { get; set; }
        [BindProperty] public int Quantity { get; set; } = 1;
        [BindProperty] public string? TempPrescriptionJson { get; set; }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!User.Identity.IsAuthenticated) return Challenge();
            var uid = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            try
            {
                await _cartService.AddToCartAsync(uid, ProductType, ProductId, Quantity, TempPrescriptionJson);
                TempData["SuccessMessage"] = "Đã thêm vào giỏ hàng";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            return RedirectToPage("/Cart/Index");
        }
    }
}