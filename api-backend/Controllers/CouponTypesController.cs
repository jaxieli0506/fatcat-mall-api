using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using api_backend.Models;

namespace api_backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CouponTypesController : ControllerBase
    {
        private readonly TeamDb2Context _context;

        public CouponTypesController(TeamDb2Context context)
        {
            _context = context;
        }

        // GET: api/CouponTypes
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CouponType>>> GetCouponTypes()
        {
            return await _context.CouponTypes.ToListAsync();
        }

        // GET: api/CouponTypes/5
        [HttpGet("{id}")]
        public async Task<ActionResult<CouponType>> GetCouponType(int id)
        {
            var couponType = await _context.CouponTypes.FindAsync(id);

            if (couponType == null)
            {
                return NotFound();
            }

            return couponType;
        }

        // PUT: api/CouponTypes/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCouponType(int id, CouponType couponType)
        {
            if (id != couponType.CoupontypeId)
            {
                return BadRequest();
            }

            _context.Entry(couponType).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CouponTypeExists(id))
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

        // POST: api/CouponTypes
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<CouponType>> PostCouponType(CouponType couponType)
        {
            _context.CouponTypes.Add(couponType);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetCouponType", new { id = couponType.CoupontypeId }, couponType);
        }

        // DELETE: api/CouponTypes/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCouponType(int id)
        {
            var couponType = await _context.CouponTypes.FindAsync(id);
            if (couponType == null)
            {
                return NotFound();
            }

            _context.CouponTypes.Remove(couponType);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool CouponTypeExists(int id)
        {
            return _context.CouponTypes.Any(e => e.CoupontypeId == id);
        }
    }
}
