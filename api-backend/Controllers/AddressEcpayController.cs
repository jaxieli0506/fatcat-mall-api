using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace api_backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AddressEcpayController : ControllerBase
    {
        // POST: api/addressecpay / map-callback
        [HttpPost("map-callback")]
        [AllowAnonymous]
        public IActionResult MapCallback([FromForm] MapCallbackRequest request) 
        {
            try 
            {
                // 記錄接收到的資料
                Console.WriteLine($"收到 7-11 地圖回調: {System.Text.Json.JsonSerializer.Serialize(request)}");

                // 回傳成功頁面 包含門市資訊
                var html = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset='utf-8'>
                    <title>門市選擇完成</title>
                    <script>
                        // 將門市資訊傳回父視窗
                        if (window.opener) {{
                            window.opener.postMessage({{
                                type: 'store_selected',
                                storeInfo: {{
                                    storeId: '{request.StoreID}',
                                    storeName: '{request.StoreName}',
                                    storeAddress: '{request.StoreAddress}',
                                    storePhone: '{request.StorePhone ?? ""}'
                                }}
                            }}, '*');
                            window.close();
                        }} else {{
                            // 如果沒有父視窗，顯示門市資訊
                            var infoDiv = document.getElementById('storeInfo');
                            if (infoDiv) {{
                                infoDiv.style.display = 'block';
                            }}
                        }}
                    </script>
                    </head>
                    <body>
                        <div id='storeInfo' style='display: none; padding: 20px; text-align: center;'>
                            <h2>門市選擇完成</h2>
                            <p><strong>門市名稱：</strong>{request.StoreName}</p>
                            <p><strong>門市地址：</strong>{request.StoreAddress}</p>
                            <p><strong>門市電話：</strong>{request.StorePhone ?? "無"}</p>
                            <p>此視窗將自動關閉...</p>
                        </div>
                    </body>
                    </html>";

                return Content(html,"text/html");
            }
            catch(Exception ex) 
            {
                Console.WriteLine($"處理7-11 地圖回調失敗: {ex.Message}");
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        public class MapCallbackRequest 
        {
            public string StoreID { get; set; }
            public string StoreName { get; set; }
            public string StoreAddress { get; set; }
            public string? StorePhone { get; set; }
        }

    }
}
