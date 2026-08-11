# Tài liệu Mô tả Mối quan hệ giữa các Thực thể (ERD)

Tài liệu này chuẩn hóa và mô tả chi tiết mối quan hệ giữa các thực thể cốt lõi trong hệ thống quản lý cuộc thi/sự kiện dựa trên sơ đồ ERD, đồng thời đã hiệu chỉnh lại cách quản lý tiêu chí chấm điểm của **Track** thông qua hệ thống **Template** (Mẫu tiêu chí) để tối ưu hóa việc tái sử dụng dữ liệu trong thực tế.

---

## 1. Danh sách các Thực thể (Entities)

* **School (Trường học):** Đại diện cho các tổ chức giáo dục tham gia hệ thống.
* **User (Người dùng):** Các cá nhân trong hệ thống (thí sinh, giám khảo, ban tổ chức...). Có thuộc tính `isAdmin` để phân quyền.
* **UserRejection (Từ chối người dùng):** Lưu trữ lịch sử hoặc lý do một tài khoản/yêu cầu của người dùng bị từ chối.
* **Event (Sự kiện / Cuộc thi):** Thực thể trung tâm, đại diện cho một cuộc thi hoặc sự kiện được tổ chức.
* **EventRole (Vai trò trong sự kiện):** Thực thể trung gian xác định vai trò cụ thể của một `User` khi tham gia vào một `Event` cụ thể (ví dụ: Thí sinh, Giám khảo, Ban tổ chức).
* **Round (Vòng thi):** Các giai đoạn hoặc vòng đấu khác nhau nằm trong một `Event` (ví dụ: Vòng loại, Vòng bán kết, Vòng chung kết).
* **Track (Hạng mục / Bảng đấu):** Các phân nhánh hoặc bảng thi đấu nhỏ hơn nằm trong một `Round` (ví dụ: Bảng thi Lập trình, Bảng thi Thiết kế).
* **Template (Mẫu tiêu chí):** Bộ khung hoặc mẫu chấm điểm (ví dụ: Mẫu chấm thi Thuyết trình, Mẫu chấm thi Technical). Được dùng để áp dụng chung cho một hoặc nhiều Track.
* **Criteria (Tiêu chí chấm điểm):** Các quy tắc, tiêu chuẩn đánh giá nguyên tử (ví dụ: Tính sáng tạo, Tính khả thi, Kỹ năng team-work).

---

## 2. Mô tả chi tiết các Mối quan hệ (Relationships)

### 2.1. School và User
* **Loại quan hệ:** 1 - Nhiều (1:N)
* **Ký hiệu trên sơ đồ:** `School` --(1)--- **HAS** ---(N)--> `User`
* **Mô tả:** Một trường học (`School`) có thể có nhiều người dùng (`User`) đăng ký tham gia. Ngược lại, mỗi người dùng tại một thời điểm chỉ thuộc về và đại diện cho một trường học duy nhất.

### 2.2. User và UserRejection
* **Loại quan hệ:** 1 - Nhiều (1:N)
* **Ký hiệu trên sơ đồ:** `User` --(1)--- **HAS** ---(N)--> `UserRejection`
* **Mô tả:** Một người dùng (`User`) có thể có nhiều bản ghi từ chối (`UserRejection`) trong lịch sử hoạt động hệ thống. Mỗi bản ghi từ chối chỉ gắn liền với một người dùng cụ thể.

### 2.3. User và EventRole (Qua mối quan hệ JOIN)
* **Loại quan hệ:** 1 - Nhiều (1:N)
* **Ký hiệu trên sơ đồ:** `User` --(1)--- **JOIN** ---(N)--> `EventRole`
* **Mô tả:** Một người dùng (`User`) có thể tham gia vào nhiều vai trò khác nhau trong các sự kiện khác nhau (`EventRole`). Mỗi bản ghi vai trò cụ thể chỉ thuộc về một người dùng duy nhất.

