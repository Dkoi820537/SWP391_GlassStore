
using EyewearStore_SWP391.Models;
using EyewearStore_SWP391.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EyewearStore_SWP391.Pages.Admin.Appointments
{
    [Authorize(Roles = "admin,manager")]
    public class IndexModel : PageModel
    {
        private readonly EyewearStoreContext _db;
        private readonly IEmailService _email;

        public IndexModel(EyewearStoreContext db, IEmailService email)
        {
            _db = db;
            _email = email;
        }

        [BindProperty(SupportsGet = true)] public string? Search { get; set; }
        [BindProperty(SupportsGet = true)] public string? StatusFilter { get; set; }
        [BindProperty(SupportsGet = true)] public string? DateFilter { get; set; }
        [BindProperty(SupportsGet = true)] public int Page { get; set; } = 1;
        public const int PageSize = 15;

        public List<EyeExamAppointment> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public bool HasPrev => Page > 1;
        public bool HasNext => Page < TotalPages;

        public int StatTotal { get; set; }
        public int StatPending { get; set; }
        public int StatConfirmed { get; set; }
        public int StatToday { get; set; }

        public async Task OnGetAsync()
        {
            var all = _db.EyeExamAppointments.AsQueryable();

            StatTotal = await all.CountAsync();
            StatPending = await all.CountAsync(a => a.Status == "Pending");
            StatConfirmed = await all.CountAsync(a => a.Status == "Confirmed");
            StatToday = await all.CountAsync(a =>
                a.AppointmentDate == DateOnly.FromDateTime(DateTime.Today));

            if (!string.IsNullOrWhiteSpace(Search))
            {
                var s = Search.Trim().ToLower();
                all = all.Where(a =>
                    a.FullName.ToLower().Contains(s) ||
                    a.Phone.Contains(s) ||
                    (a.Email != null && a.Email.ToLower().Contains(s)));
            }
            if (!string.IsNullOrWhiteSpace(StatusFilter))
                all = all.Where(a => a.Status == StatusFilter);
            if (!string.IsNullOrWhiteSpace(DateFilter) &&
                DateOnly.TryParse(DateFilter, out var df))
                all = all.Where(a => a.AppointmentDate == df);

            all = all.OrderByDescending(a => a.AppointmentDate).ThenBy(a => a.TimeSlot);

            TotalCount = await all.CountAsync();
            TotalPages = Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
            Page = Math.Clamp(Page, 1, TotalPages);

            Items = await all.Skip((Page - 1) * PageSize).Take(PageSize).ToListAsync();
        }

        public async Task<IActionResult> OnPostConfirmAsync(int id)
        {
            var appt = await _db.EyeExamAppointments.FindAsync(id);
            if (appt == null) return NotFound();

            appt.Status = "Confirmed";
            await _db.SaveChangesAsync();

            if (!string.IsNullOrEmpty(appt.Email))
                await _email.SendAppointmentConfirmedAsync(
                    appt.Email, appt.FullName,
                    appt.AppointmentDate.ToString("dd/MM/yyyy"),
                    appt.TimeSlot);

            TempData["Success"] = $"✅ Confirmed appointment for {appt.FullName}. Email sent.";
            // FIX: phải truyền tên page "Index" rõ ràng
            return RedirectToPage("Index", new { Search, StatusFilter, DateFilter, Page });
        }

        public async Task<IActionResult> OnPostCompleteAsync(int id)
        {
            var appt = await _db.EyeExamAppointments.FindAsync(id);
            if (appt == null) return NotFound();

            appt.Status = "Completed";
            await _db.SaveChangesAsync();

            TempData["Success"] = $"✅ Appointment #{id} marked as completed.";
            return RedirectToPage("Index", new { Search, StatusFilter, DateFilter, Page });
        }

        public async Task<IActionResult> OnPostCancelAsync(int id, string? reason)
        {
            var appt = await _db.EyeExamAppointments.FindAsync(id);
            if (appt == null) return NotFound();

            appt.Status = "Cancelled";
            await _db.SaveChangesAsync();

            if (!string.IsNullOrEmpty(appt.Email))
                await _email.SendAppointmentCancelledAsync(
                    appt.Email, appt.FullName,
                    appt.AppointmentDate.ToString("dd/MM/yyyy"),
                    appt.TimeSlot,
                    reason ?? "");

            TempData["Success"] = $"Appointment #{id} cancelled. Email sent to {appt.FullName}.";
            return RedirectToPage("Index", new { Search, StatusFilter, DateFilter, Page });
        }
    }
}
