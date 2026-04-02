using EyewearStore_SWP391.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EyewearStore_SWP391.Pages.Customer.Services
{
    [Authorize]
    public class CustomGlassesModel : PageModel
    {
        private readonly EyewearStoreContext _context;
        public CustomGlassesModel(EyewearStoreContext context) => _context = context;

        public List<Frame> Frames { get; set; } = new();
        public List<Lens> Lenses { get; set; } = new();
        public List<Service> Services { get; set; } = new();
        public List<PrescriptionProfile> PrescriptionProfiles { get; set; } = new();

        /// <summary>
        /// Maps each frameId to its list of compatible lens type strings.
        /// Empty list / missing key = open compatibility (show all lenses).
        /// </summary>
        public Dictionary<int, List<string>> CompatibilityMap { get; set; } = new();

        /// <summary>
        /// Maps each lensProductId → { IsPrescription, PrescriptionFee }.
        /// Used by frontend JS to decide whether to show the prescription step.
        /// </summary>
        public Dictionary<int, LensPrescriptionInfo> LensPrescriptionMap { get; set; } = new();

        public record LensPrescriptionInfo(bool IsPrescription, decimal PrescriptionFee);

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = GetCurrentUserId();

            Frames = await _context.Set<Frame>()
                .Include(f => f.ProductImages)
                .Where(f => f.IsActive && (f.InventoryQty == null || f.InventoryQty > 0))
                .OrderBy(f => f.Name)
                .ToListAsync();

            Lenses = await _context.Set<Lens>()
                .Include(l => l.ProductImages)
                .Where(l => l.IsActive && (l.InventoryQty == null || l.InventoryQty > 0))
                .OrderBy(l => l.Name)
                .ToListAsync();

            Services = await _context.Services
                .Where(s => s.IsActive)
                .OrderBy(s => s.Name)
                .ToListAsync();

            // Load frame → compatible lens types mapping
            var frameIds = Frames.Select(f => f.ProductId).ToList();
            CompatibilityMap = (await _context.FrameCompatibleLensTypes
                .Where(c => frameIds.Contains(c.FrameProductId))
                .ToListAsync())
                .GroupBy(c => c.FrameProductId)
                .ToDictionary(g => g.Key, g => g.Select(c => c.LensType).ToList());

            // Build lens prescription info map
            LensPrescriptionMap = Lenses.ToDictionary(
                l => l.ProductId,
                l => new LensPrescriptionInfo(l.IsPrescription, l.PrescriptionFee ?? 0m));

            // Load user's active prescription profiles
            if (userId > 0)
            {
                PrescriptionProfiles = await _context.PrescriptionProfiles
                    .Where(p => p.UserId == userId && p.IsActive)
                    .OrderByDescending(p => p.CreatedAt)
                    .ToListAsync();
            }

            return Page();
        }

        /// <summary>
        /// AJAX endpoint — creates a new prescription profile inline from the wizard.
        /// Returns JSON { success, prescriptionId, profileName }.
        /// </summary>
        public async Task<IActionResult> OnPostCreatePrescriptionAsync(
            string? profileName,
            decimal? leftSph, decimal? leftCyl, int? leftAxis,
            decimal? rightSph, decimal? rightCyl, int? rightAxis)
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
                return new JsonResult(new { success = false, error = "Not authenticated." });

            // Basic validation
            if (string.IsNullOrWhiteSpace(profileName))
                profileName = $"Prescription {DateTime.Now:MMM dd, yyyy}";

            var profile = new PrescriptionProfile
            {
                UserId = userId,
                ProfileName = profileName.Trim(),
                LeftSph = leftSph,
                LeftCyl = leftCyl,
                LeftAxis = leftAxis,
                RightSph = rightSph,
                RightCyl = rightCyl,
                RightAxis = rightAxis,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.PrescriptionProfiles.Add(profile);
            await _context.SaveChangesAsync();

            return new JsonResult(new
            {
                success = true,
                prescriptionId = profile.PrescriptionId,
                profileName = profile.ProfileName,
                leftSph = profile.LeftSph,
                leftCyl = profile.LeftCyl,
                leftAxis = profile.LeftAxis,
                rightSph = profile.RightSph,
                rightCyl = profile.RightCyl,
                rightAxis = profile.RightAxis
            });
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(claim, out var id) ? id : 0;
        }
    }
}
