// 檔案: rrDOTs/FavoriteDtos.cs
using System.ComponentModel.DataAnnotations;

namespace api_backend.rrDOTs
{
    // 用於「顯示」在收藏頁面的 DTO
    public class FavoriteItemDto
    {
        // 這裡我們用 ProductId，因為對前端來說，這才是重點
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string ProductImageUrl { get; set; }
        public decimal ProductPrice { get; set; }
        public DateTime CreatedAt { get; set; } // 加入收藏的時間
    }

    // 用於「新增」一個收藏時，從前端接收的 DTO
    public class AddFavoriteDto
    {
        [Required]
        public int ProductId { get; set; }
    }
}