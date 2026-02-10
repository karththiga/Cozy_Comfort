using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using SOC_CozyComfort_API.Models;

namespace SOC_CozyComfort_API.Services
{
    public static class OrderRequestRepository
    {
        private static string ConnectionString => ConfigurationManager.ConnectionStrings["CozyComfortDb"].ConnectionString;

        public static List<OrderRequestDto> GetIncoming(string role)
        {
            return GetBySql(@"
SELECT *
FROM dbo.OrderRequests
WHERE RequestedToRole = @Role
ORDER BY CreatedAt DESC", role);
        }

        public static List<OrderRequestDto> GetOutgoing(string role)
        {
            return GetBySql(@"
SELECT *
FROM dbo.OrderRequests
WHERE RequestedByRole = @Role
ORDER BY CreatedAt DESC", role);
        }

        public static OrderRequestDto CreateSellerToDistributor(CreateSellerRequestDto request)
        {
            var dto = new OrderRequestDto
            {
                RequestType = "SellerToDistributor",
                RequestedByRole = "Seller",
                RequestedToRole = "Distributor",
                RequestedByUser = request.RequestedByUser,
                Sku = request.Sku,
                BlanketName = request.BlanketName,
                Quantity = request.Quantity,
                Status = "PendingDistributorReview",
                Notes = request.Notes
            };

            return Insert(dto);
        }

        public static OrderRequestDto EscalateToManufacturer(int sellerRequestId, RequestActionDto action)
        {
            var source = GetById(sellerRequestId);
            if (source == null || source.RequestedToRole != "Distributor")
            {
                return null;
            }

            var created = Insert(new OrderRequestDto
            {
                RequestType = "DistributorToManufacturer",
                RequestedByRole = "Distributor",
                RequestedToRole = "Manufacturer",
                RequestedByUser = action.PerformedByUser,
                Sku = source.Sku,
                BlanketName = source.BlanketName,
                Quantity = source.Quantity,
                Status = "PendingManufacturerReview",
                Notes = string.IsNullOrWhiteSpace(action.Notes) ? "Escalated by distributor due to low stock." : action.Notes,
                SourceRequestId = source.Id
            });

            UpdateStatus(source.Id, "EscalatedToManufacturer", action.Notes);
            return created;
        }

        public static bool MarkDistributorFulfilled(int requestId, RequestActionDto action)
        {
            return UpdateStatus(requestId, "FulfilledByDistributor", action.Notes, "Distributor");
        }

        public static bool MarkManufacturerProductionStarted(int requestId, RequestActionDto action)
        {
            return UpdateStatus(requestId, "ProductionInProgress", action.Notes, "Manufacturer");
        }

        public static bool MarkManufacturerDispatched(int requestId, RequestActionDto action)
        {
            return UpdateStatus(requestId, "DispatchedByManufacturer", action.Notes, "Manufacturer");
        }

        public static OrderRequestDto GetById(int id)
        {
            using (var connection = new SqlConnection(ConnectionString))
            using (var command = new SqlCommand("SELECT * FROM dbo.OrderRequests WHERE Id = @Id", connection))
            {
                command.Parameters.AddWithValue("@Id", id);
                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    return reader.Read() ? Map(reader) : null;
                }
            }
        }

        private static List<OrderRequestDto> GetBySql(string sql, string role)
        {
            var result = new List<OrderRequestDto>();
            using (var connection = new SqlConnection(ConnectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@Role", role);
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

        private static OrderRequestDto Insert(OrderRequestDto request)
        {
            const string sql = @"
INSERT INTO dbo.OrderRequests
(RequestType, RequestedByRole, RequestedToRole, RequestedByUser, Sku, BlanketName, Quantity, [Status], Notes, CreatedAt, UpdatedAt, SourceRequestId)
VALUES
(@RequestType, @RequestedByRole, @RequestedToRole, @RequestedByUser, @Sku, @BlanketName, @Quantity, @Status, @Notes, @CreatedAt, @UpdatedAt, @SourceRequestId);
SELECT CAST(SCOPE_IDENTITY() AS INT);";

            var now = DateTime.Now;
            using (var connection = new SqlConnection(ConnectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@RequestType", request.RequestType);
                command.Parameters.AddWithValue("@RequestedByRole", request.RequestedByRole);
                command.Parameters.AddWithValue("@RequestedToRole", request.RequestedToRole);
                command.Parameters.AddWithValue("@RequestedByUser", request.RequestedByUser);
                command.Parameters.AddWithValue("@Sku", request.Sku);
                command.Parameters.AddWithValue("@BlanketName", request.BlanketName);
                command.Parameters.AddWithValue("@Quantity", request.Quantity);
                command.Parameters.AddWithValue("@Status", request.Status);
                command.Parameters.AddWithValue("@Notes", (object)request.Notes ?? DBNull.Value);
                command.Parameters.AddWithValue("@CreatedAt", now);
                command.Parameters.AddWithValue("@UpdatedAt", now);
                command.Parameters.Add("@SourceRequestId", SqlDbType.Int).Value = (object)request.SourceRequestId ?? DBNull.Value;

                connection.Open();
                request.Id = Convert.ToInt32(command.ExecuteScalar());
                request.CreatedAt = now;
                request.UpdatedAt = now;
                return request;
            }
        }

        private static bool UpdateStatus(int requestId, string status, string notes, string expectedRecipientRole = null)
        {
            var sql = @"
UPDATE dbo.OrderRequests
SET [Status] = @Status,
    Notes = COALESCE(@Notes, Notes),
    UpdatedAt = @UpdatedAt
WHERE Id = @Id";

            if (!string.IsNullOrWhiteSpace(expectedRecipientRole))
            {
                sql += " AND RequestedToRole = @RequestedToRole";
            }

            using (var connection = new SqlConnection(ConnectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@Status", status);
                command.Parameters.AddWithValue("@Notes", (object)notes ?? DBNull.Value);
                command.Parameters.AddWithValue("@UpdatedAt", DateTime.Now);
                command.Parameters.AddWithValue("@Id", requestId);
                if (!string.IsNullOrWhiteSpace(expectedRecipientRole))
                {
                    command.Parameters.AddWithValue("@RequestedToRole", expectedRecipientRole);
                }

                connection.Open();
                return command.ExecuteNonQuery() > 0;
            }
        }

        private static OrderRequestDto Map(SqlDataReader reader)
        {
            return new OrderRequestDto
            {
                Id = Convert.ToInt32(reader["Id"]),
                RequestType = Convert.ToString(reader["RequestType"]),
                RequestedByRole = Convert.ToString(reader["RequestedByRole"]),
                RequestedToRole = Convert.ToString(reader["RequestedToRole"]),
                RequestedByUser = Convert.ToString(reader["RequestedByUser"]),
                Sku = Convert.ToString(reader["Sku"]),
                BlanketName = Convert.ToString(reader["BlanketName"]),
                Quantity = Convert.ToInt32(reader["Quantity"]),
                Status = Convert.ToString(reader["Status"]),
                Notes = reader["Notes"] == DBNull.Value ? null : Convert.ToString(reader["Notes"]),
                CreatedAt = Convert.ToDateTime(reader["CreatedAt"]),
                UpdatedAt = Convert.ToDateTime(reader["UpdatedAt"]),
                SourceRequestId = reader["SourceRequestId"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["SourceRequestId"])
            };
        }
    }
}
