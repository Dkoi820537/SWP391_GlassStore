using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using EyewearStore_SWP391.DTOs;
using EyewearStore_SWP391.Models;
using EyewearStore_SWP391.Services;

namespace EyewearStore_SWP391.Pages.Checkout;

/// <summary>
/// Checkout page: displays cart summary, address picker / add-address form,
/// and initiates the Stripe Checkout Session.
/// </summary>
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

    [BindProperty]
    public int SelectedAddressId { get; set; }

    // Fields for inline "add new address" form
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
        return Page();
    }

    /// <summary>
    /// POST handler: creates a pending order and redirects to Stripe Checkout.
    /// </summary>
    public async Task<IActionResult> OnPostCheckoutAsync()
    {
        if (!User.Identity?.IsAuthenticated ?? true)
            return RedirectToPage("/Account/Login");

        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        // If user is adding a new address inline
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

            // If it's the user's first address, make it default
            var hasAddresses = await _context.Addresses.AnyAsync(a => a.UserId == userId);
            if (!hasAddresses)
                newAddr.IsDefault = true;

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
            return Page();
        }

        try
        {
            // Create a pending order from the cart
            var order = await _orderService.CreatePendingOrderAsync(userId, SelectedAddressId);

            // Build line items for Stripe
            var cart = await _cartService.GetCartByUserIdAsync(userId);
            var lineItems = new List<StripeLineItemDto>();

            if (cart != null)
            {
                foreach (var ci in cart.CartItems)
                {
                    decimal unitPrice = ci.Product?.Price ?? 0m;
                    if (ci.Service != null)
                        unitPrice += ci.Service.Price;

                    var itemName = ci.Product?.Name ?? "Product";
                    if (ci.Service != null)
                        itemName += $" + {ci.Service.Name}";

                    // Get product primary image URL
                    string? imageUrl = null;
                    if (ci.Product?.ProductImages != null)
                    {
                        var primaryImg = ci.Product.ProductImages
                            .FirstOrDefault(img => img.IsPrimary && img.IsActive);
                        if (primaryImg != null && !string.IsNullOrEmpty(primaryImg.ImageUrl))
                        {
                            // Make absolute URL for Stripe
                            imageUrl = primaryImg.ImageUrl.StartsWith("http")
                                ? primaryImg.ImageUrl
                                : $"{Request.Scheme}://{Request.Host}{primaryImg.ImageUrl}";
                        }
                    }

                    lineItems.Add(new StripeLineItemDto
                    {
                        ProductName = itemName,
                        // VND is zero-decimal: 1 VND = 1 unit
                        UnitAmountInSmallestUnit = (long)unitPrice,
                        Quantity = ci.Quantity,
                        ImageUrl = imageUrl
                    });
                }
            }

            // Build success/cancel URLs
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var successUrl = $"{baseUrl}/Checkout/Success";
            var cancelUrl = $"{baseUrl}/Checkout/Cancel";

            // Get user email
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value
                         ?? User.FindFirst(ClaimTypes.Name)?.Value
                         ?? "";

            // Create Stripe Checkout Session and redirect
            var stripeUrl = await _stripeService.CreateCheckoutSessionAsync(
                order.OrderId, lineItems, successUrl, cancelUrl, userEmail);

            return Redirect(stripeUrl);
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            Cart = await _cartService.GetCartByUserIdAsync(userId);
            Total = await _cartService.CalculateCartTotalAsync(userId);
            await LoadAddressesAsync(userId);
            return Page();
        }
    }

    public async Task<IActionResult> OnPostSaveAddressAsync([FromBody] SaveAddressRequest request)
    {
        if (request == null)
            return new JsonResult(new { success = false, message = "Invalid request data." });

        if (string.IsNullOrWhiteSpace(request.ReceiverName) ||
            string.IsNullOrWhiteSpace(request.Phone) ||
            string.IsNullOrWhiteSpace(request.AddressLine))
        {
            return new JsonResult(new { success = false, message = "Please fill in all required fields." });
        }

        if (!User.Identity?.IsAuthenticated ?? true)
            return new JsonResult(new { success = false, message = "User not authenticated." });

        try 
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var newAddr = new Address
            {
                UserId = userId,
                ReceiverName = request.ReceiverName,
                Phone = request.Phone,
                AddressLine = request.AddressLine,
                IsDefault = false, // Default is usually explicitly set, or handled by logic below
                CreatedAt = DateTime.UtcNow
            };

            // Check if this is the first address
            var hasAddresses = await _context.Addresses.AnyAsync(a => a.UserId == userId);
            if (!hasAddresses)
                newAddr.IsDefault = true;

            _context.Addresses.Add(newAddr);
            await _context.SaveChangesAsync();

            return new JsonResult(new 
            { 
                success = true, 
                message = "Address saved successfully!",
                address = new 
                {
                    addressId = newAddr.AddressId,
                    receiverName = newAddr.ReceiverName,
                    phone = newAddr.Phone,
                    addressLine = newAddr.AddressLine,
                    isDefault = newAddr.IsDefault
                }
            });
        }
        catch (Exception ex)
        {
            // Log error if logger was available
            return new JsonResult(new { success = false, message = "Error saving address: " + ex.Message });
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

    public class SaveAddressRequest
    {
        public string ReceiverName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string AddressLine { get; set; } = string.Empty;
    }
}
