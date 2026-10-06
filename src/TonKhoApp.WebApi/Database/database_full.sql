
IF DB_ID('tonkho_db') IS NULL
BEGIN
    CREATE DATABASE tonkho_db;
END
GO

USE tonkho_db;
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
SET XACT_ABORT ON;
GO

ALTER DATABASE SCOPED CONFIGURATION SET IDENTITY_CACHE = OFF;
GO

IF OBJECT_ID('dbo.Users') IS NULL
BEGIN
    CREATE TABLE dbo.Users (
        Id           INT IDENTITY(1,1) NOT NULL,
        UserId       NVARCHAR(50)  NOT NULL,

        UserPassword VARCHAR(255)  NOT NULL,

        FullName     NVARCHAR(100) NULL,

        IsActive     BIT           NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT 1,

        LastLoginAt  DATETIME2(3)  NULL,

        CreatedAt    DATETIME2(3)  NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedBy    INT           NULL,
        UpdatedAt    DATETIME2(3)  NULL,
        UpdatedBy    INT           NULL,

        Deleted      BIT           NOT NULL CONSTRAINT DF_Users_Deleted DEFAULT 0,
        DeletedAt    DATETIME2(3)  NULL,
        DeletedBy    INT           NULL,

        CONSTRAINT PK_Users PRIMARY KEY (Id),

        CONSTRAINT CK_Users_Deleted CHECK (
            ([Deleted] = 0 AND [DeletedAt] IS NULL) OR
            ([Deleted] = 1 AND [DeletedAt] IS NOT NULL))
    );

    CREATE UNIQUE INDEX UX_Users_UserId ON dbo.Users(UserId) WHERE Deleted = 0;

END
GO

IF OBJECT_ID('dbo.Roles') IS NULL
BEGIN
    CREATE TABLE dbo.Roles (
        Id          INT IDENTITY(1,1) NOT NULL,
        Code        VARCHAR(50)   NOT NULL,
        Name        NVARCHAR(100) NOT NULL,
        Description NVARCHAR(255) NULL,
        IsSystem    BIT           NOT NULL CONSTRAINT DF_Roles_IsSystem  DEFAULT 0,
        CreatedAt   DATETIME2(3)  NOT NULL CONSTRAINT DF_Roles_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedBy   INT           NULL,
        UpdatedAt   DATETIME2(3)  NULL,
        UpdatedBy   INT           NULL,

        CONSTRAINT PK_Roles PRIMARY KEY (Id),
        CONSTRAINT FK_Roles_Users_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES dbo.Users(Id),
        CONSTRAINT FK_Roles_Users_UpdatedBy FOREIGN KEY (UpdatedBy) REFERENCES dbo.Users(Id)
    );

    CREATE UNIQUE INDEX UX_Roles_Code ON dbo.Roles(Code);

    CREATE UNIQUE INDEX UX_Roles_Name ON dbo.Roles(Name);

    CREATE INDEX IX_Roles_CreatedBy ON dbo.Roles(CreatedBy);
    CREATE INDEX IX_Roles_UpdatedBy ON dbo.Roles(UpdatedBy);

END
GO

IF OBJECT_ID('dbo.UserRoles') IS NULL
BEGIN
    CREATE TABLE dbo.UserRoles (
        Id         INT IDENTITY(1,1) NOT NULL,
        UserId     INT          NOT NULL,
        RoleId     INT          NOT NULL,

        AssignedAt DATETIME2(3) NOT NULL CONSTRAINT DF_UserRoles_AssignedAt DEFAULT SYSUTCDATETIME(),
        AssignedBy INT          NULL,

        CONSTRAINT PK_UserRoles PRIMARY KEY (Id),

        CONSTRAINT FK_UserRoles_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(Id) ON DELETE CASCADE,

        CONSTRAINT FK_UserRoles_Roles FOREIGN KEY (RoleId) REFERENCES dbo.Roles(Id),

        CONSTRAINT FK_UserRoles_Users_AssignedBy FOREIGN KEY (AssignedBy) REFERENCES dbo.Users(Id)
    );

    CREATE UNIQUE INDEX UX_UserRoles_User_Role ON dbo.UserRoles(UserId, RoleId);

    CREATE INDEX IX_UserRoles_RoleId     ON dbo.UserRoles(RoleId);
    CREATE INDEX IX_UserRoles_AssignedBy ON dbo.UserRoles(AssignedBy);

END
GO

IF OBJECT_ID('dbo.RefreshTokens') IS NULL
BEGIN
    CREATE TABLE dbo.RefreshTokens (
        Id                BIGINT IDENTITY(1,1) NOT NULL,
        UserId            INT          NOT NULL,
        TokenHash         VARCHAR(88)  NOT NULL,
        ExpiresAt         DATETIME2(3) NOT NULL,
        CreatedAt         DATETIME2(3) NOT NULL CONSTRAINT DF_RefreshTokens_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedByIp       VARCHAR(45)  NULL,
        RevokedAt         DATETIME2(3) NULL,
        RevokedReason     VARCHAR(30)  NULL,

        ReplacedByTokenId BIGINT       NULL,

        CONSTRAINT PK_RefreshTokens PRIMARY KEY (Id),
        CONSTRAINT FK_RefreshTokens_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(Id) ON DELETE CASCADE,

        CONSTRAINT FK_RefreshTokens_Replaced FOREIGN KEY (ReplacedByTokenId) REFERENCES dbo.RefreshTokens(Id),

        CONSTRAINT CK_RefreshTokens_Revoked CHECK (
            ([RevokedAt] IS NULL     AND [RevokedReason] IS NULL) OR
            ([RevokedAt] IS NOT NULL AND [RevokedReason] IS NOT NULL)),

        CONSTRAINT CK_RefreshTokens_Reason CHECK (
            [RevokedReason] IS NULL OR
            [RevokedReason] IN ('LOGOUT','ROTATED','ADMIN_LOCK','REUSE_DETECTED','PASSWORD_CHANGED')),

        CONSTRAINT CK_RefreshTokens_Expiry CHECK ([ExpiresAt] > [CreatedAt])
    );

    CREATE UNIQUE INDEX UX_RefreshTokens_TokenHash ON dbo.RefreshTokens(TokenHash);

    CREATE INDEX IX_RefreshTokens_User_Expires ON dbo.RefreshTokens(UserId, ExpiresAt);

    CREATE UNIQUE INDEX UX_RefreshTokens_ReplacedBy
        ON dbo.RefreshTokens(ReplacedByTokenId) WHERE ReplacedByTokenId IS NOT NULL;

END
GO

IF OBJECT_ID('dbo.AuditLogs') IS NULL
BEGIN
    CREATE TABLE dbo.AuditLogs (
        Id          BIGINT IDENTITY(1,1) NOT NULL,
        EntityName  VARCHAR(50)    NOT NULL,
        EntityId    VARCHAR(50)    NULL,
        [Action]    VARCHAR(30)    NOT NULL,
        BeforeJson  NVARCHAR(MAX)  NULL,
        AfterJson   NVARCHAR(MAX)  NULL,
        UserId      INT            NULL,
        UserName    NVARCHAR(50)   NULL,
        IpAddress   VARCHAR(45)    NULL,
        Description NVARCHAR(500)  NULL,
        CreatedAt   DATETIME2(3)   NOT NULL CONSTRAINT DF_AuditLogs_CreatedAt DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_AuditLogs PRIMARY KEY (Id),

        CONSTRAINT FK_AuditLogs_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(Id),

        CONSTRAINT CK_AuditLogs_Action CHECK ([Action] IN (
            'CREATE','UPDATE','DELETE','PRICE_CHANGE','LOCK','UNLOCK',
            'ROLE_GRANT','ROLE_REVOKE','PASSWORD_RESET','PASSWORD_CHANGE',
            'LOGIN','LOGIN_FAILED','LOGOUT',
            'ORDER_CONFIRM','ORDER_RECEIVE','ORDER_CANCEL',
            'STOCK_ADJUST','STOCK_ISSUE','STOCK_REVERSAL'))
    );

    CREATE INDEX IX_AuditLogs_Entity      ON dbo.AuditLogs(EntityName, EntityId, CreatedAt);

    CREATE INDEX IX_AuditLogs_User_Date   ON dbo.AuditLogs(UserId, CreatedAt);

    CREATE INDEX IX_AuditLogs_Action_Date ON dbo.AuditLogs([Action], CreatedAt);

END
GO

IF OBJECT_ID('dbo.Products') IS NULL
BEGIN
    CREATE TABLE dbo.Products (
        Id          INT IDENTITY(1,1) NOT NULL,
        ItemCode    NVARCHAR(50)  NOT NULL,
        ItemName    NVARCHAR(255) NOT NULL,
        Unit        NVARCHAR(50)  NOT NULL CONSTRAINT DF_Products_Unit  DEFAULT 'kg',
        Price       DECIMAL(18,2) NOT NULL CONSTRAINT DF_Products_Price DEFAULT 0,

        MinQuantity DECIMAL(18,3) NOT NULL CONSTRAINT DF_Products_MinQuantity DEFAULT 0,

        IsActive    BIT           NOT NULL CONSTRAINT DF_Products_IsActive  DEFAULT 1,

        CreatedAt   DATETIME2(3)  NOT NULL CONSTRAINT DF_Products_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedBy   INT           NULL,
        UpdatedAt   DATETIME2(3)  NULL,
        UpdatedBy   INT           NULL,

        Deleted     BIT           NOT NULL CONSTRAINT DF_Products_Deleted DEFAULT 0,
        DeletedAt   DATETIME2(3)  NULL,
        DeletedBy   INT           NULL,

        CONSTRAINT PK_Products PRIMARY KEY (Id),
        CONSTRAINT FK_Products_Users_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES dbo.Users(Id),
        CONSTRAINT FK_Products_Users_UpdatedBy FOREIGN KEY (UpdatedBy) REFERENCES dbo.Users(Id),
        CONSTRAINT FK_Products_Users_DeletedBy FOREIGN KEY (DeletedBy) REFERENCES dbo.Users(Id),

        CONSTRAINT CK_Products_Price       CHECK (Price >= 0),
        CONSTRAINT CK_Products_MinQuantity CHECK (MinQuantity >= 0),
        CONSTRAINT CK_Products_Deleted CHECK (
            ([Deleted] = 0 AND [DeletedAt] IS NULL) OR
            ([Deleted] = 1 AND [DeletedAt] IS NOT NULL))
    );

    CREATE UNIQUE INDEX UX_Products_ItemCode ON dbo.Products(ItemCode) WHERE Deleted = 0;
    CREATE INDEX IX_Products_ItemName  ON dbo.Products(ItemName);
    CREATE INDEX IX_Products_CreatedBy ON dbo.Products(CreatedBy);
    CREATE INDEX IX_Products_UpdatedBy ON dbo.Products(UpdatedBy);
    CREATE INDEX IX_Products_DeletedBy ON dbo.Products(DeletedBy);

END
GO

