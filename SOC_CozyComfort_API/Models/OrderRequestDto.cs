using System;
using System.ComponentModel.DataAnnotations;

namespace SOC_CozyComfort_API.Models
{
    public class OrderRequestDto
    {
        public int Id { get; set; }

        [Required]
        public string RequestType { get; set; }

        [Required]
        public string RequestedByRole { get; set; }

        [Required]
        public string RequestedToRole { get; set; }

        [Required]
        public string RequestedByUser { get; set; }

        public string RequestedToUser { get; set; }

        [Required]
        public string Sku { get; set; }

        [Required]
        public string BlanketName { get; set; }

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        [Required]
        public string Status { get; set; }

        public string Notes { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int? SourceRequestId { get; set; }
    }

    public class CreateSellerRequestDto
    {
        [Required]
        public string RequestedByUser { get; set; }

        [Required]
        public string Sku { get; set; }

        [Required]
        public string BlanketName { get; set; }

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        public string Notes { get; set; }
    }



    public class CreateCustomerOrderDto
    {
        [Required]
        public string RequestedByUser { get; set; }

        [Required]
        public string Sku { get; set; }

        [Required]
        public string BlanketName { get; set; }

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        public string Notes { get; set; }
    }

    public class RequestActionDto
    {
        [Required]
        public string PerformedByUser { get; set; }

        public string Notes { get; set; }
    }
}
