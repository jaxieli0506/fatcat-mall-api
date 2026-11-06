// 檔案: /rrDOTs/ComplaintCreateDto.cs
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace api_backend.rrDOTs
{
    public class ComplaintCreateDto
    {
        public int OrderId { get; set; }

        [Required(ErrorMessage = "申訴主旨為必填項。")]
        [MaxLength(200)]
        public string Subject { get; set; }

        [Required(ErrorMessage = "申訴類型為必選項。")]
        public int ComplaintTypeId { get; set; }

        [Required(ErrorMessage = "問題描述為必填項。")]
        [MaxLength(4000)]
        public string Description { get; set; }

        // 讓前端可以透過 form-data 的形式上傳一或多個檔案
        public List<IFormFile>? Attachments { get; set; }
    }
}