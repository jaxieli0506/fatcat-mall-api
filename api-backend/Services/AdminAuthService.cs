//rr新增來用管理者部分的

// src/Services/AdminAuthService.cs

using api_backend.Models;
using api_backend.rrDOTs;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace api_backend.Services
{
    public class AdminAuthService : IAdminAuthService
    {
        private readonly TeamDb2Context _context;
        private readonly JwtService _jwtService; 

        // 建構子注入 TeamDb2Context 和 JwtService
        public AdminAuthService(TeamDb2Context context, JwtService jwtService)
        {
            _context = context;
            _jwtService = jwtService;
        }

        //登入方法
        // 這個方法會接收 AdminLoginDto，並返回 JWT Token
        // 如果登入失敗，則返回 null
        public async Task<string> LoginAsync(AdminLoginDto loginDto)
        {
            //1.根據Email查詢用戶
            var admin = await _context.Admins
            .FirstOrDefaultAsync(a => a.Email == loginDto.Email);

            // 2.如果沒有找到對應的管理員，，則返回 null
            if (admin == null)
            {  
                return null;
            }

            // 3. 或是密碼不正確(之後要改成雜湊密碼)
            bool isPasswordValid = false;

            try 
            {
                // 嘗試用Bcrypt 驗證密碼
                isPasswordValid = BCrypt.Net.BCrypt.Verify(loginDto.Password, admin.Password);
            }
            catch
            { 
                isPasswordValid = false;
            }

            // 如果雜湊驗證失敗，再比對明文密碼
            if (!isPasswordValid && admin.Password == loginDto.Password) 
            {
                isPasswordValid = true;
                // 如果明文密碼正確，則更新為雜湊密碼
                admin.Password = BCrypt.Net.BCrypt.HashPassword(loginDto.Password);
            }
            

            // 儲存密碼升級或登入時間
            await _context.SaveChangesAsync();

            // 5. 生成 JWT Token
            return _jwtService.GenerateTokenForAdmin(admin, loginDto.RememberMe);
            //   注意：這裡假設 loginDto.RememberMe 是 bool 類型，表示是否記住我
            //    這個值會影響 Token 的過期時間
            //    例如：如果 RememberMe 為 true，則 Token 有效期為 30 天；否則為 1 小時
            //  將 loginDto.RememberMe 傳遞過去

        }
    }
}