using System;
using System.Configuration;
using System.Net;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using SOC_Cozy_Comfort_Client.Models;

namespace SOC_Cozy_Comfort_Client.Services
{
    public class AuthApiClient
    {
        private readonly string _baseUrl;

        public AuthApiClient()
        {
            _baseUrl = ConfigurationManager.AppSettings["InventoryApiBaseUrl"] ?? "https://localhost:44377";
        }

        public ApiOperationResult Login(string userName, string password, out LoginApiResponse payload)
        {
            payload = null;
            using (var client = BuildClient())
            {
                var request = new { UserName = userName, Password = password };
                var body = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
                var response = client.PostAsync("api/auth/login", body).Result;

                if (response.IsSuccessStatusCode)
                {
                    var json = response.Content.ReadAsStringAsync().Result;
                    payload = JsonConvert.DeserializeObject<LoginApiResponse>(json);
                    return new ApiOperationResult { Success = true, Message = payload?.Message ?? "Login successful." };
                }

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    return new ApiOperationResult { Success = false, Message = "Invalid username or password." };
                }

                return new ApiOperationResult { Success = false, Message = ReadErrorMessage(response, "Login validation failed from API.") };
            }
        }

        private HttpClient BuildClient()
        {
            return new HttpClient { BaseAddress = new Uri(_baseUrl.TrimEnd('/') + "/") };
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
