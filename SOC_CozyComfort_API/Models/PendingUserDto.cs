namespace SOC_CozyComfort_API.Models
{
    public class PendingUserDto
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string UserName { get; set; }
        public string RequestedRole { get; set; }
        public string AssignedDistributor { get; set; }
    }
}
