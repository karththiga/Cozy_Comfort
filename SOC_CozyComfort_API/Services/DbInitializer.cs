using System;
using System.Configuration;
using System.Data.SqlClient;

namespace SOC_CozyComfort_API.Services
{
    public static class DbInitializer
    {
        public static void EnsureCreated()
        {
            var appConnectionString = ConfigurationManager.ConnectionStrings["CozyComfortDb"]?.ConnectionString;
            if (string.IsNullOrWhiteSpace(appConnectionString))
            {
                throw new InvalidOperationException("Missing connection string: CozyComfortDb");
            }

            var builder = new SqlConnectionStringBuilder(appConnectionString);
            var databaseName = builder.InitialCatalog;

            var masterBuilder = new SqlConnectionStringBuilder(appConnectionString)
            {
                InitialCatalog = "master"
            };

            using (var masterConnection = new SqlConnection(masterBuilder.ConnectionString))
            {
                masterConnection.Open();
                using (var cmd = new SqlCommand($"IF DB_ID(N'{databaseName}') IS NULL CREATE DATABASE [{databaseName}];", masterConnection))
                {
                    cmd.ExecuteNonQuery();
                }
            }

            using (var dbConnection = new SqlConnection(appConnectionString))
            {
                dbConnection.Open();

                var createScript = @"
IF OBJECT_ID(N'dbo.Roles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Roles(
        Id INT IDENTITY(1,1) PRIMARY KEY,
        RoleName NVARCHAR(50) NOT NULL UNIQUE
    );
END;

IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Users(
        Id INT IDENTITY(1,1) PRIMARY KEY,
        UserName NVARCHAR(100) NOT NULL UNIQUE,
        [Password] NVARCHAR(100) NOT NULL,
        RoleId INT NOT NULL,
        DistributorUserId INT NULL,
        FullName NVARCHAR(150) NULL,
        Email NVARCHAR(200) NULL,
        CONSTRAINT FK_Users_Roles FOREIGN KEY(RoleId) REFERENCES dbo.Roles(Id),
        CONSTRAINT FK_Users_Distributor FOREIGN KEY(DistributorUserId) REFERENCES dbo.Users(Id)
    );
END;

IF COL_LENGTH('dbo.Users', 'DistributorUserId') IS NULL
BEGIN
    ALTER TABLE dbo.Users ADD DistributorUserId INT NULL;
END;

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Users_Distributor')
BEGIN
    ALTER TABLE dbo.Users WITH CHECK ADD CONSTRAINT FK_Users_Distributor FOREIGN KEY(DistributorUserId) REFERENCES dbo.Users(Id);
END;

IF COL_LENGTH('dbo.Users', 'FullName') IS NULL
BEGIN
    ALTER TABLE dbo.Users ADD FullName NVARCHAR(150) NULL;
END;

IF COL_LENGTH('dbo.Users', 'Email') IS NULL
BEGIN
    ALTER TABLE dbo.Users ADD Email NVARCHAR(200) NULL;
END;

IF COL_LENGTH('dbo.Users', 'IsApproved') IS NULL
BEGIN
    ALTER TABLE dbo.Users ADD IsApproved BIT NOT NULL CONSTRAINT DF_Users_IsApproved DEFAULT(0);
END;

IF COL_LENGTH('dbo.Users', 'ApprovedBy') IS NULL
BEGIN
    ALTER TABLE dbo.Users ADD ApprovedBy NVARCHAR(100) NULL;
END;

IF COL_LENGTH('dbo.Users', 'ApprovedAt') IS NULL
BEGIN
    ALTER TABLE dbo.Users ADD ApprovedAt DATETIME NULL;
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Users_Email' AND object_id = OBJECT_ID('dbo.Users'))
BEGIN
    CREATE UNIQUE INDEX UX_Users_Email ON dbo.Users(Email) WHERE Email IS NOT NULL;
END;

IF OBJECT_ID(N'dbo.InventoryItems', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.InventoryItems(
        Id INT IDENTITY(1,1) PRIMARY KEY,
        RoleName NVARCHAR(50) NOT NULL,
        OwnerUserName NVARCHAR(100) NULL,
        Sku NVARCHAR(100) NOT NULL,
        [Name] NVARCHAR(200) NOT NULL,
        Quantity INT NOT NULL,
        [Location] NVARCHAR(200) NULL,
        LastUpdated DATETIME NOT NULL
    );
END;

IF COL_LENGTH('dbo.InventoryItems', 'OwnerUserName') IS NULL
BEGIN
    ALTER TABLE dbo.InventoryItems ADD OwnerUserName NVARCHAR(100) NULL;
END;


IF OBJECT_ID(N'dbo.OrderRequests', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.OrderRequests(
        Id INT IDENTITY(1,1) PRIMARY KEY,
        RequestType NVARCHAR(60) NOT NULL,
        RequestedByRole NVARCHAR(50) NOT NULL,
        RequestedToRole NVARCHAR(50) NOT NULL,
        RequestedByUser NVARCHAR(100) NOT NULL,
        RequestedToUser NVARCHAR(100) NULL,
        Sku NVARCHAR(100) NOT NULL,
        BlanketName NVARCHAR(200) NOT NULL,
        Quantity INT NOT NULL,
        [Status] NVARCHAR(80) NOT NULL,
        Notes NVARCHAR(500) NULL,
        CreatedAt DATETIME NOT NULL,
        UpdatedAt DATETIME NOT NULL,
        SourceRequestId INT NULL
    );
END;

IF COL_LENGTH('dbo.OrderRequests', 'RequestedToUser') IS NULL
BEGIN
    ALTER TABLE dbo.OrderRequests ADD RequestedToUser NVARCHAR(100) NULL;
END;


IF OBJECT_ID(N'dbo.Notifications', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Notifications(
        Id INT IDENTITY(1,1) PRIMARY KEY,
        RecipientRole NVARCHAR(50) NOT NULL,
        Title NVARCHAR(200) NOT NULL,
        Message NVARCHAR(1000) NOT NULL,
        NotificationType NVARCHAR(80) NOT NULL,
        IsRead BIT NOT NULL,
        RelatedRequestId INT NULL,
        CreatedAt DATETIME NOT NULL
    );
END;
";

                using (var cmd = new SqlCommand(createScript, dbConnection))
                {
                    cmd.ExecuteNonQuery();
                }

                // NOTE: Users email/full-name migration intentionally skipped at startup to keep initializer safe on legacy schemas.

                SeedData(dbConnection);
            }
        }

        private static void SeedData(SqlConnection connection)
        {
            var seedScript = @"
IF NOT EXISTS(SELECT 1 FROM dbo.Roles WHERE RoleName='Manufacturer') INSERT INTO dbo.Roles(RoleName) VALUES('Manufacturer');
IF NOT EXISTS(SELECT 1 FROM dbo.Roles WHERE RoleName='Distributor') INSERT INTO dbo.Roles(RoleName) VALUES('Distributor');
IF NOT EXISTS(SELECT 1 FROM dbo.Roles WHERE RoleName='Seller') INSERT INTO dbo.Roles(RoleName) VALUES('Seller');
IF NOT EXISTS(SELECT 1 FROM dbo.Roles WHERE RoleName='Customer') INSERT INTO dbo.Roles(RoleName) VALUES('Customer');
IF NOT EXISTS(SELECT 1 FROM dbo.Roles WHERE RoleName='Admin') INSERT INTO dbo.Roles(RoleName) VALUES('Admin');

IF NOT EXISTS(SELECT 1 FROM dbo.Users WHERE UserName='m_admin')
    INSERT INTO dbo.Users(UserName, [Password], RoleId, IsApproved, ApprovedBy, ApprovedAt)
    SELECT 'm_admin', 'M@123', Id, 1, 'system', GETDATE() FROM dbo.Roles WHERE RoleName='Manufacturer';

IF NOT EXISTS(SELECT 1 FROM dbo.Users WHERE UserName='d_admin')
    INSERT INTO dbo.Users(UserName, [Password], RoleId, IsApproved, ApprovedBy, ApprovedAt)
    SELECT 'd_admin', 'D@123', Id, 1, 'system', GETDATE() FROM dbo.Roles WHERE RoleName='Distributor';

IF NOT EXISTS(SELECT 1 FROM dbo.Users WHERE UserName='s_admin')
    INSERT INTO dbo.Users(UserName, [Password], RoleId, IsApproved, ApprovedBy, ApprovedAt)
    SELECT 's_admin', 'S@123', Id, 1, 'system', GETDATE() FROM dbo.Roles WHERE RoleName='Seller';

IF NOT EXISTS(SELECT 1 FROM dbo.Users WHERE UserName='admin')
    INSERT INTO dbo.Users(UserName, [Password], RoleId, IsApproved, ApprovedBy, ApprovedAt)
    SELECT 'admin', 'Admin@123', Id, 1, 'system', GETDATE() FROM dbo.Roles WHERE RoleName='Admin';

IF NOT EXISTS(SELECT 1 FROM dbo.Users WHERE UserName='c_customer')
    INSERT INTO dbo.Users(UserName, [Password], RoleId, IsApproved, ApprovedBy, ApprovedAt)
    SELECT 'c_customer', 'C@123', Id, 1, 'system', GETDATE() FROM dbo.Roles WHERE RoleName='Customer';

UPDATE dbo.Users SET IsApproved = 1 WHERE UserName IN ('m_admin', 'd_admin', 's_admin', 'admin', 'c_customer');
UPDATE s
SET s.DistributorUserId = d.Id
FROM dbo.Users s
JOIN dbo.Users d ON d.UserName = 'd_admin'
JOIN dbo.Roles sr ON sr.Id = s.RoleId
WHERE sr.RoleName = 'Seller'
  AND s.UserName = 's_admin'
  AND s.DistributorUserId IS NULL;

IF NOT EXISTS(SELECT 1 FROM dbo.InventoryItems)
BEGIN
    INSERT INTO dbo.InventoryItems(RoleName, OwnerUserName, Sku, [Name], Quantity, [Location], LastUpdated) VALUES
    ('Manufacturer', NULL, 'CC-WOOL-QUEEN', 'Wool Queen Blanket', 5420, 'Main Manufacturing Facility', GETDATE()),
    ('Manufacturer', NULL, 'CC-COTTON-KING', 'Cotton King Blanket', 2210, 'Main Manufacturing Facility', GETDATE()),
    ('Distributor', 'd_admin', 'CC-WOOL-QUEEN', 'Wool Queen Blanket', 640, 'Central Warehouse', GETDATE()),
    ('Distributor', 'd_admin', 'CC-FLEECE-SINGLE', 'Fleece Single Blanket', 190, 'North Hub', GETDATE()),
    ('Seller', NULL, 'CC-COTTON-KING', 'Cotton King Blanket', 24, 'Store A-12', GETDATE()),
    ('Seller', NULL, 'CC-FLEECE-SINGLE', 'Fleece Single Blanket', 16, 'Store A-12', GETDATE());
END;

UPDATE dbo.InventoryItems
SET [Location] = 'Main Manufacturing Facility'
WHERE RoleName = 'Manufacturer';

UPDATE dbo.InventoryItems
SET OwnerUserName = 'd_admin'
WHERE RoleName = 'Distributor' AND OwnerUserName IS NULL;


IF NOT EXISTS(SELECT 1 FROM dbo.OrderRequests)
BEGIN
    INSERT INTO dbo.OrderRequests
    (RequestType, RequestedByRole, RequestedToRole, RequestedByUser, RequestedToUser, Sku, BlanketName, Quantity, [Status], Notes, CreatedAt, UpdatedAt, SourceRequestId)
    VALUES
    ('SellerToDistributor', 'Seller', 'Distributor', 's_admin', 'd_admin', 'CC-COTTON-KING', 'Cotton King Blanket', 40, 'PendingDistributorReview', 'Need stock for weekend promo.', GETDATE(), GETDATE(), NULL);
END;

UPDATE dbo.OrderRequests
SET RequestedToUser = 'd_admin'
WHERE RequestedToRole = 'Distributor' AND RequestedToUser IS NULL;


IF NOT EXISTS(SELECT 1 FROM dbo.Notifications)
BEGIN
    INSERT INTO dbo.Notifications(RecipientRole, Title, Message, NotificationType, IsRead, RelatedRequestId, CreatedAt)
    VALUES
    ('Distributor', 'New seller request', 'Seller s_admin requested replenishment for CC-COTTON-KING.', 'OrderRequest', 0, 1, GETDATE()),
    ('Manufacturer', 'Escalation alert', 'Distributor escalated a blanket request to manufacturer.', 'Escalation', 0, 1, GETDATE());
END;
";

            using (var cmd = new SqlCommand(seedScript, connection))
            {
                cmd.ExecuteNonQuery();
            }
        }
    }
}
