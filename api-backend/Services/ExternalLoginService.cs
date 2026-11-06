using Microsoft.EntityFrameworkCore;
using api_backend.Models;
using api_backend.Models.DTO;

namespace api_backend.Services
{
    public class ExternalLoginService : IExternalLoginService
    {
        //依賴注入
        private readonly TeamDb2Context _context;
        private readonly ILogger<ExternalLoginService> _logger;

        //建構函式
        public ExternalLoginService(TeamDb2Context context, ILogger<ExternalLoginService> logger)
        {
            _context = context;
            _logger = logger;
        }

        //讀取使用者的外部登入資訊
        public async Task<List<ExternalLoginDTO>> GetUserExternalLoginsAsync(int userId)
        {
            try
            {
                var externalLogins = await _context.ExternalLogins
                    .Where(el => el.UserId == userId)
                    .Select(el => new ExternalLoginDTO
                    {
                        LoginId = el.LoginId,
                        LoginProvider = el.LoginProvider, // 登入提供者,ex: Google, Facebook
                        ProviderId = el.ProviderId, // 提供者ID,ex: Google的UserId
                        CreateDate = el.Createdate,
                        IsConnected = true
                    })
                    .ToListAsync();

                return externalLogins; // 返回外部登入資訊列表
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting external logins for user {UserId}", userId);
                throw;
            }
        }

        //綁定外部登入
        public async Task<ExternalLoginResponseDTO> BindExternalLoginAsync(int userId, ExternalLoginRequestDTO request)
        {
            try
            {
                // 檢查是否已經綁定 existingLogin 
                var existingLogin = await _context.ExternalLogins
                    .FirstOrDefaultAsync(el => el.UserId == userId && el.LoginProvider == request.LoginProvider);
                // 如果已經綁定，則返回錯誤訊息
                if (existingLogin != null) 
                {
                    return new ExternalLoginResponseDTO
                    {
                        Success = false,
                        Message = "此第三方帳號已經綁定",
                        LoginProvider = request.LoginProvider,
                        IsNewBinding = false
                    };               
                }

                // 檢查此第三方帳號是否已被其他用戶綁定
                var existingProviderLogin = await _context.ExternalLogins
                    .FirstOrDefaultAsync(el => el.LoginProvider == request.LoginProvider && el.ProviderId == request.ProviderId); //找資料表中有相同的登入提供者和提供者ID
                // 如果已經被其他用戶綁定，則返回錯誤訊息
                if (existingProviderLogin != null) 
                {
                    return new ExternalLoginResponseDTO
                    {
                        Success = false,
                        Message = "此第三方帳號已被其他用戶綁定",
                        LoginProvider = request.LoginProvider,
                        IsNewBinding = false
                    };
                }

                // 如果沒有綁定，則新增一筆外部登入資料
                var newExternalLogin = new ExternalLogin
                {
                    UserId = userId,
                    LoginProvider = request.LoginProvider,
                    ProviderId = request.ProviderId,
                    Token = request.AccessToken,
                    Createdate = DateTime.UtcNow
                };

                _context.ExternalLogins.Add(newExternalLogin); // 新增外部登入資料到資料庫
                await _context.SaveChangesAsync(); // 儲存變更

                // 返回成功訊息
                return new ExternalLoginResponseDTO
                {
                    Success = true,
                    Message = "第三方帳號綁定成功",
                    LoginProvider = request.LoginProvider,
                    IsNewBinding = true
                };
            }
            catch (Exception ex) 
            {
                _logger.LogError(ex, "Error binding external login for user {UserId}", userId);
                throw;
            }      
        }

        //解除綁定外部登入
        public async Task<ExternalLoginResponseDTO> UnbindExternalLoginAsync(int userId, string loginProvider)
        {
            try 
            {
                var externalLogin = await _context.ExternalLogins
                    .FirstOrDefaultAsync(el => el.UserId == userId && el.LoginProvider == loginProvider); //找到要解除綁定的外部登入資料(相同的使用者ID和登入提供者)

                if (externalLogin == null) 
                {
                    // 如果找不到要解除綁定的外部登入資料，則返回錯誤訊息
                    return new ExternalLoginResponseDTO
                    {
                        Success = false,
                        Message = "找不到要解除綁定的第三方帳號",
                        LoginProvider = loginProvider,
                        IsNewBinding = false
                    };
                }

                // 如果找到要解除綁定的外部登入資料，則從資料庫中移除它
                _context.ExternalLogins.Remove(externalLogin); // 從資料庫中移除外部登入資料
                await _context.SaveChangesAsync(); // 儲存變更

                // 返回解除綁定成功訊息
                return new ExternalLoginResponseDTO
                {
                    Success = true,
                    Message = "第三方帳號解除綁定成功",
                    LoginProvider = loginProvider,
                    IsNewBinding = false
                };
            }
            catch (Exception ex) 
            {
                _logger.LogError(ex, "Error unbinding external login for user {UserId}", userId);
                throw;
            }
        }

        //驗證外部登入的Token
        public async Task<bool> ValidateExternalTokenAsync(string provider, string token)
        {
            try
            {
                switch (provider.ToLower())
                {
                    case "google":
                        return await ValidateGoogleTokenAsync(token);
                    case "facebook":
                        return await ValidateFacebookTokenAsync(token);
                    case "line":
                        return await ValidateLineTokenAsync(token);
                    default:
                        _logger.LogWarning("Unsupported login provider: {Provider}", provider);
                        return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating external token for provider {Provider}", provider);
                return false;
            }
        }

        // 驗證 Google Token (Access Token)
        private async Task<bool> ValidateGoogleTokenAsync(string token)
        {
            try
            {
                using var httpClient = new HttpClient();
                // 使用 Google 的 tokeninfo endpoint 來驗證 access token
                var response = await httpClient.GetAsync($"https://oauth2.googleapis.com/tokeninfo?access_token={token}");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation("Google access token validation successful");
                    return true;
                }
                
                _logger.LogWarning("Google access token validation failed with status: {StatusCode}", response.StatusCode);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating Google access token");
                return false;
            }
        }

        // 驗證 Facebook Token
        private async Task<bool> ValidateFacebookTokenAsync(string token)
        {
            try
            {
                using var httpClient = new HttpClient();
                var response = await httpClient.GetAsync($"https://graph.facebook.com/me?access_token={token}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating Facebook token");
                return false;
            }
        }

        // 驗證 LINE Token
        private async Task<bool> ValidateLineTokenAsync(string token)
        {
            try
            {
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                var response = await httpClient.GetAsync("https://api.line.me/v2/profile");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating LINE token");
                return false;
            }
        }


    }
}
