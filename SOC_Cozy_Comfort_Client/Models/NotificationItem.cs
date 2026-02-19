using System;

namespace SOC_Cozy_Comfort_Client.Models
{
    public class NotificationItem
    {
        public int Id { get; set; }
        public string RecipientRole { get; set; }
        public string RecipientUserName { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public string NotificationType { get; set; }
        public bool IsRead { get; set; }
        public int? RelatedRequestId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
