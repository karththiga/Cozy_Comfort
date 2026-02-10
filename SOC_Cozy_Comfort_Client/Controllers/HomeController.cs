using System.Web.Mvc;
using SOC_Cozy_Comfort_Client.Models;
using SOC_Cozy_Comfort_Client.Services;

namespace SOC_Cozy_Comfort_Client.Controllers
{
    public class HomeController : Controller
    {
        private readonly InventoryApiClient _inventoryApiClient = new InventoryApiClient();
        private readonly AuthApiClient _authApiClient = new AuthApiClient();
        private readonly OrderRequestApiClient _orderRequestApiClient = new OrderRequestApiClient();

        public ActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(string userName, string password)
        {
            LoginApiResponse responsePayload;
            var result = _authApiClient.Login(userName, password, out responsePayload);
            if (!result.Success)
            {
                ViewBag.ErrorMessage = result.Message;
                return View();
            }

            Session["LoggedInUser"] = responsePayload.UserName;
            Session["LoggedInRole"] = responsePayload.Role;

            return RedirectToAction("Index");
        }

        public ActionResult Logout()
        {
            Session.Clear();
            return RedirectToAction("Login");
        }

        public ActionResult Manufacturer()
        {
            return RenderDashboard("Manufacturer");
        }

        public ActionResult Distributor()
        {
            return RenderDashboard("Distributor");
        }

