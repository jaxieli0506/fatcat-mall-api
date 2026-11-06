// src/Controllers/AdminAuthController.cs

using api_backend.Models; // 👈 確保有引用 Models
using api_backend.rrDOTs;
using api_backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // 👈 確保有引用 EF Core
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization; 

namespace api_backend.Controllers
{
    [Route("api/admin")]
    [ApiController] 
    public class AdminAuthController : ControllerBase
    {
        private readonly IAdminAuthService _adminAuthService;
        private readonly TeamDb2Context _context; // 👈 加上 DbContext

        public AdminAuthController(IAdminAuthService adminAuthService, TeamDb2Context context) // 👈 修改建構函式
        {
            _adminAuthService = adminAuthService;
            _context = context; // 👈 初始化 DbContext
        }

        // 管理員登入
        // POST api/admin/login
        [HttpPost("login")]
        public async Task<IActionResult> Login(AdminLoginDto loginDto)
        {
            // 調用認證服務
            var token = await _adminAuthService.LoginAsync(loginDto);

            if (token == null)
            {
                return Unauthorized("無效的帳號或密碼。");
            }

            // 登入成功後，從資料庫撈取管理員資料，以便回傳給前端
            var admin = await _context.Admins
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Email == loginDto.Email);
            
            // 在回傳的 JSON 中，同時包含 token 和 admin 的基本資料
            return Ok(new
            {
                token,
                admin = new
                {
                    adminId = admin.AdminId,
                    name = admin.Name,
                    role = admin.Role
                }
            });
        }
    }
}