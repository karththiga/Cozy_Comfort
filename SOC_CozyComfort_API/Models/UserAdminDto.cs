namespace SOC_CozyComfort_API.Models
{
    public class UserAdminDto
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string UserName { get; set; }
        public string RoleName { get; set; }
        public bool IsApproved { get; set; }
    }
}
