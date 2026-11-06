using System.ComponentModel.DataAnnotations;
namespace api_backend.Models.DTO
{
    public class CreateProductDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public int Price { get; set; }
        public int Stock { get; set; }

        public byte Status { get; set; } // ← 對應後端的 Status 欄位（0 = 下架、1 = 上架）

        public int CategoryId { get; set; }  // ← 注意命名
        public int SortId { get; set; }

        public int SortOrder { get; set; } = 1;

        public bool StockAlert { get; set; }
        public int? LowerthanAlert { get; set; }

        [Required]
        public List<int> Tags { get; set; } = new List<int>();

        [Required]
        public List<ImageUploadDto> Images { get; set; } = new List<ImageUploadDto>();
    }
}
