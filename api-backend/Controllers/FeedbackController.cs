using api_backend.Models;
using api_backend.rrDOTs; // 確保你的 DTO 命名空間正確
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace api_backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FeedbackController : ControllerBase
    {
        private readonly TeamDb2Context _context;

        public FeedbackController(TeamDb2Context context)
        {
            _context = context;
        }

        //GET: api/feedback/reports
        //檔案：Controllers/FeedbackController.cs
       [HttpGet("reports")]
       [Authorize]
        public async Task<ActionResult<IEnumerable<ReportDto>>> GetMyReports()
        {
            //var currentUserId = 2;

            var userIdStr = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var currentUserId))
            {
                return Unauthorized("無效的 Token 或找不到使用者 ID。");
            }

            // --- 👇👇👇【修正版的 Join 邏輯】👇👇👇 ---
            var query = from report in _context.ReviewReports
                        join review in _context.Reviews on report.ReviewId equals review.ReviewId into reviewGroup
                        from rev in reviewGroup.DefaultIfEmpty()
                        join reason in _context.ReportReasonTypes on report.ReasonTypeId equals reason.ReasonTypeId into reasonGroup
                        from rea in reasonGroup.DefaultIfEmpty()
                        join status in _context.ReportStatuses on report.ReportStatusId equals status.ReportStatusId into statusGroup
                        from sta in statusGroup.DefaultIfEmpty()

                            // 修正點：JOIN 的來源是 report.AdminId，而不是 sta.AdminId
                        join admin in _context.Admins on report.AdminId equals admin.AdminId into adminGroup
                        from adm in adminGroup.DefaultIfEmpty()

                        where report.UserId == currentUserId
                        orderby report.CreatedAt descending
                        select new ReportDto
                        {
                            ReportId = report.ReportId,
                            CreatedAt = report.CreatedAt,
                            ReportedReviewId = report.ReviewId,
                            ReportedReviewComment = rev != null ? rev.Comment : "（評論內容無法顯示）",
                            ReasonName = rea != null ? rea.ReasonName : "（原因類型無法顯示）",
                            ReasonComment = report.ReasonComment,
                            StatusId = report.ReportStatusId,
                            StatusName = sta != null ? sta.StatusName : "未知狀態",
                            AdminName = adm != null ? adm.Name : null // 現在，只有當 report.AdminId 有值時，這裡才會有名字
                        };

            var reportDtos = await query.ToListAsync();
            return Ok(reportDtos);
        }
    }
}
    
