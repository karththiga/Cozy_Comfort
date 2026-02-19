using System;
using System.ComponentModel.DataAnnotations;

namespace SOC_CozyComfort_API.Models
{
    public class InventoryItemDto
    {
        public int Id { get; set; }

        [Required]
        public string Sku { get; set; }

        [Required]
        public string Name { get; set; }

        [Range(0, int.MaxValue)]
        public int Quantity { get; set; }

        public string Location { get; set; }

        public string OwnerUserName { get; set; }

        public string OwnerFullName { get; set; }

        public string SellerLocation { get; set; }

        public DateTime LastUpdated { get; set; }
    }
}
