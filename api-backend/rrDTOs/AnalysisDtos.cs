// DTOs/AnalysisDtos.cs 
namespace api_backend.DTOs
{
    //=========================== 儀錶板 ===============================

    // 用於熱銷商品/類別的結構
    public class BestSellerDto
    {
        public string Name { get; set; }
        public int Quantity { get; set; }
    }

    // 儀表板頁面所需的完整數據包
    public class DashboardDto
    {
        // Banner
        public decimal CurrentMonthSalesTotal { get; set; }

        // 四張 KPI 卡片
        public decimal TodaySalesTotal { get; set; }
        public int TodayOrderCount { get; set; }
        public BestSellerDto MonthlyBestSellingProduct { get; set; }
        public BestSellerDto MonthlyBestSellingCategory { get; set; }

        // 兩個圖表
        public List<SalesDataPoint> YearRevenueTrend { get; set; }
        public List<TopSpenderDto> TopSpenders { get; set; }
    }
    //=========================== 分析報表 - 會員 ===============================

    // 通用圖表數據結構
    public class ChartData<T>
    {
        public List<string> Labels { get; set; }
        public List<T> Values { get; set; }
    }

    // 高消費會員排行榜的單筆資料結構
    public class TopSpenderDto
    {
        public string UserName { get; set; }
        public decimal TotalAmount { get; set; }
    }

    // 性別/年齡分佈的單筆資料結構
    public class DistributionDto
    {
        public string Key { get; set; } // e.g., "男性", "20-29歲"
        public int Count { get; set; }
    }

    // 會員分析頁面所需的完整數據包
    // DTOs/AnalysisDtos.cs
    public class MemberAnalysisDto
    {
        // 將 (圖表資料結構) >>　NewMemberCount (一個單純的數字)
        public int NewMemberCount { get; set; }

        public List<DistributionDto> GenderDistribution { get; set; }
        public List<DistributionDto> AgeDistribution { get; set; }
        public ChartData<int> LoyaltyDistribution { get; set; }
        public List<TopSpenderDto> TopSpenders { get; set; }
    }

    //=========================== 分析報表 - 產品 ===============================

    // 通用的排行資料結構
    public class ProductRankingDto
    {
        public string Name { get; set; } // 產品/類別/標籤的名稱
        public int TotalQuantity { get; set; } // 總銷售數量
    }

    // 產品分析頁面所需的完整數據包
    public class ProductAnalysisDto
    {
        public List<ProductRankingDto> CategorySales { get; set; }
        public List<ProductRankingDto> TagSales { get; set; }
        public List<ProductRankingDto> ProductSales { get; set; }
        public List<ProductRankingDto> SortSales { get; set; }
    }

    //=========================== 分析報表 - 銷售 ===============================

    // 通用於圖表的資料點結構
    public class SalesDataPoint
    {
        public string Label { get; set; } // X軸標籤 (例如: '一月', '07-15')
        public decimal Value { get; set; } // Y軸數值 (例如: 銷售額, 訂單數)
    }

    // 用於銷售數據明細表格的結構
    public class SalesDetail
    {
        public string Date { get; set; }
        public decimal Amount { get; set; }
    }

    // 銷售分析頁面所需的完整數據包
    public class SalesAnalysisDto
    {
        // KPI 卡片 #1：固定的本月總額
        public decimal ThisMonthTotalRevenue { get; set; }

        // KPI 卡片 #2：根據日期範圍變化的總額
        public decimal SelectedRangeTotalRevenue { get; set; }

        // 智慧圖表 #3：可能是每日銷售額，也可能是每小時銷售趨勢
        public List<SalesDataPoint> SalesTrend { get; set; }

        // 用來告訴前端，SalesTrend 到底是每日還是每小時
        public string SalesTrendInterval { get; set; } // "Daily" or "Hourly"

        // 圖表 #4：區間訂單量統計
        public List<SalesDataPoint> OrderCountTrend { get; set; }
    }

}