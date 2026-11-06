namespace api_backend.ViewModels
{
    public class ProductDetailViewModel
    {
        public int ProductsId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public byte Status { get; set; }
        public string StatusText { get; set; } // 👈 新增這行
        public string CategoryName { get; set; }
        public string SortName { get; set; }
        public string ImageUrl { get; set; } // 主圖
    }
}
