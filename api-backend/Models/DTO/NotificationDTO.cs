using System;
using System.Collections.Generic;

namespace api_backend.Models.DTO
{
    public class NotificationDTO
    {
        public int NotificationId { get; set; }
        public int AdminId { get; set; }
        public string AdminName { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int NotificationType { get; set; }
        public DateTime? Time { get; set; }
        public int TotalReceivers { get; set; }
        public int ReadCount { get; set; }
    }

    public class CreateNotificationDto
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public int NotificationType { get; set; }
        public List<int> ReceiverIds { get; set; } = new List<int>();
        public string ReceiverType { get; set; } // "all_users(所有使用者)", "all_admins(所有管理員)", "specific(特定人)"
    }

    public class NotificationReceiverDto
    {
        public int ReceiverId { get; set; }
        public int NotificationId { get; set; }
        public int? UserId { get; set; }
        public int? AdminId { get; set; }
        public string? ReceiverName { get; set; }
        public bool IsRead { get; set; }
        public DateTime? ReadTime { get; set; }
        public DateTime? NotificationTime { get; set; }
    }

    public class UserNotificationDto
    {
        public int NotificationId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int NotificationType { get; set; }
        public DateTime? Time { get; set; }
        public bool IsRead { get; set; }
        public DateTime? ReadTime { get; set; }
        public int ReceiverId { get; internal set; }

    }
}
