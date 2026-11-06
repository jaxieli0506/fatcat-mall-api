using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using api_backend.Models;

namespace api_backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductTagsController : ControllerBase
    {
        private readonly TeamDb2Context _context;

        public ProductTagsController(TeamDb2Context context)
        {
            _context = context;
        }

        // ✅ 1. 取得所有商品標籤關聯（除錯用）
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProductTag>>> GetProductTags()
        {
            return await _context.ProductTags.ToListAsync();
        }

        // ✅ 2. 查詢單筆 ProductTag（如真的需要）
        [HttpGet("{id}")]
        public async Task<ActionResult<ProductTag>> GetProductTag(int id)
        {
            var productTag = await _context.ProductTags.FindAsync(id);

            if (productTag == null)
            {
                return NotFound();
            }

            return productTag;
        }

        // ✅ 3. 🔍 取得某商品的所有標籤（只回傳 TagId）
        [HttpGet("byProduct/{productId}")]
        public async Task<ActionResult<IEnumerable<int>>> GetTagsByProduct(int productId)
        {
            var tagIds = await _context.ProductTags
                .Where(pt => pt.ProductId == productId)
                .Select(pt => pt.TagId)
                .ToListAsync();

            return tagIds;
        }

        // ✅ 4. 🛠 更新某商品的所有標籤（清空重設）
        [HttpPost("updateForProduct")]
        public async Task<IActionResult> UpdateTagsForProduct([FromBody] ProductTagUpdateRequest request)
        {
            var productId = request.ProductId;

            // 刪除舊的標籤
            var existing = _context.ProductTags.Where(pt => pt.ProductId == productId);
            _context.ProductTags.RemoveRange(existing);

            // 加入新的標籤
            foreach (var tagId in request.TagId)
            {
                _context.ProductTags.Add(new ProductTag
                {
                    ProductId = productId,
                    TagId = tagId
                });
            }

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // ✅ 5. 單筆新增（可保留）
        [HttpPost]
        public async Task<ActionResult<ProductTag>> PostProductTag(ProductTag productTag)
        {
            _context.ProductTags.Add(productTag);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetProductTag), new { id = productTag.ProductTagsId }, productTag);
        }

        // ✅ 6. 單筆刪除（如需移除某個關聯）
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProductTag(int id)
        {
            var productTag = await _context.ProductTags.FindAsync(id);
            if (productTag == null)
            {
                return NotFound();
            }

            _context.ProductTags.Remove(productTag);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ProductTagExists(int id)
        {
            return _context.ProductTags.Any(e => e.ProductTagsId == id);
        }
    }

    // ✅ DTO：用於前端更新某商品標籤用的 class
    public class ProductTagUpdateRequest
    {
        public int ProductId { get; set; }
        public List<int> TagId { get; set; }
    }
}
