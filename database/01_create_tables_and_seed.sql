IF DB_ID(N'CozyComfortDb') IS NULL
BEGIN
    CREATE DATABASE CozyComfortDb;
END;
GO

USE CozyComfortDb;
GO

IF OBJECT_ID(N'dbo.Roles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Roles(
        Id INT IDENTITY(1,1) PRIMARY KEY,
        RoleName NVARCHAR(50) NOT NULL UNIQUE
    );
END;
GO

IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Users(
        Id INT IDENTITY(1,1) PRIMARY KEY,
        UserName NVARCHAR(100) NOT NULL UNIQUE,
        [Password] NVARCHAR(100) NOT NULL,
        RoleId INT NOT NULL,
        DistributorUserId INT NULL,
        CONSTRAINT FK_Users_Roles FOREIGN KEY(RoleId) REFERENCES dbo.Roles(Id),
        CONSTRAINT FK_Users_Distributor FOREIGN KEY(DistributorUserId) REFERENCES dbo.Users(Id)
    );
END;
GO

IF COL_LENGTH('dbo.Users', 'DistributorUserId') IS NULL
BEGIN
    ALTER TABLE dbo.Users ADD DistributorUserId INT NULL;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Users_Distributor')
BEGIN
    ALTER TABLE dbo.Users WITH CHECK ADD CONSTRAINT FK_Users_Distributor FOREIGN KEY(DistributorUserId) REFERENCES dbo.Users(Id);
END;
GO

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
GO

IF COL_LENGTH('dbo.InventoryItems', 'OwnerUserName') IS NULL
BEGIN
    ALTER TABLE dbo.InventoryItems ADD OwnerUserName NVARCHAR(100) NULL;
END;
GO

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
GO

IF COL_LENGTH('dbo.OrderRequests', 'RequestedToUser') IS NULL
BEGIN
    ALTER TABLE dbo.OrderRequests ADD RequestedToUser NVARCHAR(100) NULL;
END;
GO

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
GO

IF NOT EXISTS(SELECT 1 FROM dbo.Roles WHERE RoleName='Manufacturer') INSERT INTO dbo.Roles(RoleName) VALUES('Manufacturer');
IF NOT EXISTS(SELECT 1 FROM dbo.Roles WHERE RoleName='Distributor') INSERT INTO dbo.Roles(RoleName) VALUES('Distributor');
IF NOT EXISTS(SELECT 1 FROM dbo.Roles WHERE RoleName='Seller') INSERT INTO dbo.Roles(RoleName) VALUES('Seller');
GO

IF NOT EXISTS(SELECT 1 FROM dbo.Users WHERE UserName='m_admin')
    INSERT INTO dbo.Users(UserName, [Password], RoleId) SELECT 'm_admin', 'M@123', Id FROM dbo.Roles WHERE RoleName='Manufacturer';
IF NOT EXISTS(SELECT 1 FROM dbo.Users WHERE UserName='d_admin')
    INSERT INTO dbo.Users(UserName, [Password], RoleId) SELECT 'd_admin', 'D@123', Id FROM dbo.Roles WHERE RoleName='Distributor';
IF NOT EXISTS(SELECT 1 FROM dbo.Users WHERE UserName='s_admin')
    INSERT INTO dbo.Users(UserName, [Password], RoleId) SELECT 's_admin', 'S@123', Id FROM dbo.Roles WHERE RoleName='Seller';
GO

UPDATE s
SET s.DistributorUserId = d.Id
FROM dbo.Users s
JOIN dbo.Users d ON d.UserName = 'd_admin'
JOIN dbo.Roles sr ON sr.Id = s.RoleId
WHERE sr.RoleName = 'Seller'
  AND s.UserName = 's_admin'
  AND s.DistributorUserId IS NULL;
GO

IF NOT EXISTS(SELECT 1 FROM dbo.InventoryItems)
BEGIN
    INSERT INTO dbo.InventoryItems(RoleName, OwnerUserName, Sku, [Name], Quantity, [Location], LastUpdated) VALUES
    ('Manufacturer', NULL, 'CC-WOOL-QUEEN', 'Wool Queen Blanket', 5420, 'Factory A', GETDATE()),
    ('Manufacturer', NULL, 'CC-COTTON-KING', 'Cotton King Blanket', 2210, 'Factory B', GETDATE()),
    ('Distributor', 'd_admin', 'CC-WOOL-QUEEN', 'Wool Queen Blanket', 640, 'Central Warehouse', GETDATE()),
    ('Distributor', 'd_admin', 'CC-FLEECE-SINGLE', 'Fleece Single Blanket', 190, 'North Hub', GETDATE()),
    ('Seller', NULL, 'CC-COTTON-KING', 'Cotton King Blanket', 24, 'Store A-12', GETDATE()),
    ('Seller', NULL, 'CC-FLEECE-SINGLE', 'Fleece Single Blanket', 16, 'Store A-12', GETDATE());
END;
GO

UPDATE dbo.InventoryItems
SET OwnerUserName = 'd_admin'
WHERE RoleName = 'Distributor' AND OwnerUserName IS NULL;
GO

IF NOT EXISTS(SELECT 1 FROM dbo.OrderRequests)
BEGIN
    INSERT INTO dbo.OrderRequests
    (RequestType, RequestedByRole, RequestedToRole, RequestedByUser, RequestedToUser, Sku, BlanketName, Quantity, [Status], Notes, CreatedAt, UpdatedAt, SourceRequestId)
    VALUES
    ('SellerToDistributor', 'Seller', 'Distributor', 's_admin', 'd_admin', 'CC-COTTON-KING', 'Cotton King Blanket', 40, 'PendingDistributorReview', 'Need stock for weekend promo.', GETDATE(), GETDATE(), NULL);
END;
GO

UPDATE dbo.OrderRequests
SET RequestedToUser = 'd_admin'
WHERE RequestedToRole = 'Distributor' AND RequestedToUser IS NULL;
GO


IF NOT EXISTS(SELECT 1 FROM dbo.Notifications)
BEGIN
    INSERT INTO dbo.Notifications(RecipientRole, Title, Message, NotificationType, IsRead, RelatedRequestId, CreatedAt)
    VALUES
    ('Distributor', 'New seller request', 'Seller s_admin requested replenishment for CC-COTTON-KING.', 'OrderRequest', 0, 1, GETDATE()),
    ('Manufacturer', 'Escalation alert', 'Distributor escalated a blanket request to manufacturer.', 'Escalation', 0, 1, GETDATE());
END;
GO
