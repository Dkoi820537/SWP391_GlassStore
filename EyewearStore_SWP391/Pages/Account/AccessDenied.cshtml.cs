using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EyewearStore_SWP391.Pages.Account
{
    public class AccessDeniedModel : PageModel
    {
        private readonly ILogger<AccessDeniedModel> _logger;

        public AccessDeniedModel(ILogger<AccessDeniedModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {
            var user = User.Identity?.Name ?? "Anonymous";
            var returnUrl = HttpContext.Request.Query["ReturnUrl"].FirstOrDefault() ?? "unknown";
            _logger.LogWarning(
                "Access denied for user '{User}' attempting to access '{ReturnUrl}'",
                user, returnUrl);
        }
    }
}
