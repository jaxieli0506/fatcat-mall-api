using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting; // 需要這個來取得 wwwroot 的路徑
using System;
using System.IO;
using System.Threading.Tasks;

namespace api_backend.Controllers
{
    [Route("api/chat")]
    [ApiController]
    [Authorize] // 確保只有登入的使用者/管理員能上傳
    public class ChatController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;

        // 使用依賴注入來獲取 IWebHostEnvironment，這是取得 wwwroot 路徑的最佳方式
        public ChatController(IWebHostEnvironment env)
        {
            _env = env;
        }

        [HttpPost("upload")]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("沒有上傳任何檔案。");
            }

            // 簡單的檔案類型檢查，增加安全性
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(extension) || !allowedExtensions.Contains(extension))
            {
                return BadRequest("只允許上傳圖片檔案 (jpg, jpeg, png, gif)。");
            }

            // 組合儲存路徑 (wwwroot/uploads/chat)
            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "chat");
            Directory.CreateDirectory(uploadsFolder); // 如果資料夾不存在，就建立它

            // 產生一個唯一的檔名，避免檔案覆蓋
            var uniqueFileName = Guid.NewGuid().ToString() + extension;
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            // 將檔案儲存到伺服器
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            // 產生一個前端可以直接使用的公開 URL
            var publicUrl = $"/uploads/chat/{uniqueFileName}";

            // 回傳這個 URL 給前端
            return Ok(new { url = publicUrl });
        }
    }
}