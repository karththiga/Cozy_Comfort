using System.Collections.Generic;
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
        public IHttpActionResult GetByRole(string role)
        {
            if (!InventoryRepository.IsValidRole(role))
            {
                return BadRequest("Invalid role.");
            }

            var items = InventoryRepository.GetByRole(role);
            return Ok(items);
        }

        [HttpGet]
        [Route("{role}/{id:int}")]
        public IHttpActionResult GetById(string role, int id)
        {
            if (!InventoryRepository.IsValidRole(role))
            {
                return BadRequest("Invalid role.");
            }

            var item = InventoryRepository.GetById(role, id);
            if (item == null)
            {
                return NotFound();
            }

            return Ok(item);
        }

        [HttpPost]
        [Route("{role}")]
        public IHttpActionResult Create(string role, [FromBody] InventoryItemDto item)
        {
            if (!InventoryRepository.IsValidRole(role))
            {
                return BadRequest("Invalid role.");
            }

            if (item == null || string.IsNullOrWhiteSpace(item.Sku) || string.IsNullOrWhiteSpace(item.Name))
            {
                return BadRequest("SKU and Name are required.");
            }

            var created = InventoryRepository.Add(role, item);
            return Ok(created);
        }

        [HttpPut]
        [Route("{role}/{id:int}")]
        public IHttpActionResult Update(string role, int id, [FromBody] InventoryItemDto item)
        {
            if (!InventoryRepository.IsValidRole(role))
            {
                return BadRequest("Invalid role.");
            }

            if (item == null || string.IsNullOrWhiteSpace(item.Sku) || string.IsNullOrWhiteSpace(item.Name))
            {
                return BadRequest("SKU and Name are required.");
            }

            var updated = InventoryRepository.Update(role, id, item);
            if (!updated)
            {
                return NotFound();
            }

            return Ok();
        }

        [HttpDelete]
        [Route("{role}/{id:int}")]
        public IHttpActionResult Delete(string role, int id)
        {
            if (!InventoryRepository.IsValidRole(role))
            {
                return BadRequest("Invalid role.");
            }

            var deleted = InventoryRepository.Delete(role, id);
            if (!deleted)
            {
                return NotFound();
            }

            return Ok();
        }
    }
}
