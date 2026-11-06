using Microsoft.EntityFrameworkCore;
using api_backend.Models;
using api_backend.Models.DTO;


namespace api_backend.Services
{
    public interface IAuthService
    {
        //定義 LoginAsync 方法 - 接受LoginRequestDTO（包含Email和Password），回傳LoginResponseDTO（可能為null，表示登入失敗）
        Task<LoginResponseDTO?> LoginAsync(LoginRequestDTO request);

        //定義 RegisterAsync 方法 - 接受RegisterRequestDTO（包含註冊資訊），回傳bool（true表示註冊成功，false表示失敗）
        Task<bool> RegisterAsync(RegisterRequestDTO request);

        // 更新用戶資料
        Task<bool> UpdateUserAsync(int userId, UpdateUserRequestDTO request);

        // 取得完整用戶資料
        Task<User?> GetUserByIdAsync(int userId);

        //變更密碼
        Task<bool> ChangePasswordAsync(int userId,ChangePasswordRequestDTO request);

        //忘記密碼 - 發送重設密碼郵件
        Task<bool> ForgotPasswordAsync(ForgotPasswordRequestDTO request);

        //重設密碼 - 驗證令牌並重設密碼
        Task<bool> ResetPasswordAsync(ResetPasswordRequestDTO request);
    }

}

