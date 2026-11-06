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
    public class ShippingStatusController : ControllerBase
    {
        private readonly TeamDb2Context _context;

        public ShippingStatusController(TeamDb2Context context)
        {
            _context = context;
        }

        // GET: api/ShippingStatus
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ShippingStatus>>> GetShippingStatuses()
        {
            return await _context.ShippingStatuses.ToListAsync();
        }

        // GET: api/ShippingStatus/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ShippingStatus>> GetShippingStatus(int id)
        {
            var shippingStatus = await _context.ShippingStatuses.FindAsync(id);

            if (shippingStatus == null)
            {
                return NotFound();
            }

            return shippingStatus;
        }

        // PUT: api/ShippingStatus/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutShippingStatus(int id, ShippingStatus shippingStatus)
        {
            if (id != shippingStatus.ShippingStatusId)
            {
                return BadRequest();
            }

            _context.Entry(shippingStatus).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ShippingStatusExists(id))
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

        // POST: api/ShippingStatus
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<ShippingStatus>> PostShippingStatus(ShippingStatus shippingStatus)
        {
            _context.ShippingStatuses.Add(shippingStatus);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetShippingStatus", new { id = shippingStatus.ShippingStatusId }, shippingStatus);
        }

        // DELETE: api/ShippingStatus/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteShippingStatus(int id)
        {
            var shippingStatus = await _context.ShippingStatuses.FindAsync(id);
            if (shippingStatus == null)
            {
                return NotFound();
            }

            _context.ShippingStatuses.Remove(shippingStatus);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ShippingStatusExists(int id)
        {
            return _context.ShippingStatuses.Any(e => e.ShippingStatusId == id);
        }
    }
}