IF OBJECT_ID('dbo.Vendors') IS NULL
BEGIN
    CREATE TABLE dbo.Vendors (
        Id         INT IDENTITY(1,1) NOT NULL,
        VendorCode NVARCHAR(50)  NOT NULL,
        VendorName NVARCHAR(255) NOT NULL,
        Address    NVARCHAR(500) NULL,
        Phone      NVARCHAR(50)  NULL,
        Email      NVARCHAR(255) NULL,
        TaxCode    NVARCHAR(50)  NULL,
        IsActive   BIT           NOT NULL CONSTRAINT DF_Vendors_IsActive  DEFAULT 1,

        CreatedAt  DATETIME2(3)  NOT NULL CONSTRAINT DF_Vendors_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedBy  INT           NULL,
        UpdatedAt  DATETIME2(3)  NULL,
        UpdatedBy  INT           NULL,

        Deleted    BIT           NOT NULL CONSTRAINT DF_Vendors_Deleted DEFAULT 0,
        DeletedAt  DATETIME2(3)  NULL,
        DeletedBy  INT           NULL,

        CONSTRAINT PK_Vendors PRIMARY KEY (Id),
        CONSTRAINT FK_Vendors_Users_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES dbo.Users(Id),
        CONSTRAINT FK_Vendors_Users_UpdatedBy FOREIGN KEY (UpdatedBy) REFERENCES dbo.Users(Id),
        CONSTRAINT FK_Vendors_Users_DeletedBy FOREIGN KEY (DeletedBy) REFERENCES dbo.Users(Id),

        CONSTRAINT CK_Vendors_Deleted CHECK (
            ([Deleted] = 0 AND [DeletedAt] IS NULL) OR
            ([Deleted] = 1 AND [DeletedAt] IS NOT NULL))
    );

    CREATE UNIQUE INDEX UX_Vendors_VendorCode ON dbo.Vendors(VendorCode) WHERE Deleted = 0;
    CREATE UNIQUE INDEX UX_Vendors_VendorName ON dbo.Vendors(VendorName) WHERE Deleted = 0;
    CREATE INDEX IX_Vendors_CreatedBy ON dbo.Vendors(CreatedBy);
    CREATE INDEX IX_Vendors_UpdatedBy ON dbo.Vendors(UpdatedBy);
    CREATE INDEX IX_Vendors_DeletedBy ON dbo.Vendors(DeletedBy);

END
GO

IF OBJECT_ID('dbo.Orders') IS NULL
BEGIN
    CREATE TABLE dbo.Orders (
        Id            INT IDENTITY(1,1) NOT NULL,
        OrderNo       NVARCHAR(50) NOT NULL,
        VendorId      INT          NOT NULL,
        OrderDate     DATE         NOT NULL,
        HandlerUserId INT          NULL,
        [Status]      VARCHAR(20)  NOT NULL CONSTRAINT DF_Orders_Status DEFAULT 'DRAFT',
        Note          NVARCHAR(500) NULL,

        CancelledAt   DATETIME2(3)  NULL,
        CancelledBy   INT           NULL,
        CancelReason  NVARCHAR(500) NULL,

        CreatedAt     DATETIME2(3) NOT NULL CONSTRAINT DF_Orders_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedBy     INT          NULL,
        UpdatedAt     DATETIME2(3) NULL,
        UpdatedBy     INT          NULL,

        Deleted       BIT          NOT NULL CONSTRAINT DF_Orders_Deleted DEFAULT 0,
        DeletedAt     DATETIME2(3) NULL,
        DeletedBy     INT          NULL,

        CONSTRAINT PK_Orders PRIMARY KEY (Id),

        CONSTRAINT FK_Orders_Vendors       FOREIGN KEY (VendorId)      REFERENCES dbo.Vendors(Id),
        CONSTRAINT FK_Orders_Users_Handler FOREIGN KEY (HandlerUserId) REFERENCES dbo.Users(Id),
        CONSTRAINT FK_Orders_Users_CancelledBy FOREIGN KEY (CancelledBy) REFERENCES dbo.Users(Id),
        CONSTRAINT FK_Orders_Users_CreatedBy   FOREIGN KEY (CreatedBy)   REFERENCES dbo.Users(Id),
        CONSTRAINT FK_Orders_Users_UpdatedBy   FOREIGN KEY (UpdatedBy)   REFERENCES dbo.Users(Id),
        CONSTRAINT FK_Orders_Users_DeletedBy   FOREIGN KEY (DeletedBy)   REFERENCES dbo.Users(Id),

        CONSTRAINT CK_Orders_Status CHECK ([Status] IN ('DRAFT','CONFIRMED','RECEIVED','CANCELLED')),

        CONSTRAINT CK_Orders_Cancel CHECK (
            ([Status] <> 'CANCELLED' AND [CancelledAt] IS NULL AND [CancelReason] IS NULL) OR
            ([Status] =  'CANCELLED' AND [CancelledAt] IS NOT NULL AND
             [CancelReason] IS NOT NULL AND LEN(LTRIM([CancelReason])) > 0)),

        CONSTRAINT CK_Orders_Deleted CHECK (
            ([Deleted] = 0 AND [DeletedAt] IS NULL) OR
            ([Deleted] = 1 AND [DeletedAt] IS NOT NULL)),

        CONSTRAINT CK_Orders_NoDeleteReceived CHECK ([Deleted] = 0 OR [Status] <> 'RECEIVED')
    );

    CREATE UNIQUE INDEX UX_Orders_OrderNo ON dbo.Orders(OrderNo);

    CREATE INDEX IX_Orders_VendorId      ON dbo.Orders(VendorId);
    CREATE INDEX IX_Orders_HandlerUserId ON dbo.Orders(HandlerUserId);
    CREATE INDEX IX_Orders_CancelledBy   ON dbo.Orders(CancelledBy);
    CREATE INDEX IX_Orders_CreatedBy     ON dbo.Orders(CreatedBy);
    CREATE INDEX IX_Orders_UpdatedBy     ON dbo.Orders(UpdatedBy);
    CREATE INDEX IX_Orders_DeletedBy     ON dbo.Orders(DeletedBy);

    CREATE INDEX IX_Orders_Deleted_Status    ON dbo.Orders(Deleted, [Status]);
    CREATE INDEX IX_Orders_Deleted_OrderDate ON dbo.Orders(Deleted, OrderDate);

END
GO

IF OBJECT_ID('dbo.OrderStatusHistory') IS NULL
BEGIN
    CREATE TABLE dbo.OrderStatusHistory (
        Id         BIGINT IDENTITY(1,1) NOT NULL,
        OrderId    INT           NOT NULL,
        FromStatus VARCHAR(20)   NULL,
        ToStatus   VARCHAR(20)   NOT NULL,
        Reason     NVARCHAR(500) NULL,
        ChangedBy  INT           NULL,
        ChangedAt  DATETIME2(3)  NOT NULL CONSTRAINT DF_OSH_ChangedAt DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_OrderStatusHistory PRIMARY KEY (Id),
        CONSTRAINT FK_OSH_Orders FOREIGN KEY (OrderId)   REFERENCES dbo.Orders(Id) ON DELETE CASCADE,
        CONSTRAINT FK_OSH_Users  FOREIGN KEY (ChangedBy) REFERENCES dbo.Users(Id),

        CONSTRAINT CK_OrderStatusHistory_From CHECK (
            [FromStatus] IS NULL OR [FromStatus] IN ('DRAFT','CONFIRMED','RECEIVED','CANCELLED')),
        CONSTRAINT CK_OrderStatusHistory_To CHECK (
            [ToStatus] IN ('DRAFT','CONFIRMED','RECEIVED','CANCELLED')),

        CONSTRAINT CK_OrderStatusHistory_Change CHECK (
            [FromStatus] IS NULL OR [FromStatus] <> [ToStatus]),

        CONSTRAINT CK_OrderStatusHistory_CancelReason CHECK (
            [ToStatus] <> 'CANCELLED' OR ([Reason] IS NOT NULL AND LEN(LTRIM([Reason])) > 0))
    );

    CREATE INDEX IX_OrderStatusHistory_Order_Date ON dbo.OrderStatusHistory(OrderId, ChangedAt);
    CREATE INDEX IX_OrderStatusHistory_ChangedBy  ON dbo.OrderStatusHistory(ChangedBy);

END
GO

IF OBJECT_ID('dbo.OrderProducts') IS NULL
BEGIN
    CREATE TABLE dbo.OrderProducts (
        Id         INT IDENTITY(1,1) NOT NULL,
        OrderId    INT           NOT NULL,
        ProductId  INT           NOT NULL,

        ItemCode   NVARCHAR(50)  NOT NULL,
        ItemName   NVARCHAR(255) NOT NULL,
        Unit       NVARCHAR(50)  NOT NULL,

        Quantity   DECIMAL(18,3) NOT NULL CONSTRAINT DF_OrderProducts_Quantity  DEFAULT 0,

        UnitPrice  DECIMAL(18,2) NOT NULL CONSTRAINT DF_OrderProducts_UnitPrice DEFAULT 0,

        LineAmount AS (CONVERT(decimal(18,2), [Quantity] * [UnitPrice])) PERSISTED,

        CONSTRAINT PK_OrderProducts PRIMARY KEY (Id),

        CONSTRAINT FK_OrderProducts_Orders   FOREIGN KEY (OrderId)   REFERENCES dbo.Orders(Id) ON DELETE CASCADE,
        CONSTRAINT FK_OrderProducts_Products FOREIGN KEY (ProductId) REFERENCES dbo.Products(Id),

        CONSTRAINT CK_OrderProducts_Quantity  CHECK (Quantity > 0),
        CONSTRAINT CK_OrderProducts_UnitPrice CHECK (UnitPrice >= 0)
    );

    CREATE UNIQUE INDEX UX_OrderProducts_Order_Product ON dbo.OrderProducts(OrderId, ProductId);
    CREATE INDEX IX_OrderProducts_ProductId ON dbo.OrderProducts(ProductId);

END
GO

IF OBJECT_ID('dbo.Stock') IS NULL
BEGIN
    CREATE TABLE dbo.Stock (
        Id           INT IDENTITY(1,1) NOT NULL,
        ProductId    INT           NOT NULL,
        Quantity     DECIMAL(18,3) NOT NULL CONSTRAINT DF_Stock_Quantity  DEFAULT 0,

        [RowVersion] ROWVERSION    NOT NULL,

        CreatedAt    DATETIME2(3)  NOT NULL CONSTRAINT DF_Stock_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedBy    INT           NULL,
        UpdatedAt    DATETIME2(3)  NULL,
        UpdatedBy    INT           NULL,

        CONSTRAINT PK_Stock PRIMARY KEY (Id),
        CONSTRAINT FK_Stock_Products FOREIGN KEY (ProductId) REFERENCES dbo.Products(Id) ON DELETE CASCADE,
        CONSTRAINT FK_Stock_Users_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES dbo.Users(Id),
        CONSTRAINT FK_Stock_Users_UpdatedBy FOREIGN KEY (UpdatedBy) REFERENCES dbo.Users(Id),
        CONSTRAINT CK_Stock_Quantity CHECK (Quantity >= 0)
    );

    CREATE UNIQUE INDEX UX_Stock_ProductId ON dbo.Stock(ProductId);
    CREATE INDEX IX_Stock_CreatedBy ON dbo.Stock(CreatedBy);
    CREATE INDEX IX_Stock_UpdatedBy ON dbo.Stock(UpdatedBy);

