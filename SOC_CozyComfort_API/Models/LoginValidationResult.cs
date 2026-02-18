namespace SOC_CozyComfort_API.Models
{
    public class LoginValidationResult
    {
        public bool IsSuccess { get; set; }
        public string Role { get; set; }
        public string Message { get; set; }
    }
}
