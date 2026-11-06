using api_backend.Models;
using api_backend.Models.DTO;
using Microsoft.EntityFrameworkCore;

namespace api_backend.Services
{
    public class AddressService : IAddressService
    {
        private readonly TeamDb2Context _context;

        public AddressService(TeamDb2Context context)
        {
            _context = context;
        }

        public async Task<List<AddressResponseDTO>> GetUserAddressesAsync(int userId)
        {
            var addresses = await _context.ShippingAddresses
                .Where(a => a.UserId == userId && a.DeletedAt == null)
                .OrderByDescending(a => a.IsDefault)
                .ThenBy(a => a.AddressId)
                .ToListAsync();

            return addresses.Select(MapToResponseDTO).ToList();
        }

        public async Task<AddressResponseDTO> GetAddressByIdAsync(int addressId, int userId)
        {
            var address = await _context.ShippingAddresses
                .FirstOrDefaultAsync(a => a.AddressId == addressId && a.UserId == userId && a.DeletedAt == null);

            if (address == null)
                throw new ArgumentException("地址不存在或無權限存取");

            return MapToResponseDTO(address);
        }

        public async Task<AddressResponseDTO> CreateAddressAsync(AddressRequestDTO request, int userId)
        {
            // 驗證超商相關欄位
            if (request.AddressType == 1) // 超商取貨地址
            {
                if (!request.StoreType.HasValue)
                    throw new ArgumentException("超商類型為必填欄位");
                if (string.IsNullOrEmpty(request.StoreName))
                    throw new ArgumentException("超商名稱為必填欄位");
                //if (string.IsNullOrEmpty(request.StoreBranch))
                //    throw new ArgumentException("超商分店為必填欄位");
            }

            // 如果設為預設地址，先取消其他地址的預設狀態
            if (request.IsDefault)
            {
                var existingDefaults = await _context.ShippingAddresses
                    .Where(a => a.UserId == userId && a.IsDefault && a.DeletedAt == null)
                    .ToListAsync();

                foreach (var addr in existingDefaults)
                {
                    addr.IsDefault = false;
                }
            }

            var address = new ShippingAddress
            {
                UserId = userId,
                RecipientName = request.RecipientName,
                PhoneNumber = request.PhoneNumber,
                AddressType = request.AddressType,
                StoreType = request.StoreType,
                StoreName = request.StoreName,
                StoreBranch = request.StoreBranch,
                City = request.City,
                District = request.District,
                AddressDetail = request.AddressDetail,
                IsDefault = request.IsDefault
            };

            _context.ShippingAddresses.Add(address);
            await _context.SaveChangesAsync();

            return MapToResponseDTO(address);
        }

        public async Task<AddressResponseDTO> UpdateAddressAsync(int addressId, AddressRequestDTO request, int userId)
        {
            var address = await _context.ShippingAddresses
                .FirstOrDefaultAsync(a => a.AddressId == addressId && a.UserId == userId && a.DeletedAt == null);

            if (address == null)
                throw new ArgumentException("地址不存在或無權限存取");

            // 驗證超商相關欄位
            if (request.AddressType == 1) // 超商取貨地址
            {
                if (!request.StoreType.HasValue)
                    throw new ArgumentException("超商類型為必填欄位");
                if (string.IsNullOrEmpty(request.StoreName))
                    throw new ArgumentException("超商名稱為必填欄位");
                //if (string.IsNullOrEmpty(request.StoreBranch))
                //    throw new ArgumentException("超商分店為必填欄位");
            }

            // 如果設為預設地址，先取消其他地址的預設狀態
            if (request.IsDefault && !address.IsDefault)
            {
                var existingDefaults = await _context.ShippingAddresses
                    .Where(a => a.UserId == userId && a.IsDefault && a.AddressId != addressId && a.DeletedAt == null)
                    .ToListAsync();

                foreach (var addr in existingDefaults)
                {
                    addr.IsDefault = false;
                }
            }

            // 更新地址資訊
            address.RecipientName = request.RecipientName;
            address.PhoneNumber = request.PhoneNumber;
            address.AddressType = request.AddressType;
            address.StoreType = request.StoreType;
            address.StoreName = request.StoreName;
            address.StoreBranch = request.StoreBranch;
            address.City = request.City;
            address.District = request.District;
            address.AddressDetail = request.AddressDetail;
            address.IsDefault = request.IsDefault;

            await _context.SaveChangesAsync();

            return MapToResponseDTO(address);
        }

        public async Task<bool> DeleteAddressAsync(int addressId, int userId)
        {
            var address = await _context.ShippingAddresses
                .FirstOrDefaultAsync(a => a.AddressId == addressId && a.UserId == userId && a.DeletedAt == null);

            if (address == null)
                throw new ArgumentException("地址不存在或無權限存取");

            // 軟刪除
            address.DeletedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> SetDefaultAddressAsync(int addressId, int userId)
        {
            var address = await _context.ShippingAddresses
                .FirstOrDefaultAsync(a => a.AddressId == addressId && a.UserId == userId && a.DeletedAt == null);

            if (address == null)
                throw new ArgumentException("地址不存在或無權限存取");

            // 取消其他地址的預設狀態
            var existingDefaults = await _context.ShippingAddresses
                .Where(a => a.UserId == userId && a.IsDefault && a.AddressId != addressId && a.DeletedAt == null)
                .ToListAsync();

            foreach (var addr in existingDefaults)
            {
                addr.IsDefault = false;
            }

            // 設定當前地址為預設
            address.IsDefault = true;
            await _context.SaveChangesAsync();

            return true;
        }

        private AddressResponseDTO MapToResponseDTO(ShippingAddress address)
        {
            var addressTypeText = address.AddressType switch
            {
                0 => "一般宅配地址",
                1 => "超商取貨地址",
                _ => "未知"
            };

            var storeTypeText = address.StoreType switch
            {
                0 => "7-Eleven",
                1 => "FamilyMart",
                2 => "OK",
                3 => "Hi-Life",
                _ => ""
            };

            var fullAddress = address.AddressType == 1
                ? $"{address.City}{address.District}{storeTypeText}{address.StoreName}{address.StoreBranch}"
                : $"{address.City}{address.District}{address.AddressDetail}";

            return new AddressResponseDTO
            {
                AddressId = address.AddressId,
                UserId = address.UserId,
                RecipientName = address.RecipientName,
                PhoneNumber = address.PhoneNumber,
                AddressType = address.AddressType,
                AddressTypeText = addressTypeText,
                StoreType = address.StoreType,
                StoreName = address.StoreName,
                StoreBranch = address.StoreBranch,
                City = address.City,
                District = address.District,
                AddressDetail = address.AddressDetail,
                IsDefault = address.IsDefault,
                FullAddress = fullAddress
            };
        }
    }
} 