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

        public static List<OrderRequestDto> GetIncoming(string role, string userName = null)
        {
            return GetBySql(@"
SELECT *
FROM dbo.OrderRequests
WHERE RequestedToRole = @Role
  AND (@UserName IS NULL OR RequestedToUser = @UserName)
ORDER BY CreatedAt DESC", role, userName);
        }

        public static List<OrderRequestDto> GetOutgoing(string role, string userName = null)
        {
            return GetBySql(@"
SELECT *
FROM dbo.OrderRequests
WHERE RequestedByRole = @Role
  AND (@UserName IS NULL OR RequestedByUser = @UserName)
ORDER BY CreatedAt DESC", role, userName);
        }

        public static OrderRequestDto CreateCustomerToSeller(CreateCustomerOrderDto request)
        {
            var dto = new OrderRequestDto
            {
                RequestType = "CustomerToSeller",
                RequestedByRole = "Customer",
                RequestedToRole = "Seller",
                RequestedByUser = request.RequestedByUser,
                RequestedToUser = null,
                Sku = request.Sku,
                BlanketName = request.BlanketName,
                Quantity = request.Quantity,
                Status = "PendingSellerConfirmation",
                Notes = request.Notes
            };

            var created = Insert(dto);
            NotificationRepository.Add("Seller", "New customer order", $"Customer {request.RequestedByUser} ordered {request.Quantity} x {request.BlanketName} ({request.Sku}).", "CustomerOrder", created.Id);
            return created;
        }

        public static OrderRequestDto CreateSellerToDistributor(CreateSellerRequestDto request)
        {
            var distributorUserName = AuthService.GetAssignedDistributorUserName(request.RequestedByUser);
            if (string.IsNullOrWhiteSpace(distributorUserName))
            {
                throw new InvalidOperationException("Seller is not assigned to a valid distributor.");
            }

            var dto = new OrderRequestDto
            {
                RequestType = "SellerToDistributor",
                RequestedByRole = "Seller",
                RequestedToRole = "Distributor",
                RequestedByUser = request.RequestedByUser,
                RequestedToUser = distributorUserName,
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
                RequestedToUser = null,
                Sku = source.Sku,
                BlanketName = source.BlanketName,
                Quantity = source.Quantity,
                Status = "PendingManufacturerReview",
                Notes = string.IsNullOrWhiteSpace(action.Notes) ? "Escalated by distributor due to low stock." : action.Notes,
                SourceRequestId = source.Id
            });

            UpdateStatus(source.Id, "EscalatedToManufacturer", action.Notes, expectedToRole: "Distributor", expectedToUser: action.PerformedByUser);
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

            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    if (!TryDecreaseInventory(connection, transaction, "Distributor", request.Sku, request.Quantity, request.RequestedToUser))
                    {
                        transaction.Rollback();
                        return false;
                    }

                    AddOrIncreaseInventory(connection, transaction, "Seller", request.Sku, request.BlanketName, request.Quantity, "Seller Hub");

                    var note = string.IsNullOrWhiteSpace(action.Notes)
                        ? "Fulfilled and moved to seller inventory from distributor stock."
                        : action.Notes;

                    var ok = UpdateStatus(connection, transaction, requestId, "FulfilledByDistributor", note, expectedToRole: "Distributor", expectedToUser: action.PerformedByUser);
                    if (!ok)
                    {
                        transaction.Rollback();
                        return false;
                    }

                    transaction.Commit();
                }
            }

            NotificationRepository.Add("Seller", "Distributor fulfilled request", $"Request #{requestId} was fulfilled by distributor and added to seller inventory.", "Fulfillment", requestId);
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

            var distributorUserName = ResolveDistributorUserNameForManufacturerRequest(request);
            if (string.IsNullOrWhiteSpace(distributorUserName))
            {
                return false;
            }

            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    AddOrIncreaseInventory(connection, transaction, "Distributor", request.Sku, request.BlanketName, request.Quantity, "Distributor Hub", distributorUserName);

                    var note = string.IsNullOrWhiteSpace(action.Notes)
                        ? "Dispatched by manufacturer and received in distributor inventory."
                        : action.Notes;

                    var ok = UpdateStatus(connection, transaction, requestId, "DispatchedByManufacturer", note, expectedToRole: "Manufacturer");
                    if (!ok)
                    {
                        transaction.Rollback();
                        return false;
                    }

                    transaction.Commit();
                }
            }

            NotificationRepository.Add("Distributor", "Manufacturer dispatched blankets", $"Request #{requestId} dispatched and added to distributor inventory.", "Dispatch", requestId);
            return true;
        }

        private static string ResolveDistributorUserNameForManufacturerRequest(OrderRequestDto manufacturerRequest)
        {
            if (!string.IsNullOrWhiteSpace(manufacturerRequest.RequestedByUser))
            {
                return manufacturerRequest.RequestedByUser;
            }

            if (!manufacturerRequest.SourceRequestId.HasValue)
            {
                return null;
            }

            var sourceRequest = GetById(manufacturerRequest.SourceRequestId.Value);
            if (sourceRequest == null)
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(sourceRequest.RequestedToUser))
            {
                return sourceRequest.RequestedToUser;
            }

            return sourceRequest.RequestedByUser;
        }

        public static bool ConfirmCustomerOrderBySeller(int requestId, RequestActionDto action)
        {
            var request = GetById(requestId);
            if (request == null || !string.Equals(request.RequestedByRole, "Customer", StringComparison.OrdinalIgnoreCase) || !string.Equals(request.RequestedToRole, "Seller", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    var hasStock = TryDecreaseInventory(connection, transaction, "Seller", request.Sku, request.Quantity, null);

                    if (hasStock)
                    {
                        var note = string.IsNullOrWhiteSpace(action.Notes)
                            ? "Order confirmed by seller and stock reserved for customer."
                            : action.Notes;

                        var confirmed = UpdateStatus(connection, transaction, requestId, "ConfirmedBySeller", note, expectedByRole: "Customer", expectedToRole: "Seller");
                        if (!confirmed)
                        {
                            transaction.Rollback();
                            return false;
                        }

                        transaction.Commit();
                        NotificationRepository.Add("Customer", "Order confirmed", $"Your order #{requestId} was confirmed by seller.", "CustomerOrder", requestId);
                        return true;
                    }

                    var sellerRequest = new OrderRequestDto
                    {
                        RequestType = "SellerToDistributor",
                        RequestedByRole = "Seller",
                        RequestedToRole = "Distributor",
                        RequestedByUser = action.PerformedByUser,
                        RequestedToUser = AuthService.GetAssignedDistributorUserName(action.PerformedByUser),
                        Sku = request.Sku,
                        BlanketName = request.BlanketName,
                        Quantity = request.Quantity,
                        Status = "PendingDistributorReview",
                        Notes = $"Auto-created from customer order #{requestId} due to seller stock shortage.",
                        SourceRequestId = requestId
                    };

                    if (string.IsNullOrWhiteSpace(sellerRequest.RequestedToUser))
                    {
                        transaction.Rollback();
                        return false;
                    }

                    var sellerRequestId = Insert(connection, transaction, sellerRequest);
                    var updated = UpdateStatus(connection, transaction, requestId, "RequestedFromDistributor", "Seller stock unavailable. Request sent to distributor.", expectedByRole: "Customer", expectedToRole: "Seller");
                    if (!updated)
                    {
                        transaction.Rollback();
                        return false;
                    }

                    transaction.Commit();
                    NotificationRepository.Add("Distributor", "Seller requested stock for customer order", $"Seller created request #{sellerRequestId} for customer order #{requestId} ({request.Sku}).", "OrderRequest", sellerRequestId);
                    NotificationRepository.Add("Customer", "Order forwarded", $"Your order #{requestId} was forwarded to distributor via seller due to stock shortage.", "CustomerOrder", requestId);
                    return true;
                }
            }
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
            var ok = UpdateStatus(requestId, "CancelledByDistributor", action.Notes, expectedToRole: "Distributor", expectedToUser: action.PerformedByUser);
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

        private static List<OrderRequestDto> GetBySql(string sql, string role, string userName = null)
        {
            var result = new List<OrderRequestDto>();
            using (var connection = new SqlConnection(ConnectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@Role", role);
                command.Parameters.AddWithValue("@UserName", (object)userName ?? DBNull.Value);
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

        private static int Insert(SqlConnection connection, SqlTransaction transaction, OrderRequestDto request)
        {
            const string sql = @"
INSERT INTO dbo.OrderRequests
(RequestType, RequestedByRole, RequestedToRole, RequestedByUser, RequestedToUser, Sku, BlanketName, Quantity, [Status], Notes, CreatedAt, UpdatedAt, SourceRequestId)
VALUES
(@RequestType, @RequestedByRole, @RequestedToRole, @RequestedByUser, @RequestedToUser, @Sku, @BlanketName, @Quantity, @Status, @Notes, @CreatedAt, @UpdatedAt, @SourceRequestId);
SELECT CAST(SCOPE_IDENTITY() AS INT);";

            var now = DateTime.Now;
            using (var command = new SqlCommand(sql, connection, transaction))
            {
                command.Parameters.AddWithValue("@RequestType", request.RequestType);
                command.Parameters.AddWithValue("@RequestedByRole", request.RequestedByRole);
                command.Parameters.AddWithValue("@RequestedToRole", request.RequestedToRole);
                command.Parameters.AddWithValue("@RequestedByUser", request.RequestedByUser);
                command.Parameters.AddWithValue("@RequestedToUser", (object)request.RequestedToUser ?? DBNull.Value);
                command.Parameters.AddWithValue("@Sku", request.Sku);
                command.Parameters.AddWithValue("@BlanketName", request.BlanketName);
                command.Parameters.AddWithValue("@Quantity", request.Quantity);
                command.Parameters.AddWithValue("@Status", request.Status);
                command.Parameters.AddWithValue("@Notes", (object)request.Notes ?? DBNull.Value);
                command.Parameters.AddWithValue("@CreatedAt", now);
                command.Parameters.AddWithValue("@UpdatedAt", now);
                command.Parameters.Add("@SourceRequestId", SqlDbType.Int).Value = (object)request.SourceRequestId ?? DBNull.Value;
                return Convert.ToInt32(command.ExecuteScalar());
            }
        }

        private static OrderRequestDto Insert(OrderRequestDto request)
        {
            const string sql = @"
INSERT INTO dbo.OrderRequests
(RequestType, RequestedByRole, RequestedToRole, RequestedByUser, RequestedToUser, Sku, BlanketName, Quantity, [Status], Notes, CreatedAt, UpdatedAt, SourceRequestId)
VALUES
(@RequestType, @RequestedByRole, @RequestedToRole, @RequestedByUser, @RequestedToUser, @Sku, @BlanketName, @Quantity, @Status, @Notes, @CreatedAt, @UpdatedAt, @SourceRequestId);
SELECT CAST(SCOPE_IDENTITY() AS INT);";

            var now = DateTime.Now;
            using (var connection = new SqlConnection(ConnectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@RequestType", request.RequestType);
                command.Parameters.AddWithValue("@RequestedByRole", request.RequestedByRole);
                command.Parameters.AddWithValue("@RequestedToRole", request.RequestedToRole);
                command.Parameters.AddWithValue("@RequestedByUser", request.RequestedByUser);
                command.Parameters.AddWithValue("@RequestedToUser", (object)request.RequestedToUser ?? DBNull.Value);
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

        private static bool TryDecreaseInventory(SqlConnection connection, SqlTransaction transaction, string roleName, string sku, int quantity, string ownerUserName = null)
        {
            const string sql = @"
UPDATE dbo.InventoryItems
SET Quantity = Quantity - @Quantity,
    LastUpdated = @Now
WHERE RoleName = @RoleName
  AND (@OwnerUserName IS NULL OR OwnerUserName = @OwnerUserName)
  AND Sku = @Sku
  AND Quantity >= @Quantity";

            using (var command = new SqlCommand(sql, connection, transaction))
            {
                command.Parameters.AddWithValue("@Quantity", quantity);
                command.Parameters.AddWithValue("@Now", DateTime.Now);
                command.Parameters.AddWithValue("@RoleName", roleName);
                command.Parameters.AddWithValue("@OwnerUserName", (object)ownerUserName ?? DBNull.Value);
                command.Parameters.AddWithValue("@Sku", sku);
                return command.ExecuteNonQuery() > 0;
            }
        }

        private static void AddOrIncreaseInventory(SqlConnection connection, SqlTransaction transaction, string roleName, string sku, string name, int quantity, string location, string ownerUserName = null)
        {
            const string sql = @"
IF EXISTS(SELECT 1 FROM dbo.InventoryItems WHERE RoleName = @RoleName AND Sku = @Sku AND (@OwnerUserName IS NULL OR OwnerUserName = @OwnerUserName))
BEGIN
    UPDATE dbo.InventoryItems
    SET Quantity = Quantity + @Quantity,
        LastUpdated = @Now,
        [Location] = COALESCE(NULLIF([Location], ''), @Location)
    WHERE RoleName = @RoleName AND Sku = @Sku AND (@OwnerUserName IS NULL OR OwnerUserName = @OwnerUserName);
END
ELSE
BEGIN
    INSERT INTO dbo.InventoryItems(RoleName, OwnerUserName, Sku, [Name], Quantity, [Location], LastUpdated)
    VALUES(@RoleName, @OwnerUserName, @Sku, @Name, @Quantity, @Location, @Now);
END";

            using (var command = new SqlCommand(sql, connection, transaction))
            {
                command.Parameters.AddWithValue("@RoleName", roleName);
                command.Parameters.AddWithValue("@OwnerUserName", (object)ownerUserName ?? DBNull.Value);
                command.Parameters.AddWithValue("@Sku", sku);
                command.Parameters.AddWithValue("@Name", name);
                command.Parameters.AddWithValue("@Quantity", quantity);
                command.Parameters.AddWithValue("@Location", location);
                command.Parameters.AddWithValue("@Now", DateTime.Now);
                command.ExecuteNonQuery();
            }
        }

        private static bool UpdateStatus(SqlConnection connection, SqlTransaction transaction, int requestId, string status, string notes, string expectedByRole = null, string expectedToRole = null, string expectedToUser = null)
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

            if (!string.IsNullOrWhiteSpace(expectedToUser))
            {
                sql += " AND RequestedToUser = @RequestedToUser";
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

                if (!string.IsNullOrWhiteSpace(expectedToUser))
                {
                    command.Parameters.AddWithValue("@RequestedToUser", expectedToUser);
                }

                return command.ExecuteNonQuery() > 0;
            }
        }

        private static bool UpdateStatus(int requestId, string status, string notes, string expectedByRole = null, string expectedToRole = null, string expectedToUser = null)
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

            if (!string.IsNullOrWhiteSpace(expectedToUser))
            {
                sql += " AND RequestedToUser = @RequestedToUser";
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

                if (!string.IsNullOrWhiteSpace(expectedToUser))
                {
                    command.Parameters.AddWithValue("@RequestedToUser", expectedToUser);
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
                RequestedToUser = reader["RequestedToUser"] == DBNull.Value ? null : Convert.ToString(reader["RequestedToUser"]),
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
