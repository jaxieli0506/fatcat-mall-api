using Microsoft.AspNetCore.Mvc;
using api_backend.Models;
using System.Linq;

namespace api_backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SearchController : ControllerBase
    {
        private readonly TeamDb2Context _context;
        public SearchController(TeamDb2Context context)
        {
            _context = context;
        }

        // 搜尋
        // http://localhost:7017/api/search?keyword=xxx
        [HttpGet]
        public IActionResult Search([FromQuery] string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return BadRequest("請輸入關鍵字");
            }

            // 商品大分類
            var categories = _context.ProductCategories
                .Where(c => c.Name.Contains(keyword))
                .Select(c => new { c.ProductCategoriesId, c.Name, c.ParentId })
                .ToList();

            // 商品小分類
            var sorts = _context.Sorts
                .Where(s => s.Name.Contains(keyword))
                .Select(s => new { s.SortId, s.Name, s.CategoryId, s.ParentId })
                .ToList();

            // 商品名稱
            var products = _context.Products
                .Where(p => p.Name.Contains(keyword) || p.Description.Contains(keyword))
                .Select(p => new { p.ProductsId, p.Name, p.Description, p.Price, p.Stock })
                .ToList();

            // 優惠券
            var coupons = _context.Coupons
                .Where(c => c.CouponCode.Contains(keyword) || c.Description.Contains(keyword))
                .Select(c => new { c.CouponId, c.CouponCode, c.Description, c.DiscountAmount, c.Expirydate })
                .ToList();

            // 活動
            var campaigns = _context.Campaigns
                .Where(c => c.Title.Contains(keyword) || c.Content.Contains(keyword))
                .Select(c => new { c.CampaignId, c.Title, c.Content, c.CoverImageUrl, c.StartDate, c.EndDate })
                .ToList();

            return Ok(new
            {
                categories,
                sorts,
                products,
                coupons,
                campaigns
            });
        }
    }
} 