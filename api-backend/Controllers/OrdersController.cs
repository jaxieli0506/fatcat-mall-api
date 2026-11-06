using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using api_backend.Models;
using Microsoft.AspNetCore.Authorization;

namespace api_backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly TeamDb2Context _context;

        public OrdersController(TeamDb2Context context)
        {
            _context = context;
        }

        // GET: api/Orders
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Order>>> GetOrders()
        {
            return await _context.Orders.ToListAsync();
        }

        // GET: api/Orders/5
        [HttpGet("{id}")]
       
        public async Task<ActionResult<object>> GetOrder(int id)
        {        
            // 查詢訂單
            var order = await _context.Orders
                .Where(o => o.OrderId == id)
                .FirstOrDefaultAsync();

            if (order == null)
            {
                return NotFound("找不到指定的訂單");
            }

            // 查詢用戶資料
            var user = await _context.Users
                .Where(u => u.UserId == order.UserId)
                .FirstOrDefaultAsync();

            if (user == null)
            {
                return BadRequest("找不到用戶資料");
            }

            // 查詢預設收貨地址
            var defaultAddress = await _context.ShippingAddresses
                .Where(sa => sa.UserId == order.UserId && sa.IsDefault && sa.DeletedAt == null)
                .FirstOrDefaultAsync();

            // 組合回傳資料
            var result = new
            {
                // 訂單基本資料
                order.OrderId,
                order.UserId,
                order.CartId,
                order.Orderdate,
                order.Location,
                order.CouponId,
                order.TotalAmount,
                order.PayableAmount,
                order.OrderStatusId,
                order.ShippingId,
                order.ShippingStatusId,
                order.SupportpaymentMethodId,
                
                // 用戶個人資料
                UserName = user.Name,
                UserEmail = user.Email,
                UserPhone = user.Phone,
                
                // 收貨地址資料
                RecipientName = defaultAddress?.RecipientName,
                RecipientPhone = defaultAddress?.PhoneNumber,
                RecipientAddress = defaultAddress != null 
                    ? $"{defaultAddress.City}{defaultAddress.District}{defaultAddress.AddressDetail}" 
                    : null,
                StoreName = defaultAddress?.StoreName
            };

            return Ok(result);
        }

        // PUT: api/Orders/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutOrder(int id, Order order)
        {
            if (id != order.OrderId)
            {
                return BadRequest();
            }

            _context.Entry(order).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!OrderExists(id))
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

        // POST: api/Orders
        [HttpPost]
        [Authorize]  // 加入授權檢查
        public async Task<ActionResult<Order>> PostOrder(Order order)
        {
            // 從 JWT claims 取得當前用戶 ID
            var userIdStr = User.FindFirst("UserId")?.Value;
            if (!int.TryParse(userIdStr, out var currentUserId))
            {
                return Unauthorized("無法識別使用者身分。");
            }

            // 確保訂單的 UserId 是當前登入的用戶
            order.UserId = currentUserId;

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetOrder", new { id = order.OrderId }, order);
        }

        // DELETE: api/Orders/5
        [HttpDelete("{id}")]
        [Authorize]  // 加入授權檢查
        public async Task<IActionResult> DeleteOrder(int id)
        {
            // 從 JWT claims 取得當前用戶 ID
            var userIdStr = User.FindFirst("UserId")?.Value;
            if (!int.TryParse(userIdStr, out var currentUserId))
            {
                return Unauthorized("無法識別使用者身分。");
            }

            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            // 確保用戶只能刪除自己的訂單
            if (order.UserId != currentUserId)
            {
                return Forbid("您沒有權限刪除此訂單");
            }

            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool OrderExists(int id)
        {
            return _context.Orders.Any(e => e.OrderId == id);
        }
    }
}
