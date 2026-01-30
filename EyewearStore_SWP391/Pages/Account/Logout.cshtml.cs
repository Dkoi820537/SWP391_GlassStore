using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace EyewearStore_SWP391.Pages.Account
{
    public class LogoutModel : PageModel
    {
        public void OnGet()
        {
            // Hiển thị trang confirm logout (nếu cần)
        }

        public async Task<IActionResult> OnPostAsync()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            // Optional: clear other cookies if you used any
            return RedirectToPage("/Account/Login");
        }
    }
}