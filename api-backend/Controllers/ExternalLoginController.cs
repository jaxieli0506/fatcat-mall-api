using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using api_backend.Models.DTO;
using api_backend.Services;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace api_backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ExternalLoginController : ControllerBase
    {
        //依賴注入
        private readonly IExternalLoginService _externalLoginService;
        private readonly ILogger<ExternalLoginController> _logger;
        private readonly IConfiguration _configuration;

        //建構函式
        public ExternalLoginController(
            IExternalLoginService externalLoginService,
            ILogger<ExternalLoginController> logger,
            IConfiguration configuration)
        {
            _externalLoginService = externalLoginService;
            _logger = logger;
            _configuration = configuration;
        }

        // 測試端點 - 不需要認證
        // GET: api/ExternalLogin/test
        [HttpGet("test")]
        public IActionResult Test()
        {
            return Ok(new { 
                message = "ExternalLogin API 正常運作", 
                timestamp = DateTime.UtcNow,
                version = "1.0.0"
            });
        }

        // 取得使用者的外部登入資訊
        // GET: api/ExternalLogin/connections
        [Authorize]
        [HttpGet("connections")]
        public async Task<IActionResult> GetUserConnections()
        {
            try
            {
                // 從 Claims 中取得用戶 ID
                // 使用與其他控制器一致的 UserId claim
                var userIdClaim = User.FindFirst("UserId");
                // 檢查用戶 ID 是否存在且可轉換為整數
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized(new { message = "無效的用戶身份" });
                }

                // 取得使用者的外部登入資訊(呼叫externalLoginService中的GetUserExternalLoginsAsync方法)
                //會回傳 List<ExternalLoginDTO> (包含登入提供者、提供者ID、建立日期等資訊)(也就是GetUserExternalLoginsAsync方法中的 externalLogins)
                var connections = await _externalLoginService.GetUserExternalLoginsAsync(userId);
                return Ok(new { success = true, data = connections });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user external connections");
                return StatusCode(500, new { message = "取得第三方帳號連結時發生錯誤" });
            }
        }

        //新增或綁定外部登入
        // POST: api/ExternalLogin/bind
        [Authorize]
        [HttpPost("bind")]
        public async Task<IActionResult> BindExternalLogin([FromBody] ExternalLoginRequestDTO request)
        {
            try
            {
                // 從 Claims 中取得用戶 ID
                var userIdClaim = User.FindFirst("UserId");

                // 如果用戶 ID Claim 不存在或無法轉換為整數，則返回未授權
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized(new { message = "無效的用戶身份" });
                }

                // 驗證第三方 Token
                var isValidToken = await _externalLoginService.ValidateExternalTokenAsync(
                    request.LoginProvider,
                    request.AccessToken
                );

                if (!isValidToken)
                {
                    return BadRequest(new { message = "無效的第三方登入 Token" });
                }

                // 呼叫服務層綁定外部登入
                var result = await _externalLoginService.BindExternalLoginAsync(userId, request);

                if (result.Success)
                {
                    return Ok(result);
                }
                else
                {
                    return BadRequest(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error binding external login");
                return StatusCode(500, new { message = "綁定第三方帳號時發生錯誤" });
            }
        }

        // 解除綁定外部登入
        // POST: api/ExternalLogin/unbind/{provider}
        [Authorize]
        [HttpPost("unbind/{provider}")]
        public async Task<IActionResult> UnbindExternalLogin(string provider)
        {
            try
            {
                var userIdClaim = User.FindFirst("UserId");
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized(new { message = "無效的用戶身份" });
                }

                // 呼叫服務層(ExternalLoginService.cs中UnbindExternalLoginAsync方法)解除綁定外部登入
                var result = await _externalLoginService.UnbindExternalLoginAsync(userId, provider);
                
                if (result.Success)
                {
                    return Ok(result);
                }
                else
                {
                    return BadRequest(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unbinding external login");
                return StatusCode(500, new { message = "解除綁定第三方帳號時發生錯誤" });
            }
        }

        // LINE Token 交換端點
        // POST: api/ExternalLogin/line-token-exchange
        [HttpPost("line-token-exchange")]
        public async Task<IActionResult> ExchangeLineToken([FromBody] LineTokenExchangeRequest request)
        {
            try
            {
                _logger.LogInformation("🔄 開始處理 LINE token 交換請求");
                _logger.LogInformation("📋 請求參數: Code={Code}, RedirectUri={RedirectUri}", 
                    request.Code?.Length > 10 ? request.Code.Substring(0, 10) + "..." : request.Code, 
                    request.RedirectUri);

                // 取得 LINE Channel 設定
                var lineChannelId = _configuration["ExternalLogin:Line:ChannelId"];
                var lineChannelSecret = _configuration["ExternalLogin:Line:ChannelSecret"];

                _logger.LogInformation("🔧 LINE 設定檢查: ChannelId={ChannelId}, HasSecret={HasSecret}", 
                    lineChannelId, 
                    !string.IsNullOrEmpty(lineChannelSecret));

                if (string.IsNullOrEmpty(lineChannelId) || string.IsNullOrEmpty(lineChannelSecret))
                {
                    _logger.LogError("❌ LINE Channel 設定不完整");
                    return BadRequest(new { message = "LINE Channel 設定不完整" });
                }

                // 準備交換 token 的請求
                using var httpClient = new HttpClient();
                var tokenRequest = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("grant_type", "authorization_code"),
                    new KeyValuePair<string, string>("code", request.Code),
                    new KeyValuePair<string, string>("redirect_uri", request.RedirectUri),
                    new KeyValuePair<string, string>("client_id", lineChannelId),
                    new KeyValuePair<string, string>("client_secret", lineChannelSecret)
                });

                _logger.LogInformation("📡 向 LINE API 發送 token 交換請求");

                // 向 LINE 取得 access token
                var tokenResponse = await httpClient.PostAsync("https://api.line.me/oauth2/v2.1/token", tokenRequest);
                
                _logger.LogInformation("📥 LINE Token API 回應: Status={StatusCode}", tokenResponse.StatusCode);

                if (!tokenResponse.IsSuccessStatusCode)
                {
                    var errorContent = await tokenResponse.Content.ReadAsStringAsync();
                    _logger.LogError("❌ LINE token exchange failed: Status={StatusCode}, Error={Error}", 
                        tokenResponse.StatusCode, errorContent);
                    return BadRequest(new { message = "LINE token 交換失敗", details = errorContent });
                }

                var tokenContent = await tokenResponse.Content.ReadAsStringAsync();
                _logger.LogInformation("✅ 成功取得 LINE token 回應");
                _logger.LogInformation("📄 LINE Token API 原始回應內容: {Content}", tokenContent);

                var tokenData = JsonSerializer.Deserialize<LineTokenResponse>(tokenContent, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                _logger.LogInformation("🔍 反序列化結果: AccessToken={HasToken}, TokenType={TokenType}", 
                    !string.IsNullOrEmpty(tokenData?.AccessToken), 
                    tokenData?.TokenType);

                if (tokenData?.AccessToken == null)
                {
                    _logger.LogError("❌ LINE token 回應中沒有 access_token");
                    return BadRequest(new { message = "LINE token 回應格式錯誤" });
                }

                _logger.LogInformation("🔑 成功解析 access token");

                // 使用 access token 取得用戶資料
                httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenData.AccessToken);
                
                _logger.LogInformation("👤 向 LINE API 請求用戶資料");
                var profileResponse = await httpClient.GetAsync("https://api.line.me/v2/profile");
                
                _logger.LogInformation("📥 LINE Profile API 回應: Status={StatusCode}", profileResponse.StatusCode);

                if (!profileResponse.IsSuccessStatusCode)
                {
                    var profileErrorContent = await profileResponse.Content.ReadAsStringAsync();
                    _logger.LogError("❌ 無法取得 LINE 用戶資料: Status={StatusCode}, Error={Error}", 
                        profileResponse.StatusCode, profileErrorContent);
                    return BadRequest(new { message = "無法取得 LINE 用戶資料", details = profileErrorContent });
                }

                var profileContent = await profileResponse.Content.ReadAsStringAsync();
                _logger.LogInformation("✅ 成功取得 LINE 用戶資料");

                var profileData = JsonSerializer.Deserialize<LineProfileResponse>(profileContent, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                if (profileData?.UserId == null)
                {
                    _logger.LogError("❌ LINE 用戶資料中沒有 UserId");
                    return BadRequest(new { message = "LINE 用戶資料格式錯誤" });
                }

                var result = new
                {
                    accessToken = tokenData.AccessToken,
                    userID = profileData.UserId,
                    email = "", // LINE 不一定提供 email
                    name = profileData.DisplayName
                };

                _logger.LogInformation("🎉 LINE 登入處理完成: UserID={UserId}, Name={Name}", 
                    profileData.UserId, profileData.DisplayName);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ LINE token 交換時發生未預期錯誤");
                return StatusCode(500, new { message = "LINE token 交換時發生錯誤", details = ex.Message });
            }
        }
    }

    // LINE Token 交換請求 DTO
    public class LineTokenExchangeRequest
    {
        public string Code { get; set; }
        public string RedirectUri { get; set; }
    }

    // LINE Token 響應 DTO
    public class LineTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; }
        
        [JsonPropertyName("token_type")]
        public string TokenType { get; set; }
        
        [JsonPropertyName("refresh_token")]
        public string RefreshToken { get; set; }
        
        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
        
        [JsonPropertyName("scope")]
        public string Scope { get; set; }
        
        [JsonPropertyName("id_token")]
        public string IdToken { get; set; }
    }

    // LINE 用戶資料響應 DTO
    public class LineProfileResponse
    {
        [JsonPropertyName("userId")]
        public string UserId { get; set; }
        
        [JsonPropertyName("displayName")]
        public string DisplayName { get; set; }
        
        [JsonPropertyName("pictureUrl")]
        public string PictureUrl { get; set; }
        
        [JsonPropertyName("statusMessage")]
        public string StatusMessage { get; set; }
    }
}