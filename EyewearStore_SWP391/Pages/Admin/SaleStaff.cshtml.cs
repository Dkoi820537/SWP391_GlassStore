using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EyewearStore_SWP391.Pages.Admin
{
  
    [Authorize(Roles = "sale,sale staff,admin")]
    public class SaleStaffModel : PageModel
    {
        public void OnGet() { }
    }
}