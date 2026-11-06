using api_backend.Models;
using api_backend.rrDOTs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace api_backend.Controllers
{
    // 我們把路由拆開，讓 Controller 更專注
    [Route("api")]
    [ApiController]
    public class ReviewsController : ControllerBase
    {
        private readonly TeamDb2Context _context;

        public ReviewsController(TeamDb2Context context)
        {
            _context = context;
        }

        // ====================================================================
        // 功能 A：取得某個商品的「所有公開評論」 
        // 這個 API 是公開的，任何人都可以存取。
        // ====================================================================
        [HttpGet("public/products/{productId}/reviews")]
        public async Task<ActionResult<IEnumerable<ReviewDto>>> GetPublicReviewsForProduct(int productId)
        {
            // --- 嘗試獲取當前訪客的 UserId (如果他有登入的話) ---
            // 有登入，就顯示他是否對某則評論按過讚。
            int? currentUserId = null;
            var userIdStr = User.FindFirst("UserId")?.Value;
            if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out var parsedUserId))
            {
                currentUserId = parsedUserId; // 如果有登入且 Token 有效，就賦值
            }

            // --- 區塊 2：從資料庫查詢評論 ---
            var reviewDtos = await _context.Reviews
                // 只撈取屬於這個商品(productId)且狀態為公開(Status == true)的評論
                .Where(r => r.ProductsId == productId && r.Status == true)
                .Include(r => r.User)
                .Include(r => r.Admin)
                .Include(r => r.ReviewAttachments)
                .Include(r => r.ReviewHelpfulnesses)
                .OrderByDescending(r => r.CreatedAt)
                // --- 區塊 3：將查詢結果轉換成前端需要的 DTO 格式 ---
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
                    AdminName = r.Admin != null ? r.Admin.Name : null,
                    RespondedAt = r.RespondedAt,
                    HelpfulnessCount = r.ReviewHelpfulnesses.Count(h => h.VoteType == true),
                    // 【關鍵邏輯】只有在 currentUserId 有值 (已登入) 的情況下才去計算
                    CurrentUserVote = currentUserId.HasValue
                        ? r.ReviewHelpfulnesses
                            .Where(h => h.UserId == currentUserId.Value)
                            .Select(h => (bool?)h.VoteType)
                            .FirstOrDefault()
                        : null, // 未登入則直接給 null
                    Attachments = r.ReviewAttachments.Select(a => new ReviewAttachmentDto
                    {
                        FilePath = a.FilePath,
                        FileName = a.FileName
                    }).ToList()
                })
                .ToListAsync();

            return Ok(reviewDtos);
        }

        // GET: api/products/5/reviews 
        // ====================================================================
        // 功能 B：取得我的評論紀錄
        // ====================================================================
        // 檔案: Controllers/ReviewsController.cs (或你的評論API所在的Controller)
        // 檔案: Controllers/ReviewsController.cs

        // 【修改處 1】將 [HttpGet("reviews")] 改為 [HttpGet]
        [HttpGet("reviews")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<ReviewDto>>> GetMyReviews()
        {
            // 1. 安全地獲取使用者 ID
            var userIdStr = User.FindFirst("UserId")?.Value;
            if (!int.TryParse(userIdStr, out var currentUserId))
            {
                return Unauthorized("無法識別使用者身分。");
            }

            // 2. 根據 UserId 查詢該使用者的所有評論
            var myReviews = await _context.Reviews
                .Where(r => r.UserId == currentUserId)
                .Include(r => r.Product)
                .Include(r => r.ReviewAttachments)
                .Include(r => r.Admin)
                .Include(r => r.ReviewReports)
                    .ThenInclude(rr => rr.ReasonType)
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
                    Response = r.Response,
                    AdminName = r.Admin != null ? r.Admin.Name : null,
                    RespondedAt = r.RespondedAt,
                    Attachments = r.ReviewAttachments.Select(a => new ReviewAttachmentDto
                    {
                        FilePath = a.FilePath,
                        FileName = a.FileName
                    }).ToList(),

                    Status = r.Status,

                    // 【修改處 2】重新整理 LINQ 查詢的括號，解決綠線問題
                    HiddenReason = (r.Status == false)
                        ? r.ReviewReports
                             .Where(rr => rr.ReportStatusId == 1)
                             .OrderByDescending(rr => rr.ProcessedAt)
                             .Select(rr => rr.ReasonType.ReasonName)
                             .FirstOrDefault()
                        : null
                })
                .ToListAsync();

            return Ok(myReviews);
        }
        // POST: api/products/5/reviews (使用者新增一則評論)
        // 檔案: /Controllers/ReviewsController.cs
        [HttpPost("products/{productId}/reviews")]
        [Authorize]
        public async Task<IActionResult> PostReview(int productId, [FromForm] ReviewCreateDto createDto)
        {
            // ====================================================================
            // 區塊 1：基本模型驗證
            // ====================================================================
            if (!ModelState.IsValid) return BadRequest(ModelState);

            // ====================================================================
            // 區塊 2：使用者身分驗證
            // ====================================================================
            var userIdStr = User.FindFirst("UserId")?.Value;
            if (!int.TryParse(userIdStr, out var currentUserId))
            {
                return Unauthorized("無效的使用者身分，請重新登入。");
            }

            // ====================================================================
            // 區塊 3：業務邏輯 - 購買權限驗證 
            // ====================================================================
            var isValidPurchase = await _context.OrderDetails
                .AnyAsync(od =>
                    od.OrderId == createDto.OrderId &&      // 訂單ID相符
                    od.Order.UserId == currentUserId &&   // 該訂單屬於當前使用者
                    od.Item.ProductsId == productId);     // 透過 Item 導覽到 ShoppingCartItem，再找到原始的 ProductsId

            if (!isValidPurchase)
            {
                return BadRequest("您只能對已購買的商品發表評論。");  
            }

            // ====================================================================
            // 區塊 4：業務邏輯 - 防止重複評論
            // ====================================================================
            var hasAlreadyReviewed = await _context.Reviews
                .AnyAsync(r => r.UserId == currentUserId && r.ProductsId == productId);

            if (hasAlreadyReviewed)
            {
                return Conflict("您已經對此商品發表過評論。");
            }

            // ====================================================================
            // 區塊 5：建立主要的 Review 物件並存入資料庫
            // ====================================================================
            var review = new Review
            {
                Rating = createDto.Rating,
                Comment = createDto.Comment,
                UserId = currentUserId,
                ProductsId = productId,
                CreatedAt = DateTime.Now,
                Status = true,
            };
            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            // ====================================================================
            // 區塊 6：處理檔案上傳邏輯
            // ====================================================================
            if (createDto.Files != null && createDto.Files.Count > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "reviews");
                Directory.CreateDirectory(uploadsFolder);

                foreach (var file in createDto.Files)
                {
                    if (file.Length > 0)
                    {
                        var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(fileStream);
                        }

                        var attachment = new ReviewAttachment
                        {
                            ReviewId = review.ReviewId,
                            FilePath = Path.Combine("/uploads/reviews", uniqueFileName).Replace('\\', '/'),
                            FileName = file.FileName,
                            FileType = file.ContentType,
                            UploadedAt = DateTime.Now
                        };
                        _context.ReviewAttachments.Add(attachment);
                    }
                }
                await _context.SaveChangesAsync();
            }

            // ====================================================================
            // 區塊 7：回傳成功訊息
            // ====================================================================
            return Ok(new { message = "感謝您的評論！" });
        }




        // --- 【檢舉】 ---
        // GET: api/reviews/report-reasons (取得所有可用的檢舉原因)
        [HttpGet("/api/reviews/report-reasons")]
        public async Task<ActionResult<IEnumerable<ReportReasonDto>>> GetReportReasons()
        {
            var reasons = await _context.ReportReasonTypes
                .Where(rt => rt.IsActive == true) // 只撈取啟用中的原因
                .Select(rt => new ReportReasonDto
                {
                    ReasonTypeId = rt.ReasonTypeId,
                    ReasonName = rt.ReasonName
                })
                .ToListAsync();

            return Ok(reasons);
        }

        
        // POST: api/reviews/5/report (使用者檢舉一則評論)
        [HttpPost("reviews/{reviewId}/report")]
        [Authorize] // 強烈建議保持驗證，確保只有登入者能檢舉
        public async Task<IActionResult> ReportReview(int reviewId, [FromBody] ReportSubmissionDto reportDto)
        {
            // 1. 安全地獲取當前登入的使用者 ID
            var userIdStr = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var currentUserId))
            {
                return Unauthorized("無效的 Token 或找不到使用者 ID。");
            }

            // 2. 檢查被檢舉的評論是否存在
            var reviewExists = await _context.Reviews.AnyAsync(r => r.ReviewId == reviewId && r.Status == true);
            if (!reviewExists)
            {
                return NotFound("找不到要檢舉的評論。");
            }

            // 3. 建立新的檢舉報告
            var newReport = new ReviewReport
            {
                ReviewId = reviewId,
                UserId = currentUserId, // 永遠使用從 Token 解析出來的 ID，最安全
                ReasonTypeId = reportDto.ReasonTypeId,
                ReasonComment = reportDto.ReasonComment,
                CreatedAt = DateTime.Now, // 使用你決定的伺服器本地時間
                ReportStatusId = 0,       // 預設為「待處理」
            };

            // 4. 新增到資料庫並儲存
            _context.ReviewReports.Add(newReport);
            await _context.SaveChangesAsync();

            // 5. 回傳成功訊息給前端
            return Ok(new { message = "感謝您的檢舉，我們將會盡快處理。" });
        }

        // --- 【讚/倒讚】 
        // POST: api/reviews/5/helpfulness---路由改成以 / 開頭的絕對路徑，避免與 Controller 的路由衝突
        [HttpPost("reviews/{reviewId}/helpfulness")]
        [Authorize]
        public async Task<IActionResult> VoteForReview(int reviewId, [FromBody] ReviewHelpfulnessDto helpfulnessDto)
        {
            var userIdStr = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var currentUserId))
            {
                return Unauthorized("無效的 Token 或找不到使用者 ID。");
            }

            var reviewExists = await _context.Reviews.AnyAsync(r => r.ReviewId == reviewId);
            if (!reviewExists)
            {
                return NotFound("找不到指定的評論。");
            }

            var existingVote = await _context.ReviewHelpfulnesses
                .FirstOrDefaultAsync(h => h.ReviewId == reviewId && h.UserId == currentUserId);

            if (existingVote != null)
            {
                // 【只有在投票狀態改變時才更新，避免不必要的資料庫寫入】
                if (existingVote.VoteType != helpfulnessDto.IsHelpful)
                {
                    existingVote.VoteType = helpfulnessDto.IsHelpful;
                }
            }
            else
            {
                var newVote = new ReviewHelpfulness
                {
                    ReviewId = reviewId,
                    UserId = currentUserId,
                    VoteType = helpfulnessDto.IsHelpful,
                    CreatedAt = DateTime.Now
                };
                _context.ReviewHelpfulnesses.Add(newVote);
            }

            await _context.SaveChangesAsync();
      
            // 在儲存變更後，重新計算一次這則評論最新的「讚」總數
            var newHelpfulnessCount = await _context.ReviewHelpfulnesses
                .CountAsync(h => h.ReviewId == reviewId && h.VoteType == true);

            // 將最新的讚數連同成功訊息一起回傳給前端
            return Ok(new
            {
                message = "投票成功！",
                newCount = newHelpfulnessCount // 前端可以從 response.data.newCount 拿到這個值
            });
        }

        // ====================================================================
        // 功能 ：【新增】取得當前使用者「所有」已評論過的商品 ID 列表
        // GET: api/reviews/by-user/reviewed-products
        // ====================================================================
        [HttpGet("reviews/by-user/reviewed-products")]
        [Authorize] // 必須登入才能查詢
        public async Task<ActionResult<IEnumerable<int>>> GetReviewedProductIdsForCurrentUser()
        {
            // 1. 從 Token 中獲取當前登入的使用者 ID
            var userIdStr = User.FindFirst("UserId")?.Value;
            if (!int.TryParse(userIdStr, out int currentUserId))
            {
                return Unauthorized("無法識別使用者身分。");
            }

            // 2. 查詢 Reviews 資料表，找出這位使用者評論過的所有 ProductsId
            var reviewedProductIds = await _context.Reviews
                .Where(r => r.UserId == currentUserId) // 只篩選當前使用者
                .Select(r => r.ProductsId)             // 只選擇 ProductsId 欄位
                .Distinct()                            // 確保不重複
                .ToListAsync();

            // 3. 回傳一個包含所有已評論過的 ProductId 的陣列
            return Ok(reviewedProductIds);
        }
    }
}
