# Hướng dẫn tự dựng CI/CD deploy (không dùng lại Jenkins/Portainer cũ)

Không cần VPS, không cần quản lý server thủ công. Dùng 2 nền tảng miễn phí, tích hợp thẳng với GitHub (mỗi lần push vào `main` là tự deploy):

- **Frontend** → **Vercel** (đúng nền tảng dự án cũ từng dùng cho Next.js — `swp391-frontend.vercel.app`)
- **Backend + Database** → **Render** (hỗ trợ Docker sẵn, có Postgres miễn phí)

## Phần 1 — Deploy Frontend lên Vercel

1. Vào **https://vercel.com** → **Sign up** → chọn **Continue with GitHub** (đăng nhập bằng tài khoản GitHub đang có quyền trên org `SU26-SWP-BL3W`).
2. Sau khi đăng nhập, bấm **Add New → Project**.
3. Vercel hiện danh sách repo GitHub — tìm và chọn **`SU26_SWP_BL3W_FE`** → **Import**.
4. Vercel tự nhận diện đây là Next.js, không cần đổi gì ở mục **Build & Output Settings**.
5. Mục **Environment Variables**, thêm 1 biến:
   - Name: `NEXT_PUBLIC_API_URL`
   - Value: để tạm `http://localhost:5180/api` — **sẽ quay lại sửa thành URL Render thật ở Phần 3**.
6. Bấm **Deploy**. Sau ~1-2 phút có URL dạng `https://su26-swp-bl3w-fe.vercel.app`.
7. Từ nay, mỗi lần push code lên nhánh `main` của repo FE, Vercel tự động build + deploy lại — không cần làm gì thêm.

## Phần 2 — Deploy Backend + Database lên Render

1. Vào **https://render.com** → **Get Started** → đăng nhập bằng GitHub (tài khoản có quyền trên org `SU26-SWP-BL3W`).
2. Bấm **New +** → **Blueprint**.
3. Chọn repo **`SU26_SWP_BL3W_BE`** (org `SU26-SWP-BL3W`) → **Connect**.
4. Render sẽ tự đọc file `backend/render.yaml` đã có sẵn trong repo (mình đã tạo) — hiện lên 2 thứ sẽ tạo: web service `seal-bl3w-backend` + database `seal-bl3w-db`. Bấm **Apply**.
5. Ở bước hỏi giá trị biến `FRONTEND_URL` (đánh dấu `sync: false` nên Render sẽ hỏi tay) — điền tạm URL Vercel có được ở Phần 1 bước 6 (vd `https://su26-swp-bl3w-fe.vercel.app`).
6. Đợi Render build Docker image + khởi tạo Postgres (~3-5 phút lần đầu). Xong sẽ có URL dạng `https://seal-bl3w-backend.onrender.com`.
7. Kiểm tra: mở `https://seal-bl3w-backend.onrender.com/health` → phải thấy `{"status":"ok"}`. Mở `/swagger` để xem API docs.
8. Từ nay, mỗi lần push code lên nhánh `main` của repo BE, Render tự động build lại Docker image + deploy — không cần Jenkins/Portainer nữa.

## Phần 3 — Nối lại 2 chiều FE ⇄ BE

1. Quay lại Vercel → project FE → **Settings → Environment Variables** → sửa `NEXT_PUBLIC_API_URL` thành URL Render thật (vd `https://seal-bl3w-backend.onrender.com/api`).
2. Vào **Deployments** → bấm **Redeploy** trên bản mới nhất để áp dụng biến môi trường mới.

## Lưu ý về gói miễn phí

- **Render Free**: web service tự "ngủ" sau ~15 phút không có request, lần gọi tiếp theo mất khoảng 30-60 giây để "thức dậy" — bình thường cho demo/đồ án, không phù hợp cho production thật. Database free có hạn 90 ngày, hết hạn cần tạo lại hoặc nâng cấp gói trả phí nhỏ (~7 USD/tháng).
- **Vercel Free**: đủ dùng cho cả năm học, không giới hạn thời gian như Render.
- Không cần thẻ tín dụng để bắt đầu với gói free ở cả 2 bên.

## Khi nào cần EF migration chạy trên Render

Database Render tạo ra là **rỗng hoàn toàn** — cần áp schema. Cách đơn giản nhất: từ máy local, trỏ connection string tới Render Postgres (lấy trong Render dashboard → database → **Connect** → External Connection String) rồi chạy:
```bash
dotnet ef database update --project backend/SEAL.Infrastructure --startup-project backend/SEAL_Backend
```
(nhớ đặt `Database__*` env var trỏ tới Render trước khi chạy lệnh trên).
