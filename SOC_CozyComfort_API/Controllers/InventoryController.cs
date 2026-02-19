using System.Linq;
using System.Web.Http;
using SOC_CozyComfort_API.Models;
using SOC_CozyComfort_API.Services;

namespace SOC_CozyComfort_API.Controllers
{
    /// <summary>
    /// Exposes role-scoped inventory CRUD endpoints for client applications.
    /// </summary>
    [RoutePrefix("api/inventory")]
    public class InventoryController : ApiController
    {
        [HttpGet]
        [Route("{role}")]
        public IHttpActionResult GetByRole(string role, string userName = null)
        {
            if (!InventoryRepository.IsValidRole(role))
            {
                return BadRequest("Invalid role.");
            }

            return Ok(InventoryRepository.GetByRole(role, userName));
        }

        [HttpGet]
        [Route("{role}/{id:int}")]
        public IHttpActionResult GetById(string role, int id, string userName = null)
        {
            if (!InventoryRepository.IsValidRole(role))
            {
                return BadRequest("Invalid role.");
            }

            var item = InventoryRepository.GetById(role, id, userName);
            if (item == null)
            {
                return NotFound();
            }

            return Ok(item);
        }

        [HttpPost]
        [Route("{role}")]
        public IHttpActionResult Create(string role, [FromBody] InventoryItemDto item, string userName = null)
        {
            if (!InventoryRepository.IsValidRole(role))
            {
                return BadRequest("Invalid role.");
            }

            if (!ValidatePayload(item))
            {
                return BadRequest(GetModelStateErrors());
            }

            var created = InventoryRepository.Add(role, item, userName);
            return Ok(created);
        }

        [HttpPut]
        [Route("{role}/{id:int}")]
        public IHttpActionResult Update(string role, int id, [FromBody] InventoryItemDto item, string userName = null)
        {
            if (!InventoryRepository.IsValidRole(role))
            {
                return BadRequest("Invalid role.");
            }

            if (!ValidatePayload(item))
            {
                return BadRequest(GetModelStateErrors());
            }

            var updated = InventoryRepository.Update(role, id, item, userName);
            if (!updated)
            {
                return NotFound();
            }

            return Ok();
        }

        [HttpDelete]
        [Route("{role}/{id:int}")]
        public IHttpActionResult Delete(string role, int id, string userName = null)
        {
            if (!InventoryRepository.IsValidRole(role))
            {
                return BadRequest("Invalid role.");
            }

            var deleted = InventoryRepository.Delete(role, id, userName);
            if (!deleted)
            {
                return NotFound();
            }

            return Ok();
        }

        private bool ValidatePayload(InventoryItemDto item)
        {
            if (item == null)
            {
                ModelState.AddModelError("item", "Request body is required.");
                return false;
            }

            Validate(item);
            return ModelState.IsValid;
        }

        private string GetModelStateErrors()
        {
            return string.Join(" | ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
        }
    }
}
