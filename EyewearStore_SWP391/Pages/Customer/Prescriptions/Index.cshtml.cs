using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using EyewearStore_SWP391.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using System;

namespace EyewearStore_SWP391.Pages.Customer.Prescriptions
{
    /// <summary>
    /// User manages their own prescription profiles
    /// BR: Users can CRUD their own prescriptions
    /// </summary>
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly EyewearStoreContext _context;

        public IndexModel(EyewearStoreContext context)
        {
            _context = context;
        }

        public List<PrescriptionProfile> Prescriptions { get; set; } = new();

        public async Task OnGetAsync()
        {
            var userId = GetCurrentUserId();

            Prescriptions = await _context.PrescriptionProfiles
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int prescriptionId)
        {
            var userId = GetCurrentUserId();

            var prescription = await _context.PrescriptionProfiles
                .FirstOrDefaultAsync(p => p.PrescriptionId == prescriptionId && p.UserId == userId);

            if (prescription == null)
            {
                TempData["Error"] = "Prescription not found or you don't have permission to delete it!";
                return RedirectToPage();
            }

            // Check if prescription is used in any orders
            var isUsed = await _context.OrderItems
                .AnyAsync(oi => oi.PrescriptionId == prescriptionId);

            if (isUsed)
            {
                TempData["Error"] = "Cannot delete prescription that has been used in orders!";
                return RedirectToPage();
            }

            _context.PrescriptionProfiles.Remove(prescription);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Prescription profile deleted successfully!";
            return RedirectToPage();
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }
    }
}
