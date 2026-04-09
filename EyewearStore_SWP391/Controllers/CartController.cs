using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EyewearStore_SWP391.Models;
using EyewearStore_SWP391.Services;

namespace EyewearStore_SWP391.Controllers;

[ApiController]
[Route("api/cart")]
public class CartController : ControllerBase
{
    private readonly ICartService _cartService;
    private readonly EyewearStoreContext _context;

    public CartController(ICartService cartService, EyewearStoreContext context)
    {
        _cartService = cartService;
        _context = context;
    }

    /// <summary>
    /// Returns pre-rendered HTML for the cart dropdown items.
    /// Called via AJAX when the dropdown is opened (lazy loading).
    /// </summary>
    [HttpGet("dropdown-items")]
    public async Task<IActionResult> GetDropdownItems()
    {
        var uidStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(uidStr, out var uid))
            return Content("", "text/html");

        Cart? cart;
        try
        {
            cart = await _cartService.GetCartByUserIdAsync(uid);
        }
        catch
        {
            return Content("<div class='text-center py-2 text-muted small'>Could not load cart items. <a href='/Cart/Index'>View full cart</a></div>", "text/html");
        }

        if (cart == null || !cart.CartItems.Any())
            return Content("<div class='text-center py-2 text-muted small'>Cart is empty</div>", "text/html");

        var items = cart.CartItems.ToList();

        // Load lens products for service orders
        var lensIds = items
            .Select(ci => CartService.ExtractLensProductId(ci.TempPrescriptionJson))
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        Dictionary<int, Product> lensProducts = new();
        if (lensIds.Any())
        {
            lensProducts = await _context.Products
                .AsNoTracking()
                .Where(p => lensIds.Contains(p.ProductId))
                .ToDictionaryAsync(p => p.ProductId);
        }

