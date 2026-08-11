# APPLICATION FEATURES PLAN - Tầng Application (Vertical Slice)

**Người thực hiện:** Leader  
**Mục tiêu:** Triển khai đầy đủ các Feature theo DEVELOPMENT_WORKFLOW.md (Bước 4)

Tài liệu này liệt kê **toàn bộ các Feature** cần viết cho các entity anh đang chịu trách nhiệm, đã cập nhật theo luồng quản lý tiêu chí qua Template.

Mỗi Feature thường bao gồm:
- Command / Query
- Handler  
- Request DTO + Response DTO

---

## 1. School Module

| STT | Feature Name                    | Type    | Folder                          | Ghi chú |
|-----|---------------------------------|---------|---------------------------------|--------|
| 1   | CreateSchool                    | Command | Schools/Commands                | Tạo trường học mới |
| 2   | UpdateSchool                    | Command | Schools/Commands                | Cập nhật thông tin trường |
| 3   | DeleteSchool                    | Command | Schools/Commands                | Xóa trường (soft delete) |
| 4   | GetSchoolById                   | Query   | Schools/Queries                 | Chi tiết 1 trường |
| 5   | GetAllSchools                   | Query   | Schools/Queries                 | Danh sách tất cả trường |
| 6   | GetSchoolsWithUserCount         | Query   | Schools/Queries                 | Danh sách trường + số user |

---

## 2. User Module (Ưu tiên cao nhất)

| STT | Feature Name                        | Type    | Folder                        | Ghi chú |
|-----|-------------------------------------|---------|-------------------------------|--------|
| 1   | RegisterUser                        | Command | Users/Commands                | Đăng ký tài khoản mới |
| 2   | LoginUser                           | Command | Users/Commands                | Đăng nhập + trả về JWT |
| 3   | ApproveUser                         | Command | Users/Commands                | Phê duyệt user |
| 4   | RejectUser                          | Command | Users/Commands                | Từ chối user + tạo UserRejection |
| 5   | UpdateUserProfile                   | Command | Users/Commands                | User cập nhật thông tin cá nhân |
| 6   | ChangePassword                      | Command | Users/Commands                | Đổi mật khẩu |
| 7   | GetUserById                         | Query   | Users/Queries                 | Chi tiết user |
| 8   | GetUsersBySchoolId                  | Query   | Users/Queries                 | Danh sách user theo trường |
| 9   | GetAllUsers                         | Query   | Users/Queries                 | Admin xem tất cả user (phân trang) |
| 10  | GetCurrentUser                      | Query   | Users/Queries                 | Thông tin user đang đăng nhập |

---

## 3. UserRejection Module

| STT | Feature Name                        | Type    | Folder                          | Ghi chú |
|-----|-------------------------------------|---------|---------------------------------|--------|
| 1   | CreateUserRejection                 | Command | UserRejections/Commands         | Tạo lý do từ chối |
| 2   | GetUserRejectionsByUserId           | Query   | UserRejections/Queries          | Xem lịch sử từ chối của 1 user |
| 3   | GetAllUserRejections                | Query   | UserRejections/Queries          | Admin xem toàn bộ lịch sử từ chối (Paging) |
| 4   | UpdateUserRejection                 | Command | UserRejections/Commands         | Admin sửa lại lý do nếu viết sai/thiếu |
| 5   | DeleteUserRejection                 | Command | UserRejections/Commands         | Xóa bản ghi (dùng khi thao tác nhầm) |

---

## 4. Event Module

| STT | Feature Name                        | Type    | Folder                       | Ghi chú |
|-----|-------------------------------------|---------|------------------------------|--------|
| 1   | CreateEvent                         | Command | Events/Commands              | Tạo sự kiện mới |
| 2   | UpdateEvent                         | Command | Events/Commands              | Cập nhật sự kiện |
| 3   | DeleteEvent                         | Command | Events/Commands              | Xóa sự kiện |
| 4   | GetEventById                        | Query   | Events/Queries               | Chi tiết 1 event |
| 5   | GetAllEvents                        | Query   | Events/Queries               | Danh sách tất cả event |
| 6   | GetUpcomingEvents                   | Query   | Events/Queries               | Event sắp diễn ra |

---

## 5. Round Module

| STT | Feature Name                        | Type    | Folder                        | Ghi chú |
|-----|-------------------------------------|---------|-------------------------------|--------|
| 1   | CreateRound                         | Command | Rounds/Commands               | Tạo Round |
| 2   | UpdateRound                         | Command | Rounds/Commands               | Cập nhật Round |
| 3   | DeleteRound                         | Command | Rounds/Commands               | Xóa Round |
| 4   | GetRoundById                        | Query   | Rounds/Queries                | Chi tiết Round |
| 5   | GetRoundsByEventId                  | Query   | Rounds/Queries                | Danh sách Round của Event |

