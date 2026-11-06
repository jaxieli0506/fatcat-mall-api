using api_backend.Models;
using api_backend.rrDOTs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace api_backend.Hubs
{
    [Authorize] // 為整個 Hub 加上驗證~
    public class ChatHub : Hub
    {
        private static readonly ConcurrentDictionary<string, string> UserConnections = new();
        private static readonly ConcurrentDictionary<string, string> AdminConnections = new();
        private readonly ILogger<ChatHub> _logger;
        private readonly TeamDb2Context _context;

        //確認建構函式有注入 DbContext
        public ChatHub(ILogger<ChatHub> logger, TeamDb2Context context)
        {
            _logger = logger;
            _context = context;
        }

        private string GetCurrentPrincipalId()
        {
            var id = Context.User.FindFirst("AdminId")?.Value;
            if (string.IsNullOrEmpty(id))
            {
                id = Context.User.FindFirst("UserId")?.Value;
            }
            return id;
        }

        public async Task JoinAsUser()
        {
            var userId = GetCurrentPrincipalId();

            await Groups.AddToGroupAsync(Context.ConnectionId, "LiveStreamViewers");
            UserConnections[Context.ConnectionId] = userId;
            await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");

            _logger.LogInformation("已登入使用者 {UserId} 已加入直播間", userId);
            await Clients.Group("Admins").SendAsync("UserOnline", userId);
        }

        public async Task JoinAsAdmin()
        {
            var adminId = GetCurrentPrincipalId();
            if (string.IsNullOrEmpty(adminId)) return;

            AdminConnections[Context.ConnectionId] = adminId;
            await Groups.AddToGroupAsync(Context.ConnectionId, "Admins");
            _logger.LogInformation("管理員 {AdminId} 已上線", adminId);

            var onlineUsers = UserConnections.Values.Distinct().ToList();
            await Clients.Caller.SendAsync("OnlineUsersList", onlineUsers);
        }

        public async Task SendMessageToAdmin(ChatMessage message)
        {
            var userId = GetCurrentPrincipalId();
            if (string.IsNullOrEmpty(userId)) return;

            _logger.LogInformation("收到使用者 {UserId} 的讯息: '{Message}'", userId, message.Message);

            // 建立一个扁平结构的 messageData 物件
            var messageData = new
            {
                userId, // 使用者的 ID
                type = message.Type, // 使用前端传来的 type ("text" 或 "image")
                message = message.Message, // 使用前端传来的 message (文字内容或图片URL)
                timestamp = DateTime.Now 
            };

            await Clients.Group("Admins").SendAsync("ReceiveMessage", messageData);
        }

        public async Task SendMessageToUser(string targetUserId, ChatMessage message)
        {
            var adminId = GetCurrentPrincipalId(); // 这会是 AdminId
            if (string.IsNullOrEmpty(adminId)) return;

            _logger.LogInformation("管理员 {AdminId} 发送讯息给使用者 {UserId}", adminId, targetUserId);

            // 建立一个扁平结构的 messageData 物件
            var messageData = new
            {
                userId = adminId, // 发送者是 admin
                type = message.Type.StartsWith("admin") ? message.Type : $"admin-{message.Type}", // 自动加上 admin- 前缀
                message = message.Message,
                timestamp = DateTime.Now
            };

            await Clients.Group($"User_{targetUserId}").SendAsync("ReceiveMessage", messageData);
        }

        // ========================================================================
        // 【新增的主打商品功能方法】
        // ========================================================================
        public async Task FeatureProduct(string productName) // 1. 參數名稱改變
        {
            var adminId = GetCurrentPrincipalId();
            _logger.LogInformation("管理員 {AdminId} 嘗試主打商品名稱: {ProductName}", adminId, productName); // 4. 日誌訊息更新

            // 2. 檢查名稱是否為空
            if (string.IsNullOrWhiteSpace(productName))
            {
                await Clients.Caller.SendAsync("FeatureProductFailed", "商品名稱不能為空。");
                return;
            }

            // 3. 查詢條件從 ID 改為 Name
            var product = await _context.Products
                .Where(p => p.Name == productName) // 產品名稱
                .Select(p => new {
                    Id = p.ProductsId,
                    Name = p.Name,
                    ImageUrl = p.ProductImages.OrderBy(img => img.IsMain == 1 ? 0 : 1).Select(img => img.ImageUrl).FirstOrDefault(),
                    Price = p.Price
                })
                .FirstOrDefaultAsync();

            if (product == null)
            {
                // 4. 錯誤訊息更新
                await Clients.Caller.SendAsync("FeatureProductFailed", $"找不到名稱為 '{productName}' 的商品。");
                return;
            }

            // 除了廣播給觀眾，也要傳回給呼叫者(管理者自己)
            await Clients.Group("LiveStreamViewers").SendAsync("ReceiveFeaturedProduct", product);
            await Clients.Caller.SendAsync("ReceiveFeaturedProduct", product);

            _logger.LogInformation("已成功向所有觀眾主打商品: {ProductName}", product.Name);
        }

        public async Task ClearFeaturedProduct()
        {
            var adminId = GetCurrentPrincipalId();
            _logger.LogInformation("管理員 {AdminId} 清除主打商品", adminId);

            // 【修改處】同樣地，清除時也要通知自己
            await Clients.Group("LiveStreamViewers").SendAsync("ReceiveClearProduct");
            await Clients.Caller.SendAsync("ReceiveClearProduct"); // <--- 新增這一行
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var principalId = GetCurrentPrincipalId();
            if (string.IsNullOrEmpty(principalId))
            {
                await base.OnDisconnectedAsync(exception);
                return;
            }

            // 處理用戶斷線
            if (UserConnections.TryRemove(Context.ConnectionId, out _))
            {
                // 檢查是否該用戶的所有連線都已斷開
                if (!UserConnections.Values.Contains(principalId))
                {
                    _logger.LogInformation("使用者 {UserId} 已完全離線。", principalId);
                    await Clients.Group("Admins").SendAsync("UserOffline", principalId);
                }
            }

            // 處理管理員斷線
            if (AdminConnections.TryRemove(Context.ConnectionId, out _))
            {
                _logger.LogInformation("管理員 {AdminId} 已離線。", principalId);
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}