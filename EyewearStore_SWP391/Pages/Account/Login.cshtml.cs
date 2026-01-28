using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using EyewearStore_SWP391.Models;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace EyewearStore_SWP391.Pages.Account
{
    public class LoginViewModel
    {
        [Required, EmailAddress]
        public string Email { get; set; }

        [Required, DataType(DataType.Password)]
        public string Password { get; set; }

        public bool RememberMe { get; set; }
    }

    public class LoginModel : PageModel
    {
        private readonly EyewearStoreContext _context;
        private readonly PasswordHasher<User> _passwordHasher;

        public LoginModel(EyewearStoreContext context)
        {
            _context = context;
            _passwordHasher = new PasswordHasher<User>();
        }

        [BindProperty]
        public LoginViewModel Input { get; set; }

        // Nhận message từ TempData
        [TempData]
        public string SuccessMessage { get; set; }

        public void OnGet()
        {
            // SuccessMessage sẽ tự được map từ TempData nếu có
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            var user = _context.Users.FirstOrDefault(u => u.Email == Input.Email);
            if (user == null)
            {
                ModelState.AddModelError("", "Email hoặc mật khẩu không đúng");
                return Page();
            }

            if (user.Status != "Active")
            {
                ModelState.AddModelError("", "Tài khoản không được phép đăng nhập");
                return Page();
            }

            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, Input.Password);
            if (result == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError("", "Email hoặc mật khẩu không đúng");
                return Page();
            }

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.FullName ?? user.Email),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            var props = new AuthenticationProperties
            {
                IsPersistent = Input.RememberMe,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
            };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, props);

            return RedirectToPage("/Index");
        }
    }
}