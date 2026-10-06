# Hệ thống quản lý mua hàng và tồn kho trong doanh nghiệp (PO-ZAIKO)

> **Ghi chú:** Hệ thống không có chức năng đăng ký tài khoản. Đây là phần mềm nội bộ
> của doanh nghiệp, tài khoản do Admin cấp cho nhân viên chứ không ai tự đăng ký.
> Sau khi chạy file sql để tạo data mẫu thì dùng tài khoản đã tạo để đăng nhập.
---

## 1. Tạo database

Chạy file : `src/TonKhoApp.WebApi/Database/database_full.sql`

File này gồm cả cấu trúc bảng và data. Data mẫu gồm 3 tài khoản, 5 nhà cung cấp, 10 sản phẩm, 6 đơn hàng,
12 bút toán kho và 56 dòng nhật ký thao tác.

---

## 2. Khai báo kết nối

```bash
cd src/TonKhoApp.WebApi
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost,1433;Database=tonkho_db;User Id=sa;Password=<mật khẩu sa>;TrustServerCertificate=True;"
dotnet user-secrets set "JwtSettings:SecretKey" "chuỗi ngẫu nhiên dài hơn 32 ký tự"
```

## 3. Chạy app

```bash
dotnet run --project src/TonKhoApp.WebApi
```

Mở trình duyệt: **http://localhost:5186**

## 4. Đăng nhập
```
admin / admin123
```
Hai tài khoản còn lại cùng mật khẩu: `kaori` (nhân viên mua hàng), `nguyen` (nhân viên kho).
