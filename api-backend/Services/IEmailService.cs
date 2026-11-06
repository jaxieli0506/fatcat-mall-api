namespace api_backend.Services
{
    public interface IEmailService
    {
        // 發送重設密碼郵件
        // name="toEmail"收件人信箱
        // name="resetToken"重設令牌
        // name="resetUrl"重設密碼的完整URL
        // 發送是否成功
        Task<bool> SendPasswordResetEmailAsync(string toEmail, string resetToken, string resetUrl);

        // 發送一般郵件
        //  name="toEmail" 收件人信箱
        //  name="subject" 郵件主旨
        //  name="htmlBody" HTML格式的郵件內容
        //  發送是否成功
        Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody);
    }
} 