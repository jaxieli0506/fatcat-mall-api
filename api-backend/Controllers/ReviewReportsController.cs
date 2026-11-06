using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using api_backend.Models;
using api_backend.rrDOTs;

namespace api_backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReviewReportsController : ControllerBase
    {
        private readonly TeamDb2Context _context;

        public ReviewReportsController(TeamDb2Context context)
        {
            _context = context;
        }

        // GET: api/ReviewReports
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ReviewReport>>> GetReviewReports()
        {
            return await _context.ReviewReports.ToListAsync();
        }

        // GET: api/ReviewReports/5
        //[HttpGet("{id}")]
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<ReportDto>>> GetUserReports(int userId)
        {
            // 這裡回傳你原本的 ReportDto，這樣就不會影響前台功能
            return await _context.ReviewReports
                .Where(r => r.UserId == userId)
                .Include(r => r.Review)
                .Include(r => r.ReasonType)
                .Include(r => r.ReportStatus)
                .Include(r => r.Admin)
                .Select(r => new ReportDto
                {
                    ReportId = r.ReportId,
                    CreatedAt = r.CreatedAt,
                    ReportedReviewId = r.ReviewId,
                    ReportedReviewComment = r.Review.Comment,
                    ReasonName = r.ReasonType.ReasonName,
                    ReasonComment = r.ReasonComment,
                    StatusId = r.ReportStatusId,
                    StatusName = r.ReportStatus.StatusName,
                    AdminName = r.Admin != null ? r.Admin.Name : null
                })
                .ToListAsync();
        }

        // PUT: api/ReviewReports/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutReviewReport(int id, ReviewReport reviewReport)
        {
            if (id != reviewReport.ReportId)
            {
                return BadRequest();
            }

            _context.Entry(reviewReport).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ReviewReportExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/ReviewReports
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<ReviewReport>> PostReviewReport(ReviewReport reviewReport)
        {
            _context.ReviewReports.Add(reviewReport);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetReviewReport", new { id = reviewReport.ReportId }, reviewReport);
        }

        // DELETE: api/ReviewReports/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteReviewReport(int id)
        {
            var reviewReport = await _context.ReviewReports.FindAsync(id);
            if (reviewReport == null)
            {
                return NotFound();
            }

            _context.ReviewReports.Remove(reviewReport);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ReviewReportExists(int id)
        {
            return _context.ReviewReports.Any(e => e.ReportId == id);
        }


        // --- 以下是為「後台管理」新增的端點 ---

        // GET: api/ReviewReports/admin-view
        // 這個端點專門給後台管理頁面使用，回傳管理者需要的完整資訊

        // 檔案: ReviewReportsController.cs
        // 檔案: api_backend/Controllers/ReviewReportsController.cs
        // 檔案: /Controllers/ReviewReportsController.cs

        [HttpGet("admin-view")]
        public async Task<ActionResult<IEnumerable<AdminReviewReportDto>>> GetReportsForAdmin()
        {
            try
            {
                // 1. 建立基礎查詢，直接查詢 ReviewReports 主表
                var query = _context.ReviewReports.AsQueryable();

                // 2. 使用手動 JOIN 來確保所有資料都被撈取
                //    DefaultIfEmpty() 的作用就是實現 LEFT JOIN
                var result = await (
                    from report in query

                    join user in _context.Users on report.UserId equals user.UserId into userGroup
                    from u in userGroup.DefaultIfEmpty()

                    join review in _context.Reviews on report.ReviewId equals review.ReviewId into reviewGroup
                    from rev in reviewGroup.DefaultIfEmpty()

                    join reasonType in _context.ReportReasonTypes on report.ReasonTypeId equals reasonType.ReasonTypeId into reasonTypeGroup
                    from rt in reasonTypeGroup.DefaultIfEmpty()

                    join status in _context.ReportStatuses on report.ReportStatusId equals status.ReportStatusId into statusGroup
                    from s in statusGroup.DefaultIfEmpty()

                    join admin in _context.Admins on report.AdminId equals admin.AdminId into adminGroup
                    from adm in adminGroup.DefaultIfEmpty()

                    orderby report.CreatedAt descending

                    // 3. 將結果投影到 DTO，並做 null 檢查，讓程式更健壯
                    select new AdminReviewReportDto
                    {
                        ReportId = report.ReportId,
                        ReviewId = report.ReviewId,
                        ReviewContent = rev != null ? rev.Comment : "（沒有評論）",
                        UserId = report.UserId,
                        ReporterUserName = u != null ? u.Name : "（使用者不存在）",
                        ReasonName = rt != null ? rt.ReasonName : "（原因不存在）",
                        ReasonComment = report.ReasonComment,
                        CreatedAt = report.CreatedAt,
                        ReportStatusId = report.ReportStatusId,
                        StatusName = s != null ? s.StatusName : "（狀態不存在）",
                        AdminId = report.AdminId,
                        AdminUserName = adm != null ? adm.Name : "N/A",
                        ProcessedAt = report.ProcessedAt
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


        //審核檢舉通過或不通過
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateReportStatus(int id, [FromBody] UpdateReportStatusDto dto)
        {
            // 1. 根據 ID 找出要更新的檢舉紀錄
            var reviewReport = await _context.ReviewReports
                // ★☆ jj 新增,要寫入通知資料表
                .Include(r => r.User) //包含 User 資訊
                .Include(r => r.Review) // 包含 Review 資訊
                .Include(r => r.Admin) // 包含 ReportStatus 資訊
                .FirstOrDefaultAsync(r=>r.ReportId == id);

            if (reviewReport == null)
            {
                return NotFound("找不到指定的檢舉紀錄。");
            }

            // 2. 更新欄位
            reviewReport.ReportStatusId = dto.NewStatusId;
            reviewReport.AdminId = dto.AdminId;
            reviewReport.ProcessedAt = DateTime.Now;
            // 2.5. 檢查檢舉是否「成立」( StatusId = 1 代表成立)
            if (dto.NewStatusId == 1)
            {
                // 如果檢舉成立，就去找出對應的評論並進行軟刪除
                // 因為前面已經 Include(r => r.Review)，所以可以直接從 reviewReport.Review 取得
                var targetReview = reviewReport.Review;

                if (targetReview != null)
                {
                    // 軟刪除：將評論的 Status 設為 false (隱藏)
                    targetReview.Status = false;
                    // 我們可以順便記錄是哪個管理員處理的
                    targetReview.AdminId = dto.AdminId;
                }
                else
                {
                    // 防呆：如果找不到對應的評論（可能已被刪除），可以記錄一個 Log
                    // 在此情境下不阻斷流程，繼續更新檢舉狀態即可
                    Console.WriteLine($"警告：檢舉 #{id} 成立，但找不到對應的評論 #{reviewReport.ReviewId}。");
                }
            }
            try
            {
                // 3. 儲存變更到資料庫
                await _context.SaveChangesAsync();

                // ★☆ jj 新增,要寫入通知資料表
                // 4. 如果狀態是「成立」(1) 或「不成立」(2)，則發送通知給檢舉人
                if (dto.NewStatusId == 1 || dto.NewStatusId == 2)
                {
                    await SendReviewReportNotification(reviewReport, dto.NewStatusId);
                }
            }
            catch (DbUpdateException ex)
            {
                // 如果儲存失敗，回傳錯誤
                return StatusCode(500, $"更新資料庫時發生錯誤: {ex.Message}");
            }

            // 4. 回傳成功 (NoContent 通常用於 PUT/DELETE 成功後)
            return NoContent();
        }

        /// <summary>
        /// 發送評論檢舉處理結果通知給檢舉人 ★☆ jj 新增
        /// </summary>
        /// <param name="reviewReport">檢舉紀錄</param>
        /// <param name="statusId">處理結果狀態ID</param>
        private async Task SendReviewReportNotification(ReviewReport reviewReport, int statusId)
        {
            try
            {
                // 1. 建立通知內容
                string statusText = statusId == 1 ? "成立" : "不成立";
                string title = $"評論檢舉處理結果通知";
                string description = $"您於 {reviewReport.CreatedAt:yyyy/MM/dd HH:mm} 檢舉的評論已處理完成，審核結果為「{statusText}」。";

                // 2. 建立 Notification 記錄
                var notification = new Notification
                {
                    Title = title,
                    Description = description,
                    NotificationType = 9, // 評論相關通知
                    Time = DateTime.Now,
                    AdminId = reviewReport.AdminId ?? 1 // 使用處理的管理員ID，如果沒有則使用預設值
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync(); // 先儲存以取得 NotificationId

                // 3. 建立 NotificationReceiver 記錄，發送給檢舉人
                var notificationReceiver = new NotificationReceiver
                {
                    NotificationId = notification.NotificationId,
                    UserId = reviewReport.UserId, // 發送給檢舉人
                    IsRead = false
                };

                _context.NotificationReceivers.Add(notificationReceiver);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex) 
            {
                // 記錄錯誤但不中斷主要流程
                // 在實際環境中，你可能會想要記錄到日誌系統
                Console.WriteLine($"發送評論檢舉通知時發生錯誤: {ex.Message}");
            }
        }


    }

}