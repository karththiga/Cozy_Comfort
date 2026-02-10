using System.Web.Mvc;
using SOC_Cozy_Comfort_Client.Models;
using SOC_Cozy_Comfort_Client.Services;

namespace SOC_Cozy_Comfort_Client.Controllers
{
    public class HomeController : Controller
    {
        private readonly InventoryApiClient _inventoryApiClient = new InventoryApiClient();
        private readonly AuthApiClient _authApiClient = new AuthApiClient();

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
        public ActionResult Login(string userName, string password, string role)
        {
            LoginApiResponse responsePayload;
            var result = _authApiClient.Login(userName, password, role, out responsePayload);
            if (!result.Success)
            {
                ViewBag.ErrorMessage = result.Message;
                return View();
            }

            Session["LoggedInUser"] = responsePayload.UserName;
            Session["LoggedInRole"] = responsePayload.Role;

            return RedirectToRoleDashboard(responsePayload.Role);
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
