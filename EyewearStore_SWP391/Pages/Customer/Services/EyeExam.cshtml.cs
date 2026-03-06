// Pages/Customer/Services/EyeExam.cshtml.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Threading.Tasks;
using EyewearStore_SWP391.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EyewearStore_SWP391.Pages.Customer.Services
{
    public class EyeExamModel : PageModel
    {
        private readonly EyewearStoreContext _context;
        public EyeExamModel(EyewearStoreContext context) => _context = context;

        [BindProperty]
        public BookingInput Input { get; set; } = new();

        public string? PrefillName { get; set; }
        public string? PrefillPhone { get; set; }
        public string? PrefillEmail { get; set; }

        public async Task OnGetAsync()
        {
            // Pre-fill from logged-in user if available
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out int uid))
            {
                var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == uid);
                if (user != null)
                {
                    PrefillName = user.FullName;
                    PrefillPhone = user.Phone;
                    PrefillEmail = user.Email;
                }
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            // Check date is at least tomorrow
            if (Input.AppointmentDate <= DateOnly.FromDateTime(DateTime.Today))
            {
                ModelState.AddModelError("Input.AppointmentDate", "Please select a date at least 1 day ahead.");
                return Page();
            }

            // Get logged-in userId if any
            int? userId = null;
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out int uid)) userId = uid;

            var appt = new EyeExamAppointment
            {
                UserId = userId,
                FullName = Input.FullName.Trim(),
                Phone = Input.Phone.Trim(),
                Email = Input.Email?.Trim(),
                AppointmentDate = Input.AppointmentDate,
                TimeSlot = Input.TimeSlot,
                Notes = Input.Notes?.Trim(),
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            _context.EyeExamAppointments.Add(appt);
            await _context.SaveChangesAsync();

            TempData["BookingSuccess"] = "true";
            TempData["BookingName"] = appt.FullName;
            TempData["BookingDate"] = appt.AppointmentDate.ToString("dd/MM/yyyy");
            TempData["BookingTime"] = appt.TimeSlot;

            return RedirectToPage("/Customer/Services/EyeExam");
        }

        // ── Input Model ───────────────────────────────────────────────────────
        public class BookingInput
        {
            [Required(ErrorMessage = "Full name is required.")]
            [MaxLength(150)]
            public string FullName { get; set; } = "";

            [Required(ErrorMessage = "Phone number is required.")]
            [MaxLength(20)]
            public string Phone { get; set; } = "";

            [EmailAddress]
            [MaxLength(200)]
            public string? Email { get; set; }

            [Required(ErrorMessage = "Please select a date.")]
            public DateOnly AppointmentDate { get; set; }

            [Required(ErrorMessage = "Please select a time slot.")]
            public string TimeSlot { get; set; } = "";

            [MaxLength(1000)]
            public string? Notes { get; set; }
        }
    }
}