---

## 6. Track Module (Cập nhật liên kết Template)

| STT | Feature Name                        | Type    | Folder                        | Ghi chú |
|-----|-------------------------------------|---------|-------------------------------|--------|
| 1   | CreateTrack                         | Command | Tracks/Commands               | Tạo Track |
| 2   | UpdateTrack                         | Command | Tracks/Commands               | Cập nhật Track |
| 3   | DeleteTrack                         | Command | Tracks/Commands               | Xóa Track |
| 4   | AssignTemplateToTrack               | Command | Tracks/Commands               | Gán hoặc thay đổi Template cho Track |
| 5   | GetTrackById                        | Query   | Tracks/Queries                | Chi tiết Track (kèm theo thông tin Template đang áp dụng) |
| 6   | GetTracksByRoundId                  | Query   | Tracks/Queries                | Danh sách Track thuộc một Vòng thi (Round) |

---

## 7. Template Module (Thêm mới)

| STT | Feature Name                        | Type    | Folder                        | Ghi chú |
|-----|-------------------------------------|---------|-------------------------------|--------|
| 1   | CreateTemplate                      | Command | Templates/Commands            | Tạo mẫu tiêu chí mới |
| 2   | UpdateTemplate                      | Command | Templates/Commands            | Cập nhật tên/mô tả mẫu tiêu chí |
| 3   | DeleteTemplate                      | Command | Templates/Commands            | Xóa mẫu tiêu chí |
| 4   | AddCriteriaToTemplate               | Command | Templates/Commands            | Thêm tiêu chí vào mẫu + cấu hình weight, max_score (Template_Criteria) |
| 5   | RemoveCriteriaFromTemplate          | Command | Templates/Commands            | Gỡ tiêu chí ra khỏi mẫu |
| 6   | UpdateTemplateCriteriaConfig        | Command | Templates/Commands            | Chỉnh sửa weight hoặc max_score của tiêu chí trong mẫu |
| 7   | GetTemplateById                     | Query   | Templates/Queries             | Xem chi tiết mẫu (bao gồm danh sách tiêu chí bên trong) |
| 8   | GetAllTemplates                     | Query   | Templates/Queries             | Lấy danh sách toàn bộ các mẫu để chọn khi cấu hình Track |

---

## 8. Criteria Module (Cập nhật theo thực thể độc lập)

| STT | Feature Name                        | Type    | Folder                         | Ghi chú |
|-----|-------------------------------------|---------|--------------------------------|--------|
| 1   | CreateCriteria                      | Command | Criterias/Commands             | Tạo tiêu chí chấm điểm gốc (Sáng tạo, Khả thi...) |
| 2   | UpdateCriteria                      | Command | Criterias/Commands             | Cập nhật thông tin tiêu chí gốc |
| 3   | DeleteCriteria                      | Command | Criterias/Commands             | Xóa tiêu chí gốc |
| 4   | GetAllCriteria                      | Query   | Criterias/Queries              | Lấy danh sách tiêu chí gốc để admin chọn đưa vào Template |
| 5   | GetCriteriaById                     | Query   | Criterias/Queries              | Chi tiết 1 tiêu chí |

---

## 9. EventRole Module (Rất quan trọng)

| STT | Feature Name                            | Type    | Folder                          | Ghi chú |
|-----|-----------------------------------------|---------|---------------------------------|--------|
| 1   | AssignEventRole                         | Command | EventRoles/Commands             | Gán vai trò cho User trong Event |
| 2   | UpdateEventRole                         | Command | EventRoles/Commands             | Cập nhật vai trò |
| 3   | RemoveEventRole                         | Command | EventRoles/Commands             | Thu hồi vai trò |
| 4   | GetEventRolesByEventId                  | Query   | EventRoles/Queries              | Danh sách vai trò trong 1 Event |
| 5   | GetEventRolesByUserId                   | Query   | EventRoles/Queries              | User có vai trò ở những Event nào |
| 6   | GetUsersByRoleInEvent                   | Query   | EventRoles/Queries              | Lấy danh sách Judge/Organizer... trong Event |
| 7   | CheckUserHasRoleInEvent                 | Query   | EventRoles/Queries              | Kiểm tra User có vai trò cụ thể (dùng cho Policy) |

---

## Khuyến nghị Thứ tự Triển khai

1. **School** → **User** → **UserRejection**
2. **Event** → **Round** → **Criteria** → **Template** → **Track** (Phải làm Criteria và Template trước để có mẫu gán vào Track)
3. **EventRole**

---

**Lưu ý:**
- Nên tạo folder theo cấu trúc: `Features/[ModuleName]/[Commands|Queries]/`
- Sử dụng FluentValidation cho Request DTO
- Xử lý Authorization trong Handler hoặc bằng Policy