using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using SOC_CozyComfort_API.Models;

namespace SOC_CozyComfort_API.Services
{
    public static class InventoryRepository
    {
        private static string ConnectionString => ConfigurationManager.ConnectionStrings["CozyComfortDb"].ConnectionString;
        private const string ManufacturerDefaultLocation = "Main Manufacturing Facility";
        private const string DistributorDefaultLocationSuffix = "Hub";

        public static bool IsValidRole(string role)
        {
            return AuthService.IsValidRole(role);
        }

        public static List<InventoryItemDto> GetByRole(string role, string userName = null)
        {
            var result = new List<InventoryItemDto>();
            const string sql = @"
SELECT i.Id, i.Sku, i.[Name], i.Quantity, i.[Location], i.OwnerUserName, u.FullName AS OwnerFullName, u.SellerLocation, i.LastUpdated
FROM dbo.InventoryItems i
LEFT JOIN dbo.Users u ON u.UserName = i.OwnerUserName
WHERE i.RoleName = @RoleName
  AND (@OwnerUserName IS NULL OR i.OwnerUserName = @OwnerUserName)
ORDER BY i.Sku";

            using (var connection = new SqlConnection(ConnectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@RoleName", role);
                command.Parameters.AddWithValue("@OwnerUserName", GetOwnerUserNameParam(role, userName));
                connection.Open();

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(Map(reader));
                    }
                }
            }

            return result;
        }

