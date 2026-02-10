using System.Collections.Generic;

namespace SOC_CozyComfort_API.Services
{
    public static class AuthService
    {
        private static readonly Dictionary<string, string> RoleUserMap = new Dictionary<string, string>
        {
            { "Manufacturer", "m_admin" },
            { "Distributor", "d_admin" },
            { "Seller", "s_admin" }
        };

        private static readonly Dictionary<string, string> RolePasswordMap = new Dictionary<string, string>
        {
            { "Manufacturer", "M@123" },
            { "Distributor", "D@123" },
            { "Seller", "S@123" }
        };

        public static bool IsValidRole(string role)
        {
            return RoleUserMap.ContainsKey(role) && RolePasswordMap.ContainsKey(role);
        }

        public static bool IsValidLogin(string userName, string password, string role)
        {
            return IsValidRole(role) && RoleUserMap[role] == userName && RolePasswordMap[role] == password;
        }
    }
}