### 2.4. Event và EventRole (Qua mối quan hệ BELONG)
* **Loại quan hệ:** 1 - Nhiều (1:N)
* **Ký hiệu trên sơ đồ:** `Event` --(1)--- **BELONG** ---(N)--> `EventRole`
* **Mô tả:** Một sự kiện (`Event`) có thể có nhiều thành viên tham gia với các vai trò khác nhau (`EventRole`). Một vai trò trong sự kiện cụ thể chỉ thuộc về duy nhất một sự kiện đó.

### 2.5. Event và Round
* **Loại quan hệ:** 1 - Nhiều (1:N)
* **Ký hiệu trên sơ đồ:** `Event` --(1)--- **HAS** ---(N)--> `Round`
* **Mô tả:** Một cuộc thi hoặc sự kiện (`Event`) sẽ bao gồm một hoặc nhiều vòng thi (`Round`). Ngược lại, một vòng thi cụ thể bắt buộc phải nằm trong một sự kiện duy nhất.

### 2.6. Round và Track
* **Loại quan hệ:** 1 - Nhiều (1:N)
* **Ký hiệu trên sơ đồ:** `Round` --(1)--- **HAS** ---(N)--> `Track`
* **Mô tả:** Trong một vòng thi (`Round`) có thể chia làm nhiều bảng đấu hoặc hạng mục thi đấu độc lập (`Track`). Mỗi bảng đấu (`Track`) chỉ được định nghĩa bên trong một vòng thi cụ thể.

### 2.7. Template và Track (Cập nhật mới)
* **Loại quan hệ:** 1 - Nhiều (1:N)
* **Ký hiệu trên sơ đồ:** `Template` --(1)--- **HAS** ---(N)--> `Track`
* **Mô tả:** Một mẫu tiêu chí (`Template`) có thể được áp dụng gán cho nhiều hạng mục thi đấu (`Track`) khác nhau để chấm điểm. Tuy nhiên, mỗi `Track` tại một thời điểm chỉ áp dụng duy nhất một bộ `Template`.

### 2.8. Template và Criteria (Qua thực thể liên kết Template_Criteria)
* **Loại quan hệ:** Nhiều - Nhiều (N:N)
* **Ký hiệu trên sơ đồ:** * `Template` --(1)--- **HAS** ---(N)--> `Template_Criteria`
  * `Criteria` --(1)--- **HAS** ---(N)--> `Template_Criteria`
* **Mô tả:** Một `Template` bao gồm nhiều tiêu chí đánh giá (`Criteria`) chi tiết bên trong. Ngược lại, một tiêu chí (`Criteria`) nguyên tử cũng có thể nằm trong nhiều bộ `Template` khác nhau. Mối quan hệ này được cấu trúc hóa thông qua thực thể trung gian `Template_Criteria`.

---

## 3. Gợi ý thiết kế bảng Cơ sở dữ liệu (Database Schema)

Dựa trên sơ đồ cấu trúc mới, hệ thống bảng để lưu trữ phần Tiêu chí và Hạng mục thi được thiết kế như sau:

### Bảng: `templates` (Mẫu tiêu chí)
* `id` (Primary Key)
* `template_name` (Tên mẫu, ví dụ: Mẫu chấm Chung kết, Mẫu Hackathon)
* `description` (Mô tả chung)

### Bảng: `tracks` (Hạng mục thi)
* `id` (Primary Key)
* `round_id` (Foreign Key trỏ đến bảng `rounds`)
* `template_id` (Foreign Key trỏ đến bảng `templates` - Thể hiện mối quan hệ 1:N)
* `track_name` (Tên hạng mục)

### Bảng: `criteria` (Tiêu chí chấm điểm gốc)
* `id` (Primary Key)
* `criteria_name` (Tên tiêu chí: Sáng tạo, Khả thi, Code Quality...)
* `description` (Mô tả chi tiết tiêu chí)

### Bảng trung gian: `template_criteria` (Chi tiết tiêu chí trong mẫu)
* `template_id` (Foreign Key trỏ đến bảng `templates`)
* `criteria_id` (Foreign Key trỏ đến bảng `criteria`)
* `weight` (Trọng số/Hệ số điểm của tiêu chí trong mẫu này)
* `max_score` (Điểm tối đa của tiêu chí trong mẫu này)
* *Primary Key cấu thành từ cặp (`template_id`, `criteria_id`)*