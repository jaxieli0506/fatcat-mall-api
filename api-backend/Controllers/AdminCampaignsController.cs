using api_backend.Models;
using api_backend.rrDOTs; 
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO; // 👈 處理檔案路徑需要這個
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace api_backend.Controllers
{
    [Route("api/admin/[controller]")] //  建議加上 /admin 前綴，表明這是後台管理 API
    [ApiController]
    public class AdminCampaignsController : ControllerBase
    {

        private readonly TeamDb2Context _context;

        public AdminCampaignsController(TeamDb2Context context)
        {
            _context = context;
        }

        // GET: api/admin/Campaigns
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CampaignDto>>> GetCampaigns()
        {
            var campaigns = await _context.Campaigns
                .Include(c => c.Admin) // 載入關聯的 Admin 資料
                .OrderByDescending(c => c.UploadedAt)
                .Select(c => new CampaignDto // 轉換成 DTO
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
                    AdminName = c.Admin.Name // 從關聯物件取得 AdminName
                })
                .ToListAsync();

            return Ok(campaigns);
        }

        // GET: api/admin/Campaigns/5
        [HttpGet("{id}")]
        public async Task<ActionResult<CampaignDto>> GetCampaign(int id)
        {
            var campaign = await _context.Campaigns
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
                .FirstOrDefaultAsync(c => c.CampaignId == id);

            if (campaign == null)
            {
                return NotFound();
            }

            return Ok(campaign);
        }

        // PUT: api/admin/AdminCampaigns/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCampaign(int id, [FromForm] CampaignUpdateDto campaignUpdateDto)
        {
            // 1. 從資料庫中找到要修改的原始活動資料
            var campaign = await _context.Campaigns.FindAsync(id);
            if (campaign == null)
            {
                return NotFound();
            }

            // 2. 處理檔案上傳 (只有在管理者真的上傳了新檔案時，才執行這段)
            if (campaignUpdateDto.CoverImageFile != null && campaignUpdateDto.CoverImageFile.Length > 0)
            {
                // (可選) 在這裡可以先刪除舊的圖片檔案，以節省伺服器空間
                // var oldImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", campaign.CoverImageUrl.TrimStart('/'));
                // if (System.IO.File.Exists(oldImagePath))
                // {
                //     System.IO.File.Delete(oldImagePath);
                // }

                // 產生新檔名並儲存新檔案
                var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(campaignUpdateDto.CoverImageFile.FileName);
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "campaigns");
                Directory.CreateDirectory(uploadsFolder); // 確保資料夾存在
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await campaignUpdateDto.CoverImageFile.CopyToAsync(fileStream);
                }

                // 【關鍵】更新 C# 物件中的圖片路徑，EF Core 會偵測到這個變化
                campaign.CoverImageUrl = Path.Combine("/uploads/campaigns", uniqueFileName).Replace('\\', '/');
            }

            // 3. 從 DTO 更新其他的文字資料
            campaign.Title = campaignUpdateDto.Title;
            campaign.Content = campaignUpdateDto.Content;
            campaign.StartDate = campaignUpdateDto.StartDate;
            campaign.EndDate = campaignUpdateDto.EndDate;
            campaign.IsPublished = campaignUpdateDto.IsPublished;
            campaign.CallToActionUrl = campaignUpdateDto.CallToActionUrl;

            // 4. 將所有偵測到的變更 (包括可能更新的 CoverImageUrl 和其他文字欄位) 一次性儲存到資料庫
            await _context.SaveChangesAsync();

            return NoContent(); // 回傳 204，代表成功但無內容回傳
        }


        // POST: api/admin/AdminCampaigns
        [HttpPost]
        public async Task<ActionResult<CampaignDto>> PostCampaign([FromForm] CampaignCreateDto campaignCreateDto)
        {
            // 為了讓 POST 也能處理檔案，我們也需要修改它
            if (campaignCreateDto.CoverImageFile == null || campaignCreateDto.CoverImageFile.Length == 0)
            {
                return BadRequest("新增活動時必須上傳封面圖片。");
            }

            // 處理檔案儲存
            var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(campaignCreateDto.CoverImageFile.FileName);
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "campaigns");
            Directory.CreateDirectory(uploadsFolder);
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await campaignCreateDto.CoverImageFile.CopyToAsync(fileStream);
            }

            var adminId = 1; // 之後從 Token 拿

            var campaign = new Campaign
            {
                Title = campaignCreateDto.Title,
                Content = campaignCreateDto.Content,
                CoverImageUrl = Path.Combine("/uploads/campaigns", uniqueFileName).Replace('\\', '/'), // 使用儲存後的路徑
                StartDate = campaignCreateDto.StartDate,
                EndDate = campaignCreateDto.EndDate,
                IsPublished = campaignCreateDto.IsPublished,
                CallToActionUrl = campaignCreateDto.CallToActionUrl,
                UploadedAt = DateTime.UtcNow,
                AdminId = adminId
            };

            _context.Campaigns.Add(campaign);
            await _context.SaveChangesAsync();

            // 查詢剛新增的資料以包含 AdminName
            var newCampaign = await _context.Campaigns
                .Include(c => c.Admin)
                .FirstAsync(c => c.CampaignId == campaign.CampaignId);

            var newCampaignDto = new CampaignDto { /* ... 轉換邏輯 ... */ };

            return CreatedAtAction(nameof(GetCampaign), new { id = campaign.CampaignId }, newCampaignDto);
        }

        // DELETE: api/admin/Campaigns/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCampaign(int id)
        {
            var campaign = await _context.Campaigns.FindAsync(id);
            if (campaign == null)
            {
                return NotFound();
            }

            _context.Campaigns.Remove(campaign); // 活動頁通常可以真的刪除
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool CampaignExists(int id)
        {
            return _context.Campaigns.Any(e => e.CampaignId == id);
        }
    }
}
