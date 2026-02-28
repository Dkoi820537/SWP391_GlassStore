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

        [BindProperty] public int ProductId { get; set; }           // Frame (hoặc sản phẩm thường)
        [BindProperty] public int? LensProductId { get; set; }      // Lens — chỉ có khi đặt gia công
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
                // Đơn gia công: có cả LensProductId lẫn ServiceId
                if (LensProductId.HasValue && ServiceId.HasValue)
                {
                    await _cartService.AddServiceOrderAsync(
                        uid,
                        frameProductId: ProductId,
                        lensProductId: LensProductId.Value,
                        serviceId: ServiceId.Value,
                        quantity: Quantity);
                }
                else
                {
                    // Đơn thường
                    await _cartService.AddToCartAsync(
                        uid, ProductId, Quantity,
                        ServiceId, TempPrescriptionJson, PrescriptionId);
                }

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


































