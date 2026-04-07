using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using EyewearStore_SWP391.Models;
using EyewearStore_SWP391.Services;
using System.Security.Claims;

namespace EyewearStore_SWP391.Pages.Cart
{
    public class IndexModel : PageModel
    {
        private readonly ICartService _cartService;
        private readonly EyewearStoreContext _context;

        public IndexModel(ICartService cartService, EyewearStoreContext context)
        {
            _cartService = cartService;
            _context = context;
        }

        public Models.Cart? Cart { get; set; }

        public decimal Total { get; set; }
        public decimal SubtotalBase { get; set; }
        public decimal PrescriptionFeesTotal { get; set; }
        public decimal ShippingFee { get; set; }

        public List<PrescriptionProfile> Prescriptions { get; set; } = new();

        /// <summary>
        /// Dictionary lensProductId -> Product, dùng để hiển thị tên + giá Lens
        /// trong các CartItem của đơn gia công.
        /// </summary>
        public Dictionary<int, Product> LensProducts { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            if (User.Identity?.IsAuthenticated != true)
                return Challenge();

            var uidValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(uidValue, out var uid))
                return Challenge();

            Cart = await _cartService.GetCartByUserIdAsync(uid);

            // Load Lens products cho các đơn gia công
            if (Cart?.CartItems != null)
            {
                var lensIds = Cart.CartItems
                    .Select(ci => CartService.ExtractLensProductId(ci.TempPrescriptionJson))
                    .Where(id => id.HasValue)
                    .Select(id => id!.Value)
                    .Distinct()
                    .ToList();

                if (lensIds.Any())
                {
                    LensProducts = await _context.Products
                        .Where(p => lensIds.Contains(p.ProductId))
                        .ToDictionaryAsync(p => p.ProductId);
                }
            }

            var (subtotalBase, prescriptionFees, shippingFee, grandTotal) =
                await _cartService.GetCartTotalsBreakdownAsync(uid);

            SubtotalBase = subtotalBase;
            PrescriptionFeesTotal = prescriptionFees;
            ShippingFee = shippingFee;
            Total = grandTotal;

            await LoadPrescriptionsAsync(uid);
            return Page();
        }

        private async Task LoadPrescriptionsAsync(int userId)
        {
            Prescriptions = await _context.PrescriptionProfiles
                .Where(p => p.UserId == userId && p.IsActive)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
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
            try
            {
                await _cartService.RemoveItemAsync(cartItemId);
                TempData["SuccessMessage"] = "Item removed from cart";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostClearAsync()
        {
            try
            {
                if (User.Identity?.IsAuthenticated != true)
                    return Challenge();

                var uidValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(uidValue, out var uid))
                    return Challenge();

                await _cartService.ClearCartAsync(uid);
                TempData["SuccessMessage"] = "Cart has been cleared";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

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

        public async Task<IActionResult> OnPostUpdatePrescriptionByIdAsync(int cartItemId, int? prescriptionId)
        {
            try
            {
                if (User.Identity?.IsAuthenticated != true)
                    return Challenge();

                var uidValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(uidValue, out var uid))
                    return Challenge();

                await _cartService.UpdateItemPrescriptionByIdAsync(cartItemId, prescriptionId, uid);
                TempData["SuccessMessage"] = "Prescription updated for this item.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToPage();
        }
    }
}