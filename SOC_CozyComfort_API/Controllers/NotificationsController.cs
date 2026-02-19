using System.Web.Http;
using SOC_CozyComfort_API.Services;

namespace SOC_CozyComfort_API.Controllers
{
    [RoutePrefix("api/notifications")]
    public class NotificationsController : ApiController
    {
        [HttpGet]
        [Route("{role}")]
        public IHttpActionResult GetByRole(string role, [FromUri] string userName)
        {
            if (!AuthService.IsValidRole(role))
            {
                return BadRequest("Invalid role.");
            }

            if (string.IsNullOrWhiteSpace(userName) || !AuthService.IsApprovedUserInRole(userName, role))
            {
                return BadRequest("Invalid notification recipient.");
            }

            return Ok(NotificationRepository.GetByRoleAndUser(role, userName));
        }

        [HttpPost]
        [Route("{role}/read/{id:int}")]
        public IHttpActionResult MarkRead(string role, int id, [FromUri] string userName)
        {
            if (!AuthService.IsValidRole(role))
            {
                return BadRequest("Invalid role.");
            }

            if (string.IsNullOrWhiteSpace(userName) || !AuthService.IsApprovedUserInRole(userName, role))
            {
                return BadRequest("Invalid notification recipient.");
            }

            return NotificationRepository.MarkAsRead(id, role, userName) ? (IHttpActionResult)Ok() : NotFound();
        }
    }
}
