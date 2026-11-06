// 檔案: /rrDOTs/ComplaintDto.cs (用於顯示管理者後台的申訴列表)
namespace api_backend.rrDOTs
{
    public class ComplaintDto
    {
        public int ComplaintId { get; set; }
        public int OrderId { get; set; }
        public string Subject { get; set; }
        public string Description { get; set; }
        public string ComplaintTypeName { get; set; } // 來自 ComplaintType 表
        public string ComplaintStatusName { get; set; } // 來自 ComplaintStatus 表
        public DateTime CreatedAt { get; set; }
        public string ResponseText { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public List<AttachmentDto> Attachments { get; set; }
    }
}