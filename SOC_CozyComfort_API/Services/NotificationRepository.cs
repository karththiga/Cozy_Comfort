using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using SOC_CozyComfort_API.Models;

namespace SOC_CozyComfort_API.Services
{
    public static class NotificationRepository
    {
        private static string ConnectionString => ConfigurationManager.ConnectionStrings["CozyComfortDb"].ConnectionString;

        public static List<NotificationDto> GetByRoleAndUser(string role, string userName)
        {
            var result = new List<NotificationDto>();
            const string sql = @"
SELECT Id, RecipientRole, RecipientUserName, Title, Message, NotificationType, IsRead, RelatedRequestId, CreatedAt
FROM dbo.Notifications
WHERE RecipientRole = @RecipientRole
  AND RecipientUserName = @RecipientUserName
ORDER BY CreatedAt DESC";

            using (var connection = new SqlConnection(ConnectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@RecipientRole", role);
                command.Parameters.AddWithValue("@RecipientUserName", userName);
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

        public static bool MarkAsRead(int id, string role, string userName)
        {
            const string sql = @"
UPDATE dbo.Notifications
SET IsRead = 1
WHERE Id = @Id
  AND RecipientRole = @RecipientRole
  AND RecipientUserName = @RecipientUserName";

            using (var connection = new SqlConnection(ConnectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@Id", id);
                command.Parameters.AddWithValue("@RecipientRole", role);
                command.Parameters.AddWithValue("@RecipientUserName", userName);
                connection.Open();
                return command.ExecuteNonQuery() > 0;
            }
        }

        public static void Add(string recipientRole, string recipientUserName, string title, string message, string notificationType, int? relatedRequestId = null)
        {
            const string sql = @"
INSERT INTO dbo.Notifications
(RecipientRole, RecipientUserName, Title, Message, NotificationType, IsRead, RelatedRequestId, CreatedAt)
VALUES
(@RecipientRole, @RecipientUserName, @Title, @Message, @NotificationType, 0, @RelatedRequestId, @CreatedAt);";

            using (var connection = new SqlConnection(ConnectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@RecipientRole", recipientRole);
                command.Parameters.AddWithValue("@RecipientUserName", (object)recipientUserName ?? DBNull.Value);
                command.Parameters.AddWithValue("@Title", title);
                command.Parameters.AddWithValue("@Message", message);
                command.Parameters.AddWithValue("@NotificationType", notificationType);
                command.Parameters.Add("@RelatedRequestId", SqlDbType.Int).Value = (object)relatedRequestId ?? DBNull.Value;
                command.Parameters.AddWithValue("@CreatedAt", DateTime.Now);

                connection.Open();
                command.ExecuteNonQuery();
            }
        }

        private static NotificationDto Map(SqlDataReader reader)
        {
            return new NotificationDto
            {
                Id = Convert.ToInt32(reader["Id"]),
                RecipientRole = Convert.ToString(reader["RecipientRole"]),
                RecipientUserName = reader["RecipientUserName"] == DBNull.Value ? null : Convert.ToString(reader["RecipientUserName"]),
                Title = Convert.ToString(reader["Title"]),
                Message = Convert.ToString(reader["Message"]),
                NotificationType = Convert.ToString(reader["NotificationType"]),
                IsRead = Convert.ToBoolean(reader["IsRead"]),
                RelatedRequestId = reader["RelatedRequestId"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["RelatedRequestId"]),
                CreatedAt = Convert.ToDateTime(reader["CreatedAt"])
            };
        }
    }
}
