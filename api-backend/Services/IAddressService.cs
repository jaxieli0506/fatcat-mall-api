using api_backend.Models.DTO;

namespace api_backend.Services
{
    public interface IAddressService
    {
        Task<List<AddressResponseDTO>> GetUserAddressesAsync(int userId);
        Task<AddressResponseDTO> GetAddressByIdAsync(int addressId, int userId);
        Task<AddressResponseDTO> CreateAddressAsync(AddressRequestDTO request, int userId);
        Task<AddressResponseDTO> UpdateAddressAsync(int addressId, AddressRequestDTO request, int userId);
        Task<bool> DeleteAddressAsync(int addressId, int userId);
        Task<bool> SetDefaultAddressAsync(int addressId, int userId);
    }
} 