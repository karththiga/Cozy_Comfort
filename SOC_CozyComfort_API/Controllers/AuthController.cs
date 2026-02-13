using System.Web.Http;
using SOC_CozyComfort_API.Models;
using SOC_CozyComfort_API.Services;

namespace SOC_CozyComfort_API.Controllers
{
    [RoutePrefix("api/auth")]
    public class AuthController : ApiController
    {

        [HttpPost]
        [Route("signup")]
        public IHttpActionResult Signup([FromBody] SignupRequestDto request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.FullName) || string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.UserName) || string.IsNullOrWhiteSpace(request.Role) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest("All signup fields are required.");
            }

            string message;
            if (!AuthService.TryCreateUser(request.FullName, request.Email, request.UserName, request.Role, request.Password, out message))
            {
                return BadRequest(message);
            }

            return Ok(new { Message = message });
        }

        [HttpPost]
        [Route("login")]
        public IHttpActionResult Login([FromBody] LoginRequestDto request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.UserName) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest("Username and password are required.");
            }

            var role = AuthService.GetRoleForLogin(request.UserName, request.Password);
            if (string.IsNullOrWhiteSpace(role))
            {
                return Unauthorized();
            }

            return Ok(new LoginResponseDto
            {
                UserName = request.UserName,
                Role = role,
                Message = "Login successful."
            });
        }
    }
}
