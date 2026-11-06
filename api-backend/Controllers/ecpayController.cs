using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Web;
using System.Security.Cryptography;
using System.Linq;
using System.Collections.Generic;
using api_backend.Models.DTO;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using api_backend.Models;
using Microsoft.EntityFrameworkCore;

namespace api_backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EcpayController : ControllerBase
    {
        private const string MerchantID = "3002607"; // 測試用
        private const string HashKey = "pwFHCqoQZGmho4w6";
        private const string HashIV = "EkRm7iFT261dpevs";
        private const string ECPayGateway = "https://payment-stage.ecpay.com.tw/Cashier/AioCheckOut/V5";

        [HttpPost]
        public IActionResult CreateECPayPayment([FromBody] ECPayOrderDto dto)
        {
            int totalAmount = dto.PayableAmount;
            if (totalAmount <= 0)
                return BadRequest("付款金額必須大於 0");

            var order = new Dictionary<string, string>
            {
                { "MerchantID", MerchantID },
                { "MerchantTradeNo", $"EC{DateTime.Now:yyyyMMddHHmmss}" },
                { "MerchantTradeDate", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") },
                { "PaymentType", "aio" },
                { "TotalAmount", totalAmount.ToString() },
                { "TradeDesc", "購物平台訂單付款" },
                { "ItemName", "肥貓麵鋪" },
                { "ReturnURL", "https://localhost:7017/api/ecpay/callback" },
                { "ClientBackURL", $"http://localhost:5180/checkoutfinish?orderId={dto.OrderId}" },
                { "ChoosePayment", "Credit" },
                { "EncryptType", "1" },
                { "CustomField1", dto.OrderId.ToString() } // ✅ 傳遞 orderId 到 callback
            };

            string checkMacValue = GenerateCheckMacValue(order);
            order.Add("CheckMacValue", checkMacValue);

            var sb = new StringBuilder();
            sb.AppendLine($"<form id='ecpay-form' method='post' action='{ECPayGateway}'>");
            foreach (var kv in order)
            {
                sb.AppendLine($"<input type='hidden' name='{kv.Key}' value='{HttpUtility.HtmlEncode(kv.Value)}' />");
            }
            sb.AppendLine("</form>");
            sb.AppendLine("<script>document.getElementById('ecpay-form').submit();</script>");

            return Content(sb.ToString(), "text/html");
        }

        [HttpPost("callback")]
        public async Task<IActionResult> ECPayCallback([FromForm] IFormCollection form, [FromServices] TeamDb2Context db)
        {
            try
            {
                string rtnCode = form["RtnCode"];
                if (rtnCode != "1")
                    return Content("0|FAIL");

                string merchantTradeNo = form["MerchantTradeNo"];
                int orderId = int.Parse(form["CustomField1"]);

                // ✅ 查詢是否已存在訂單（或可略過這步直接寫入）
                var existingOrder = await db.Orders.FindAsync(orderId);
                if (existingOrder == null)
                {
                    return Content("0|ORDER_NOT_FOUND");
                }

                // ✅ 示例資料：實務上應該從暫存的購物車資料取得
                var orderDetails = new List<OrderDetail>
                {
                    new OrderDetail { OrderId = orderId, ProductName = "測試商品", Quantity = 1, Unitprice = 500 }
                };

                db.OrderDetails.AddRange(orderDetails);
                await db.SaveChangesAsync();

                return Content("1|OK");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Callback 發生錯誤: " + ex.Message);
                return Content("0|EXCEPTION");
            }
        }

        private string GenerateCheckMacValue(Dictionary<string, string> parameters)
        {
            var sorted = parameters.OrderBy(p => p.Key)
                                   .Select(p => $"{p.Key}={p.Value}")
                                   .ToList();
            var raw = $"HashKey={HashKey}&{string.Join("&", sorted)}&HashIV={HashIV}";
            var encoded = UrlEncodeToECPaySpec(raw).ToLower();
            return ComputeSHA256Hash(encoded).ToUpper();
        }

        private string UrlEncodeToECPaySpec(string input)
        {
            var encoded = HttpUtility.UrlEncode(input).ToLower();
            encoded = encoded.Replace("%2d", "-")
                             .Replace("%5f", "_")
                             .Replace("%2e", ".")
                             .Replace("%21", "!")
                             .Replace("%2a", "*")
                             .Replace("%28", "(")
                             .Replace("%29", ")")
                             .Replace("%20", "+");
            return encoded;
        }

        private string ComputeSHA256Hash(string raw)
        {
            using var sha256 = SHA256.Create();
            byte[] bytes = Encoding.UTF8.GetBytes(raw);
            byte[] hash = sha256.ComputeHash(bytes);
            return BitConverter.ToString(hash).Replace("-", "").ToUpper();
        }
    }
}
