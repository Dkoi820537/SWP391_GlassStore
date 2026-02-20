using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace EyewearStore_SWP391.Services
{
    public interface IEmailService
    {
        Task SendRestockNotificationAsync(string toEmail, string customerName, string productName, string productUrl);
        Task SendEmailAsync(string toEmail, string subject, string body);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendRestockNotificationAsync(string toEmail, string customerName, string productName, string productUrl)
        {
            var subject = $"🎉 {productName} is Back in Stock!";

            var body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; margin: 0; padding: 0; background-color: #f4f4f4; }}
        .container {{ max-width: 600px; margin: 40px auto; background: white; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 12px rgba(0,0,0,0.1); }}
        .header {{ background: linear-gradient(135deg, #1a2332 0%, #2c4a6e 100%); padding: 40px 30px; text-align: center; }}
        .header h1 {{ color: white; margin: 0; font-size: 28px; }}
        .header p {{ color: rgba(255,255,255,0.9); margin: 10px 0 0 0; font-size: 16px; }}
        .content {{ padding: 40px 30px; }}
        .product-card {{ background: linear-gradient(135deg, #f8f9fa 0%, #e9ecef 100%); border-radius: 12px; padding: 30px; text-align: center; margin: 20px 0; border-left: 4px solid #d4a574; }}
        .product-name {{ font-size: 24px; font-weight: 700; color: #1a2332; margin-bottom: 15px; }}
        .cta-button {{ display: inline-block; background: linear-gradient(135deg, #d4a574 0%, #e8c7a0 100%); color: white; padding: 15px 40px; text-decoration: none; border-radius: 8px; font-weight: 600; font-size: 16px; margin-top: 20px; box-shadow: 0 4px 12px rgba(212, 165, 116, 0.3); }}
        .cta-button:hover {{ background: linear-gradient(135deg, #c69463 0%, #d4a574 100%); }}
        .footer {{ background: #f8f9fa; padding: 30px; text-align: center; color: #6c757d; font-size: 14px; }}
        .emoji {{ font-size: 48px; margin-bottom: 20px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>👓 Eyewear Store</h1>
            <p>Your wishlist item is back!</p>
        </div>
        
        <div class='content'>
            <div class='emoji'>🎉</div>
            <h2 style='color: #1a2332; text-align: center; margin-bottom: 20px;'>Great News, {customerName}!</h2>
            
            <p style='color: #495057; font-size: 16px; text-align: center; line-height: 1.6;'>
                The item you've been waiting for is <strong style='color: #d4a574;'>back in stock</strong>!
            </p>
            
            <div class='product-card'>
                <div class='product-name'>{productName}</div>
                <p style='color: #6c757d; margin: 0;'>✨ Limited quantity available</p>
                <p style='color: #6c757d; margin: 5px 0 0 0;'>⚡ Order now before it's gone again!</p>
            </div>
            
            <div style='text-align: center;'>
                <a href='{productUrl}' class='cta-button'>
                    🛒 Shop Now
                </a>
            </div>
            
            <p style='color: #868e96; font-size: 14px; text-align: center; margin-top: 30px;'>
                💡 Pro tip: Add to cart quickly - popular items sell out fast!
            </p>
        </div>
        
        <div class='footer'>
            <p style='margin: 0 0 10px 0;'>You're receiving this because you added this item to your wishlist.</p>
            <p style='margin: 0;'>© 2024 Eyewear Store. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";

            await SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            // Get email settings from configuration
            var smtpHost = _configuration["Email:SmtpHost"] ?? "smtp.gmail.com";
            var smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
            var fromEmail = _configuration["Email:FromEmail"] ?? "eyewearstore@gmail.com";
            var fromName = _configuration["Email:FromName"] ?? "Eyewear Store";
            var password = _configuration["Email:Password"] ?? "";

            using (var message = new MailMessage())
            {
                message.From = new MailAddress(fromEmail, fromName);
                message.To.Add(new MailAddress(toEmail));
                message.Subject = subject;
                message.Body = body;
                message.IsBodyHtml = true;

                using (var client = new SmtpClient(smtpHost, smtpPort))
                {
                    client.EnableSsl = true;
                    client.Credentials = new NetworkCredential(fromEmail, password);

                    await client.SendMailAsync(message);
                }
            }
        }
    }
}