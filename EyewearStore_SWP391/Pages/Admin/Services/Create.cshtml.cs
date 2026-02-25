using System.ComponentModel.DataAnnotations;
using EyewearStore_SWP391.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EyewearStore_SWP391.Pages.Admin.Services
{
    [Authorize(Roles = "admin,manager")]
    public class CreateModel : PageModel
    {
        private readonly IServiceService _svc;
        private readonly IWebHostEnvironment _env;

        public CreateModel(IServiceService svc, IWebHostEnvironment env)
        {
            _svc = svc;
            _env = env;
        }

        [BindProperty] public ServiceFormInput Input { get; set; } = new();

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            await _svc.CreateAsync(new ServiceCreateDto
            {
                Name = Input.Name,
                Description = Input.Description,
                Price = Input.Price,
                DurationMin = Input.DurationMin,
                IsActive = Input.IsActive,
                ImageFile = Input.ImageFile
            }, _env);

            TempData["Success"] = $"Service \"{Input.Name}\" created successfully!";
            return RedirectToPage("Index");
        }
    }

    // ── Shared input model (used by both Create and Edit) ──────────────────
    public class ServiceFormInput
    {
        public int ServiceId { get; set; }

        [Required(ErrorMessage = "Service name is required.")]
        [MaxLength(200, ErrorMessage = "Max 200 characters.")]
        public string Name { get; set; } = "";

        public string? Description { get; set; }

        [Required(ErrorMessage = "Price is required.")]
        [Range(0, 999_999_999, ErrorMessage = "Price must be a positive number.")]
        public decimal Price { get; set; }

        [Range(1, 1440, ErrorMessage = "Duration must be between 1 and 1440 minutes.")]
        public int? DurationMin { get; set; }

        public bool IsActive { get; set; } = true;

        public IFormFile? ImageFile { get; set; }

        public bool RemoveImage { get; set; } = false;
    }
}