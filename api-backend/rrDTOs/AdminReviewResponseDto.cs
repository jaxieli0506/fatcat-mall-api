//  AdminReviewResponseDto.cs  (用於管理員回覆)
using System.ComponentModel.DataAnnotations;

namespace api_backend.rrDOTs
{
    public class AdminReviewResponseDto
    {
        [Required]
        public bool Status { get; set; } // 公開或隱藏

        [MaxLength(2000)]
        public string? Response { get; set; }
    }
}