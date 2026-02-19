namespace SOC_CozyComfort_API.Models
{
    public class AdminCreateUserRequestDto
    {
        public string AdminUserName { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string UserName { get; set; }
        public string Role { get; set; }
        public string Password { get; set; }
    }
}
