# Feedback của cô — Tiến độ xử lý & Đề xuất

_Cập nhật 02/07/2026 · Nhánh `loc/fix/feedback-co` (cả BE lẫn FE) · Commit chia theo từng chức năng · **CHƯA push** — chờ review._

> Mỗi mục ✅ đều đã **chạy thật** để kiểm chứng (Swagger/curl + đối chiếu DB, và test tay trên FE với Playwright), không chỉ đọc code.

## Tóm tắt tiến độ

- **Đã xong: ~26 mục** (chấm điểm, phân vai trò, vòng đời đội/bài nộp, hồ sơ/reject/auth, UX cảnh báo, minh bạch điểm, ẩn danh chấm).
- **Còn lại: 8 mục** — phần lớn vướng quyết định (đổi schema/migration) hoặc là đề xuất mới phát sinh khi review (xem mục **C. Đề xuất cần làm**).
- **BE:** 18 commit · **FE:** 13 commit.

---

## A. 6 điểm cô nêu

| # | Nội dung | Trạng thái | Ở đâu |
|---|----------|------------|-------|
| 1 | Thiếu chỗ xem "đang mời" phía người gửi | ✅ **Xong** | BE `3349912` GET team invitations · FE `31e6330` mục "Lời mời đã gửi" |
| 2 | 1 người vừa Giám khảo vừa Mentor 1 hạng mục | ✅ **Xong** | BE `e634ba6` (chặn ở cả 2 luồng mời) |
| 3 | Bộ tiêu chí sửa lại theo **hệ 10** | ✅ **Xong** | BE `d799cb4` công thức hệ 10 có trọng số · FE `6cf8293` form tiêu chí |
| 4 | Chưa hiển thị thời gian đăng ký sự kiện | ❌ **Chưa** (cần đổi schema Event) | → mục C1 |
| 5 | Sửa nhãn "updated profile" | ✅ **Xong** | FE `bed0893` "Hồ sơ thí sinh" + trạng thái chưa-đăng-ký |
| 6 | Chạy toàn bộ local | ✅ **Xong** | Config BE + FE trỏ localhost |

---

## B. Đã xử lý thêm (tự review nghiệp vụ + cô/bạn nêu thêm)

### B.1 — Chấm điểm (đúng nghiệp vụ chấm thi)
| Vấn đề | Commit |
|--------|--------|
| **Trọng số bị bỏ qua khi tính điểm** — nay `Điểm = Σ(value/maxScore × weight/100) × 10` (hệ 10 có trọng số) | BE `d799cb4` |
| **Bắt chấm đủ tất cả tiêu chí** (trước chấm thiếu vẫn lưu) | BE `445f05e` |
| **Chặn tính kết quả khi chưa đủ giám khảo chấm** (đội chưa chấm bị 0 điểm → loại oan) | BE `2b5399b` |
| **Giám khảo không được chấm bài đội mình** (xung đột lợi ích) | BE `ad79b2f` |
| **Khóa điểm sau khi công bố kết quả** | BE `0ff49ef` |
| **Xử lý đồng hạng** — xếp hạng chuẩn "1-1-3" (đội bằng điểm cùng hạng); đồng hạng ở vạch top:N → tất cả cùng thăng (có chủ đích) | BE `8f1ac38` (đã có sẵn) |
| **Thí sinh xem breakdown điểm** theo từng giám khảo × tiêu chí (minh bạch) | BE `913fd1f` + `466f654` · FE `f5c3244` |
| **Ẩn danh phía chấm** — giám khảo thấy "Bài nộp #N", không biết chấm đội nào | FE `c8fd6ea` |

### B.2 — Phân vai trò (tránh xung đột / gian lận)
| Vấn đề | Commit |
|--------|--------|
| Không cho 1 người vừa Giám khảo vừa Mentor 1 hạng mục | BE `e634ba6` |
| **Thí sinh (thành viên đội) không được mời làm Giám khảo/Mentor** | BE `1a6b239` + `a420f6d` |

### B.3 — Vòng đời đội & bài nộp
| Vấn đề | Commit |
|--------|--------|
| **Chỉ đội đã đăng ký chính thức (Registered) mới nộp bài** | BE `92feb0d` |
| **Không thêm/xóa thành viên khi đội đã khóa** | BE `5235ea9` + `dc11d86` |
| **Không sửa/xóa bài nộp sau khi công bố kết quả** (chống đổi bài đã chấm) | BE `6ce2ae6` |
| **Chuyển quyền Trưởng nhóm** — bịt bế tắc leader muốn rời phải xóa cả đội | BE `bf7898f` · FE `e4ec60e` |
| Ẩn form "Mời thành viên" với thành viên thường (chỉ trưởng nhóm) | FE `cc437a6` |

### B.4 — Hồ sơ / Reject / Auth
| Vấn đề | Commit |
|--------|--------|
| Nhãn "Hồ sơ thí sinh" + tách trạng thái *chưa đăng ký* / *chờ duyệt* | FE `bed0893` |
| **Sửa lỗi trạng thái "bị từ chối" không bao giờ hiện** (double-unwrap) | FE `80188c7` |
| **Nút "Yêu cầu gỡ khóa"** cho tài khoản bị từ chối ≥ 2 lần | FE `d70d21f` |
| Sửa link email gỡ khóa `/admin` (không tồn tại) → `/users` | BE `22c70de` |
| **Quên / Đặt lại mật khẩu** (trước thiếu cả endpoint lẫn trang) | BE `90d9384` · FE `527fe3d` |

