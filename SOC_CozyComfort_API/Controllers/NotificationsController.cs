using System.Web.Http;
using SOC_CozyComfort_API.Services;

namespace SOC_CozyComfort_API.Controllers
{
    [RoutePrefix("api/notifications")]
    public class NotificationsController : ApiController
    {
        [HttpGet]
        [Route("{role}")]
        public IHttpActionResult GetByRole(string role)
        {
            if (!AuthService.IsValidRole(role))
            {
                return BadRequest("Invalid role.");
            }

            return Ok(NotificationRepository.GetByRole(role));
        }

        [HttpPost]
        [Route("{role}/read/{id:int}")]
        public IHttpActionResult MarkRead(string role, int id)
        {
            if (!AuthService.IsValidRole(role))
            {
                return BadRequest("Invalid role.");
            }

            return NotificationRepository.MarkAsRead(id, role) ? (IHttpActionResult)Ok() : NotFound();
        }
    }
}
