using api_backend.Models;
using api_backend.rrDOTs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace api_backend.Controllers
{
    // 這個 Controller 的路徑是 /api/Campaigns，專門給前台使用者讀取資料
    [Route("api/[controller]")]
    [ApiController]
    public class CampaignsController : ControllerBase
    {
        private readonly TeamDb2Context _context;

        public CampaignsController(TeamDb2Context context)
        {
            _context = context;
        }

        // GET: api/Campaigns
        // 取得所有「已發佈」且「在活動期間內」的活動列表
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CampaignDto>>> GetCampaigns()
        {
            var now = DateTime.Now;

            var campaigns = await _context.Campaigns
                // 篩選出符合前台顯示條件的活動
                .Where(c => c.IsPublished == true && c.StartDate <= now && c.EndDate >= now)
                .Include(c => c.Admin)
                .OrderByDescending(c => c.UploadedAt)
                .Select(c => new CampaignDto // 轉換成安全的 DTO
                {
                    CampaignId = c.CampaignId,
                    Title = c.Title,
                    Content = c.Content,
                    CoverImageUrl = c.CoverImageUrl,
                    StartDate = c.StartDate,
                    EndDate = c.EndDate,
                    IsPublished = c.IsPublished,
                    CallToActionUrl = c.CallToActionUrl,
                    UploadedAt = c.UploadedAt,
                    AdminName = c.Admin.Name
                })
                .ToListAsync();

            return Ok(campaigns);
        }

        // GET: api/Campaigns/5
        // 取得單一活動的詳細資料，同樣需要檢查是否符合顯示條件
        [HttpGet("{id}")]
        public async Task<ActionResult<CampaignDto>> GetCampaign(int id)
        {
            var now = DateTime.Now;

            var campaignDto = await _context.Campaigns
                .Where(c => c.CampaignId == id && c.IsPublished == true && c.StartDate <= now && c.EndDate >= now)
                .Include(c => c.Admin)
                .Select(c => new CampaignDto
                {
                    CampaignId = c.CampaignId,
                    Title = c.Title,
                    Content = c.Content,
                    CoverImageUrl = c.CoverImageUrl,
                    StartDate = c.StartDate,
                    EndDate = c.EndDate,
                    IsPublished = c.IsPublished,
                    CallToActionUrl = c.CallToActionUrl,
                    UploadedAt = c.UploadedAt,
                    AdminName = c.Admin.Name
                })
                .FirstOrDefaultAsync();

            if (campaignDto == null)
            {
                // 如果活動不存在，或是已過期/未發佈，都回傳 404
                return NotFound();
            }

            return Ok(campaignDto);
        }

        // 我們移除了 POST, PUT, DELETE 方法，因為這個控制器只應該提供公開的讀取功能。
    }
}
