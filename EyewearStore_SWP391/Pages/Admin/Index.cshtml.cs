using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EyewearStore_SWP391.Pages.Admin
{
    [Authorize(Roles = "admin")]
    public class IndexModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}
