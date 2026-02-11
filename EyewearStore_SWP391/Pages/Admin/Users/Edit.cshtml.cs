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
    [Authorize(Roles = "admin,Administrator")]
    public class EditModel : PageModel
    {
        private readonly EyewearStoreContext _context;

        public EditModel(EyewearStoreContext context)
        {
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public int UserId { get; set; }
        public string CurrentRole { get; set; } = "";
        public DateTime CreatedAt { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; } = "";

            [Required]
            [StringLength(100, MinimumLength = 2)]
            public string FullName { get; set; } = "";

            [Phone]
            public string? Phone { get; set; }

            [StringLength(100, MinimumLength = 6)]
            public string? NewPassword { get; set; }

            [Required]
            public string Role { get; set; } = "";

            public bool IsActive { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            UserId = user.UserId;
            CurrentRole = user.Role;
            CreatedAt = user.CreatedAt;

            Input = new InputModel
            {
                Email = user.Email,
                FullName = user.FullName ?? "",
                Phone = user.Phone,
                Role = user.Role,
                IsActive = user.IsActive
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            if (!ModelState.IsValid) return Page();

            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            // Check email uniqueness (excluding current user)
            var emailExists = await _context.Users
                .AnyAsync(u => u.Email.ToLower() == Input.Email.ToLower() && u.UserId != id);

            if (emailExists)
            {
                ModelState.AddModelError("Input.Email", "Email already in use by another user");
                return Page();
            }

            // Update user
            user.Email = Input.Email.Trim();
            user.FullName = Input.FullName.Trim();
            user.Phone = Input.Phone?.Trim();
            user.Role = Input.Role;
            user.IsActive = Input.IsActive;

            // Update password if provided
            if (!string.IsNullOrWhiteSpace(Input.NewPassword))
            {
                user.PasswordHash = Input.NewPassword; // TODO: Hash properly
            }

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"User {user.Email} updated successfully!";
            return RedirectToPage("./Index");
        }
    }
}
