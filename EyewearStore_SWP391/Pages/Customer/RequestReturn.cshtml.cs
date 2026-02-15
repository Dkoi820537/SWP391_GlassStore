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

            // Check 30-day return window
            if ((DateTime.UtcNow - Order.CreatedAt).TotalDays > 30)
            {
                TempData["ErrorMessage"] = "This order is past the 30-day return window.";
                return RedirectToPage("/Customer/MyOrders");
            }

            // ✅ FIX: KHÔNG FILTER - Hiển thị tất cả items
            // Validation sẽ làm ở OnPost thay vì OnGet
            // Để user thấy form ngay cả khi có return rồi

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int orderId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                return RedirectToPage("/Account/Login");

            // Validation
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

            // Load order
            var order = await _context.Orders
                .Include(o => o.OrderItems!)
                    .ThenInclude(oi => oi.Returns)
                .FirstOrDefaultAsync(o => o.OrderId == orderId && o.UserId == userId);

            if (order == null)
            {
                TempData["ErrorMessage"] = "Order not found!";
                return RedirectToPage("/Customer/MyOrders");
            }

            // ✅ VALIDATION: Chỉ check items được chọn, không filter toàn bộ
            var validatedItemIds = new List<int>();

            foreach (var itemId in SelectedOrderItemIds.Distinct())
            {
                var orderItem = order.OrderItems.FirstOrDefault(oi => oi.OrderItemId == itemId);
                if (orderItem == null) continue;

                // Chỉ skip item nếu có return PENDING hoặc APPROVED
                // Cho phép return lại nếu trước đó bị REJECTED
                var hasPendingReturn = orderItem.Returns
                    .Any(r => r.Status == "Pending" || r.Status == "Approved");

                if (!hasPendingReturn)
                {
                    validatedItemIds.Add(itemId);
                }
            }

            if (!validatedItemIds.Any())
            {
                TempData["ErrorMessage"] = "Selected items already have pending return requests.";
                return await OnGetAsync(orderId);
            }

            try
            {
                // Handle image uploads
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

                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await image.CopyToAsync(stream);
                            }

                            imageUrls.Add($"/uploads/returns/{fileName}");
                        }
                    }
                }

                // Create return requests
                var now = DateTime.UtcNow;

                foreach (var itemId in validatedItemIds)
                {
                    var orderItem = order.OrderItems.First(oi => oi.OrderItemId == itemId);

                    // Get return quantity
                    var quantityKey = $"ReturnQuantities_{itemId}";
                    var quantityStr = Request.Form[quantityKey].FirstOrDefault();
                    int returnQuantity = int.TryParse(quantityStr, out var qty)
                        ? Math.Min(qty, orderItem.Quantity)
                        : orderItem.Quantity;

                    var returnRequest = new Return
                    {
                        OrderItemId = itemId,
                        UserId = userId,
                        Quantity = returnQuantity,
                        ReturnType = ReturnType,
                        ReasonCategory = ReasonCategory,
                        Reason = ReasonCategory,
                        Description = Description,
                        ImageUrls = imageUrls.Any()
                            ? System.Text.Json.JsonSerializer.Serialize(imageUrls)
                            : null,
                        Status = "Pending",
                        RefundAmount = orderItem.UnitPrice * returnQuantity,
                        CreatedAt = now
                    };

                    _context.Returns.Add(returnRequest);
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Return request submitted successfully! We'll review it within 24-48 hours.";
                return RedirectToPage("/Customer/MyOrders");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
                return await OnGetAsync(orderId);
            }
        }
    }
}
