using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using EyewearStore_SWP391.Models;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace EyewearStore_SWP391.Pages.Account
{
    public class RegisterViewModel
    {
        [Required, EmailAddress]
        public string Email { get; set; } = null!;

        [Required, MinLength(6)]
        [DataType(DataType.Password)]
        public string Password { get; set; } = null!;

        [Required]
        public string FullName { get; set; } = null!;

        [Phone]
        public string? Phone { get; set; }
    }

    public class RegisterModel : PageModel
    {
        private readonly EyewearStoreContext _context;
        private readonly PasswordHasher<User> _passwordHasher;

        public RegisterModel(EyewearStoreContext context)
        {
            _context = context;
            _passwordHasher = new PasswordHasher<User>();
        }

        [BindProperty]
        public RegisterViewModel Input { get; set; } = new();

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            var exists = _context.Users.FirstOrDefault(u => u.Email == Input.Email);
            if (exists != null)
            {
                ModelState.AddModelError("", "Email đã tồn tại");
                return Page();
            }

            var user = new User
            {
                Email = Input.Email,
                FullName = Input.FullName,
                Phone = Input.Phone,
                Role = "Customer",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            user.PasswordHash = _passwordHasher.HashPassword(user, Input.Password);
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đăng ký thành công. Vui lòng đăng nhập bằng email và mật khẩu vừa tạo.";

            return RedirectToPage("/Account/Login");
        }
    }
}