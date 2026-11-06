// src/rrDOTs/AdminLoginDto.cs
using System.ComponentModel.DataAnnotations;

namespace api_backend.rrDOTs
{
    public class AdminLoginDto
    {
        [Required, EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }

        // 👇 新增這個屬性
        public bool RememberMe { get; set; } = false; // 預設為 false  
    }
}