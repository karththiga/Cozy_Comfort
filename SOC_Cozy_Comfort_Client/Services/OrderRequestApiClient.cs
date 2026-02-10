using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using SOC_Cozy_Comfort_Client.Models;

namespace SOC_Cozy_Comfort_Client.Services
{
    public class OrderRequestApiClient
    {
        private readonly string _baseUrl;

        public OrderRequestApiClient()
        {
            _baseUrl = ConfigurationManager.AppSettings["InventoryApiBaseUrl"] ?? "https://localhost:44377";
        }

        public List<OrderRequestItem> GetIncoming(string role)
        {
            return GetList("api/orderrequests/incoming/" + role);
        }

        public List<OrderRequestItem> GetOutgoing(string role)
        {
            return GetList("api/orderrequests/outgoing/" + role);
        }

        public ApiOperationResult CreateSellerRequest(string userName, OrderRequestItem request)
        {
            var payload = new
            {
                RequestedByUser = userName,
                Sku = request.Sku,
                BlanketName = request.BlanketName,
                Quantity = request.Quantity,
                Notes = request.Notes
            };

            return Post("api/orderrequests/seller-to-distributor", payload, "Request sent to distributor.");
        }

        public ApiOperationResult DistributorEscalate(int requestId, string userName, string notes)
        {
            return Post("api/orderrequests/distributor/escalate/" + requestId, new { PerformedByUser = userName, Notes = notes }, "Request escalated to manufacturer.");
        }

        public ApiOperationResult DistributorFulfill(int requestId, string userName, string notes)
        {
            return Post("api/orderrequests/distributor/fulfill/" + requestId, new { PerformedByUser = userName, Notes = notes }, "Distributor marked request as fulfilled.");
        }

        public ApiOperationResult ManufacturerStartProduction(int requestId, string userName, string notes)
        {
            return Post("api/orderrequests/manufacturer/start-production/" + requestId, new { PerformedByUser = userName, Notes = notes }, "Production started.");
        }

        public ApiOperationResult ManufacturerDispatch(int requestId, string userName, string notes)
        {
            return Post("api/orderrequests/manufacturer/dispatch/" + requestId, new { PerformedByUser = userName, Notes = notes }, "Manufacturer dispatched blankets to distributor.");
        }

        private List<OrderRequestItem> GetList(string path)
        {
            using (var client = BuildClient())
            {
                var response = client.GetAsync(path).Result;
                if (!response.IsSuccessStatusCode)
                {
                    return new List<OrderRequestItem>();
                }

                var json = response.Content.ReadAsStringAsync().Result;
                return JsonConvert.DeserializeObject<List<OrderRequestItem>>(json) ?? new List<OrderRequestItem>();
            }
        }

        private ApiOperationResult Post(string path, object payload, string successMessage)
        {
            using (var client = BuildClient())
            {
                var body = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
                var response = client.PostAsync(path, body).Result;

                if (response.IsSuccessStatusCode)
                {
                    return new ApiOperationResult { Success = true, Message = successMessage };
                }

                var msg = response.Content.ReadAsStringAsync().Result;
                return new ApiOperationResult { Success = false, Message = string.IsNullOrWhiteSpace(msg) ? "Request failed." : msg.Trim('"') };
            }
        }

        private HttpClient BuildClient()
        {
            return new HttpClient { BaseAddress = new Uri(_baseUrl.TrimEnd('/') + "/") };
        }
    }
}
