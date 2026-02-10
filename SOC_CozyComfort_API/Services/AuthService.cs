using System.Configuration;
using System.Data.SqlClient;

namespace SOC_CozyComfort_API.Services
{
    public static class AuthService
    {
        private static string ConnectionString => ConfigurationManager.ConnectionStrings["CozyComfortDb"].ConnectionString;

        public static bool IsValidRole(string role)
        {
            using (var connection = new SqlConnection(ConnectionString))
            using (var command = new SqlCommand("SELECT COUNT(1) FROM dbo.Roles WHERE RoleName = @RoleName", connection))
            {
                command.Parameters.AddWithValue("@RoleName", role);
                connection.Open();
                return (int)command.ExecuteScalar() > 0;
            }
        }

        public static string GetRoleForLogin(string userName, string password)
        {
            const string sql = @"
SELECT TOP 1 r.RoleName
FROM dbo.Users u
JOIN dbo.Roles r ON r.Id = u.RoleId
WHERE u.UserName = @UserName
  AND u.[Password] = @Password";

            using (var connection = new SqlConnection(ConnectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@UserName", userName);
                command.Parameters.AddWithValue("@Password", password);
                connection.Open();
                return command.ExecuteScalar() as string;
            }
        }
    }
}
