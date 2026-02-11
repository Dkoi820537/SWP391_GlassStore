using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using EyewearStore_SWP391.Models;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using System.Security.Claims;
using System;

namespace EyewearStore_SWP391.Pages.Customer.Prescriptions
{
    [Authorize]
    public class CreateModel : PageModel
    {
        private readonly EyewearStoreContext _context;

        public CreateModel(EyewearStoreContext context)
        {
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

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

            public bool IsActive { get; set; } = true;
        }

        public void OnGet()
        {
            Input.IsActive = true;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var userId = GetCurrentUserId();

            var prescription = new PrescriptionProfile
            {
                UserId = userId,
                ProfileName = Input.ProfileName.Trim(),
                RightSph = Input.RightSph,
                RightCyl = Input.RightCyl,
                RightAxis = Input.RightAxis,
                LeftSph = Input.LeftSph,
                LeftCyl = Input.LeftCyl,
                LeftAxis = Input.LeftAxis,
                IsActive = Input.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            _context.PrescriptionProfiles.Add(prescription);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Prescription profile created successfully!";
            return RedirectToPage("./Index");
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }
    }
}
