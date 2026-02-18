using System;
using System.Configuration;
using System.Net;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using SOC_Cozy_Comfort_Client.Models;
using System.Collections.Generic;

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
                    return new ApiOperationResult { Success = false, Message = ReadErrorMessage(response, "Invalid username or password.") };
                }

                return new ApiOperationResult { Success = false, Message = ReadErrorMessage(response, "Login validation failed from API.") };
            }
        }



        public ApiOperationResult Signup(string fullName, string email, string userName, string role, string password)
        {
            using (var client = BuildClient())
            {
                var request = new { FullName = fullName, Email = email, UserName = userName, Role = role, Password = password };
                var body = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
                var response = client.PostAsync("api/auth/signup", body).Result;

                if (response.IsSuccessStatusCode)
                {
                    var message = ReadMessageObject(response, "Signup request submitted.");
                    return new ApiOperationResult { Success = true, Message = message };
                }

                return new ApiOperationResult { Success = false, Message = ReadErrorMessage(response, "Signup failed from API.") };
            }
        }

        public List<PendingUserItem> GetPendingUsers(string adminUserName)
        {
            using (var client = BuildClient())
            {
                var response = client.GetAsync($"api/auth/pending-users?adminUserName={WebUtility.UrlEncode(adminUserName)}").Result;
                if (!response.IsSuccessStatusCode)
                {
                    return new List<PendingUserItem>();
                }

                var json = response.Content.ReadAsStringAsync().Result;
                return JsonConvert.DeserializeObject<List<PendingUserItem>>(json) ?? new List<PendingUserItem>();
            }
        }

        public ApiOperationResult ApproveUser(string adminUserName, int userId)
        {
            using (var client = BuildClient())
            {
                var request = new { AdminUserName = adminUserName, UserId = userId };
                var body = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
                var response = client.PostAsync("api/auth/approve-user", body).Result;

                if (response.IsSuccessStatusCode)
                {
                    return new ApiOperationResult { Success = true, Message = ReadMessageObject(response, "User approved successfully.") };
                }

                return new ApiOperationResult { Success = false, Message = ReadErrorMessage(response, "Could not approve user.") };
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

        private static string ReadMessageObject(HttpResponseMessage response, string fallback)
        {
            try
            {
                var json = response.Content.ReadAsStringAsync().Result;
                if (string.IsNullOrWhiteSpace(json))
                {
                    return fallback;
                }

                var payload = JsonConvert.DeserializeAnonymousType(json, new { Message = fallback });
                return payload?.Message ?? fallback;
            }
            catch
            {
                return fallback;
            }
        }
    }
}
