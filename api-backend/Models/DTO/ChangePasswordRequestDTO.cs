using System.ComponentModel.DataAnnotations;

namespace api_backend.Models.DTO
{
    public class ChangePasswordRequestDTO
    {
        [Required(ErrorMessage = "目前密碼為必填")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "新密碼為必填")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "密碼長度必須為6-100個字元")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "確認密碼為必填")]
        [Compare("NewPassword", ErrorMessage = "確認密碼與新密碼不符")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
