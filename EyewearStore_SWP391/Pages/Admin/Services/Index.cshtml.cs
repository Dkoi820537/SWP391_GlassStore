using EyewearStore_SWP391.Models;
using EyewearStore_SWP391.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EyewearStore_SWP391.Pages.Admin.Services
{
    [Authorize(Roles = "admin,manager")]
    public class IndexModel : PageModel
    {
        private readonly IServiceService _svc;
        public IndexModel(IServiceService svc) => _svc = svc;

        // Filter & pagination
        [BindProperty(SupportsGet = true)] public string? SearchTerm { get; set; }
        [BindProperty(SupportsGet = true)] public bool? FilterActive { get; set; }
        [BindProperty(SupportsGet = true)] public int CurrentPage { get; set; } = 1;
        public const int PageSize = 10;

        // Page data
        public List<Service> Items { get; set; } = new();
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
        public (int Total, int Active, int Inactive) Stats { get; set; }

        // Toast from redirect
        public string? SuccessMessage => TempData.Peek("Success") as string;
        public string? ErrorMessage => TempData.Peek("Error") as string;

        public async Task OnGetAsync()
        {
            var result = await _svc.GetAllAsync(SearchTerm, FilterActive, CurrentPage, PageSize);
            Items = result.Items;
            TotalPages = result.TotalPages;
            TotalCount = result.TotalCount;
            CurrentPage = result.CurrentPage;
            Stats = await _svc.GetStatsAsync();
        }
    }
}