        public static InventoryItemDto GetById(string role, int id, string userName = null)
        {
            const string sql = @"
SELECT i.Id, i.Sku, i.[Name], i.Quantity, i.[Location], i.OwnerUserName, u.FullName AS OwnerFullName, u.SellerLocation, i.LastUpdated
FROM dbo.InventoryItems i
LEFT JOIN dbo.Users u ON u.UserName = i.OwnerUserName
WHERE i.RoleName = @RoleName
  AND i.Id = @Id
  AND (@OwnerUserName IS NULL OR i.OwnerUserName = @OwnerUserName)";

            using (var connection = new SqlConnection(ConnectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@RoleName", role);
                command.Parameters.AddWithValue("@OwnerUserName", GetOwnerUserNameParam(role, userName));
                command.Parameters.AddWithValue("@Id", id);
                connection.Open();

                using (var reader = command.ExecuteReader())
                {
                    return reader.Read() ? Map(reader) : null;
                }
            }
        }

        public static InventoryItemDto Add(string role, InventoryItemDto item, string userName = null)
        {
            const string sql = @"
INSERT INTO dbo.InventoryItems(RoleName, OwnerUserName, Sku, [Name], Quantity, [Location], LastUpdated)
VALUES(@RoleName, @OwnerUserName, @Sku, @Name, @Quantity, @Location, @LastUpdated);
SELECT CAST(SCOPE_IDENTITY() AS INT);";

            var now = DateTime.Now;
            var normalizedLocation = NormalizeLocation(role, userName, item.Location);
            using (var connection = new SqlConnection(ConnectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@RoleName", role);
                command.Parameters.AddWithValue("@OwnerUserName", GetOwnerUserNameParam(role, userName));
                command.Parameters.AddWithValue("@Sku", item.Sku);
                command.Parameters.AddWithValue("@Name", item.Name);
                command.Parameters.AddWithValue("@Quantity", item.Quantity);
                command.Parameters.AddWithValue("@Location", (object)normalizedLocation ?? DBNull.Value);
                command.Parameters.AddWithValue("@LastUpdated", now);

                connection.Open();
                var id = (int)command.ExecuteScalar();
                item.Id = id;
                item.Location = normalizedLocation;
                item.LastUpdated = now;
                return item;
            }
        }

        public static bool Update(string role, int id, InventoryItemDto item, string userName = null)
        {
            const string sql = @"
UPDATE dbo.InventoryItems
SET Sku = @Sku,
    [Name] = @Name,
    Quantity = @Quantity,
    [Location] = @Location,
    LastUpdated = @LastUpdated
WHERE RoleName = @RoleName
  AND (@OwnerUserName IS NULL OR OwnerUserName = @OwnerUserName)
  AND Id = @Id";

            using (var connection = new SqlConnection(ConnectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                var normalizedLocation = NormalizeLocation(role, userName, item.Location);
                command.Parameters.AddWithValue("@Sku", item.Sku);
                command.Parameters.AddWithValue("@Name", item.Name);
                command.Parameters.AddWithValue("@Quantity", item.Quantity);
                command.Parameters.AddWithValue("@Location", (object)normalizedLocation ?? DBNull.Value);
                command.Parameters.AddWithValue("@LastUpdated", DateTime.Now);
                command.Parameters.AddWithValue("@RoleName", role);
                command.Parameters.AddWithValue("@OwnerUserName", GetOwnerUserNameParam(role, userName));
                command.Parameters.AddWithValue("@Id", id);

                connection.Open();
                return command.ExecuteNonQuery() > 0;
            }
        }

        private static string NormalizeLocation(string role, string userName, string location)
        {
            if (string.Equals(role, "Manufacturer", StringComparison.OrdinalIgnoreCase))
            {
                return ManufacturerDefaultLocation;
            }

            if (string.Equals(role, "Distributor", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrWhiteSpace(userName))
                {
                    var existingLocation = GetDistributorLocationForOwner(userName);
                    if (!string.IsNullOrWhiteSpace(existingLocation))
                    {
                        return existingLocation;
                    }

                    return userName + " " + DistributorDefaultLocationSuffix;
                }

                return "Distributor " + DistributorDefaultLocationSuffix;
            }

            return location;
        }

        private static string GetDistributorLocationForOwner(string ownerUserName)
        {
            const string sql = @"SELECT TOP 1 [Location]
FROM dbo.InventoryItems
WHERE RoleName = 'Distributor'
  AND OwnerUserName = @OwnerUserName
  AND [Location] IS NOT NULL
  AND LTRIM(RTRIM([Location])) <> ''
ORDER BY LastUpdated DESC";

            using (var connection = new SqlConnection(ConnectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@OwnerUserName", ownerUserName);
                connection.Open();
                return command.ExecuteScalar() as string;
            }
        }

        public static bool Delete(string role, int id, string userName = null)
        {
            const string sql = @"DELETE FROM dbo.InventoryItems
WHERE RoleName = @RoleName
  AND Id = @Id
  AND (@OwnerUserName IS NULL OR OwnerUserName = @OwnerUserName)";

            using (var connection = new SqlConnection(ConnectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@RoleName", role);
                command.Parameters.AddWithValue("@OwnerUserName", GetOwnerUserNameParam(role, userName));
                command.Parameters.AddWithValue("@Id", id);
                connection.Open();
                return command.ExecuteNonQuery() > 0;
            }
        }

        private static object GetOwnerUserNameParam(string role, string userName)
        {
            return (string.Equals(role, "Distributor", StringComparison.OrdinalIgnoreCase)
                || string.Equals(role, "Seller", StringComparison.OrdinalIgnoreCase))
                ? (object)(userName ?? string.Empty)
                : DBNull.Value;
        }

        private static InventoryItemDto Map(SqlDataReader reader)
        {
            return new InventoryItemDto
            {
                Id = Convert.ToInt32(reader["Id"]),
                Sku = Convert.ToString(reader["Sku"]),
                Name = Convert.ToString(reader["Name"]),
                Quantity = Convert.ToInt32(reader["Quantity"]),
                Location = reader["Location"] == DBNull.Value ? null : Convert.ToString(reader["Location"]),
                OwnerUserName = reader["OwnerUserName"] == DBNull.Value ? null : Convert.ToString(reader["OwnerUserName"]),
                OwnerFullName = reader["OwnerFullName"] == DBNull.Value ? null : Convert.ToString(reader["OwnerFullName"]),
                SellerLocation = reader["SellerLocation"] == DBNull.Value ? null : Convert.ToString(reader["SellerLocation"]),
                LastUpdated = Convert.ToDateTime(reader["LastUpdated"])
            };
        }
    }
}
