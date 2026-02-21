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
        public decimal SubtotalBase { get; set; }
        public decimal PrescriptionFeesTotal { get; set; }
        public List<Address> Addresses { get; set; } = new();
        public Address? DefaultAddress { get; set; }


        [BindProperty]
        public int SelectedAddressId { get; set; }

        [BindProperty]
        public string PaymentMethod { get; set; } = "Stripe";

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
            var (subtotalBase, prescriptionFees, grandTotal) = await _cartService.GetCartTotalsBreakdownAsync(userId);
            SubtotalBase = subtotalBase;
            PrescriptionFeesTotal = prescriptionFees;
            Total = grandTotal;

            if (Cart == null || !Cart.CartItems.Any())
            {
                TempData["ErrorMessage"] = "Your cart is empty.";
                return RedirectToPage("/Cart/Index");
            }

            await LoadAddressesAsync(userId);

            return Page();
        }

        // ✅✅✅ FIXED METHOD - TẠO ORDER VỚI ADDRESS SNAPSHOT ✅✅✅
        public async Task<IActionResult> OnPostCheckoutAsync()
        {
            if (!User.Identity?.IsAuthenticated ?? true)
                return RedirectToPage("/Account/Login");

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            // ✅ STEP 1: Get or create address
            Address? selectedAddress = null;

            if (SelectedAddressId == 0 &&
                !string.IsNullOrWhiteSpace(NewReceiverName) &&
                !string.IsNullOrWhiteSpace(NewPhone) &&
                !string.IsNullOrWhiteSpace(NewAddressLine))
            {
                // User is adding new address inline
                selectedAddress = new Address
                {
                    UserId = userId,
                    ReceiverName = NewReceiverName.Trim(),
                    Phone = NewPhone.Trim(),
                    AddressLine = NewAddressLine.Trim(),
                    IsDefault = false,
                    CreatedAt = DateTime.UtcNow
                };

                var hasAddresses = await _context.Addresses.AnyAsync(a => a.UserId == userId);
                if (!hasAddresses)
                    selectedAddress.IsDefault = true;

                _context.Addresses.Add(selectedAddress);
                await _context.SaveChangesAsync();
            }
            else if (SelectedAddressId > 0)
            {
                // User selected existing address
                selectedAddress = await _context.Addresses
                    .AsNoTracking()
                    .FirstOrDefaultAsync(a => a.AddressId == SelectedAddressId && a.UserId == userId);
            }

            if (selectedAddress == null)
            {
                TempData["ErrorMessage"] = "Please select or add a shipping address.";
                Cart = await _cartService.GetCartByUserIdAsync(userId);
                var (subtotalBase, prescriptionFees, grandTotal) = await _cartService.GetCartTotalsBreakdownAsync(userId);
                SubtotalBase = subtotalBase;
                PrescriptionFeesTotal = prescriptionFees;
                Total = grandTotal;
                await LoadAddressesAsync(userId);

                return Page();
            }

            try
            {
                // Get cart with per-item prescription data and totals breakdown
                var cart = await _cartService.GetCartByUserIdAsync(userId);
                var (subtotalBase, prescriptionFeesTotal, total) = await _cartService.GetCartTotalsBreakdownAsync(userId);

                if (cart == null || !cart.CartItems.Any())
                {
                    TempData["ErrorMessage"] = "Your cart is empty.";
                    return RedirectToPage("/Cart/Index");
                }

                // CREATE ORDER WITH ADDRESS SNAPSHOT
                var order = new Order
                {
                    UserId = userId,
                    ReceiverName = selectedAddress.ReceiverName,
                    Phone = selectedAddress.Phone,
                    AddressLine = selectedAddress.AddressLine,
                    AddressId = selectedAddress.AddressId,
                    TotalAmount = total,
                    Status = PaymentMethod == "COD" ? "Pending Confirmation" : "Pending",
                    PaymentMethod = PaymentMethod == "COD" ? "COD" : "Stripe",
                    CreatedAt = DateTime.UtcNow
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // Create order items: preserve per-item PrescriptionId and PrescriptionFee from cart
                foreach (var cartItem in cart.CartItems)
                {
                    decimal unitPrice = cartItem.Product?.Price ?? 0m;
                    if (cartItem.Service != null)
                        unitPrice += cartItem.Service.Price;

                    var orderItem = new OrderItem
                    {
                        OrderId = order.OrderId,
                        ProductId = cartItem.ProductId,
                        PrescriptionId = cartItem.PrescriptionId,
                        PrescriptionFee = cartItem.PrescriptionFee,
                        Quantity = cartItem.Quantity,
                        UnitPrice = unitPrice,
                        IsBundle = false,
                        SnapshotJson = null
                    };

                    _context.OrderItems.Add(orderItem);
                }

                await _context.SaveChangesAsync();

                // ── COD PATH: skip Stripe, reduce inventory, clear cart, redirect ──
                if (PaymentMethod == "COD")
                {
                    // Reduce inventory
                    foreach (var oi in order.OrderItems)
                    {
                        var product = await _context.Products.FindAsync(oi.ProductId);
                        if (product != null && product.InventoryQty.HasValue)
                        {
                            product.InventoryQty -= oi.Quantity;
                            if (product.InventoryQty < 0) product.InventoryQty = 0;
                        }
                    }
                    await _context.SaveChangesAsync();

                    // Clear cart
                    await _cartService.ClearCartAsync(userId);

                    return RedirectToPage("/Checkout/Success", new { order_id = order.OrderId });
                }

                // ── STRIPE PATH (existing flow) ──
                var lineItems = new List<StripeLineItemDto>();

                foreach (var ci in cart.CartItems)
                {
                    decimal unitPrice = ci.Product?.Price ?? 0m;
                    if (ci.Service != null)
                        unitPrice += ci.Service.Price;
                    decimal unitTotal = unitPrice + ci.PrescriptionFee;

                    var itemName = ci.Product?.Name ?? ci.Service?.Name ?? "Product";
                    if (ci.Service != null)
                        itemName += $" + {ci.Service.Name}";
                    if (ci.PrescriptionId.HasValue && ci.Prescription != null)
                        itemName += $" (Rx: {ci.Prescription.ProfileName ?? "Prescription"})";

                    string? imageUrl = null;
                    if (ci.Product?.ProductImages != null)
                    {
                        var primaryImg = ci.Product.ProductImages
                            .FirstOrDefault(img => img.IsPrimary && img.IsActive);
                        if (primaryImg != null && !string.IsNullOrEmpty(primaryImg.ImageUrl))
                        {
                            imageUrl = primaryImg.ImageUrl.StartsWith("http")
                                ? primaryImg.ImageUrl
                                : $"{Request.Scheme}://{Request.Host}{primaryImg.ImageUrl}";
                        }
                    }

                    lineItems.Add(new StripeLineItemDto
                    {
                        ProductName = itemName,
                        UnitAmountInSmallestUnit = (long)unitTotal,
                        Quantity = ci.Quantity,
                        ImageUrl = imageUrl
                    });
                }

                // ✅ STEP 7: Create Stripe session
                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                var successUrl = $"{baseUrl}/Checkout/Success";
                var cancelUrl = $"{baseUrl}/Checkout/Cancel";

                var userEmail = User.FindFirst(ClaimTypes.Email)?.Value
                             ?? User.FindFirst(ClaimTypes.Name)?.Value
                             ?? "";

                var stripeUrl = await _stripeService.CreateCheckoutSessionAsync(
                    order.OrderId,
                    lineItems,
                    successUrl,
                    cancelUrl,
                    userEmail);

                return Redirect(stripeUrl);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                Cart = await _cartService.GetCartByUserIdAsync(userId);
                var (subtotalBase, prescriptionFees, grandTotal) = await _cartService.GetCartTotalsBreakdownAsync(userId);
                SubtotalBase = subtotalBase;
                PrescriptionFeesTotal = prescriptionFees;
                Total = grandTotal;
                await LoadAddressesAsync(userId);

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

            DefaultAddress = Addresses.FirstOrDefault(a => a.IsDefault)
                          ?? Addresses.FirstOrDefault();

            if (DefaultAddress != null && SelectedAddressId == 0)
                SelectedAddressId = DefaultAddress.AddressId;
        }


    }
}