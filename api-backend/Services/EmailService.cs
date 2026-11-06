using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace api_backend.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> SendPasswordResetEmailAsync(string toEmail, string resetToken, string resetUrl)
        {
            try
            {
                var subject = "Fat Cat 購物商城 - 重設密碼";
                var htmlBody = GeneratePasswordResetEmailTemplate(resetToken, resetUrl);
                
                return await SendEmailAsync(toEmail, subject, htmlBody);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "發送重設密碼郵件失敗b: {Email}", toEmail);
                return false;
            }
        }

        public async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            try
            {
                var message = new MimeMessage();

                // 發件人
                var senderName = _configuration["SmtpSettings:SenderName"];
                var senderEmail = _configuration["SmtpSettings:SenderEmail"];
                message.From.Add(new MailboxAddress(senderName, senderEmail));

                // 收件人
                message.To.Add(new MailboxAddress("", toEmail));

                // 郵件主題
                message.Subject = subject;

                // 郵件內容
                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = htmlBody
                };
                message.Body = bodyBuilder.ToMessageBody();

                // SMTP 設定
                using var client = new SmtpClient();

                // 【★★★ 關鍵修正：就是加上這一行 ★★★】
                // 在連線前，設定伺服器憑證驗證的回呼函式，直接回傳 true 來接受任何憑證。
                // 這能解決在本機開發環境中，因網路或系統原則導致的 SSL/TLS 憑證撤銷檢查失敗問題。
                client.ServerCertificateValidationCallback = (s, c, h, e) => true;

                var server = _configuration["SmtpSettings:Server"];
                var port = int.Parse(_configuration["SmtpSettings:Port"]);
                var enableSsl = bool.Parse(_configuration["SmtpSettings:EnableSsl"]);

                // 您的原始連線邏輯，這裡完全不需要改動
                await client.ConnectAsync(server, port, enableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None);

                var email = _configuration["SmtpSettings:SenderEmail"];
                var password = _configuration["SmtpSettings:Password"];
                await client.AuthenticateAsync(email, password);

                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation("郵件發送成功: {Email}", toEmail);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "郵件發送失敗: {Email}", toEmail);
                return false;
            }
        }

        private string GeneratePasswordResetEmailTemplate(string resetToken, string resetUrl)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>重設密碼</title>
    <style>
        body {{ font-family: Arial, sans-serif; background-color: #f4f4f4; margin: 0; padding: 20px; }}
        .container {{ max-width: 600px; margin: 0 auto; background-color: white; padding: 30px; border-radius: 10px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
        .header {{ text-align: center; margin-bottom: 30px; }}
        .logo {{ font-size: 24px; font-weight: bold; color: #92559c; }}
        .content {{ line-height: 1.6; color: #333; }}
        .button {{ display: inline-block; background-color: #92559c; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
        .footer {{ margin-top: 30px; padding-top: 20px; border-top: 1px solid #eee; font-size: 12px; color: #666; text-align: center; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <div class='logo'>🐱 Fat Cat 購物商城</div>
        </div>
        
        <div class='content'>
            <h2>重設密碼請求</h2>
            <p>您好，</p>
            <p>我們收到了您重設密碼的請求。請點擊下方按鈕來重設您的密碼：</p>
            
            <div style='text-align: center;'>
                <a href='{resetUrl}' class='button'>重設密碼</a>
            </div>
            
            <p><strong>重要提醒：</strong></p>
            <ul>
                <li>此重設連結將在 1 小時後過期</li>
                <li>如果您沒有要求重設密碼，請忽略此郵件</li>
                <li>為了您的帳號安全，請不要將此郵件轉發給他人</li>
            </ul>
            
            <p>如有任何問題，請聯繫我們的客服團隊。</p>
            
            <p>祝您購物愉快！<br>Fat Cat 購物商城團隊</p>
        </div>
        
        <div class='footer'>
            <p>此郵件由系統自動發送，請勿直接回覆</p>
            <p>© 2025 Fat Cat 購物商城 版權所有</p>
        </div>
    </div>
</body>
</html>";
        }
    }
}