### B.5 — UX (cô nêu: phân biệt cảnh báo vs thông báo)
| Vấn đề | Commit |
|--------|--------|
| **Thêm CẢNH BÁO (warning)** — vàng + ⚠, tách khỏi thông báo xanh/đỏ | FE `b8c4e70` |
| **Cảnh báo khi tạo sự kiện chưa gắn giám khảo** (không chặn cứng) | FE `1c6d66b` |

### B.6 — Khác
| Vấn đề | Commit |
|--------|--------|
| GET danh sách bộ tiêu chí trả kèm tiêu chí (chống chọn trùng) | BE `fa77227` |
| AsNoTracking cho query read-only (hiệu năng) | BE `cfb9f2b` |

---

## C. ĐỀ XUẤT CẦN LÀM (chưa xử lý)

### 🔴 Ưu tiên cao — nghiệp vụ cốt lõi
| # | Việc | Vướng mắc / ghi chú |
|---|------|---------------------|
| C1 | **Mốc thời gian đăng ký sự kiện** (#4) + **gate thời gian** nộp bài/chấm (B3, C8) | Cần **thêm cột vào Event** (RegistrationStart/Deadline) = **migration**. Vướng: migration bị gitignore + không auto-migrate → cột mới không lan qua git, DB team thiếu cột sẽ lỗi 500. **Cần chốt cách team đồng bộ schema trước.** |
| C2 | **Event.Status → enum trạng thái** (nháp/mở đăng ký/đang chấm/đã công bố) (C7) | Nền tảng cho C1. Cũng là đổi schema (như trên). |
| C3 | **Nối API thật cho luồng CHẤM ĐIỂM** | ⚠️ Phát hiện khi review: `ScoringPanel` (tab Chấm điểm của giám khảo) **đang là MOCK** (Team Alpha/Beta/Gamma cứng, "xóa khi có API thật"). Luồng chấm điểm FE **chưa hoàn thiện** — cần nối vào SubmitResults/Criteria thật (giữ nhãn ẩn danh "Bài nộp #N"). |

### 🟠 Ưu tiên vừa — chính sách / hoàn thiện
| # | Việc | Ghi chú |
|---|------|---------|
| C4 | **Chính sách ẩn danh link bài nộp** | URL repo/demo do đội tự đặt (github.com/**tên-đội**) tự lộ danh tính — FE không che được (giám khảo cần link để chấm). Cần yêu cầu team đặt tên ẩn danh khi nộp, hoặc BE proxy link. |
| C5 | **Thông báo trong web khi mời Giám khảo/Mentor/EC** | Hiện chỉ gửi email; hệ `EventRoleInvitation`/NotificationBell đã có nhưng FE không gọi `/EventRoles/invite`. Cần chốt hướng: (a) mời-chờ-chấp-nhận qua chuông, (b) thông báo thuần, (c) giữ email. |
| C6 | **Auto-scroll/focus tới ô lỗi khi submit form fail** (D4) | UX — user không thấy lỗi ở đâu trên form dài. |

### 🟡 Ưu tiên thấp — nit / kỹ thuật
| # | Việc | Ghi chú |
|---|------|---------|
| C8 | Chuẩn hoá **404 vs 400** cho tài nguyên không tồn tại; sửa comment "Soft Delete" nhưng hard delete (B4) | Nit toàn codebase. |
| C9 | `getProfile` không tính `IsRejected` → field trả sai (B6) | FE chưa phụ thuộc nên chưa lỗi, nhưng nên tính hoặc bỏ field. |
| C10 | **Dọn 22 unit test fail** (4 class Team CRUD) | Test cũ lệch handler sau khi refactor sang `.Entities` (không phải bug sản phẩm). Nên viết lại bằng EF Core InMemory. |

### ⏸️ Won't-fix / By-design
| # | Việc | Lý do |
|---|------|-------|
| — | Reject văng đội khỏi **TẤT CẢ** event (B2) | `IsApproved` là cờ toàn cục (dùng ở ConfirmRegistration/RespondInvitation) → cascade-all là nhất quán. Muốn scope theo event phải **redesign approval per-event**. |

---

## Phụ lục — cách kiểm chứng
- **BE:** dựng bản build riêng chạy trên DB demo `seal_demo`, gọi thật từng API bằng Swagger/curl **đúng cách FE gửi**, đối chiếu dữ liệu DB; mỗi rule test cả ca hợp lệ (200) lẫn ca vi phạm (400/403).
- **FE:** chạy `next dev` trỏ BE local, thao tác tay đúng luồng người dùng thật (Playwright + Chrome), kiểm request/response + trạng thái hiển thị + chụp màn hình.

## Lưu ý khi review
- Toàn bộ trên nhánh **`loc/fix/feedback-co`** (BE + FE), **chưa push**.
- Không commit file test lên nhánh.
- BE có sẵn **~22 unit test fail từ trước** (không liên quan các fix này — xem C10) → lưu ý khi chạy `dotnet test`.
