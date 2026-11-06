//  (用於顯示評論)
using System.ComponentModel.DataAnnotations;

namespace api_backend.rrDOTs
{
    public class ReviewDto
    {
        public int ReviewId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } // 來自關聯的 Product Table
        public int UserId { get; set; }
        public string UserName { get; set; } // 來自關聯的 User Table
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool Status { get; set; }
        public string HiddenReason { get; set; } // 可以是 null

        // 官方回應相關
        public string? Response { get; set; }
        public string? AdminName { get; set; } // 來自關聯的 Admin Table
        public DateTime? RespondedAt { get; set; }

        // 附件列表
        public List<ReviewAttachmentDto> Attachments { get; set; } = new List<ReviewAttachmentDto>();

        // 有幫助 沒幫助
        public int HelpfulnessCount { get; set; }
        // 用 nullable bool (bool?) 來表示三種狀態：
        // true:  當前使用者投了「讚」
        // false: 當前使用者投了「倒讚」
        // null:  當前使用者「未投票」
        public bool? CurrentUserVote { get; set; }
    }

    public class ReviewAttachmentDto
    {
        public string FilePath { get; set; }
        public string FileName { get; set; }
    }

    //以下檢舉
    public class ReportReasonDto
    {
        public int ReasonTypeId { get; set; }
        public string ReasonName { get; set; }
    }

    public class ReviewReportCreateDto
    {
        [Required]
        public int ReasonTypeId { get; set; }

        [StringLength(500)]
        public string? ReasonComment { get; set; }
    }
}