        public ActionResult Seller()
        {
            return RenderDashboard("Seller");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddInventory(string role, InventoryItem newItem)
        {
            if (!IsAuthorizedFor(role))
            {
                return RedirectToAction("Login");
            }

            var result = _inventoryApiClient.Create(role, newItem);
            TempData[result.Success ? "InventoryMessage" : "InventoryError"] = result.Message;
            return RedirectToRoleDashboard(role);
        }

        [HttpGet]
        public ActionResult EditInventory(string role, int id)
        {
            if (!IsAuthorizedFor(role))
            {
                return RedirectToAction("Login");
            }

            var item = _inventoryApiClient.GetById(role, id);
            if (item == null)
            {
                TempData["InventoryError"] = "Inventory item not found.";
                return RedirectToRoleDashboard(role);
            }

            ViewBag.Role = role;
            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditInventory(string role, InventoryItem item)
        {
            if (!IsAuthorizedFor(role))
            {
                return RedirectToAction("Login");
            }

            var result = _inventoryApiClient.Update(role, item.Id, item);
            if (!result.Success)
            {
                ViewBag.Role = role;
                ViewBag.ErrorMessage = result.Message;
                return View(item);
            }

            TempData["InventoryMessage"] = result.Message;
            return RedirectToRoleDashboard(role);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteInventory(string role, int id)
        {
            if (!IsAuthorizedFor(role))
            {
                return RedirectToAction("Login");
            }

            var result = _inventoryApiClient.Delete(role, id);
            TempData[result.Success ? "InventoryMessage" : "InventoryError"] = result.Message;
            return RedirectToRoleDashboard(role);
        }

        [HttpGet]
        public ActionResult SellerRequests()
        {
            if (!IsAuthorizedFor("Seller"))
            {
                return RedirectToAction("Login");
            }

            var model = new RequestBoardViewModel
            {
                Role = "Seller",
                LoggedInUser = Session["LoggedInUser"] as string,
                OutgoingRequests = _orderRequestApiClient.GetOutgoing("Seller"),
                IncomingRequests = _orderRequestApiClient.GetIncoming("Seller"),
                NewRequest = new OrderRequestItem()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateSellerRequest(OrderRequestItem newRequest)
        {
            if (!IsAuthorizedFor("Seller"))
            {
                return RedirectToAction("Login");
            }

            var userName = Session["LoggedInUser"] as string;
            var result = _orderRequestApiClient.CreateSellerRequest(userName, newRequest);
            TempData[result.Success ? "RequestMessage" : "RequestError"] = result.Message;
            return RedirectToAction("SellerRequests");
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SellerCancelRequest(int requestId, string notes)
        {
            if (!IsAuthorizedFor("Seller"))
            {
                return RedirectToAction("Login");
            }

            var result = _orderRequestApiClient.SellerCancel(requestId, Session["LoggedInUser"] as string, notes);
            TempData[result.Success ? "RequestMessage" : "RequestError"] = result.Message;
            return RedirectToAction("SellerRequests");
        }

        [HttpGet]
        public ActionResult DistributorRequests()
        {
            if (!IsAuthorizedFor("Distributor"))
            {
                return RedirectToAction("Login");
            }

            var model = new RequestBoardViewModel
            {
                Role = "Distributor",
                LoggedInUser = Session["LoggedInUser"] as string,
                IncomingRequests = _orderRequestApiClient.GetIncoming("Distributor"),
                OutgoingRequests = _orderRequestApiClient.GetOutgoing("Distributor")
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DistributorEscalate(int requestId, string notes)
        {
            if (!IsAuthorizedFor("Distributor"))
            {
                return RedirectToAction("Login");
            }

            var result = _orderRequestApiClient.DistributorEscalate(requestId, Session["LoggedInUser"] as string, notes);
            TempData[result.Success ? "RequestMessage" : "RequestError"] = result.Message;
            return RedirectToAction("DistributorRequests");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DistributorFulfill(int requestId, string notes)
        {
            if (!IsAuthorizedFor("Distributor"))
            {
                return RedirectToAction("Login");
            }

            var result = _orderRequestApiClient.DistributorFulfill(requestId, Session["LoggedInUser"] as string, notes);
            TempData[result.Success ? "RequestMessage" : "RequestError"] = result.Message;
            return RedirectToAction("DistributorRequests");
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DistributorCancelRequest(int requestId, string notes)
        {
            if (!IsAuthorizedFor("Distributor"))
            {
                return RedirectToAction("Login");
            }

            var result = _orderRequestApiClient.DistributorCancel(requestId, Session["LoggedInUser"] as string, notes);
            TempData[result.Success ? "RequestMessage" : "RequestError"] = result.Message;
            return RedirectToAction("DistributorRequests");
        }

        [HttpGet]
        public ActionResult ManufacturerRequests()
        {
            if (!IsAuthorizedFor("Manufacturer"))
            {
                return RedirectToAction("Login");
            }

            var model = new RequestBoardViewModel
            {
                Role = "Manufacturer",
                LoggedInUser = Session["LoggedInUser"] as string,
                IncomingRequests = _orderRequestApiClient.GetIncoming("Manufacturer"),
                OutgoingRequests = _orderRequestApiClient.GetOutgoing("Manufacturer")
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ManufacturerStartProduction(int requestId, string notes)
        {
            if (!IsAuthorizedFor("Manufacturer"))
            {
                return RedirectToAction("Login");
            }

            var result = _orderRequestApiClient.ManufacturerStartProduction(requestId, Session["LoggedInUser"] as string, notes);
            TempData[result.Success ? "RequestMessage" : "RequestError"] = result.Message;
            return RedirectToAction("ManufacturerRequests");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ManufacturerDispatch(int requestId, string notes)
        {
            if (!IsAuthorizedFor("Manufacturer"))
            {
                return RedirectToAction("Login");
            }

            var result = _orderRequestApiClient.ManufacturerDispatch(requestId, Session["LoggedInUser"] as string, notes);
            TempData[result.Success ? "RequestMessage" : "RequestError"] = result.Message;
            return RedirectToAction("ManufacturerRequests");
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ManufacturerCancelRequest(int requestId, string notes)
        {
            if (!IsAuthorizedFor("Manufacturer"))
            {
                return RedirectToAction("Login");
            }

            var result = _orderRequestApiClient.ManufacturerCancel(requestId, Session["LoggedInUser"] as string, notes);
            TempData[result.Success ? "RequestMessage" : "RequestError"] = result.Message;
            return RedirectToAction("ManufacturerRequests");
        }

        public ActionResult Orders()
        {
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "SOC architecture overview for Cozy Comfort.";
            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Project communication channels.";
            return View();
        }

        private ActionResult RenderDashboard(string role)
        {
            if (!IsAuthorizedFor(role))
            {
                return RedirectToAction("Login");
            }

            var model = new RoleDashboardViewModel
            {
                Role = role,
                LoggedInUser = Session["LoggedInUser"] as string,
                Items = _inventoryApiClient.GetByRole(role),
                NewItem = new InventoryItem()
            };

            return View(role, model);
        }

        private bool IsAuthorizedFor(string requiredRole)
        {
            var loggedInRole = Session["LoggedInRole"] as string;
            return !string.IsNullOrWhiteSpace(loggedInRole) && loggedInRole == requiredRole;
        }

        private ActionResult RedirectToRoleDashboard(string role)
        {
            switch (role)
            {
                case "Manufacturer":
                    return RedirectToAction("Manufacturer");
                case "Distributor":
                    return RedirectToAction("Distributor");
                case "Seller":
                    return RedirectToAction("Seller");
                default:
                    return RedirectToAction("Login");
            }
        }
    }
}