END
GO

IF OBJECT_ID('dbo.StockTransactions') IS NULL
BEGIN
    CREATE TABLE dbo.StockTransactions (
        Id              BIGINT IDENTITY(1,1) NOT NULL,
        ProductId       INT           NOT NULL,
        TransactionType VARCHAR(10)   NOT NULL,
        Quantity        DECIMAL(18,3) NOT NULL,

        BalanceBefore   DECIMAL(18,3) NOT NULL,
        BalanceAfter    DECIMAL(18,3) NOT NULL,

        UnitPrice       DECIMAL(18,2) NULL,
        RefType         VARCHAR(20)   NULL,
        RefId           INT           NULL,

        OriginalTransactionId BIGINT  NULL,

        TransactionDate DATETIME2(3)  NOT NULL CONSTRAINT DF_ST_Date DEFAULT SYSUTCDATETIME(),
        CreatedBy       INT           NULL,
        Note            NVARCHAR(500) NULL,

        CONSTRAINT PK_StockTransactions PRIMARY KEY (Id),
        CONSTRAINT FK_ST_Products FOREIGN KEY (ProductId) REFERENCES dbo.Products(Id),
        CONSTRAINT FK_ST_Users    FOREIGN KEY (CreatedBy) REFERENCES dbo.Users(Id),
        CONSTRAINT FK_ST_Original FOREIGN KEY (OriginalTransactionId) REFERENCES dbo.StockTransactions(Id),

        CONSTRAINT CK_ST_Type    CHECK (TransactionType IN ('IN','OUT','ADJUST','REVERSAL')),
        CONSTRAINT CK_ST_RefType CHECK (RefType IS NULL OR RefType IN ('ORDER','MANUAL','REVERSAL')),

        CONSTRAINT CK_ST_Balance CHECK ([BalanceAfter] = [BalanceBefore] + [Quantity]),

        CONSTRAINT CK_ST_BalanceSign CHECK ([BalanceBefore] >= 0 AND [BalanceAfter] >= 0),

        CONSTRAINT CK_ST_QuantitySign CHECK (
            (TransactionType = 'IN'       AND Quantity > 0)  OR
            (TransactionType = 'OUT'      AND Quantity < 0)  OR
            (TransactionType = 'ADJUST'   AND Quantity <> 0) OR
            (TransactionType = 'REVERSAL' AND Quantity <> 0)),

        CONSTRAINT CK_ST_Reversal CHECK (
            (TransactionType =  'REVERSAL' AND OriginalTransactionId IS NOT NULL) OR
            (TransactionType <> 'REVERSAL' AND OriginalTransactionId IS NULL)),

        CONSTRAINT CK_ST_UnitPrice CHECK (UnitPrice IS NULL OR UnitPrice >= 0)
    );

    CREATE INDEX IX_StockTransactions_Product_Date
        ON dbo.StockTransactions(ProductId, TransactionDate)
        INCLUDE (Quantity, BalanceBefore, BalanceAfter);

    CREATE INDEX IX_StockTransactions_Ref ON dbo.StockTransactions(RefType, RefId);

    CREATE INDEX IX_StockTransactions_CreatedBy ON dbo.StockTransactions(CreatedBy);

    CREATE UNIQUE INDEX UX_StockTransactions_Order_Product
        ON dbo.StockTransactions(RefId, ProductId)
        WHERE RefType = 'ORDER';

    CREATE UNIQUE INDEX UX_StockTransactions_Reversal
        ON dbo.StockTransactions(OriginalTransactionId)
        WHERE OriginalTransactionId IS NOT NULL;

END
GO

IF NOT EXISTS (SELECT 1 FROM sys.sequences WHERE name = 'Seq_OrderNo')
BEGIN
    CREATE SEQUENCE dbo.Seq_OrderNo AS BIGINT START WITH 1 INCREMENT BY 1 NO CYCLE NO CACHE;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.sequences WHERE name = 'Seq_VendorCode')
BEGIN
    CREATE SEQUENCE dbo.Seq_VendorCode AS BIGINT START WITH 1 INCREMENT BY 1 NO CYCLE NO CACHE;
END
GO

GO

USE tonkho_db;
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
SET XACT_ABORT ON;
GO

IF EXISTS (SELECT 1 FROM dbo.Users)
BEGIN
    SET NOEXEC ON;
END
GO

SET IDENTITY_INSERT dbo.[Roles] ON;
INSERT INTO dbo.[Roles] ([Id], [Code], [Name], [Description], [IsSystem], [CreatedAt], [CreatedBy], [UpdatedAt], [UpdatedBy]) VALUES (1, N'Admin', N'Quản trị viên', N'Toàn quyền trên hệ thống', 1, '2026-08-27T10:37:08.656', NULL, NULL, NULL);
INSERT INTO dbo.[Roles] ([Id], [Code], [Name], [Description], [IsSystem], [CreatedAt], [CreatedBy], [UpdatedAt], [UpdatedBy]) VALUES (2, N'Purchasing', N'Nhân viên mua hàng', N'Quản lý danh mục sản phẩm/NCC, lập và xác nhận đơn mua', 1, '2026-08-27T10:37:08.656', NULL, NULL, NULL);
INSERT INTO dbo.[Roles] ([Id], [Code], [Name], [Description], [IsSystem], [CreatedAt], [CreatedBy], [UpdatedAt], [UpdatedBy]) VALUES (3, N'User', N'Nhân viên', N'Chỉ xem, không thêm/sửa/xoá', 1, '2026-08-27T10:37:08.656', NULL, NULL, NULL);
INSERT INTO dbo.[Roles] ([Id], [Code], [Name], [Description], [IsSystem], [CreatedAt], [CreatedBy], [UpdatedAt], [UpdatedBy]) VALUES (4, N'Warehouse', N'Nhân viên kho', N'Nhập kho theo đơn, xuất kho, điều chỉnh tồn, đảo bút toán', 1, '2026-08-27T10:37:08.656', NULL, NULL, NULL);
SET IDENTITY_INSERT dbo.[Roles] OFF;
GO

SET IDENTITY_INSERT dbo.[Users] ON;
INSERT INTO dbo.[Users] ([Id], [UserId], [UserPassword], [FullName], [IsActive], [LastLoginAt], [CreatedAt], [CreatedBy], [UpdatedAt], [UpdatedBy], [Deleted], [DeletedAt], [DeletedBy]) VALUES (1, N'admin', N'$2a$10$buwzdElNVwk9my6LMQM4OeUI3.QcNWbBCiD65jK/J.Ud5h06nr8mm', N'Quản trị viên', 1, '2026-10-06T07:41:11.755', '2026-08-07T11:56:15.282', NULL, '2026-10-06T07:41:11.756', NULL, 0, NULL, NULL);
INSERT INTO dbo.[Users] ([Id], [UserId], [UserPassword], [FullName], [IsActive], [LastLoginAt], [CreatedAt], [CreatedBy], [UpdatedAt], [UpdatedBy], [Deleted], [DeletedAt], [DeletedBy]) VALUES (2, N'kaori', N'$2a$10$buwzdElNVwk9my6LMQM4OeUI3.QcNWbBCiD65jK/J.Ud5h06nr8mm', N'hayashi kaori', 1, '2026-08-14T03:09:22.150', '2026-08-12T06:25:57.970', NULL, '2026-08-14T03:09:22.152', NULL, 0, NULL, NULL);
INSERT INTO dbo.[Users] ([Id], [UserId], [UserPassword], [FullName], [IsActive], [LastLoginAt], [CreatedAt], [CreatedBy], [UpdatedAt], [UpdatedBy], [Deleted], [DeletedAt], [DeletedBy]) VALUES (3, N'nguyen', N'$2a$10$buwzdElNVwk9my6LMQM4OeUI3.QcNWbBCiD65jK/J.Ud5h06nr8mm', N'nguyen A', 1, '2026-08-13T13:02:45.616', '2026-08-12T07:57:02.323', NULL, '2026-08-13T13:02:45.616', NULL, 0, NULL, NULL);
INSERT INTO dbo.[Users] ([Id], [UserId], [UserPassword], [FullName], [IsActive], [LastLoginAt], [CreatedAt], [CreatedBy], [UpdatedAt], [UpdatedBy], [Deleted], [DeletedAt], [DeletedBy]) VALUES (17, N'_e2e', N'$2a$10$buwzdElNVwk9my6LMQM4OeUI3.QcNWbBCiD65jK/J.Ud5h06nr8mm', N'Tai khoan test E2E', 0, '2026-08-27T14:45:13.247', '2026-08-27T14:33:21.084', NULL, '2026-08-27T14:45:13.364', NULL, 1, '2026-08-27T14:45:58.165', NULL);
INSERT INTO dbo.[Users] ([Id], [UserId], [UserPassword], [FullName], [IsActive], [LastLoginAt], [CreatedAt], [CreatedBy], [UpdatedAt], [UpdatedBy], [Deleted], [DeletedAt], [DeletedBy]) VALUES (18, N'_e2eview', N'$2a$10$buwzdElNVwk9my6LMQM4OeUI3.QcNWbBCiD65jK/J.Ud5h06nr8mm', N'Test chi xem', 0, '2026-08-27T14:41:19.796', '2026-08-27T14:41:19.479', NULL, '2026-08-27T14:41:41.267', 17, 1, '2026-08-27T14:45:58.165', NULL);
SET IDENTITY_INSERT dbo.[Users] OFF;
GO

SET IDENTITY_INSERT dbo.[UserRoles] ON;
INSERT INTO dbo.[UserRoles] ([Id], [UserId], [RoleId], [AssignedAt], [AssignedBy]) VALUES (1, 1, 1, '2026-08-27T10:37:08.789', NULL);
INSERT INTO dbo.[UserRoles] ([Id], [UserId], [RoleId], [AssignedAt], [AssignedBy]) VALUES (2, 2, 2, '2026-08-27T10:37:08.789', NULL);
INSERT INTO dbo.[UserRoles] ([Id], [UserId], [RoleId], [AssignedAt], [AssignedBy]) VALUES (3, 3, 4, '2026-08-27T10:37:08.789', NULL);
INSERT INTO dbo.[UserRoles] ([Id], [UserId], [RoleId], [AssignedAt], [AssignedBy]) VALUES (4, 17, 1, '2026-08-27T14:33:21.096', NULL);
INSERT INTO dbo.[UserRoles] ([Id], [UserId], [RoleId], [AssignedAt], [AssignedBy]) VALUES (5, 17, 2, '2026-08-27T14:33:21.096', NULL);
INSERT INTO dbo.[UserRoles] ([Id], [UserId], [RoleId], [AssignedAt], [AssignedBy]) VALUES (6, 17, 4, '2026-08-27T14:33:21.096', NULL);
INSERT INTO dbo.[UserRoles] ([Id], [UserId], [RoleId], [AssignedAt], [AssignedBy]) VALUES (7, 18, 3, '2026-08-27T14:41:19.479', NULL);
SET IDENTITY_INSERT dbo.[UserRoles] OFF;
GO

