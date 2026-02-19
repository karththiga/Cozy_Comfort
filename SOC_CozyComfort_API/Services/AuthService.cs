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
            return TryCreateUser(fullName, email, userName, role, password, null, false, out message);
        }

        public static bool TryCreateUser(string fullName, string email, string userName, string role, string password, int? distributorUserId, bool createdByAdmin, out string message)
        {
            message = "";
            if (!IsValidRole(role) || role == "Admin")
            {
                message = "Selected role is invalid.";
                return false;
            }

            var normalizedRole = role.Trim();
            var isSellerRole = string.Equals(normalizedRole, "Seller", System.StringComparison.OrdinalIgnoreCase);
            var isDistributorRole = string.Equals(normalizedRole, "Distributor", System.StringComparison.OrdinalIgnoreCase);
            var isManufacturerRole = string.Equals(normalizedRole, "Manufacturer", System.StringComparison.OrdinalIgnoreCase);

            if (!createdByAdmin && (isDistributorRole || isManufacturerRole))
            {
                message = "Self signup is not available for Distributor or Manufacturer accounts.";
                return false;
            }

            if (isSellerRole && (!distributorUserId.HasValue || distributorUserId.Value <= 0))
            {
                message = "Seller signup requires selecting a distributor.";
                return false;
            }

            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                if (isManufacturerRole)
                {
                    using (var manufacturerCheck = new SqlCommand(@"SELECT COUNT(1)
FROM dbo.Users u
JOIN dbo.Roles r ON r.Id = u.RoleId
WHERE r.RoleName = 'Manufacturer'", connection))
                    {
                        if ((int)manufacturerCheck.ExecuteScalar() > 0)
                        {
                            message = "Only one manufacturer account is allowed in the system.";
                            return false;
                        }
                    }
                }

                if (isSellerRole)
                {
                    using (var distributorCheck = new SqlCommand(@"SELECT COUNT(1)
FROM dbo.Users u
JOIN dbo.Roles r ON r.Id = u.RoleId
WHERE u.Id = @DistributorUserId
  AND r.RoleName = 'Distributor'
  AND u.IsApproved = 1", connection))
                    {
                        distributorCheck.Parameters.AddWithValue("@DistributorUserId", distributorUserId.Value);
                        if ((int)distributorCheck.ExecuteScalar() == 0)
                        {
                            message = "Selected distributor is not available.";
                            return false;
                        }
                    }
                }

                var isCustomerRole = string.Equals(normalizedRole, "Customer", System.StringComparison.OrdinalIgnoreCase);
                var isApproved = isCustomerRole || createdByAdmin;

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

INSERT INTO dbo.Users(UserName, [Password], RoleId, FullName, Email, IsApproved, DistributorUserId, ApprovedBy, ApprovedAt)
SELECT @UserName, @Password, r.Id, @FullName, @Email, @IsApproved, @DistributorUserId, @ApprovedBy, @ApprovedAt
FROM dbo.Roles r
WHERE r.RoleName = @Role;

SELECT 1;";

                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@UserName", userName.Trim());
                    command.Parameters.AddWithValue("@Password", password);
                    command.Parameters.AddWithValue("@Role", normalizedRole);
                    command.Parameters.AddWithValue("@FullName", fullName.Trim());
                    command.Parameters.AddWithValue("@Email", email.Trim());
                    command.Parameters.AddWithValue("@IsApproved", isApproved ? 1 : 0);
                    command.Parameters.AddWithValue("@DistributorUserId", (object)distributorUserId ?? System.DBNull.Value);
                    command.Parameters.AddWithValue("@ApprovedBy", createdByAdmin ? "admin" : (object)System.DBNull.Value);
                    command.Parameters.AddWithValue("@ApprovedAt", createdByAdmin ? (object)System.DateTime.Now : System.DBNull.Value);

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

                    message = createdByAdmin
                        ? "User account created successfully."
                        : isCustomerRole
                        ? "Customer account created. You can login and place orders now."
                        : "Signup request submitted. Wait for admin approval before login.";
                    return true;
                }
            }
        }

        public static List<DistributorOptionDto> GetApprovedDistributors()
        {
            const string sql = @"SELECT u.Id, u.UserName, u.FullName
FROM dbo.Users u
JOIN dbo.Roles r ON r.Id = u.RoleId
WHERE r.RoleName = 'Distributor'
  AND u.IsApproved = 1
ORDER BY COALESCE(NULLIF(LTRIM(RTRIM(u.FullName)), ''), u.UserName)";

            var distributors = new List<DistributorOptionDto>();
            using (var connection = new SqlConnection(ConnectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var fullName = reader["FullName"] as string;
                        var userName = reader["UserName"] as string;
                        distributors.Add(new DistributorOptionDto
                        {
                            Id = (int)reader["Id"],
                            UserName = userName,
                            FullName = fullName,
                            DisplayName = string.IsNullOrWhiteSpace(fullName) ? userName : fullName + " (" + userName + ")"
                        });
                    }
                }
            }

            return distributors;
        }

        public static string GetAssignedDistributorUserName(string sellerUserName)
        {
            const string sql = @"SELECT d.UserName
FROM dbo.Users s
JOIN dbo.Roles sr ON sr.Id = s.RoleId
LEFT JOIN dbo.Users d ON d.Id = s.DistributorUserId
LEFT JOIN dbo.Roles dr ON dr.Id = d.RoleId
WHERE s.UserName = @SellerUserName
  AND sr.RoleName = 'Seller'
  AND dr.RoleName = 'Distributor'
  AND d.IsApproved = 1";

            using (var connection = new SqlConnection(ConnectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@SellerUserName", sellerUserName);
                connection.Open();
                return command.ExecuteScalar() as string;
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
