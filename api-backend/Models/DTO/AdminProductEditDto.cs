namespace api_backend.Models.DTO
{
    public class AdminProductEditDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public int Price { get; set; }
        public int Stock { get; set; }
        public byte Status { get; set; }
        public int CategoryId { get; set; }
        public int SortId { get; set; }
        public bool StockAlert { get; set; }
        public int? LowerthanAlert { get; set; }
    }
}
