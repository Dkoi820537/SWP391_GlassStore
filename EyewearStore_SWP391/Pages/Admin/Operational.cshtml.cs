using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EyewearStore_SWP391.Pages.Admin
{
    [Authorize(Roles = "operational staff,operational,admin")]
    public class OperationalModel : PageModel
    {
        public void OnGet() { }
    }
}