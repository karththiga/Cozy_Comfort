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
            if (!AuthService.TryCreateUser(request.FullName, request.Email, request.UserName, request.Role, request.Password, request.DistributorUserId, request.SellerLocation, false, out message))
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

            var loginResult = AuthService.ValidateLogin(request.UserName, request.Password);
            if (!loginResult.IsSuccess)
            {
                return Content(System.Net.HttpStatusCode.Unauthorized, loginResult.Message);
            }

            return Ok(new LoginResponseDto
            {
                UserName = request.UserName,
                Role = loginResult.Role,
                Message = loginResult.Message
            });
        }

        [HttpGet]
        [Route("distributors")]
        public IHttpActionResult Distributors()
        {
            return Ok(AuthService.GetApprovedDistributors());
        }

        [HttpPost]
        [Route("admin-create-user")]
        public IHttpActionResult AdminCreateUser([FromBody] AdminCreateUserRequestDto request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.AdminUserName) || string.IsNullOrWhiteSpace(request.FullName) ||
                string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.UserName) ||
                string.IsNullOrWhiteSpace(request.Role) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest("All fields are required.");
            }

            if (!AuthService.IsAdminUser(request.AdminUserName))
            {
                return Unauthorized();
            }

            if (request.Role != "Distributor" && request.Role != "Manufacturer")
            {
                return BadRequest("Admin can only create Distributor or Manufacturer accounts.");
            }

            string message;
            if (!AuthService.TryCreateUser(request.FullName, request.Email, request.UserName, request.Role, request.Password, null, null, true, out message))
            {
                return BadRequest(message);
            }

            return Ok(new { Message = message });
        }


        [HttpGet]
        [Route("users")]
        public IHttpActionResult Users(string adminUserName)
        {
            if (string.IsNullOrWhiteSpace(adminUserName) || !AuthService.IsAdminUser(adminUserName))
            {
                return Unauthorized();
            }

            return Ok(AuthService.GetUsersForAdmin());
        }

        [HttpPost]
        [Route("delete-user")]
        public IHttpActionResult DeleteUser([FromBody] DeleteUserRequestDto request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.AdminUserName) || request.UserId <= 0)
            {
                return BadRequest("Admin username and user id are required.");
            }

            if (!AuthService.IsAdminUser(request.AdminUserName))
            {
                return Unauthorized();
            }

            string message;
            if (!AuthService.DeleteUserByAdmin(request.UserId, request.AdminUserName, out message))
            {
                return BadRequest(message);
            }

            return Ok(new { Message = message });
        }

        [HttpGet]
        [Route("pending-users")]
        public IHttpActionResult PendingUsers(string adminUserName)
        {
            if (string.IsNullOrWhiteSpace(adminUserName) || !AuthService.IsAdminUser(adminUserName))
            {
                return Unauthorized();
            }

            return Ok(AuthService.GetPendingUsers());
        }

        [HttpPost]
        [Route("approve-user")]
        public IHttpActionResult ApproveUser([FromBody] ApproveUserRequestDto request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.AdminUserName) || request.UserId <= 0)
            {
                return BadRequest("Admin username and user id are required.");
            }

            if (!AuthService.IsAdminUser(request.AdminUserName))
            {
                return Unauthorized();
            }

            if (!AuthService.ApproveUser(request.UserId, request.AdminUserName))
            {
                return BadRequest("Unable to approve user. It may already be approved.");
            }

            return Ok(new { Message = "User approved successfully." });
        }
    }
}
