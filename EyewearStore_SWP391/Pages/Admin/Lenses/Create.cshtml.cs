using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EyewearStore_SWP391.Pages.Admin.Lenses;

/// <summary>
/// Page model for creating a new lens product
/// </summary>
[Authorize(Roles = "admin")]
public class CreateModel : PageModel
{
    /// <summary>
    /// Handles GET request - form submission is handled via JavaScript/API
    /// </summary>
    public void OnGet()
    {
        // Form submission handled via JavaScript calling the API
    }
}
