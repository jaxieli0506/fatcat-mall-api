using api_backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api_backend.Models.DTO;

namespace api_backend.Controllers
{
    [Route("api/admin/members")]
    [ApiController]
    [Authorize]
    public class AdminMembersController : ControllerBase
    {
        private readonly TeamDb2Context _context;

        public AdminMembersController(TeamDb2Context context)
        {
            _context = context;
        }

        // GET: api/admin/members
        [HttpGet]
        public async Task<ActionResult<IEnumerable<AdminMemberDto>>> GetMembers(
            [FromQuery] string? searchTerm,
            [FromQuery] int? status,
            [FromQuery] int? gender,
            [FromQuery] bool? isThirdParty)
        {
            var query = _context.Users.AsQueryable();

            // 搜尋條件
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(u => u.Name.Contains(searchTerm) || u.Email.Contains(searchTerm) || u.Phone.Contains(searchTerm));
            }

            // 狀態篩選
            if (status.HasValue)
            {
                query = query.Where(u => u.Status == status.Value);
            }

            // 性別篩選
            if (gender.HasValue)
            {
                query = query.Where(u => u.Gender == gender.Value);
            }

            // 第三方登入篩選
            if (isThirdParty.HasValue)
            {
                query = query.Where(u => u.IsThirdparty == isThirdParty.Value);
            }

            var members = await query
                .OrderByDescending(u => u.Registerdate)
                .Select(u => new AdminMemberDto
                {
                    UserId = u.UserId,
                    Name = u.Name,
                    Email = u.Email,
                    Phone = u.Phone,
                    Gender = u.Gender,
                    Birthdate = u.Birthdate,
                    Status = u.Status,
                    Registerdate = u.Registerdate,
                    LastLogin = u.LastLogin,
                    IsThirdparty = u.IsThirdparty
                })
                .ToListAsync();

            return Ok(members);
        }

        // GET: api/admin/members/5
        [HttpGet("{id}")]
        public async Task<ActionResult<AdminMemberDto>> GetMember(int id)
        {
            var member = await _context.Users
                .Where(u => u.UserId == id)
                .Select(u => new AdminMemberDto
                {
                    UserId = u.UserId,
                    Name = u.Name,
                    Email = u.Email,
                    Phone = u.Phone,
                    Gender = u.Gender,
                    Birthdate = u.Birthdate,
                    Status = u.Status,
                    Registerdate = u.Registerdate,
                    LastLogin = u.LastLogin,
                    IsThirdparty = u.IsThirdparty
                })
                .FirstOrDefaultAsync();

            if (member == null)
            {
                return NotFound();
            }

            return Ok(member);
        }

        // POST: api/admin/members
        [HttpPost]
        public async Task<ActionResult<AdminMemberDto>> CreateMember([FromBody] CreateMemberDto createDto)
        {
            // 手動驗證 DTO
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // 檢查 Email 是否已存在
            if (await _context.Users.AnyAsync(u => u.Email == createDto.Email))
            {
                return BadRequest(new { message = "此 Email 已被註冊" });
            }

            // 創建用戶實體
            var user = new User
            {
                Name = createDto.Name,
                Email = createDto.Email,
                Password = "123", // 預設密碼
                Phone = createDto.Phone ?? "",
                PicPath = "", // 預設空字串
                Status = 1, // 正常狀態
                Registerdate = DateTime.UtcNow,
                IsThirdparty = false,
                Gender = createDto.Gender,
                Birthdate = createDto.Birthdate
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var result = new AdminMemberDto
            {
                UserId = user.UserId,
                Name = user.Name,
                Email = user.Email,
                Phone = user.Phone,
                Gender = user.Gender,
                Birthdate = user.Birthdate,
                Status = user.Status,
                Registerdate = user.Registerdate,
                LastLogin = user.LastLogin,
                IsThirdparty = user.IsThirdparty
            };

            return CreatedAtAction(nameof(GetMember), new { id = user.UserId }, result);
        }

        // PUT: api/admin/members/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateMember(int id, [FromBody] UpdateMemberDto updateDto)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // 檢查 Email 是否被其他用戶使用
            if (await _context.Users.AnyAsync(u => u.Email == updateDto.Email && u.UserId != id))
            {
                return BadRequest(new { message = "此 Email 已被其他用戶使用" });
            }

            // 更新用戶資料
            user.Name = updateDto.Name;
            user.Email = updateDto.Email;
            user.Phone = updateDto.Phone ?? "";
            user.Gender = updateDto.Gender;
            user.Birthdate = updateDto.Birthdate;
            user.Status = updateDto.Status;
            // 注意：不更新 Password 和 PicPath，保持原有值

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/admin/members/5 (軟刪除)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMember(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // 軟刪除：將狀態設為 0
            user.Status = 0;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // PUT: api/admin/members/5/status
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateMemberStatus(int id, [FromBody] UpdateStatusDto statusDto)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            user.Status = statusDto.Status;
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
} 