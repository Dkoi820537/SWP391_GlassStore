using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EyewearStore_SWP391.Pages
{
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [IgnoreAntiforgeryToken]
    public class StatusCodeModel : PageModel
    {
        public void OnGet(int code)
        {
            // Optionally log the original path that caused the error
            var feature = HttpContext.Features.Get<IStatusCodeReExecuteFeature>();
            var originalPath = feature?.OriginalPath ?? "unknown";

            // You can log here if needed:
            // _logger.LogWarning("Status {Code} for path {Path}", code, originalPath);
        }
    }
}
