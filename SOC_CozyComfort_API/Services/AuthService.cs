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


        public static bool TryCreateUser(string fullName, string email, string userName, string role, string password, out string message)
        {
            message = "";
            if (!IsValidRole(role))
            {
                message = "Selected role is invalid.";
                return false;
            }

            const string sql = @"
IF EXISTS(SELECT 1 FROM dbo.Users WHERE UserName = @UserName)
BEGIN
    SELECT -1;
    RETURN;
END;

IF EXISTS(SELECT 1 FROM dbo.Users WHERE Email = @Email)
BEGIN
    SELECT -2;
    RETURN;
END;

INSERT INTO dbo.Users(UserName, [Password], RoleId, FullName, Email)
SELECT @UserName, @Password, r.Id, @FullName, @Email
FROM dbo.Roles r
WHERE r.RoleName = @Role;

SELECT 1;";

            using (var connection = new SqlConnection(ConnectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@UserName", userName.Trim());
                command.Parameters.AddWithValue("@Password", password);
                command.Parameters.AddWithValue("@Role", role.Trim());
                command.Parameters.AddWithValue("@FullName", fullName.Trim());
                command.Parameters.AddWithValue("@Email", email.Trim());

                connection.Open();
                var result = (int)command.ExecuteScalar();
                if (result == -1)
                {
                    message = "Username already exists.";
                    return false;
                }

                if (result == -2)
                {
                    message = "Email already exists.";
                    return false;
                }

                message = "Sign up successful. You can login now.";
                return true;
            }
        }

    }
}
