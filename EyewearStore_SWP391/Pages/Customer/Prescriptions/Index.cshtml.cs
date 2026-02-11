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
    /// Added: Server-side pagination (PageNumber, PageSize)
    /// </summary>
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly EyewearStoreContext _context;

        public IndexModel(EyewearStoreContext context)
        {
            _context = context;
        }

        // Paged result
        public List<PrescriptionProfile> Prescriptions { get; set; } = new();

        // Pagination controls (bind from querystring)
        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 6; // default page size

        public int TotalItems { get; set; }
        public int TotalPages { get; set; }

        // Options user can choose (UI)
        public int[] PageSizeOptions { get; } = new[] { 6, 12, 24 };

        public async Task OnGetAsync()
        {
            var userId = GetCurrentUserId();

            if (userId == 0)
            {
                // should not happen due to [Authorize], but guard anyway
                Prescriptions = new List<PrescriptionProfile>();
                TotalItems = 0;
                TotalPages = 0;
                return;
            }

            if (PageSize <= 0) PageSize = 6;
            if (PageNumber <= 0) PageNumber = 1;

            // Count total items for pagination
            TotalItems = await _context.PrescriptionProfiles
                .Where(p => p.UserId == userId)
                .CountAsync();

            TotalPages = (int)Math.Ceiling(TotalItems / (double)PageSize);
            if (TotalPages == 0) TotalPages = 1; // at least 1 page

            // Ensure PageNumber within range
            if (PageNumber > TotalPages) PageNumber = TotalPages;

            var skip = (PageNumber - 1) * PageSize;

            Prescriptions = await _context.PrescriptionProfiles
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .Skip(skip)
                .Take(PageSize)
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
                return RedirectToPage(new { pageNumber = PageNumber, pageSize = PageSize });
            }

            // Check if prescription is used in any orders
            var isUsed = await _context.OrderItems
                .AnyAsync(oi => oi.PrescriptionId == prescriptionId);

            if (isUsed)
            {
                TempData["Error"] = "Cannot delete prescription that has been used in orders!";
                return RedirectToPage(new { pageNumber = PageNumber, pageSize = PageSize });
            }

            // Count before deletion to adjust page if needed
            var totalBefore = await _context.PrescriptionProfiles
                .Where(p => p.UserId == userId)
                .CountAsync();

            _context.PrescriptionProfiles.Remove(prescription);
            await _context.SaveChangesAsync();

            // After deletion, if current page becomes empty and it's not the first page, step back one page
            var totalAfter = totalBefore - 1;
            var maxPageAfter = (int)Math.Ceiling(totalAfter / (double)PageSize);
            if (maxPageAfter < 1) maxPageAfter = 1;

            if (PageNumber > maxPageAfter)
            {
                PageNumber = maxPageAfter;
            }

            TempData["Success"] = "Prescription profile deleted successfully!";
            return RedirectToPage(new { pageNumber = PageNumber, pageSize = PageSize });
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }
    }
}