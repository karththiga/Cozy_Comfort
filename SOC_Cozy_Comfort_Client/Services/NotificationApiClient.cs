using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net.Http;
using Newtonsoft.Json;
using SOC_Cozy_Comfort_Client.Models;

namespace SOC_Cozy_Comfort_Client.Services
{
    public class NotificationApiClient
    {
        private readonly string _baseUrl;

        public NotificationApiClient()
        {
            _baseUrl = ConfigurationManager.AppSettings["InventoryApiBaseUrl"] ?? "https://localhost:44377";
        }

        public List<NotificationItem> GetByRole(string role, string userName)
        {
            using (var client = BuildClient())
            {
                var encodedUser = Uri.EscapeDataString(userName ?? string.Empty);
                var response = client.GetAsync("api/notifications/" + role + "?userName=" + encodedUser).Result;
                if (!response.IsSuccessStatusCode)
                {
                    return new List<NotificationItem>();
                }

                var json = response.Content.ReadAsStringAsync().Result;
                return JsonConvert.DeserializeObject<List<NotificationItem>>(json) ?? new List<NotificationItem>();
            }
        }

        public ApiOperationResult MarkRead(string role, string userName, int id)
        {
            using (var client = BuildClient())
            {
                var encodedUser = Uri.EscapeDataString(userName ?? string.Empty);
                var response = client.PostAsync("api/notifications/" + role + "/read/" + id + "?userName=" + encodedUser, new StringContent("{}", System.Text.Encoding.UTF8, "application/json")).Result;
                return new ApiOperationResult
                {
                    Success = response.IsSuccessStatusCode,
                    Message = response.IsSuccessStatusCode ? "Notification marked as read." : "Unable to update notification."
                };
            }
        }

        private HttpClient BuildClient()
        {
            return new HttpClient { BaseAddress = new Uri(_baseUrl.TrimEnd('/') + "/") };
        }
    }
}
