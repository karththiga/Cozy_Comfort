using System.Collections.Generic;

namespace SOC_Cozy_Comfort_Client.Models
{
    public class RoleDashboardViewModel
    {
        public string Role { get; set; }
        public string LoggedInUser { get; set; }
        public List<InventoryItem> Items { get; set; }
        public InventoryItem NewItem { get; set; }
    }
}
