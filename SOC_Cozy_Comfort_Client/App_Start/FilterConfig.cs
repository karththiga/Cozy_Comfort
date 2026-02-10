using System.Web;
using System.Web.Mvc;

namespace SOC_Cozy_Comfort_Client
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }
    }
}
