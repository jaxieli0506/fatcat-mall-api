using System.ComponentModel.DataAnnotations;

namespace api_backend.Models.DTO
{
    public class ResetPasswordRequestDTO
    {
        [Required(ErrorMessage = "重設令牌為必填")]
        public string Token { get; set; } = string.Empty;

        [Required(ErrorMessage = "電子郵件為必填")]
        [EmailAddress(ErrorMessage = "請輸入有效的電子郵件格式")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "新密碼為必填")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "密碼長度必須為6-100個字元")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "確認密碼為必填")]
        [Compare("NewPassword", ErrorMessage = "確認密碼與新密碼不符")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
} 