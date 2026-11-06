namespace api_backend.Models.DTO
{
    public class ExternalLoginResponseDTO
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string LoginProvider { get; set; } // 登入提供者,ex: Google, Facebook
        public bool IsNewBinding { get; set; } // 是否為新綁定
    }
}
