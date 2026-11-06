using System;
using System.ComponentModel.DataAnnotations;

namespace api_backend.Models.DTO
{
    //會員列表
    public class AdminMemberDto 
    {
        public int UserId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public int? Gender { get; set; }
        public DateTime? Birthdate { get; set; }
        public int Status { get; set; }
        public DateTime Registerdate { get; set; }
        public DateTime? LastLogin { get; set; }
        public bool IsThirdparty { get; set; }
    }

    public class CreateMemberDto
    {
        [Required(ErrorMessage = "姓名是必填項目")]
        public string Name { get; set; }
        
        [Required(ErrorMessage = "Email是必填項目")]
        [EmailAddress(ErrorMessage = "Email格式錯誤")]
        public string Email { get; set; }
        
        public string? Phone { get; set; }
        public int? Gender { get; set; }
        public DateTime? Birthdate { get; set; }
    }

    public class UpdateMemberDto
    {
        [Required(ErrorMessage = "姓名是必填項目")]
        public string Name { get; set; }
        
        [Required(ErrorMessage = "Email是必填項目")]
        [EmailAddress(ErrorMessage = "Email格式錯誤")]
        public string Email { get; set; }
        
        public string? Phone { get; set; }
        public int? Gender { get; set; }
        public DateTime? Birthdate { get; set; }
        public int Status { get; set; }
    }

    public class UpdateStatusDto
    {
        public int Status { get; set; }
    }
} 