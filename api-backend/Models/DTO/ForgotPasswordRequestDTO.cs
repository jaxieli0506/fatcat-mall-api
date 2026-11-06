using System.ComponentModel.DataAnnotations;

namespace api_backend.Models.DTO
{
    public class ForgotPasswordRequestDTO
    {
        [Required(ErrorMessage = "電子郵件為必填")]
        [EmailAddress(ErrorMessage = "請輸入有效的電子郵件格式")]
        public string Email { get; set; } = string.Empty;
    }
} 