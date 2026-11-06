// 檔案: /rrDOTs/AdminComplaintDto.cs
namespace api_backend.rrDOTs
{
    public class AdminComplaintDto
    {
        public int ComplaintId { get; set; }
        public int OrderId { get; set; }
        public string Subject { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public string ResponseText { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public List<AttachmentDto> Attachments { get; set; }

        // --- 關聯資料 ---
        public int UserId { get; set; }
        public string UserName { get; set; } // 來自 User 表
        public string ComplaintTypeName { get; set; } // 來自 ComplaintType 表
        public int ComplaintStatusId { get; set; }
        public string ComplaintStatusName { get; set; } // 來自 ComplaintStatus 表
        public int? AdminId { get; set; }
        public string AdminName { get; set; } // 來自 Admin 表
    }
}