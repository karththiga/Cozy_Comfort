using System;
using System.ComponentModel.DataAnnotations;

namespace SOC_CozyComfort_API.Models
{
    public class NotificationDto
    {
        public int Id { get; set; }

        [Required]
        public string RecipientRole { get; set; }

        public string RecipientUserName { get; set; }

        [Required]
        public string Title { get; set; }

        [Required]
        public string Message { get; set; }

        [Required]
        public string NotificationType { get; set; }

        public bool IsRead { get; set; }
        public int? RelatedRequestId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
