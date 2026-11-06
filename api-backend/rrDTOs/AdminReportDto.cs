// In /rrDOTs/AdminReviewReportDto.cs
namespace api_backend.rrDOTs
{
    // 這個 DTO 專門給後台管理介面使用
    public class AdminReviewReportDto
    {
        public int ReportId { get; set; }
        public int ReviewId { get; set; }
        public string ReviewContent { get; set; } // 來自 Review Table
        public int UserId { get; set; }
        public string ReporterUserName { get; set; } // 【重要】來自 User Table，管理者需要知道是誰檢舉的
        public string ReasonName { get; set; } // 來自 ReportReasonType Table
        public string ReasonComment { get; set; }
        public DateTime CreatedAt { get; set; }
        public int ReportStatusId { get; set; }
        public string StatusName { get; set; } // 來自 ReportStatus Table
        public int? AdminId { get; set; }
        public string AdminUserName { get; set; } // 來自 Admin Table
        public DateTime? ProcessedAt { get; set; }
    }
}