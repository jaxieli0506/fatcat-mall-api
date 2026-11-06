//  rrDOTs/LogDto.cs「日誌資料傳輸物件 (LogDto)」
namespace api_backend.rrDOTs
{
    public class LogDto
    {
        public int LogId { get; set; }
        public string AdminName { get; set; } // 我們要顯示名稱，而不是 ID
        public string Action { get; set; }
        public string TargetTable { get; set; }
        public int TargetId { get; set; }
        public DateTime Time { get; set; }
    }
}