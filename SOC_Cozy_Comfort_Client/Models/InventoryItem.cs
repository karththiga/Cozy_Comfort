using System;
using System.ComponentModel.DataAnnotations;

namespace SOC_Cozy_Comfort_Client.Models
{
    public class InventoryItem
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "SKU")]
        public string Sku { get; set; }

        [Required]
        [Display(Name = "Item Name")]
        public string Name { get; set; }

        [Range(0, int.MaxValue)]
        public int Quantity { get; set; }

        [Display(Name = "Location")]
        public string Location { get; set; }

        public string OwnerUserName { get; set; }

        [Display(Name = "Last Updated")]
        public DateTime LastUpdated { get; set; }
    }
}
