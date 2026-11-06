using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using api_backend.Services;
using api_backend.Models.DTO;
using System.Security.Claims;

namespace api_backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AddressController : ControllerBase
    {
        private readonly IAddressService _addressService;

        public AddressController(IAddressService addressService)
        {
            _addressService = addressService;
        }

        // GET: api/address
        [HttpGet]
        public async Task<IActionResult> GetUserAddresses()
        {
            try
            {
                var userId = GetCurrentUserId();
                var addresses = await _addressService.GetUserAddressesAsync(userId);
                return Ok(new { success = true, data = addresses });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // GET: api/address/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetAddress(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var address = await _addressService.GetAddressByIdAsync(id, userId);
                return Ok(new { success = true, data = address });
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // POST: api/address
        [HttpPost]
        public async Task<IActionResult> CreateAddress([FromBody] AddressRequestDTO request)
        {
            try
            {
                // 加上詳細的診斷日誌
                Console.WriteLine($"收到新增地址請求");
                Console.WriteLine($"請求內容: {System.Text.Json.JsonSerializer.Serialize(request)}");
                Console.WriteLine($"ModelState.IsValid: {ModelState.IsValid}");
                
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    Console.WriteLine($"驗證錯誤: {string.Join(", ", errors)}");
                    return BadRequest(new { success = false, message = "驗證失敗", errors });
                }

                var userId = GetCurrentUserId();
                Console.WriteLine($"用戶ID: {userId}");
                
                var address = await _addressService.CreateAddressAsync(request, userId);
                return CreatedAtAction(nameof(GetAddress), new { id = address.AddressId }, 
                    new { success = true, data = address, message = "地址新增成功" });
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"ArgumentException: {ex.Message}");
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // PUT: api/address/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAddress(int id, [FromBody] AddressRequestDTO request)
        {
            try
            {
                // 加上詳細的診斷日誌
                Console.WriteLine($"收到更新地址請求 - ID: {id}");
                Console.WriteLine($"請求內容: {System.Text.Json.JsonSerializer.Serialize(request)}");
                Console.WriteLine($"ModelState.IsValid: {ModelState.IsValid}");
                
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    Console.WriteLine($"驗證錯誤: {string.Join(", ", errors)}");
                    return BadRequest(new { success = false, message = "驗證失敗", errors });
                }

                var userId = GetCurrentUserId();
                Console.WriteLine($"用戶ID: {userId}");
                
                var address = await _addressService.UpdateAddressAsync(id, request, userId);
                return Ok(new { success = true, data = address, message = "地址更新成功" });
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"ArgumentException: {ex.Message}");
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // DELETE: api/address/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAddress(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _addressService.DeleteAddressAsync(id, userId);
                return Ok(new { success = true, message = "地址刪除成功" });
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // POST: api/address/{id}/default
        [HttpPost("{id}/default")]
        public async Task<IActionResult> SetDefaultAddress(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _addressService.SetDefaultAddressAsync(id, userId);
                return Ok(new { success = true, message = "預設地址設定成功" });
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("UserId");
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                throw new UnauthorizedAccessException("無法取得用戶ID");
            }
            return userId;
        }
    }
} 