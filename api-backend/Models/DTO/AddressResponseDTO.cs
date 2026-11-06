namespace api_backend.Models.DTO
{
    public class AddressResponseDTO
    {
        public int AddressId { get; set; }
        public int UserId { get; set; }
        public string RecipientName { get; set; }
        public string PhoneNumber { get; set; }
        public int AddressType { get; set; }
        public string AddressTypeText { get; set; }
        public int? StoreType { get; set; }
        public string StoreName { get; set; }
        public string? StoreBranch { get; set; }
        public string City { get; set; }
        public string District { get; set; }
        public string AddressDetail { get; set; }
        public bool IsDefault { get; set; }
        public string FullAddress { get; set; }
    }
} 