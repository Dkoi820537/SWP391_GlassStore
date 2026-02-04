using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EyewearStore_SWP391.Pages.Admin
{
    [Authorize(Roles = "manager,admin")]
    public class ManagerModel : PageModel
    {
        public void OnGet() { }
    }
}