// 檔案: /rrDOTs/ComplaintReplyDto.cs (用於接收管理者「回覆」的內容)
using System.ComponentModel.DataAnnotations;

namespace api_backend.rrDOTs
{
    public class ComplaintReplyDto
    {
        [Required(ErrorMessage = "回覆內容不可為空。")]
        [MaxLength(4000)]
        public string ResponseText { get; set; }

        [Required]
        public int AdminId { get; set; }
    }
}