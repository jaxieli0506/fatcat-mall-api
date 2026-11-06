using api_backend.Models;
using api_backend.rrDOTs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace api_backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ComplaintsController : ControllerBase
    {
        private readonly TeamDb2Context _context;

        public ComplaintsController(TeamDb2Context context)
        {
            _context = context;
        }

        // --- Endpoint 1: 取得當前使用者的所有申訴紀錄 ---
        [HttpGet("my-complaints")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<ComplaintDto>>> GetMyComplaints()
        {
            
            var userIdStr = User.FindFirst("UserId")?.Value;
            if (!int.TryParse(userIdStr, out int currentUserId))
            {
                return Unauthorized("無法識別使用者身分。");
            }

            var complaints = await _context.Complaints
                .Where(c => c.UserId == currentUserId)
                .Include(c => c.ComplaintType)
                .Include(c => c.ComplaintStatus)
                .Include(c => c.ComplaintAttachments)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new ComplaintDto
                {
                    ComplaintId = c.ComplaintId,
                    OrderId = c.OrderId,
                    Subject = c.Subject,
                    Description = c.Description,
                    ComplaintTypeName = c.ComplaintType.TypeName,
                    ComplaintStatusName = c.ComplaintStatus.StatusName,
                    CreatedAt = c.CreatedAt,
                    ResponseText = c.ResponseText,
                    ResolvedAt = c.ResolvedAt,
                    // 將載入的附件資料，轉換成 DTO 並放進去
                    Attachments = c.ComplaintAttachments.Select(a => new AttachmentDto
                    {
                        AttachmentId = a.AttachmentId,
                        FilePath = a.FilePath
                    }).ToList()
                })
                .ToListAsync();

            return Ok(complaints);
        }

        // --- Endpoint 2: 取得所有可用的申訴類型 ---
        [HttpGet("types")]
        public async Task<ActionResult<IEnumerable<ComplaintType>>> GetComplaintTypes()
        {
            return await _context.ComplaintTypes
                .Where(ct => ct.IsActive)
                .ToListAsync();
        }

        // --- Endpoint 3: 提交一個新的申訴 ---
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<Complaint>> CreateComplaint([FromForm] ComplaintCreateDto createDto)
        {
            //   【統一使用你專案中正確的 Claim 名稱 "UserId"】
            var userIdStr = User.FindFirst("UserId")?.Value;

            if (!int.TryParse(userIdStr, out int currentUserId))
            {
                return Unauthorized("無法識別使用者身分。");
            }

            var orderExists = await _context.Orders
                .AnyAsync(o => o.OrderId == createDto.OrderId && o.UserId == currentUserId);
            if (!orderExists)
            {
                return BadRequest("指定的訂單不存在或不屬於您。");
            }

            var complaint = new Complaint
            {
                UserId = currentUserId,
                OrderId = createDto.OrderId,
                Subject = createDto.Subject,
                ComplaintTypeId = createDto.ComplaintTypeId,
                Description = createDto.Description,
                ComplaintStatusId = 0, // 0 = 待處理
                CreatedAt = DateTime.Now
            };

            _context.Complaints.Add(complaint);
            await _context.SaveChangesAsync();

            //          【核心修正補上完整的檔案儲存邏輯】
            if (createDto.Attachments != null && createDto.Attachments.Count > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "complaints");
                Directory.CreateDirectory(uploadsFolder);

                foreach (var file in createDto.Attachments)
                {
                    if (file.Length > 0)
                    {
                        var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(fileStream);
                        }

                        var attachment = new ComplaintAttachment
                        {
                            ComplaintId = complaint.ComplaintId, // 關聯到剛剛建立的 Complaint
                            FilePath = Path.Combine("/uploads/complaints", uniqueFileName).Replace('\\', '/'),
                            FileName = file.FileName,
                            FileType = file.ContentType,
                            UploadedUserId = currentUserId, // 記錄是哪個使用者上傳的
                            UploadedAt = DateTime.Now
                        };
                        _context.ComplaintAttachments.Add(attachment);
                    }
                }
                await _context.SaveChangesAsync(); // 第二次儲存，寫入所有附件的紀錄
            }

            return CreatedAtAction(nameof(GetMyComplaints), new { id = complaint.ComplaintId }, complaint);
        }

        // ====================================================================
        // Endpoint 4: 【管理者專用】獲取所有申訴列表
        // GET: /api/complaints/admin-view
        // ====================================================================
        [HttpGet("admin-view")]
        [Authorize] // 假設只有管理者能存取
        public async Task<ActionResult<IEnumerable<AdminComplaintDto>>> GetComplaintsForAdmin()
        {
            try
            {
                // 1. 建立基礎查詢，直接查詢 Complaints 主表
                var query = _context.Complaints.AsQueryable();

                // 2. 使用手動 JOIN 來確保所有資料都被撈取
                //    DefaultIfEmpty() 的作用就是實現 LEFT JOIN，確保主表資料不會因關聯資料不存在而遺失
                var result = await (
                    from complaint in query

                        // LEFT JOIN Users
                    join user in _context.Users on complaint.UserId equals user.UserId into userGroup
                    from u in userGroup.DefaultIfEmpty()

                        // LEFT JOIN ComplaintTypes
                    join type in _context.ComplaintTypes on complaint.ComplaintTypeId equals type.ComplaintTypeId into typeGroup
                    from ct in typeGroup.DefaultIfEmpty()

                        // LEFT JOIN ComplaintStatuses
                    join status in _context.ComplaintStatuses on complaint.ComplaintStatusId equals status.ComplaintStatusId into statusGroup
                    from cs in statusGroup.DefaultIfEmpty()

                        // LEFT JOIN Admins
                    join admin in _context.Admins on complaint.AdminId equals admin.AdminId into adminGroup
                    from adm in adminGroup.DefaultIfEmpty()

                    orderby complaint.CreatedAt descending

                    // 3. 將結果投影到 DTO，並做 null 檢查，讓程式更健壯
                    select new AdminComplaintDto
                    {
                        ComplaintId = complaint.ComplaintId,
                        OrderId = complaint.OrderId,
                        Subject = complaint.Subject,
                        Description = complaint.Description,
                        CreatedAt = complaint.CreatedAt,
                        ResponseText = complaint.ResponseText,
                        ResolvedAt = complaint.ResolvedAt,

                        UserId = complaint.UserId,
                        UserName = u != null ? u.Name : "（使用者不存在）",

                        ComplaintTypeName = ct != null ? ct.TypeName : "（類型不存在）",

                        ComplaintStatusId = complaint.ComplaintStatusId,
                        ComplaintStatusName = cs != null ? cs.StatusName : "（狀態不存在）",

                        AdminId = complaint.AdminId,
                        AdminName = adm != null ? adm.Name : "N/A",

                        // 【修改點】在這裡加入一個子查詢來獲取附件
                        Attachments = (from att in _context.ComplaintAttachments
                                       where att.ComplaintId == complaint.ComplaintId
                                       select new AttachmentDto
                                       {
                                           AttachmentId = att.AttachmentId,
                                           FilePath = att.FilePath
                                       }).ToList()
                                        }
                            ).ToListAsync();


                return Ok(result);
            }
            catch (Exception ex)
            {
                // 如果發生任何預期外的錯誤，回傳詳細訊息方便除錯
                return StatusCode(500, $"後端查詢時發生錯誤: {ex.Message}");
            }
        }

        // ====================================================================
        // Endpoint 5: 【管理者專用】回覆一則申訴
        // 這會自動將狀態更新為「已回覆」
        // POST: /api/complaints/{id}/reply
        // ====================================================================
        [HttpPost("{id}/reply")]
        [Authorize]
        public async Task<IActionResult> ReplyToComplaint(int id, [FromBody] ComplaintReplyDto replyDto)
        {
            var complaint = await _context.Complaints.FindAsync(id);
            if (complaint == null)
            {
                return NotFound("找不到指定的申訴案件。");
            }

            complaint.ResponseText = replyDto.ResponseText;
            complaint.AdminId = replyDto.AdminId;
            complaint.ComplaintStatusId = 1; // 自動更新狀態為 1 (已回覆)
            complaint.ResolvedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // ====================================================================
        // Endpoint 6: 【管理者專用】手動更新申訴狀態
        // 用於將案件手動改為「已結案」等狀態
        // PUT: /api/complaints/{id}/status
        // ====================================================================
        [HttpPut("{id}/status")]
        [Authorize]
        public async Task<IActionResult> UpdateComplaintStatus(int id, [FromBody] UpdateComplaintStatusDto statusDto)
        {
            var complaint = await _context.Complaints.FindAsync(id);
            if (complaint == null)
            {
                return NotFound("找不到指定的申訴案件。");
            }

            complaint.ComplaintStatusId = statusDto.NewStatusId;
            complaint.AdminId = statusDto.AdminId; // 記錄是哪個管理者操作的
                                                   // 如果是結案，也更新處理時間
            if (statusDto.NewStatusId == 2) // 假設 2 = 已結案
            {
                complaint.ResolvedAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
