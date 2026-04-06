using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EyewearStore_SWP391.Pages
{
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [IgnoreAntiforgeryToken]
    public class StatusCodeModel : PageModel
    {
        private readonly ILogger<StatusCodeModel> _logger;

        public int StatusCodeValue { get; set; }
        public string OriginalPath { get; set; } = string.Empty;

        public StatusCodeModel(ILogger<StatusCodeModel> logger)
        {
            _logger = logger;
        }

        public void OnGet(int code)
        {
            StatusCodeValue = code;

            var feature = HttpContext.Features.Get<IStatusCodeReExecuteFeature>();
            OriginalPath = feature?.OriginalPath ?? "unknown";

            var user = User.Identity?.Name ?? "Anonymous";

            switch (code)
            {
                case 401:
                    _logger.LogWarning(
                        "HTTP 401 Unauthorized — User: '{User}', Path: '{Path}'",
                        user, OriginalPath);
                    break;
                case 403:
                    _logger.LogWarning(
                        "HTTP 403 Forbidden — User: '{User}', Path: '{Path}'",
                        user, OriginalPath);
                    break;
                case 404:
                    _logger.LogInformation(
                        "HTTP 404 Not Found — Path: '{Path}'",
                        OriginalPath);
                    break;
                default:
                    _logger.LogWarning(
                        "HTTP {Code} — User: '{User}', Path: '{Path}'",
                        code, user, OriginalPath);
                    break;
            }
        }
    }
}
