using System.Collections.Generic;

namespace SOC_Cozy_Comfort_Client.Models
{
    public class RequestBoardViewModel
    {
        public string Role { get; set; }
        public string LoggedInUser { get; set; }
        public List<OrderRequestItem> IncomingRequests { get; set; }
        public List<OrderRequestItem> OutgoingRequests { get; set; }
        public OrderRequestItem NewRequest { get; set; }
        public List<InventoryItem> SellerCatalogItems { get; set; }
        public Dictionary<string, string> SellerCatalogImageUrls { get; set; }
        public Dictionary<string, string> SellerCatalogDetails { get; set; }
    }
}
