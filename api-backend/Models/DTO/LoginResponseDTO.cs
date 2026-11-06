namespace api_backend.Models.DTO
{
    public class LoginResponseDTO
    {
        public string Token { get; set; }           // JWT Token (不儲存在DB)
        public int UserId { get; set; }             // 來自User.UserId
        public string Name { get; set; }            // 來自User.Name  
        public string Email { get; set; }           // 來自User.Email
        public DateTime ExpiresAt { get; set; }     // Token到期時間
        public string? PicPath { get; set; }

    }
}
