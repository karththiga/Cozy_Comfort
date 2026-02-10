using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using SOC_Cozy_Comfort_Client.Models;

namespace SOC_Cozy_Comfort_Client.Services
{
    /// <summary>
    /// Encapsulates all HTTP calls from MVC client to the inventory API.
    /// </summary>
    public class InventoryApiClient
    {
        private readonly string _baseUrl;

        public InventoryApiClient()
        {
            _baseUrl = ConfigurationManager.AppSettings["InventoryApiBaseUrl"] ?? "http://localhost:59573";
        }

        public List<InventoryItem> GetByRole(string role)
        {
            using (var client = BuildClient())
            {
                var response = client.GetAsync("api/inventory/" + role).Result;
                if (!response.IsSuccessStatusCode)
                {
                    return new List<InventoryItem>();
                }

                var json = response.Content.ReadAsStringAsync().Result;
                return JsonConvert.DeserializeObject<List<InventoryItem>>(json) ?? new List<InventoryItem>();
            }
        }

        public InventoryItem GetById(string role, int id)
        {
            using (var client = BuildClient())
            {
                var response = client.GetAsync("api/inventory/" + role + "/" + id).Result;
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var json = response.Content.ReadAsStringAsync().Result;
                return JsonConvert.DeserializeObject<InventoryItem>(json);
            }
        }

        public ApiOperationResult Create(string role, InventoryItem item)
        {
            using (var client = BuildClient())
            {
                var body = new StringContent(JsonConvert.SerializeObject(item), Encoding.UTF8, "application/json");
                var response = client.PostAsync("api/inventory/" + role, body).Result;
                return BuildResult(response, "Inventory item added successfully.");
            }
        }

        public ApiOperationResult Update(string role, int id, InventoryItem item)
        {
            using (var client = BuildClient())
            {
                var body = new StringContent(JsonConvert.SerializeObject(item), Encoding.UTF8, "application/json");
                var response = client.PutAsync("api/inventory/" + role + "/" + id, body).Result;
                return BuildResult(response, "Inventory item updated successfully.");
            }
        }

        public ApiOperationResult Delete(string role, int id)
        {
            using (var client = BuildClient())
            {
                var response = client.DeleteAsync("api/inventory/" + role + "/" + id).Result;
                return BuildResult(response, "Inventory item deleted successfully.");
            }
        }

        private HttpClient BuildClient()
        {
            var client = new HttpClient { BaseAddress = new Uri(_baseUrl.TrimEnd('/') + "/") };
            return client;
        }

        private static ApiOperationResult BuildResult(HttpResponseMessage response, string successMessage)
        {
            if (response.IsSuccessStatusCode)
            {
                return new ApiOperationResult { Success = true, Message = successMessage };
            }

            return new ApiOperationResult
            {
                Success = false,
                Message = ReadErrorMessage(response, "API validation failed.")
            };
        }

        private static string ReadErrorMessage(HttpResponseMessage response, string fallback)
        {
            try
            {
                var msg = response.Content.ReadAsStringAsync().Result;
                return string.IsNullOrWhiteSpace(msg) ? fallback : msg.Trim('"');
            }
            catch
            {
                return fallback;
            }
        }
    }
}
