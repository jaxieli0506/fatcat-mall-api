using Microsoft.EntityFrameworkCore;
using api_backend.Models;
using api_backend.Models.DTO;

namespace api_backend.Services
{
    //  實作 LoginAsync 方法 - 驗證用戶帳號密碼
    //  實作 RegisterAsync 方法 - 註冊新用戶
    //  使用 BCrypt 驗證和加密密碼
    public class AuthService : IAuthService
    {
        private readonly TeamDb2Context _context;
        private readonly JwtService _jwtService;
        private readonly IEmailService _emailService;

        // 暫存重設令牌的字典 (生產環境應該使用資料庫或Redis)
        private static readonly Dictionary<string, (string Email, DateTime Expiry)> _resetTokens = new();

        //建構函式：依賴注入資料庫上下文、JWT服務和郵件服務
        //用於存取資料庫、生成Token和發送郵件
        public AuthService(TeamDb2Context context, JwtService jwtService, IEmailService emailService)
        {
            _context = context;
            _jwtService = jwtService;
            _emailService = emailService;
        }


        //登入方法：驗證用戶帳號密碼,更新最後登入時間,生成JWT Token,回傳登入資料
        public async Task<LoginResponseDTO?> LoginAsync(LoginRequestDTO request)
        {
            try
            {
                //1.根據Email查詢用戶
                var user = await _context.Users.FirstOrDefaultAsync(u=>u.Email == request.Email);
                // 2. 檢查用戶是否存在
                if (user==null)
                {
                    return null; //用戶不存在
                }

                // 3. 驗證密碼功能區------------------------------------------------
                bool isPasswordValid = false;

                //因為資料庫已生成明文密碼的假資料，所以需要做此修改，將既存的明文密碼轉成雜湊密碼
                // 先嘗試 BCrypt 驗證（已雜湊的密碼）
                try
                {
                    isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.Password);
                }
                catch
                {
                    // BCrypt 驗證失敗，可能是明文密碼
                    isPasswordValid = false;
                }

                // 如果 BCrypt 驗證失敗，檢查是否為明文密碼
                if (!isPasswordValid && user.Password == request.Password)
                {
                    isPasswordValid = true;

                    // 將明文密碼轉換為雜湊密碼
                    user.Password = BCrypt.Net.BCrypt.HashPassword(request.Password);
                    // 注意：這裡不要立即儲存，等到第5步一起儲存
                }

                if (!isPasswordValid)
                {
                    return null; // 密碼錯誤
                }

               // 驗證密碼功能區end------------------------------------
                // 4. 檢查用戶狀態（Status=1表示正常）
                if (user.Status != 1) 
                {
                    return null; //帳號被停用
                }

                // 5. 更新最後登入時間
                user.LastLogin = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // 6. 生成JWT Token(根據RememberMe參數調整過期時間)
                var token = _jwtService.GenerateToken(user, request.RememberMe);
                var tokenExpiry = _jwtService.GetTokenExpiry(request.RememberMe);

                // 7. 回傳登入成功資料
                return new LoginResponseDTO
                {
                    Token = token,
                    UserId = user.UserId,
                    Name = user.Name,
                    Email = user.Email,
                    ExpiresAt = tokenExpiry,
                    PicPath = user.PicPath
                };
            }
            catch (Exception ex)
            {
                // 記錄錯誤（實際專案中可以使用ILogger）
                // 這裡簡化處理
                return null;
            }
        }

