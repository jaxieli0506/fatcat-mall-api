using api_backend.Models;
using api_backend.rrDOTs; // 👈 記得 using 你的 DTOs 資料夾
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace api_backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminsLogsController : ControllerBase
    {
        private readonly TeamDb2Context _context;

        public AdminsLogsController(TeamDb2Context context)
        {
            _context = context;
        }

        // GET: api/AdminsLogs
        // 這個方法被強化了，加入了搜尋功能
        [HttpGet]
        public async Task<ActionResult<IEnumerable<LogDto>>> GetLogs(
            [FromQuery] int? adminId,
            [FromQuery] string? action,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate)
        {
            // 1. 初始查詢，並使用 Include 預先載入關聯的 Admin 資料
            var query = _context.Logs.Include(log => log.Admin).AsQueryable();

            // 2. 根據傳入的參數，動態增加篩選條件
            if (adminId.HasValue)
            {
                query = query.Where(log => log.AdminId == adminId.Value);
            }
            if (!string.IsNullOrEmpty(action))
            {
                query = query.Where(log => log.Action.Contains(action));
            }
            if (startDate.HasValue)
            {
                query = query.Where(log => log.Time >= startDate.Value);
            }
            if (endDate.HasValue)
            {
                // 結束日期通常包含當天，所以加一天
                query = query.Where(log => log.Time < endDate.Value.AddDays(1));
            }

            // 3. 執行查詢，並轉換成 DTO
            var logDtos = await query
                .OrderByDescending(log => log.Time) // 讓最新的紀錄在最上面
                .Select(log => new LogDto
                {
                    LogId = log.LogId,
                    AdminName = log.Admin.Name, // 直接從關聯的 Admin 物件取得名稱
                    Action = log.Action,
                    TargetTable = log.TargetTable,
                    TargetId = log.TargetId,
                    Time = log.Time
                })
                .ToListAsync();

            return Ok(logDtos);
        }

        // GET: api/AdminsLogs/5
        [HttpGet("{id}")]
        public async Task<ActionResult<LogDto>> GetLog(int id)
        {
            var log = await _context.Logs
                .Include(l => l.Admin) // 同樣需要 Include
                .FirstOrDefaultAsync(l => l.LogId == id);

            if (log == null)
            {
                return NotFound();
            }

            // 將單一筆 Entity 轉換成 DTO
            var logDto = new LogDto
            {
                LogId = log.LogId,
                AdminName = log.Admin.Name,
                Action = log.Action,
                TargetTable = log.TargetTable,
                TargetId = log.TargetId,
                Time = log.Time
            };

            return Ok(logDto);
        }


        // === 以下為停用的方法 ===

        // PUT: api/AdminsLogs/5
        [HttpPut("{id}")]
        public IActionResult PutLog(int id, Log log)
        {
            /*
            // 由於日誌僅供查看，此功能已被停用。
            if (id != log.LogId)
            {
                return BadRequest();
            }
            _context.Entry(log).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
            */
            return StatusCode(405, "日誌不允許被修改 (This operation is not allowed).");
        }

        // POST: api/AdminsLogs
        [HttpPost]
        public IActionResult PostLog(Log log)
        {
            /*
            // 日誌是由系統在特定操作時自動產生，不應由使用者直接新增。
            _context.Logs.Add(log);
            await _context.SaveChangesAsync();
            return CreatedAtAction("GetLog", new { id = log.LogId }, log);
            */
            return StatusCode(405, "日誌不允許被直接新增 (This operation is not allowed).");
        }

        // DELETE: api/AdminsLogs/5
        [HttpDelete("{id}")]
        public IActionResult DeleteLog(int id)
        {
            /*
            // 為了保持操作紀錄的完整性，日誌不應被刪除。
            var log = await _context.Logs.FindAsync(id);
            if (log == null)
            {
                return NotFound();
            }
            _context.Logs.Remove(log);
            await _context.SaveChangesAsync();
            return NoContent();
            */
            return StatusCode(405, "日誌不允許被刪除 (This operation is not allowed).");
        }

        private bool LogExists(int id)
        {
            return _context.Logs.Any(e => e.LogId == id);
        }
    }
}
