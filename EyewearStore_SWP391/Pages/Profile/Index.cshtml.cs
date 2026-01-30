
using EyewearStore_SWP391.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace EyewearStore_SWP391.Pages.Profile
{
    public class IndexModel : PageModel
    {
        private readonly EyewearStoreContext _context;
        private readonly PasswordHasher<User> _passwordHasher;

        public IndexModel(EyewearStoreContext context)
        {
            _context = context;
            _passwordHasher = new PasswordHasher<User>();
        }

        [BindProperty]
        public ProfileInputModel Input { get; set; }

        [BindProperty]
        public ChangePasswordModel ChangePassword { get; set; }

        [BindProperty]
        public AddressInputModel AddressInput { get; set; }

        public List<Address> Addresses { get; set; } = new();

        public class ProfileInputModel
        {
            public int UserId { get; set; }
            public string Email { get; set; } = "";
            public string FullName { get; set; } = "";
            public string? Phone { get; set; }
        }

        public class ChangePasswordModel
        {
            public string CurrentPassword { get; set; } = "";
            public string NewPassword { get; set; } = "";
            public string ConfirmPassword { get; set; } = "";
        }

        public class AddressInputModel
        {
            public int? AddressId { get; set; }
            public string ReceiverName { get; set; } = "";
            public string Phone { get; set; } = "";
            public string AddressLine { get; set; } = "";
            public bool IsDefault { get; set; } = false;
        }

        private int CurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return claim == null ? 0 : int.Parse(claim);
        }

        public async Task<IActionResult> OnGetAsync()
        {
            if (!User.Identity?.IsAuthenticated ?? true) return Challenge();

            var uid = CurrentUserId();
            var user = await _context.Users.FindAsync(uid);
            if (user == null) return NotFound();

            Input = new ProfileInputModel
            {
                UserId = user.UserId,
                Email = user.Email,
                FullName = user.FullName,
                Phone = user.Phone
            };

            Addresses = await _context.Addresses
                .Where(a => a.UserId == uid)
                .OrderByDescending(a => a.IsDefault)
                .ThenByDescending(a => a.CreatedAt)
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostUpdateProfileAsync()
        {
            if (!ModelState.IsValid) return await OnGetAndReturnPage();

            var uid = CurrentUserId();
            var user = await _context.Users.FindAsync(uid);
            if (user == null) return NotFound();

            user.FullName = Input.FullName?.Trim() ?? user.FullName;
            user.Phone = Input.Phone?.Trim();
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Cập nhật thông tin cá nhân thành công.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostChangePasswordAsync()
        {
            if (ChangePassword.NewPassword != ChangePassword.ConfirmPassword)
            {
                ModelState.AddModelError("", "Mật khẩu mới và xác nhận mật khẩu không khớp.");
                return await OnGetAndReturnPage();
            }

            var uid = CurrentUserId();
            var user = await _context.Users.FindAsync(uid);
            if (user == null) return NotFound();

            var verify = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, ChangePassword.CurrentPassword);
            if (verify == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError("", "Mật khẩu hiện tại không đúng.");
                return await OnGetAndReturnPage();
            }

            user.PasswordHash = _passwordHasher.HashPassword(user, ChangePassword.NewPassword);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đổi mật khẩu thành công.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostAddAddressAsync()
        {
            if (string.IsNullOrWhiteSpace(AddressInput.ReceiverName) ||
                string.IsNullOrWhiteSpace(AddressInput.Phone) ||
                string.IsNullOrWhiteSpace(AddressInput.AddressLine))
            {
                ModelState.AddModelError("", "Vui lòng điền đầy đủ họ tên, số điện thoại và địa chỉ.");
                return await OnGetAndReturnPage();
            }

            var uid = CurrentUserId();

            if (AddressInput.IsDefault)
            {
                var others = await _context.Addresses.Where(a => a.UserId == uid && a.IsDefault).ToListAsync();
                others.ForEach(a => a.IsDefault = false);
            }

            var address = new Address
            {
                UserId = uid,
                ReceiverName = AddressInput.ReceiverName.Trim(),
                Phone = AddressInput.Phone.Trim(),
                AddressLine = AddressInput.AddressLine.Trim(),
                IsDefault = AddressInput.IsDefault,
                CreatedAt = DateTime.UtcNow
            };

            _context.Addresses.Add(address);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã thêm địa chỉ mới.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostEditAddressAsync()
        {
            if (!AddressInput.AddressId.HasValue) return BadRequest();

            var uid = CurrentUserId();
            var addr = await _context.Addresses.FirstOrDefaultAsync(a => a.AddressId == AddressInput.AddressId.Value && a.UserId == uid);
            if (addr == null) return NotFound();

            if (AddressInput.IsDefault)
            {
                var others = await _context.Addresses.Where(a => a.UserId == uid && a.IsDefault).ToListAsync();
                others.ForEach(a => a.IsDefault = false);
            }

            addr.ReceiverName = AddressInput.ReceiverName.Trim();
            addr.Phone = AddressInput.Phone.Trim();
            addr.AddressLine = AddressInput.AddressLine.Trim();
            addr.IsDefault = AddressInput.IsDefault;
     

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Cập nhật địa chỉ thành công.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAddressAsync(int addressId)
        {
            var uid = CurrentUserId();
            var addr = await _context.Addresses.FirstOrDefaultAsync(a => a.AddressId == addressId && a.UserId == uid);
            if (addr == null) return NotFound();

            _context.Addresses.Remove(addr);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Xóa địa chỉ thành công.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostSetDefaultAsync(int addressId)
        {
            var uid = CurrentUserId();
            var addr = await _context.Addresses.FirstOrDefaultAsync(a => a.AddressId == addressId && a.UserId == uid);
            if (addr == null) return NotFound();

            var others = await _context.Addresses.Where(a => a.UserId == uid && a.IsDefault).ToListAsync();
            others.ForEach(a => a.IsDefault = false);

            addr.IsDefault = true;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã đặt mặc định địa chỉ.";
            return RedirectToPage();
        }

        private async Task<IActionResult> OnGetAndReturnPage()
        {
            var uid = CurrentUserId();
            Addresses = await _context.Addresses
                .Where(a => a.UserId == uid)
                .OrderByDescending(a => a.IsDefault)
                .ThenByDescending(a => a.CreatedAt)
                .ToListAsync();

            var user = await _context.Users.FindAsync(uid);
            Input ??= new ProfileInputModel
            {
                UserId = user?.UserId ?? 0,
                Email = user?.Email ?? "",
                FullName = user?.FullName ?? "",
                Phone = user?.Phone
            };

            return Page();
        }
    }
}