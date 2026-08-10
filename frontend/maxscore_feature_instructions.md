# Hướng dẫn triển khai tính năng cấu hình Thang điểm linh hoạt (Bộ tiêu chí)

> [!IMPORTANT]
> Mục tiêu: Loại bỏ hoàn toàn việc hard-code thang điểm 10 ở Frontend. Tính toán và lưu trực tiếp giá trị `maxScore` cho từng tiêu chí dựa trên tổng điểm và trọng số (`weight`), gửi xuống Backend để lưu cố định.

## 1. Cập nhật UI (Form tạo/sửa Bộ tiêu chí)

Thêm một nhóm control (Dropdown / Radio buttons) cho phép Admin chọn **"Thang điểm tổng"** (Base Scale) cho bộ tiêu chí:
- **Thang 10** (Mặc định)
- **Thang 100**
- **Tùy chỉnh** (Custom)

**Logic Component:**
- Trạng thái state quản lý: `scaleType` (10 | 100 | custom) và `customScaleValue` (number).
- Nếu chọn "Tùy chỉnh", hiển thị thêm một input number (bắt buộc nhập, giá trị > 0) để Admin điền tổng điểm tối đa (ví dụ: `900`, `1000`).

## 2. Logic tính toán tự động (Reactivity)

Mỗi tiêu chí có 2 giá trị số chính: `weight` (trọng số, tính theo %) và `maxScore`.

**Công thức tính maxScore cơ bản:**
```javascript
const X = scaleType === 'custom' ? customScaleValue : Number(scaleType);
let maxScore = (weight / 100) * X;
```

**Auto-update:**
- Frontend phải cập nhật lại `maxScore` của toàn bộ tiêu chí **ngay lập tức** khi có bất kỳ sự thay đổi nào ở `scaleType`, `customScaleValue`, hoặc `weight`.
- Giao diện input `maxScore` nên ở trạng thái `disabled` (read-only) vì đây là Computed value.

## 3. Vấn đề làm tròn số (Rounding) và Sai số cộng dồn

Với các trường hợp chia lẻ (ví dụ 3 tiêu chí cùng `weight = 33.33%` trên thang 10), `maxScore` sẽ ra số thập phân dài vô hạn (3.33333333333...). Nếu không xử lý, hàm tính tổng lúc hiển thị có thể gặp lỗi sai số floating point (ví dụ tổng ra 9.999 thay vì 10).

**Cách xử lý (FE tự quyết định format phù hợp):**
- **Làm tròn từng phần tử:** Làm tròn `maxScore` về 2 hoặc 3 chữ số thập phân trước khi lưu.
  ```javascript
  maxScore = Math.round(((weight / 100) * X) * 100) / 100;
  ```
- **Xử lý hao hụt số dư ở phần tử cuối:** Để đảm bảo tổng các `maxScore` luôn bằng chính xác `X`, đối với phần tử cuối cùng trong mảng tiêu chí, FE không nên tính bằng công thức nhân mà tính bằng công thức trừ:
  ```javascript
  const lastMaxScore = X - (tổng các maxScore đã làm tròn của n-1 phần tử trước đó);
  ```

## 4. Xử lý trường hợp `weight = 0`

- Một tiêu chí có `weight = 0` (dẫn đến `maxScore = 0`) thông thường có nghĩa là tiêu chí đó mang tính chất tham khảo hoặc không tính vào tổng điểm.
- **Quyết định (Cần confirm với BA/PO):** Nếu nghiệp vụ không cho phép tiêu chí "vô tác dụng" như vậy tồn tại, FE cần thêm Validate để chặn: **bắt buộc `weight > 0` cho mọi tiêu chí**.

## 5. Validation trước khi Lưu

- Cập nhật rule: `weight` của mỗi tiêu chí phải `> 0` (nếu chốt không cho phép `weight = 0`).
- **Tổng `weight`** của tất cả các tiêu chí trong mảng phải chính xác bằng `100%`.
- Nếu chọn Thang điểm Tùy chỉnh, giá trị `customScaleValue` phải hợp lệ (số dương > 0).

## 6. Chuẩn bị Payload gọi API

Không gửi thông tin Thang điểm (`scaleType`, `customScaleValue`) xuống Backend. Gửi payload danh sách tiêu chí với `weight` và `maxScore` đã làm tròn chính xác.

Ví dụ bộ 3 tiêu chí, **Thang điểm 10**, weight `[33.33, 33.33, 33.34]`:
```json
{
  "criteria": [
    { "name": "Tiêu chí 1", "weight": 33.33, "maxScore": 3.33 },
    { "name": "Tiêu chí 2", "weight": 33.33, "maxScore": 3.33 },
    { "name": "Tiêu chí 3", "weight": 33.34, "maxScore": 3.34 }
  ]
}
```

## 7. Xử lý phần hiển thị / Chấm thi (Dọn dẹp Hardcode)

Khi đã triển khai tính năng trên, gỡ bỏ triệt để hard-code 10 ở các modal hiển thị (vd: `EditModal.jsx`, `utils.jsx`).

- Gỡ bỏ hằng số `TOTAL_MAX = 10`.
- **Tổng điểm tối đa (Total Max):** Tính dynamic bằng cách sum `maxScore`:
  ```javascript
  const dynamicTotalMax = criteria.reduce((sum, c) => sum + (c.maxScore || 0), 0);
  // (Đã xử lý làm tròn lúc tạo form nên lúc sum sẽ giảm rủi ro số lẻ floating point)
  ```
- Hàm `calcScoreNormalized` chỉ việc cộng dồn điểm thực tế (`scores[i]`), không cần chia tỷ lệ trên 10 mặc định nữa. Mọi tỷ lệ đã được quy định cứng bởi `maxScore` lưu dưới DB.
