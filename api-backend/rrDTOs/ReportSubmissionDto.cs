// 檔案路徑: /rrDOTs/ReportSubmissionDto.cs

namespace api_backend.rrDOTs 
{
    /// <summary>
    /// 用於接收使用者從前台提交評論檢舉時的資料模型。
    /// </summary>
    public class ReportSubmissionDto
    {
        /// <summary>
        /// 對應到使用者選擇的「檢舉原因」下拉選單的 ID。
        /// </summary>
        public int ReasonTypeId { get; set; }

        /// <summary>
        /// 對應到使用者填寫的「補充說明」文字 (此欄位可為空)。
        /// </summary>
        public string? ReasonComment { get; set; }
    }
}