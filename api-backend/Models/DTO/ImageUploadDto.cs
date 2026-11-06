namespace api_backend.Models.DTO
{
    public class ImageUploadDto
    {
        public string ImageUrl { get; set; }   // 圖片檔名（不含資料夾路徑）
        public bool IsMain { get; set; }       // 是否是主圖
    }
}
