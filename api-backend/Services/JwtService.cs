using Microsoft.EntityFrameworkCore;
using api_backend.Models;
using api_backend.Models.DTO;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace api_backend.Services
{
    public class JwtService
    {
        private readonly IConfiguration _configuration;

        //建構函式透過依賴注入取得 IConfiguration,用來讀取 appsettings.json中的JWT設定
        public JwtService(IConfiguration configuration) 
        {

            _configuration = configuration;
        }

        //接收 User 物件，生成 JWT Token(包含用戶的基本資料UserId,Name,Email)
        public string GenerateToken(User user, bool rememberMe = false) 
        {
            var key = _configuration["Jwt:Key"];
            var issuer = _configuration["Jwt:Issuer"];
            var audience = _configuration["Jwt:Audience"];
            // 根據 rememberMe 參數決定過期時間
            // false: 1小時後自動登出, true: 30天 (720小時)
            var expiryHours = rememberMe ? 720 : 24;

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            // 定義聲明 (Claims)，將用戶資訊編碼到Token中
            // 後續API可以從Token中取得這些資訊
            var claims = new[]
            {
                new Claim("UserId",user.UserId.ToString()),
                new Claim("Name",user.Name),
                new Claim("Email",user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                //"jti" 是 JWT ID 的標準名稱，用於唯一標識這個Token
                new Claim(JwtRegisteredClaimNames.Iat, new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString(),ClaimValueTypes.Integer64)
                //"iat" 是 Issued At 的標準名稱，用於表示這個Token的發行時間
            };

            // 建立 JwtSecurityToken 物件，包含發行者、受眾、聲明、過期時間和簽名憑證
            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(expiryHours),
                signingCredentials: credentials
            );

            // 使用 JwtSecurityTokenHandler 生成Token
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // 回傳Token的過期時間，從配置中讀取過期小時數，並計算當前時間加上這個小時數
        // 用於 LoginResponseDTO 的 ExpiresAt 欄位
        public DateTime GetTokenExpiry(bool rememberMe = false) 
        {
            // 根據 rememberMe 參數決定過期時間
            // false: 1天 (24小時), true: 30天 (720小時)
            var expiryHours = rememberMe ? 720 : 1;
            return DateTime.UtcNow.AddHours(expiryHours);
        }

        //--------------------------------------------------------------------------
        //以下是rr 新增 管理員登入用的
        public string GenerateTokenForAdmin(Admin admin, bool rememberMe = false)
        {
            // 1. 沿用 jj 的過期時間邏輯
            var expiryHours = rememberMe ? 720 : 24; // 記住我:30天, 否則:1小時

            // 2. 建立專屬於 Admin 的 Claims
            var claims = new[]
            {
                new Claim("AdminId", admin.AdminId.ToString()),
                new Claim(ClaimTypes.Name, admin.Name),
                new Claim(ClaimTypes.Email, admin.Email),
                new Claim(ClaimTypes.Role, admin.Role),

                // 3.和 jj 一樣的標準 Claims (Jti, Iat)，確保 Token 結構一致
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            };

            // 4. Token 的其他設定 (金鑰、發行者、受眾) 完全沿用，確保加密和驗證方式一致
            var key = _configuration["Jwt:Key"];
            var issuer = _configuration["Jwt:Issuer"];
            var audience = _configuration["Jwt:Audience"];

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                  issuer: issuer,
                  audience: audience,
                  claims: claims,
                  expires: DateTime.UtcNow.AddHours(expiryHours),
                  signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
