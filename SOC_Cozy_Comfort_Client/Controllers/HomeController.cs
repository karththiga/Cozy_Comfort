using System.Collections.Generic;
using System.Web.Mvc;
using SOC_Cozy_Comfort_Client.Models;
using SOC_Cozy_Comfort_Client.Services;

namespace SOC_Cozy_Comfort_Client.Controllers
{
    public class HomeController : Controller
    {
        private readonly InventoryApiClient _inventoryApiClient = new InventoryApiClient();

        private static readonly Dictionary<string, string> RoleUserMap = new Dictionary<string, string>
        {
            { "Manufacturer", "m_admin" },
            { "Distributor", "d_admin" },
            { "Seller", "s_admin" }
        };

        private static readonly Dictionary<string, string> RolePasswordMap = new Dictionary<string, string>
        {
            { "Manufacturer", "M@123" },
            { "Distributor", "D@123" },
            { "Seller", "S@123" }
        };

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
            if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(role))
            {
                ViewBag.ErrorMessage = "Please enter username, password, and select a role.";
                return View();
            }

            if (!RoleUserMap.ContainsKey(role) || !RolePasswordMap.ContainsKey(role) ||
                RoleUserMap[role] != userName || RolePasswordMap[role] != password)
            {
                ViewBag.ErrorMessage = "Invalid login details for the selected role.";
                return View();
            }

            Session["LoggedInUser"] = userName;
            Session["LoggedInRole"] = role;

            return RedirectToRoleDashboard(role);
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

            if (newItem == null || string.IsNullOrWhiteSpace(newItem.Sku) || string.IsNullOrWhiteSpace(newItem.Name))
            {
                TempData["InventoryError"] = "SKU and Item Name are required.";
                return RedirectToRoleDashboard(role);
            }

            var created = _inventoryApiClient.Create(role, newItem);
            TempData[created ? "InventoryMessage" : "InventoryError"] = created
                ? "Inventory item added successfully."
                : "Failed to add inventory item through API.";
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

            if (item == null || string.IsNullOrWhiteSpace(item.Sku) || string.IsNullOrWhiteSpace(item.Name))
            {
                ViewBag.Role = role;
                ViewBag.ErrorMessage = "SKU and Item Name are required.";
                return View(item);
            }

            var updated = _inventoryApiClient.Update(role, item.Id, item);
            TempData[updated ? "InventoryMessage" : "InventoryError"] = updated
                ? "Inventory item updated successfully."
                : "Failed to update inventory item through API.";
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

            var deleted = _inventoryApiClient.Delete(role, id);
            TempData[deleted ? "InventoryMessage" : "InventoryError"] = deleted
                ? "Inventory item deleted successfully."
                : "Failed to delete inventory item through API.";
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
