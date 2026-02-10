using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EyewearStore_SWP391.Pages.Checkout;

/// <summary>
/// Cancel page: shown when the user cancels the Stripe Checkout.
/// </summary>
public class CancelModel : PageModel
{
    public void OnGet()
    {
        // Nothing to load — page is purely informational
    }
}
