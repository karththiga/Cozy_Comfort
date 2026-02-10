using System.Web.Mvc;

namespace SOC_Cozy_Comfort_Client.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Manufacturer()
        {
            return View();
        }

        public ActionResult Distributor()
        {
            return View();
        }

        public ActionResult Seller()
        {
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
