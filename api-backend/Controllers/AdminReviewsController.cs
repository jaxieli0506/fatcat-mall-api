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
    [Route("api/admin/reviews")]
    [ApiController]
    public class AdminReviewsController : ControllerBase
    {
        private readonly TeamDb2Context _context;

        public AdminReviewsController(TeamDb2Context context)
        {
            _context = context;
        }

        // GET: api/admin/reviews (讓管理者搜尋所有評論)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ReviewDto>>> GetAllReviews()
        {
            // 這裡可以加入更多搜尋參數，例如 [FromQuery] string userName
            var reviews = await _context.Reviews
                .Include(r => r.User)
                .Include(r => r.Admin)
                .Include(r => r.Product)
                .Include(r => r.ReviewAttachments)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new ReviewDto
                {
                    ReviewId = r.ReviewId,
                    ProductId = r.ProductsId,
                    ProductName = r.Product.Name,
                    UserId = r.UserId,
                    UserName = r.User.Name,
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt,
                    Status = r.Status,
                    Response = r.Response,
                    AdminName = r.Admin.Name,
                    RespondedAt = r.RespondedAt,
                    Attachments = r.ReviewAttachments.Select(a => new ReviewAttachmentDto
                    {
                        FilePath = a.FilePath,
                        FileName = a.FileName
                    }).ToList()
                })
                .ToListAsync();

            return Ok(reviews);
        }

        // PUT: api/admin/reviews/5/response (管理員回覆或隱藏評論)
        [HttpPut("{id}/response")]
        public async Task<IActionResult> RespondToReview(int id, [FromBody] AdminReviewResponseDto responseDto)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review == null)
            {
                return NotFound();
            }

            var adminId = 1; // 之後從 Token 拿

            // 只更新管理員可以修改的欄位
            review.Response = responseDto.Response;
            review.Status = responseDto.Status; // true=公開, false=隱藏
            review.AdminId = adminId;
            review.RespondedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // 操作成功，回傳 204 No Content
            return NoContent();
        }
    }
}
