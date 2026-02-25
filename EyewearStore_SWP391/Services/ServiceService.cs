using EyewearStore_SWP391.Models;
using Microsoft.EntityFrameworkCore;

namespace EyewearStore_SWP391.Services
{
    // ── DTOs ──────────────────────────────────────────────────────────────────

    public class ServiceCreateDto
    {
        public string Name { get; set; } = "";
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public int? DurationMin { get; set; }
        public bool IsActive { get; set; } = true;
        public IFormFile? ImageFile { get; set; }
    }

    public class ServiceUpdateDto
    {
        public string Name { get; set; } = "";
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public int? DurationMin { get; set; }
        public bool IsActive { get; set; } = true;
        public IFormFile? ImageFile { get; set; }
        public bool RemoveImage { get; set; } = false;
    }

    public class ServiceListResult
    {
        public List<Service> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
    }

    // ── Interface ─────────────────────────────────────────────────────────────

    public interface IServiceService
    {
        Task<ServiceListResult> GetAllAsync(string? search, bool? isActive, int page, int pageSize);
        Task<Service?> GetByIdAsync(int id);
        Task<Service> CreateAsync(ServiceCreateDto dto, IWebHostEnvironment env);
        Task<Service?> UpdateAsync(int id, ServiceUpdateDto dto, IWebHostEnvironment env);
        Task<bool> ToggleActiveAsync(int id);
        Task<bool> DeleteAsync(int id);
        Task<(int Total, int Active, int Inactive)> GetStatsAsync();
    }

    // ── Implementation ────────────────────────────────────────────────────────

    public class ServiceService : IServiceService
    {
        private readonly EyewearStoreContext _context;
        private readonly string[] _allowedExt = { ".jpg", ".jpeg", ".png", ".webp" };
        private const long MaxFileSize = 5 * 1024 * 1024;

        public ServiceService(EyewearStoreContext context)
        {
            _context = context;
        }

        public async Task<ServiceListResult> GetAllAsync(
            string? search, bool? isActive, int page, int pageSize)
        {
            var q = _context.Services.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                q = q.Where(x => x.Name.ToLower().Contains(s)
                    || (x.Description != null && x.Description.ToLower().Contains(s)));
            }

            if (isActive.HasValue)
                q = q.Where(x => x.IsActive == isActive.Value);

            var total = await q.CountAsync();
            var totalPages = (int)Math.Ceiling(total / (double)pageSize);
            page = Math.Max(1, Math.Min(page, Math.Max(1, totalPages)));

            var items = await q
                .OrderByDescending(x => x.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new ServiceListResult
            {
                Items = items,
                TotalCount = total,
                TotalPages = totalPages,
                CurrentPage = page,
                PageSize = pageSize
            };
        }

        public async Task<Service?> GetByIdAsync(int id)
            => await _context.Services.FirstOrDefaultAsync(s => s.ServiceId == id);

        public async Task<Service> CreateAsync(ServiceCreateDto dto, IWebHostEnvironment env)
        {
            var svc = new Service
            {
                Name = dto.Name.Trim(),
                Description = dto.Description?.Trim(),
                Price = dto.Price,
                DurationMin = dto.DurationMin,
                IsActive = dto.IsActive,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Services.Add(svc);
            await _context.SaveChangesAsync();

            if (dto.ImageFile is { Length: > 0 })
            {
                svc.ImageUrl = await SaveImageAsync(svc.ServiceId, dto.ImageFile, env);
                await _context.SaveChangesAsync();
            }

            return svc;
        }

        public async Task<Service?> UpdateAsync(int id, ServiceUpdateDto dto, IWebHostEnvironment env)
        {
            var svc = await _context.Services.FindAsync(id);
            if (svc == null) return null;

            svc.Name = dto.Name.Trim();
            svc.Description = dto.Description?.Trim();
            svc.Price = dto.Price;
            svc.DurationMin = dto.DurationMin;
            svc.IsActive = dto.IsActive;
            svc.UpdatedAt = DateTime.UtcNow;

            if (dto.RemoveImage)
            {
                DeleteImageFile(svc.ImageUrl, env);
                svc.ImageUrl = null;
            }

            if (dto.ImageFile is { Length: > 0 })
            {
                DeleteImageFile(svc.ImageUrl, env);
                svc.ImageUrl = await SaveImageAsync(svc.ServiceId, dto.ImageFile, env);
            }

            await _context.SaveChangesAsync();
            return svc;
        }

        public async Task<bool> ToggleActiveAsync(int id)
        {
            var svc = await _context.Services.FindAsync(id);
            if (svc == null) return false;
            svc.IsActive = !svc.IsActive;
            svc.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var svc = await _context.Services.FindAsync(id);
            if (svc == null) return false;
            _context.Services.Remove(svc);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<(int Total, int Active, int Inactive)> GetStatsAsync()
        {
            var all = await _context.Services.ToListAsync();
            var active = all.Count(s => s.IsActive);
            return (all.Count, active, all.Count - active);
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private async Task<string> SaveImageAsync(int id, IFormFile file, IWebHostEnvironment env)
        {
            var folder = Path.Combine(env.WebRootPath, "uploads", "services", id.ToString());
            Directory.CreateDirectory(folder);

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var fileName = $"{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid().ToString("N")[..8]}{ext}";
            var path = Path.Combine(folder, fileName);

            await using var stream = new FileStream(path, FileMode.Create);
            await file.CopyToAsync(stream);

            return $"/uploads/services/{id}/{fileName}";
        }

        private void DeleteImageFile(string? url, IWebHostEnvironment env)
        {
            if (string.IsNullOrEmpty(url)) return;
            var full = Path.Combine(env.WebRootPath, url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(full)) File.Delete(full);
        }
    }
}