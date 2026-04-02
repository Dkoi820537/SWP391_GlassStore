// Services/EmailService.cs  — THAY THẾ HOÀN TOÀN file cũ
using System.Net;
using System.Net.Mail;

namespace EyewearStore_SWP391.Services
{
    public interface IEmailService
    {
     
        Task SendRestockNotificationAsync(string toEmail, string customerName,
            string productName, string productUrl);
        Task SendEmailAsync(string toEmail, string subject, string body);

        Task SendAppointmentConfirmedAsync(string toEmail, string customerName,
            string date, string timeSlot);
        Task SendAppointmentCancelledAsync(string toEmail, string customerName,
            string date, string timeSlot, string reason);

        Task SendServiceOrderStatusAsync(string toEmail, string customerName,
            int orderId, string frameName, string serviceName,
            string newStatus, string? assignedTo, string? note);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }
        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var smtpHost = _configuration["Email:SmtpHost"] ?? "smtp.gmail.com";
            var smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
            var fromEmail = _configuration["Email:FromEmail"] ?? "";
            var fromName = _configuration["Email:FromName"] ?? "OptiPlus";
            var password = _configuration["Email:Password"] ?? "";

            try
            {
                using var message = new MailMessage();
                message.From = new MailAddress(fromEmail, fromName);
                message.To.Add(new MailAddress(toEmail));
                message.Subject = subject;
                message.Body = body;
                message.IsBodyHtml = true;

                using var client = new SmtpClient(smtpHost, smtpPort)
                {
                    EnableSsl = true,
                    Credentials = new NetworkCredential(fromEmail, password)
                };
                await client.SendMailAsync(message);
                _logger.LogInformation("Email sent to {To}: {Subject}", toEmail, subject);
            }
            catch (Exception ex)
            {
                // Never let email failure crash the app
                _logger.LogError(ex, "Email failed to {To}: {Subject}", toEmail, subject);
            }
        }
        public async Task SendRestockNotificationAsync(string toEmail, string customerName,
            string productName, string productUrl)
        {
            var subject = $"{productName} is Back in Stock!";
            var body = Wrap($@"
<div style='text-align:center;margin-bottom:24px'>
  <div style='font-size:44px;margin-bottom:12px'>&#9733;</div>
  <h2 style='margin:0 0 8px;font-size:22px;color:#1a2332'>Great news, {customerName}!</h2>
  <p style='margin:0;font-size:14px;color:#6b7280'>
    The item you've been waiting for is <strong style='color:#b8862a'>back in stock</strong>!
  </p>
</div>
<div style='background:#f8f6f2;border:1px solid #e8e4de;border-radius:14px;padding:24px;text-align:center;margin-bottom:24px'>
  <div style='font-size:19px;font-weight:800;color:#1a2332;margin-bottom:6px'>{productName}</div>
  <div style='font-size:12px;color:#94908a'>Limited quantity — order before it sells out again</div>
</div>
<div style='text-align:center'>
  <a href='{productUrl}' style='{Btn("#b8862a")}'>Shop Now</a>
</div>");
            await SendEmailAsync(toEmail, subject, body);
        }

        // ═══════════════════════════════════════════════════════════════════════
        // APPOINTMENT CONFIRMED
        // ═══════════════════════════════════════════════════════════════════════
        public async Task SendAppointmentConfirmedAsync(string toEmail, string customerName,
            string date, string timeSlot)
        {
            var subject = "Eye Exam Appointment Confirmed — OptiPlus";
            var body = Wrap($@"
<div style='text-align:center;margin-bottom:24px'>
  <div style='font-size:48px;margin-bottom:12px'>&#10003;</div>
  <h2 style='margin:0 0 8px;font-size:22px;color:#1a2332'>Appointment Confirmed!</h2>
  <p style='margin:0;font-size:14px;color:#6b7280'>Hi <strong>{customerName}</strong>, we look forward to seeing you.</p>
</div>
{Table(new[]{
    ("Date",     date),
    ("⏰ Time",     timeSlot),
    ("Duration", "~30 minutes"),
    ("Location", "OptiPlus Store — in person only")
})}
<div style='background:#f0fdf4;border:1px solid #bbf7d0;border-radius:12px;padding:16px 20px;margin-top:20px'>
  <p style='margin:0;font-size:13px;color:#166534;line-height:1.7'><strong>What to bring:</strong><br>
  • Your current glasses or contact lenses (if any)<br>
  • Previous prescription if available<br>
  • Photo ID</p>
</div>
<p style='text-align:center;font-size:12px;color:#94908a;margin-top:18px'>
  Need to reschedule? Please contact us at least 24 hours before your appointment.
</p>");
            await SendEmailAsync(toEmail, subject, body);
        }

        // ═══════════════════════════════════════════════════════════════════════
        // APPOINTMENT CANCELLED
        // ═══════════════════════════════════════════════════════════════════════
        public async Task SendAppointmentCancelledAsync(string toEmail, string customerName,
            string date, string timeSlot, string reason)
        {
            var subject = "Eye Exam Appointment Cancelled — OptiPlus";
            var reasonRow = string.IsNullOrWhiteSpace(reason) ? ""
                : $"<tr><td style='{Lbl}'>Reason</td><td style='{Val}'>{reason}</td></tr>";

            var body = Wrap($@"
<div style='text-align:center;margin-bottom:24px'>
  <div style='font-size:48px;margin-bottom:12px'>&#10007;</div>
  <h2 style='margin:0 0 8px;font-size:22px;color:#1a2332'>Appointment Cancelled</h2>
  <p style='margin:0;font-size:14px;color:#6b7280'>Hi <strong>{customerName}</strong>, your appointment has been cancelled.</p>
</div>
{Table(new[] { ("Date", date), ("Time", timeSlot) }, extra: reasonRow)}
<p style='font-size:14px;color:#4a4a5a;text-align:center;margin-top:20px'>
  We apologise for any inconvenience. Please book a new appointment at your convenience.
</p>
<div style='text-align:center;margin-top:20px'>
  <a href='https://localhost:7001/Customer/Services/EyeExam' style='{Btn("#2563eb")}'>Book a New Appointment</a>
</div>");
            await SendEmailAsync(toEmail, subject, body);
        }

        // ═══════════════════════════════════════════════════════════════════════
        // SERVICE ORDER STATUS
        // ═══════════════════════════════════════════════════════════════════════
        public async Task SendServiceOrderStatusAsync(string toEmail, string customerName,
            int orderId, string frameName, string serviceName,
            string newStatus, string? assignedTo, string? note)
        {
            var (emoji, color, msg) = newStatus switch
            {
                "Processing" => ("[Processing]", "#2563eb", "Our technicians are now working on your order."),
                "Ready" => ("[Ready]", "#059669", "Your glasses are ready! Please come in to collect them."),
                "Done" => ("[Done]", "#0a0a1a", "Your order has been completed. Thank you for choosing OptiPlus!"),
                "Cancelled" => ("[Cancelled]", "#dc2626", "Your service order has been cancelled. Please contact us for details."),
                _ => ("[Update]", "#b8862a", "Your order status has been updated.")
            };

            var subject = $"{emoji} Service Order #{orderId} — {newStatus}";

            var assignedRow = string.IsNullOrWhiteSpace(assignedTo) ? ""
                : $"<tr><td style='{Lbl}'>Assigned To</td><td style='{Val}'>{assignedTo}</td></tr>";

            var noteBox = string.IsNullOrWhiteSpace(note) ? "" : $@"
<div style='background:#f8f6f2;border-left:4px solid {color};border-radius:8px;padding:14px 18px;margin-top:18px'>
  <p style='margin:0;font-size:13px;color:#4a4a5a;line-height:1.6'>
    <strong>Note from our team:</strong><br>{note}
  </p>
</div>";

            var readyBtn = newStatus == "Ready" ? $@"
<div style='text-align:center;margin-top:24px'>
  <a href='https://localhost:7001/Customer/Orders' style='{Btn(color)}'>View My Orders →</a>
</div>" : "";

            var body = Wrap($@"
<div style='text-align:center;margin-bottom:24px'>
  <div style='font-size:48px;margin-bottom:10px'>{emoji}</div>
  <h2 style='margin:0 0 8px;font-size:22px;color:#1a2332'>Service Order Update</h2>
  <p style='margin:0;font-size:14px;color:#6b7280'>Hi <strong>{customerName}</strong>, here's the latest on your order.</p>
</div>
{Table(new[]{
    ("Order",    $"<strong>#{orderId}</strong>"),
    ("Frame",   frameName),
    ("Service",  serviceName),
    ("Status",   $"<span style='color:{color};font-weight:800'>{emoji} {newStatus}</span>")
}, extra: assignedRow)}
<p style='font-size:14px;color:#4a4a5a;text-align:center;margin-top:18px'>{msg}</p>
{noteBox}
{readyBtn}");

            await SendEmailAsync(toEmail, subject, body);
        }

        // ═══════════════════════════════════════════════════════════════════════
        // HELPERS
        // ═══════════════════════════════════════════════════════════════════════
        private static string Lbl =>
            "font-size:11px;font-weight:700;text-transform:uppercase;letter-spacing:.8px;" +
            "color:#94908a;padding:11px 14px;width:120px;vertical-align:top;" +
            "border-bottom:1px solid #ede9e3";
        private static string Val =>
            "font-size:14px;font-weight:600;color:#1a2332;padding:11px 14px;" +
            "border-bottom:1px solid #ede9e3";

        private static string Table((string, string)[] rows, string extra = "")
        {
            var html = string.Join("", rows.Select(r =>
                $"<tr><td style='{Lbl}'>{r.Item1}</td><td style='{Val}'>{r.Item2}</td></tr>"));
            return $"<table width='100%' style='background:#f8f6f2;border:1px solid #e8e4de;" +
                   $"border-radius:12px;overflow:hidden;border-collapse:collapse'>" +
                   $"{html}{extra}</table>";
        }

        private static string Btn(string bg) =>
            $"display:inline-block;background:{bg};color:#fff;padding:13px 32px;" +
            "text-decoration:none;border-radius:9px;font-weight:700;font-size:14px;" +
            "font-family:'Segoe UI',Arial,sans-serif;letter-spacing:.3px";

        private static string Wrap(string content) => $@"<!DOCTYPE html>
<html lang='en'>
<head><meta charset='UTF-8'/><meta name='viewport' content='width=device-width,initial-scale=1'/></head>
<body style='margin:0;padding:0;background:#f0ede8;font-family:""Segoe UI"",Arial,sans-serif'>
<table width='100%' cellpadding='0' cellspacing='0' style='padding:28px 14px'>
<tr><td align='center'>
<table width='600' cellpadding='0' cellspacing='0'
  style='max-width:600px;background:#fff;border-radius:18px;overflow:hidden;box-shadow:0 4px 24px rgba(0,0,0,.09)'>
  <tr>
    <td style='background:linear-gradient(135deg,#0a0a1a 0%,#1a2332 100%);padding:30px;text-align:center'>
      <div style='font-size:11px;font-weight:700;letter-spacing:3px;color:rgba(255,255,255,.35);text-transform:uppercase;margin-bottom:8px'>OptiPlus</div>
      <div style='font-size:22px;font-weight:800;color:#fff;margin-bottom:4px'>OptiPlus Eyewear</div>
      <div style='font-size:12px;color:rgba(255,255,255,.45)'>Premium Eye Care &amp; Optical Services</div>
    </td>
  </tr>
  <tr><td style='padding:36px 32px'>{content}</td></tr>
  <tr>
    <td style='background:#f8f6f2;padding:20px 32px;text-align:center;border-top:1px solid #e8e4de'>
      <p style='margin:0;font-size:11px;color:#94908a;line-height:1.6'>
        © 2025 OptiPlus Eyewear. All rights reserved.<br>
        You received this because you have an account or booking with us.
      </p>
    </td>
  </tr>
</table>
</td></tr>
</table>
</body></html>";
    }
}
