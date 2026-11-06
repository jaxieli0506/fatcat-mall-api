using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using api_backend.Models.DTO;
using api_backend.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using api_backend.Models;
using Microsoft.EntityFrameworkCore;

namespace api_backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController] //啟用API控制器
    public class AuthController : ControllerBase //繼承自 ControllerBase，適用於API控制器，而非controller(mvc控制器)
    {
        private readonly IAuthService _authService;
        private readonly IExternalLoginService _externalLoginService;
        private readonly TeamDb2Context _context;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IAuthService authService, 
            IExternalLoginService externalLoginService,
            TeamDb2Context context,
            ILogger<AuthController> logger)
        {
            _authService = authService;
            _externalLoginService = externalLoginService;
            _context = context;
            _logger = logger;
        }
        //登入
        // 接收JSON格式的登入請求,回傳JWT Token和用戶資料
        // POST: api/auth/login
        [HttpPost("login")]   
        public async Task<IActionResult> Login([FromBody] LoginRequestDTO request)
        {
            try
            {
                if (request == null || string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
                {
                    return BadRequest(new { message = "請提供有效的電子郵件和密碼" });
                }

                // 調用認證服務
                var result = await _authService.LoginAsync(request);
                if (result == null)
                {
                    return Unauthorized(new { message = "電子郵件或密碼錯誤" });
                }

                // 登入成功，回傳JWT Token和用戶資料
                return Ok(new
                {
                    success = true,
                    message = "登入成功",
                    data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "伺服器內部錯誤", error = ex.Message });
            }
        }

        // 註冊
        // POST: api/Auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDTO request)
        {
            try
            {
                //驗證請求數據
                if (request == null || string.IsNullOrEmpty(request.Name) || string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
                {
                    return BadRequest(new { message = "請提供完整的註冊資訊" });
                }

                //調用認證服務
                var result = await _authService.RegisterAsync(request);
                if (!result)
                {
                    return BadRequest(new { message = "註冊失敗，電子郵件已存在" });
                }

                //註冊成功
                return Ok(new
                {
                    success = true,
                    message = "註冊成功"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "伺服器內部錯誤", error = ex.Message });
            }
        }

        // 取得目前登入用戶資訊
        // GET: api/Auth/profile
        [HttpGet("profile")]
        [Authorize] // 需要JWT Token才能存取
        public async Task<IActionResult> GetProfile()
        {
            try
            {
                // 從JWT Token中取得用戶ID
                var userIdClaim = User.FindFirst("UserId")?.Value;

                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new { message = "無效的認證Token" });
                }

                // 從資料庫中獲取最新的用戶資料
                var user = await _authService.GetUserByIdAsync(userId);
                
                if (user == null)
                {
                    return NotFound(new { message = "找不到用戶資料" });
                }

                // 回傳最新的用戶資料
                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        userId = user.UserId,
                        name = user.Name,
                        email = user.Email,
                        phone = user.Phone,
                        gender = user.Gender,
                        birthdate = user.Birthdate,
                        picPath = user.PicPath
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "伺服器內部錯誤", error = ex.Message });
            }
        }

        // 登出
        // POST: api/Auth/logout
        [HttpPost("logout")]
        [Authorize]
        public IActionResult Logout()
        {
            try
            {
                // JWT是無狀態的，登出主要由前端處理（移除Token）
                // 這裡可以記錄登出日誌或執行其他清理工作
                return Ok(new { success = true, message = "登出成功" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "伺服器內部錯誤", error = ex.Message });
            }

        }

        // 上傳用戶頭像
        [HttpPost("upload-avatar")]
        [Authorize]
        public async Task<IActionResult> UploadAvatar(IFormFile file)
        {
            try
            {
                // 1. 驗證檔案
                if (file == null || file.Length == 0)
                    return BadRequest(new { message = "請選擇圖片檔案" });

                // 2. 檢查檔案類型
                var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif" };
                if (!allowedTypes.Contains(file.ContentType))
                    return BadRequest(new { message = "只支援 JPG、PNG、GIF 格式" });

                // 3. 檢查檔案大小（2MB）
                if (file.Length > 2 * 1024 * 1024)
                    return BadRequest(new { message = "檔案大小不能超過 2MB" });

                // 4. 取得用戶ID
                var userIdClaim = User.FindFirst("UserId")?.Value;
                if (!int.TryParse(userIdClaim, out int userId))
                    return Unauthorized(new { message = "無效的認證Token" });

                // 5. 產生唯一檔名
                var fileExtension = Path.GetExtension(file.FileName);
                var fileName = $"user_{userId}_{DateTime.Now:yyyyMMdd_HHmmss}{fileExtension}";

                // 6. 設定儲存路徑
                var uploadPath = Path.Combine("wwwroot", "Userpic");
                if (!Directory.Exists(uploadPath))
                    Directory.CreateDirectory(uploadPath);

                var filePath = Path.Combine(uploadPath, fileName);

                // 7. 儲存檔案
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // 8. 更新用戶資料中的圖片路徑
                var relativePath = $"/Userpic/{fileName}";
                var updateRequest = new UpdateUserRequestDTO { PicPath = relativePath };
                var result = await _authService.UpdateUserAsync(userId, updateRequest);

                if (!result)
                    return BadRequest(new { message = "更新用戶頭像失敗" });

                // 9. 返回結果
                return Ok(new
                {
                    success = true,
                    message = "頭像上傳成功",
                    data = new { picPath = relativePath }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "上傳失敗", error = ex.Message });
            }
        }

        // 更新用戶資料
        // PUT: api/Auth/profile
        [HttpPut("profile")]
        [Authorize]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateUserRequestDTO request)
        {
            try
            {
                var userIdClaim = User.FindFirst("UserId")?.Value;

                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new { message = "無效的認證Token" });
                }

                var result = await _authService.UpdateUserAsync(userId, request);

                if (!result)
                {
                    return BadRequest(new { message = "更新失敗，請稍後再試" });
                }

                // 取得更新後的用戶資料
                var updatedUser = await _authService.GetUserByIdAsync(userId);

                return Ok(new
                {
                    success = true,
                    message = "個人資料更新成功",
                    data = new
                    {
                        userId = updatedUser?.UserId,
                        name = updatedUser?.Name,
                        email = updatedUser?.Email,
                        phone = updatedUser?.Phone,
                        gender = updatedUser?.Gender,
                        birthdate = updatedUser?.Birthdate,
                        picPath = updatedUser?.PicPath
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "伺服器內部錯誤", error = ex.Message });
            }
        }

        // 密碼變更
        // POST: api/auth/change-password
        [HttpPost("change-password")]
        [Authorize] // 需要JWT Token才能存取
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDTO request)
        {
            try
            {
                // 1. 驗證請求資料
                if (request == null || string.IsNullOrEmpty(request.CurrentPassword) ||
                    string.IsNullOrEmpty(request.NewPassword) || string.IsNullOrEmpty(request.ConfirmPassword))
                {
                    return BadRequest(new { success = false, message = "請提供完整的密碼變更資訊" });
                }

                // 2. 從JWT Token中取得用戶ID
                var userIdClaim = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new { success = false, message = "無效的認證Token" });
                }

                // 3. 調用認證服務進行密碼變更
                var result = await _authService.ChangePasswordAsync(userId, request);

                if (result)
                {
                    return Ok(new { success = true, message = "密碼變更成功" });
                }
                else
                {
                    return BadRequest(new { success = false, message = "密碼變更失敗，請檢查目前密碼是否正確" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "伺服器內部錯誤", error = ex.Message });
            }
        }

        // 忘記密碼
        // POST: api/Auth/forgot-password
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDTO request)
        {
            try
            {
                _logger.LogInformation("收到忘記密碼請求: {Email}", request.Email);

                // 1. 驗證模型狀態
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage);
                    return BadRequest(new { success = false, message = "輸入資料無效", errors });
                }

                // 2. 調用認證服務處理忘記密碼
                var result = await _authService.ForgotPasswordAsync(request);
                _logger.LogInformation("AuthService.ForgotPasswordAsync returned: {Result}", result);

                if (result)
                {
                    _logger.LogInformation("Forgot password result is true. Returning 200 OK.");
                    return Ok(new { 
                        success = true, 
                        message = "如果該電子郵件地址存在於我們的系統中，您將收到重設密碼的郵件" 
                    });
                }
                else
                {
                    _logger.LogWarning("Forgot password result is false. Returning 400 BadRequest.");
                    return BadRequest(new { success = false, message = "處理請求時發生錯誤" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "忘記密碼處理錯誤");
                return StatusCode(500, new { success = false, message = "伺服器內部錯誤" });
            }
        }

        // 重設密碼
        // POST: api/Auth/reset-password
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDTO request)
        {
            try
            {
                _logger.LogInformation("收到重設密碼請求: {Email}", request.Email);

                // 1. 驗證模型狀態
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage);
                    return BadRequest(new { success = false, message = "輸入資料無效", errors });
                }

                // 2. 調用認證服務處理重設密碼
                var result = await _authService.ResetPasswordAsync(request);

                if (result)
                {
                    return Ok(new { success = true, message = "密碼重設成功，請使用新密碼登入" });
                }
                else
                {
                    return BadRequest(new { 
                        success = false, 
                        message = "重設密碼失敗，請確認重設連結是否有效或是否已過期" 
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "重設密碼處理錯誤");
                return StatusCode(500, new { success = false, message = "伺服器內部錯誤" });
            }
        }

        // Facebook 登入
        // POST: api/Auth/facebook-login
        [HttpPost("facebook-login")]
        public async Task<IActionResult> FacebookLogin([FromBody] ExternalLoginRequestDTO request)
        {
            try
            {
                _logger.LogInformation("收到 Facebook 登入請求: {Email}", request.Email);

                // 1. 驗證必要參數
                if (string.IsNullOrEmpty(request.AccessToken) || 
                    string.IsNullOrEmpty(request.ProviderId) || 
                    string.IsNullOrEmpty(request.Email))
                {
                    return BadRequest(new { 
                        success = false, 
                        message = "請提供完整的 Facebook 登入資訊" 
                    });
                }

                // 2. 驗證 Facebook Token
                var isValidToken = await _externalLoginService.ValidateExternalTokenAsync(
                    "Facebook", request.AccessToken);
                
                if (!isValidToken)
                {
                    return BadRequest(new { 
                        success = false, 
                        message = "無效的 Facebook Token" 
                    });
                }

                // 3. 檢查此 Facebook 帳號是否已綁定
                var existingBinding = await _context.ExternalLogins
                    .FirstOrDefaultAsync(el => 
                        el.LoginProvider == "Facebook" && 
                        el.ProviderId == request.ProviderId);

                if (existingBinding != null)
                {
                    // 3a. 已綁定 - 取得對應的用戶資料
                    var user = await _context.Users
                        .FirstOrDefaultAsync(u => u.UserId == existingBinding.UserId);
                    
                    if (user == null)
                    {
                        return BadRequest(new { 
                            success = false, 
                            message = "綁定的用戶帳號不存在，請聯繫客服" 
                        });
                    }
                    
                    _logger.LogInformation("Facebook 帳號已綁定，用戶ID: {UserId}", user.UserId);

                    // 更新最後登入時間
                    user.LastLogin = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    // 生成 JWT Token - 直接使用 JwtService
                    var jwtService = HttpContext.RequestServices.GetRequiredService<JwtService>();
                    var token = jwtService.GenerateToken(user, false); // false = 不記住我
                    var tokenExpiry = jwtService.GetTokenExpiry(false);

                    var loginResponse = new LoginResponseDTO
                    {
                        Token = token,
                        UserId = user.UserId,
                        Name = user.Name,
                        Email = user.Email,
                        ExpiresAt = tokenExpiry,
                        PicPath = user.PicPath
                    };

                    return Ok(new
                    {
                        success = true,
                        message = "Facebook 登入成功",
                        data = loginResponse
                    });
                }
                else
                {
                    // 3b. 未綁定 - 檢查是否有相同 Email 的用戶
                    var existingUser = await _context.Users
                        .FirstOrDefaultAsync(u => u.Email == request.Email);

                    if (existingUser != null)
                    {
                        // 有相同 Email 的用戶，建議綁定到現有帳號
                        return Ok(new
                        {
                            success = false,
                            needBinding = true,
                            message = "此電子郵件已被註冊。請先登入您的帳號，然後在個人設定中綁定 Facebook。",
                            email = request.Email
                        });
                    }

                    // 沒有相同 Email，創建新用戶並自動綁定
                    _logger.LogInformation("創建新用戶並綁定 Facebook: {Email}", request.Email);

                    var newUser = new User
                    {
                        Name = request.Name ?? "Facebook 用戶",
                        Email = request.Email,
                        // 為第三方登入用戶生成隨機密碼
                        Password = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()),
                        Registerdate = DateTime.UtcNow,
                        LastLogin = DateTime.UtcNow,
                        Status = 1,
                        IsThirdparty = true // 標記為第三方用戶
                    };

                    _context.Users.Add(newUser);
                    await _context.SaveChangesAsync();

                    // 創建第三方登入記錄
                    var externalLogin = new ExternalLogin
                    {
                        UserId = newUser.UserId,
                        LoginProvider = "Facebook",
                        ProviderId = request.ProviderId,
                        Token = request.AccessToken,
                        Createdate = DateTime.UtcNow
                    };

                    _context.ExternalLogins.Add(externalLogin);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("新用戶創建成功，用戶ID: {UserId}, Facebook 綁定完成", newUser.UserId);

                    // 生成 JWT Token
                    var jwtService = HttpContext.RequestServices.GetRequiredService<JwtService>();
                    var token = jwtService.GenerateToken(newUser, false); // false = 不記住我
                    var tokenExpiry = jwtService.GetTokenExpiry(false);

                    var loginResponse = new LoginResponseDTO
                    {
                        Token = token,
                        UserId = newUser.UserId,
                        Name = newUser.Name,
                        Email = newUser.Email,
                        ExpiresAt = tokenExpiry,
                        PicPath = newUser.PicPath
                    };

                    return Ok(new
                    {
                        success = true,
                        message = "Facebook 帳號註冊並登入成功",
                        isNewUser = true,
                        data = loginResponse
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Facebook 登入處理失敗: {Message}", ex.Message);
                return StatusCode(500, new { 
                    success = false, 
                    message = "伺服器內部錯誤，請稍後再試" 
                });
            }
        }

        // Google 登入
        // POST: api/Auth/google-login
        [HttpPost("google-login")]
        public async Task<IActionResult> GoogleLogin([FromBody] ExternalLoginRequestDTO request)
        {
            try
            {
                _logger.LogInformation("收到 Google 登入請求: {Email}", request.Email);

                // 1. 驗證必要參數
                if (string.IsNullOrEmpty(request.AccessToken) || 
                    string.IsNullOrEmpty(request.ProviderId) || 
                    string.IsNullOrEmpty(request.Email))
                {
                    return BadRequest(new { 
                        success = false, 
                        message = "請提供完整的 Google 登入資訊" 
                    });
                }

                // 2. 驗證 Google Token
                var isValidToken = await _externalLoginService.ValidateExternalTokenAsync(
                    "Google", request.AccessToken);
                
                if (!isValidToken)
                {
                    return BadRequest(new { 
                        success = false, 
                        message = "無效的 Google Token" 
                    });
                }

                // 3. 檢查此 Google 帳號是否已綁定
                var existingBinding = await _context.ExternalLogins
                    .FirstOrDefaultAsync(el => 
                        el.LoginProvider == "Google" && 
                        el.ProviderId == request.ProviderId);

                if (existingBinding != null)
                {
                    // 3a. 已綁定 - 取得對應的用戶資料
                    var user = await _context.Users
                        .FirstOrDefaultAsync(u => u.UserId == existingBinding.UserId);
                    
                    if (user == null)
                    {
                        return BadRequest(new { 
                            success = false, 
                            message = "綁定的用戶帳號不存在，請聯繫客服" 
                        });
                    }
                    
                    _logger.LogInformation("Google 帳號已綁定，用戶ID: {UserId}", user.UserId);

                    // 更新最後登入時間
                    user.LastLogin = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    // 生成 JWT Token - 直接使用 JwtService
                    var jwtService = HttpContext.RequestServices.GetRequiredService<JwtService>();
                    var token = jwtService.GenerateToken(user, false); // false = 不記住我
                    var tokenExpiry = jwtService.GetTokenExpiry(false);

                    var loginResponse = new LoginResponseDTO
                    {
                        Token = token,
                        UserId = user.UserId,
                        Name = user.Name,
                        Email = user.Email,
                        ExpiresAt = tokenExpiry,
                        PicPath = user.PicPath
                    };

                    return Ok(new
                    {
                        success = true,
                        message = "Google 登入成功",
                        data = loginResponse
                    });
                }
                else
                {
                    // 3b. 未綁定 - 檢查是否有相同 Email 的用戶
                    var existingUser = await _context.Users
                        .FirstOrDefaultAsync(u => u.Email == request.Email);

                    if (existingUser != null)
                    {
                        // 有相同 Email 的用戶，建議綁定到現有帳號
                        return Ok(new
                        {
                            success = false,
                            needBinding = true,
                            message = "此電子郵件已被註冊。請先登入您的帳號，然後在個人設定中綁定 Google。",
                            email = request.Email
                        });
                    }

                    // 沒有相同 Email，創建新用戶並自動綁定
                    _logger.LogInformation("創建新用戶並綁定 Google: {Email}", request.Email);

                    var newUser = new User
                    {
                        Name = request.Name ?? "Google 用戶",
                        Email = request.Email,
                        // 為第三方登入用戶生成隨機密碼
                        Password = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()),
                        Registerdate = DateTime.UtcNow,
                        LastLogin = DateTime.UtcNow,
                        Status = 1,
                        IsThirdparty = true // 標記為第三方用戶
                    };

                    _context.Users.Add(newUser);
                    await _context.SaveChangesAsync();

                    // 創建第三方登入記錄
                    var externalLogin = new ExternalLogin
                    {
                        UserId = newUser.UserId,
                        LoginProvider = "Google",
                        ProviderId = request.ProviderId,
                        Token = request.AccessToken,
                        Createdate = DateTime.UtcNow
                    };

                    _context.ExternalLogins.Add(externalLogin);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("新用戶創建成功，用戶ID: {UserId}, Google 綁定完成", newUser.UserId);

                    // 生成 JWT Token
                    var jwtService = HttpContext.RequestServices.GetRequiredService<JwtService>();
                    var token = jwtService.GenerateToken(newUser, false); // false = 不記住我
                    var tokenExpiry = jwtService.GetTokenExpiry(false);

                    var loginResponse = new LoginResponseDTO
                    {
                        Token = token,
                        UserId = newUser.UserId,
                        Name = newUser.Name,
                        Email = newUser.Email,
                        ExpiresAt = tokenExpiry,
                        PicPath = newUser.PicPath
                    };

                    return Ok(new
                    {
                        success = true,
                        message = "Google 帳號註冊並登入成功",
                        isNewUser = true,
                        data = loginResponse
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Google 登入處理失敗: {Message}", ex.Message);
                return StatusCode(500, new { 
                    success = false, 
                    message = "伺服器內部錯誤，請稍後再試" 
                });
            }
        }

        // LINE 登入
        // POST: api/Auth/line-login
        [HttpPost("line-login")]
        public async Task<IActionResult> LineLogin([FromBody] ExternalLoginRequestDTO request)
        {
            try
            {
                _logger.LogInformation("收到 LINE 登入請求: ProviderId={ProviderId}, Name={Name}", 
                    request.ProviderId, request.Name);

                // 1. 驗證必要參數
                if (string.IsNullOrEmpty(request.AccessToken) || 
                    string.IsNullOrEmpty(request.ProviderId))
                {
                    return BadRequest(new { 
                        success = false, 
                        message = "請提供完整的 LINE 登入資訊" 
                    });
                }

                // 2. 驗證 LINE Token
                var isValidToken = await _externalLoginService.ValidateExternalTokenAsync(
                    "LINE", request.AccessToken);
                
                if (!isValidToken)
                {
                    return BadRequest(new { 
                        success = false, 
                        message = "無效的 LINE Token" 
                    });
                }

                // 3. 檢查此 LINE 帳號是否已綁定
                var existingBinding = await _context.ExternalLogins
                    .FirstOrDefaultAsync(el => 
                        el.LoginProvider == "LINE" && 
                        el.ProviderId == request.ProviderId);

                if (existingBinding != null)
                {
                    // 3a. 已綁定 - 取得對應的用戶資料
                    var user = await _context.Users
                        .FirstOrDefaultAsync(u => u.UserId == existingBinding.UserId);
                    
                    if (user == null)
                    {
                        return BadRequest(new { 
                            success = false, 
                            message = "綁定的用戶帳號不存在，請聯繫客服" 
                        });
                    }
                    
                    _logger.LogInformation("LINE 帳號已綁定，用戶ID: {UserId}", user.UserId);

                    // 更新最後登入時間
                    user.LastLogin = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    // 生成 JWT Token
                    var jwtService = HttpContext.RequestServices.GetRequiredService<JwtService>();
                    var token = jwtService.GenerateToken(user, false); // false = 不記住我
                    var tokenExpiry = jwtService.GetTokenExpiry(false);

                    var loginResponse = new LoginResponseDTO
                    {
                        Token = token,
                        UserId = user.UserId,
                        Name = user.Name,
                        Email = user.Email,
                        ExpiresAt = tokenExpiry,
                        PicPath = user.PicPath
                    };

                    return Ok(new
                    {
                        success = true,
                        message = "LINE 登入成功",
                        data = loginResponse
                    });
                }
                else
                {
                    // 3b. 未綁定 - 檢查是否有相同 Email 的用戶（如果有提供 Email）
                    if (!string.IsNullOrEmpty(request.Email))
                    {
                        var existingUser = await _context.Users
                            .FirstOrDefaultAsync(u => u.Email == request.Email);

                        if (existingUser != null)
                        {
                            // 有相同 Email 的用戶，建議綁定到現有帳號
                            return Ok(new
                            {
                                success = false,
                                needBinding = true,
                                message = "此電子郵件已被註冊。請先登入您的帳號，然後在個人設定中綁定 LINE。",
                                email = request.Email
                            });
                        }
                    }

                    // 沒有相同 Email 或沒有提供 Email，創建新用戶並自動綁定
                    _logger.LogInformation("創建新用戶並綁定 LINE: ProviderId={ProviderId}, Name={Name}", 
                        request.ProviderId, request.Name);

                    var newUser = new User
                    {
                        Name = request.Name ?? "LINE 用戶",
                        Email = request.Email ?? $"line_{request.ProviderId}@temp.local", // LINE 可能不提供 email
                        // 為第三方登入用戶生成隨機密碼
                        Password = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()),
                        Registerdate = DateTime.UtcNow,
                        LastLogin = DateTime.UtcNow,
                        Status = 1,
                        IsThirdparty = true // 標記為第三方用戶
                    };

                    _context.Users.Add(newUser);
                    await _context.SaveChangesAsync();

                    // 創建第三方登入記錄
                    var externalLogin = new ExternalLogin
                    {
                        UserId = newUser.UserId,
                        LoginProvider = "LINE",
                        ProviderId = request.ProviderId,
                        Token = request.AccessToken,
                        Createdate = DateTime.UtcNow
                    };

                    _context.ExternalLogins.Add(externalLogin);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("新用戶創建成功，用戶ID: {UserId}, LINE 綁定完成", newUser.UserId);

                    // 生成 JWT Token
                    var jwtService = HttpContext.RequestServices.GetRequiredService<JwtService>();
                    var token = jwtService.GenerateToken(newUser, false); // false = 不記住我
                    var tokenExpiry = jwtService.GetTokenExpiry(false);

                    var loginResponse = new LoginResponseDTO
                    {
                        Token = token,
                        UserId = newUser.UserId,
                        Name = newUser.Name,
                        Email = newUser.Email,
                        ExpiresAt = tokenExpiry,
                        PicPath = newUser.PicPath
                    };

                    return Ok(new
                    {
                        success = true,
                        message = "LINE 帳號註冊並登入成功",
                        isNewUser = true,
                        data = loginResponse
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LINE 登入處理失敗: {Message}", ex.Message);
                return StatusCode(500, new { 
                    success = false, 
                    message = "伺服器內部錯誤，請稍後再試" 
                });
            }
        }

    }
}
