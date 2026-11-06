using System;
using System.ComponentModel.DataAnnotations;

namespace api_backend.Models.DTO
{
    //管理員列表
    public class AdminSettingsDTO
    {
        public int AdminId { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public string Phone { get; set; }

        [Required]
        public string Role { get; set; }

        public int Status { get; set; }

        public string statusText => Status switch
        {
            0 => "停用",
            1 => "啟用",
            2 => "已刪除",
            _ => "未設定"
        };
    }

    // 管理員新增
    public class AdminCreateDTO
    {
        [Required(ErrorMessage = "Email是必填項目")]
        [EmailAddress(ErrorMessage = "Email格式錯誤")]
        public string Email { get; set; }

        [Required(ErrorMessage ="密碼為必填欄位")]
        [MinLength(6, ErrorMessage = "密碼長度至少為6個字元")]
        public string Password { get; set; }

        [Required(ErrorMessage = "姓名是必填項目")]
        public string Name { get; set; }

        [Required(ErrorMessage = "電話是必填項目")]
        [Phone(ErrorMessage = "電話格式錯誤")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "角色是必填項目")]
        public string Role { get; set; }
    }

    // 管理員更新
    public class AdminUpdateDTO
    {
        [Required(ErrorMessage = "Email是必填項目")]
        [EmailAddress(ErrorMessage = "Email格式錯誤")]
        public string Email { get; set; }

        [Required(ErrorMessage = "姓名是必填項目")]
        public string Name { get; set; }

        [Required(ErrorMessage = "電話是必填項目")]
        [Phone(ErrorMessage = "電話格式錯誤")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "角色是必填項目")]
        public string Role { get; set; }

        public int Status { get; set; }
    }

    // 管理員列表查詢參數
    public class AdminQueryDTO
    {
        public string? SearchKeyword { get; set; }
        public string? SortBy { get; set; } = "AdminId";
        public string? SortOrder { get; set; } = "asc";
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    // 管理員列表回應
    public class AdminListResponseDTO
    {
        public List<AdminSettingsDTO> Admins { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; }
    }
}
