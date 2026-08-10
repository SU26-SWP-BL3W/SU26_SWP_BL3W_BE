# Quy trình làm việc & khung dự án SEAL (nhóm BL3W)

## 1. Cấu trúc repo

Dự án tách **2 repo riêng**, cùng nằm dưới org GitHub `SU26-SWP-BL3W`:

| Repo | Nội dung | Vai trò |
| --- | --- | --- |
| `SU26_SWP_BL3W` | Backend (.NET 10, Clean Architecture) | mỗi người thêm feature/handler flow mình phụ trách |
| `SU26_SWP_BL3W_FE` | Frontend (Next.js, kiến trúc MVVM) | UI dựng lại hoàn toàn mới, không dùng lại giao diện cũ |

Database dùng chung schema đã có sẵn ở `SU26_SWP_BL3W/database/` (docker-compose + seed của Phúc) — Backend dùng EF Code-First (migration `InitialCreate`) làm nguồn chân lý, đã đối chiếu khớp 19/19 bảng với schema đó.

## 2. Nhánh & quy trình PR

- Nhánh `main` = production-ready, `dev` = nhánh tích hợp hằng ngày.
- Mỗi flow làm trên **1 nhánh riêng** tách từ `dev` (không code thẳng lên `dev`/`main`), tên nhánh kiểu `TenNguoi_Flow_MoTa` (ví dụ `Loc_Scaffold_BaseSetup`, `Phuc_FLow_3_QuanLyDoiThi`).
- Xong việc → mở Pull Request **vào `dev`** → có người review trước khi merge. Không tự merge thẳng.
- `dev` → `main` chỉ merge khi đã ổn định (không phải mỗi PR nhỏ).
- CI (GitHub Actions) tự chạy build+test cho cả 2 repo mỗi khi push/mở PR vào `main`/`dev`.

## 3. Khung Backend (`SU26_SWP_BL3W/backend/`)

Clean Architecture, 5 project:

```
backend/
├── SEAL.Domain/          # Entity (19 bảng), Result/BaseException, BaseResponse — không phụ thuộc project nào khác
├── SEAL.Application/     # Interfaces (IUnitOfWork, ICurrentUserService...), Commons — nơi MỖI FLOW thêm Features/<TenFlow>/
├── SEAL.Infrastructure/  # DbContext + Configurations, Repository, UnitOfWork, TokenService, EmailService, Storage
├── SEAL_Backend/         # Program.cs, Middlewares, Filters phân quyền — nơi MỖI FLOW thêm Controllers/<TenFlow>Controller.cs
└── SEAL.Tests.Application/
```

**Đã có sẵn trong khung** (dùng chung, không tự viết lại): JWT auth, Result pattern (`Result<T>`, trả lỗi qua `BaseException`), `BaseResponse<T>` bọc mọi response, `IUnitOfWork.GetRepository<T>()` cho CRUD, `ICurrentUserService.UserId` lấy user đang đăng nhập, filter phân quyền `[AdminAuthorize]`/`[EventRoleAuthorize]`, `GlobalExceptionMiddleware` tự bắt lỗi trả JSON chuẩn.

**Cách thêm 1 flow mới** (chi tiết xem `backend/docs/DEVELOPMENT_WORKFLOW.md`): tạo entity nếu cần (đã có sẵn 19 entity chuẩn, thường không cần tạo mới) → thêm `IEntityTypeConfiguration` nếu entity mới → viết CQRS slice trong `SEAL.Application/Features/<TenFlow>/` (Command/Query + Handler + Validator theo MediatR) → thêm Controller gọi `IMediator.Send(...)`.

## 4. Khung Frontend (`SU26_SWP_BL3W_FE/`)

Next.js 16 (App Router) + TypeScript-only + Tailwind v4 + React Query, tổ chức theo **MVVM**:

```
src/
├── app/          # Route Next.js — CHỈ import và render 1 View tương ứng, không chứa logic
├── views/        # Màn hình — ghép ViewModel + component lại thành 1 trang hoàn chỉnh
├── viewModels/   # 1 custom hook / màn hình — chứa toàn bộ state + logic, View chỉ đọc ra render
├── models/       # apiClient.ts (axios, tự gắn token + tự refresh khi hết hạn) + types.ts (BaseResponse, ApiError)
├── components/ui/# UI dùng chung (Button, Card, Input, Badge) — mọi feature dùng lại, KHÔNG tự định nghĩa Button riêng
├── providers/    # QueryProvider (React Query)
└── styles/       # tokens.css — bảng màu/spacing dùng chung (đang là placeholder, chờ thiết kế UI thật)
```

**Quy tắc bắt buộc** (để tránh lặp lại tình trạng "mò kiếm code" của FE cũ — 2 hệ auth song song, 2 API client, code cũ chồng code mới):
1. Chỉ TypeScript (`.tsx`/`.ts`), không `.jsx`/`.js`.
2. 1 API client duy nhất: `models/apiClient.ts` — mọi lời gọi API đi qua đây.
3. Mỗi màn hình = 1 ViewModel (hook `useXxxViewModel`) + 1 View (`XxxView.tsx`) + 1 route mỏng trong `app/` render View đó. Xem ví dụ mẫu: `viewModels/useBackendHealthViewModel.ts` + `views/HomeView.tsx` + `app/page.tsx`.
4. UI dùng chung chỉ sống ở `components/ui/`.
5. `viewModels/useAuth.ts` hiện chỉ đọc phiên đăng nhập đã lưu (chưa có login/register vì Backend chưa có endpoint Auth) — flow Auth thêm mutation hook (`useLogin`, `useRegister`...) vào `models/`/`viewModels/` riêng khi build, không sửa cấu trúc chung này.

## 5. Đã kiểm chứng chạy thật

- Backend: `dotnet build backend/SEAL_Backend.slnx` xanh; chạy `dotnet run --project backend/SEAL_Backend` lên thật, `GET /health` trả `{"status":"ok"}`, `/swagger` load được. Migration `InitialCreate` sinh ra khớp đúng 19 bảng với schema SQL đã có sẵn.
- Frontend: `npm run build` xanh; chạy `npm run dev`, trang chủ gọi thật `GET /health` sang Backend đang chạy và hiển thị kết quả — xác nhận 2 chiều FE⇄BE hoạt động.

## 6. Việc còn để ngỏ cho từng flow

- Backend khung **chưa có bất kỳ Controller/Feature nào** (kể cả Auth) — mỗi flow tự thêm trên nhánh riêng của mình.
- Frontend khung **chưa có route/màn hình thật nào** ngoài trang mẫu kiểm tra kết nối — UI thật sẽ thiết kế lại từ đầu.
- `Program.cs` backend đã bỏ dòng `AddHttpClient<CreateUserCommandHandler>()` (dùng gọi FPT Mock API lúc đăng ký) vì handler đó chưa tồn tại — flow Auth cần thêm lại dòng này khi build tính năng đăng ký sinh viên FPT.