SET IDENTITY_INSERT dbo.[Vendors] ON;
INSERT INTO dbo.[Vendors] ([Id], [VendorCode], [VendorName], [Address], [Phone], [Email], [TaxCode], [IsActive], [CreatedAt], [CreatedBy], [UpdatedAt], [UpdatedBy], [Deleted], [DeletedAt], [DeletedBy]) VALUES (1, N'NCC001', N'Công ty TNHH ABC', N'123 Nguyễn Trãi, Q.1, TP.HCM', NULL, NULL, NULL, 1, '2026-08-07T11:56:15.306', NULL, NULL, NULL, 0, NULL, NULL);
INSERT INTO dbo.[Vendors] ([Id], [VendorCode], [VendorName], [Address], [Phone], [Email], [TaxCode], [IsActive], [CreatedAt], [CreatedBy], [UpdatedAt], [UpdatedBy], [Deleted], [DeletedAt], [DeletedBy]) VALUES (2, N'NCC002', N'Công ty CP XYZ', N'456 Lê Lợi, Q.3, TP.HCM', N'03 1234 5671', N'syz@gmail.com', N'123', 1, '2026-08-07T11:56:15.306', NULL, '2026-08-13T12:46:50.959', 2, 0, NULL, NULL);
INSERT INTO dbo.[Vendors] ([Id], [VendorCode], [VendorName], [Address], [Phone], [Email], [TaxCode], [IsActive], [CreatedAt], [CreatedBy], [UpdatedAt], [UpdatedBy], [Deleted], [DeletedAt], [DeletedBy]) VALUES (3, N'NCC003', N'Đại lý Minh Phát', N'789 Trần Hưng Đạo, Q.5, TP.HCM', NULL, NULL, NULL, 1, '2026-08-07T11:56:15.306', NULL, NULL, NULL, 0, NULL, NULL);
INSERT INTO dbo.[Vendors] ([Id], [VendorCode], [VendorName], [Address], [Phone], [Email], [TaxCode], [IsActive], [CreatedAt], [CreatedBy], [UpdatedAt], [UpdatedBy], [Deleted], [DeletedAt], [DeletedBy]) VALUES (4, N'NCC004', N'Công ty Vật tư Hoàng Long', N'12 Hai Bà Trưng, Hà Nội', NULL, NULL, NULL, 1, '2026-08-07T11:56:15.306', NULL, NULL, NULL, 0, NULL, NULL);
INSERT INTO dbo.[Vendors] ([Id], [VendorCode], [VendorName], [Address], [Phone], [Email], [TaxCode], [IsActive], [CreatedAt], [CreatedBy], [UpdatedAt], [UpdatedBy], [Deleted], [DeletedAt], [DeletedBy]) VALUES (5, N'NCC005', N'Nhà phân phối Tân Tiến', N'34 Pasteur, Q.1, TP.HCM', NULL, NULL, NULL, 1, '2026-08-07T11:56:15.306', NULL, NULL, NULL, 0, NULL, NULL);
SET IDENTITY_INSERT dbo.[Vendors] OFF;
GO

SET IDENTITY_INSERT dbo.[Products] ON;
INSERT INTO dbo.[Products] ([Id], [ItemCode], [ItemName], [Unit], [Price], [IsActive], [CreatedAt], [CreatedBy], [UpdatedAt], [UpdatedBy], [Deleted], [DeletedAt], [DeletedBy], [MinQuantity]) VALUES (1, N'LK001', N'Ốc vít M5x20 SUS304', N'PCS', 150.00, 1, '2026-08-07T11:56:15.318', NULL, '2026-08-11T14:22:52.712', 1, 0, NULL, NULL, 500.000);
INSERT INTO dbo.[Products] ([Id], [ItemCode], [ItemName], [Unit], [Price], [IsActive], [CreatedAt], [CreatedBy], [UpdatedAt], [UpdatedBy], [Deleted], [DeletedAt], [DeletedBy], [MinQuantity]) VALUES (2, N'LK002', N'Bạc đạn 6205-2RS', N'PCS', 85000.00, 1, '2026-08-07T11:56:15.318', NULL, NULL, NULL, 0, NULL, NULL, 20.000);
INSERT INTO dbo.[Products] ([Id], [ItemCode], [ItemName], [Unit], [Price], [IsActive], [CreatedAt], [CreatedBy], [UpdatedAt], [UpdatedBy], [Deleted], [DeletedAt], [DeletedBy], [MinQuantity]) VALUES (3, N'LK003', N'Dây đai timing belt HTD-5M', N'PCS', 120000.00, 1, '2026-08-07T11:56:15.318', NULL, NULL, NULL, 0, NULL, NULL, 10.000);
INSERT INTO dbo.[Products] ([Id], [ItemCode], [ItemName], [Unit], [Price], [IsActive], [CreatedAt], [CreatedBy], [UpdatedAt], [UpdatedBy], [Deleted], [DeletedAt], [DeletedBy], [MinQuantity]) VALUES (4, N'LK004', N'Cảm biến tiệm cận E2E-X5ME1', N'PCS', 350000.00, 1, '2026-08-07T11:56:15.318', NULL, NULL, NULL, 0, NULL, NULL, 5.000);
INSERT INTO dbo.[Products] ([Id], [ItemCode], [ItemName], [Unit], [Price], [IsActive], [CreatedAt], [CreatedBy], [UpdatedAt], [UpdatedBy], [Deleted], [DeletedAt], [DeletedBy], [MinQuantity]) VALUES (5, N'LK005', N'Xy lanh khí SMC CDU20-50D', N'PCS', 480000.00, 1, '2026-08-07T11:56:15.318', NULL, NULL, NULL, 0, NULL, NULL, 5.000);
INSERT INTO dbo.[Products] ([Id], [ItemCode], [ItemName], [Unit], [Price], [IsActive], [CreatedAt], [CreatedBy], [UpdatedAt], [UpdatedBy], [Deleted], [DeletedAt], [DeletedBy], [MinQuantity]) VALUES (6, N'LK006', N'Relay trung gian MY2N-GS 24VDC', N'PCS', 95000.00, 1, '2026-08-07T11:56:15.318', NULL, NULL, NULL, 0, NULL, NULL, 15.000);
INSERT INTO dbo.[Products] ([Id], [ItemCode], [ItemName], [Unit], [Price], [IsActive], [CreatedAt], [CreatedBy], [UpdatedAt], [UpdatedBy], [Deleted], [DeletedAt], [DeletedBy], [MinQuantity]) VALUES (7, N'LK007', N'Động cơ servo 400W Mitsubishi', N'PCS', 5200000.00, 1, '2026-08-07T11:56:15.318', NULL, NULL, NULL, 0, NULL, NULL, 2.000);
INSERT INTO dbo.[Products] ([Id], [ItemCode], [ItemName], [Unit], [Price], [IsActive], [CreatedAt], [CreatedBy], [UpdatedAt], [UpdatedBy], [Deleted], [DeletedAt], [DeletedBy], [MinQuantity]) VALUES (8, N'LK008', N'PLC Mitsubishi FX3U-32MT', N'PCS', 8500000.00, 1, '2026-08-07T11:56:15.318', NULL, NULL, NULL, 0, NULL, NULL, 1.000);
INSERT INTO dbo.[Products] ([Id], [ItemCode], [ItemName], [Unit], [Price], [IsActive], [CreatedAt], [CreatedBy], [UpdatedAt], [UpdatedBy], [Deleted], [DeletedAt], [DeletedBy], [MinQuantity]) VALUES (9, N'LK009', N'Nhôm thanh profile 4040 (1m)', N'PCS', 145000.00, 1, '2026-08-07T11:56:15.318', NULL, NULL, NULL, 0, NULL, NULL, 30.000);
INSERT INTO dbo.[Products] ([Id], [ItemCode], [ItemName], [Unit], [Price], [IsActive], [CreatedAt], [CreatedBy], [UpdatedAt], [UpdatedBy], [Deleted], [DeletedAt], [DeletedBy], [MinQuantity]) VALUES (10, N'LK010', N'Đầu nối khí nhanh KQ2H06-01S', N'PCS', 25000.00, 1, '2026-08-07T11:56:15.318', NULL, NULL, NULL, 0, NULL, NULL, 100.000);
SET IDENTITY_INSERT dbo.[Products] OFF;
GO

SET IDENTITY_INSERT dbo.[Stock] ON;
INSERT INTO dbo.[Stock] ([Id], [ProductId], [Quantity], [CreatedAt], [CreatedBy], [UpdatedAt], [UpdatedBy]) VALUES (1, 7, 1.000, '2026-08-13T07:38:55.355', 3, '2026-08-14T03:47:22.446', 1);
INSERT INTO dbo.[Stock] ([Id], [ProductId], [Quantity], [CreatedAt], [CreatedBy], [UpdatedAt], [UpdatedBy]) VALUES (2, 4, 3.000, '2026-08-14T03:46:23.798', 1, '2026-08-14T03:48:33.127', 1);
INSERT INTO dbo.[Stock] ([Id], [ProductId], [Quantity], [CreatedAt], [CreatedBy], [UpdatedAt], [UpdatedBy]) VALUES (3, 6, 1.000, '2026-08-14T03:46:23.798', 1, NULL, NULL);
INSERT INTO dbo.[Stock] ([Id], [ProductId], [Quantity], [CreatedAt], [CreatedBy], [UpdatedAt], [UpdatedBy]) VALUES (4, 1, 3.000, '2026-08-14T03:51:35.028', 1, '2026-10-06T07:06:40.996', 1);
INSERT INTO dbo.[Stock] ([Id], [ProductId], [Quantity], [CreatedAt], [CreatedBy], [UpdatedAt], [UpdatedBy]) VALUES (5, 10, 0.000, '2026-08-14T03:51:35.028', 1, '2026-10-06T07:08:43.608', 1);
INSERT INTO dbo.[Stock] ([Id], [ProductId], [Quantity], [CreatedAt], [CreatedBy], [UpdatedAt], [UpdatedBy]) VALUES (13, 2, 2.000, '2026-10-06T07:06:40.996', 1, NULL, NULL);
SET IDENTITY_INSERT dbo.[Stock] OFF;
GO

