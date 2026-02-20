using System.Linq;
using System.Web.Http;
using SOC_CozyComfort_API.Models;
using SOC_CozyComfort_API.Services;

namespace SOC_CozyComfort_API.Controllers
{
    [RoutePrefix("api/orderrequests")]
    public class OrderRequestsController : ApiController
    {
        [HttpGet]
        [Route("incoming/{role}")]
        public IHttpActionResult Incoming(string role, string userName = null)
        {
            if (!AuthService.IsValidRole(role))
            {
                return BadRequest("Invalid role.");
            }

            return Ok(OrderRequestRepository.GetIncoming(role, userName));
        }

        [HttpGet]
        [Route("outgoing/{role}")]
        public IHttpActionResult Outgoing(string role, string userName = null)
        {
            if (!AuthService.IsValidRole(role))
            {
                return BadRequest("Invalid role.");
            }

            return Ok(OrderRequestRepository.GetOutgoing(role, userName));
        }

        [HttpPost]
        [Route("customer-to-seller")]
        public IHttpActionResult CreateCustomerToSeller([FromBody] CreateCustomerOrderDto request)
        {
            if (!ValidatePayload(request))
            {
                return BadRequest(GetModelStateErrors());
            }

            OrderRequestDto created;
            try
            {
                created = OrderRequestRepository.CreateCustomerToSeller(request);
            }
            catch (System.InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }

            return Ok(created);
        }

        [HttpPost]
        [Route("seller/confirm-customer/{requestId:int}")]
        public IHttpActionResult SellerConfirmCustomer(int requestId, [FromBody] RequestActionDto action)
        {
            if (!ValidatePayload(action))
            {
                return BadRequest(GetModelStateErrors());
            }

            return OrderRequestRepository.ConfirmCustomerOrderBySeller(requestId, action) ? (IHttpActionResult)Ok() : NotFound();
        }

        [HttpPost]
        [Route("seller-to-distributor")]
        public IHttpActionResult CreateSellerToDistributor([FromBody] CreateSellerRequestDto request)
        {
            if (!ValidatePayload(request))
            {
                return BadRequest(GetModelStateErrors());
            }

            OrderRequestDto created;
            try
            {
                created = OrderRequestRepository.CreateSellerToDistributor(request);
            }
            catch (System.InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            return Ok(created);
        }

        [HttpPost]
        [Route("distributor/escalate/{requestId:int}")]
        public IHttpActionResult DistributorEscalate(int requestId, [FromBody] RequestActionDto action)
        {
            if (!ValidatePayload(action))
            {
                return BadRequest(GetModelStateErrors());
            }

            var created = OrderRequestRepository.EscalateToManufacturer(requestId, action);
            if (created == null)
            {
                return NotFound();
            }

            return Ok(created);
        }

        [HttpPost]
        [Route("distributor/fulfill/{requestId:int}")]
        public IHttpActionResult DistributorFulfill(int requestId, [FromBody] RequestActionDto action)
        {
            if (!ValidatePayload(action))
            {
                return BadRequest(GetModelStateErrors());
            }

            return OrderRequestRepository.MarkDistributorFulfilled(requestId, action) ? (IHttpActionResult)Ok() : NotFound();
        }

        [HttpPost]
        [Route("manufacturer/start-production/{requestId:int}")]
        public IHttpActionResult ManufacturerStartProduction(int requestId, [FromBody] RequestActionDto action)
        {
            if (!ValidatePayload(action))
            {
                return BadRequest(GetModelStateErrors());
            }

            return OrderRequestRepository.MarkManufacturerProductionStarted(requestId, action) ? (IHttpActionResult)Ok() : NotFound();
        }

        [HttpPost]
        [Route("manufacturer/dispatch/{requestId:int}")]
        public IHttpActionResult ManufacturerDispatch(int requestId, [FromBody] RequestActionDto action)
        {
            if (!ValidatePayload(action))
            {
                return BadRequest(GetModelStateErrors());
            }

            string message;
            if (!OrderRequestRepository.MarkManufacturerDispatched(requestId, action, out message))
            {
                return BadRequest(message);
            }

            return Ok(new { Message = message });
        }


        [HttpPost]
        [Route("seller/cancel/{requestId:int}")]
        public IHttpActionResult SellerCancel(int requestId, [FromBody] RequestActionDto action)
        {
            if (!ValidatePayload(action))
            {
                return BadRequest(GetModelStateErrors());
            }

            return OrderRequestRepository.CancelBySeller(requestId, action) ? (IHttpActionResult)Ok() : NotFound();
        }

        [HttpPost]
        [Route("distributor/cancel/{requestId:int}")]
        public IHttpActionResult DistributorCancel(int requestId, [FromBody] RequestActionDto action)
        {
            if (!ValidatePayload(action))
            {
                return BadRequest(GetModelStateErrors());
            }

            return OrderRequestRepository.CancelByDistributor(requestId, action) ? (IHttpActionResult)Ok() : NotFound();
        }

        [HttpPost]
        [Route("manufacturer/cancel/{requestId:int}")]
        public IHttpActionResult ManufacturerCancel(int requestId, [FromBody] RequestActionDto action)
        {
            if (!ValidatePayload(action))
            {
                return BadRequest(GetModelStateErrors());
            }

            return OrderRequestRepository.CancelByManufacturer(requestId, action) ? (IHttpActionResult)Ok() : NotFound();
        }

        private bool ValidatePayload<T>(T model)
        {
            if (model == null)
            {
                ModelState.AddModelError("payload", "Request body is required.");
                return false;
            }

            Validate(model);
            return ModelState.IsValid;
        }

        private string GetModelStateErrors()
        {
            return string.Join(" | ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
        }
    }
}
