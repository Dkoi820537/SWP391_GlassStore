using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EyewearStore_SWP391.Pages.Admin.Lenses;

/// <summary>
/// Page model for listing and managing lens products
/// </summary>
[Authorize(Roles = "admin")]
public class IndexModel : PageModel
{
    /// <summary>
    /// Handles GET request - page content is loaded via JavaScript/API
    /// </summary>
    public void OnGet()
    {
        // Data is loaded via JavaScript calling the API
    }
}
