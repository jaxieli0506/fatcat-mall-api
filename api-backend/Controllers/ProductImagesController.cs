using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using api_backend.Models;
using api_backend.ViewModels;

namespace api_backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductImagesController : ControllerBase
    {
        private readonly TeamDb2Context _context;

        public ProductImagesController(TeamDb2Context context)
        {
            _context = context;
        }

        // ✅ 1. 取得所有圖片
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProductImage>>> GetProductImages()
        {
            return await _context.ProductImages.ToListAsync();
        }

        // 2. 取得單張圖片
        [HttpGet("{id}")]
        public async Task<ActionResult<ProductImage>> GetProductImage(int id)
        {
            var productImage = await _context.ProductImages.FindAsync(id);

            if (productImage == null)
            {
                return NotFound();
            }

            return productImage;
        }

        // 3. 依商品取得所有圖片
        [HttpGet("byProduct/{productId}")]
        public async Task<ActionResult<IEnumerable<ProductImageViewModel>>> GetImagesByProduct(int productId)
        {
            var images = await _context.ProductImages
                .Where(p => p.ProductId == productId)
                .Select(p => new ProductImageViewModel
                {
                    ProductImagesId = p.ProductImagesId,
                    ProductId = p.ProductId,
                    IsMain = p.IsMain,
                    ImageUrl = p.ImageUrl.StartsWith("/ProductImages/")
                        ? p.ImageUrl
                        : "/ProductImages/" + p.ImageUrl
                })
                .ToListAsync();

            return images;
        }

        // 4. 設定主圖（會自動取消其他圖片的主圖）
        [HttpPut("{id}/setMain")]
        public async Task<IActionResult> SetMainImage(int id)
        {
            var image = await _context.ProductImages.FindAsync(id);
            if (image == null)
            {
                return NotFound();
            }

            var productId = image.ProductId;

            // 把該商品所有圖片的 IsMain 改為 0，再把目標圖片改為 1
            var allImages = await _context.ProductImages
                .Where(p => p.ProductId == productId)
                .ToListAsync();

            foreach (var img in allImages)
            {
                img.IsMain = (byte)(img.ProductImagesId == id ? 1 : 0);
            }

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // 5. 更新圖片資料
        [HttpPut("{id}")]
        public async Task<IActionResult> PutProductImage(int id, ProductImage productImage)
        {
            if (id != productImage.ProductImagesId)
            {
                return BadRequest();
            }

            _context.Entry(productImage).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductImageExists(id))
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

        // 6. 新增圖片資料（圖片上傳功能如需擴充再說）
        [HttpPost]
        public async Task<ActionResult<ProductImage>> PostProductImage(ProductImage productImage)
        {
            _context.ProductImages.Add(productImage);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetProductImage), new { id = productImage.ProductImagesId }, productImage);
        }

        // 7. 刪除圖片
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProductImage(int id)
        {
            var productImage = await _context.ProductImages.FindAsync(id);
            if (productImage == null)
            {
                return NotFound();
            }

            _context.ProductImages.Remove(productImage);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ProductImageExists(int id)
        {
            return _context.ProductImages.Any(e => e.ProductImagesId == id);
        }
    }
}
