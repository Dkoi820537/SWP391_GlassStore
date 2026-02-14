using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using EyewearStore_SWP391.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace EyewearStore_SWP391.Pages.Customer
{
    [Authorize(Roles = "customer,Customer")]
    public class RequestReturnModel : PageModel
    {
        private readonly EyewearStoreContext _context;
        private readonly IWebHostEnvironment _environment;

        public RequestReturnModel(EyewearStoreContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public Order? Order { get; set; }

        // View model per order item for UI
        public class ReturnableItemVm
        {
            public int OrderItemId { get; set; }
            public string ProductName { get; set; } = "";
            public string Sku { get; set; } = "";
            public int Quantity { get; set; }
            public decimal UnitPrice { get; set; }
            public bool IsReturnable { get; set; } = true;
            public string? ExistingReturnStatus { get; set; } = null;
        }

        // Items to render in UI
        public List<ReturnableItemVm> DisplayItems { get; set; } = new();

        [BindProperty]
        public List<int> SelectedOrderItemIds { get; set; } = new();

        [BindProperty]
        public string ReturnType { get; set; } = "Refund";

        [BindProperty]
        public string? ReasonCategory { get; set; }

        [BindProperty]
        public string? Description { get; set; }

        [BindProperty]
        public List<IFormFile>? Images { get; set; }

        public async Task<IActionResult> OnGetAsync(int orderId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                return RedirectToPage("/Account/Login");

            // load order + items + products + returns
            Order = await _context.Orders
                .Include(o => o.OrderItems!)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.OrderItems!)
                    .ThenInclude(oi => oi.Returns)
                .FirstOrDefaultAsync(o => o.OrderId == orderId && o.UserId == userId);

            if (Order == null)
            {
                TempData["ErrorMessage"] = "Order not found!";
                return RedirectToPage("/Customer/MyOrders");
            }

            // Check return window
            if ((DateTime.UtcNow - Order.CreatedAt).TotalDays > 30)
            {
                TempData["ErrorMessage"] = "This order is past the 30-day return window.";
                return RedirectToPage("/Customer/MyOrders");
            }

            // Build DisplayItems: keep everything but mark non-returnable items
            DisplayItems = Order.OrderItems
                .Select(oi =>
                {
                    // find any active return (status not Rejected and not Cancelled)
                    var active = oi.Returns
                        .FirstOrDefault(r => !string.Equals(r.Status, "Rejected", StringComparison.OrdinalIgnoreCase)
                                          && !string.Equals(r.Status, "Cancelled", StringComparison.OrdinalIgnoreCase));

                    return new ReturnableItemVm
                    {
                        OrderItemId = oi.OrderItemId,
                        ProductName = oi.Product?.Name ?? "(Product removed)",
                        Sku = oi.Product?.Sku ?? "",
                        Quantity = oi.Quantity,
                        UnitPrice = oi.UnitPrice,
                        IsReturnable = active == null,
                        ExistingReturnStatus = active?.Status
                    };
                })
                .ToList();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int orderId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                return RedirectToPage("/Account/Login");

            // server-side validation: at least one selected
            if (SelectedOrderItemIds == null || !SelectedOrderItemIds.Any())
            {
                TempData["ErrorMessage"] = "Please select at least one item to return.";
                return await OnGetAsync(orderId);
            }
            if (string.IsNullOrWhiteSpace(ReasonCategory))
            {
                TempData["ErrorMessage"] = "Please select a reason for return.";
                return await OnGetAsync(orderId);
            }

            // Load order + items + returns to validate selection
            var order = await _context.Orders
                .Include(o => o.OrderItems!)
                    .ThenInclude(oi => oi.Returns)
                .FirstOrDefaultAsync(o => o.OrderId == orderId && o.UserId == userId);

            if (order == null)
            {
                TempData["ErrorMessage"] = "Order not found!";
                return RedirectToPage("/Customer/MyOrders");
            }

            // Re-validate selections (skip any item that already has active return)
            var validatedSelected = new List<int>();
            foreach (var id in SelectedOrderItemIds.Distinct())
            {
                var oi = order.OrderItems.FirstOrDefault(x => x.OrderItemId == id);
                if (oi == null) continue;

                var hasActiveReturn = oi.Returns.Any(r => !string.Equals(r.Status, "Rejected", StringComparison.OrdinalIgnoreCase)
                                                        && !string.Equals(r.Status, "Cancelled", StringComparison.OrdinalIgnoreCase));
                if (!hasActiveReturn)
                    validatedSelected.Add(id);
            }

            if (!validatedSelected.Any())
            {
                TempData["ErrorMessage"] = "Selected items cannot be returned (maybe a return was already created).";
                return await OnGetAsync(orderId);
            }

            // handle images upload
            List<string> imageUrls = new();
            if (Images != null && Images.Any())
            {
                var uploadsFolder = Path.Combine(_environment.WebRootPath ?? ".", "uploads", "returns");
                Directory.CreateDirectory(uploadsFolder);

                foreach (var image in Images.Take(5))
                {
                    if (image.Length > 0 && image.Length <= 5 * 1024 * 1024)
                    {
                        var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(image.FileName)}";
                        var filePath = Path.Combine(uploadsFolder, fileName);
                        using var stream = new FileStream(filePath, FileMode.Create);
                        await image.CopyToAsync(stream);
                        imageUrls.Add($"/uploads/returns/{fileName}");
                    }
                }
            }

            // create Return records
            var now = DateTime.UtcNow;
            foreach (var orderItemId in validatedSelected)
            {
                var oi = order.OrderItems.FirstOrDefault(x => x.OrderItemId == orderItemId);
                if (oi == null) continue;

                // read return quantity from posted form
                var quantityKey = $"ReturnQuantities_{orderItemId}";
                var quantityStr = Request.Form[quantityKey].FirstOrDefault();
                int returnQuantity = int.TryParse(quantityStr, out var q) ? Math.Min(q, oi.Quantity) : oi.Quantity;

                var returnRequest = new Return
                {
                    OrderItemId = orderItemId,
                    UserId = userId,
                    Quantity = returnQuantity,
                    ReturnType = ReturnType,
                    ReasonCategory = ReasonCategory,
                    Reason = ReasonCategory,
                    Description = Description,
                    ImageUrls = imageUrls.Any() ? System.Text.Json.JsonSerializer.Serialize(imageUrls) : null,
                    Status = "Pending",
                    RefundAmount = oi.UnitPrice * returnQuantity,
                    CreatedAt = now
                };

                _context.Returns.Add(returnRequest);
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Return request submitted successfully! We'll review it within 24-48 hours.";
            return RedirectToPage("/Customer/MyOrders");
        }
    }
}