using api_backend.Models;
using api_backend.Models.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace api_backend.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class AdminsController : ControllerBase
    {
        private readonly TeamDb2Context _context;

        public AdminsController(TeamDb2Context context)
        {
            _context = context;
        }

        // GET: api/Admins - 取得管理員列表（支援搜尋、排序、分頁）
        [HttpGet]
        public async Task<ActionResult<AdminListResponseDTO>> GetAdmins([FromQuery] AdminQueryDTO query)
        {
            try
            {
                var queryable = _context.Admins.AsQueryable();

                //排除已刪除的管理員
                queryable = queryable.Where(a => a.Status != 2);

                //搜尋功能
                if (!string.IsNullOrEmpty(query.SearchKeyword))
                {
                    var keyword = query.SearchKeyword.Trim().ToLower();
                    queryable = queryable.Where(a => a.Email.Contains(keyword) || a.Name.Contains(keyword) || a.Phone.Contains(keyword));
                }

                //排序功能
                queryable = query.SortBy?.ToLower() switch
                {
                    "name" => query.SortOrder?.ToLower() == "desc" ? 
                        queryable.OrderByDescending(a => a.Name) : 
                        queryable.OrderBy(a => a.Name),
                    "email" => query.SortOrder?.ToLower() == "desc" ?
                        queryable.OrderByDescending(a => a.Email) :
                        queryable.OrderBy(a => a.Email),
                    "phone" => query.SortOrder?.ToLower() == "desc" ?
                        queryable.OrderByDescending(a => a.Phone) :
                        queryable.OrderBy(a => a.Phone),
                    "role" => query.SortOrder?.ToLower() == "desc" ? 
                        queryable.OrderByDescending(a => a.Role) :
                        queryable.OrderBy(a => a.Role),
                    "status" => query.SortOrder?.ToLower() == "desc" ?
                        queryable.OrderByDescending(a => a.Status) :
                        queryable.OrderBy(a => a.Status),
                    _ => query.SortOrder?.ToLower() == "desc" ? 
                        queryable.OrderByDescending(a => a.AdminId) :
                        queryable.OrderBy(a => a.AdminId) //預設排序
                };

                //計算總數
                var totalCount = await queryable.CountAsync();
                var totalPages = (int)Math.Ceiling((double)totalCount / query.PageSize);

                //分頁
                var admins = await queryable
                    .Skip((query.Page - 1) * query.PageSize)
                    .Take(query.PageSize)
                    .Select(a => new AdminSettingsDTO
                    {
                        AdminId = a.AdminId,
                        Email = a.Email,
                        Name = a.Name,
                        Phone = a.Phone,
                        Role = a.Role,
                        Status = a.Status
                    })
                    .ToListAsync();
                return Ok(new AdminListResponseDTO
                {
                    Admins = admins,
                    TotalCount = totalCount,
                    TotalPages = totalPages,
                    CurrentPage = query.Page
                });
            }
            catch (Exception ex) 
            {
                return StatusCode(500, new { message= "取得管理員列表時發生錯誤",error = ex.Message});
            }
        }

        // GET: api/Admins/5 - 取得單一管理員詳細資訊
        [HttpGet("{id}")]
        public async Task<ActionResult<AdminSettingsDTO>> GetAdmin(int id)
        {
            try
            {
                var admin = await _context.Admins
                    .Where(a=>a.AdminId == id && a.Status != 2)
                    .Select(a=> new AdminSettingsDTO 
                    {
                        AdminId = a.AdminId,
                        Email = a.Email,
                        Name = a.Name,
                        Phone = a.Phone,
                        Role = a.Role,
                        Status = a.Status
                    })
                    .FirstOrDefaultAsync();

                if (admin == null || admin.Status == 2) //已刪除的管理員
                {
                    return NotFound(new { message = "管理員不存在或已被刪除" });
                }
                return Ok(admin);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "取得管理員詳細資訊時發生錯誤", error = ex.Message });
            }
        }

        // POST: api/Admins - 新增管理員
        [HttpPost]
        public async Task<ActionResult<AdminSettingsDTO>> CreateAdmin(AdminCreateDTO createDto)
        {
            try
            {
                //檢查Email是否已存在
                var existingAdmin = await _context.Admins
                    .FirstOrDefaultAsync(a => a.Email == createDto.Email && a.Status != 2);//排除已刪除的管理員

                if (existingAdmin != null)
                {
                    return BadRequest(new { message = "此Email已被使用" });
                }

                //密碼加密
                var hashedPassword = HashPassword(createDto.Password);

                //建立新的管理員實體
                var admin = new Admin
                {
                    Email = createDto.Email,
                    Password = hashedPassword,
                    Name = createDto.Name,
                    Phone = createDto.Phone,
                    Role = createDto.Role,
                    Status = 1 //預設狀態為啟用
                };
                _context.Admins.Add(admin);
                await _context.SaveChangesAsync();

                var result = new AdminSettingsDTO
                {
                    AdminId = admin.AdminId,
                    Email = admin.Email,
                    Name = admin.Name,
                    Phone = admin.Phone,
                    Role = admin.Role,
                    Status = admin.Status
                }; 

                return CreatedAtAction(nameof(GetAdmin), new { id = admin.AdminId }, result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "新增管理員時發生錯誤", error = ex.Message });
            }
        }

        // PUT: api/Admins/5 - 更新管理員資訊
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAdmin(int id, AdminUpdateDTO updateDto)
        {
            try
            {
                var admin = await _context.Admins.FindAsync(id);
                if (admin == null || admin.Status == 2) //已刪除的管理員
                {
                    return NotFound(new { message = "管理員不存在或已被刪除" });
                }
                //檢查Email是否已存在（排除當前管理員）
                var existingAdmin = await _context.Admins
                    .Where(a => a.AdminId != id && a.Email == updateDto.Email && a.Status != 2)
                    .FirstOrDefaultAsync();
                if (existingAdmin != null)
                {
                    return BadRequest(new { message = "此Email已被其他管理員使用" });
                }

                //更新管理員資訊
                admin.Email = updateDto.Email;
                admin.Name = updateDto.Name;
                admin.Phone = updateDto.Phone;
                admin.Role = updateDto.Role;
                admin.Status = updateDto.Status;

                await _context.SaveChangesAsync();
                return Ok(new {message = "管理員資料更新成功"});
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "更新管理員資訊時發生錯誤", error = ex.Message });
            }
        }

        // DELETE: api/Admins/{id} - 刪除管理員
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAdmin(int id)
        {
            try
            {
                var admin = await _context.Admins.FindAsync(id);
                if (admin == null || admin.Status == 2) //已刪除的管理員
                {
                    return NotFound(new { message = "管理員不存在或已被刪除" });
                }
                //將狀態設為已刪除
                admin.Status = 2; //2代表已刪除
                await _context.SaveChangesAsync();
                return Ok(new { message = "管理員已成功刪除" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "刪除管理員時發生錯誤", error = ex.Message });
            }
        }

        // 超級管理員查看已刪除的管理員列表
        // GET: api/Admins/deleted
        [HttpGet("deleted")]
        [Authorize(Roles = "超級管理員")]
        public async Task<IActionResult> GetDeletedAdmins([FromQuery] string searchKeyword = "",
            [FromQuery] string sortBy = "AdminId",
            [FromQuery] string sortOrder = "desc",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var query = _context.Admins.Where(a => a.Status == 2); // 狀態 = 2 表示已刪除

                // 搜尋篩選
                if (!string.IsNullOrEmpty(searchKeyword))
                {
                    query = query.Where(a =>
                        a.Name.Contains(searchKeyword) ||
                        a.Email.Contains(searchKeyword) ||
                        a.Phone.Contains(searchKeyword));
                }

                // 排序
                query = sortBy.ToLower() switch
                {
                    "name" => sortOrder.ToLower() == "desc" ? query.OrderByDescending(a => a.Name) : query.OrderBy(a => a.Name),
                    "email" => sortOrder.ToLower() == "desc" ? query.OrderByDescending(a => a.Email) : query.OrderBy(a => a.Email),
                    "role" => sortOrder.ToLower() == "desc" ? query.OrderByDescending(a => a.Role) : query.OrderBy(a => a.Role),
                    _ => sortOrder.ToLower() == "desc" ? query.OrderByDescending(a => a.AdminId) : query.OrderBy(a => a.AdminId)
                };

                // 分頁
                var totalCount = await query.CountAsync();
                var admins = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(a => new
                    {
                        a.AdminId,
                        a.Email,
                        a.Name,
                        a.Phone,
                        a.Role,
                        a.Status
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                        admins,
                        totalCount,
                        page,
                        pageSize,
                        totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // PUT: api/Admins/{id}/restore
        [HttpPut("{id}/restore")]
        [Authorize(Roles = "超級管理員")]
        public async Task<IActionResult> RestoreAdmin(int id)
        {
            try
            {
                var admin = await _context.Admins.FindAsync(id);
                if (admin == null)
                {
                    return NotFound(new { success = false, message = "管理員不存在" });
                }

                if (admin.Status != 2)
                {
                    return BadRequest(new { success = false, message = "該管理員未被刪除" });
                }

                // 復原管理員（設為啟用狀態）
                admin.Status = 1;

                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "管理員復原成功" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }



        //密碼加密方法
        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(password);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }
    }
}
