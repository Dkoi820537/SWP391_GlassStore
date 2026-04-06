using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
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

        // LensProducts dùng để hiển thị tên lens trong checkout page
        public Dictionary<int, Product> LensProducts { get; set; } = new();

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

            // Load lens products để hiển thị tên trên checkout page
            await LoadLensProductsAsync(Cart);
            await LoadAddressesAsync(userId);

            return Page();
        }

        public async Task<IActionResult> OnPostCheckoutAsync()
        {
            if (!User.Identity?.IsAuthenticated ?? true)
                return RedirectToPage("/Account/Login");

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            // STEP 1: Get or create address
            Address? selectedAddress = null;

            if (SelectedAddressId == 0 &&
                !string.IsNullOrWhiteSpace(NewReceiverName) &&
                !string.IsNullOrWhiteSpace(NewPhone) &&
                !string.IsNullOrWhiteSpace(NewAddressLine))
            {
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
                selectedAddress = await _context.Addresses
                    .AsNoTracking()
                    .FirstOrDefaultAsync(a => a.AddressId == SelectedAddressId && a.UserId == userId);
            }

            if (selectedAddress == null)
            {
                TempData["ErrorMessage"] = "Please select or add a shipping address.";
                Cart = await _cartService.GetCartByUserIdAsync(userId);
                var (sb, pf, gt) = await _cartService.GetCartTotalsBreakdownAsync(userId);
                SubtotalBase = sb; PrescriptionFeesTotal = pf; Total = gt;
                await LoadLensProductsAsync(Cart);
                await LoadAddressesAsync(userId);
                return Page();
            }

            try
            {
                var cart = await _cartService.GetCartByUserIdAsync(userId);
                var (subtotalBase, prescriptionFeesTotal, total) = await _cartService.GetCartTotalsBreakdownAsync(userId);

                if (cart == null || !cart.CartItems.Any())
                {
                    TempData["ErrorMessage"] = "Your cart is empty.";
                    return RedirectToPage("/Cart/Index");
                }

                // Load lens products for price calculation
                var lensIds = cart.CartItems
                    .Select(ci => CartService.ExtractLensProductId(ci.TempPrescriptionJson))
                    .Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();

                var lensProducts = lensIds.Any()
                    ? await _context.Products
                        .Where(p => lensIds.Contains(p.ProductId))
                        .ToDictionaryAsync(p => p.ProductId)
                    : new Dictionary<int, Product>();

                // ── STEP: Group cart items by type ────────────────────────────
                var customItems = new List<CartItem>();
                var standardItems = new List<CartItem>();

                foreach (var ci in cart.CartItems)
                {
                    var extractedLensId = CartService.ExtractLensProductId(ci.TempPrescriptionJson);
                    if (extractedLensId.HasValue)
                        customItems.Add(ci);
                    else
                        standardItems.Add(ci);
                }

                // Generate a shared group ID if we have both types
                bool hasBothTypes = customItems.Any() && standardItems.Any();
                string? orderGroupId = hasBothTypes ? Guid.NewGuid().ToString("N") : null;

                var createdOrders = new List<Order>();

                // ── Helper: create an order for a group of items ─────────────
                async Task<Order> CreateOrderForGroup(
                    List<CartItem> items, string orderType)
                {
                    // Calculate group total
                    decimal groupTotal = 0m;
                    foreach (var ci in items)
                    {
                        var lid = CartService.ExtractLensProductId(ci.TempPrescriptionJson);
                        decimal up = ci.Product?.Price ?? 0m;
                        if (lid.HasValue && lensProducts.TryGetValue(lid.Value, out var lp))
                            up += lp.Price;
                        if (ci.Service != null) up += ci.Service.Price;
                        groupTotal += (up + ci.PrescriptionFee) * ci.Quantity;
                    }

                    var order = new Order
                    {
                        UserId = userId,
                        ReceiverName = selectedAddress.ReceiverName,
                        Phone = selectedAddress.Phone,
                        AddressLine = selectedAddress.AddressLine,
                        AddressId = selectedAddress.AddressId,
                        TotalAmount = groupTotal,
                        Status = PaymentMethod == "COD" ? "Pending Confirmation" : "Pending",
                        PaymentMethod = PaymentMethod == "COD" ? "COD" : "Stripe",
                        OrderType = orderType,
                        OrderGroupId = orderGroupId,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.Orders.Add(order);
                    await _context.SaveChangesAsync();

                    // Create order items
                    foreach (var cartItem in items)
                    {
                        var lid = CartService.ExtractLensProductId(cartItem.TempPrescriptionJson);
                        bool isServiceOrder = lid.HasValue;

                        decimal unitPrice = cartItem.Product?.Price ?? 0m;

                        Product? lensProduct = null;
                        if (isServiceOrder && lid.HasValue && lensProducts.TryGetValue(lid.Value, out var lp))
                        {
                            lensProduct = lp;
                            unitPrice += lp.Price;
                        }

                        if (cartItem.Service != null)
                            unitPrice += cartItem.Service.Price;

                        string? snapshotJson = null;
                        if (isServiceOrder)
                        {
                            snapshotJson = JsonSerializer.Serialize(new
                            {
                                isServiceOrder = true,
                                lensProductId = lid!.Value,
                                lensProductName = lensProduct?.Name ?? "",
                                lensPrice = lensProduct?.Price ?? 0m,
                                serviceId = cartItem.ServiceId,
                                serviceName = cartItem.Service?.Name ?? "",
                                servicePrice = cartItem.Service?.Price ?? 0m,
                                framePrice = cartItem.Product?.Price ?? 0m,
                                frameName = cartItem.Product?.Name ?? ""
                            });
                        }

                        var orderItem = new OrderItem
                        {
                            OrderId = order.OrderId,
                            ProductId = cartItem.ProductId,
                            PrescriptionId = cartItem.PrescriptionId,
                            PrescriptionFee = cartItem.PrescriptionFee,
                            Quantity = cartItem.Quantity,
                            UnitPrice = unitPrice,
                            IsBundle = false,
                            SnapshotJson = snapshotJson
                        };

                        _context.OrderItems.Add(orderItem);
                    }

                    await _context.SaveChangesAsync();
                    return order;
                }

                // ── Create order(s) ──────────────────────────────────────────
                if (standardItems.Any())
                    createdOrders.Add(await CreateOrderForGroup(standardItems, "Standard"));

                if (customItems.Any())
                    createdOrders.Add(await CreateOrderForGroup(customItems, "Custom"));

                // ── COD PATH ─────────────────────────────────────────────────
                if (PaymentMethod == "COD")
                {
                    foreach (var ord in createdOrders)
                    {
                        // Reload order items for inventory deduction
                        var orderWithItems = await _context.Orders
                            .Include(o => o.OrderItems)
                            .FirstAsync(o => o.OrderId == ord.OrderId);

                        foreach (var oi in orderWithItems.OrderItems)
                        {
                            var product = await _context.Products.FindAsync(oi.ProductId);
                            if (product != null && product.InventoryQty.HasValue)
                            {
                                product.InventoryQty -= oi.Quantity;
                                if (product.InventoryQty < 0) product.InventoryQty = 0;
                            }
                        }
                    }
                    await _context.SaveChangesAsync();
                    await _cartService.ClearCartAsync(userId);

                    var orderIds = string.Join(",", createdOrders.Select(o => o.OrderId));
                    return RedirectToPage("/Checkout/Success", new { order_ids = orderIds });
                }

                // ── STRIPE PATH ──────────────────────────────────────────────
                var lineItems = new List<StripeLineItemDto>();

                foreach (var ci in cart.CartItems)
                {
                    var lensId = CartService.ExtractLensProductId(ci.TempPrescriptionJson);
                    decimal unitPrice = ci.Product?.Price ?? 0m;
                    if (lensId.HasValue && lensProducts.TryGetValue(lensId.Value, out var lp2))
                        unitPrice += lp2.Price;
                    if (ci.Service != null) unitPrice += ci.Service.Price;
                    decimal unitTotal = unitPrice + ci.PrescriptionFee;

                    var itemName = ci.Product?.Name ?? "Product";
                    if (lensId.HasValue && lensProducts.TryGetValue(lensId.Value, out var lp3))
                        itemName += $" + {lp3.Name}";
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

                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                var successUrl = $"{baseUrl}/Checkout/Success";
                var cancelUrl = $"{baseUrl}/Checkout/Cancel";

                var userEmail = User.FindFirst(ClaimTypes.Email)?.Value
                             ?? User.FindFirst(ClaimTypes.Name)?.Value
                             ?? "";

                var allOrderIds = createdOrders.Select(o => o.OrderId).ToList();
                var stripeUrl = await _stripeService.CreateCheckoutSessionAsync(
                    allOrderIds, lineItems, successUrl, cancelUrl, userEmail);

                return Redirect(stripeUrl);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                Cart = await _cartService.GetCartByUserIdAsync(userId);
                var (sb, pf, gt) = await _cartService.GetCartTotalsBreakdownAsync(userId);
                SubtotalBase = sb; PrescriptionFeesTotal = pf; Total = gt;
                await LoadLensProductsAsync(Cart);
                await LoadAddressesAsync(userId);
                return Page();
            }
        }

        private async Task LoadLensProductsAsync(Models.Cart? cart)
        {
            if (cart == null) return;
            var lensIds = cart.CartItems
                .Select(ci => CartService.ExtractLensProductId(ci.TempPrescriptionJson))
                .Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();
            if (lensIds.Any())
            {
                LensProducts = await _context.Products
                    .Where(p => lensIds.Contains(p.ProductId))
                    .ToDictionaryAsync(p => p.ProductId);
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
