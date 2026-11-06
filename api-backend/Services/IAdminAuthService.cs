// src/Services/IAdminAuthService.cs
//rr新增來用管理者部分的

using api_backend.rrDOTs;
using System.Threading.Tasks;

namespace api_backend.Services
{
    public interface IAdminAuthService
    {
        Task<string> LoginAsync(AdminLoginDto loginDto);
    }
}