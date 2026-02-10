using System.Web.Mvc;

namespace SOC_Cozy_Comfort_Client.Controllers
{
    public class HomeController : Controller
    {
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

            TempData["LoggedInUser"] = userName;

            switch (role)
            {
                case "Manufacturer":
                    return RedirectToAction("Manufacturer");
                case "Distributor":
                    return RedirectToAction("Distributor");
                case "Seller":
                    return RedirectToAction("Seller");
                default:
                    ViewBag.ErrorMessage = "Invalid role selected.";
                    return View();
            }
        }

        public ActionResult Manufacturer()
        {
            ViewBag.LoggedInUser = TempData["LoggedInUser"];
            return View();
        }

        public ActionResult Distributor()
        {
            ViewBag.LoggedInUser = TempData["LoggedInUser"];
            return View();
        }

        public ActionResult Seller()
        {
            ViewBag.LoggedInUser = TempData["LoggedInUser"];
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
    }
}