SET IDENTITY_INSERT dbo.[Orders] ON;
INSERT INTO dbo.[Orders] ([Id], [OrderNo], [VendorId], [OrderDate], [HandlerUserId], [Status], [Note], [CreatedAt], [CreatedBy], [UpdatedAt], [UpdatedBy], [Deleted], [DeletedAt], [DeletedBy], [CancelledAt], [CancelledBy], [CancelReason]) VALUES (1, N'PO00001', 2, '2026-08-13', 3, N'RECEIVED', N'', '2026-08-13T05:02:00.468', 3, '2026-08-13T07:38:55.355', 3, 0, NULL, NULL, NULL, NULL, NULL);
INSERT INTO dbo.[Orders] ([Id], [OrderNo], [VendorId], [OrderDate], [HandlerUserId], [Status], [Note], [CreatedAt], [CreatedBy], [UpdatedAt], [UpdatedBy], [Deleted], [DeletedAt], [DeletedBy], [CancelledAt], [CancelledBy], [CancelReason]) VALUES (6, N'PO00023', 5, '2026-08-15', 2, N'DRAFT', N'', '2026-08-14T01:38:03.448', 2, NULL, NULL, 0, NULL, NULL, NULL, NULL, NULL);
INSERT INTO dbo.[Orders] ([Id], [OrderNo], [VendorId], [OrderDate], [HandlerUserId], [Status], [Note], [CreatedAt], [CreatedBy], [UpdatedAt], [UpdatedBy], [Deleted], [DeletedAt], [DeletedBy], [CancelledAt], [CancelledBy], [CancelReason]) VALUES (7, N'PO00024', 5, '2026-08-15', 2, N'RECEIVED', N'', '2026-08-14T01:38:35.776', 2, '2026-08-14T03:51:35.028', 1, 0, NULL, NULL, NULL, NULL, NULL);
INSERT INTO dbo.[Orders] ([Id], [OrderNo], [VendorId], [OrderDate], [HandlerUserId], [Status], [Note], [CreatedAt], [CreatedBy], [UpdatedAt], [UpdatedBy], [Deleted], [DeletedAt], [DeletedBy], [CancelledAt], [CancelledBy], [CancelReason]) VALUES (9, N'PO00026', 4, '2026-08-16', 1, N'RECEIVED', N'', '2026-08-14T03:45:25.235', 1, '2026-08-14T03:46:23.798', 1, 0, NULL, NULL, NULL, NULL, NULL);
INSERT INTO dbo.[Orders] ([Id], [OrderNo], [VendorId], [OrderDate], [HandlerUserId], [Status], [Note], [CreatedAt], [CreatedBy], [UpdatedAt], [UpdatedBy], [Deleted], [DeletedAt], [DeletedBy], [CancelledAt], [CancelledBy], [CancelReason]) VALUES (15, N'PO00050', 1, '2026-08-27', 17, N'CANCELLED', N'Don test E2E', '2026-08-27T14:38:48.023', 17, '2026-08-27T14:39:05.453', 17, 1, '2026-08-27T14:45:58.165', NULL, '2026-08-27T14:39:05.449', 17, N'NCC báo hết hàng, chuyển sang NCC khác');
INSERT INTO dbo.[Orders] ([Id], [OrderNo], [VendorId], [OrderDate], [HandlerUserId], [Status], [Note], [CreatedAt], [CreatedBy], [UpdatedAt], [UpdatedBy], [Deleted], [DeletedAt], [DeletedBy], [CancelledAt], [CancelledBy], [CancelReason]) VALUES (38, N'PO00073', 2, '2026-10-06', 1, N'RECEIVED', N'', '2026-10-06T07:06:13.916', 1, '2026-10-06T07:06:40.996', 1, 0, NULL, NULL, NULL, NULL, NULL);
SET IDENTITY_INSERT dbo.[Orders] OFF;
GO

SET IDENTITY_INSERT dbo.[OrderProducts] ON;
INSERT INTO dbo.[OrderProducts] ([Id], [OrderId], [ProductId], [Quantity], [UnitPrice], [ItemCode], [ItemName], [Unit]) VALUES (4, 1, 7, 2.000, 5200000.00, N'LK007', N'Động cơ servo 400W Mitsubishi', N'PCS');
INSERT INTO dbo.[OrderProducts] ([Id], [OrderId], [ProductId], [Quantity], [UnitPrice], [ItemCode], [ItemName], [Unit]) VALUES (7, 9, 4, 2.000, 350000.00, N'LK004', N'Cảm biến tiệm cận E2E-X5ME1', N'PCS');
INSERT INTO dbo.[OrderProducts] ([Id], [OrderId], [ProductId], [Quantity], [UnitPrice], [ItemCode], [ItemName], [Unit]) VALUES (8, 9, 6, 1.000, 95000.00, N'LK006', N'Relay trung gian MY2N-GS 24VDC', N'PCS');
INSERT INTO dbo.[OrderProducts] ([Id], [OrderId], [ProductId], [Quantity], [UnitPrice], [ItemCode], [ItemName], [Unit]) VALUES (12, 6, 7, 1.000, 5200000.00, N'LK007', N'Động cơ servo 400W Mitsubishi', N'PCS');
INSERT INTO dbo.[OrderProducts] ([Id], [OrderId], [ProductId], [Quantity], [UnitPrice], [ItemCode], [ItemName], [Unit]) VALUES (13, 6, 10, 1.000, 25000.00, N'LK010', N'Đầu nối khí nhanh KQ2H06-01S', N'PCS');
INSERT INTO dbo.[OrderProducts] ([Id], [OrderId], [ProductId], [Quantity], [UnitPrice], [ItemCode], [ItemName], [Unit]) VALUES (14, 7, 1, 2.000, 150.00, N'LK001', N'Ốc vít M5x20 SUS304', N'PCS');
INSERT INTO dbo.[OrderProducts] ([Id], [OrderId], [ProductId], [Quantity], [UnitPrice], [ItemCode], [ItemName], [Unit]) VALUES (15, 7, 10, 1.000, 25000.00, N'LK010', N'Đầu nối khí nhanh KQ2H06-01S', N'PCS');
INSERT INTO dbo.[OrderProducts] ([Id], [OrderId], [ProductId], [Quantity], [UnitPrice], [ItemCode], [ItemName], [Unit]) VALUES (20, 15, 1, 5.000, 150.00, N'LK001', N'Ốc vít M5x20 SUS304', N'PCS');
INSERT INTO dbo.[OrderProducts] ([Id], [OrderId], [ProductId], [Quantity], [UnitPrice], [ItemCode], [ItemName], [Unit]) VALUES (45, 38, 1, 1.000, 150.00, N'LK001', N'Ốc vít M5x20 SUS304', N'PCS');
INSERT INTO dbo.[OrderProducts] ([Id], [OrderId], [ProductId], [Quantity], [UnitPrice], [ItemCode], [ItemName], [Unit]) VALUES (46, 38, 2, 2.000, 85000.00, N'LK002', N'Bạc đạn 6205-2RS', N'PCS');
SET IDENTITY_INSERT dbo.[OrderProducts] OFF;
GO

SET IDENTITY_INSERT dbo.[OrderStatusHistory] ON;
INSERT INTO dbo.[OrderStatusHistory] ([Id], [OrderId], [FromStatus], [ToStatus], [Reason], [ChangedBy], [ChangedAt]) VALUES (1, 1, NULL, N'RECEIVED', N'N蘯｡p l蘯｡i t盻ｫ d盻ｯ li盻㎡ trﾆｰ盻嫩 v4 - khﾃｴng cﾃｳ l盻議h s盻ｭ chuy盻ハ tr蘯｡ng thﾃ｡i g盻祖', 3, '2026-08-13T05:02:00.468');
INSERT INTO dbo.[OrderStatusHistory] ([Id], [OrderId], [FromStatus], [ToStatus], [Reason], [ChangedBy], [ChangedAt]) VALUES (2, 6, NULL, N'DRAFT', N'N蘯｡p l蘯｡i t盻ｫ d盻ｯ li盻㎡ trﾆｰ盻嫩 v4 - khﾃｴng cﾃｳ l盻議h s盻ｭ chuy盻ハ tr蘯｡ng thﾃ｡i g盻祖', 2, '2026-08-14T01:38:03.448');
INSERT INTO dbo.[OrderStatusHistory] ([Id], [OrderId], [FromStatus], [ToStatus], [Reason], [ChangedBy], [ChangedAt]) VALUES (3, 7, NULL, N'RECEIVED', N'N蘯｡p l蘯｡i t盻ｫ d盻ｯ li盻㎡ trﾆｰ盻嫩 v4 - khﾃｴng cﾃｳ l盻議h s盻ｭ chuy盻ハ tr蘯｡ng thﾃ｡i g盻祖', 2, '2026-08-14T01:38:35.776');
INSERT INTO dbo.[OrderStatusHistory] ([Id], [OrderId], [FromStatus], [ToStatus], [Reason], [ChangedBy], [ChangedAt]) VALUES (4, 9, NULL, N'RECEIVED', N'N蘯｡p l蘯｡i t盻ｫ d盻ｯ li盻㎡ trﾆｰ盻嫩 v4 - khﾃｴng cﾃｳ l盻議h s盻ｭ chuy盻ハ tr蘯｡ng thﾃ｡i g盻祖', 1, '2026-08-14T03:45:25.235');
INSERT INTO dbo.[OrderStatusHistory] ([Id], [OrderId], [FromStatus], [ToStatus], [Reason], [ChangedBy], [ChangedAt]) VALUES (5, 15, NULL, N'DRAFT', NULL, 17, '2026-08-27T14:38:48.255');
INSERT INTO dbo.[OrderStatusHistory] ([Id], [OrderId], [FromStatus], [ToStatus], [Reason], [ChangedBy], [ChangedAt]) VALUES (6, 15, N'DRAFT', N'CONFIRMED', NULL, 17, '2026-08-27T14:39:05.141');
INSERT INTO dbo.[OrderStatusHistory] ([Id], [OrderId], [FromStatus], [ToStatus], [Reason], [ChangedBy], [ChangedAt]) VALUES (7, 15, N'CONFIRMED', N'CANCELLED', N'NCC báo hết hàng, chuyển sang NCC khác', 17, '2026-08-27T14:39:05.450');
INSERT INTO dbo.[OrderStatusHistory] ([Id], [OrderId], [FromStatus], [ToStatus], [Reason], [ChangedBy], [ChangedAt]) VALUES (54, 38, NULL, N'DRAFT', NULL, 1, '2026-10-06T07:06:14.282');
INSERT INTO dbo.[OrderStatusHistory] ([Id], [OrderId], [FromStatus], [ToStatus], [Reason], [ChangedBy], [ChangedAt]) VALUES (55, 38, N'DRAFT', N'CONFIRMED', NULL, 1, '2026-10-06T07:06:36.454');
INSERT INTO dbo.[OrderStatusHistory] ([Id], [OrderId], [FromStatus], [ToStatus], [Reason], [ChangedBy], [ChangedAt]) VALUES (56, 38, N'CONFIRMED', N'RECEIVED', NULL, 1, '2026-10-06T07:06:40.986');
SET IDENTITY_INSERT dbo.[OrderStatusHistory] OFF;
GO

