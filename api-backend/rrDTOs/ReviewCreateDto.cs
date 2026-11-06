// 檔案: /rrDOTs/ReviewCreateDto.cs
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace api_backend.rrDOTs
{
    public class ReviewCreateDto
    {
        // 我們需要這個 OrderId 來做權限驗證
        [Required]
        public int OrderId { get; set; }

        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }

        [MaxLength(2000)]
        public string? Comment { get; set; }

        public List<IFormFile>? Files { get; set; }
    }
}
