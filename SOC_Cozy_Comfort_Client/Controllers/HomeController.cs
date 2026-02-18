using System.Collections.Generic;
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
        private readonly NotificationApiClient _notificationApiClient = new NotificationApiClient();

        private static readonly string[] CustomerBlanketImageUrls =
        {
            "https://nanascraftyhome.com/wp-content/uploads/2021/11/Marjorie-Blanket-1-scaled.jpg",
            "https://www.marymaxim.ca/cdn/shop/files/M95559.jpg?v=1713794020",
            "https://i.ytimg.com/vi/2R6NSSxe-Dk/hq720.jpg?sqp=-oaymwEhCK4FEIIDSFryq4qpAxMIARUAAAAAGAElAADIQj0AgKJD&rs=AOn4CLBXUZQZiDYHMEIdArUnFQWWL9ouEg",
            "https://i.pinimg.com/736x/23/93/e7/2393e731c54011ecdf287c53f2f2ee3b.jpg",
            "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcSs58Vz8rJVp-CK27C8dMu3Ag7yf5qJX87gcA&s",
            "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcTyY2gvs9wTv1Z3A893E11hRcG_3pvlLhT-CA&s",
            "https://shop.babyspace.lk/cdn/shop/files/100_cotton-plain120x120cmbabyspaceshopIII_1.png?v=1721132338&width=533",
            "https://www.inkandbrayer.co.nz/cdn/shop/articles/RuanuiStation-Stack-NZ-WoolThrows_1024x566.jpg?v=1695890470"
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

        [HttpGet]
        public ActionResult Signup()
        {
            return View();
        }

        [HttpGet]
        public ActionResult CustomerSignup()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Signup(string fullName, string email, string userName, string role, string password, string confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(role))
            {
                ViewBag.ErrorMessage = "All fields are required.";
                return View();
            }

            if (string.IsNullOrWhiteSpace(password) || password != confirmPassword)
            {
                ViewBag.ErrorMessage = "Password and confirm password must match.";
                return View();
            }

            var signupResult = _authApiClient.Signup(fullName, email, userName, role, password);
            if (!signupResult.Success)
            {
                ViewBag.ErrorMessage = signupResult.Message;
                return View();
            }

            TempData["AuthMessage"] = signupResult.Message;
            return RedirectToAction("Login");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CustomerSignup(string fullName, string email, string userName, string password, string confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(userName))
            {
                ViewBag.ErrorMessage = "All fields are required.";
                return View();
            }

            if (string.IsNullOrWhiteSpace(password) || password != confirmPassword)
            {
                ViewBag.ErrorMessage = "Password and confirm password must match.";
                return View();
            }

            var signupResult = _authApiClient.Signup(fullName, email, userName, "Customer", password);
            if (!signupResult.Success)
            {
                ViewBag.ErrorMessage = signupResult.Message;
                return View();
            }

            TempData["AuthMessage"] = signupResult.Message;
            return RedirectToAction("Login");
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

        [HttpGet]
        public ActionResult Customer()
        {
            if (!IsAuthorizedFor("Customer"))
            {
                return RedirectToAction("Login");
            }

            var sellerCatalogItems = _inventoryApiClient.GetByRole("Seller");

            var model = new RequestBoardViewModel
            {
                Role = "Customer",
                LoggedInUser = Session["LoggedInUser"] as string,
                OutgoingRequests = _orderRequestApiClient.GetOutgoing("Customer"),
                SellerCatalogItems = sellerCatalogItems,
                SellerCatalogImageUrls = BuildSellerCatalogImageUrls(sellerCatalogItems),
                SellerCatalogDetails = BuildSellerCatalogDetails(sellerCatalogItems),
                NewRequest = new OrderRequestItem()
            };

            return View(model);
        }

        [HttpGet]
        public ActionResult Admin()
        {
            if (!IsAuthorizedFor("Admin"))
            {
                return RedirectToAction("Login");
            }

            var model = new AdminDashboardViewModel
            {
                LoggedInUser = Session["LoggedInUser"] as string,
                PendingUsers = _authApiClient.GetPendingUsers(Session["LoggedInUser"] as string)
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ApproveUser(int userId)
        {
            if (!IsAuthorizedFor("Admin"))
            {
                return RedirectToAction("Login");
            }

            var adminUserName = Session["LoggedInUser"] as string;
            var result = _authApiClient.ApproveUser(adminUserName, userId);
            TempData[result.Success ? "AuthMessage" : "InventoryError"] = result.Message;
            return RedirectToAction("Admin");
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
        public ActionResult ProcessCustomerOrder(string sku, string blanketName, int quantity, string notes)
        {
            if (!IsAuthorizedFor("Seller"))
            {
                return RedirectToAction("Login");
            }

            if (string.IsNullOrWhiteSpace(sku) || quantity <= 0)
            {
                TempData["RequestError"] = "Customer order requires SKU and quantity.";
                return RedirectToAction("SellerRequests");
            }

            var sellerItems = _inventoryApiClient.GetByRole("Seller");
            var inventoryItem = sellerItems.Find(i => string.Equals(i.Sku, sku, System.StringComparison.OrdinalIgnoreCase));

            if (inventoryItem != null && inventoryItem.Quantity >= quantity)
            {
                inventoryItem.Quantity -= quantity;
                var updateResult = _inventoryApiClient.Update("Seller", inventoryItem.Id, inventoryItem);
                TempData[updateResult.Success ? "RequestMessage" : "RequestError"] = updateResult.Success
                    ? $"Customer order fulfilled from seller stock for {sku} (Qty: {quantity})."
                    : updateResult.Message;
                return RedirectToAction("SellerRequests");
            }

            var shortageQty = quantity;
            if (inventoryItem != null && inventoryItem.Quantity > 0)
            {
                shortageQty = quantity - inventoryItem.Quantity;
            }

            var newRequest = new OrderRequestItem
            {
                Sku = sku,
                BlanketName = string.IsNullOrWhiteSpace(blanketName) ? (inventoryItem?.Name ?? sku) : blanketName,
                Quantity = shortageQty <= 0 ? quantity : shortageQty,
                Notes = string.IsNullOrWhiteSpace(notes)
                    ? $"Auto-created from customer order at seller. Requested qty: {quantity}."
                    : notes
            };

            var createResult = _orderRequestApiClient.CreateSellerRequest(Session["LoggedInUser"] as string, newRequest);
            TempData[createResult.Success ? "RequestMessage" : "RequestError"] = createResult.Success
                ? $"Seller stock is not enough for {sku}. Request sent to distributor for qty {newRequest.Quantity}."
                : createResult.Message;

            return RedirectToAction("SellerRequests");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateCustomerOrder(OrderRequestItem newRequest)
        {
            if (!IsAuthorizedFor("Customer"))
            {
                return RedirectToAction("Login");
            }

            var userName = Session["LoggedInUser"] as string;
            var result = _orderRequestApiClient.CreateCustomerOrder(userName, newRequest);
            TempData[result.Success ? "RequestMessage" : "RequestError"] = result.Message;
            return RedirectToAction("Customer");
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
        public ActionResult SellerConfirmCustomerOrder(int requestId, string notes)
        {
            if (!IsAuthorizedFor("Seller"))
            {
                return RedirectToAction("Login");
            }

            var result = _orderRequestApiClient.SellerConfirmCustomerOrder(requestId, Session["LoggedInUser"] as string, notes);
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


        [HttpGet]
        public ActionResult Notifications()
        {
            var role = Session["LoggedInRole"] as string;
            if (string.IsNullOrWhiteSpace(role))
            {
                return RedirectToAction("Login");
            }

            return View(_notificationApiClient.GetByRole(role));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult MarkNotificationRead(int id)
        {
            var role = Session["LoggedInRole"] as string;
            if (string.IsNullOrWhiteSpace(role))
            {
                return RedirectToAction("Login");
            }

            var result = _notificationApiClient.MarkRead(role, id);
            TempData[result.Success ? "RequestMessage" : "RequestError"] = result.Message;
            return RedirectToAction("Notifications");
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


        private Dictionary<string, string> BuildSellerCatalogImageUrls(List<InventoryItem> items)
        {
            var result = new Dictionary<string, string>();
            if (items == null || items.Count == 0)
            {
                return result;
            }

            for (var i = 0; i < items.Count; i++)
            {
                var sku = items[i].Sku ?? string.Empty;
                if (!result.ContainsKey(sku))
                {
                    var img = CustomerBlanketImageUrls[i % CustomerBlanketImageUrls.Length];
                    result.Add(sku, img);
                }
            }

            return result;
        }

        private Dictionary<string, string> BuildSellerCatalogDetails(List<InventoryItem> items)
        {
            var result = new Dictionary<string, string>();
            if (items == null)
            {
                return result;
            }

            foreach (var item in items)
            {
                var key = item.Sku ?? string.Empty;
                if (!result.ContainsKey(key))
                {
                    var locationText = string.IsNullOrWhiteSpace(item.Location) ? "Seller Warehouse" : item.Location;
                    result.Add(key, "Location: " + locationText + " • Last Updated: " + item.LastUpdated.ToString("yyyy-MM-dd HH:mm"));
                }
            }

            return result;
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
                case "Admin":
                    return RedirectToAction("Admin");
                case "Customer":
                    return RedirectToAction("Customer");
                default:
                    return RedirectToAction("Login");
            }
        }
    }
}
