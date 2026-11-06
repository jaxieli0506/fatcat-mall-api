using api_backend.Models.DTO;

namespace api_backend.Services
{
    public interface IExternalLoginService
    {
        Task<List<ExternalLoginDTO>> GetUserExternalLoginsAsync(int userId);
        Task<ExternalLoginResponseDTO> BindExternalLoginAsync(int userId, ExternalLoginRequestDTO request);
        Task<ExternalLoginResponseDTO> UnbindExternalLoginAsync(int userId, string loginProvider);
        Task<bool> ValidateExternalTokenAsync(string provider, string token);
    }
}
