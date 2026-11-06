// 檔案: rrDOTs/CampaignDto.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace api_backend.rrDOTs
{
    // 用於「回傳」給前端顯示的 DTO
    public class CampaignDto
    {
        public int CampaignId { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string CoverImageUrl { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsPublished { get; set; }
        public string CallToActionUrl { get; set; }
        public DateTime UploadedAt { get; set; }
        public string AdminName { get; set; } // 顯示管理員名稱
    }

    // 用於「新增」一個活動頁時，從前端接收的 DTO
    public class CampaignCreateDto
    {
        [Required]
        [StringLength(100)]
        public string Title { get; set; }

        public string Content { get; set; }

        [Required]
        public string CoverImageUrl { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [Required]
        public bool IsPublished { get; set; }

        public string CallToActionUrl { get; set; }

        public IFormFile? CoverImageFile { get; set; }

    }

    // 用於「修改」一個活動頁時，從前端接收的 DTO
    // 欄位可能跟 Create 一樣，但分開定義讓未來擴充更有彈性
    public class CampaignUpdateDto
    {
        [Required]
        [StringLength(100)]
        public string Title { get; set; }

        public string Content { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [Required]
        public bool IsPublished { get; set; }

        public string CallToActionUrl { get; set; }

        public IFormFile? CoverImageFile { get; set; }

    }
}