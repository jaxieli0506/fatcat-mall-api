using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using api_backend.Models;

namespace api_backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SortsController : ControllerBase
    {
        private readonly TeamDb2Context _context;

        public SortsController(TeamDb2Context context)
        {
            _context = context;
        }

        // 1. 取得所有小分類
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Sort>>> GetSorts()
        {
            return await _context.Sorts.ToListAsync();
        }

        // 2. 依商品種類取得小分類
        [HttpGet("byCategory/{categoryId}")]
        public async Task<ActionResult<IEnumerable<Sort>>> GetSortsByCategory(int categoryId)
        {
            var sorts = await _context.Sorts
                .Where(s => s.CategoryId == categoryId)
                .ToListAsync();

            return sorts;
        }

        // . 取得單筆小分類
        [HttpGet("{id}")]
        public async Task<ActionResult<Sort>> GetSort(int id)
        {
            var sort = await _context.Sorts.FindAsync(id);

            if (sort == null)
            {
                return NotFound();
            }

            return sort;
        }

        // 4. 新增小分類
        [HttpPost]
        public async Task<ActionResult<Sort>> PostSort(Sort sort)
        {
            _context.Sorts.Add(sort);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetSort), new { id = sort.SortId }, sort);
        }

        // 5. 修改小分類
        [HttpPut("{id}")]
        public async Task<IActionResult> PutSort(int id, Sort sort)
        {
            if (id != sort.SortId)
            {
                return BadRequest();
            }

            _context.Entry(sort).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SortExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // 6. 刪除小分類
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSort(int id)
        {
            var sort = await _context.Sorts.FindAsync(id);
            if (sort == null)
            {
                return NotFound();
            }

            _context.Sorts.Remove(sort);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool SortExists(int id)
        {
            return _context.Sorts.Any(e => e.SortId == id);
        }
    }
}
