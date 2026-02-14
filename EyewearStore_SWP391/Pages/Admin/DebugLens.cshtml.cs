using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EyewearStore_SWP391.Models;
using Microsoft.AspNetCore.Authorization;

namespace EyewearStore_SWP391.Pages.Admin
{
    // Restrict to Admin only for security
    [Authorize(Roles = "Admin")] 
    public class DebugLensModel : PageModel
    {
        private readonly EyewearStoreContext _context;

        public DebugLensModel(EyewearStoreContext context)
        {
            _context = context;
        }

        public List<Lens> Lenses { get; set; } = new List<Lens>();

        public async Task OnGetAsync()
        {
            // Fetch all lenses explicitly joining with Product table via inheritance
            Lenses = await _context.Lenses
                .OrderBy(l => l.Name)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostTogglePrescriptionAsync(int productId)
        {
            var lens = await _context.Lenses.FindAsync(productId);
            if (lens != null)
            {
                lens.IsPrescription = !lens.IsPrescription;
                // Ensure fee is set if enabling prescription
                if (lens.IsPrescription && (lens.PrescriptionFee == null || lens.PrescriptionFee == 0))
                {
                    lens.PrescriptionFee = 500000;
                }
                
                await _context.SaveChangesAsync();
            }
            return RedirectToPage();
        }
    }
}
