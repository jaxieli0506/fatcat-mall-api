namespace api_backend.Models.DTO
{
    public class ExternalLoginRequestDTO
    {
        public string LoginProvider { get; set; }
        public string ProviderId { get; set; }
        public string AccessToken { get; set; }
        public string Email { get; set; }
        public string Name { get; set; }
    }
}
