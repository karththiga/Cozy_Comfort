-- Cozy Comfort Local SQL Server setup
-- Target DB: CozyComfortDb

IF DB_ID(N'CozyComfortDb') IS NULL
    CREATE DATABASE CozyComfortDb;
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
        CONSTRAINT FK_Users_Roles FOREIGN KEY(RoleId) REFERENCES dbo.Roles(Id)
    );
END;
GO

IF OBJECT_ID(N'dbo.InventoryItems', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.InventoryItems(
        Id INT IDENTITY(1,1) PRIMARY KEY,
        RoleName NVARCHAR(50) NOT NULL,
        Sku NVARCHAR(100) NOT NULL,
        [Name] NVARCHAR(200) NOT NULL,
        Quantity INT NOT NULL,
        [Location] NVARCHAR(200) NULL,
        LastUpdated DATETIME NOT NULL
    );
END;
GO

IF NOT EXISTS(SELECT 1 FROM dbo.Roles WHERE RoleName='Manufacturer') INSERT INTO dbo.Roles(RoleName) VALUES('Manufacturer');
IF NOT EXISTS(SELECT 1 FROM dbo.Roles WHERE RoleName='Distributor') INSERT INTO dbo.Roles(RoleName) VALUES('Distributor');
IF NOT EXISTS(SELECT 1 FROM dbo.Roles WHERE RoleName='Seller') INSERT INTO dbo.Roles(RoleName) VALUES('Seller');
GO

IF NOT EXISTS(SELECT 1 FROM dbo.Users WHERE UserName='m_admin')
    INSERT INTO dbo.Users(UserName, [Password], RoleId)
    SELECT 'm_admin', 'M@123', Id FROM dbo.Roles WHERE RoleName='Manufacturer';

IF NOT EXISTS(SELECT 1 FROM dbo.Users WHERE UserName='d_admin')
    INSERT INTO dbo.Users(UserName, [Password], RoleId)
    SELECT 'd_admin', 'D@123', Id FROM dbo.Roles WHERE RoleName='Distributor';

IF NOT EXISTS(SELECT 1 FROM dbo.Users WHERE UserName='s_admin')
    INSERT INTO dbo.Users(UserName, [Password], RoleId)
    SELECT 's_admin', 'S@123', Id FROM dbo.Roles WHERE RoleName='Seller';
GO

IF NOT EXISTS(SELECT 1 FROM dbo.InventoryItems)
BEGIN
    INSERT INTO dbo.InventoryItems(RoleName, Sku, [Name], Quantity, [Location], LastUpdated) VALUES
    ('Manufacturer', 'CC-WOOL-QUEEN', 'Wool Queen Blanket', 5420, 'Factory A', GETDATE()),
    ('Manufacturer', 'CC-COTTON-KING', 'Cotton King Blanket', 2210, 'Factory B', GETDATE()),
    ('Distributor', 'CC-WOOL-QUEEN', 'Wool Queen Blanket', 640, 'Central Warehouse', GETDATE()),
    ('Distributor', 'CC-FLEECE-SINGLE', 'Fleece Single Blanket', 190, 'North Hub', GETDATE()),
    ('Seller', 'CC-COTTON-KING', 'Cotton King Blanket', 24, 'Store A-12', GETDATE()),
    ('Seller', 'CC-FLEECE-SINGLE', 'Fleece Single Blanket', 16, 'Store A-12', GETDATE());
END;
GO