SET IDENTITY_INSERT dbo.[StockTransactions] ON;
INSERT INTO dbo.[StockTransactions] ([Id], [ProductId], [TransactionType], [Quantity], [BalanceAfter], [UnitPrice], [RefType], [RefId], [TransactionDate], [CreatedBy], [Note], [BalanceBefore], [OriginalTransactionId]) VALUES (1, 7, N'IN', 2.000, 2.000, 5200000.00, N'ORDER', 1, '2026-08-13T07:38:55.284', 3, N'Nhập kho theo đơn PO00001', 0.000, NULL);
INSERT INTO dbo.[StockTransactions] ([Id], [ProductId], [TransactionType], [Quantity], [BalanceAfter], [UnitPrice], [RefType], [RefId], [TransactionDate], [CreatedBy], [Note], [BalanceBefore], [OriginalTransactionId]) VALUES (2, 4, N'IN', 2.000, 2.000, 350000.00, N'ORDER', 9, '2026-08-14T03:46:23.724', 1, N'Nhập kho theo đơn PO00026', 0.000, NULL);
INSERT INTO dbo.[StockTransactions] ([Id], [ProductId], [TransactionType], [Quantity], [BalanceAfter], [UnitPrice], [RefType], [RefId], [TransactionDate], [CreatedBy], [Note], [BalanceBefore], [OriginalTransactionId]) VALUES (3, 6, N'IN', 1.000, 1.000, 95000.00, N'ORDER', 9, '2026-08-14T03:46:23.724', 1, N'Nhập kho theo đơn PO00026', 0.000, NULL);
INSERT INTO dbo.[StockTransactions] ([Id], [ProductId], [TransactionType], [Quantity], [BalanceAfter], [UnitPrice], [RefType], [RefId], [TransactionDate], [CreatedBy], [Note], [BalanceBefore], [OriginalTransactionId]) VALUES (4, 7, N'OUT', -1.000, 1.000, 5200000.00, N'MANUAL', NULL, '2026-08-14T03:47:22.442', 1, N'xuất cho chuyền A', 2.000, NULL);
INSERT INTO dbo.[StockTransactions] ([Id], [ProductId], [TransactionType], [Quantity], [BalanceAfter], [UnitPrice], [RefType], [RefId], [TransactionDate], [CreatedBy], [Note], [BalanceBefore], [OriginalTransactionId]) VALUES (5, 4, N'ADJUST', 1.000, 3.000, 350000.00, N'MANUAL', NULL, '2026-08-14T03:48:33.127', 1, N'nguyen A', 2.000, NULL);
INSERT INTO dbo.[StockTransactions] ([Id], [ProductId], [TransactionType], [Quantity], [BalanceAfter], [UnitPrice], [RefType], [RefId], [TransactionDate], [CreatedBy], [Note], [BalanceBefore], [OriginalTransactionId]) VALUES (6, 1, N'IN', 2.000, 2.000, 150.00, N'ORDER', 7, '2026-08-14T03:51:35.018', 1, N'Nhập kho theo đơn PO00024', 0.000, NULL);
INSERT INTO dbo.[StockTransactions] ([Id], [ProductId], [TransactionType], [Quantity], [BalanceAfter], [UnitPrice], [RefType], [RefId], [TransactionDate], [CreatedBy], [Note], [BalanceBefore], [OriginalTransactionId]) VALUES (7, 10, N'IN', 1.000, 1.000, 25000.00, N'ORDER', 7, '2026-08-14T03:51:35.018', 1, N'Nhập kho theo đơn PO00024', 0.000, NULL);
INSERT INTO dbo.[StockTransactions] ([Id], [ProductId], [TransactionType], [Quantity], [BalanceAfter], [UnitPrice], [RefType], [RefId], [TransactionDate], [CreatedBy], [Note], [BalanceBefore], [OriginalTransactionId]) VALUES (9, 1, N'ADJUST', 48.000, 50.000, 150.00, N'MANUAL', NULL, '2026-08-27T14:39:44.517', 17, N'Kiểm kê thử E2E', 2.000, NULL);
INSERT INTO dbo.[StockTransactions] ([Id], [ProductId], [TransactionType], [Quantity], [BalanceAfter], [UnitPrice], [RefType], [RefId], [TransactionDate], [CreatedBy], [Note], [BalanceBefore], [OriginalTransactionId]) VALUES (10, 1, N'REVERSAL', -48.000, 2.000, 150.00, N'REVERSAL', NULL, '2026-08-27T14:40:02.140', 17, N'Đảo bút toán #9: Đếm nhầm kệ, kiểm kê lại', 50.000, 9);
INSERT INTO dbo.[StockTransactions] ([Id], [ProductId], [TransactionType], [Quantity], [BalanceAfter], [UnitPrice], [RefType], [RefId], [TransactionDate], [CreatedBy], [Note], [BalanceBefore], [OriginalTransactionId]) VALUES (35, 1, N'IN', 1.000, 3.000, 150.00, N'ORDER', 38, '2026-10-06T07:06:40.536', 1, N'Nhập kho theo đơn PO00073', 2.000, NULL);
INSERT INTO dbo.[StockTransactions] ([Id], [ProductId], [TransactionType], [Quantity], [BalanceAfter], [UnitPrice], [RefType], [RefId], [TransactionDate], [CreatedBy], [Note], [BalanceBefore], [OriginalTransactionId]) VALUES (36, 2, N'IN', 2.000, 2.000, 85000.00, N'ORDER', 38, '2026-10-06T07:06:40.536', 1, N'Nhập kho theo đơn PO00073', 0.000, NULL);
INSERT INTO dbo.[StockTransactions] ([Id], [ProductId], [TransactionType], [Quantity], [BalanceAfter], [UnitPrice], [RefType], [RefId], [TransactionDate], [CreatedBy], [Note], [BalanceBefore], [OriginalTransactionId]) VALUES (37, 10, N'REVERSAL', -1.000, 0.000, 25000.00, N'REVERSAL', NULL, '2026-10-06T07:08:43.584', 1, N'Đảo bút toán #7: go nham', 1.000, 7);
SET IDENTITY_INSERT dbo.[StockTransactions] OFF;
GO

