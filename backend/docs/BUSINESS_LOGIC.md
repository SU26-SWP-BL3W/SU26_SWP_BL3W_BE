# SEAL — Tài liệu Nghiệp vụ Toàn dự án

> Tài liệu này mô tả **toàn bộ nghiệp vụ thật đang có trong code** (backend .NET, Clean Architecture/CQRS với MediatR) — không phải bản đặc tả lý tưởng. Mỗi mục ghi rõ: ai được thực hiện, điều kiện/ràng buộc thực tế đọc trực tiếp từ handler, kết quả/tác động lên DB, và file:line tham chiếu để tra cứu nhanh.
>
> Được biên soạn bằng cách khảo sát toàn bộ `SEAL.Application/Features/**` (18 module) trên nhánh `dev`, tính đến commit `efb76e3`.

## Mục lục

1. [Định danh & Phân quyền](#1-định-danh--phân-quyền-users-eventroles) — Users, EventRoles, mời EC/Judge/Mentor, UserRejections
2. [Cấu trúc Sự kiện & Cấu hình chấm điểm](#2-cấu-trúc-sự-kiện--cấu-hình-chấm-điểm) — Events, Rounds, Tracks, Templates, Criterias, Schools
3. [Quản lý Đội thi](#3-quản-lý-đội-thi-teams) — Teams
4. [Nộp bài, Chấm điểm & Phúc khảo](#4-nộp-bài-chấm-điểm--phúc-khảo-submitresults-scores-appeals) — SubmitResults, Scores, ScoreDetails, Appeals
5. [Kết quả, Giải thưởng & Tiện ích Demo](#5-kết-quả-giải-thưởng--tiện-ích-demo) — FinalResults, Prizes, UserRejections, Demo
6. [Tổng hợp các vấn đề phát hiện được](#6-tổng-hợp-các-vấn-đề-phát-hiện-được) — bất nhất comment/code, lỗ hổng phân quyền, race condition

## Kiến trúc dữ liệu tổng quan

```
Event (1) ──< Round (N) ──< Track (N) ──> Template (0..1) ──< TemplateCriteria >── Criteria
  │              │              │
  │              │              └──< SubmitResult (N, 1 đội / 1 track) ──< Score (N, 1/giám khảo) ──< ScoreDetail (N, 1/tiêu chí)
  │              │                                                            │
  │              └──< FinalResult (N, 1/đội — kết quả đã tính)                └──< Appeal (đơn phúc khảo, gắn theo SubmitResult)
  │              
  └──< Team (N) ──< EventRole (N — TeamLeader/TeamMember/EventCoordinator/Judge/Mentor, gắn User + tùy chọn Team/Track)
  └──< Prize (N) ──> gán vào FinalResult.PrizeId
```

Ghi chú xuyên suốt:
- Toàn bộ thao tác `DeleteAsync` trong `GenericRepository` là **xóa cứng** (`_dbSet.Remove`), dù nhiều XML-doc trên controller ghi nhầm "(Soft Delete)". Trường `DeletedTime` tồn tại trên `BaseEntity` nhưng không có `HasQueryFilter` áp dụng.
- `EventRoleChecker.HasRoleAsync` có **bypass toàn cục cho Admin** (`user.IsAdmin` → luôn true) trước khi xét bất kỳ role nào, và cache kết quả 60 giây theo `(userId, eventId)`.
- Công thức tính điểm áp dụng thống nhất toàn hệ thống (mục 4.2): `TotalScore = Σ (Value / MaxScore × Weight/100) × 10`, làm tròn 2 chữ số `AwayFromZero`.

---

## 1. Định danh & Phân quyền (Users, EventRoles)

### 1.1. Nhóm nghiệp vụ tài khoản (SEAL.Application/Features/Users)

#### 1.1.1. Đăng ký tài khoản (RegisterUser)
- **Ai được thực hiện:** Bất kỳ ai (`POST /api/Auth/register`, `[AllowAnonymous]`).
- **Điều kiện/ràng buộc:**
  - Nếu email đã tồn tại và **không** phải tài khoản tạm chưa xác thực (`existingUser.IsTemporary && !existingUser.IsEmailVerified`) → báo trùng email.
  - Trường hợp email trùng với một **tài khoản tạm** (do được mời vào đội/vai trò trước đó) và chưa xác thực → cho phép "nhận lại" (claim) chính tài khoản đó, giữ nguyên `Id` để không mất lời mời đang chờ.
  - Không cho tự đăng ký làm Admin: `IsAdmin = false` cứng; `IsApproved = false`; `IsEmailVerified = false`.
  - Token xác thực email hết hạn sau **24 giờ**.
- **Kết quả:** Tạo/cập nhật `User`; gửi email kích hoạt `{FrontendUrl}/auth/verify-email?token=...`.
- **File:** `SEAL.Application/Features/Users/Commands/RegisterUser/RegisterUserCommandHandler.cs:46-132`

#### 1.1.2. Đăng nhập email/mật khẩu (LoginUser)
- **Ai được thực hiện:** Bất kỳ ai (`[AllowAnonymous]`).
- **Điều kiện/ràng buộc:**
  - Sai email/mật khẩu → thông báo chung (không lộ email nào sai).
  - `!IsAdmin && !IsEmailVerified && !IsTemporary` → chặn đăng nhập.
  - Tài khoản tạm chỉ đăng nhập được nếu có `EventRole` còn hiệu lực, **hoặc** đang có lời mời (Team/EventRole) còn hạn — tránh vòng lặp "cần role để login, cần login để nhận role".
- **Kết quả:** Sinh access/refresh token; lưu `RefreshToken`/`RefreshTokenExpiryTime`.
- **File:** `SEAL.Application/Features/Users/Commands/LoginUser/LoginUserCommandHandler.cs:29-101`

#### 1.1.3. Đăng nhập Google (GoogleLogin)
- **Ai được thực hiện:** Bất kỳ ai (`[AllowAnonymous]`).
- **Điều kiện/ràng buộc:**
  - Thiếu cấu hình `GoogleAuth:ClientId` → **chặn hoàn toàn** (fail-closed), không bỏ qua kiểm tra audience.
  - Xác thực chữ ký + Audience qua `GoogleJsonWebSignature.ValidateAsync`.
  - Email chưa có tài khoản → tự tạo: `IsEmailVerified = true`, `IsStudent = true`, `IsAdmin = false`, `IsApproved = false`.
- **Kết quả:** Tạo/tái dùng `User`, sinh JWT; gửi email "chào mừng" (mới) hoặc "cảnh báo đăng nhập Google mới" (cũ).
- **File:** `SEAL.Application/Features/Users/Commands/GoogleLogin/GoogleLoginCommandHandler.cs:41-174`

#### 1.1.4. Xác thực email (VerifyEmail)
- **Ai được thực hiện:** Bất kỳ ai giữ link (`[AllowAnonymous]`).
- **Điều kiện:** Token khớp và chưa hết hạn.
- **Kết quả:** `IsEmailVerified = true`; nếu là tài khoản tạm → tự `IsApproved = true` + cấp **mật khẩu tạm ngẫu nhiên**, gửi email chứa mật khẩu.
- **File:** `SEAL.Application/Features/Users/Commands/VerifyEmail/VerifyEmailCommandHandler.cs:29-104`

#### 1.1.5. Làm mới token (RefreshToken)
- **Điều kiện:** RefreshToken khớp và chưa hết hạn; áp lại điều kiện xác thực như lúc login.
- **Kết quả:** Rotate access/refresh token.
- **File:** `SEAL.Application/Features/Users/Commands/RefreshToken/RefreshTokenCommandHandler.cs:29-67`

#### 1.1.6. Đăng xuất (Logout)
- **Ai:** Đã đăng nhập. **Kết quả:** Xóa refresh token.
- **File:** `SEAL.Application/Features/Users/Commands/Logout/LogoutCommandHandler.cs:19-36`

#### 1.1.7. Quên mật khẩu (ForgotPassword)
- **Điều kiện:** Luôn trả thông báo chung (chống dò email); chỉ xử lý khi `IsEmailVerified`; cooldown **5 phút**/yêu cầu; token dùng chung field với VerifyEmail, hết hạn **24h**.
- **File:** `SEAL.Application/Features/Users/Commands/ForgotPassword/ForgotPasswordCommandHandler.cs:40-99`

#### 1.1.8. Đặt lại mật khẩu (ResetPassword)
- **Kết quả:** Đổi mật khẩu; **xóa refresh token** (vô hiệu hóa mọi phiên cũ); token dùng 1 lần; gửi email cảnh báo.
- **File:** `SEAL.Application/Features/Users/Commands/ResetPassword/ResetPasswordCommandHandler.cs:25-70`

#### 1.1.9. Đổi mật khẩu (ChangePassword)
- **Điều kiện:** Mật khẩu cũ khớp hash hiện tại; mật khẩu mới ≥ 6 ký tự.
- **File:** `SEAL.Application/Features/Users/Commands/ChangePassword/ChangePasswordCommandHandler.cs:21-43`

#### 1.1.10. Yêu cầu gỡ khóa (RequestUnblock)
- **Điều kiện:** Chỉ xử lý khi số `UserRejection` ≥ **2** (`RejectionLockThreshold`); cooldown **24h**/yêu cầu.
- **Kết quả:** Không đổi DB, chỉ gửi email cho `SupportEmail` + user. Gỡ khóa thật sự qua mục 1.4.3.
- **File:** `SEAL.Application/Features/Users/Commands/RequestUnblock/RequestUnblockCommandHandler.cs:48-111`

#### 1.1.11. Admin tạo tài khoản (CreateUser)
- **Ai:** Chỉ Admin (2 lớp: `[AdminAuthorize]` + check lại trong handler).
- **Kết quả:** Tạo `User` với `IsApproved = true`, `IsEmailVerified = true` mặc định.
- **File:** `SEAL.Application/Features/Users/Commands/CreateUser/CreateUserCommandHandler.cs:23-78`

#### 1.1.12. Duyệt hồ sơ (ApproveUser)
- **Ai:** Admin, hoặc EC của **bất kỳ sự kiện nào** mà người bị duyệt có vai trò thí sinh (`EventRole.TeamId != null`).
- **Kết quả:** `IsApproved = true`. (Lưu ý: duyệt user **không còn** tự động duyệt đội — tách riêng qua ApproveTeamRegistration.)
- **File:** `SEAL.Application/Features/Users/Commands/ApproveUser/ApproveUserCommandHandler.cs:33-104`

#### 1.1.13. Từ chối hồ sơ (RejectUser)
- **Ai:** Admin hoặc EC liên quan (cùng cơ chế ApproveUser).
- **Điều kiện:** `Reason` bắt buộc.
- **Kết quả:** `IsApproved = false`; tạo `UserRejection`; hạ mọi đội `Registered`/`IsActive` của người này về `Forming` + `IsActive = false`; gửi email cảnh báo "từ chối quá 2 lần sẽ bị khóa cập nhật hồ sơ".
- **File:** `SEAL.Application/Features/Users/Commands/RejectUser/RejectUserCommandHandler.cs:40-181`

#### 1.1.14. Admin xóa tài khoản (DeleteUser)
- **Ai:** Chỉ Admin. **Điều kiện:** Không được tự xóa chính mình.
- **File:** `SEAL.Application/Features/Users/Commands/DeleteUser/DeleteUserCommandHandler.cs:24-57`

#### 1.1.15. Admin/EC cập nhật tài khoản (UpdateUser)
- **Ai:** Admin hoặc EC còn hiệu lực (không giới hạn sự kiện cụ thể).
- **Điều kiện (chống tự khóa/mất admin cuối):** không tự vô hiệu hóa chính mình; không tự gỡ quyền admin của chính mình; không gỡ quyền/khóa **admin cuối cùng** đang hoạt động của hệ thống.
- **File:** `SEAL.Application/Features/Users/Commands/UpdateUser/UpdateUserCommandHandler.cs:24-104`

#### 1.1.16. Nộp hồ sơ sinh viên (UpdateStudentProfile)
- **Ai:** Tự nộp hồ sơ của chính mình. **Điều kiện:** Chặn Admin nộp hồ sơ; **khóa nếu bị từ chối ≥ 2 lần** (đếm tổng `UserRejection`, không lọc `IsActive`); FPT → gọi FPT Mock API xác thực `StudentCode` + đối chiếu email; non-FPT → bắt buộc ảnh thẻ SV.
- **Kết quả:** `IsApproved = IsFpt` (tự duyệt nếu là SV FPT đã xác thực); `IsTemporary = false`; vô hiệu hóa (không xóa) mọi `UserRejection` đang active.
- **File:** `SEAL.Application/Features/Users/Commands/UpdateStudentProfile/UpdateStudentProfileCommandHandler.cs:43-194`

#### 1.1.17–1.1.19. Truy vấn Users
- `GetAllUsers`: Admin/EC hiệu lực; luôn loại Admin khỏi kết quả; filter `HasSubmittedProfile` = `IsStudent && SchoolId != null`.
- `GetUserById`: Chính chủ, Admin, hoặc EC hiệu lực.
- `GetUserProfile`: Chính chủ.
- **File:** `Queries/GetAllUsers/GetAllUsersQueryHandler.cs:25-115`, `Queries/GetUserById/GetUserByIdQueryHandler.cs:22-64`, `Queries/GetUserProfile/GetUserProfileQueryHandler.cs:23-50`

#### 1.1.20. Chuông thông báo lời mời (GetMyInvitations)
- Gộp `TeamInvitation` (Pending/TransferPending còn hạn) + `EventRoleInvitation` (Pending còn hạn), cộng lịch sử phản hồi **7 ngày** gần nhất.
- **File:** `SEAL.Application/Features/Users/Queries/GetMyInvitations/GetMyInvitationsQueryHandler.cs:50-169`

### 1.2. Vai trò sự kiện (EventRoles)

Cơ chế chung: `[EventRoleAuthorize(EventRoleType.EventCoordinator)]` + **Admin luôn bypass**.

#### 1.2.0. Ma trận xung đột vai trò dùng chung (`EventRoleValidationHelper.CheckRoleConflictAsync`)
1. Trùng chính xác cùng vai trò trên cùng Track → lỗi.
2. TeamLeader/TeamMember **không kiêm nhiệm** EC/Judge/Mentor (và ngược lại).
3. EventCoordinator **không kiêm nhiệm** Judge/Mentor (và ngược lại).
4. Judge và Mentor **loại trừ nhau trong cùng 1 Track**.
- **File:** `SEAL.Application/Features/EventRoles/EventRoleValidationHelper.cs:12-96`

#### 1.2.1. Gán vai trò trực tiếp (AssignEventRole)
- Gán `EventCoordinator` chỉ Admin được làm (validator). TeamLeader/TeamMember áp quy tắc "1 user/1 team/1 event".
- **Kết quả:** `ExpiredAt = request ?? Event.EndDate` (mặc định).
- **File:** `SEAL.Application/Features/EventRoles/Commands/AssignEventRole/AssignEventRoleCommandHandler.cs:24-110`

#### 1.2.2. Cập nhật vai trò (UpdateEventRole)
- Không cho đổi sang/khỏi vai trò thành viên đội qua endpoint này; không đổi Role/Track nếu đã có `Score` gắn.
- **File:** `SEAL.Application/Features/EventRoles/Commands/UpdateEventRole/UpdateEventRoleCommandHandler.cs:24-109`

#### 1.2.3. Thu hồi vai trò (RemoveEventRole)
- Không xóa được TeamLeader/TeamMember qua đây; không xóa được nếu đã gắn `Score` (Cascade sẽ nuốt điểm).
- **File:** `SEAL.Application/Features/EventRoles/Commands/RemoveEventRole/RemoveEventRoleCommandHandler.cs:21-54`

#### 1.2.4. Mời vai trò qua email (InviteEventRole)
- Chỉ mời được Judge/Mentor/EventCoordinator; lời mời hết hạn **24h**.
- **File:** `SEAL.Application/Features/EventRoles/Commands/InviteEventRole/InviteEventRoleCommandHandler.cs:39-208`

#### 1.2.5. Phản hồi lời mời vai trò (RespondEventRoleInvitation)
- Chỉ chính người được mời; lazy-expire; re-validate xung đột **tại thời điểm chấp nhận**; idempotent nếu `EventRole` đã tồn tại.
- **Kết quả:** Tạo `EventRole`; `InvalidateCache` ngay để có hiệu lực tức thì.
- **File:** `SEAL.Application/Features/EventRoles/Commands/RespondEventRoleInvitation/RespondEventRoleInvitationCommandHandler.cs:30-176`

#### 1.2.6. Từ chối qua link công khai (DeclineEventRoleInvitation)
- `[AllowAnonymous]` — an toàn vì chỉ Reject/Expire, không bao giờ tạo `EventRole`. Idempotent nếu đã Rejected/Expired.
- **File:** `SEAL.Application/Features/EventRoles/Commands/DeclineEventRoleInvitation/DeclineEventRoleInvitationCommandHandler.cs:29-63`

#### 1.2.7. Truy vấn EventRoles
- `CheckUserHasRoleInEvent`, `GetEventRolesByEventId`, `GetEventRolesByUserId`, `GetUserRoleInEvent`, `GetUsersByRoleInEvent` — chỉ yêu cầu đăng nhập, không giới hạn thêm.

### 1.3. Mời vai trò chuyên biệt kèm tự tạo tài khoản tạm

Ba handler dùng chung khuôn mẫu: nhập email → tự tạo `User` tạm nếu chưa có → tạo `EventRoleInvitation` (accept mới tạo `EventRole` thật).

- **1.3.1 InviteEventCoordinator** — `SEAL.Application/Features/EventCoordinators/Commands/InviteEventCoordinator/InviteEventCoordinatorCommandHandler.cs:46-179`
- **1.3.2 InviteJudgeToTrack** — `SEAL.Application/Features/Judges/Commands/InviteJudgeToTrack/InviteJudgeToTrackCommandHandler.cs:46-190`
- **1.3.3 InviteMentorToTrack** — `SEAL.Application/Features/Mentors/Commands/InviteMentorToTrack/InviteMentorToTrackCommandHandler.cs:46-190`

> Cả 3 dùng chung `EventRoleValidationHelper` (mục 1.2.0), token 24h. **Lưu ý:** tài khoản tạm được tạo **trước khi** biết người được mời có chấp nhận hay không — không thấy cơ chế dọn dẹp tài khoản tạm bị "treo" vĩnh viễn nếu không ai chấp nhận.

### 1.4. Lịch sử từ chối hồ sơ (UserRejections)

#### 1.4.1. Tạo bản ghi từ chối (CreateUserRejection)
- ⚠️ **Không có kiểm tra quyền nào ở tầng controller lẫn handler.** "Phải là Admin" chỉ được xác minh gián tiếp qua *dữ liệu*: `RejectedBy` phải trỏ tới 1 `User` có `IsAdmin = true` — không xác minh danh tính người gọi API thật sự.
- **File:** `SEAL.Application/Features/UserRejections/Commands/CreateUserRejection/CreateUserRejectionCommandHandler.cs:20-66`

#### 1.4.2. Cập nhật lý do (UpdateUserRejection)
- ⚠️ Không kiểm tra quyền nào (không `[Authorize]`, không check ownership).
- **File:** `SEAL.Application/Features/UserRejections/Commands/UpdateUserRejection/UpdateUserRejectionCommandHandler.cs:12-48`

#### 1.4.3. Xóa bản ghi từ chối — cơ chế gỡ khóa thật (DeleteUserRejection)
- **Ai:** Admin hoặc chính người tạo bản ghi (handler UserRejections **duy nhất** có kiểm tra quyền).
- **Kết quả:** Xóa cứng; nếu không còn `UserRejection.IsActive = true` nào khác → reset `IsApproved = false`. Đây là cách BTC gỡ khóa tài khoản bị khóa cập nhật hồ sơ (ngưỡng ≥2 ở mục 1.1.16).
- **File:** `SEAL.Application/Features/UserRejections/Commands/DeleteUserRejection/DeleteUserRejectionCommandHandler.cs:10-74`

#### 1.4.4. Truy vấn (GetAllUserRejections / GetUserRejectionsByUserId)
- ⚠️ Không kiểm tra quyền.

---

## 2. Cấu trúc Sự kiện & Cấu hình chấm điểm

Kiến trúc: `Event (1) → Round (N) → Track (N) → Template (0..1) → TemplateCriteria → Criteria`. Cascade: `Event→Round`/`Round→Track` = Cascade; `Track→Template` = SetNull; `TemplateCriteria→*` = Cascade; `School→User` = Restrict.

### 2.1. Sự kiện (Event)

#### 2.1.1. Tạo sự kiện (kèm cây Round → Track lồng nhau)
- **Ai:** Controller **không gắn attribute phân quyền** — quyền nằm trong Validator: Admin, hoặc user đã có `EventRole.EventCoordinator` **ở bất kỳ sự kiện nào từ trước**.
- **Điều kiện:** `StartDate < EndDate`; đăng ký (nếu có) phải nằm trong khung event; bắt buộc ≥1 Round; Round phải nằm trong khung Event, `AdvancementRule` khớp regex `^(top|percent|minScore)\s*:\s*\d+(\.\d+)?$`; Track tên duy nhất trong Round, thời gian nằm trong khung Event; **mọi Template được Track tham chiếu phải có tổng trọng số = 100%** (kiểm tra trước khi tạo bất cứ gì).
- **Kết quả:** Toàn bộ trong 1 transaction, ngày giờ chuẩn hóa UTC.
- **File:** `SEAL.Application/Features/Events/Commands/CreateEvent/CreateEventCommandHandler.cs:16-192`

#### 2.1.2. Cập nhật sự kiện
- **Ai:** `[EventRoleAuthorize(EventCoordinator)]` theo đúng event.
- **Điều kiện:** Thu hẹp thời gian Event không được làm Round nào "lọt ra ngoài"; giảm `MaxTeams` không được thấp hơn số đội `IsActive` hiện tại.
- **File:** `SEAL.Application/Features/Events/Commands/UpdateEvent/UpdateEventCommandHandler.cs:23-102`

#### 2.1.3. Xóa sự kiện
- **Ai:** Admin, Owner (CreatedBy), hoặc EC — kiểm tra kép (filter + handler).
- **Điều kiện:** Đã có Team đăng ký → chặn; `Status == true` (công khai) → phải ẩn trước mới xóa được.
- **File:** `SEAL.Application/Features/Events/Commands/DeleteEvent/DeleteEventCommandHandler.cs:26-69`

#### 2.1.4. Truy vấn (GetAllEvents/GetEventById/GetMyEvents/GetUpcomingEvents)
- `GetUpcomingEvents`: chỉ `StartDate > now && Status == true`. `GetMyEvents` (`[Authorize]`): theo `EventRole` chưa hết hạn.

### 2.2. Vòng thi (Round)

#### 2.2.1. Tạo vòng thi
- **Điều kiện:** `Start < End`; nằm trong khung Event; tên & số thứ tự không trùng trong Event; `AdvancementRule` khớp regex.
- **File:** `SEAL.Application/Features/Rounds/Commands/CreateRound/CreateRoundCommandHandler.cs:20-106`

#### 2.2.2. Cập nhật vòng thi — khóa dần theo dữ liệu đã phát sinh
- Không đổi `EventId`. **Mức khóa tăng dần:**
  - Đã publish (`FinalResult`) → không đổi giờ/số thứ tự/giờ chấm.
  - Đã có `Score` → không đổi giờ diễn ra/giờ chấm.
  - Đã có bài nộp (`hasSubmissions`) → không đổi số thứ tự; chỉ được **mở rộng** cửa sổ thời gian, không được thu hẹp.
- **File:** `SEAL.Application/Features/Rounds/Commands/UpdateRound/UpdateRoundCommandHandler.cs:21-156`

#### 2.2.3. Xóa vòng thi
- ⚠️ **Không có kiểm tra chặn nào** (khác hẳn Update) — xóa cứng, cascade xóa toàn bộ Track con dù đã có bài nộp/điểm.
- **File:** `SEAL.Application/Features/Rounds/Commands/DeleteRound/DeleteRoundCommandHandler.cs:26-63`

### 2.3. Hạng mục thi (Track)

#### 2.3.1. Tạo hạng mục
- **Điều kiện:** Nằm trong khung Event (so với `Event`, không phải `Round`); tên duy nhất trong Round; Template (nếu có) phải tồn tại + tổng trọng số = 100%.
- **File:** `SEAL.Application/Features/Tracks/Commands/CreateTrack/CreateTrackCommandHandler.cs:21-105`

#### 2.3.2. Cập nhật hạng mục
- Đổi Round: không sang Event khác; đã có bài nộp → chặn hoàn toàn.
- Đổi Template: đã có `Score` → chặn hoàn toàn.
- **File:** `SEAL.Application/Features/Tracks/Commands/UpdateTrack/UpdateTrackCommandHandler.cs:22-144`

#### 2.3.3. Xóa hạng mục
- Đã có bài nộp → chặn (tránh vỡ FK Restrict trên Score, hoặc cascade xóa âm thầm SubmitResult chưa chấm).
- **File:** `SEAL.Application/Features/Tracks/Commands/DeleteTrack/DeleteTrackCommandHandler.cs:26-79`

#### 2.3.4. Gán Template cho Track (AssignTemplateToTrack)
- ⚠️ Kiểm tra tổng trọng số = 100% nhưng **không kiểm tra** Track đã có `Score` hay chưa — khác với `UpdateTrack` (có chặn). Có thể đổi Template của Track đã có điểm qua đường vòng này.
- **File:** `SEAL.Application/Features/Tracks/Commands/AssignTemplateToTrack/AssignTemplateToTrackCommandHandler.cs:22-53`

### 2.4. Trường học (School)

- **CreateSchool/UpdateSchool:** `[AdminAuthorize]` (chỉ Admin, chặt hơn Template/Criteria). ⚠️ Update **không kiểm tra trùng tên** (khác Create).
- **DeleteSchool:** Chặn nếu còn `User.SchoolId` trỏ tới (đảm bảo kép bởi FK Restrict).
- **Lưu ý:** `School` entity **không có field phân biệt FPT/non-FPT** — phân biệt đó nằm ở `User.IsFpt`.
- **File:** `SEAL.Application/Features/Schools/Commands/*/​*CommandHandler.cs`

### 2.5. Mẫu tiêu chí chấm điểm (Template)

**Quyền chung nhóm này:** `[AdminOrCoordinatorAuthorize]` — Admin **hoặc** EC ở **bất kỳ** event nào (check toàn cục, không giới hạn event cụ thể).

#### 2.5.1–2.5.3. CRUD Template cơ bản
- Create/Update chỉ ràng buộc tên duy nhất.
- Delete: chặn nếu đang gán cho Track, hoặc đã có `ScoreDetail` dùng template này.

#### 2.5.4. Thêm tiêu chí vào mẫu (AddCriteriaToTemplate)
- Chặn hoàn toàn nếu Template đã dùng chấm điểm hoặc đang gán cho Track (phải gỡ khỏi Track trước).
- Chỉ chặn khi tổng trọng số **vượt quá** 100% — cho phép build dần dưới 100%.

#### 2.5.5. Cập nhật Weight/MaxScore (UpdateTemplateCriteriaConfig)
- Chặn hoàn toàn nếu đã dùng chấm điểm.
- Nếu Template **đang gán cho Track**: vẫn cho sửa, nhưng tổng sau khi đổi **bắt buộc đúng 100%** (khác Add/Remove — không bị khóa tuyệt đối).

#### 2.5.6. Gỡ tiêu chí (RemoveCriteriaFromTemplate)
- Chặn hoàn toàn nếu đã dùng chấm điểm hoặc đang gán Track (không có đường vòng như Update).

> **Tóm lại về ràng buộc 100%:** không ép buộc ngay lúc CRUD Template (Add/Update Weight có thể tạm <100%), mà ép buộc **tại 4 điểm sử dụng**: tạo Event kèm Track có Template, tạo/sửa Track gắn Template, AssignTemplateToTrack. Sau khi Template đã gán ≥1 Track, mọi thay đổi cấu trúc (Add/Remove tiêu chí) bị khóa hoàn toàn; chỉ Update Weight/MaxScore từng tiêu chí vẫn được phép, miễn giữ tổng 100%.

### 2.6. Tiêu chí đơn lẻ (Criteria)

- Create/Update: chỉ ràng buộc trùng tên; Update **không kiểm tra** Criteria đã dùng ở đâu (khác Template — tự do đổi tên/mô tả/`IsActive` dù đã chấm điểm).
- Delete: chặn nếu đang nằm trong bất kỳ Template nào.
- ToggleCriteriaStatus: đảo `IsActive` vô điều kiện, kể cả khi đang dùng chấm điểm.

---

## 3. Quản lý Đội thi (Teams)

### 3.1. Tạo đội thi (CreateTeam)
- **Ai:** Bất kỳ user đã đăng nhập.
- **Điều kiện:** Event đang trong hạn đăng ký; user `IsApproved`; **không** đang giữ vai trò EC/Judge/Mentor trong event; chưa thuộc đội nào khác trong event; tên đội không trùng; số đội active < `Event.MaxTeams`.
- **Kết quả:** Tạo `Team` (`Status = Forming`) + tự động tạo `EventRole(TeamLeader)` cho người tạo.
- **File:** `Commands/CreateTeam/CreateTeamCommandHandler.cs`

### 3.2. Cập nhật đội (UpdateTeam)
- Đội không còn `Forming` → chỉ EC được sửa (Leader bị chặn); đổi `IsActive` chỉ EC được phép.
- **File:** `Commands/UpdateTeam/UpdateTeamCommandHandler.cs`

### 3.3. Xóa đội (DeleteTeam)
- Đội không `Forming` → chỉ Admin/EC xóa được; đội đã có `SubmitResult` → chặn xóa.
- **File:** `Commands/DeleteTeam/DeleteTeamCommandHandler.cs`

### 3.4. Thêm thành viên trực tiếp (AddTeamMember)
- Đội phải `Forming`; user được thêm phải `IsApproved`, còn hạn đăng ký, không giữ vai trò tổ chức, chưa ở đội khác; tổng thành viên < `MAX_TEAM_SIZE = 5`.
- ⚠️ **Không có bước chống race-condition** (khác `RespondTeamInvitation`, xem mục 6).
- **File:** `Commands/AddTeamMember/AddTeamMemberCommandHandler.cs`

### 3.5. Xóa thành viên (RemoveTeamMember)
- Không được xóa TeamLeader bằng API này; đội phải `Forming`.
- **File:** `Commands/RemoveTeamMember/RemoveTeamMemberCommandHandler.cs`

### 3.6. Tự rời đội (LeaveTeam)
- Đội phải `Forming` (chặn cả `PendingApproval`, không chỉ `Registered` — tránh đội tụt quân số trong lúc EC đang duyệt); TeamLeader không được tự rời.
- **File:** `Commands/LeaveTeam/LeaveTeamCommandHandler.cs`

### 3.7. Mời thành viên qua email (InviteTeamMember)
- Đội `Forming`, còn hạn đăng ký; email chưa có tài khoản → tự tạo tài khoản tạm; đếm cả thành viên + lời mời `PendingAccept` còn hạn để giới hạn `MAX_TEAM_SIZE = 5`; lời mời hết hạn **24h**.
- **File:** `Commands/InviteTeamMember/InviteTeamMemberCommandHandler.cs`

### 3.8. Phản hồi lời mời (RespondTeamInvitation)
- Xử lý cả 2 loại lời mời (`PendingAccept` = vào đội thường, `TransferPending` = nhận chuyển quyền Leader) qua cùng bảng `TeamInvitations`.
- Lazy-expire nếu `ExpiresAt <= now`.
- Accept-join: đội phải `Forming`, còn hạn đăng ký, caller `IsApproved` + đã hoàn tất hồ sơ (`IsStudent && SchoolId`), chưa ở đội khác, đội chưa đầy.
- ✅ **Chống race-condition đầy đội:** sau `SaveChanges`, đọc lại toàn bộ thành viên; nếu vượt `MAX_TEAM_SIZE`, giữ lại 5 người theo `(CreatedTime, Id)`, tự xóa người thừa và set `Invitation.Status = Expired`.
- **File:** `Commands/RespondTeamInvitation/RespondTeamInvitationCommandHandler.cs:226-252` (race-fix)

### 3.9. Chuyển quyền Trưởng nhóm (TransferTeamLeader)
- Chỉ **khởi tạo yêu cầu** (`TransferPending`); hoán vai thật sự xảy ra khi người nhận Accept qua mục 3.8.
- ⚠️ Comment ghi "chờ chấp nhận 7 ngày" nhưng hằng số thực tế `TransferLifetime = TimeSpan.FromHours(24)` = 24 giờ (xem mục 6).
- **File:** `Commands/TransferTeamLeader/TransferTeamLeaderCommandHandler.cs:21-22`

### 3.10. Chốt danh sách (ConfirmTeamRegistration)
- `Forming → PendingApproval`. Điều kiện: số thành viên trong khoảng `MIN_TEAM_SIZE=3` – `MAX_TEAM_SIZE=5`; **toàn bộ** thành viên phải đã nộp hồ sơ (`IsStudent && SchoolId`).
- **Kết quả:** Xóa `LastRejectReason` (coi như nộp lại từ đầu).
- **File:** `Commands/ConfirmTeamRegistration/ConfirmTeamRegistrationCommandHandler.cs`

### 3.11. EC duyệt đội (ApproveTeamRegistration)
- Chỉ EC (không phải Leader). `PendingApproval → Registered`; gửi email cho toàn bộ thành viên.
- **File:** `Commands/ApproveTeamRegistration/ApproveTeamRegistrationCommandHandler.cs`

### 3.12. EC từ chối đội (RejectTeamRegistration)
- Chỉ EC. `Reason` bắt buộc. `PendingApproval → Forming`; **lưu `LastRejectReason` vào DB** (không chỉ gửi email).
- **File:** `Commands/RejectTeamRegistration/RejectTeamRegistrationCommandHandler.cs`

### 3.13–3.18. Truy vấn Teams
- **GetTeamById:** Bảo vệ PII — chỉ Admin/thành viên/EC thấy `Email`/`StudentCode`.
- **GetMyTeam:** ⚠️ Model **thiếu field `LastRejectReason`** (có ở GetTeamById) — xem mục 6.
- **GetTeamsList:** Phân trang, không lộ PII.
- **GetMyTeamInvitation:** ⚠️ Không lọc `ExpiresAt > now` (khác GetTeamInvitations, có tính `effectiveStatus`).
- **GetTeamInvitations:** `[EventRoleAuthorize(EC, TeamLeader)]`.
- **GetMySubmissions:** Chỉ tính `EventRole` còn hiệu lực khi xác định đội của user.

### Sơ đồ trạng thái `TeamStatus`

```
enum: Forming=0, Registered=1, Disqualified=2, PendingApproval=3, Rejected=4
(giá trị int cố định — không được đổi thứ tự khai báo, comment trong code cảnh báo rõ)

                 CreateTeam
                     │
                     ▼
   ┌────────────► Forming ◄────────────┐
   │                 │                  │
   │   ConfirmTeamRegistration    RejectTeamRegistration
   │   (3-5 người + đủ hồ sơ)     (Reason bắt buộc → LastRejectReason)
   │                 │                  │
   │                 ▼                  │
   │           PendingApproval ─────────┘
   │                 │
   │      ApproveTeamRegistration
   │                 ▼
   └────────────  Registered
```

- `Forming`: duy nhất cho phép Add/Remove/Leave/Invite/Update/Delete (Leader tự làm được, không cần EC).
- `PendingApproval`/`Registered`: khóa roster hoàn toàn; Update/Delete chỉ EC/Admin.
- Enum có `Disqualified`/`Rejected` nhưng **không có handler nào trong 18 use case Teams set 2 giá trị này** — khả năng thuộc module xử lý vi phạm khác, hoặc dự phòng chưa triển khai.

---

## 4. Nộp bài, Chấm điểm & Phúc khảo (SubmitResults, Scores, Appeals)

### 4.1. Nộp bài dự thi (SubmitResults)

#### 4.1.1. Tạo bài nộp (CreateSubmitResult)
- **Ai:** TeamLeader hoặc EC.
- **Điều kiện:** Đội `Registered`; hạn nộp = `Track.Start/End ?? Round.Start/End`; Round chưa có `FinalResult`; nếu có vòng trước, đội phải `IsAdvanced = true` ở vòng đó; chống nộp trùng theo `(TeamId, TrackId)`.
- ✅ **Chống race-condition nộp trùng:** sau khi lưu, đọc lại toàn bộ bài nộp cùng cặp; bài `CreatedTime` sớm nhất thắng (hòa → so `Id` ordinal), bản thua bị xóa cứng.
- **File:** `Commands/CreateSubmitResult/CreateSubmitResultCommandHandler.cs:32-212`

#### 4.1.2. Cập nhật bài nộp (UpdateSubmitResult)
- Không sửa được nếu đã có `Score`; hạn sửa theo Track/Round; đổi `IsActive` chỉ EC.
- **File:** `Commands/UpdateSubmitResult/UpdateSubmitResultCommandHandler.cs:30-142`

#### 4.1.3. Xóa bài nộp (DeleteSubmitResult)
- Không xóa được nếu đã chấm điểm; hạn xóa theo `Round` (không fallback Track).
- **File:** `Commands/DeleteSubmitResult/DeleteSubmitResultCommandHandler.cs:27-109`

#### 4.1.4–4.1.5. Truy vấn SubmitResults
- `GetSubmitResultsList`: phạm vi nhìn thấy theo vai trò — Admin thấy hết; EC/Judge-Mentor cấp event thấy theo filter; Judge/Mentor gắn Track chỉ thấy Track của mình; thành viên đội chỉ thấy bài của đội mình.

### 4.2. Chấm điểm (Scores & ScoreDetails)

#### Công thức tính điểm (dùng chung mọi đường ghi điểm, qua `ScoreTotalCalculator`)
```
TotalScore = Σ (Value / MaxScore × Weight/100) × 10   [làm tròn 2 chữ số, AwayFromZero]
```

#### 4.2.1. Lưu phiếu chấm gộp (SaveScore)
- **Ai:** Chỉ `Judge`, chính chủ hoặc EC.
- **Điều kiện:** Không được là thành viên đội đang chấm (chống xung đột lợi ích); Track phải có Template; khóa sửa nếu `IsSubmitted = true` **trừ khi** có `Appeal.Approved` với `AssignedJudgeId` khớp; nếu không: (1) chỉ chấm sau khi hạng mục hết hạn nộp, (2) trong cửa sổ `ScoringStart/EndDate`, (3) chưa có `FinalResult`; bắt buộc chấm đủ & đúng tiêu chí; mỗi giá trị ≤ `MaxScore`.
- ✅ **Chống double-submit:** unique index `(EventRoleId, SubmitResultId)` + `try/catch DbUpdateException`.
- **File:** `Commands/SaveScore/SaveScoreCommandHandler.cs:38-311`; index: `SEAL.Infrastructure/Persistence/Configurations/ScoreConfiguration.cs:20`

#### 4.2.2–4.2.4. CreateScore / UpdateScore / DeleteScore
- CreateScore: đồng bộ hóa lại với rào chắn của SaveScore (trước đây là "đường vòng lách luật").
- UpdateScore: không cho đổi `SubmitResultId` (phải dùng SaveScore để chuyển).
- DeleteScore: khóa nếu `FinalResult` đã tồn tại.

#### 4.2.5–4.2.6. Truy vấn Scores
- `GetTeamScoreBreakdown`: **cố ý không cho Judge xem** (lộ điểm đồng nghiệp) — chỉ Admin/EC/thành viên đội/Mentor hiệu lực.

### 4.3. Điểm chi tiết (ScoreDetails — CRUD riêng lẻ)
- Tất cả 4 handler (Create/Update/Delete ScoreDetail) đều **tự tính lại `Score.TotalScore`** bằng cùng `ScoreTotalCalculator` ngay sau khi thay đổi — đảm bảo đồng bộ với SaveScore. Khóa nếu `FinalResult` đã tồn tại.

### 4.4. Phúc khảo (Appeals)

#### 4.4.1. Gửi đơn phúc khảo (CreateAppeal)
- **Ai:** Chỉ TeamLeader của đội sở hữu bài nộp.
- **Điều kiện:** Trong khung `Round.Start/EndDate` (không fallback Track); ✅ **chặn nếu `FinalResult.IsPublished = true`** cho (Round,Team) — bugfix đã áp dụng trong phiên làm việc trước; chống gửi trùng khi đang có đơn `Pending`.
- **File:** `Commands/CreateAppeal/CreateAppealCommandHandler.cs:27-101`

#### 4.4.2. Duyệt/từ chối đơn (RespondAppeal)
- **Ai:** Admin hoặc EC của event chứa bài nộp. Chỉ phản hồi đơn đang `Pending`. Approved → gán `AssignedJudgeId`, điều kiện này chính là cái mở khóa cho giám khảo sửa điểm đã chốt (mục 4.2.1).
- **File:** `Commands/RespondAppeal/RespondAppealCommandHandler.cs:27-87`

#### 4.4.3–4.4.5. Truy vấn Appeals
- `GetAppealsByRound`/`GetAppealsByTeam`: chỉ cần đăng nhập, **không kiểm tra vai trò/quyền sở hữu** trong handler.
- `GetAssignedAppeals`: lọc theo `EventRoleId` **client tự truyền vào**, không đối chiếu với `currentUserId`.

### Bảng tổng hợp cơ chế khóa theo mốc thời gian

| Mốc / trạng thái | Nộp bài | Chấm điểm | Phúc khảo |
|---|---|---|---|
| Trong `[Track.Start, End]` | Được nộp/sửa | Chặn chấm | — |
| Sau End, trước ScoringStart | Hết hạn sửa/xóa | Chặn ("chưa tới giờ chấm") | Theo khung Round riêng |
| Trong `[ScoringStart, ScoringEnd]` | — | Được chấm (nếu chưa `IsSubmitted`) | — |
| Sau ScoringEnd | — | Chặn, trừ phúc khảo được giao | — |
| `FinalResult` tồn tại (đã tính) | Khóa hoàn toàn | Khóa toàn bộ CRUD | — |
| `FinalResult.IsPublished = true` | — | — | Khóa gửi đơn mới |
| Appeal `Approved` + `AssignedJudgeId` khớp | — | Cho sửa lại dù đã chốt | Hiện trong GetAssignedAppeals |

---

## 5. Kết quả, Giải thưởng & Tiện ích Demo

### 5.1. Tính kết quả vòng thi (CalculateRoundResults)
- **Ai:** EC của event hoặc Admin (double-check: filter + tự kiểm tra lại trong handler).
- **Điều kiện:** Chặn nếu vòng **sau** đã có bài nộp/kết quả dựa trên vòng này; vòng phải có bài nộp; chỉ tính điểm từ `Judge` thật sự; **chặn nếu chấm chưa đầy đủ** (đối chiếu số giám khảo được phân công còn hiệu lực với số phiếu đã chấm cho từng bài).
- **Công thức FinalScore:** trung bình các **điểm hạng mục** (mỗi hạng mục = trung bình phiếu chấm của các giám khảo), không phải trung bình phẳng mọi phiếu. Đội không nộp ở 1 track: nếu track đã hết hạn → tính 0 điểm hạng mục đó (ép buộc); nếu track chưa hết hạn → tạm loại khỏi phép tính.
- **Xếp hạng:** Standard competition ranking kiểu **"1-1-3"** (đồng điểm nhận cùng Rank, người kế tiếp nhảy đúng số người đã đứng trước).
- **AdvancementRule:** parse chuỗi `"top:N"` / `"percent:P"` (ngưỡng = `Ceiling(total*P/100)`) / `"minscore:X"`; sai định dạng/rỗng → fallback dùng `topN` từ query string. `IsAdvanced` theo `Rank <= cutoffRank` (đồng hạng ở ranh giới đều được thăng — **chủ đích, không phải bug**) hoặc `FinalScore >= minScore`.
- **Kết quả:** Xóa toàn bộ `FinalResult` cũ của Round rồi tạo lại từ đầu (idempotent kiểu "xóa-tạo-lại"), luôn `IsPublished = false`.
- **File:** `Commands/CalculateRoundResults/CalculateRoundResultsCommandHandler.cs:32-262`

### 5.2–5.4. Publish / SetPublishStatus / Unpublish
- **PublishRoundResults:** 1 chiều (nháp→công bố), yêu cầu đã Calculate trước.
- **SetRoundResultsPublishStatus:** 2 chiều (nháp⇄công bố), **không** có guard "vòng sau chưa vận hành" vì không hủy dữ liệu — an toàn đảo qua đảo lại.
- **UnpublishRoundResults:** **xóa cứng** toàn bộ `FinalResult` của Round; **có** guard vòng sau chưa vận hành (giống Calculate).

### 5.5–5.7. CRUD FinalResult thủ công
- **CreateFinalResult (upsert):** Bắt buộc đúng 1 trong 3 phạm vi (RoundId/EventId/TrackId); upsert theo khóa `(TeamId, RoundId, EventId, TrackId)`.
- **UpdateFinalResult:** Không chuyển kết quả sang vòng của event khác; chặn trùng đội trong cùng vòng.
- **DeleteFinalResult:** 3 nhóm được xóa — Admin, Owner (CreatedBy), hoặc EC của event chứa Round.

### 5.8. Truy vấn FinalResult
- `GetById`: ⚠️ **không lọc `IsPublished`** — bất kỳ user đăng nhập nào biết đúng Id đều xem được bản nháp.
- `GetByRoundId`: mặc định chỉ thấy `IsPublished=true`; EC/Admin thấy cả nháp.
- `GetByTeamId`: chỉ **Admin** thấy nháp (thận trọng hơn GetByRoundId vì trải nhiều event).

### 5.9. Gán giải thưởng (AssignPrize)
- **Ai:** EC (theo controller, handler không tự check lại).
- **Điều kiện:** Đếm `currentAssignedCount` **live** (`Count()`, không dùng bộ đếm giảm dần) so với `Prize.Quantity` — ✅ đã xác nhận không lệch off-by-one, không thể âm khi gỡ giải.
- ⚠️ Không kiểm tra `Prize.EventId` khớp với event của `FinalResult` — về lý thuyết có thể gán giải event A cho kết quả event B.

### 5.10–5.13. Prizes (CRUD + truy vấn)
- ⚠️ **`PrizesController` hoàn toàn không có `[Authorize]`/role check nào** — mở cho mọi caller kể cả chưa đăng nhập (xem mục 6).
- UpdatePrize không kiểm tra hạ `Quantity` xuống thấp hơn số đã gán hiện tại → có thể tạo trạng thái "vượt quota ngầm".
- DeletePrize không kiểm tra `FinalResult` đang tham chiếu → có thể tạo tham chiếu treo.

### 5.14. Domain shape — FinalResult & Prize
- `FinalResult`: đúng 1 trong 3 phạm vi (Round/Event/Track, không ràng buộc DB-level); `IsPublished` mặc định `false`.
- `Prize`: `Quantity` mặc định 1, `Value` là chuỗi tự do (không validate định dạng số).

### 5.15–5.19. UserRejections (chi tiết xem mục 1.4)

### 5.20. Demo — 2 sự kiện mẫu (SetupDemoEvents)
- ⚠️ **Không có `[Authorize]`** — mở cho mọi caller.
- Dọn dẹp mọi Event tên `"[DEMO]"` cũ trước khi tạo (theo đúng thứ tự tránh vỡ FK Restrict: Appeal → FinalResult → Score → EventRole → Event).
- Toàn bộ mốc thời gian hard-code tương đối theo `targetDate` truyền vào — Event 1 "Nộp Bài" canh đúng lúc đang mở nộp; Event 2 "Chấm Điểm" canh đúng lúc đã hết hạn nộp, đang mở chấm.
- **File:** `Commands/SetupDemoEvents/SetupDemoEventsCommandHandler.cs:24-307`

### 5.21. Demo — Sự kiện Phúc khảo (SetupDemoAppealEvent)
- ⚠️ Không `[Authorize]`; ⚠️ **không dọn dẹp demo cũ** trước khi chạy (khác SetupDemoEvents) — gọi lặp lại sẽ tạo trùng dữ liệu.
- Tạo sẵn 2 `Score` + 1 `Appeal` mẫu ở trạng thái `Pending`.
- **File:** `Commands/SetupDemoAppealEvent/SetupDemoAppealEventCommandHandler.cs:24-159`

---

## 6. Tổng hợp các vấn đề phát hiện được

> **Cập nhật 2026-08-13:** File này ban đầu được copy nguyên từ repo `SU26_SWP_SE1907_BACKEND` (cùng gốc SEAL) chứ chưa từng khảo sát riêng cho `SU26_SWP_BL3W_BE`. Đã chạy lại 3 agent đọc trực tiếp code hiện tại của repo này (không suy đoán từ doc) để xác minh — **toàn bộ 18 mục dưới đây đều xác nhận có thật trên BL3W, y hệt mô tả**, và đã được vá. Ngoài ra phát hiện thêm 3 điều BL3W-specific, cũng đã xử lý:
> - `UserRejectionsController` và `DemoController` bị **mất** `[Authorize]`/`[AdminAuthorize]` ngay trong lúc port code sang BL3W (commit `bd396dc`, 2026-08-13) — không phải lỗi kế thừa, đã vá lại.
> - `StorageController` (Upload/Download file) hoàn toàn không có auth — lỗ hổng có thật, có ở cả 2 repo, chưa từng nằm trong danh sách gốc — đã thêm `[Authorize]`.
> - `FptMockController` cũng không có auth nhưng là mock/stub test cho FE (dữ liệu giả cứng) — **cố ý không sửa**.

Danh sách dưới đây tổng hợp mọi bất nhất/lỗ hổng cụ thể mà 5 agent khảo sát phát hiện được khi đọc trực tiếp code (không phải suy đoán). Xếp theo mức độ nghiêm trọng giảm dần.

### 🔴 Nghiêm trọng — thiếu kiểm soát truy cập (Authorization)

| # | Controller/Handler | Vấn đề | File |
|---|---|---|---|
| 1 | `PrizesController` | Toàn bộ action (Create/Update/Delete/List Prize) **không có `[Authorize]`** — mở cho người chưa đăng nhập | `SEAL_Backend/Controllers/PrizesController.cs` |
| 2 | `UserRejectionsController` | Create/Update/List rejection không có `[Authorize]`; "phải là Admin" chỉ xác minh qua *dữ liệu* `RejectedBy`, không xác minh *người gọi* | `SEAL_Backend/Controllers/UserRejectionsController.cs`, `Commands/CreateUserRejection/CreateUserRejectionCommandHandler.cs:20-66` |
| 3 | `DemoController` | `SetupDemoEvents`/`SetupDemoAppealEvent` không `[Authorize]` — người ngoài có thể gọi để xóa/tạo dữ liệu demo | `SEAL_Backend/Controllers/DemoController.cs` |
| 4 | `GetAppealsByRound`/`GetAppealsByTeam`/`GetAssignedAppeals` | Chỉ cần đăng nhập, không kiểm tra người gọi có liên quan tới Round/Team/EventRole đang truy vấn hay không; `GetAssignedAppeals` tin tưởng `EventRoleId` client tự truyền, không đối chiếu `currentUserId` | `Queries/GetAppealsBy*/`, `Queries/GetAssignedAppeals/` |
| 5 | `GetFinalResultById` | Không lọc `IsPublished` — user đăng nhập bất kỳ xem được bản nháp nếu biết Id | `Queries/GetFinalResultById/GetFinalResultByIdQueryHandler.cs` |

### 🟡 Trung bình — bất đối xứng logic / race condition

| # | Vấn đề | File |
|---|---|---|
| 6 | `AddTeamMemberCommandHandler` **không có** bước chống race-condition đầy đội (đếm-rồi-ghi TOCTOU) như `RespondTeamInvitationCommandHandler` đã có — 2 request `AddTeamMember` đồng thời có thể vượt quá 5 thành viên | `Commands/AddTeamMember/AddTeamMemberCommandHandler.cs:133-156` vs `Commands/RespondTeamInvitation/RespondTeamInvitationCommandHandler.cs:226-252` |
| 7 | `AssignTemplateToTrackCommandHandler` không kiểm tra Track đã có `Score` hay chưa trước khi đổi Template — trong khi `UpdateTrackCommandHandler` có chặn trường hợp tương tự | `Commands/AssignTemplateToTrack/AssignTemplateToTrackCommandHandler.cs` vs `Commands/UpdateTrack/UpdateTrackCommandHandler.cs` |
| 8 | `UpdatePrizeCommandHandler` không kiểm tra hạ `Quantity` xuống dưới số đã gán hiện tại → có thể tạo trạng thái vượt quota ngầm | `Commands/UpdatePrize/UpdatePrizeCommandHandler.cs` |
| 9 | `DeletePrizeCommandHandler` không kiểm tra `FinalResult` đang tham chiếu `PrizeId` trước khi xóa → có thể để lại tham chiếu treo | `Commands/DeletePrize/DeletePrizeCommandHandler.cs` |
| 10 | `AssignPrizeCommandHandler` không kiểm tra `Prize.EventId` khớp event của `FinalResult` đang gán | `Commands/AssignPrize/AssignPrizeCommandHandler.cs` |
| 11 | `UpdateSchoolCommandHandler` không kiểm tra trùng tên (khác `CreateSchool` có kiểm tra) | `Commands/UpdateSchool/UpdateSchoolCommandHandler.cs` |
| 12 | `SetupDemoAppealEventCommandHandler` không dọn dữ liệu demo cũ trước khi chạy (khác `SetupDemoEvents`) → gọi lặp tạo dữ liệu trùng | `Commands/SetupDemoAppealEvent/SetupDemoAppealEventCommandHandler.cs` |

### 🟢 Nhẹ — lệch pha comment/code, thiếu field (dễ gây hiểu nhầm khi bảo trì)

| # | Vấn đề | File |
|---|---|---|
| 13 | `TransferTeamLeaderCommandHandler`: comment ghi "chờ chấp nhận trong 7 ngày" nhưng hằng số thực tế là `TimeSpan.FromHours(24)` (24 giờ) | `Commands/TransferTeamLeader/TransferTeamLeaderCommandHandler.cs:21-22` |
| 14 | `TeamInvitation.ExpiresAt`: doc-comment trên entity ghi "mặc định 48h" nhưng `InviteTeamMemberCommandHandler` dùng hằng số `INVITATION_EXPIRY_HOURS = 24` | `SEAL.Domain/Entity/TeamInvitation.cs:19` vs `Commands/InviteTeamMember/InviteTeamMemberCommandHandler.cs:23` |
| 15 | `MyTeamResponseModel` (dùng bởi `GetMyTeam`) thiếu field `LastRejectReason` mà `TeamDetailResponseModel` (`GetTeamById`) có — FE gọi `/my-team` không hiện được banner lý do bị từ chối như thiết kế | `Queries/GetMyTeam/Models/MyTeamResponseModel.cs` vs `Queries/GetTeamById/Models/TeamDetailResponseModel.cs:16-20` |
| 16 | `GetMyTeamInvitationQueryHandler` không lọc `ExpiresAt > now` — có thể trả về lời mời đã hết hạn nhưng chưa "lazy-expire" với `Status = "PendingAccept"`, trong khi `GetTeamInvitationsQueryHandler` tính `effectiveStatus` đúng | `Queries/GetMyTeamInvitation/GetMyTeamInvitationQueryHandler.cs:35-41` vs `Queries/GetTeamInvitations/GetTeamInvitationsQueryHandler.cs:51-56` |
| 17 | `DeleteUserRejectionCommandHandler`: XML-doc endpoint ghi "Soft Delete" nhưng thực chất là xóa cứng (`_dbSet.Remove`) | `Commands/DeleteUserRejection/DeleteUserRejectionCommandHandler.cs` |
| 18 | Nhiều controller khác cũng ghi "(Soft Delete)" trong XML-doc dù toàn bộ `GenericRepository.DeleteAsync` là hard-delete; trường `DeletedTime` tồn tại nhưng không có `HasQueryFilter` nào áp dụng trong `DatabaseContext` | `SEAL.Infrastructure/Persistence/Repositories/GenericRepository.cs:235-239` |

### Ghi chú
- Mục 1–5, 6-11 đã được các agent xác nhận qua đọc trực tiếp code — **không phải suy đoán**.
- Đề xuất ưu tiên xử lý: nhóm 🔴 trước (đặc biệt #1–#3, vì đây là các API có khả năng ghi/xóa dữ liệu mà không cần đăng nhập), sau đó #6 (race condition đội) vì đã có pattern chuẩn sẵn ở `RespondTeamInvitation` để tái sử dụng.
