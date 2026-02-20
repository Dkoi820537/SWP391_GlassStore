using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using EyewearStore_SWP391.Models;
using EyewearStore_SWP391.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace EyewearStore_SWP391.Pages.Account
{
    public class RegisterViewModel
    {
        [Required, EmailAddress]
        public string Email { get; set; } = null!;
        [Required, MinLength(6)]
        [DataType(DataType.Password)]
        public string Password { get; set; } = null!;
        [Required]
        public string FullName { get; set; } = null!;
        [Phone]
        public string? Phone { get; set; }
    }

    public class RegisterModel : PageModel
    {
        private readonly EyewearStoreContext _context;
        private readonly PasswordHasher<User> _passwordHasher;
        private readonly IOtpService _otpService;
        private readonly IEmailService _emailService;

        public RegisterModel(
            EyewearStoreContext context,
            IOtpService otpService,
            IEmailService emailService)
        {
            _context = context;
            _passwordHasher = new PasswordHasher<User>();
            _otpService = otpService;
            _emailService = emailService;
        }

        [BindProperty] public RegisterViewModel Input { get; set; } = new();
        [BindProperty] public string OtpCode { get; set; } = "";

        // Controls which step is shown: "form" | "otp"
        public string Step { get; set; } = "form";

        // ── STEP 1: Show registration form ────────────────────────────────────
        public void OnGet() => Step = "form";

        // ── STEP 2: Validate form → send OTP ─────────────────────────────────
        public async Task<IActionResult> OnPostSendOtpAsync()
        {
            // Validate only form fields, not OtpCode
            foreach (var key in ModelState.Keys.Where(k => k.Contains("OtpCode")))
                ModelState.Remove(key);

            if (!ModelState.IsValid) { Step = "form"; return Page(); }

            // Check duplicate email
            if (_context.Users.Any(u => u.Email == Input.Email))
            {
                ModelState.AddModelError("", "This email is already registered.");
                Step = "form";
                return Page();
            }

            // Generate & send OTP
            var code = _otpService.GenerateOtp(Input.Email);
            await _emailService.SendEmailAsync(
                Input.Email,
                "Your OptiPlus Verification Code",
                BuildOtpEmailHtml(Input.FullName, code)
            );

            // Store form data in TempData to survive the redirect
            TempData["Reg_Email"] = Input.Email;
            TempData["Reg_Password"] = Input.Password;
            TempData["Reg_FullName"] = Input.FullName;
            TempData["Reg_Phone"] = Input.Phone;
            TempData["Reg_Step"] = "otp";

            Step = "otp";
            return Page();
        }

        // ── STEP 3: Verify OTP → create account ───────────────────────────────
        public async Task<IActionResult> OnPostVerifyOtpAsync()
        {
            // Restore form data from TempData
            Input.Email = TempData["Reg_Email"] as string ?? "";
            Input.Password = TempData["Reg_Password"] as string ?? "";
            Input.FullName = TempData["Reg_FullName"] as string ?? "";
            Input.Phone = TempData["Reg_Phone"] as string;

            // Keep TempData alive in case we need to re-show OTP page
            TempData.Keep();

            if (string.IsNullOrWhiteSpace(OtpCode) || OtpCode.Length != 6)
            {
                ModelState.AddModelError("OtpCode", "Please enter the 6-digit code.");
                Step = "otp";
                return Page();
            }

            if (!_otpService.ValidateOtp(Input.Email, OtpCode))
            {
                ModelState.AddModelError("OtpCode", "Invalid or expired code. Please try again.");
                Step = "otp";
                // Re-send so TempData is available for re-render
                TempData["Reg_Email"] = Input.Email;
                TempData["Reg_Password"] = Input.Password;
                TempData["Reg_FullName"] = Input.FullName;
                TempData["Reg_Phone"] = Input.Phone;
                TempData["Reg_Step"] = "otp";
                return Page();
            }

            // Create user
            var user = new User
            {
                Email = Input.Email,
                FullName = Input.FullName,
                Phone = Input.Phone,
                Role = "Customer",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            user.PasswordHash = _passwordHasher.HashPassword(user, Input.Password);
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Account created! Please log in.";
            return RedirectToPage("/Account/Login");
        }

        // ── STEP: Resend OTP ──────────────────────────────────────────────────
        public async Task<IActionResult> OnPostResendOtpAsync()
        {
            Input.Email = TempData["Reg_Email"] as string ?? "";
            Input.FullName = TempData["Reg_FullName"] as string ?? "";
            Input.Password = TempData["Reg_Password"] as string ?? "";
            Input.Phone = TempData["Reg_Phone"] as string;

            if (!string.IsNullOrEmpty(Input.Email))
            {
                var code = _otpService.GenerateOtp(Input.Email);
                await _emailService.SendEmailAsync(
                    Input.Email,
                    "Your OptiPlus Verification Code (Resent)",
                    BuildOtpEmailHtml(Input.FullName, code)
                );
            }

            TempData["Reg_Email"] = Input.Email;
            TempData["Reg_Password"] = Input.Password;
            TempData["Reg_FullName"] = Input.FullName;
            TempData["Reg_Phone"] = Input.Phone;
            TempData["Reg_Step"] = "otp";
            TempData["ResendMsg"] = "A new code has been sent to your email.";

            Step = "otp";
            return Page();
        }

        // ── Email HTML template ───────────────────────────────────────────────
        private static string BuildOtpEmailHtml(string name, string code) => $@"
<!DOCTYPE html>
<html>
<head>
  <style>
    body {{ font-family: 'Segoe UI', sans-serif; background: #f4f4f4; margin: 0; padding: 0; }}
    .wrap {{ max-width: 520px; margin: 40px auto; background: white; border-radius: 16px; overflow: hidden; box-shadow: 0 4px 20px rgba(0,0,0,0.08); }}
    .header {{ background: linear-gradient(135deg, #1a1610 0%, #2a2015 100%); padding: 40px 32px; text-align: center; }}
    .header h1 {{ color: #c9a96e; font-size: 26px; margin: 0 0 6px; }}
    .header p {{ color: rgba(255,255,255,0.6); margin: 0; font-size: 14px; }}
    .body {{ padding: 40px 32px; text-align: center; }}
    .greeting {{ font-size: 17px; color: #333; margin-bottom: 8px; }}
    .msg {{ color: #777; font-size: 14px; margin-bottom: 32px; line-height: 1.6; }}
    .otp-box {{ display: inline-block; background: #f8f5f0; border: 2px dashed #c9a96e; border-radius: 12px; padding: 20px 40px; margin-bottom: 24px; }}
    .otp-code {{ font-size: 42px; font-weight: 700; letter-spacing: 10px; color: #1a1610; font-family: monospace; }}
    .expire {{ color: #aaa; font-size: 13px; margin-bottom: 28px; }}
    .warning {{ background: #fffbf5; border-left: 3px solid #c9a96e; padding: 12px 16px; border-radius: 6px; font-size: 13px; color: #7a6040; text-align: left; }}
    .footer {{ background: #f8f5f0; padding: 20px 32px; text-align: center; color: #aaa; font-size: 12px; }}
  </style>
</head>
<body>
  <div class='wrap'>
    <div class='header'>
      <h1>👓 OptiPlus</h1>
      <p>Email Verification</p>
    </div>
    <div class='body'>
      <p class='greeting'>Hello, <strong>{name}</strong>!</p>
      <p class='msg'>Use the code below to verify your email address and complete your registration.</p>
      <div class='otp-box'>
        <div class='otp-code'>{code}</div>
      </div>
      <p class='expire'>⏱ This code expires in <strong>10 minutes</strong></p>
      <div class='warning'>
        🔒 <strong>Security notice:</strong> Never share this code with anyone. OptiPlus staff will never ask for your OTP.
      </div>
    </div>
    <div class='footer'>© {DateTime.Now.Year} OptiPlus. All rights reserved.</div>
  </div>
</body>
</html>";
    }
}
