namespace api_backend.Models.DTO
{
    public class ExternalLoginDTO
    {
        public int LoginId { get; set; }
        public string LoginProvider { get; set; }
        public string ProviderId { get; set; }
        public DateTime? CreateDate { get; set; }
        public bool IsConnected { get; set; }
    }
}
