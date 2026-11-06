// 檔案: /rrDOTs/UpdateComplaintStatusDto.cs(用於接收管理者「手動變更狀態」的指令)  ComplantContrlller的PUT方法
using System.ComponentModel.DataAnnotations;

namespace api_backend.rrDOTs
{
    public class UpdateComplaintStatusDto
    {
        [Required]
        public int NewStatusId { get; set; }

        [Required]
        public int AdminId { get; set; }
    }
}