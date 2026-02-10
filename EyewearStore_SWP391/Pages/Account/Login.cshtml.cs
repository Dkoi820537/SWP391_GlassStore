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
using System.Collections.Generic;

namespace EyewearStore_SWP391.Pages.Account
{
    public class LoginViewModel
    {
        [Required, EmailAddress]
        public string Email { get; set; } = null!;

        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = null!;

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
        public LoginViewModel Input { get; set; } = new();

        [TempData]
        public string SuccessMessage { get; set; } = null!;

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            if (!ModelState.IsValid) return Page();

            var user = _context.Users.FirstOrDefault(u => u.Email == Input.Email);
            if (user == null)
            {
                ModelState.AddModelError("", "Invalid email or password");
                return Page();
            }

            if (!user.IsActive)
            {
                ModelState.AddModelError("", "This account is not allowed to log in");
                return Page();
            }

            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, Input.Password);
            if (result == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError("", "Invalid email or password");
                return Page();
            }

            // create claims (get role from user.Role)
            var role = (user.Role ?? "customer").Trim();
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.FullName ?? user.Email),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, role)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            var props = new AuthenticationProperties
            {
                IsPersistent = Input.RememberMe,
                ExpiresUtc = Input.RememberMe
                    ? DateTimeOffset.UtcNow.AddDays(7)
                    : DateTimeOffset.UtcNow.AddHours(8),
                AllowRefresh = true,
                IssuedUtc = DateTimeOffset.UtcNow
            };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, props);

            // redirect by role (lowercase compare)
            var r = role.ToLowerInvariant();
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }

            if (r == "admin")
            {
                return LocalRedirect("/Admin");
            }
            if (r == "sale" || r == "sale staff")
            {
                return LocalRedirect("/Admin/SaleStaff");
            }
            if (r == "operational" || r == "operational staff")
            {
                return LocalRedirect("/Admin/Operational");
            }
            if (r == "staff")
            {
                return LocalRedirect("/Admin/Staff");
            }
            if (r == "manager")
            {
                return LocalRedirect("/Admin/Manager");
            }
            // default for customer
            return LocalRedirect("/Profile/Index");
        }
    }
}