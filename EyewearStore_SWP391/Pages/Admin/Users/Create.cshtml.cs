using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using EyewearStore_SWP391.Models;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using System;

namespace EyewearStore_SWP391.Pages.Admin.Users
{
    /// <summary>
    /// Create New User - Admin creates user accounts
    /// BR: Admin can create users with any role
    /// </summary>
    [Authorize(Roles = "admin,Administrator")]
    public class CreateModel : PageModel
    {
        private readonly EyewearStoreContext _context;

        public CreateModel(EyewearStoreContext context)
        {
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required(ErrorMessage = "Email is required")]
            [EmailAddress(ErrorMessage = "Invalid email address")]
            [Display(Name = "Email")]
            public string Email { get; set; } = "";

            [Required(ErrorMessage = "Full name is required")]
            [StringLength(100, MinimumLength = 2, ErrorMessage = "Full name must be between 2 and 100 characters")]
            [Display(Name = "Full Name")]
            public string FullName { get; set; } = "";

            [Phone(ErrorMessage = "Invalid phone number")]
            [Display(Name = "Phone Number")]
            public string? Phone { get; set; }

            [Required(ErrorMessage = "Password is required")]
            [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; } = "";

            [StringLength(500, ErrorMessage = "Address cannot exceed 500 characters")]
            [Display(Name = "Address")]
            public string? Address { get; set; }

            [Required(ErrorMessage = "Role is required")]
            [Display(Name = "Role")]
            public string Role { get; set; } = "customer";

            [Display(Name = "Active Status")]
            public bool IsActive { get; set; } = true;
        }

        public void OnGet()
        {
            // Initialize with defaults
            Input.IsActive = true;
            Input.Role = "customer";
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Validate role
            var validRoles = new[] { "customer", "sale", "sales", "support", "staff", "operational", "manager", "admin", "Administrator" };
            if (!Array.Exists(validRoles, r => r == Input.Role))
            {
                ModelState.AddModelError("Input.Role", "Invalid role selected");
                return Page();
            }

            // Check if email already exists
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == Input.Email.ToLower());

            if (existingUser != null)
            {
                ModelState.AddModelError("Input.Email", "An account with this email already exists");
                return Page();
            }

            // Create new user
            var user = new User
            {
                Email = Input.Email.Trim(),
                FullName = Input.FullName.Trim(),
                Phone = Input.Phone?.Trim(),
                PasswordHash = HashPassword(Input.Password), // Simple hash for demo
                Role = Input.Role,
                IsActive = Input.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Create default address if provided
            if (!string.IsNullOrWhiteSpace(Input.Address))
            {
                var address = new Address
                {
                    UserId = user.UserId,
                    ReceiverName = user.FullName,
                    Phone = user.Phone ?? "",
                    AddressLine = Input.Address.Trim(),
                    IsDefault = true,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Addresses.Add(address);
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = $"User {user.Email} created successfully with role: {user.Role}";
            return RedirectToPage("./Index");
        }

        /// <summary>
        /// Simple password hashing (use proper hashing like BCrypt in production)
        /// </summary>
        private string HashPassword(string password)
        {
            // TODO: Implement proper password hashing (BCrypt, PBKDF2, or use Identity)
            // For now, just store as-is for demo purposes
            // In production, NEVER store plain text passwords!
            return password; // DEMO ONLY - REPLACE WITH PROPER HASHING!
        }
    }
}
