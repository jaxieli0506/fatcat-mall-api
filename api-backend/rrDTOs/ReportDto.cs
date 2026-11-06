namespace api_backend.rrDOTs
{
    public class ReportDto
    {
        public int ReportId { get; set; }
        public DateTime CreatedAt { get; set; }

        // 被檢舉的評論內容 (讓使用者知道是哪一則)
        public int ReportedReviewId { get; set; }
        public string? ReportedReviewComment { get; set; }

        // 檢舉的細節
        public string? ReasonName { get; set; }
        public string? ReasonComment { get; set; }

        // 處理進度
        public int StatusId { get; set; }
        public string? StatusName { get; set; }
        public string? AdminName { get; set; }
    }
}