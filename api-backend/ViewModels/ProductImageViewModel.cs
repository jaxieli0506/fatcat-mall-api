namespace api_backend.ViewModels
{
    public class ProductImageViewModel
    {
        public int ProductImagesId { get; set; }
        public int ProductId { get; set; }
        public string ImageUrl { get; set; }
        public byte IsMain { get; set; }
    }
}
