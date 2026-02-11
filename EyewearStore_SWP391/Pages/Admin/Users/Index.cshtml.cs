using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using EyewearStore_SWP391.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Security.Claims;

namespace EyewearStore_SWP391.Pages.Admin.Users
{
    /// <summary>
    /// User Management - Admin can manage all users, roles, and permissions
    /// BR: Admin has full control over user accounts
    /// </summary>
    [Authorize(Roles = "admin,Administrator")]
    public class IndexModel : PageModel
    {
        private readonly EyewearStoreContext _context;

        public IndexModel(EyewearStoreContext context)
        {
            _context = context;
        }

        public List<UserDto> Users { get; set; } = new();
        public UserStats Stats { get; set; } = new();
        public int CurrentUserId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Search { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? RoleFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? StatusFilter { get; set; }

        public List<string> AvailableRoles { get; } = new()
        {
            "customer",
            "sale",
            "sales",
            "support",
            "staff",
            "operational",
            "manager",
            "admin",
            "Administrator"
        };

        public class UserDto
        {
            public int UserId { get; set; }
            public string Email { get; set; } = "";
            public string? FullName { get; set; }
            public string? Phone { get; set; }
            public string Role { get; set; } = "";
            public bool IsActive { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        public class UserStats
        {
            public int TotalUsers { get; set; }
            public int ActiveUsers { get; set; }
            public int CustomerCount { get; set; }
            public int StaffCount { get; set; }
            public int AdminCount { get; set; }
        }

        public async Task OnGetAsync()
        {
            // Get current user ID
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out var currentUserId))
            {
                CurrentUserId = currentUserId;
            }

            // Calculate stats
            await CalculateStatsAsync();

            // Build query
            var query = _context.Users.AsQueryable();

            // Search filter
            if (!string.IsNullOrWhiteSpace(Search))
            {
                var searchTerm = Search.Trim().ToLower();
                query = query.Where(u =>
                    u.Email.ToLower().Contains(searchTerm) ||
                    (u.FullName != null && u.FullName.ToLower().Contains(searchTerm)) ||
                    (u.Phone != null && u.Phone.Contains(searchTerm)));
            }

            // Role filter
            if (!string.IsNullOrWhiteSpace(RoleFilter) && AvailableRoles.Contains(RoleFilter))
            {
                query = query.Where(u => u.Role == RoleFilter);
            }

            // Status filter
            if (!string.IsNullOrWhiteSpace(StatusFilter))
            {
                var isActive = StatusFilter == "Active";
                query = query.Where(u => u.IsActive == isActive);
            }

            // Get users
            Users = await query
                .OrderByDescending(u => u.CreatedAt)
                .Select(u => new UserDto
                {
                    UserId = u.UserId,
                    Email = u.Email,
                    FullName = u.FullName,
                    Phone = u.Phone,
                    Role = u.Role,
                    IsActive = u.IsActive,
                    CreatedAt = u.CreatedAt
                })
                .ToListAsync();
        }

        private async Task CalculateStatsAsync()
        {
            Stats.TotalUsers = await _context.Users.CountAsync();
            Stats.ActiveUsers = await _context.Users.CountAsync(u => u.IsActive);
            Stats.CustomerCount = await _context.Users.CountAsync(u => u.Role == "customer");

            // Staff count (all non-customer, non-admin roles)
            Stats.StaffCount = await _context.Users
                .Where(u => u.Role != "customer" && u.Role != "admin" && u.Role != "Administrator")
                .CountAsync();

            // Admin count
            Stats.AdminCount = await _context.Users
                .Where(u => u.Role == "admin" || u.Role == "Administrator")
                .CountAsync();
        }

        /// <summary>
        /// Delete user (soft delete by setting IsActive = false)
        /// BR: Admin can deactivate users but cannot delete themselves
        /// </summary>
        public async Task<IActionResult> OnPostDeleteAsync(int userId)
        {
            // Prevent self-deletion
            if (userId == CurrentUserId)
            {
                TempData["Error"] = "You cannot delete your own account!";
                return RedirectToPage();
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                TempData["Error"] = "User not found!";
                return RedirectToPage();
            }

            // Soft delete (deactivate)
            user.IsActive = false;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"User {user.Email} has been deactivated successfully!";
            return RedirectToPage();
        }
    }
}