        //註冊方法：檢查Email是否已存在,加密密碼,建立新用戶記錄,儲存到資料庫
        public async Task<bool> RegisterAsync(RegisterRequestDTO request) 
        {
            try
            {
                // 1. 檢查Email是否已存在
                var existingUser = await _context.Users.FirstOrDefaultAsync(u=>u.Email == request.Email);
                if (existingUser != null) 
                {
                    return false; //Email已存在
                }

                // 2. 加密密碼
                var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);

                // 3. 建立新用戶
                var newUser = new User
                {
                    Name = request.Name,
                    Email = request.Email,
                    Password = hashedPassword,
                    Phone = request.Phone,
                    Gender = request.Gender,
                    Birthdate = request.Birthdate,
                    Status = 1, //預設為正常狀態
                    Registerdate = DateTime.UtcNow,
                    IsThirdparty = false,
                    LastLogin = null, //註冊時還沒有登入
                    PicPath = null //預設無頭像
                };

                // 4. 儲存到資料庫
                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();
                return true; //註冊成功
            }
            catch (Exception ex) 
            {
                // 記錄錯誤（實際專案中可以使用ILogger）
                return false; //註冊失敗
            
            }
        
        }

        // 更新用戶資料
        public async Task<bool> UpdateUserAsync(int userId, UpdateUserRequestDTO request)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return false;
                }

                // 更新允許修改的欄位
                if (!string.IsNullOrEmpty(request.Name))
                {
                    user.Name = request.Name;
                }

                if (!string.IsNullOrEmpty(request.Phone))
                {
                    user.Phone = request.Phone;
                }

                if (request.Gender.HasValue)
                {
                    user.Gender = request.Gender.Value;
                }

                if (request.Birthdate.HasValue)
                {
                    user.Birthdate = request.Birthdate.Value;
                }

                if (!string.IsNullOrEmpty(request.PicPath))
                {
                    user.PicPath = request.PicPath;
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"更新用戶資料錯誤，來自後端authService.cs: {ex.Message}");
                return false;
            }
        }

        // 取得完整用戶資料
        public async Task<User?> GetUserByIdAsync(int userId)
        {
            return await _context.Users.FindAsync(userId);
        }

        //密碼變更
        // 密碼變更方法
        public async Task<bool> ChangePasswordAsync(int userId, ChangePasswordRequestDTO request)
        {
            try
            {
                // 1. 查找用戶
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return false; // 用戶不存在
                }

                // 2. 驗證目前密碼
                bool isCurrentPasswordValid = BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.Password);
                if (!isCurrentPasswordValid)
                {
                    return false; // 目前密碼錯誤
                }

                // 3. 確認新密碼不與目前密碼相同
                bool isSamePassword = BCrypt.Net.BCrypt.Verify(request.NewPassword, user.Password);
                if (isSamePassword)
                {
                    return false; // 新密碼不能與目前密碼相同
                }

                // 4. 加密新密碼
                var hashedNewPassword = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);

                // 5. 更新密碼
                user.Password = hashedNewPassword;

                // 6. 可選：記錄密碼變更時間（如果User模型有這個欄位）
                // user.PasswordChangedAt = DateTime.UtcNow;

                // 7. 儲存變更
                await _context.SaveChangesAsync();

                return true; // 密碼變更成功
            }
            catch (Exception ex)
            {
                // 記錄錯誤
                Console.WriteLine($"密碼變更錯誤: {ex.Message}");
                return false; // 密碼變更失敗
            }
        }

        // 忘記密碼 - 發送重設密碼郵件
        public async Task<bool> ForgotPasswordAsync(ForgotPasswordRequestDTO request)
        {
            try
            {
                // 1. 檢查用戶是否存在
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
                if (user == null)
                {
                    // 基於安全考量，即使用戶不存在也返回 true
                    // 避免洩露用戶是否存在的資訊
                    return true;
                }

                // 2. 檢查用戶狀態
                if (user.Status != 1)
                {
                    Console.WriteLine("此帳號被停用");
                    return false; // 帳號被停用
                }

                // 3. 生成重設令牌 (6位數字 + 時間戳記)
                var token = GenerateResetToken();
                var expiry = DateTime.UtcNow.AddHours(1); // 1小時後過期

                // 4. 暫存令牌（生產環境應存在資料庫）
                _resetTokens[token] = (request.Email, expiry);

                // 5. 清理過期的令牌
                CleanupExpiredTokens();

                // 6. 生成重設密碼的完整URL
                var resetUrl = $"http://localhost:5180/reset-password?token={token}&email={Uri.EscapeDataString(request.Email)}";

                // 7. 發送重設密碼郵件
                var emailSent = await _emailService.SendPasswordResetEmailAsync(request.Email, token, resetUrl);

                if (emailSent)
                {
                    Console.WriteLine($"重設密碼郵件已發送給: {request.Email}");
                    Console.WriteLine($"重設連結: {resetUrl}");
                    return true;
                }
                else
                {
                    Console.WriteLine($"發送重設密碼郵件失敗a: {request.Email}");
                    // 如果郵件發送失敗，移除令牌
                    _resetTokens.Remove(token);
                    return false;
                }
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"忘記密碼錯誤: {ex.Message}");
                return false;
            }
        }

        // 重設密碼 - 驗證令牌並重設密碼
        public async Task<bool> ResetPasswordAsync(ResetPasswordRequestDTO request)
        {
            try
            {
                Console.WriteLine($"開始處理重設密碼請求: Email={request.Email}, Token={request.Token}");
                Console.WriteLine($"當前儲存的令牌數量: {_resetTokens.Count}");
                
                // 1. 檢查令牌是否存在且未過期
                if (!_resetTokens.TryGetValue(request.Token, out var tokenData))
                {
                    Console.WriteLine($"令牌不存在: {request.Token}");
                    return false; // 令牌不存在
                }

                Console.WriteLine($"找到令牌: Email={tokenData.Email}, 過期時間={tokenData.Expiry}");

                if (DateTime.UtcNow > tokenData.Expiry)
                {
                    // 令牌已過期，移除它
                    Console.WriteLine($"令牌已過期: {request.Token}");
                    _resetTokens.Remove(request.Token);
                    return false;
                }

                // 2. 檢查 Email 是否匹配
                if (tokenData.Email != request.Email)
                {
                    Console.WriteLine($"Email 不匹配: 期望={tokenData.Email}, 實際={request.Email}");
                    return false; // Email 不匹配
                }

                // 3. 查找用戶
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
                if (user == null)
                {
                    return false; // 用戶不存在
                }

                // 4. 檢查用戶狀態
                if (user.Status != 1)
                {
                    return false; // 帳號被停用
                }

                // 5. 加密新密碼
                var hashedNewPassword = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);

                // 6. 更新密碼
                user.Password = hashedNewPassword;

                // 7. 儲存變更
                await _context.SaveChangesAsync();

                // 8. 移除已使用的令牌
                _resetTokens.Remove(request.Token);

                Console.WriteLine($"用戶 {request.Email} 密碼重設成功");

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"重設密碼錯誤: {ex.Message}");
                return false;
            }
        }

        // 生成重設令牌
        private static string GenerateResetToken()
        {
            var random = new Random();
            var token = random.Next(100000, 999999).ToString(); // 6位數字
            var timestamp = DateTime.UtcNow.Ticks.ToString()[^6..]; // 時間戳記的後6位
            return token + timestamp; // 12位令牌
        }

        // 清理過期的令牌
        private static void CleanupExpiredTokens()
        {
            var now = DateTime.UtcNow;
            var expiredTokens = _resetTokens
                .Where(kvp => now > kvp.Value.Expiry)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var token in expiredTokens)
            {
                _resetTokens.Remove(token);
            }
        }


    }
}
