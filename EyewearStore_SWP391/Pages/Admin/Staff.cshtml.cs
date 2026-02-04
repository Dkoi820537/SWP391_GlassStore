using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EyewearStore_SWP391.Pages.Admin
{
    [Authorize(Roles = "staff,admin")]
    public class StaffModel : PageModel
    {
        public void OnGet() { }
    }
}