SET IDENTITY_INSERT dbo.[AuditLogs] ON;
INSERT INTO dbo.[AuditLogs] ([Id], [EntityName], [EntityId], [Action], [BeforeJson], [AfterJson], [UserId], [UserName], [IpAddress], [Description], [CreatedAt]) VALUES (1, N'User', NULL, N'LOGIN_FAILED', NULL, NULL, NULL, NULL, N'::1', N'Đăng nhập thất bại cho ''admin'': sai mật khẩu', '2026-08-27T14:32:44.711');
INSERT INTO dbo.[AuditLogs] ([Id], [EntityName], [EntityId], [Action], [BeforeJson], [AfterJson], [UserId], [UserName], [IpAddress], [Description], [CreatedAt]) VALUES (2, N'User', N'17', N'LOGIN', NULL, NULL, 17, N'_e2e', N'::1', N'Đăng nhập thành công (Admin, Purchasing, Warehouse)', '2026-08-27T14:33:34.866');
INSERT INTO dbo.[AuditLogs] ([Id], [EntityName], [EntityId], [Action], [BeforeJson], [AfterJson], [UserId], [UserName], [IpAddress], [Description], [CreatedAt]) VALUES (3, N'User', N'17', N'LOGIN', NULL, NULL, 17, N'_e2e', N'::1', N'Đăng nhập thành công (Admin, Purchasing, Warehouse)', '2026-08-27T14:38:06.042');
INSERT INTO dbo.[AuditLogs] ([Id], [EntityName], [EntityId], [Action], [BeforeJson], [AfterJson], [UserId], [UserName], [IpAddress], [Description], [CreatedAt]) VALUES (4, N'Order', N'15', N'ORDER_CONFIRM', NULL, NULL, 17, N'_e2e', N'::1', N'Xác nhận đơn PO00050', '2026-08-27T14:39:05.143');
INSERT INTO dbo.[AuditLogs] ([Id], [EntityName], [EntityId], [Action], [BeforeJson], [AfterJson], [UserId], [UserName], [IpAddress], [Description], [CreatedAt]) VALUES (5, N'Order', N'15', N'ORDER_CANCEL', N'{"Status":"CONFIRMED"}', N'{"Status":"CANCELLED","Reason":"NCC báo hết hàng, chuyển sang NCC khác"}', 17, N'_e2e', N'::1', N'Huỷ đơn PO00050: NCC báo hết hàng, chuyển sang NCC khác', '2026-08-27T14:39:05.453');
INSERT INTO dbo.[AuditLogs] ([Id], [EntityName], [EntityId], [Action], [BeforeJson], [AfterJson], [UserId], [UserName], [IpAddress], [Description], [CreatedAt]) VALUES (6, N'Stock', N'LK001', N'STOCK_ADJUST', N'{"Quantity":2.000}', N'{"Quantity":50,"Delta":48.000}', 17, N'_e2e', N'::1', N'Điều chỉnh tồn LK001: 2 → 50 (+48). Kiểm kê thử E2E', '2026-08-27T14:39:44.580');
INSERT INTO dbo.[AuditLogs] ([Id], [EntityName], [EntityId], [Action], [BeforeJson], [AfterJson], [UserId], [UserName], [IpAddress], [Description], [CreatedAt]) VALUES (7, N'StockTransaction', N'9', N'STOCK_REVERSAL', N'{"TransactionType":"ADJUST","Quantity":48.000,"BalanceAfter":50.000}', N'{"Quantity":-48.000,"BalanceAfter":2.000}', 17, N'_e2e', N'::1', N'Đảo bút toán #9 của LK001: Đếm nhầm kệ, kiểm kê lại', '2026-08-27T14:40:02.150');
INSERT INTO dbo.[AuditLogs] ([Id], [EntityName], [EntityId], [Action], [BeforeJson], [AfterJson], [UserId], [UserName], [IpAddress], [Description], [CreatedAt]) VALUES (8, N'Role', NULL, N'CREATE', NULL, N'{"Code":"Accounting","Name":"Kế toán","Description":"Xem báo cáo tài chính"}', 17, N'_e2e', N'::1', N'Tạo vai trò ''Accounting''', '2026-08-27T14:40:56.328');
INSERT INTO dbo.[AuditLogs] ([Id], [EntityName], [EntityId], [Action], [BeforeJson], [AfterJson], [UserId], [UserName], [IpAddress], [Description], [CreatedAt]) VALUES (9, N'User', N'18', N'LOGIN', NULL, NULL, 18, N'_e2eview', N'::1', N'Đăng nhập thành công (User)', '2026-08-27T14:41:19.797');
INSERT INTO dbo.[AuditLogs] ([Id], [EntityName], [EntityId], [Action], [BeforeJson], [AfterJson], [UserId], [UserName], [IpAddress], [Description], [CreatedAt]) VALUES (10, N'User', N'18', N'UPDATE', N'{"FullName":"Test chi xem","Roles":["User"],"IsActive":true}', N'{"FullName":"Test chi xem","Roles":["User"],"IsActive":false}', 17, N'_e2e', N'::1', N'Cập nhật tài khoản ''_e2eview''', '2026-08-27T14:41:41.266');
INSERT INTO dbo.[AuditLogs] ([Id], [EntityName], [EntityId], [Action], [BeforeJson], [AfterJson], [UserId], [UserName], [IpAddress], [Description], [CreatedAt]) VALUES (11, N'User', N'18', N'LOCK', NULL, NULL, 17, N'_e2e', N'::1', N'Khoá tài khoản ''_e2eview'' và thu hồi toàn bộ phiên đăng nhập', '2026-08-27T14:41:41.266');
INSERT INTO dbo.[AuditLogs] ([Id], [EntityName], [EntityId], [Action], [BeforeJson], [AfterJson], [UserId], [UserName], [IpAddress], [Description], [CreatedAt]) VALUES (12, N'RefreshToken', N'4', N'LOGIN_FAILED', NULL, NULL, NULL, NULL, N'::1', N'Phát hiện refresh token đã thu hồi bị dùng lại - đã thu hồi toàn bộ phiên của tài khoản', '2026-08-27T14:41:41.550');
INSERT INTO dbo.[AuditLogs] ([Id], [EntityName], [EntityId], [Action], [BeforeJson], [AfterJson], [UserId], [UserName], [IpAddress], [Description], [CreatedAt]) VALUES (13, N'User', N'17', N'LOGIN_FAILED', NULL, NULL, NULL, NULL, N'::1', N'Đăng nhập thất bại cho ''_e2e'': sai mật khẩu', '2026-08-27T14:45:12.269');
INSERT INTO dbo.[AuditLogs] ([Id], [EntityName], [EntityId], [Action], [BeforeJson], [AfterJson], [UserId], [UserName], [IpAddress], [Description], [CreatedAt]) VALUES (14, N'User', N'17', N'LOGIN', NULL, NULL, 17, N'_e2e', N'::1', N'Đăng nhập thành công (Admin, Purchasing, Warehouse)', '2026-08-27T14:45:13.356');
INSERT INTO dbo.[AuditLogs] ([Id], [EntityName], [EntityId], [Action], [BeforeJson], [AfterJson], [UserId], [UserName], [IpAddress], [Description], [CreatedAt]) VALUES (15, N'User', N'1', N'LOGIN_FAILED', NULL, NULL, NULL, NULL, N'::1', N'Đăng nhập thất bại cho ''admin'': sai mật khẩu', '2026-10-03T13:42:31.069');
INSERT INTO dbo.[AuditLogs] ([Id], [EntityName], [EntityId], [Action], [BeforeJson], [AfterJson], [UserId], [UserName], [IpAddress], [Description], [CreatedAt]) VALUES (16, N'User', N'1', N'LOGIN_FAILED', NULL, NULL, NULL, NULL, N'::1', N'Đăng nhập thất bại cho ''admin'': sai mật khẩu', '2026-10-03T13:43:27.847');
INSERT INTO dbo.[AuditLogs] ([Id], [EntityName], [EntityId], [Action], [BeforeJson], [AfterJson], [UserId], [UserName], [IpAddress], [Description], [CreatedAt]) VALUES (17, N'User', N'1', N'LOGIN_FAILED', NULL, NULL, NULL, NULL, N'::1', N'Đăng nhập thất bại cho ''admin'': sai mật khẩu', '2026-10-03T13:43:28.463');
INSERT INTO dbo.[AuditLogs] ([Id], [EntityName], [EntityId], [Action], [BeforeJson], [AfterJson], [UserId], [UserName], [IpAddress], [Description], [CreatedAt]) VALUES (18, N'User', N'1', N'LOGIN_FAILED', NULL, NULL, NULL, NULL, N'::1', N'Đăng nhập thất bại cho ''admin'': sai mật khẩu', '2026-10-03T13:43:29.356');
INSERT INTO dbo.[AuditLogs] ([Id], [EntityName], [EntityId], [Action], [BeforeJson], [AfterJson], [UserId], [UserName], [IpAddress], [Description], [CreatedAt]) VALUES (19, N'User', N'1', N'LOGIN_FAILED', NULL, NULL, NULL, NULL, N'::1', N'Đăng nhập thất bại cho ''admin'': sai mật khẩu', '2026-10-03T13:43:30.021');
INSERT INTO dbo.[AuditLogs] ([Id], [EntityName], [EntityId], [Action], [BeforeJson], [AfterJson], [UserId], [UserName], [IpAddress], [Description], [CreatedAt]) VALUES (20, N'User', N'1', N'LOGIN_FAILED', NULL, NULL, NULL, NULL, N'::1', N'Đăng nhập thất bại cho ''admin'': sai mật khẩu', '2026-10-03T13:43:30.418');
INSERT INTO dbo.[AuditLogs] ([Id], [EntityName], [EntityId], [Action], [BeforeJson], [AfterJson], [UserId], [UserName], [IpAddress], [Description], [CreatedAt]) VALUES (21, N'User', N'1', N'LOGIN_FAILED', NULL, NULL, NULL, NULL, N'::1', N'Đăng nhập thất bại cho ''admin'': sai mật khẩu', '2026-10-03T13:43:30.742');
INSERT INTO dbo.[AuditLogs] ([Id], [EntityName], [EntityId], [Action], [BeforeJson], [AfterJson], [UserId], [UserName], [IpAddress], [Description], [CreatedAt]) VALUES (22, N'User', N'1', N'LOGIN_FAILED', NULL, NULL, NULL, NULL, N'::1', N'Đăng nhập thất bại cho ''admin'': sai mật khẩu', '2026-10-03T13:43:31.102');
INSERT INTO dbo.[AuditLogs] ([Id], [EntityName], [EntityId], [Action], [BeforeJson], [AfterJson], [UserId], [UserName], [IpAddress], [Description], [CreatedAt]) VALUES (23, N'User', N'1', N'LOGIN_FAILED', NULL, NULL, NULL, NULL, N'::1', N'Đăng nhập thất bại cho ''admin'': sai mật khẩu', '2026-10-03T13:43:31.510');
INSERT INTO dbo.[AuditLogs] ([Id], [EntityName], [EntityId], [Action], [BeforeJson], [AfterJson], [UserId], [UserName], [IpAddress], [Description], [CreatedAt]) VALUES (24, N'User', N'1', N'LOGIN_FAILED', NULL, NULL, NULL, NULL, N'::1', N'Đăng nhập thất bại cho ''admin'': sai mật khẩu', '2026-10-03T13:43:31.839');
INSERT INTO dbo.[AuditLogs] ([Id], [EntityName], [EntityId], [Action], [BeforeJson], [AfterJson], [UserId], [UserName], [IpAddress], [Description], [CreatedAt]) VALUES (25, N'User', N'1', N'LOGIN_FAILED', NULL, NULL, NULL, NULL, N'::1', N'Đăng nhập thất bại cho ''admin'': sai mật khẩu', '2026-10-03T13:43:32.181');
INSERT INTO dbo.[AuditLogs] ([Id], [EntityName], [EntityId], [Action], [BeforeJson], [AfterJson], [UserId], [UserName], [IpAddress], [Description], [CreatedAt]) VALUES (26, N'User', N'1', N'LOGIN_FAILED', NULL, NULL, NULL, NULL, N'::1', N'Đăng nhập thất bại cho ''admin'': sai mật khẩu', '2026-10-03T13:43:32.521');
INSERT INTO dbo.[AuditLogs] ([Id], [EntityName], [EntityId], [Action], [BeforeJson], [AfterJson], [UserId], [UserName], [IpAddress], [Description], [CreatedAt]) VALUES (27, N'User', N'1', N'LOGIN_FAILED', NULL, NULL, NULL, NULL, N'::1', N'Đăng nhập thất bại cho ''admin'': sai mật khẩu', '2026-10-03T13:43:32.823');
INSERT INTO dbo.[AuditLogs] ([Id], [EntityName], [EntityId], [Action], [BeforeJson], [AfterJson], [UserId], [UserName], [IpAddress], [Description], [CreatedAt]) VALUES (28, N'User', N'1', N'LOGIN_FAILED', NULL, NULL, NULL, NULL, N'127.0.0.1', N'Đăng nhập thất bại cho ''admin'': sai mật khẩu', '2026-10-03T13:44:09.974');
INSERT INTO dbo.[AuditLogs] ([Id], [EntityName], [EntityId], [Action], [BeforeJson], [AfterJson], [UserId], [UserName], [IpAddress], [Description], [CreatedAt]) VALUES (29, N'User', N'1', N'LOGIN_FAILED', NULL, NULL, NULL, NULL, N'127.0.0.1', N'Đăng nhập thất bại cho ''admin'': sai mật khẩu', '2026-10-03T13:44:10.302');
INSERT INTO dbo.[AuditLogs] ([Id], [EntityName], [EntityId], [Action], [BeforeJson], [AfterJson], [UserId], [UserName], [IpAddress], [Description], [CreatedAt]) VALUES (30, N'User', N'1', N'LOGIN_FAILED', NULL, NULL, NULL, NULL, N'127.0.0.1', N'Đăng nhập thất bại cho ''admin'': sai mật khẩu', '2026-10-03T13:44:10.564');
INSERT INTO dbo.[AuditLogs] ([Id], [EntityName], [EntityId], [Action], [BeforeJson], [AfterJson], [UserId], [UserName], [IpAddress], [Description], [CreatedAt]) VALUES (31, N'User', N'1', N'LOGIN_FAILED', NULL, NULL, NULL, NULL, N'127.0.0.1', N'Đăng nhập thất bại cho ''admin'': sai mật khẩu', '2026-10-03T13:44:10.851');
INSERT INTO dbo.[AuditLogs] ([Id], [EntityName], [EntityId], [Action], [BeforeJson], [AfterJson], [UserId], [UserName], [IpAddress], [Description], [CreatedAt]) VALUES (32, N'User', N'1', N'LOGIN_FAILED', NULL, NULL, NULL, NULL, N'127.0.0.1', N'Đăng nhập thất bại cho ''admin'': sai mật khẩu', '2026-10-03T13:44:11.028');
INSERT INTO dbo.[AuditLogs] ([Id], [EntityName], [EntityId], [Action], [BeforeJson], [AfterJson], [UserId], [UserName], [IpAddress], [Description], [CreatedAt]) VALUES (33, N'User', N'1', N'LOGIN_FAILED', NULL, NULL, NULL, NULL, N'127.0.0.1', N'Đăng nhập thất bại cho ''admin'': sai mật khẩu', '2026-10-03T13:44:11.270');
INSERT INTO dbo.[AuditLogs] ([Id], [EntityName], [EntityId], [Action], [BeforeJson], [AfterJson], [UserId], [UserName], [IpAddress], [Description], [CreatedAt]) VALUES (34, N'User', N'1', N'LOGIN_FAILED', NULL, NULL, NULL, NULL, N'127.0.0.1', N'Đăng nhập thất bại cho ''admin'': sai mật khẩu', '2026-10-03T13:44:11.560');
INSERT INTO dbo.[AuditLogs] ([Id], [EntityName], [EntityId], [Action], [BeforeJson], [AfterJson], [UserId], [UserName], [IpAddress], [Description], [CreatedAt]) VALUES (35, N'User', N'1', N'LOGIN_FAILED', NULL, NULL, NULL, NULL, N'127.0.0.1', N'Đăng nhập thất bại cho ''admin'': sai mật khẩu', '2026-10-03T13:44:11.821');
INSERT INTO dbo.[AuditLogs] ([Id], [EntityName], [EntityId], [Action], [BeforeJson], [AfterJson], [UserId], [UserName], [IpAddress], [Description], [CreatedAt]) VALUES (36, N'User', N'1', N'LOGIN_FAILED', NULL, NULL, NULL, NULL, N'127.0.0.1', N'Đăng nhập thất bại cho ''admin'': sai mật khẩu', '2026-10-03T13:44:12.079');
INSERT INTO dbo.[AuditLogs] ([Id], [EntityName], [EntityId], [Action], [BeforeJson], [AfterJson], [UserId], [UserName], [IpAddress], [Description], [CreatedAt]) VALUES (37, N'User', N'1', N'LOGIN_FAILED', NULL, NULL, NULL, NULL, N'127.0.0.1', N'Đăng nhập thất bại cho ''admin'': sai mật khẩu', '2026-10-03T13:44:12.320');
INSERT INTO dbo.[AuditLogs] ([Id], [EntityName], [EntityId], [Action], [BeforeJson], [AfterJson], [UserId], [UserName], [IpAddress], [Description], [CreatedAt]) VALUES (38, N'User', NULL, N'LOGIN_FAILED', NULL, NULL, NULL, NULL, N'127.0.0.1', N'Đăng nhập thất bại cho ''zzz_khong_ton_tai'': tên đăng nhập không tồn tại', '2026-10-03T13:53:23.904');
INSERT INTO dbo.[AuditLogs] ([Id], [EntityName], [EntityId], [Action], [BeforeJson], [AfterJson], [UserId], [UserName], [IpAddress], [Description], [CreatedAt]) VALUES (43, N'User', N'19', N'LOGIN_FAILED', NULL, NULL, NULL, NULL, N'127.0.0.1', N'Đăng nhập thất bại cho ''zz_admin'': sai mật khẩu', '2026-10-04T05:53:39.792');
INSERT INTO dbo.[AuditLogs] ([Id], [EntityName], [EntityId], [Action], [BeforeJson], [AfterJson], [UserId], [UserName], [IpAddress], [Description], [CreatedAt]) VALUES (57, N'User', N'19', N'LOGIN_FAILED', NULL, NULL, NULL, NULL, N'127.0.0.1', N'Đăng nhập thất bại cho ''zz_admin'': sai mật khẩu', '2026-10-04T05:54:36.884');
INSERT INTO dbo.[AuditLogs] ([Id], [EntityName], [EntityId], [Action], [BeforeJson], [AfterJson], [UserId], [UserName], [IpAddress], [Description], [CreatedAt]) VALUES (69, N'User', N'22', N'LOGIN_FAILED', NULL, NULL, NULL, NULL, N'127.0.0.1', N'Đăng nhập thất bại cho ''zz_xem'': sai mật khẩu', '2026-10-04T05:54:57.813');
INSERT INTO dbo.[AuditLogs] ([Id], [EntityName], [EntityId], [Action], [BeforeJson], [AfterJson], [UserId], [UserName], [IpAddress], [Description], [CreatedAt]) VALUES (81, N'User', N'19', N'LOGIN_FAILED', NULL, NULL, NULL, NULL, N'127.0.0.1', N'Đăng nhập thất bại cho ''zz_admin'': sai mật khẩu', '2026-10-04T05:56:50.980');
INSERT INTO dbo.[AuditLogs] ([Id], [EntityName], [EntityId], [Action], [BeforeJson], [AfterJson], [UserId], [UserName], [IpAddress], [Description], [CreatedAt]) VALUES (94, N'User', N'22', N'LOGIN_FAILED', NULL, NULL, NULL, NULL, N'127.0.0.1', N'Đăng nhập thất bại cho ''zz_xem'': sai mật khẩu', '2026-10-04T05:57:10.878');
INSERT INTO dbo.[AuditLogs] ([Id], [EntityName], [EntityId], [Action], [BeforeJson], [AfterJson], [UserId], [UserName], [IpAddress], [Description], [CreatedAt]) VALUES (237, N'User', N'1', N'LOGIN_FAILED', NULL, NULL, NULL, NULL, N'127.0.0.1', N'Đăng nhập thất bại cho ''admin'': sai mật khẩu', '2026-10-04T06:54:18.893');
INSERT INTO dbo.[AuditLogs] ([Id], [EntityName], [EntityId], [Action], [BeforeJson], [AfterJson], [UserId], [UserName], [IpAddress], [Description], [CreatedAt]) VALUES (239, N'User', N'1', N'LOGIN_FAILED', NULL, NULL, NULL, NULL, N'::1', N'Đăng nhập thất bại cho ''admin'': sai mật khẩu', '2026-10-06T07:05:06.355');
INSERT INTO dbo.[AuditLogs] ([Id], [EntityName], [EntityId], [Action], [BeforeJson], [AfterJson], [UserId], [UserName], [IpAddress], [Description], [CreatedAt]) VALUES (240, N'User', N'1', N'LOGIN_FAILED', NULL, NULL, NULL, NULL, N'::1', N'Đăng nhập thất bại cho ''admin'': sai mật khẩu', '2026-10-06T07:05:06.360');
INSERT INTO dbo.[AuditLogs] ([Id], [EntityName], [EntityId], [Action], [BeforeJson], [AfterJson], [UserId], [UserName], [IpAddress], [Description], [CreatedAt]) VALUES (241, N'User', N'1', N'LOGIN', NULL, NULL, 1, N'admin', N'::1', N'Đăng nhập thành công (Admin)', '2026-10-06T07:05:14.208');
INSERT INTO dbo.[AuditLogs] ([Id], [EntityName], [EntityId], [Action], [BeforeJson], [AfterJson], [UserId], [UserName], [IpAddress], [Description], [CreatedAt]) VALUES (242, N'Order', N'38', N'ORDER_CONFIRM', NULL, NULL, 1, N'admin', N'::1', N'Xác nhận đơn PO00073', '2026-10-06T07:06:36.455');
INSERT INTO dbo.[AuditLogs] ([Id], [EntityName], [EntityId], [Action], [BeforeJson], [AfterJson], [UserId], [UserName], [IpAddress], [Description], [CreatedAt]) VALUES (243, N'Order', N'38', N'ORDER_RECEIVE', NULL, N'{"Lines":2}', 1, N'admin', N'::1', N'Nhập kho theo đơn PO00073 (2 mặt hàng)', '2026-10-06T07:06:40.995');
INSERT INTO dbo.[AuditLogs] ([Id], [EntityName], [EntityId], [Action], [BeforeJson], [AfterJson], [UserId], [UserName], [IpAddress], [Description], [CreatedAt]) VALUES (244, N'StockTransaction', N'7', N'STOCK_REVERSAL', N'{"TransactionType":"IN","Quantity":1.000,"BalanceAfter":1.000}', N'{"Quantity":-1.000,"BalanceAfter":0.000}', 1, N'admin', N'::1', N'Đảo bút toán #7 của LK010: go nham', '2026-10-06T07:08:43.608');
INSERT INTO dbo.[AuditLogs] ([Id], [EntityName], [EntityId], [Action], [BeforeJson], [AfterJson], [UserId], [UserName], [IpAddress], [Description], [CreatedAt]) VALUES (245, N'User', N'1', N'LOGOUT', NULL, NULL, 1, N'admin', N'::1', N'Đăng xuất', '2026-10-06T07:28:28.551');
INSERT INTO dbo.[AuditLogs] ([Id], [EntityName], [EntityId], [Action], [BeforeJson], [AfterJson], [UserId], [UserName], [IpAddress], [Description], [CreatedAt]) VALUES (246, N'User', N'1', N'LOGIN', NULL, NULL, 1, N'admin', N'127.0.0.1', N'Đăng nhập thành công (Admin)', '2026-10-06T07:41:05.227');
INSERT INTO dbo.[AuditLogs] ([Id], [EntityName], [EntityId], [Action], [BeforeJson], [AfterJson], [UserId], [UserName], [IpAddress], [Description], [CreatedAt]) VALUES (247, N'User', N'1', N'LOGIN_FAILED', NULL, NULL, NULL, NULL, N'127.0.0.1', N'Đăng nhập thất bại cho ''admin'': sai mật khẩu', '2026-10-06T07:41:06.532');
INSERT INTO dbo.[AuditLogs] ([Id], [EntityName], [EntityId], [Action], [BeforeJson], [AfterJson], [UserId], [UserName], [IpAddress], [Description], [CreatedAt]) VALUES (248, N'User', N'1', N'LOGIN', NULL, NULL, 1, N'admin', N'127.0.0.1', N'Đăng nhập thành công (Admin)', '2026-10-06T07:41:11.756');
INSERT INTO dbo.[AuditLogs] ([Id], [EntityName], [EntityId], [Action], [BeforeJson], [AfterJson], [UserId], [UserName], [IpAddress], [Description], [CreatedAt]) VALUES (249, N'User', N'1', N'LOGIN_FAILED', NULL, NULL, NULL, NULL, N'127.0.0.1', N'Đăng nhập thất bại cho ''admin'': sai mật khẩu', '2026-10-06T07:41:12.146');
INSERT INTO dbo.[AuditLogs] ([Id], [EntityName], [EntityId], [Action], [BeforeJson], [AfterJson], [UserId], [UserName], [IpAddress], [Description], [CreatedAt]) VALUES (250, N'User', N'1', N'LOGIN_FAILED', NULL, NULL, NULL, NULL, N'127.0.0.1', N'Đăng nhập thất bại cho ''admin'': sai mật khẩu', '2026-10-06T07:41:12.381');
SET IDENTITY_INSERT dbo.[AuditLogs] OFF;
GO

