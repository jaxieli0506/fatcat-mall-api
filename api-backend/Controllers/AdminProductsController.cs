using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using api_backend.Models;
using api_backend.Models.DTO;

namespace api_backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminProductsController : ControllerBase
    {
        private readonly TeamDb2Context _context;

        public AdminProductsController(TeamDb2Context context)
        {
            _context = context;
        }

        // GET: api/AdminProducts
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetProducts()
        {
            var products = await _context.Products
                .Where(p => p.IsDeleted == 0) // 篩掉被軟刪的
                .Include(p => p.Category)
                .Include(p => p.Sort)
                .Include(p => p.ProductImages)
                .Select(p => new
                {
                    p.ProductsId,
                    p.Name,
                    p.Price,
                    p.Stock,
                    p.Status,
                    IsAvailable = p.Status == 1,
                    StatusText = p.Status == 1 ? "上架中" : "已下架",
                    CategoryId = p.CategoryId,
                    CategoryName = p.Category.Name,
                    SortId = p.SortId,
                    SortName = p.Sort.Name,
                    MainImage = "/ProductImages/" +
                        (p.ProductImages.FirstOrDefault(i => i.IsMain == 1).ImageUrl ?? "default.jpg")
                })
                .ToListAsync();

            return Ok(products);
        }

        // PATCH: api/AdminProducts/ToggleStatus/5
        [HttpPatch("ToggleStatus/{id}")]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            // 記錄原始狀態，用於檢查是否需要發送上架通知  by jj
            var originalStatus = product.Status;

            // 切換 Status（1 = 上架, 0 = 下架）
            product.Status = (byte)(product.Status == 1 ? 0 : 1);

            await _context.SaveChangesAsync();

            // 檢查是否從下架變為上架，如果是則發送通知  by jj
            if (originalStatus == 0 && product.Status == 1)
            {
                await SendProductLaunchNotificationToAllUsers(product);
            }

            return Ok(new
            {
                message = "狀態已更新",
                newStatus = product.Status,
                isAvailable = product.Status == 1,
                statusText = product.Status == 1 ? "上架中" : "已下架"
            });
        }

        // GET: api/AdminProducts/5
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetProduct(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Sort)
                .Include(p => p.ProductImages)
                .Where(p => p.ProductsId == id)
                .Select(p => new
                {
                    p.ProductsId,
                    p.Name,
                    p.Description,
                    p.Price,
                    p.Stock,
                    Status = p.Status, // 0 or 1
                    CategoryId = p.CategoryId,
                    SortId = p.SortId,
                    StockAlert = p.StockAlert,
                    LowerthanAlert = p.LowerthanAlert,
                    Images = p.ProductImages.Select(img => new
                    {
                        img.ProductImagesId,
                        ImageUrl = "/ProductImages/" + img.ImageUrl,
                        img.IsMain
                    }).ToList()
                })
                .FirstOrDefaultAsync();

            if (product == null)
            {
                return NotFound();
            }

            return Ok(product);
        }

        // GET: api/AdminProducts/{id}/tags
        [HttpGet("{id}/tags")]
        public async Task<ActionResult<IEnumerable<object>>> GetProductTags(int id)
        {
            var tags = await _context.ProductTags
                .Where(pt => pt.ProductId == id)
                .Include(pt => pt.Tag)
                .Select(pt => new {
                    pt.Tag.TagsId,
                    pt.Tag.Name
                })
                .ToListAsync();

            return Ok(tags);
        }

        //廚餘桶功能
        // GET: api/AdminProducts/deleted
        [HttpGet("deleted")]
        public async Task<ActionResult<IEnumerable<object>>> GetDeletedProducts()
        {
            return await _context.Products
                .Where(p => p.IsDeleted == 1)
                .Include(p => p.Category)
                .Include(p => p.Sort)
                .Select(p => new {
                    p.ProductsId,
                    p.Name,
                    p.Price,
                    p.Stock,
                    CategoryName = p.Category.Name,
                    SortName = p.Sort.Name,
                    p.UpdatedAt
                })
                .ToListAsync();
        }

        //復原被刪除的商品
        // POST: api/AdminProducts/{id}/restore
        [HttpPost("{id}/restore")]
        public async Task<IActionResult> RestoreProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            product.IsDeleted = 0;
            product.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // PUT: api/AdminProducts/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutProduct(int id, AdminProductEditDto dto)
        {
            var product = await _context.Products
                .Include(p => p.ProductImages)
                .FirstOrDefaultAsync(p => p.ProductsId == id);

            if (product == null) return NotFound();

            // 記錄原始狀態，用於檢查是否需要發送上架通知 by jj
            var originalStatus = product.Status;

            // 更新基本欄位
            product.Name = dto.Name;
            product.Description = dto.Description;
            product.Price = dto.Price;
            product.Stock = dto.Stock;
            product.Status = dto.Status;
            product.CategoryId = dto.CategoryId;
            product.SortId = dto.SortId;
            product.StockAlert = dto.StockAlert;
            product.LowerthanAlert = dto.LowerthanAlert;
            product.UpdatedAt = DateTime.Now; //  by jj

            await _context.SaveChangesAsync();

            // 檢查是否從下架變為上架，如果是則發送通知  by jj
            if (originalStatus == 0 && product.Status == 1)
            {
                await SendProductLaunchNotificationToAllUsers(product);
            }

            // 檢查庫存是否低於警告下限
            if (product.StockAlert && product.LowerthanAlert.HasValue)
            {
                if (product.Stock <= product.LowerthanAlert.Value)
                {
                    await SendLowStockNotificationToAdmins(product);
                }
            }

            return NoContent();
        }

        // 更新商品標籤
        // PUT: api/AdminProducts/{id}/tags
        [HttpPut("{id}/tags")]
        public async Task<IActionResult> UpdateProductTags(int id, [FromBody] List<int> tagIds)
        {
            // 先刪除現有的關聯
            var existingTags = _context.ProductTags.Where(pt => pt.ProductId == id);
            _context.ProductTags.RemoveRange(existingTags);

            // 新增新的關聯
            foreach (var tagId in tagIds.Distinct())
            {
                _context.ProductTags.Add(new ProductTag
                {
                    ProductId = id,
                    TagId = tagId
                });
            }

            await _context.SaveChangesAsync();
            return NoContent();
        }

        //上傳新圖片
        [HttpPost("{id}/images")]
        public async Task<IActionResult> UploadProductImage(int id, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("請提供圖片");

            // 確保沒有夾帶路徑
            var safeFileName = Path.GetFileName(file.FileName);

            var savePath = Path.Combine("wwwroot", "ProductImages", safeFileName);

            using (var stream = new FileStream(savePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var image = new ProductImage
            {
                ProductId = id,
                ImageUrl = safeFileName, // 只儲存檔名
                IsMain = 0
            };

            _context.ProductImages.Add(image);
            await _context.SaveChangesAsync();

            return Ok(image);
        }

        //設定主圖片
        [HttpPost("images/{imageId}/set-main")]
        public async Task<IActionResult> SetMainImage(int imageId)
        {
            var image = await _context.ProductImages.FindAsync(imageId);
            if (image == null) return NotFound();

            // 先把同商品的所有圖片 IsMain 設為 0
            var images = _context.ProductImages
                .Where(i => i.ProductId == image.ProductId);

            foreach (var img in images)
            {
                img.IsMain = 0;
            }

            image.IsMain = 1;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        //刪除圖片
        [HttpDelete("images/{imageId}")]
        public async Task<IActionResult> DeleteImage(int imageId)
        {
            var image = await _context.ProductImages.FindAsync(imageId);
            if (image == null) return NotFound();

            // === 刪除實體圖片檔案 ===
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "ProductImages", image.ImageUrl);

            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }

            // === 刪除資料庫紀錄 ===
            _context.ProductImages.Remove(image);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // POST: api/AdminProducts
        // 新增商品
        [HttpPost]
        public async Task<IActionResult> CreateProduct([FromBody] CreateProductDto dto)
        {
            var product = new Product
            {
                Name = dto.Name,
                Description = dto.Description,
                Price = dto.Price,
                Stock = dto.Stock,
                Status = dto.Status,               // ← 對應 Status byte 欄位
                CategoryId = dto.CategoryId,
                SortId = dto.SortId,
                SortOrder = dto.SortOrder,
                StockAlert = dto.StockAlert,
                LowerthanAlert = dto.LowerthanAlert,
                IsDeleted = 0,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync(); // 要先 Save 才有 ProductId 可用

            foreach (var img in dto.Images)
            {
                // 確保只保留檔名，移除任何資料夾路徑
                var fileName = Path.GetFileName(img.ImageUrl);

                _context.ProductImages.Add(new ProductImage
                {
                    ProductId = product.ProductsId,
                    ImageUrl = fileName,
                    IsMain = (byte)(img.IsMain ? 1 : 0)
                });
            }

            foreach (var tagId in dto.Tags)
            {
                _context.ProductTags.Add(new ProductTag
                {
                    ProductId = product.ProductsId,
                    TagId = tagId
                });
            }

            await _context.SaveChangesAsync();

            // 如果商品狀態為上架，發送通知給所有使用者  by jj
            if (product.Status == 1)
            {
                await SendProductLaunchNotificationToAllUsers(product);
            }

            return Ok(new { message = "商品新增成功", productId = product.ProductsId });
        }

        // POST: api/AdminProducts
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        //[HttpPost]
        //public async Task<ActionResult<Product>> PostProduct(Product product)
        //{
        //    _context.Products.Add(product);
        //    await _context.SaveChangesAsync();

        //    return CreatedAtAction("GetProduct", new { id = product.ProductsId }, product);
        //}

        // DELETE: api/AdminProducts/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            // 改成軟刪除
            product.IsDeleted = 1; // byte 型別，用 1 表示刪除
            product.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // 發送商品上架通知給所有使用者  by jj
        private async Task SendProductLaunchNotificationToAllUsers(Product product)
        {
            try
            {
                // 創建通知
                var notification = new Notification
                {
                    Title = "新商品上架",
                    Description = $"新商品「{product.Name}」已上架，快來看看吧！",
                    NotificationType = 7, // 促銷活動類型
                    Time = DateTime.Now,
                    AdminId = 1 // 系統管理員ID，可以根據需要調整
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                // 獲取所有使用者ID
                var userIds = await _context.Users.Select(u => u.UserId).ToListAsync();

                // 為每個使用者創建通知接收記錄
                foreach (var userId in userIds)
                {
                    _context.NotificationReceivers.Add(new NotificationReceiver
                    {
                        NotificationId = notification.NotificationId,
                        UserId = userId,
                        IsRead = false
                    });
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // 記錄錯誤，但不影響主要的商品操作
                Console.WriteLine($"發送商品上架通知失敗: {ex.Message}");
            }
        }

        // 發送庫存不足警告通知給所有管理員
        private async Task SendLowStockNotificationToAdmins(Product product)
        {
            try
            {
                // 檢查是否已經為此商品發送過庫存警告通知（避免重複發送）
                var recentNotification = await _context.Notifications
                    .Where(n => n.Title.Contains($"商品「{product.Name}」庫存不足"))
                    .Where(n => n.Time >= DateTime.Now.AddHours(-24)) // 24小時內
                    .FirstOrDefaultAsync();

                if (recentNotification != null)
                {
                    return; // 24小時內已發送過，不重複發送
                }

                // 創建通知
                var notification = new Notification
                {
                    Title = "庫存警告",
                    Description = $"商品「{product.Name}」庫存僅剩 {product.Stock} 件，已低於警告下限 {product.LowerthanAlert} 件，請及時補貨！",
                    NotificationType = 1, // 訂單相關類型
                    Time = DateTime.Now,
                    AdminId = 1 // 系統管理員ID
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                // 獲取所有管理員ID
                var adminIds = await _context.Admins.Select(a => a.AdminId).ToListAsync();

                // 為每個管理員創建通知接收記錄
                foreach (var adminId in adminIds)
                {
                    _context.NotificationReceivers.Add(new NotificationReceiver
                    {
                        NotificationId = notification.NotificationId,
                        AdminId = adminId,
                        IsRead = false
                    });
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // 記錄錯誤，但不影響主要的商品操作
                Console.WriteLine($"發送庫存警告通知失敗: {ex.Message}");
            }
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.ProductsId == id);
        }
    }
}
