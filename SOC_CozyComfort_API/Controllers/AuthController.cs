using System.Web.Http;
using SOC_CozyComfort_API.Models;
using SOC_CozyComfort_API.Services;

namespace SOC_CozyComfort_API.Controllers
{
    [RoutePrefix("api/auth")]
    public class AuthController : ApiController
    {
        [HttpPost]
        [Route("login")]
        public IHttpActionResult Login([FromBody] LoginRequestDto request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.UserName) || string.IsNullOrWhiteSpace(request.Password) || string.IsNullOrWhiteSpace(request.Role))
            {
                return BadRequest("Username, password and role are required.");
            }

            if (!AuthService.IsValidRole(request.Role))
            {
                return BadRequest("Invalid role.");
            }

            if (!AuthService.IsValidLogin(request.UserName, request.Password, request.Role))
            {
                return Unauthorized();
            }

            return Ok(new LoginResponseDto
            {
                UserName = request.UserName,
                Role = request.Role,
                Message = "Login successful."
            });
        }
    }
}
