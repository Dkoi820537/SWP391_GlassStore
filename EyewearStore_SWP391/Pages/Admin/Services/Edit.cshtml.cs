using EyewearStore_SWP391.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EyewearStore_SWP391.Pages.Admin.Services
{
    [Authorize(Roles = "admin,manager")]
    public class EditModel : PageModel
    {
        private readonly IServiceService _svc;
        private readonly IWebHostEnvironment _env;

        public EditModel(IServiceService svc, IWebHostEnvironment env)
        {
            _svc = svc;
            _env = env;
        }

        [BindProperty] public ServiceFormInput Input { get; set; } = new();
        public string? ExistingImageUrl { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var s = await _svc.GetByIdAsync(id);
            if (s == null) return NotFound();

            ExistingImageUrl = s.ImageUrl;
            Input = new ServiceFormInput
            {
                ServiceId = s.ServiceId,
                Name = s.Name,
                Description = s.Description,
                Price = s.Price,
                DurationMin = s.DurationMin,
                IsActive = s.IsActive
            };
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                var existing = await _svc.GetByIdAsync(Input.ServiceId);
                ExistingImageUrl = existing?.ImageUrl;
                return Page();
            }

            var result = await _svc.UpdateAsync(Input.ServiceId, new ServiceUpdateDto
            {
                Name = Input.Name,
                Description = Input.Description,
                Price = Input.Price,
                DurationMin = Input.DurationMin,
                IsActive = Input.IsActive,
                ImageFile = Input.ImageFile,
                RemoveImage = Input.RemoveImage
            }, _env);

            if (result == null) return NotFound();

            TempData["Success"] = $"Service \"{Input.Name}\" updated successfully!";
            return RedirectToPage("Index");
        }
    }
}