using System.Collections.Generic;
using System.Web.Mvc;

namespace SOC_Cozy_Comfort_Client.Controllers
{
    public class HomeController : Controller
    {
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
            if (!IsAuthorizedFor("Manufacturer"))
            {
                return RedirectToAction("Login");
            }

            ViewBag.LoggedInUser = Session["LoggedInUser"];
            return View();
        }

        public ActionResult Distributor()
        {
            if (!IsAuthorizedFor("Distributor"))
            {
                return RedirectToAction("Login");
            }

            ViewBag.LoggedInUser = Session["LoggedInUser"];
            return View();
        }

        public ActionResult Seller()
        {
            if (!IsAuthorizedFor("Seller"))
            {
                return RedirectToAction("Login");
            }

            ViewBag.LoggedInUser = Session["LoggedInUser"];
            return View();
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
