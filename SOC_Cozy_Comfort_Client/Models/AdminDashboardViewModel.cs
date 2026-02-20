using System.Collections.Generic;

namespace SOC_Cozy_Comfort_Client.Models
{
    public class AdminDashboardViewModel
    {
        public string LoggedInUser { get; set; }
        public List<PendingUserItem> PendingUsers { get; set; }
        public List<UserAdminItem> Users { get; set; }
    }
}
