namespace SOC_Cozy_Comfort_Client.Models
{
    public class PendingUserItem
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string UserName { get; set; }
        public string RequestedRole { get; set; }
    }
}
