
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
