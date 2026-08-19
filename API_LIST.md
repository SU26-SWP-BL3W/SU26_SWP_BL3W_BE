# SEAL API Documentation & Mobile Integration Guide

> **Dành cho Đội ngũ Phát triển Flutter (`seal-mobile`)**  
> Dựa trên mã nguồn Backend (.NET 9 Web API - Clean Architecture) từ repository `SU26_SWP_BL3W_BE`.  
> Tài liệu này hệ thống hóa toàn bộ **23 Controllers / 80+ API Endpoints**, được cấu trúc trực quan theo **5 Luồng nghiệp vụ chính** của ứng dụng SEAL Mobile.

---

## MỤC LỤC
1. [Cấu trúc Chuẩn & Quy tắc Kết nối](#1-cấu-trúc-chuẩn--quy-tắc-kết-nối)
   - 1.1 Base URL & Environment
   - 1.2 Format Phản hồi Chuẩn (`BaseResponse<T>`)
   - 1.3 Phân trang Chuẩn (`PagedResult<T>`)
   - 1.4 Xác thực & Cấp quyền (JWT Bearer & Event Roles)
2. [LUỒNG 1: Auth, Tài khoản & Xác thực Sinh viên](#2-luồng-1-auth-tài-khoản--xác-thực-sinh-viên)
3. [LUỒNG 2: Khám phá Sự kiện, Vòng thi & Hạng mục](#3-luồng-2-khám-phá-sự-kiện-vòng-thi--hạng-mục)
4. [LUỒNG 3: Đội thi & Quản lý Thành viên](#4-luồng-3-đội-thi--quản-lý-thành-viên)
5. [LUỒNG 4: Nộp bài thi, Storage & Chấm điểm Giám khảo](#5-luồng-4-nộp-bài-thi-storage--chấm-điểm-giám-khảo)
6. [LUỒNG 5: Bảng xếp hạng, Kết quả & Phúc khảo](#6-luồng-5-bảng-xếp-hạng-kết-quả--phúc-khảo)
7. [PHỤ LỤC MASTER DATA & MOCK SERVICES](#7-phụ-lục-master-data--mock-services)
8. [Hướng dẫn Tích hợp Dio / Retrofit cho Flutter](#8-hướng-dẫn-tích-hợp-dio--retrofit-cho-flutter)

---

## 1. Cấu trúc Chuẩn & Quy tắc Kết nối

### 1.1 Base URL & Environment
- **Development / Local**: `http://localhost:5000/api` hoặc `http://10.0.2.2:5000/api` (cho Android Emulator)
- **Staging / Render Production**: `https://<your-render-domain>/api`

### 1.2 Format Phản hồi Chuẩn (`BaseResponse<T>`)
Tất cả các API ngoại trừ file stream/download đều bọc kết quả trả về trong `BaseResponse<T>` với cấu trúc JSON:

```json
{
  "data": T,                  // Dữ liệu trả về (Object, Array, hoặc primitive)
  "message": "String",        // Thông điệp phản hồi (ví dụ: "Thành công", "Lỗi...")
  "statusCode": 200,          // Mã trạng thái enum (200, 400, 401, 403, 404, 500)
  "success": true             // Boolean: true nếu statusCode == 200 (OK)
}
```

### 1.3 Phân trang Chuẩn (`PagedResult<T>`)
Đối với các API lấy danh sách có phân trang (`GetAll`), trường `data` trong `BaseResponse` sẽ chứa một object `PagedResult<T>`:

```json
{
  "data": {
    "data": [ ... ],           // Danh sách phần tử của trang hiện tại
    "currentPage": 1,         // Trang hiện tại (1-indexed)
    "pageSize": 10,           // Số lượng phần tử mỗi trang
    "totalItems": 45,         // Tổng số phần tử trong toàn hệ thống
    "totalPages": 5,          // Tổng số trang
    "hasPreviousPage": false, // Có trang trước đó hay không
    "hasNextPage": true       // Có trang tiếp theo hay không
  },
  "message": null,
  "statusCode": 200,
  "success": true
}
```

### 1.4 Xác thực & Cấp quyền (JWT Bearer & Event Roles)
- **Header xác thực**: Với các endpoint yêu cầu đăng nhập, thêm Header:
  `Authorization: Bearer <AccessToken>`
- **Hệ thống Vai trò 2 tầng**:
  1. **System Roles (Hệ thống)**: `Admin`, `Student`, `User`.
  2. **Event Roles (Trong từng Sự kiện)**: `EventCoordinator` (BTC/Điều phối viên), `Judge` (Giám khảo), `Mentor` (Cố vấn), `TeamLeader` (Trưởng đội), `TeamMember` (Thành viên đội).
- **Mã lỗi thường gặp**:
  - `401 Unauthorized`: Chưa truyền Token hoặc Token đã hết hạn / không hợp lệ.
  - `403 Forbidden`: Người dùng không có quyền truy cập endpoint này (Ví dụ: Thí sinh gọi API dành riêng cho Giám khảo).

---

## 2. LUỒNG 1: Auth, Tài khoản & Xác thực Sinh viên

### 2.1 Đăng ký Tài khoản
- **Endpoint**: `POST /api/Auth/register`
- **Auth**: `AllowAnonymous`
- **Mục đích**: Cho phép thí sinh tạo tài khoản mới bằng Email/Mật khẩu.
- **Request Body**:
```json
{
  "email": "student@fpt.edu.vn",
  "password": "Password123@",
  "fullName": "Nguyễn Văn A"
}
```
- **Response `200 OK`**: Trả về `UserModel` (Tài khoản vừa tạo, ở trạng thái chờ xác thực email).

### 2.2 Đăng nhập bằng Email & Password
- **Endpoint**: `POST /api/Auth/login`
- **Auth**: `AllowAnonymous`
- **Mục đích**: Xác thực người dùng và nhận JWT Tokens.
- **Request Body**:
```json
{
  "email": "student@fpt.edu.vn",
  "password": "Password123@"
}
```
- **Response `200 OK`**:
```json
{
  "data": {
    "accessToken": "eyJhbGciOi...",
    "refreshToken": "d8a7f9...",
    "userId": "usr_123456",
    "email": "student@fpt.edu.vn",
    "fullName": "Nguyễn Văn A",
    "isAdmin": false,
    "isStudent": true
  },
  "success": true
}
```
- **Flutter Dev Note**: Lưu `accessToken` & `refreshToken` vào `FlutterSecureStorage`.

### 2.3 Đăng nhập bằng Google (Google Sign-In)
- **Endpoint**: `POST /api/Auth/google-login`
- **Auth**: `AllowAnonymous`
- **Mục đích**: Đăng nhập bằng Google ID Token từ `google_sign_in` Flutter package. Nếu lần đầu đăng nhập, hệ thống tự động khởi tạo tài khoản.
- **Request Body**:
```json
{
  "idToken": "google_id_token_string_from_gis"
}
```
- **Response `200 OK`**: Cấu trúc trả về giống hệt `POST /api/Auth/login`.

### 2.4 Cấp lại Token khi hết hạn (Refresh Token)
- **Endpoint**: `POST /api/Auth/refresh-token`
- **Auth**: `AllowAnonymous`
- **Request Body**:
```json
{
  "refreshToken": "d8a7f9..."
}
```
- **Response `200 OK`**: Trả về `accessToken` mới và `refreshToken` mới.

### 2.5 Lấy Thông tin Hồ sơ Cá nhân (Profile)
- **Endpoint**: `GET /api/Users/profile`
- **Auth**: `Authorize` (Bearer Token)
- **Mục đích**: Lấy thông tin người dùng đang đăng nhập, bao gồm trạng thái duyệt hồ sơ sinh viên, thông tin trường, vai trò...
- **Response `200 OK`**:
```json
{
  "data": {
    "id": "usr_123456",
    "email": "student@fpt.edu.vn",
    "fullName": "Nguyễn Văn A",
    "studentCode": "SE123456",
    "schoolId": "sch_fpt_01",
    "schoolName": "Đại học FPT",
    "studentCardImageUrl": "https://storage.cloudfly.vn/...",
    "registrationStatus": "Approved", // Approved, Rejected, Unregistered, Pending
    "isAdmin": false,
    "isStudent": true,
    "isTemporary": false
  }
}
```

### 2.6 Khai báo / Cập nhật Hồ sơ Sinh viên (Student Verification)
- **Endpoint**: `POST /api/Auth/student-profiles` (Tạo mới) hoặc `PUT /api/Auth/student-profiles` (Cập nhật)
- **Auth**: `Authorize`
- **Mục đích**: Phân loại sinh viên FPT vs Sinh viên trường ngoài.
  - **SV FPT**: Truyền `studentCode` (VD: `SE123456`). Hệ thống tự gọi `FptMockController` xác minh và **TỰ ĐỘNG DUYỆT NGAY** (`Approved`).
  - **SV Trường ngoài**: Chọn `schoolId`, tải ảnh thẻ SV lên Storage rồi truyền `studentCardImageUrl`. Trạng thái sẽ là **CHỜ DUYỆT** (`Pending`), cần BTC/Admin duyệt tay.
- **Request Body**:
```json
{
  "studentCode": "SE123456",
  "schoolId": "sch_fpt_01",
  "studentCardImageUrl": "https://storage.cloudfly.vn/general/card123.jpg"
}
```

### 2.7 Yêu cầu Gỡ khóa Tài khoản (Request Unblock)
- **Endpoint**: `POST /api/Auth/request-unblock`
- **Auth**: `AllowAnonymous`
- **Mục đích**: Nếu hồ sơ sinh viên bị từ chối $\ge 2$ lần (`UserRejections`), tài khoản bị khóa không được nộp lại. Thí sinh gọi API này gửi lý do để Admin/BTC xem xét gỡ khóa.
- **Request Body**:
```json
{
  "email": "student@fpt.edu.vn",
  "reason": "Em đã bổ sung ảnh chụp mặt trước thẻ sinh viên rõ nét hơn."
}
```

### 2.8 Lấy Tổng hợp Lời mời của Tôi (Bell Notification Badge)
- **Endpoint**: `GET /api/Users/my-invitations`
- **Auth**: `Authorize`
- **Mục đích**: Lấy số lượng và danh sách chi tiết tất cả lời mời đang chờ (Lời mời vào Đội thi + Lời mời đảm nhận Vai trò Sự kiện).
- **Response `200 OK`**:
```json
{
  "data": {
    "totalPendingInvitations": 2,
    "teamInvitations": [
      {
        "invitationId": "inv_team_01",
        "teamId": "team_888",
        "teamName": "Super Code",
        "eventName": "FPT Hackathon 2026",
        "invitedByUserName": "Lê Văn C",
        "createdDate": "2026-08-10T10:00:00Z"
      }
    ],
    "eventRoleInvitations": [
      {
        "invitationId": "inv_role_02",
        "eventId": "evt_999",
        "eventName": "AI Contest",
        "roleName": "Judge",
        "trackName": "Track AI Innovation",
        "createdDate": "2026-08-12T09:00:00Z"
      }
    ]
  }
}
```

### 2.9 Các API Auth / User phụ trợ khác
- `POST /api/Auth/forgot-password`: Body `{ "email": "..." }` -> Gửi email reset pass.
- `POST /api/Auth/reset-password`: Body `{ "token": "...", "newPassword": "..." }`.
- `PUT /api/Auth/change-password`: Body `{ "currentPassword": "...", "newPassword": "..." }`.
- `POST /api/Auth/logout`: Đăng xuất (Vô hiệu hóa token phía server).
- `GET /api/Auth/verify-email?token=...`: Xác thực email khi bấm link từ mail.
- `POST /api/Users/{id}/approve`: (Admin/EC) Duyệt hồ sơ sinh viên ngoài trường.
- `POST /api/Users/{id}/reject`: (Admin/EC) Từ chối hồ sơ sinh viên kèm lý do. Body `{ "reason": "..." }`.

---

## 3. LUỒNG 2: Khám phá Sự kiện, Vòng thi & Hạng mục

### 3.1 Lấy Danh sách Sự kiện (Public / Sắp tới / Của tôi)
- **Danh sách Tất cả Sự kiện**: `GET /api/Events?pageNumber=1&pageSize=10` (Public)
- **Sự kiện Sắp diễn ra**: `GET /api/Events/upcoming?pageNumber=1&pageSize=10` (Public)
- **Sự kiện Của tôi (Đã đăng ký / Tham gia)**: `GET /api/Events/my-events` (`Authorize`)
- **Response `200 OK` (`EventModel`)**:
```json
{
  "data": {
    "data": [
      {
        "id": "evt_001",
        "title": "SEAL Hackathon 2026",
        "description": "Cuộc thi lập trình công nghệ...",
        "bannerUrl": "https://storage.cloudfly.vn/events/banner.png",
        "startDate": "2026-09-01T00:00:00Z",
        "endDate": "2026-09-30T23:59:59Z",
        "location": "FPT University Campus",
        "status": "RegistrationOpen",
        "totalTeams": 12
      }
    ],
    "currentPage": 1,
    "pageSize": 10,
    "totalItems": 1
  }
}
```

### 3.2 Lấy Chi tiết Sự kiện
- **Endpoint**: `GET /api/Events/{id}`
- **Auth**: Public
- **Mục đích**: Xem thông tin mô tả chi tiết, thời gian, địa điểm sự kiện.

### 3.3 Lấy Danh sách Vòng thi (Rounds) của Sự kiện
- **Endpoint**: `GET /api/Rounds/event?eventId=evt_001`
- **Auth**: Public
- **Mục đích**: Lấy danh sách các Vòng thi (VD: Vòng Sơ loại, Vòng Chung kết) thuộc sự kiện.
- **Response `200 OK` (`RoundModel`)**:
```json
{
  "data": {
    "data": [
      {
        "id": "rnd_01",
        "eventId": "evt_001",
        "roundName": "Vòng Sơ Loại",
        "order": 1,
        "startDate": "2026-09-01T08:00:00Z",
        "endDate": "2026-09-15T23:59:59Z",
        "advancementRule": "Top 10 teams highest score"
      }
    ]
  }
}
```

### 3.4 Lấy Danh sách Hạng mục (Tracks) của Sự kiện
- **Endpoint**: `GET /api/Tracks/event?eventId=evt_001`
- **Auth**: Public
- **Mục đích**: Lấy danh sách các Hạng mục chuyên môn (VD: Web App, Mobile App, AI/ML) chạy trong sự kiện.
- **Response `200 OK` (`TrackModel`)**:
```json
{
  "data": {
    "data": [
      {
        "id": "trk_01",
        "eventId": "evt_001",
        "trackName": "AI Innovation",
        "description": "Giải pháp ứng dụng Trí tuệ nhân tạo",
        "templateId": "tpl_ai_01",
        "templateName": "Bộ tiêu chí chấm AI"
      }
    ]
  }
}
```

---

## 4. LUỒNG 3: Đội thi & Quản lý Thành viên

### 4.1 Tạo Đội thi mới (Create Team)
- **Endpoint**: `POST /api/Teams`
- **Auth**: `Authorize` (Người tạo tự động trở thành `TeamLeader`)
- **Request Body**:
```json
{
  "eventId": "evt_001",
  "name": "Nova Tech",
  "description": "Đội thi phát triển ứng dụng di động",
  "avatarUrl": "https://storage.cloudfly.vn/teams/nova.png"
}
```
- **Trạng thái khởi tạo**: Đội ở trạng thái `Forming` (Đang lập đội).

### 4.2 Lấy Thông tin Đội của tôi trong Sự kiện (My Team)
- **Endpoint**: `GET /api/Teams/my-team?eventId=evt_001`
- **Auth**: `Authorize`
- **Mục đích**: Màn hình "My Team" của thí sinh. Lấy chi tiết thông tin đội, vai trò của mình, danh sách thành viên và lời mời đang chờ.
- **Response `200 OK` (`MyTeamResponseModel`)**:
```json
{
  "data": {
    "id": "team_1001",
    "name": "Nova Tech",
    "status": "Forming", // Forming, PendingApproval, Registered, Rejected
    "leaderUserId": "usr_123456",
    "isLeader": true,
    "members": [
      {
        "userId": "usr_123456",
        "fullName": "Nguyễn Văn A",
        "email": "student@fpt.edu.vn",
        "roleInTeam": "TeamLeader",
        "registrationStatus": "Approved"
      }
    ]
  }
}
```

### 4.3 Mời Thành viên vào Đội (Invite Team Member)
- **Endpoint**: `POST /api/Teams/{teamId}/invitations`
- **Auth**: `EventRoleAuthorize` (`TeamLeader`, `EventCoordinator`)
- **Mục đích**: Trưởng nhóm gửi lời mời một người dùng khác vào đội qua email/UserId.
- **Request Body**:
```json
{
  "invitedUserId": "usr_777888"
}
```

### 4.4 Phản hồi Lời mời vào Đội (Accept / Decline Invitation)
- **Endpoint**: `POST /api/Teams/invitations/{invitationId}/respond?isAccepted=true`
- **Auth**: `Authorize` (Người được mời gọi)
- **Query Parameter**: `isAccepted` (`true` = Chấp nhận vào đội, `false` = Từ chối).

### 4.5 Thêm trực tiếp hoặc Xóa thành viên (Kick Member)
- **Thêm trực tiếp thành viên**: `POST /api/Teams/{teamId}/members` (Body `{ "userId": "..." }`)
- **Xóa thành viên khỏi đội**: `DELETE /api/Teams/{teamId}/members/{userId}`
- **Quyền**: Chỉ `TeamLeader` hoặc `EventCoordinator` mới được thực hiện.

### 4.6 Thí sinh tự rời Đội (Leave Team)
- **Endpoint**: `POST /api/Teams/{teamId}/leave`
- **Auth**: `Authorize`
- **Lưu ý**: TeamLeader không thể tự rời (phải chuyển quyền Trưởng nhóm trước hoặc xóa nhóm). Đội ở trạng thái `Registered` không được tự rời.

### 4.7 Chuyển quyền Trưởng nhóm (Transfer Leader)
- **Endpoint**: `POST /api/Teams/{teamId}/transfer-leader`
- **Auth**: `TeamLeader` / `EventCoordinator`
- **Request Body**:
```json
{
  "newLeaderUserId": "usr_777888"
}
```

### 4.8 Chốt danh sách & Duyệt Đội thi (Registration Flow)
1. **Trưởng nhóm Chốt Đăng ký**:
   - `POST /api/Teams/{teamId}/confirm-registration`
   - Điều kiện: Đội đủ từ 3 đến 5 thành viên và tất cả thành viên đã có hồ sơ cá nhân `Approved`.
   - Trạng thái chuyển: `Forming` $\rightarrow$ `PendingApproval` (Khóa đội, chờ BTC duyệt).
2. **BTC Duyệt Đội**:
   - `POST /api/Teams/{teamId}/approve-registration` (Trạng thái $\rightarrow$ `Registered`, chính thức thi đấu).
3. **BTC Từ chối Đội**:
   - `POST /api/Teams/{teamId}/reject-registration` (Body `{ "reason": "Chưa đủ giấy tờ..." }`). Trạng thái quay về `Forming` để đội bổ sung.

---

## 5. LUỒNG 4: Nộp bài thi, Storage & Chấm điểm Giám khảo

### 5.1 Upload File đính kèm lên Cloud Storage
- **Endpoint**: `POST /api/Storage/upload?folder=submissions`
- **Auth**: `Authorize`
- **Content-Type**: `multipart/form-data`
- **Body Data**: `file` (File đính kèm: `.pdf`, `.zip`, `.png`, `.docx`...)
- **Response `200 OK`**:
```json
{
  "data": {
    "fileUrl": "https://storage.cloudfly.vn/submissions/a1b2c3-slide.pdf"
  },
  "success": true
}
```

### 5.2 Nộp bài thi giải pháp (Submit Result)
- **Endpoint**: `POST /api/SubmitResults`
- **Auth**: `TeamLeader` / `EventCoordinator`
- **Request Body**:
```json
{
  "teamId": "team_1001",
  "trackId": "trk_01",
  "title": "Giải pháp AI nhận diện nông sản",
  "description": "Mô tả chi tiết giải pháp...",
  "submissionUrl": "https://github.com/myteam/project",
  "attachmentUrl": "https://storage.cloudfly.vn/submissions/a1b2c3-slide.pdf"
}
```

### 5.3 Danh sách Bài nộp của Đội tôi
- **Endpoint**: `GET /api/Teams/my-submissions?pageNumber=1&pageSize=10`
- **Auth**: `Authorize`

### 5.4 Màn hình Chấm điểm dành cho Giám khảo (Judge Scoring)

#### A. Lấy Mẫu tiêu chí & Trọng số của Hạng mục
- **Endpoint**: `GET /api/Templates/{id}`
- **Auth**: `Authorize`
- **Response**: Trả về danh sách Tiêu chí (`Criteria`), Điểm tối đa (`MaxScore`), Trọng số (`Weight`).

#### B. API Gộp Chấm điểm (Lưu phiếu chấm + Điểm chi tiết trong 1 Request)
- **Endpoint**: `POST /api/Scores/save`
- **Auth**: `EventRoleAuthorize` (`Judge`, `EventCoordinator`)
- **Mục đích**: Giám khảo nhập điểm và nhận xét cho từng tiêu chí, bấm **Lưu điểm**. Backend tự động tính toán tổng điểm có trọng số (`TotalScore`).
- **Request Body**:
```json
{
  "eventRoleId": "er_judge_01",
  "submitResultId": "sub_999",
  "comment": "Bài thi có tính sáng tạo cao, demo mượt mà.",
  "details": [
    {
      "criteriaId": "cri_01",
      "score": 8.5,
      "comment": "Thuật toán xử lý tốt"
    },
    {
      "criteriaId": "cri_02",
      "score": 9.0,
      "comment": "Giao diện đẹp"
    }
  ]
}
```

#### C. Lấy Chi tiết Phiếu chấm đã lưu của Giám khảo
- **Endpoint**: `GET /api/Scores/{id}/detail`
- **Auth**: `Judge`, `EventCoordinator`

---

## 6. LUỒNG 5: Bảng xếp hạng, Kết quả & Phúc khảo

### 6.1 Thí sinh Xem Breakdown Chi tiết Điểm của Đội mình
- **Endpoint**: `GET /api/Scores/team/{teamId}/breakdown`
- **Auth**: `Authorize` (Thành viên trong đội, EC, Admin)
- **Mục đích**: Cho phép thí sinh xem minh bạch tổng điểm, điểm từng giám khảo và điểm theo từng tiêu chí để kiểm tra trước khi quyết định nộp đơn phúc khảo.

### 6.2 Bảng xếp hạng Vòng thi (Leaderboard)
- **Endpoint**: `GET /api/FinalResults/round/{roundId}?pageNumber=1&pageSize=10`
- **Auth**: `Authorize`
- **Mục đích**: Xem bảng xếp hạng các đội thi trong một Vòng thi (xếp theo `Rank` tăng dần, `FinalScore` giảm dần).
- **Lưu ý nghiệp vụ**:
  - Người dùng bình thường / Thí sinh: **Chỉ xem được bảng xếp hạng ĐÃ CÔNG BỐ** (`isPublished = true`).
  - BTC / Admin: Xem được cả bản **NHÁP** (`isPublished = false`).

### 6.3 Lịch sử Xếp hạng của Đội qua các Vòng
- **Endpoint**: `GET /api/FinalResults/team/{teamId}`
- **Auth**: `Authorize`

### 6.4 Đội thi Gửi Đơn Phúc khảo (Create Appeal)
- **Endpoint**: `POST /api/Appeals`
- **Auth**: `Authorize` (Thành viên đội thi)
- **Request Body**:
```json
{
  "teamId": "team_1001",
  "roundId": "rnd_01",
  "reason": "Đội em muốn phúc khảo tiêu chí Giao diện do giám khảo 2 chấm điểm chưa khớp với mô tả.",
  "evidenceUrl": "https://storage.cloudfly.vn/appeals/evidence.png"
}
```

### 6.5 Danh sách Đơn Phúc khảo của Đội
- **Endpoint**: `GET /api/Appeals/team/{teamId}`
- **Auth**: `Authorize`

### 6.6 Phản hồi Đơn Phúc khảo (Dành cho BTC / Giám khảo)
- **Endpoint**: `PUT /api/Appeals/{id}/respond`
- **Auth**: `Authorize`
- **Request Body**:
```json
{
  "status": "Accepted", // Accepted, Rejected
  "responseComment": "Sau khi chấm lại, BTC cộng thêm 0.5 điểm tiêu chí Giao diện."
}
```

### 6.7 Các API Quản trị Kết quả của BTC (EventCoordinator Only)
- `POST /api/FinalResults/calculate/{roundId}?topN=5`: Tự động tính điểm trung bình và thăng hạng Top N đội (Lưu bản nháp).
- `POST /api/FinalResults/publish/{roundId}`: Công bố bảng xếp hạng chính thức cho toàn bộ người dùng xem.
- `PUT /api/FinalResults/round/{roundId}/publish-status`: Đặt trạng thái công bố 2 chiều (`isPublished: true/false`).
- `DELETE /api/FinalResults/round/{roundId}`: Hủy công bố và xóa kết quả vòng thi để thực hiện chấm lại.
- `PATCH /api/FinalResults/{id}/assign-prize`: Gán giải thưởng (Nhất, Nhì, Ba...) cho đội thi. Body `{ "prizeId": "prz_01" }`.

---

## 7. PHỤ LỤC MASTER DATA & MOCK SERVICES

### 7.1 Master Data Danh sách Trường học (Schools)
- `GET /api/Schools`: Lấy danh sách tất cả các trường học trong hệ thống.
- `GET /api/Schools/with-user-count`: Lấy danh sách trường kèm số lượng sinh viên tham gia.

### 7.2 Mock Tra cứu Sinh viên FPT (FPT Verification Service)
- **Endpoint**: `GET /api/fpt-mock/students/{studentCode}`
- **Public Sandbox Data để Test**:
  - `SE123456` - Nguyễn Văn A (Chuyên ngành SE - K17)
  - `SE789012` - Trần Thị B (Chuyên ngành SE - K18)
  - `CS001122` - Lê Văn C (Chuyên ngành CS - K17)
  - `IA334455` - Phạm Thị D (Chuyên ngành IA - K19)
  - `SS667788` - Hoàng Văn E (Chuyên ngành SS - K18)

### 7.3 Key-Value Enum Vai trò Sự kiện (EventRoleTypes)
- `GET /api/EventRoles/types`
- **Trả về mapping chuẩn**: `1`: `EventCoordinator`, `2`: `Judge`, `3`: `Mentor`, `4`: `TeamLeader`, `5`: `TeamMember`.

---

## 8. Hướng dẫn Tích hợp Dio / Retrofit cho Flutter

### 8.1 Model Wrapper BaseResponse trong Dart
```dart
class BaseResponse<T> {
  final T? data;
  final String? message;
  final int statusCode;
  final bool success;

  BaseResponse({
    this.data,
    this.message,
    required this.statusCode,
    required this.success,
  });

  factory BaseResponse.fromJson(Map<String, dynamic> json, T Function(dynamic json) fromJsonT) {
    return BaseResponse<T>(
      data: json['data'] != null ? fromJsonT(json['data']) : null,
      message: json['message'] as String?,
      statusCode: json['statusCode'] as int? ?? 200,
      success: json['success'] as bool? ?? false,
    );
  }
}
```

### 8.2 Dio Interceptor thêm Bearer Token & Auto Refresh
```dart
class AuthInterceptor extends Interceptor {
  final FlutterSecureStorage _storage = const FlutterSecureStorage();

  @override
  void onRequest(RequestOptions options, RequestInterceptorHandler handler) async {
    final token = await _storage.read(key: 'access_token');
    if (token != null && token.isNotEmpty) {
      options.headers['Authorization'] = 'Bearer $token';
    }
    handler.next(options);
  }

  @override
  void onError(DioException err, ErrorInterceptorHandler handler) async {
    if (err.response?.statusCode == 401) {
      // Logic gọi Refresh Token API & Retry Request ban đầu
    }
    handler.next(err);
  }
}
```

---
*Tài liệu được khởi tạo và kiểm tra đồng bộ trực tiếp từ Source Code Backend `SU26_SWP_BL3W_BE`.*
