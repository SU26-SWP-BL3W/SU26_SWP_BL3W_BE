# Luồng Criteria & Template

## Tổng quan

Hệ thống chấm điểm chia làm 2 tầng:

```
Tiêu chí (pool)   →   Bộ tiêu chí (template)   →   Chấm điểm sự kiện
   master data            cấu hình riêng
```

---

## Routes

| URL | Chức năng |
|-----|-----------|
| `/criteria` | Quản lý **bộ tiêu chí** (template) |
| `/criteria/pool` | Quản lý **kho tiêu chí** (pool) |

---

## 1. Kho tiêu chí — `/criteria/pool`

**File:** `src/views/CriteriaPage.jsx`  
**Service:** `src/services/criteriaService.js`

### Chức năng
- Thêm / Sửa / Xóa tiêu chí
- Bật / Tắt tiêu chí (`isActive`) — tiêu chí tắt sẽ không xuất hiện trong dropdown khi thêm vào template

### API
| Method | Endpoint | Mô tả |
|--------|----------|-------|
| GET | `/Criterias` | Lấy danh sách |
| POST | `/Criterias` | Tạo mới |
| PUT | `/Criterias/{id}` | Cập nhật |
| DELETE | `/Criterias/{id}` | Xóa |
| PATCH | `/Criterias/{id}/toggle-status` | Bật/tắt |

### Data shape
```js
{
  id:       string,
  label:    string,   // criteriaName
  desc:     string,   // description
  isActive: boolean,
  weight:   number,   // từ template (0 nếu chưa gán)
  maxScore: number,   // từ template (mặc định 10)
}
```

---

## 2. Bộ tiêu chí (Template) — `/criteria`

**File:** `src/views/TemplatePage.jsx`  
**Service:** `src/services/templateService.js`

### Chức năng
- Tạo / Sửa / Xóa bộ tiêu chí
- Click vào bộ để xem chi tiết (lazy load)
- Thêm / Sửa / Gỡ tiêu chí trong bộ — mỗi tiêu chí có:
  - **Trọng số (%)** — validate tổng không vượt 100%
  - **Điểm tối đa**
- Thanh màu hiển thị phân bổ trọng số

### API
| Method | Endpoint | Mô tả |
|--------|----------|-------|
| GET | `/Templates` | Lấy danh sách |
| GET | `/Templates/{id}` | Chi tiết (có criterias) |
| POST | `/Templates` | Tạo mới |
| PUT | `/Templates/{id}` | Cập nhật |
| DELETE | `/Templates/{id}` | Xóa |
| POST | `/Templates/{id}/criterias` | Thêm tiêu chí vào bộ |
| PUT | `/Templates/{id}/criterias/{criteriaId}` | Sửa cấu hình tiêu chí |
| DELETE | `/Templates/{id}/criterias/{criteriaId}` | Gỡ tiêu chí khỏi bộ |

### Data shape
```js
// Template
{
  id:          string,
  name:        string,
  description: string,
  isSystem:    boolean,  // true → khoá, chỉ xem
  criterias:   TemplateCriteria[],
}

// TemplateCriteria
{
  criteriaId:   string,
  criteriaName: string,
  description:  string,
  weight:       number,  // % trọng số
  maxScore:     number,  // điểm tối đa
}
```

### Lưu ý
- Template bị **khoá** (chỉ xem, không sửa/xóa) khi:
  - `isSystem === true`
  - Tên là `"Standard Pitch Deck Evaluation"`

---

## Quan hệ dữ liệu

```
Pool tiêu chí (master)         Bộ tiêu chí "Kỳ 1 2024"
──────────────────────         ────────────────────────────────────
Tính sáng tạo        ───────→  Tính sáng tạo      | 30% | max 10đ
Khả năng trình bày   ───────→  Khả năng trình bày  | 40% | max 10đ
Tính khả thi         ───────→  Tính khả thi        | 30% | max 10đ
Tính đổi mới         ✗ (chưa thêm vào bộ này)
```

> Một tiêu chí có thể xuất hiện trong **nhiều bộ** với trọng số khác nhau.  
> Xóa tiêu chí khỏi pool không tự động gỡ khỏi template.

---

## Notification pattern

Cả 2 trang dùng hàm `sn(message, type)` truyền từ route xuống view:

```js
sn('Đã lưu!', 's')   // success — màu xanh
sn('Lỗi rồi', 'e')   // error   — màu đỏ
```

Toast tự ẩn sau **3 giây**.
