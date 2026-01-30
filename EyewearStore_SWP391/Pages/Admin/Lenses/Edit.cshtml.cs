using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EyewearStore_SWP391.Pages.Admin.Lenses;

/// <summary>
/// Page model for editing an existing lens product
/// </summary>
[Authorize(Roles = "admin")]
public class EditModel : PageModel
{
    /// <summary>
    /// The lens ID from query parameter
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    /// <summary>
    /// Lens ID to pass to the view
    /// </summary>
    public int LensId => Id;

    /// <summary>
    /// Handles GET request - form data is loaded via JavaScript/API
    /// </summary>
    public IActionResult OnGet()
    {
        if (Id <= 0)
        {
            return RedirectToPage("Index");
        }

        return Page();
    }
}
