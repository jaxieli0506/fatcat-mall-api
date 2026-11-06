// Controllers/AnalysisController.cs
using api_backend.Models;
using api_backend.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace api_backend.Controllers
{
    // =================== 分析報表 - 會員 ===================
    [Route("api/[controller]")]
    [ApiController]
    public class AnalysisController : ControllerBase
    {
        private readonly TeamDb2Context _context; // 請替換成你的 DbContext 名稱

        public AnalysisController(TeamDb2Context context)
        {
            _context = context;
        }

        [HttpGet("member")]
        public async Task<ActionResult<MemberAnalysisDto>> GetMemberAnalysis(
        [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            var adjustedEndDate = endDate.Date.AddDays(1).AddTicks(-1);

            // 【新】1. 區間新註冊會員數 (從趨勢圖變成一個數字)
            var newMemberCount = await _context.Users
                .CountAsync(u => u.Registerdate >= startDate && u.Registerdate <= adjustedEndDate);

            // 2. 獲取在該區間內有「消費」的會員 UserIds
            var activeUserIds = await _context.Orders
                .Where(o => o.Orderdate >= startDate && o.Orderdate <= adjustedEndDate)
                .Select(o => o.UserId)
                .Distinct()
                .ToListAsync();

            // 【新】3. 會員性別分布 (只分析在該區間有消費的會員)
            var genderDistribution = await _context.Users
                .Where(u => activeUserIds.Contains(u.UserId))
                .GroupBy(u => u.Gender)
                .Select(g => new DistributionDto { Key = g.Key == 1 ? "男性" : (g.Key == 2 ? "女性" : "未提供"), Count = g.Count() })
                .ToListAsync();

            // 【新】4. 會員年齡分布 (也只分析在該區間有消費的會員)
            var now = DateTime.Today;
            var activeUsersWithBirthdate = await _context.Users
                .Where(u => activeUserIds.Contains(u.UserId) && u.Birthdate.HasValue)
                .ToListAsync();

            var ageGroups = new Dictionary<string, int> { { "20歲以下", 0 }, { "20-29歲", 0 }, { "30-39歲", 0 }, { "40-49歲", 0 }, { "50歲以上", 0 } };
            foreach (var user in activeUsersWithBirthdate)
            {
                var age = now.Year - user.Birthdate.Value.Year;
                if (user.Birthdate.Value.Date > now.AddYears(-age)) age--;

                if (age < 20) ageGroups["20歲以下"]++;
                else if (age <= 29) ageGroups["20-29歲"]++;
                else if (age <= 39) ageGroups["30-39歲"]++;
                else if (age <= 49) ageGroups["40-49歲"]++;
                else ageGroups["50歲以上"]++;
            }
            var ageDistribution = ageGroups.Select(g => new DistributionDto { Key = g.Key, Count = g.Value }).ToList();

            // 5. 會員忠誠度 & 高消費會員 (邏輯不變，但改用 PayableAmount)
            var userOrderStats = await _context.Orders
                .Where(o => o.Orderdate >= startDate && o.Orderdate <= adjustedEndDate)
                .GroupBy(o => o.UserId)
                .Select(g => new {
                    UserId = g.Key,
                    OrderCount = g.Count(),
                    TotalSpending = g.Sum(o => o.PayableAmount) // 【推薦】改用實收金額
                }).ToListAsync();

            var loyaltyCounts = new Dictionary<string, int> { { "1 次", 0 }, { "2-4 次", 0 }, { "5-10 次", 0 }, { "10+ 次", 0 } };
            foreach (var stat in userOrderStats)
            {
                if (stat.OrderCount == 1) loyaltyCounts["1 次"]++;
                else if (stat.OrderCount <= 4) loyaltyCounts["2-4 次"]++;
                else if (stat.OrderCount <= 10) loyaltyCounts["5-10 次"]++;
                else loyaltyCounts["10+ 次"]++;
            }
            var loyaltyChart = new ChartData<int> { Labels = loyaltyCounts.Keys.ToList(), Values = loyaltyCounts.Values.ToList() };

            var topSpenderStats = userOrderStats.OrderByDescending(s => s.TotalSpending).Take(10).ToList();
            var userIds = topSpenderStats.Select(s => s.UserId).ToList();
            var userNames = await _context.Users.Where(u => userIds.Contains(u.UserId)).ToDictionaryAsync(u => u.UserId, u => u.Name);
            var topSpenders = topSpenderStats.Select(s => new TopSpenderDto { UserName = userNames.ContainsKey(s.UserId) ? userNames[s.UserId] : "未知用戶", TotalAmount = s.TotalSpending }).ToList();

            // 【新】組合最終回傳的 DTO
            var result = new MemberAnalysisDto
            {
                NewMemberCount = newMemberCount, // 改成單一數字
                GenderDistribution = genderDistribution,
                AgeDistribution = ageDistribution,
                LoyaltyDistribution = loyaltyChart,
                TopSpenders = topSpenders
            };

            return Ok(result);
        }

        // =================== 分析報表 - 產品 ===================

        [HttpGet("product")]
        public async Task<ActionResult<ProductAnalysisDto>> GetProductAnalysis(
        [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            var adjustedEndDate = endDate.Date.AddDays(1).AddTicks(-1);

            var query = _context.OrderDetails
                .Where(od => od.Order.Orderdate >= startDate && od.Order.Orderdate <= adjustedEndDate);

            // 產品銷售排行
            var productSales = await query
                .GroupBy(od => od.ProductName)
                .Select(g => new ProductRankingDto { Name = g.Key, TotalQuantity = g.Sum(od => od.Quantity ?? 0) })
                .OrderByDescending(p => p.TotalQuantity)
                .Take(10)
                .ToListAsync();

            // 類別銷售排行
            var categorySales = await query
                .Include(od => od.Item.Category)
                // ▼▼▼ 新增過濾條件 ▼▼▼
                .Where(od => od.Item.Category != null && od.Item.Category.Name != null)
                .GroupBy(od => od.Item.Category.Name)
                .Select(g => new ProductRankingDto { Name = g.Key, TotalQuantity = g.Sum(od => od.Quantity ?? 0) })
                .OrderByDescending(c => c.TotalQuantity)
                .Take(10)
                .ToListAsync();

            // 標籤銷售排行
            var tagSales = await query
                .Include(od => od.Item.ProductTags)
                    .ThenInclude(pt => pt.Tag)
                .SelectMany(od => od.Item.ProductTags.Select(pt => new {
                    Quantity = od.Quantity ?? 0,
                    TagName = pt.Tag.Name
                }))
                // ▼▼▼ 新增過濾條件 ▼▼▼
                .Where(t => t.TagName != null)
                .GroupBy(t => t.TagName)
                .Select(g => new ProductRankingDto { Name = g.Key, TotalQuantity = g.Sum(t => t.Quantity) })
                .OrderByDescending(t => t.TotalQuantity)
                .Take(10)
                .ToListAsync();

            // 商品分類(Sort)銷售排行
            var sortSales = await query
                .Include(od => od.Item.Sort)
                // ▼▼▼ 新增過濾條件 ▼▼▼
                .Where(od => od.Item.Sort != null && od.Item.Sort.Name != null)
                .GroupBy(od => od.Item.Sort.Name)
                .Select(g => new ProductRankingDto { Name = g.Key, TotalQuantity = g.Sum(od => od.Quantity ?? 0) })
                .OrderByDescending(s => s.TotalQuantity)
                .Take(10)
                .ToListAsync();

            var result = new ProductAnalysisDto
            {
                ProductSales = productSales,
                CategorySales = categorySales,
                TagSales = tagSales,
                SortSales = sortSales
            };

            return Ok(result);
        }

        // =================== 分析報表 - 銷售 ===================
        [HttpGet("sales")]
        public async Task<ActionResult<SalesAnalysisDto>> GetSalesAnalysis(
            [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            // --- 1. 計算固定的「本月銷售總額」 ---
            var now = DateTime.Now;
            var startOfMonth = new DateTime(now.Year, now.Month, 1);
            var thisMonthTotalRevenue = await _context.Orders
                .Where(o => o.Orderdate.Date >= startOfMonth.Date && o.Orderdate.Date <= now.Date)
                .SumAsync(o => o.PayableAmount);

            // --- 2. 判斷日期範圍，決定要執行哪種查詢邏輯 ---
            List<SalesDataPoint> salesTrend;
            string salesTrendInterval;
            List<SalesDataPoint> orderCountTrend;
            decimal selectedRangeTotalRevenue;

            // 計算月份差異
            int monthDifference = (endDate.Year - startDate.Year) * 12 + endDate.Month - startDate.Month;

            if (monthDifference > 2) // 如果選擇範圍大於2個月
            {
                salesTrendInterval = "Monthly"; // 告訴前端這是「每月」數據

                // a. 查詢固定「今年度」的訂單
                var currentYear = DateTime.Now.Year;
                var yearOrders = await _context.Orders
                    .Where(o => o.Orderdate.Year == currentYear)
                    .ToListAsync();

                selectedRangeTotalRevenue = yearOrders.Sum(o => o.PayableAmount); // 總額也變成今年的總額

                // b. 計算今年1-12月的銷售額
                var revenueByMonth = yearOrders.GroupBy(o => o.Orderdate.Month)
                    .ToDictionary(g => g.Key, g => g.Sum(o => o.PayableAmount));
                salesTrend = Enumerable.Range(1, 12).Select(month => new SalesDataPoint
                {
                    Label = $"{month}月",
                    Value = revenueByMonth.ContainsKey(month) ? revenueByMonth[month] : 0
                }).ToList();

                // c. 計算今年1-12月的訂單量
                var orderCountByMonth = yearOrders.GroupBy(o => o.Orderdate.Month)
                    .ToDictionary(g => g.Key, g => (decimal)g.Count());
                orderCountTrend = Enumerable.Range(1, 12).Select(month => new SalesDataPoint
                {
                    Label = $"{month}月",
                    Value = orderCountByMonth.ContainsKey(month) ? orderCountByMonth[month] : 0
                }).ToList();
            }
            else // 如果選擇範圍小於等於2個月
            {
                var adjustedEndDate = endDate.Date.AddDays(1).AddTicks(-1);
                var filteredOrders = await _context.Orders
                    .Where(o => o.Orderdate >= startDate && o.Orderdate <= adjustedEndDate)
                    .OrderBy(o => o.Orderdate)
                    .ToListAsync();

                selectedRangeTotalRevenue = filteredOrders.Sum(o => o.PayableAmount);

                // d. 智慧圖表：單日查每小時，多日查每日 (邏輯不變)
                bool isSingleDay = startDate.Date == endDate.Date;
                if (isSingleDay)
                {
                    salesTrendInterval = "Hourly";
                    var salesByHour = filteredOrders.GroupBy(o => o.Orderdate.Hour)
                        .ToDictionary(g => g.Key, g => g.Sum(o => o.PayableAmount));
                    salesTrend = Enumerable.Range(0, 24).Select(hour => new SalesDataPoint
                    {
                        Label = $"{hour:00}:00",
                        Value = salesByHour.ContainsKey(hour) ? salesByHour[hour] : 0
                    }).ToList();
                }
                else
                {
                    salesTrendInterval = "Daily";
                    salesTrend = filteredOrders.GroupBy(o => o.Orderdate.Date)
                        .Select(g => new SalesDataPoint { Label = g.Key.ToString("MM/dd"), Value = g.Sum(o => o.PayableAmount) })
                        .ToList();
                }

                // e. 訂單量也根據「區間」計算
                orderCountTrend = filteredOrders.GroupBy(o => o.Orderdate.Date)
                    .Select(g => new SalesDataPoint { Label = g.Key.ToString("MM/dd"), Value = g.Count() })
                    .ToList();
            }

            // --- 組合最終回傳的 DTO ---
            var result = new SalesAnalysisDto
            {
                ThisMonthTotalRevenue = thisMonthTotalRevenue,
                SelectedRangeTotalRevenue = selectedRangeTotalRevenue,
                SalesTrend = salesTrend,
                SalesTrendInterval = salesTrendInterval,
                OrderCountTrend = orderCountTrend
            };

            return Ok(result);
        }
    }
}