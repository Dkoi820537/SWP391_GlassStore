using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using EyewearStore_SWP391.DTOs;
using EyewearStore_SWP391.Models;
using EyewearStore_SWP391.Services;

namespace EyewearStore_SWP391.Pages.Checkout
{
    public class IndexModel : PageModel
    {
        private readonly ICartService _cartService;
        private readonly IOrderService _orderService;
        private readonly IStripeService _stripeService;
        private readonly EyewearStoreContext _context;

        public IndexModel(
            ICartService cartService,
            IOrderService orderService,
            IStripeService stripeService,
            EyewearStoreContext context)
        {
            _cartService = cartService;
            _orderService = orderService;
            _stripeService = stripeService;
            _context = context;
        }

        public Models.Cart? Cart { get; set; }
        public decimal Total { get; set; }
        public List<Address> Addresses { get; set; } = new();
        public Address? DefaultAddress { get; set; }
        public List<PrescriptionProfile> Prescriptions { get; set; } = new();

        [BindProperty]
        public int SelectedAddressId { get; set; }

        [BindProperty]
        public int? SelectedPrescriptionId { get; set; }

        [BindProperty]
        public string? NewReceiverName { get; set; }
        [BindProperty]
        public string? NewPhone { get; set; }
        [BindProperty]
        public string? NewAddressLine { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (!User.Identity?.IsAuthenticated ?? true)
                return RedirectToPage("/Account/Login");

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            Cart = await _cartService.GetCartByUserIdAsync(userId);
            Total = await _cartService.CalculateCartTotalAsync(userId);

            if (Cart == null || !Cart.CartItems.Any())
            {
                TempData["ErrorMessage"] = "Your cart is empty.";
                return RedirectToPage("/Cart/Index");
            }

            await LoadAddressesAsync(userId);
            await LoadPrescriptionsAsync(userId);
            return Page();
        }

        public async Task<IActionResult> OnPostCheckoutAsync()
        {
            if (!User.Identity?.IsAuthenticated ?? true)
                return RedirectToPage("/Account/Login");

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            // Inline add address
            if (SelectedAddressId == 0 &&
                !string.IsNullOrWhiteSpace(NewReceiverName) &&
                !string.IsNullOrWhiteSpace(NewPhone) &&
                !string.IsNullOrWhiteSpace(NewAddressLine))
            {
                var newAddr = new Address
                {
                    UserId = userId,
                    ReceiverName = NewReceiverName,
                    Phone = NewPhone,
                    AddressLine = NewAddressLine,
                    IsDefault = false,
                    CreatedAt = DateTime.UtcNow
                };

                var hasAddresses = await _context.Addresses.AnyAsync(a => a.UserId == userId);
                if (!hasAddresses) newAddr.IsDefault = true;

                _context.Addresses.Add(newAddr);
                await _context.SaveChangesAsync();
                SelectedAddressId = newAddr.AddressId;
            }

            if (SelectedAddressId <= 0)
            {
                TempData["ErrorMessage"] = "Please select or add a shipping address.";
                Cart = await _cartService.GetCartByUserIdAsync(userId);
                Total = await _cartService.CalculateCartTotalAsync(userId);
                await LoadAddressesAsync(userId);
                await LoadPrescriptionsAsync(userId);
                return Page();
            }

            // Validate prescription selection if provided
            if (SelectedPrescriptionId.HasValue && SelectedPrescriptionId.Value > 0)
            {
                var presc = await _context.PrescriptionProfiles
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.PrescriptionId == SelectedPrescriptionId.Value && p.UserId == userId && p.IsActive);

                if (presc == null)
                {
                    TempData["ErrorMessage"] = "Selected prescription is invalid or inactive.";
                    Cart = await _cartService.GetCartByUserIdAsync(userId);
                    Total = await _cartService.CalculateCartTotalAsync(userId);
                    await LoadAddressesAsync(userId);
                    await LoadPrescriptionsAsync(userId);
                    return Page();
                }
            }

            try
            {
                // Create pending order and pass prescription id (optional)
                var order = await _orderService.CreatePendingOrderAsync(userId, SelectedAddressId, SelectedPrescriptionId);

                // Build Stripe line items (from cart) and include prescription info in product name if chosen
                var cart = await _cartService.GetCartByUserIdAsync(userId);
                var lineItems = new List<StripeLineItemDto>();

                if (cart != null)
                {
                    foreach (var ci in cart.CartItems)
                    {
                        decimal unitPrice = ci.Product?.Price ?? 0m;
                        if (ci.Service != null) unitPrice += ci.Service.Price;

                        var itemName = ci.Product?.Name ?? ci.Service?.Name ?? "Product";
                        if (ci.Service != null) itemName += $" + {ci.Service.Name}";

                        if (SelectedPrescriptionId.HasValue && SelectedPrescriptionId.Value > 0)
                        {
                            var presc = await _context.PrescriptionProfiles
                                .AsNoTracking()
                                .FirstOrDefaultAsync(p => p.PrescriptionId == SelectedPrescriptionId.Value);
                            if (presc != null) itemName += $" (Rx: {presc.ProfileName})";
                        }

                        string? imageUrl = null;
                        if (ci.Product?.ProductImages != null)
                        {
                            var primaryImg = ci.Product.ProductImages.FirstOrDefault(img => img.IsPrimary && img.IsActive);
                            if (primaryImg != null && !string.IsNullOrEmpty(primaryImg.ImageUrl))
                                imageUrl = primaryImg.ImageUrl.StartsWith("http") ? primaryImg.ImageUrl : $"{Request.Scheme}://{Request.Host}{primaryImg.ImageUrl}";
                        }

                        lineItems.Add(new StripeLineItemDto
                        {
                            ProductName = itemName,
                            UnitAmountInSmallestUnit = (long)unitPrice,
                            Quantity = ci.Quantity,
                            ImageUrl = imageUrl
                        });
                    }
                }

                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                var successUrl = $"{baseUrl}/Checkout/Success";
                var cancelUrl = $"{baseUrl}/Checkout/Cancel";

                var userEmail = User.FindFirst(ClaimTypes.Email)?.Value
                             ?? User.FindFirst(ClaimTypes.Name)?.Value ?? "";

                var stripeUrl = await _stripeService.CreateCheckoutSessionAsync(order.OrderId, lineItems, successUrl, cancelUrl, userEmail);

                return Redirect(stripeUrl);
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                Cart = await _cartService.GetCartByUserIdAsync(userId);
                Total = await _cartService.CalculateCartTotalAsync(userId);
                await LoadAddressesAsync(userId);
                await LoadPrescriptionsAsync(userId);
                return Page();
            }
        }

        private async Task LoadAddressesAsync(int userId)
        {
            Addresses = await _context.Addresses
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.IsDefault)
                .ThenByDescending(a => a.CreatedAt)
                .ToListAsync();

            DefaultAddress = Addresses.FirstOrDefault(a => a.IsDefault) ?? Addresses.FirstOrDefault();

            if (DefaultAddress != null && SelectedAddressId == 0) SelectedAddressId = DefaultAddress.AddressId;
        }

        private async Task LoadPrescriptionsAsync(int userId)
        {
            Prescriptions = await _context.PrescriptionProfiles
                .Where(p => p.UserId == userId && p.IsActive)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }
    }
}