using System;
using System.ComponentModel.DataAnnotations;

namespace SOC_Cozy_Comfort_Client.Models
{
    public class OrderRequestItem
    {
        public int Id { get; set; }
        public string RequestType { get; set; }
        public string RequestedByRole { get; set; }
        public string RequestedToRole { get; set; }
        public string RequestedByUser { get; set; }
        public string RequestedToUser { get; set; }

        [Required]
        public string Sku { get; set; }

        [Required]
        public string BlanketName { get; set; }

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        public string Status { get; set; }
        public string Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int? SourceRequestId { get; set; }
    }

    public class RequestActionInput
    {
        public string PerformedByUser { get; set; }
        public string Notes { get; set; }
    }
}