        // Build HTML
        var sb = new StringBuilder();
        foreach (var it in items)
        {
            var lensId = CartService.ExtractLensProductId(it.TempPrescriptionJson);
            bool isServiceOrder = lensId.HasValue;

            decimal unit = it.Product?.Price ?? 0m;

            Product? lensProduct = null;
            if (isServiceOrder && lensId.HasValue && lensProducts.TryGetValue(lensId.Value, out var lp))
            {
                lensProduct = lp;
                unit += lp.Price;
            }
            if (it.Service != null)
                unit += it.Service.Price;

            decimal lineTotal = unit * it.Quantity;
            string itemName = Encode(it.Product?.Name ?? it.Service?.Name ?? "Product");
            var img = it.Product?.ProductImages?.OrderByDescending(x => x.IsPrimary).FirstOrDefault()?.ImageUrl;

            sb.Append($@"<div class=""cart-item-row mb-2""
                 data-cart-item-id=""{it.CartItemId}""
                 data-unit-price=""{unit}""
                 style=""transition:opacity 0.25s ease,max-height 0.3s ease; display:flex; gap:10px; align-items:flex-start; padding:6px 0; border-bottom:1px solid #f0f0f0;"">");

            // Image
            sb.Append(@"<div style=""width:50px; height:50px; background:#f5f5f5; border-radius:6px; display:flex; align-items:center; justify-content:center; font-size:11px; color:#999; overflow:hidden; flex-shrink:0;"">");
            if (!string.IsNullOrEmpty(img))
                sb.Append($@"<img src=""{Encode(img)}"" alt=""Product"" style=""width:100%;height:100%;object-fit:cover;"" />");
            else
                sb.Append($"<span>{(isServiceOrder ? "🔧" : "IMG")}</span>");
            sb.Append("</div>");

            // Info + Controls
            sb.Append(@"<div class=""flex-grow-1 min-w-0"" style=""display:flex; flex-direction:column; gap:2px;"">");

            // Name + price + remove
            sb.Append(@"<div style=""display:flex; align-items:center; gap:6px;"">");
            sb.Append($@"<div class=""fw-semibold text-truncate"" style=""flex:1; font-size:13px;"">{itemName}");
            if (isServiceOrder)
                sb.Append(@"<span style=""font-size:10px; background:#f0f0ff; color:#5b5bdb; border-radius:4px; padding:1px 5px; font-weight:600; margin-left:3px;"">Gia công</span>");
            sb.Append("</div>");

            sb.Append($@"<div class=""fw-bold flex-shrink-0 cart-item-line-total"" style=""font-size:13px; white-space:nowrap;"">{lineTotal:N0} VND</div>");

            sb.Append($@"<button type=""button"" class=""btn-cart-remove btn btn-sm p-0""
                style=""width:24px; height:24px; display:flex; align-items:center; justify-content:center; border-radius:4px; color:#9CA3AF; border:1px solid #e5e7eb; background:transparent; outline:none; box-shadow:none; flex-shrink:0;""
                data-cart-item-id=""{it.CartItemId}"" aria-label=""Remove {itemName} from cart"" title=""Remove"">
                <svg xmlns=""http://www.w3.org/2000/svg"" width=""13"" height=""13"" fill=""none"" viewBox=""0 0 24 24"" stroke=""currentColor"" stroke-width=""2"">
                    <polyline points=""3 6 5 6 21 6""></polyline><path d=""M19 6l-1 14H6L5 6""></path><path d=""M10 11v6M14 11v6""></path><path d=""M9 6V4h6v2""></path>
                </svg>
            </button>");
            sb.Append("</div>");

            // Price breakdown (service orders)
            if (isServiceOrder)
            {
                sb.Append($@"<div style=""font-size:11px; color:#6B7280; background:#f8f9fc; border-radius:4px; padding:3px 6px; margin-top:1px; line-height:1.8;"">🕶 {(it.Product?.Price ?? 0m):N0}");
                if (lensProduct != null)
                    sb.Append($" · ⭕ {Encode(lensProduct.Name)}: {lensProduct.Price:N0}");
                if (it.Service != null)
                    sb.Append($" · ⚙️ {Encode(it.Service.Name)}: {it.Service.Price:N0}");
                sb.Append("</div>");
            }

            // Prescription label
            if (!isServiceOrder && it.Product?.ProductType == "Lens")
            {
                var rxName = Encode(it.Prescription?.ProfileName ?? "No prescription");
                sb.Append($@"<div class=""text-muted text-truncate"" style=""font-size:12px; color:#6B7280;"" title=""{rxName}"">{rxName}</div>");
            }

            // Unit price
            sb.Append($@"<div class=""small text-muted"" style=""font-size:11px;"">{unit:N0} VND each</div>");

            // Quantity stepper
            var disabledStyle = it.Quantity <= 1 ? "opacity:0.4; cursor:not-allowed;" : "";
            var disabledAttr = it.Quantity <= 1 ? "disabled" : "";
            sb.Append($@"<div style=""display:flex; align-items:center; margin-top:2px;"">
                <div class=""cart-qty-group"" style=""display:inline-flex; border:1px solid #D1D5DB; border-radius:6px; overflow:hidden; height:26px;"">
                    <button type=""button"" class=""btn-cart-qty-minus""
                        style=""width:26px; height:100%; display:flex; align-items:center; justify-content:center; border:none; background:#fff; color:#6B7280; cursor:pointer; font-size:14px; font-weight:600; transition:background .15s; outline:none; {disabledStyle}""
                        data-cart-item-id=""{it.CartItemId}"" {disabledAttr} aria-label=""Decrease quantity of {itemName}"">&minus;</button>
                    <span class=""cart-qty-display""
                        style=""width:34px; height:100%; display:flex; align-items:center; justify-content:center; background:#F3F4F6; font-size:13px; font-weight:700; color:#374151; border-left:1px solid #D1D5DB; border-right:1px solid #D1D5DB; user-select:none;""
                        aria-label=""Quantity: {it.Quantity}"">{it.Quantity}</span>
                    <button type=""button"" class=""btn-cart-qty-plus""
                        style=""width:26px; height:100%; display:flex; align-items:center; justify-content:center; border:none; background:#fff; color:#6B7280; cursor:pointer; font-size:14px; font-weight:600; transition:background .15s; outline:none;""
                        data-cart-item-id=""{it.CartItemId}"" aria-label=""Increase quantity of {itemName}"">+</button>
                </div>
            </div>");

            sb.Append("</div>"); // info
            sb.Append("</div>"); // cart-item-row
        }

        return Content(sb.ToString(), "text/html");
    }

    private static string Encode(string? s) =>
        System.Net.WebUtility.HtmlEncode(s ?? "");
}
