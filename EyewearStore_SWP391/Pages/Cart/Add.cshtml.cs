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

        [BindProperty] public int ProductId { get; set; }
        [BindProperty] public int Quantity { get; set; } = 1;
        [BindProperty] public int? ServiceId { get; set; }
        [BindProperty] public string? TempPrescriptionJson { get; set; }
        [BindProperty] public int? PrescriptionId { get; set; }

        public async Task<IActionResult> OnPostAsync()
        {
            if (User?.Identity?.IsAuthenticated != true) return Challenge();
            var uid = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            try
            {
                await _cartService.AddToCartAsync(uid, ProductId, Quantity, ServiceId, TempPrescriptionJson, PrescriptionId);
                TempData["SuccessMessage"] = "Added to cart";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            return RedirectToPage("/Cart/Index");
        }
    }
}