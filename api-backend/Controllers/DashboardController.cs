using api_backend.DTOs;
using api_backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace api_backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DashboardController : ControllerBase
    {
        private readonly TeamDb2Context _context;

        public DashboardController(TeamDb2Context context)
        {
            _context = context;
        }

        [HttpGet("data")]
        public async Task<ActionResult<DashboardDto>> GetDashboardData()
        {
            var today = DateTime.Today;
            var startOfToday = today;
            var endOfToday = today.AddDays(1);

            var now = DateTime.Now;
            var startOfMonth = new DateTime(now.Year, now.Month, 1);
            var startOfNextMonth = startOfMonth.AddMonths(1);

            // 1. 本月目前銷售總額 (Banner)
            var currentMonthSalesTotal = await _context.Orders
                .Where(o => o.Orderdate >= startOfMonth && o.Orderdate < startOfNextMonth)
                .SumAsync(o => o.PayableAmount);

            // 2. 今日銷售總額 (KPI Card 1)
            var todaySalesTotal = await _context.Orders
                .Where(o => o.Orderdate >= startOfToday && o.Orderdate < endOfToday)
                .SumAsync(o => o.PayableAmount);

            // 3. 今日訂單總量 (KPI Card 2)
            var todayOrderCount = await _context.Orders
                .CountAsync(o => o.Orderdate >= startOfToday && o.Orderdate < endOfToday);

            // 獲取本月所有訂單明細
            var monthlyOrderDetails = _context.OrderDetails
                .Where(od => od.Order.Orderdate >= startOfMonth && od.Order.Orderdate < startOfNextMonth);

            // 4. 本月最熱銷商品 (KPI Card 3)
            var monthlyBestProduct = await monthlyOrderDetails
                .GroupBy(od => od.ProductName)
                .Select(g => new BestSellerDto { Name = g.Key, Quantity = g.Sum(od => od.Quantity ?? 0) })
                .OrderByDescending(p => p.Quantity)
                .FirstOrDefaultAsync();

            // 5. 本月最熱銷類別 (KPI Card 4)
            var monthlyBestCategory = await monthlyOrderDetails
                .Include(od => od.Item.Category)
                .Where(od => od.Item.Category != null)
                .GroupBy(od => od.Item.Category.Name)
                .Select(g => new BestSellerDto { Name = g.Key, Quantity = g.Sum(od => od.Quantity ?? 0) })
                .OrderByDescending(c => c.Quantity)
                .FirstOrDefaultAsync();

            // --- 圖表數據 ---
            var currentYear = DateTime.Now.Year;
            var yearOrders = _context.Orders.Where(o => o.Orderdate.Year == currentYear);

            // 6. 今年營業額趨勢圖 (Chart 1)
            var revenueByMonth = await yearOrders
                .GroupBy(o => o.Orderdate.Month)
                .ToDictionaryAsync(g => g.Key, g => g.Sum(o => o.PayableAmount));
            var yearRevenueTrend = Enumerable.Range(1, 12).Select(month => new SalesDataPoint
            {
                Label = $"{month}月",
                Value = revenueByMonth.ContainsKey(month) ? revenueByMonth[month] : 0
            }).ToList();

            // 7. 高消費會員排行榜 Top 5 (Chart 2)
            var topSpenders = await _context.Orders
                .Where(o => o.Orderdate.Year == currentYear) // 我們這裡預設儀表板看的是今年的 Top 5
                .GroupBy(o => o.UserId)
                .Select(g => new { UserId = g.Key, TotalSpending = g.Sum(o => o.PayableAmount) })
                .OrderByDescending(s => s.TotalSpending)
                .Take(5)
                .Join(_context.Users,
                      spender => spender.UserId,
                      user => user.UserId,
                      (spender, user) => new TopSpenderDto { UserName = user.Name, TotalAmount = spender.TotalSpending })
                .ToListAsync();

            // 組合最終 DTO
            var dashboardData = new DashboardDto
            {
                CurrentMonthSalesTotal = currentMonthSalesTotal,
                TodaySalesTotal = todaySalesTotal,
                TodayOrderCount = todayOrderCount,
                MonthlyBestSellingProduct = monthlyBestProduct ?? new BestSellerDto { Name = "無資料" },
                MonthlyBestSellingCategory = monthlyBestCategory ?? new BestSellerDto { Name = "無資料" },
                YearRevenueTrend = yearRevenueTrend,
                TopSpenders = topSpenders
            };

            return Ok(dashboardData);
        }
    }
}