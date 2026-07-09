# TodoFlow

TodoFlow là app quản lý task full-stack gồm React frontend và ASP.NET Core backend. Backend hiện có auth, task/category/subtask/tag, recurring task, kéo-thả sắp xếp, sharing realtime, notification và reminder qua Gmail SMTP.

## Stack

- Frontend: React, Vite, TanStack Router, Zustand, SignalR client, dnd-kit.
- Backend: ASP.NET Core Web API .NET 10, EF Core, SQL Server, JWT Bearer, SignalR.
- Database local: SQL Server 2022 qua Docker Compose.

## Chức năng đã có

- Phase 1-3: đăng ký, đăng nhập, refresh token, CRUD task/category/subtask/tag, lọc/sắp xếp/phân trang, soft delete, Swagger, validation tiếng Việt.
- Phase 4-5: frontend nối API thật, auth flow, dashboard task, toast lỗi/thành công.
- Phase 6: chia sẻ task, quyền xem/sửa, danh sách task được share, cập nhật realtime qua SignalR.
- Phase 7: notification, reminder in-app/email/both, background worker xử lý reminder.
- Phase 8: cấu hình dev rõ hơn, giảm log SQL, tài liệu chạy dự án.
- Phase 9: ghi chú subtask, recurring tasks, kéo-thả task giữa các cột bằng dnd-kit.

## Chạy database

```bash
cd Backend
docker compose up -d
```

Database mặc định:

```text
Server=localhost,1433;Database=TodoFlowDb;User Id=sa;Password=Admin123A@;TrustServerCertificate=True;MultipleActiveResultSets=True
```

## Chạy backend

```bash
dotnet restore Backend/TodoApp.Backend.sln
dotnet ef database update \
  --project Backend/TodoApp.Infrastructure \
  --startup-project Backend/TodoApp.Api
dotnet run --project Backend/TodoApp.Api --urls http://localhost:5050
```

Swagger: `http://localhost:5050/swagger`

## Cấu hình Gmail SMTP

Không đưa mật khẩu Gmail hoặc app password vào `appsettings.json`. Dùng user-secrets trong project API:

```bash
cd Backend/TodoApp.Api
dotnet user-secrets init
dotnet user-secrets set "Smtp:Enabled" "true"
dotnet user-secrets set "Smtp:Username" "your-gmail@gmail.com"
dotnet user-secrets set "Smtp:Password" "your-gmail-app-password"
dotnet user-secrets set "Smtp:FromEmail" "your-gmail@gmail.com"
dotnet user-secrets set "Smtp:FromName" "TodoFlow"
```

Gmail cần App Password, không dùng mật khẩu đăng nhập Gmail thường. Email reminder sẽ gửi tới email của user đang sở hữu task.

## Chạy frontend

```bash
cd Frontend
npm install --legacy-peer-deps
cp .env.example .env
npm run dev -- --host 127.0.0.1
```

Frontend mặc định gọi backend tại `http://localhost:5050`. Trên macOS, cổng `5000` thường bị Control Center/AirPlay chiếm nên project dùng `5050` cho dev.

## Kiểm tra

```bash
dotnet build Backend/TodoApp.Backend.sln
dotnet test Backend/TodoApp.Backend.sln
cd Frontend && npm run build
```
