using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
namespace EyewearStore_SWP391.Pages.Customer.Services
{
    public class EyeExamModel : PageModel
    {
        // Eye exam chỉ cần form đặt lịch — không cần DB query
        public void OnGet() { }
    }
}