using api_backend.Models;
using api_backend.rrDOTs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace api_backend.Controllers
{
    [Route("api/favorites")]
    [ApiController]
    public class FavoritesController : ControllerBase
    {
        private readonly TeamDb2Context _context;

        public FavoritesController(TeamDb2Context context)
        {
            _context = context;
        }

        // GET: api/favorites
        [HttpGet]
        [Authorize]
        public async Task<ActionResult<IEnumerable<FavoriteItemDto>>> GetMyFavorites()
        {
            //var currentUserId = 1; 

            var userIdStr = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var currentUserId))
            {
                return Unauthorized("無效的 Token 或找不到使用者 ID。");
            }

            var favorites = await _context.Favorites
                .Where(f => f.UserId == currentUserId)
                .Include(f => f.Product)
                .ThenInclude(p => p.ProductImages)
                .OrderByDescending(f => f.CreatedAt)
                .Select(f => new FavoriteItemDto
                {
                    ProductId = f.Product.ProductsId,
                    ProductName = f.Product.Name,
                    ProductPrice = f.Product.Price,
                    // 從多張圖片中，選出第一張或標記為 IsMain 的那張
                    ProductImageUrl = f.Product.ProductImages
                                 .OrderBy(img => img.IsMain == 1 ? 0 : 1) // 優先選主圖
                                 .Select(img => img.ImageUrl)
                                 .FirstOrDefault() ?? "/images/default-placeholder.png" // 如果沒有圖片，給一個預設路徑
                })
        .ToListAsync();

            return Ok(favorites);
        }

        // POST: api/favorites
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AddFavorite([FromBody] AddFavoriteDto addFavoriteDto)
        {
            var userIdStr = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var currentUserId))
            {
                return Unauthorized("無效的 Token 或找不到使用者 ID。");
            }

            var existingFavorite = await _context.Favorites
                .FirstOrDefaultAsync(f => f.UserId == currentUserId && f.ProductId == addFavoriteDto.ProductId);

            if (existingFavorite != null)
            {
                return Ok(new { message = "此商品已在您的收藏清單中。" });
            }

            var favorite = new Favorite
            {
                UserId = currentUserId,
                ProductId = addFavoriteDto.ProductId,
                CreatedAt = DateTime.Now
            };

            _context.Favorites.Add(favorite);
            await _context.SaveChangesAsync();

            return StatusCode(201, new { message = "成功加入收藏！" });
        }

        // DELETE: api/favorites/5
        [HttpDelete("{productId}")]
        [Authorize]
        public async Task<IActionResult> DeleteFavorite(int productId)
        {
            var userIdStr = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var currentUserId))
            {
                return Unauthorized("無效的 Token 或找不到使用者 ID。");
            }

            var favorite = await _context.Favorites
                .FirstOrDefaultAsync(f => f.UserId == currentUserId && f.ProductId == productId);

            if (favorite == null)
            {
                return NotFound();
            }

            _context.Favorites.Remove(favorite);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
