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

            var created = Insert(dto);
            NotificationRepository.Add("Distributor", "New seller request", $"Seller {request.RequestedByUser} requested {request.Quantity} x {request.BlanketName} ({request.Sku}).", "OrderRequest", created.Id);
            return created;
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
            NotificationRepository.Add("Manufacturer", "Request escalated by distributor", $"Distributor {action.PerformedByUser} escalated request #{source.Id} for {source.Sku}.", "Escalation", created.Id);
            NotificationRepository.Add("Seller", "Request escalated", $"Your request #{source.Id} was escalated to manufacturer.", "Escalation", source.Id);
            return created;
        }

        public static bool MarkDistributorFulfilled(int requestId, RequestActionDto action)
        {
            var request = GetById(requestId);
            if (request == null || !string.Equals(request.RequestedToRole, "Distributor", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var canShipToSeller = string.Equals(request.Status, "PendingDistributorReview", StringComparison.OrdinalIgnoreCase)
                || string.Equals(request.Status, "EscalatedToManufacturer", StringComparison.OrdinalIgnoreCase)
                || string.Equals(request.Status, "ReceivedByDistributor", StringComparison.OrdinalIgnoreCase);
            if (!canShipToSeller)
            {
                return false;
            }

            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    if (!TryDecreaseInventory(connection, transaction, "Distributor", request.Sku, request.Quantity))
                    {
                        transaction.Rollback();
                        return false;
                    }

                    var note = string.IsNullOrWhiteSpace(action.Notes)
                        ? "Shipped by distributor and currently on the way to seller hub."
                        : action.Notes;

                    var ok = UpdateStatus(connection, transaction, requestId, "OnTheWayToSeller", note, expectedToRole: "Distributor", expectedCurrentStatus: request.Status);
                    if (!ok)
                    {
                        transaction.Rollback();
                        return false;
                    }

                    transaction.Commit();
                }
            }

            NotificationRepository.Add("Seller", "Shipment on the way", $"Request #{requestId} is on the way from distributor.", "Dispatch", requestId);
            return true;
        }

        public static bool MarkManufacturerProductionStarted(int requestId, RequestActionDto action)
        {
            var ok = UpdateStatus(requestId, "ProductionInProgress", action.Notes, expectedToRole: "Manufacturer");
            if (ok)
            {
                NotificationRepository.Add("Distributor", "Production started", $"Manufacturer started production for request #{requestId}.", "Production", requestId);
            }
            return ok;
        }

        public static bool MarkManufacturerDispatched(int requestId, RequestActionDto action)
        {
            var request = GetById(requestId);
            if (request == null || !string.Equals(request.RequestedToRole, "Manufacturer", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var canDispatch = string.Equals(request.Status, "PendingManufacturerReview", StringComparison.OrdinalIgnoreCase)
                || string.Equals(request.Status, "ProductionInProgress", StringComparison.OrdinalIgnoreCase);
            if (!canDispatch)
            {
                return false;
            }

            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    if (!TryDecreaseInventory(connection, transaction, "Manufacturer", request.Sku, request.Quantity))
                    {
                        transaction.Rollback();
                        return false;
                    }

                    var note = string.IsNullOrWhiteSpace(action.Notes)
                        ? "Dispatched by manufacturer and currently on the way to distributor hub."
                        : action.Notes;

                    var ok = UpdateStatus(connection, transaction, requestId, "OnTheWayToDistributor", note, expectedToRole: "Manufacturer", expectedCurrentStatus: request.Status);
                    if (!ok)
                    {
                        transaction.Rollback();
                        return false;
                    }

                    transaction.Commit();
                }
            }

            NotificationRepository.Add("Distributor", "Manufacturer shipment on the way", $"Request #{requestId} is on the way from manufacturer.", "Dispatch", requestId);
            return true;
        }


        public static bool MarkSellerReceived(int requestId, RequestActionDto action)
        {
            var request = GetById(requestId);
            if (request == null
                || !string.Equals(request.RequestedByRole, "Seller", StringComparison.OrdinalIgnoreCase)
                || !string.Equals(request.Status, "OnTheWayToSeller", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    AddOrIncreaseInventory(connection, transaction, "Seller", request.Sku, request.BlanketName, request.Quantity, "Seller Hub");
                    var note = string.IsNullOrWhiteSpace(action.Notes)
                        ? "Shipment received at seller hub and inventory updated."
                        : action.Notes;

                    var ok = UpdateStatus(connection, transaction, requestId, "ReceivedBySeller", note, expectedByRole: "Seller", expectedCurrentStatus: "OnTheWayToSeller");
                    if (!ok)
                    {
                        transaction.Rollback();
                        return false;
                    }

                    transaction.Commit();
                }
            }

            NotificationRepository.Add("Distributor", "Seller confirmed receipt", $"Seller confirmed request #{requestId} received.", "Fulfillment", requestId);
            return true;
        }

        public static bool MarkDistributorReceivedFromManufacturer(int requestId, RequestActionDto action)
        {
            var request = GetById(requestId);
            if (request == null
                || !string.Equals(request.RequestedByRole, "Distributor", StringComparison.OrdinalIgnoreCase)
                || !string.Equals(request.Status, "OnTheWayToDistributor", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    AddOrIncreaseInventory(connection, transaction, "Distributor", request.Sku, request.BlanketName, request.Quantity, "Distributor Hub");
                    var note = string.IsNullOrWhiteSpace(action.Notes)
                        ? "Shipment received at distributor hub and inventory updated."
                        : action.Notes;

                    var ok = UpdateStatus(connection, transaction, requestId, "ReceivedByDistributor", note, expectedByRole: "Distributor", expectedCurrentStatus: "OnTheWayToDistributor");
                    if (!ok)
                    {
                        transaction.Rollback();
                        return false;
                    }

                    transaction.Commit();
                }
            }

            NotificationRepository.Add("Manufacturer", "Distributor confirmed receipt", $"Distributor confirmed request #{requestId} received.", "Fulfillment", requestId);
            return true;
        }

        public static bool CancelBySeller(int requestId, RequestActionDto action)
        {
            var ok = UpdateStatus(requestId, "CancelledBySeller", action.Notes, expectedByRole: "Seller");
            if (ok)
            {
                NotificationRepository.Add("Distributor", "Seller cancelled request", $"Seller cancelled request #{requestId}.", "Cancellation", requestId);
            }
            return ok;
        }

        public static bool CancelByDistributor(int requestId, RequestActionDto action)
        {
            var ok = UpdateStatus(requestId, "CancelledByDistributor", action.Notes, expectedToRole: "Distributor");
            if (ok)
            {
                NotificationRepository.Add("Seller", "Distributor cancelled request", $"Distributor cancelled request #{requestId}.", "Cancellation", requestId);
            }
            return ok;
        }

        public static bool CancelByManufacturer(int requestId, RequestActionDto action)
        {
            var ok = UpdateStatus(requestId, "CancelledByManufacturer", action.Notes, expectedToRole: "Manufacturer");
            if (ok)
            {
                NotificationRepository.Add("Distributor", "Manufacturer cancelled request", $"Manufacturer cancelled request #{requestId}.", "Cancellation", requestId);
            }
            return ok;
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

        private static bool TryDecreaseInventory(SqlConnection connection, SqlTransaction transaction, string roleName, string sku, int quantity)
        {
            const string sql = @"
UPDATE dbo.InventoryItems
SET Quantity = Quantity - @Quantity,
    LastUpdated = @Now
WHERE RoleName = @RoleName
  AND Sku = @Sku
  AND Quantity >= @Quantity";

            using (var command = new SqlCommand(sql, connection, transaction))
            {
                command.Parameters.AddWithValue("@Quantity", quantity);
                command.Parameters.AddWithValue("@Now", DateTime.Now);
                command.Parameters.AddWithValue("@RoleName", roleName);
                command.Parameters.AddWithValue("@Sku", sku);
                return command.ExecuteNonQuery() > 0;
            }
        }

        private static void AddOrIncreaseInventory(SqlConnection connection, SqlTransaction transaction, string roleName, string sku, string name, int quantity, string location)
        {
            const string sql = @"
IF EXISTS(SELECT 1 FROM dbo.InventoryItems WHERE RoleName = @RoleName AND Sku = @Sku)
BEGIN
    UPDATE dbo.InventoryItems
    SET Quantity = Quantity + @Quantity,
        LastUpdated = @Now,
        [Location] = COALESCE(NULLIF([Location], ''), @Location)
    WHERE RoleName = @RoleName AND Sku = @Sku;
END
ELSE
BEGIN
    INSERT INTO dbo.InventoryItems(RoleName, Sku, [Name], Quantity, [Location], LastUpdated)
    VALUES(@RoleName, @Sku, @Name, @Quantity, @Location, @Now);
END";

            using (var command = new SqlCommand(sql, connection, transaction))
            {
                command.Parameters.AddWithValue("@RoleName", roleName);
                command.Parameters.AddWithValue("@Sku", sku);
                command.Parameters.AddWithValue("@Name", name);
                command.Parameters.AddWithValue("@Quantity", quantity);
                command.Parameters.AddWithValue("@Location", location);
                command.Parameters.AddWithValue("@Now", DateTime.Now);
                command.ExecuteNonQuery();
            }
        }

        private static bool UpdateStatus(SqlConnection connection, SqlTransaction transaction, int requestId, string status, string notes, string expectedByRole = null, string expectedToRole = null, string expectedCurrentStatus = null)
        {
            var sql = @"
UPDATE dbo.OrderRequests
SET [Status] = @Status,
    Notes = COALESCE(@Notes, Notes),
    UpdatedAt = @UpdatedAt
WHERE Id = @Id";

            if (!string.IsNullOrWhiteSpace(expectedByRole))
            {
                sql += " AND RequestedByRole = @RequestedByRole";
            }

            if (!string.IsNullOrWhiteSpace(expectedToRole))
            {
                sql += " AND RequestedToRole = @RequestedToRole";
            }

            if (!string.IsNullOrWhiteSpace(expectedCurrentStatus))
            {
                sql += " AND [Status] = @ExpectedCurrentStatus";
            }

            using (var command = new SqlCommand(sql, connection, transaction))
            {
                command.Parameters.AddWithValue("@Status", status);
                command.Parameters.AddWithValue("@Notes", (object)notes ?? DBNull.Value);
                command.Parameters.AddWithValue("@UpdatedAt", DateTime.Now);
                command.Parameters.AddWithValue("@Id", requestId);

                if (!string.IsNullOrWhiteSpace(expectedByRole))
                {
                    command.Parameters.AddWithValue("@RequestedByRole", expectedByRole);
                }

                if (!string.IsNullOrWhiteSpace(expectedToRole))
                {
                    command.Parameters.AddWithValue("@RequestedToRole", expectedToRole);
                }

                if (!string.IsNullOrWhiteSpace(expectedCurrentStatus))
                {
                    command.Parameters.AddWithValue("@ExpectedCurrentStatus", expectedCurrentStatus);
                }

                return command.ExecuteNonQuery() > 0;
            }
        }

        private static bool UpdateStatus(int requestId, string status, string notes, string expectedByRole = null, string expectedToRole = null, string expectedCurrentStatus = null)
        {
            var sql = @"
UPDATE dbo.OrderRequests
SET [Status] = @Status,
    Notes = COALESCE(@Notes, Notes),
    UpdatedAt = @UpdatedAt
WHERE Id = @Id";

            if (!string.IsNullOrWhiteSpace(expectedByRole))
            {
                sql += " AND RequestedByRole = @RequestedByRole";
            }

            if (!string.IsNullOrWhiteSpace(expectedToRole))
            {
                sql += " AND RequestedToRole = @RequestedToRole";
            }

            if (!string.IsNullOrWhiteSpace(expectedCurrentStatus))
            {
                sql += " AND [Status] = @ExpectedCurrentStatus";
            }

            using (var connection = new SqlConnection(ConnectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@Status", status);
                command.Parameters.AddWithValue("@Notes", (object)notes ?? DBNull.Value);
                command.Parameters.AddWithValue("@UpdatedAt", DateTime.Now);
                command.Parameters.AddWithValue("@Id", requestId);

                if (!string.IsNullOrWhiteSpace(expectedByRole))
                {
                    command.Parameters.AddWithValue("@RequestedByRole", expectedByRole);
                }

                if (!string.IsNullOrWhiteSpace(expectedToRole))
                {
                    command.Parameters.AddWithValue("@RequestedToRole", expectedToRole);
                }

                if (!string.IsNullOrWhiteSpace(expectedCurrentStatus))
                {
                    command.Parameters.AddWithValue("@ExpectedCurrentStatus", expectedCurrentStatus);
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
