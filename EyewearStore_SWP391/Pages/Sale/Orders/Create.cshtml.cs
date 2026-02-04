using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using EyewearStore_SWP391.Models;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System;
using Microsoft.EntityFrameworkCore;

namespace EyewearStore_SWP391.Pages.Sale.Orders
{
    [Authorize(Roles = "sale,admin")]
    public class CreateModel : PageModel
    {
        private readonly EyewearStoreContext _context;

        public CreateModel(EyewearStoreContext context) => _context = context;

        public class InputModel
        {
            [Required(ErrorMessage = "Customer email is required")]
            [EmailAddress(ErrorMessage = "Please enter a valid email address")]
            [Display(Name = "Customer Email")]
            public string Email { get; set; } = "";

            [Required(ErrorMessage = "Shipping address is required")]
            [StringLength(500, MinimumLength = 10, ErrorMessage = "Address must be between 10 and 500 characters")]
            [Display(Name = "Shipping Address")]
            public string Address { get; set; } = "";

            [Required(ErrorMessage = "Payment method is required")]
            [Display(Name = "Payment Method")]
            public string PaymentMethod { get; set; } = "COD";

            [Required(ErrorMessage = "At least one product is required")]
            [Display(Name = "Order Items")]
            public string ItemsRaw { get; set; } = "";
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Validate payment method
            var validPaymentMethods = new[] { "COD", "Online" };
            if (!validPaymentMethods.Contains(Input.PaymentMethod))
            {
                ModelState.AddModelError("Input.PaymentMethod", "Invalid payment method selected");
                return Page();
            }

            // Find or create user
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == Input.Email);

            if (user == null)
            {
                // Create new customer account
                user = new User
                {
                    Email = Input.Email,
                    FullName = Input.Email.Split('@')[0], // Use email prefix as default name
                    Role = "customer",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    PasswordHash = "" // Guest account - no password required
                };
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
            }
            else if (!user.IsActive)
            {
                ModelState.AddModelError("Input.Email", "This customer account is inactive");
                return Page();
            }

            // Parse and validate items
            var lines = Input.ItemsRaw.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var parsed = new List<(int productId, int qty)>();
            var errors = new List<string>();

            foreach (var line in lines)
            {
                var parts = line.Trim().Split(':');
                if (parts.Length != 2)
                {
                    errors.Add($"Invalid format: '{line}' - Expected format: ProductID:Quantity");
                    continue;
                }

                if (!int.TryParse(parts[0].Trim(), out var pid))
                {
                    errors.Add($"Invalid Product ID: '{parts[0]}' - Must be a number");
                    continue;
                }

                if (!int.TryParse(parts[1].Trim(), out var q) || q <= 0)
                {
                    errors.Add($"Invalid quantity for Product {pid}: '{parts[1]}' - Must be positive number");
                    continue;
                }

                parsed.Add((pid, q));
            }

            if (errors.Any())
            {
                foreach (var error in errors)
                {
                    ModelState.AddModelError("Input.ItemsRaw", error);
                }
                return Page();
            }

            if (!parsed.Any())
            {
                ModelState.AddModelError("Input.ItemsRaw", "No valid items provided. Please add at least one product.");
                return Page();
            }

            // Validate products exist and are active
            var productIds = parsed.Select(p => p.productId).Distinct().ToList();
            var products = await _context.Products
                .Where(p => productIds.Contains(p.ProductId) && p.IsActive)
                .ToListAsync();

            var missingProducts = productIds.Except(products.Select(p => p.ProductId)).ToList();
            if (missingProducts.Any())
            {
                ModelState.AddModelError("Input.ItemsRaw",
                    $"The following products are not found or inactive: {string.Join(", ", missingProducts)}");
                return Page();
            }

            // Check inventory availability (BR requirement)
            var inventoryErrors = new List<string>();
            foreach (var (pid, qty) in parsed)
            {
                var product = products.First(p => p.ProductId == pid);
                if (product.InventoryQty.HasValue && product.InventoryQty.Value < qty)
                {
                    inventoryErrors.Add(
                        $"Product {product.Name} (ID: {pid}): Insufficient stock. Available: {product.InventoryQty}, Requested: {qty}");
                }
            }

            if (inventoryErrors.Any())
            {
                foreach (var error in inventoryErrors)
                {
                    ModelState.AddModelError("Input.ItemsRaw", error);
                }
                return Page();
            }

            // Create address record
            var address = new Address
            {
                UserId = user.UserId,
                ReceiverName = user.FullName ?? user.Email,
                Phone = user.Phone ?? "",
                AddressLine = Input.Address,
                IsDefault = false,
                CreatedAt = DateTime.UtcNow
            };
            _context.Addresses.Add(address);
            await _context.SaveChangesAsync();

            // Create order
            var order = new Order
            {
                UserId = user.UserId,
                AddressId = address.AddressId,
                Status = "Pending Confirmation", // BR: Initial status
                PaymentMethod = Input.PaymentMethod,
                CreatedAt = DateTime.UtcNow,
                TotalAmount = 0m
            };
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Create order items and calculate total
            decimal totalAmount = 0m;
            foreach (var (pid, qty) in parsed)
            {
                var product = products.First(p => p.ProductId == pid);

                var orderItem = new OrderItem
                {
                    OrderId = order.OrderId,
                    ProductId = product.ProductId,
                    Quantity = qty,
                    UnitPrice = product.Price,
                    SnapshotJson = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        sku = product.Sku,
                        name = product.Name,
                        price = product.Price,
                        capturedAt = DateTime.UtcNow
                    })
                };
                _context.OrderItems.Add(orderItem);

                totalAmount += orderItem.UnitPrice * orderItem.Quantity;

                // Update inventory (BR requirement)
                if (product.InventoryQty.HasValue)
                {
                    product.InventoryQty -= qty;
                    _context.Products.Update(product);
                }
            }

            // Update order total
            order.TotalAmount = totalAmount;
            _context.Orders.Update(order);

            await _context.SaveChangesAsync();

            // BR: Log order creation (optional - add logging service here if needed)

            TempData["Success"] = $"Order #{order.OrderId} created successfully for {user.Email}. Total: {totalAmount:N0} ₫";
            return RedirectToPage("Details", new { id = order.OrderId });
        }
    }
}
