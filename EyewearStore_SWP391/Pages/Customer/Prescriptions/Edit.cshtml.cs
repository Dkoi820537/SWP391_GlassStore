using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using EyewearStore_SWP391.Models;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using System.Security.Claims;
using System;

namespace EyewearStore_SWP391.Pages.Customer.Prescriptions
{
    [Authorize]
    public class EditModel : PageModel
    {
        private readonly EyewearStoreContext _context;

        public EditModel(EyewearStoreContext context)
        {
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public int PrescriptionId { get; set; }
        public DateTime CreatedAt { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Profile name is required")]
            [StringLength(100, ErrorMessage = "Profile name cannot exceed 100 characters")]
            public string ProfileName { get; set; } = "";

            [Range(-20.00, 20.00, ErrorMessage = "Right SPH must be between -20.00 and +20.00 (BR-S003)")]
            public decimal? RightSph { get; set; }

            [Range(-6.00, 6.00, ErrorMessage = "Right CYL must be between -6.00 and +6.00 (BR-S003)")]
            public decimal? RightCyl { get; set; }

            [Range(0, 180, ErrorMessage = "Right AXIS must be between 0 and 180 (BR-S003)")]
            public int? RightAxis { get; set; }

            [Range(-20.00, 20.00, ErrorMessage = "Left SPH must be between -20.00 and +20.00 (BR-S003)")]
            public decimal? LeftSph { get; set; }

            [Range(-6.00, 6.00, ErrorMessage = "Left CYL must be between -6.00 and +6.00 (BR-S003)")]
            public decimal? LeftCyl { get; set; }

            [Range(0, 180, ErrorMessage = "Left AXIS must be between 0 and 180 (BR-S003)")]
            public int? LeftAxis { get; set; }

            public bool IsActive { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var userId = GetCurrentUserId();

            var prescription = await _context.PrescriptionProfiles
                .FirstOrDefaultAsync(p => p.PrescriptionId == id && p.UserId == userId);

            if (prescription == null)
            {
                return NotFound();
            }

            PrescriptionId = prescription.PrescriptionId;
            CreatedAt = prescription.CreatedAt;

            Input = new InputModel
            {
                ProfileName = prescription.ProfileName ?? "",
                RightSph = prescription.RightSph,
                RightCyl = prescription.RightCyl,
                RightAxis = prescription.RightAxis,
                LeftSph = prescription.LeftSph,
                LeftCyl = prescription.LeftCyl,
                LeftAxis = prescription.LeftAxis,
                IsActive = prescription.IsActive
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var userId = GetCurrentUserId();

            var prescription = await _context.PrescriptionProfiles
                .FirstOrDefaultAsync(p => p.PrescriptionId == id && p.UserId == userId);

            if (prescription == null)
            {
                return NotFound();
            }

            // Update prescription
            prescription.ProfileName = Input.ProfileName.Trim();
            prescription.RightSph = Input.RightSph;
            prescription.RightCyl = Input.RightCyl;
            prescription.RightAxis = Input.RightAxis;
            prescription.LeftSph = Input.LeftSph;
            prescription.LeftCyl = Input.LeftCyl;
            prescription.LeftAxis = Input.LeftAxis;
            prescription.IsActive = Input.IsActive;

            _context.PrescriptionProfiles.Update(prescription);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Prescription profile updated successfully!";
            return RedirectToPage("./Index");
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }
    }
}