DBCC CHECKIDENT ('dbo.[AuditLogs]', RESEED) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('dbo.[OrderProducts]', RESEED) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('dbo.[OrderStatusHistory]', RESEED) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('dbo.[Orders]', RESEED) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('dbo.[Products]', RESEED) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('dbo.[Roles]', RESEED) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('dbo.[Stock]', RESEED) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('dbo.[StockTransactions]', RESEED) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('dbo.[UserRoles]', RESEED) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('dbo.[Users]', RESEED) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('dbo.[Vendors]', RESEED) WITH NO_INFOMSGS;

DECLARE @next BIGINT, @sql NVARCHAR(400);

SELECT @next = ISNULL(MAX(CAST(SUBSTRING(OrderNo, 3, 20) AS BIGINT)), 0) + 1 FROM dbo.Orders;
SET @sql = N'ALTER SEQUENCE dbo.Seq_OrderNo RESTART WITH ' + CAST(@next AS NVARCHAR(20));
EXEC sp_executesql @sql;

SELECT @next = ISNULL(MAX(CAST(SUBSTRING(VendorCode, 4, 20) AS BIGINT)), 0) + 1 FROM dbo.Vendors
WHERE VendorCode LIKE 'NCC%' AND ISNUMERIC(SUBSTRING(VendorCode, 4, 20)) = 1;
SET @sql = N'ALTER SEQUENCE dbo.Seq_VendorCode RESTART WITH ' + CAST(@next AS NVARCHAR(20));
EXEC sp_executesql @sql;
GO

SET NOEXEC OFF;
GO
