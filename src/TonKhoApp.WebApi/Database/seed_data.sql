
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

MERGE dbo.Roles AS target
USING (VALUES
    ('Admin',      N'Quản trị viên',      N'Toàn quyền trên hệ thống'),
    ('Purchasing', N'Nhân viên mua hàng', N'Quản lý danh mục sản phẩm/NCC, lập và xác nhận đơn mua'),
    ('Warehouse',  N'Nhân viên kho',      N'Nhập kho theo đơn, xuất kho, điều chỉnh tồn, đảo bút toán'),
    ('User',       N'Nhân viên',          N'Chỉ xem, không thêm/sửa/xoá')
) AS source (Code, Name, Description)
ON target.Code = source.Code
WHEN MATCHED THEN
    UPDATE SET Name = source.Name, Description = source.Description, IsSystem = 1
WHEN NOT MATCHED THEN
    INSERT (Code, Name, Description, IsSystem)
    VALUES (source.Code, source.Name, source.Description, 1);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE UserId = 'admin')
BEGIN
    INSERT INTO dbo.Users (UserId, UserPassword, FullName, IsActive)
    VALUES ('admin',
            '$2a$10$buwzdElNVwk9my6LMQM4OeUI3.QcNWbBCiD65jK/J.Ud5h06nr8mm',
            N'Quản trị viên', 1);
END
GO

INSERT INTO dbo.UserRoles (UserId, RoleId, AssignedAt)
SELECT u.Id, r.Id, SYSUTCDATETIME()
FROM   dbo.Users u
CROSS  JOIN dbo.Roles r
WHERE  u.UserId = 'admin'
  AND  r.Code = 'Admin'
  AND  NOT EXISTS (
           SELECT 1 FROM dbo.UserRoles ur
           WHERE ur.UserId = u.Id AND ur.RoleId = r.Id);

GO

MERGE dbo.Vendors AS target
USING (VALUES
    ('NCC001', N'Công ty TNHH ABC',            N'123 Nguyễn Trãi, Q.1, TP.HCM'),
    ('NCC002', N'Công ty CP XYZ',              N'456 Lê Lợi, Q.3, TP.HCM'),
    ('NCC003', N'Đại lý Minh Phát',            N'789 Trần Hưng Đạo, Q.5, TP.HCM'),
    ('NCC004', N'Công ty Vật tư Hoàng Long',   N'12 Hai Bà Trưng, Hà Nội'),
    ('NCC005', N'Nhà phân phối Tân Tiến',      N'34 Pasteur, Q.1, TP.HCM')
) AS source (VendorCode, VendorName, Address)
ON target.VendorCode = source.VendorCode OR target.VendorName = source.VendorName
WHEN NOT MATCHED BY TARGET THEN
    INSERT (VendorCode, VendorName, Address, IsActive)
    VALUES (source.VendorCode, source.VendorName, source.Address, 1);
GO

IF EXISTS (SELECT 1 FROM sys.sequences WHERE name = 'Seq_VendorCode')
BEGIN
    DECLARE @maxNcc BIGINT = (
        SELECT ISNULL(MAX(TRY_CAST(SUBSTRING(VendorCode, 4, 20) AS BIGINT)), 0)
        FROM dbo.Vendors WHERE VendorCode LIKE 'NCC%');
    DECLARE @curNcc BIGINT = (
        SELECT CAST(current_value AS BIGINT) FROM sys.sequences WHERE name = 'Seq_VendorCode');

    IF @maxNcc >= @curNcc
    BEGIN

        DECLARE @sqlNcc NVARCHAR(300) =
            N'ALTER SEQUENCE dbo.Seq_VendorCode RESTART WITH ' + CAST(@maxNcc + 1 AS NVARCHAR(20));
        EXEC sp_executesql @sqlNcc;

    END
END
GO

MERGE dbo.Products AS target
USING (VALUES
    ('LK001', N'Ốc vít M5x20 SUS304',              N'PCS',     150, 500),
    ('LK002', N'Bạc đạn 6205-2RS',                 N'PCS',   85000,  20),
    ('LK003', N'Dây đai timing belt HTD-5M',       N'PCS',  120000,  10),
    ('LK004', N'Cảm biến tiệm cận E2E-X5ME1',      N'PCS',  350000,   5),
    ('LK005', N'Xy lanh khí SMC CDU20-50D',        N'PCS',  480000,   5),
    ('LK006', N'Relay trung gian MY2N-GS 24VDC',   N'PCS',   95000,  15),
    ('LK007', N'Động cơ servo 400W Mitsubishi',    N'PCS', 5200000,   2),
    ('LK008', N'PLC Mitsubishi FX3U-32MT',         N'PCS', 8500000,   1),
    ('LK009', N'Nhôm thanh profile 4040 (1m)',     N'PCS',  145000,  30),
    ('LK010', N'Đầu nối khí nhanh KQ2H06-01S',     N'PCS',   25000, 100)
) AS source (ItemCode, ItemName, Unit, Price, MinQuantity)
ON target.ItemCode = source.ItemCode

WHEN MATCHED AND target.MinQuantity = 0 AND source.MinQuantity > 0 THEN
    UPDATE SET MinQuantity = source.MinQuantity

WHEN NOT MATCHED BY TARGET THEN
    INSERT (ItemCode, ItemName, Unit, Price, MinQuantity, IsActive)
    VALUES (source.ItemCode, source.ItemName, source.Unit, source.Price, source.MinQuantity, 1);
GO

GO
