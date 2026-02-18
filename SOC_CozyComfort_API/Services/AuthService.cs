using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using SOC_CozyComfort_API.Models;

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

        public static LoginValidationResult ValidateLogin(string userName, string password)
        {
            const string sql = @"
SELECT TOP 1 r.RoleName, u.IsApproved
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

                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return new LoginValidationResult { IsSuccess = false, Message = "Invalid username or password." };
                    }

                    var approved = reader["IsApproved"] != System.DBNull.Value && (bool)reader["IsApproved"];
                    if (!approved)
                    {
                        return new LoginValidationResult { IsSuccess = false, Message = "Your account is waiting for admin approval." };
                    }

                    return new LoginValidationResult
                    {
                        IsSuccess = true,
                        Role = reader["RoleName"] as string,
                        Message = "Login successful."
                    };
                }
            }
        }

        public static bool TryCreateUser(string fullName, string email, string userName, string role, string password, out string message)
        {
            message = "";
            if (!IsValidRole(role) || role == "Admin")
            {
                message = "Selected role is invalid.";
                return false;
            }

            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                var isCustomerRole = string.Equals(role, "Customer", System.StringComparison.OrdinalIgnoreCase);

                var sql = @"IF EXISTS(SELECT 1 FROM dbo.Users WHERE UserName = @UserName)
BEGIN
    SELECT -1;
    RETURN;
END;

IF EXISTS(SELECT 1 FROM dbo.Users WHERE Email = @Email)
BEGIN
    SELECT -2;
    RETURN;
END;

INSERT INTO dbo.Users(UserName, [Password], RoleId, FullName, Email, IsApproved)
SELECT @UserName, @Password, r.Id, @FullName, @Email, @IsApproved
FROM dbo.Roles r
WHERE r.RoleName = @Role;

SELECT 1;";

                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@UserName", userName.Trim());
                    command.Parameters.AddWithValue("@Password", password);
                    command.Parameters.AddWithValue("@Role", role.Trim());
                    command.Parameters.AddWithValue("@FullName", fullName.Trim());
                    command.Parameters.AddWithValue("@Email", email.Trim());
                    command.Parameters.AddWithValue("@IsApproved", isCustomerRole ? 1 : 0);

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

                    message = isCustomerRole
                        ? "Customer account created. You can login and place orders now."
                        : "Signup request submitted. Wait for admin approval before login.";
                    return true;
                }
            }
        }

        public static bool IsAdminUser(string userName)
        {
            const string sql = @"SELECT COUNT(1)
FROM dbo.Users u
JOIN dbo.Roles r ON r.Id = u.RoleId
WHERE u.UserName = @UserName
  AND r.RoleName = 'Admin'
  AND u.IsApproved = 1";

            using (var connection = new SqlConnection(ConnectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@UserName", userName);
                connection.Open();
                return (int)command.ExecuteScalar() > 0;
            }
        }

        public static List<PendingUserDto> GetPendingUsers()
        {
            const string sql = @"SELECT u.Id, u.FullName, u.Email, u.UserName, r.RoleName
FROM dbo.Users u
JOIN dbo.Roles r ON r.Id = u.RoleId
WHERE u.IsApproved = 0
ORDER BY u.Id DESC";

            var users = new List<PendingUserDto>();
            using (var connection = new SqlConnection(ConnectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        users.Add(new PendingUserDto
                        {
                            Id = (int)reader["Id"],
                            FullName = reader["FullName"] as string,
                            Email = reader["Email"] as string,
                            UserName = reader["UserName"] as string,
                            RequestedRole = reader["RoleName"] as string
                        });
                    }
                }
            }

            return users;
        }

        public static bool ApproveUser(int userId, string adminUserName)
        {
            const string sql = @"UPDATE dbo.Users
SET IsApproved = 1,
    ApprovedBy = @ApprovedBy,
    ApprovedAt = GETDATE()
WHERE Id = @Id AND IsApproved = 0";

            using (var connection = new SqlConnection(ConnectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@Id", userId);
                command.Parameters.AddWithValue("@ApprovedBy", adminUserName);
                connection.Open();
                return command.ExecuteNonQuery() > 0;
            }
        }
    }
}
