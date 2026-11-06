using api_backend.Models;
using api_backend.Models.DTO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace api_backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationsController : ControllerBase
    {
        private readonly TeamDb2Context _context;

        public NotificationsController(TeamDb2Context context)
        {
            _context = context;
        }

        // 發送通知
        [HttpPost("Send")]
        [Authorize]
        public async Task<IActionResult> SendNotification([FromBody] CreateNotificationDto dto)
        {
            var AdminId = User.Claims.FirstOrDefault(c => c.Type == "AdminId")?.Value;
            if (string.IsNullOrEmpty(AdminId) || !int.TryParse(AdminId, out int adminId))  //AdminId(string) -> adminId(int)
            {
                return Unauthorized(new {message = "無效的管理員Token"});
            }
            
            // 1. 建立 Notification
            var notification = new Notification
            {
                Title = dto.Title,
                Description = dto.Description,
                NotificationType = dto.NotificationType,
                Time = DateTime.Now,
                AdminId = adminId  /* 取得目前登入管理員ID */
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            // 2. 產生 Receiver 名單
            List<int> userIds = new();
            List<int> adminIds = new();
            if (dto.ReceiverType == "all_users")
                userIds = _context.Users.Select(u => u.UserId).ToList();
            else if (dto.ReceiverType == "all_admins")
                adminIds = _context.Admins.Select(a => a.AdminId).ToList();
            else if (dto.ReceiverType == "specific")
            {
                // 根據 receiverIds 判斷是 user 還是 admin
                // 這裡假設 receiverIds 都是 userId
                userIds = dto.ReceiverIds;
            }

            // 3. 建立 NotificationReceiver
            foreach (var uid in userIds)
            {
                _context.NotificationReceivers.Add(new NotificationReceiver
                {
                    NotificationId = notification.NotificationId,
                    UserId = uid,
                    IsRead = false
                });
            }
            foreach (var aid in adminIds)
            {
                _context.NotificationReceivers.Add(new NotificationReceiver
                {
                    NotificationId = notification.NotificationId,
                    AdminId = aid,
                    IsRead = false
                });
            }
            await _context.SaveChangesAsync();

            return Ok();
        }
        
        // 查詢使用者通知
        [HttpGet("User/{userId}")]
        public IActionResult GetUserNotifications(int userId)
        {
            var notifications = from nr in _context.NotificationReceivers
                                join n in _context.Notifications on nr.NotificationId equals n.NotificationId
                                where nr.UserId == userId
                                orderby n.Time descending
                                select new UserNotificationDto
                                {
                                    NotificationId = n.NotificationId,
                                    Title = n.Title,
                                    Description = n.Description,
                                    NotificationType = n.NotificationType,
                                    Time = n.Time,
                                    IsRead = nr.IsRead,
                                    ReadTime = nr.ReadTime,
                                    ReceiverId = nr.ReceiverId
                                };
            return Ok(notifications.ToList());
        }

        // 查詢管理員通知
        [HttpGet("Admin/{adminId}")]
        [Authorize]
        public IActionResult GetAdminNotifications(int adminId) 
        {
            // 驗證目前登入的管理員是否為查詢的管理員
            var currentAdminId = User.Claims.FirstOrDefault(c => c.Type == "AdminId")?.Value;
            if (string.IsNullOrEmpty(currentAdminId) || !int.TryParse(currentAdminId, out int currentAdminIdInt) ) 
            {
                return Unauthorized(new {message = "無效的管理員Token"});
            }

            // 確保管理員只能查詢自己的通知
            if (currentAdminIdInt != adminId) 
            {
                return Forbid();
            }

            var notifications = from nr in _context.NotificationReceivers
                                join n in _context.Notifications on nr.NotificationId equals n.NotificationId
                                where nr.AdminId == adminId
                                orderby n.Time descending
                                select new UserNotificationDto
                                {
                                    NotificationId = n.NotificationId,
                                    Title = n.Title,
                                    Description = n.Description,
                                    NotificationType = n.NotificationType,
                                    Time = n.Time,
                                    IsRead = nr.IsRead,
                                    ReadTime = nr.ReadTime,
                                    ReceiverId = nr.ReceiverId
                                };
            return Ok(notifications.ToList());
        }

        // 已讀通知
        [HttpPost("Read")]
        public async Task<IActionResult> MarkAsRead([FromBody] NotificationReceiverDto dto)
        {
            var receiver = await _context.NotificationReceivers.FindAsync(dto.ReceiverId);
            if (receiver == null) return NotFound();
            receiver.IsRead = true;
            receiver.ReadTime = DateTime.Now;
            await _context.SaveChangesAsync();
            return Ok();
        }

    }
}
