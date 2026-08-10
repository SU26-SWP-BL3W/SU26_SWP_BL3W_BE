# Design Document — SEAL Hackathon Scoring System

## Tổng quan

Giao diện tối (dark theme) mang phong cách kỹ thuật, sử dụng màu xanh lá `#3ddc84` làm accent chính và teal `#0D9488` làm màu phụ. Không bo viền (border-radius: 0) trên các button và input để tạo cảm giác sắc bén, chuyên nghiệp.

---

## Màu sắc

### Nền & bề mặt

| Tên | Giá trị | Dùng cho |
|-----|---------|---------|
| `--bg` | `#302F2E` | Nền toàn trang |
| `--sidebar` | `#252423` | Sidebar, Topbar |
| `--card` | `#1E1D1C` | Card, modal |
| `--card2` | `#2A2928` | Card hover |
| `--border` | `#3D3C3A` | Viền toàn cục |

### Màu chữ

| Tên | Giá trị | Dùng cho |
|-----|---------|---------|
| `--text` | `#F0EDE8` | Chữ chính |
| `--muted` | `#A09890` | Chữ phụ, placeholder |
| `--faint` | `#6B6560` | Chữ mờ, version tag |

### Màu nhấn (Accent)

| Tên | Giá trị | Dùng cho |
|-----|---------|---------|
| `--accent` | `#3ddc84` | Accent chính (xanh lá) |
| `--accent2` | `#2ab368` | Gradient, hover accent |
| `--warn` | `#0D9488` | Teal — badge cảnh báo, màu phụ |
| `--blue` | `#60a5fa` | Blue — badge thông tin |
| `--purple` | `#c084fc` | Purple — badge đặc biệt |
| `--danger` | `#ff4d6d` | Đỏ — xóa, từ chối |

### Màu logo / tiêu đề

| Phần | Giá trị |
|------|---------|
| **SEAL** | `#7DD3FC` (light blue) |
| **HACKATHON** | `#0D9488` (teal) |

### Màu tiêu chí (COLORS array)
```
#3ddc84  #60a5fa  #0D9488  #c084fc  #14b8a6  #34d399
```

### Màu xếp hạng

| Hạng | Màu |
|------|-----|
| 1 (Vàng) | `#ffd700` |
| 2 (Bạc) | `#c0c8d0` |
| 3 (Đồng) | `#cd7f32` |
| Còn lại | `#7da88a` |

### Card theo loại đăng ký

| Loại | Nền | Viền |
|------|-----|------|
| FPT | `--fpt-bg: #1a2a1e` | `--fpt-border: #2d5a3d` |
| Bên ngoài | `--ext-bg: #1a2827` | `--ext-border: #1e3a38` |

---

## Typography

| Font | Dùng cho |
|------|---------|
| **Orbitron** | Logo, tiêu đề trang (Topbar), headings `h1–h6` |
| **Inter** | Body text, input, button, mọi nơi còn lại |

### Cỡ chữ thường dùng

| Cỡ | Dùng cho |
|----|---------|
| `28px` | Logo Login (`h1`) |
| `14px` | Tiêu đề Topbar |
| `13px` | Button, input, body chính |
| `12px` | Nav item sidebar |
| `11px` | Badge, sub-label Topbar |
| `10–11px` | Nhãn tracking-widest (cột bảng, label nhỏ) |
| `9px` | Version tag sidebar |

---

## Layout

```
┌──────────────────────────────────────────────────────┐
│  Topbar (height: 56px, bg: #252423)                  │
├──────────┬───────────────────────────────────────────┤
│ Sidebar  │                                           │
│ (230px)  │   Content area (padding: 28px 32px)       │
│          │                                           │
│ bg:      │                                           │
│ #252423  │                                           │
└──────────┴───────────────────────────────────────────┘
```

### Sidebar
- **Rộng:** 230px, cố định chiều cao `min-h-screen`
- **Logo:** Orbitron 11px, SEAL (#7DD3FC) + HACKATHON (#0D9488)
- **Nav item active:** nền `rgba(61,220,132,.1)`, chữ `#3ddc84`, border-left `3px solid #3ddc84`
- **Nav item hover:** nền `rgba(61,220,132,.08)`, chữ `#3ddc84`
- **Nav item inactive:** chữ `#a09890`, border-left 3px transparent

### Topbar
- **Cao:** 56px
- **Bên trái:** Tên trang (Orbitron 14px) + subtitle (Inter 11px, `#a09890`)
- **Bên phải:** Badge vai trò × 3 + badge "VI" + avatar initials + nút Đăng xuất

---

## Components

### Button

| Class | Kiểu | Màu nền | Chữ |
|-------|------|---------|-----|
| `.btn-primary` | Filled | `#3ddc84` | `#000` (đen) |
| `.btn-ghost` | Outline | transparent | `#A09890` |
| `.btn-danger` | Filled nhạt | `rgba(255,77,109,.15)` | `#ff4d6d` |
| `.btn-hover` | Wrapper | — | — |

Tất cả button: `border-radius: 0`, `font-size: 13px`, `padding: 11px 24px`.  
Hover: `opacity: .85`, `transform: scale(.98)`.

### Badge

| Class | Màu chữ | Màu nền |
|-------|---------|---------|
| `.badge-accent` | `#3ddc84` | `rgba(61,220,132,.15)` |
| `.badge-warn` | `#0D9488` | `rgba(13,148,136,.15)` |
| `.badge-blue` | `#60a5fa` | `rgba(96,165,250,.12)` |
| `.badge-danger` | `#ff4d6d` | `rgba(255,77,109,.12)` |
| `.badge-purple` | `#c084fc` | `rgba(192,132,252,.12)` |

Tất cả badge: `border-radius: 0`, `padding: 3px 10px`, `font-size: 11px`, `font-weight: 700`.

### Input / Textarea / Select (`.input-field`)
- Nền: `rgba(0,0,0,.25)`
- Viền: `1px solid #3D3C3A` → khi focus: `#3ddc84`
- **Border-radius: 0**
- Padding: `11px 14px`, Font: Inter 13px

### Card
- Nền: `#131f16` (panel xanh đậm) hoặc `#1E1D1C` (tối)
- Viền: `1px solid #1e3022`
- `.card-hover`: hover đổi viền thành `#3ddc84`, nền thành `#2A2928`

### Modal
- `.modal-overlay`: `position: fixed; inset: 0; background: rgba(0,0,0,.82); z-index: 50`
- `.modal-box`: nền `#1E1D1C`, viền `1.5px solid #3D3C3A`, **border-radius: 0**, padding `36px`
- Animation: `modalIn` — scale từ 0.96 → 1 trong 0.22s

---

## Animation

| Class | Keyframe | Thời gian |
|-------|---------|-----------|
| `.animate-fadeUp` | `fadeUp` — translateY(16px→0) + opacity | 0.4s ease |
| `.animate-modalIn` | `modalIn` — scale(0.96→1) + opacity | 0.22s ease |
| `.animate-slideDown` | `slideDown` — translateY(-10px→0) + opacity | 0.3s ease |
| `.animate-pulseGlow` | `pulseGlow` — box-shadow pulse | 2s infinite |

Danh sách item: dùng `animationDelay` tăng dần theo index (`i * 0.06s`) để tạo hiệu ứng cascade.

---

## Màn hình Login

- Nền toàn trang: `#302F2E`
- 3 card vai trò nằm ngang, mỗi card `minWidth: 210px`, padding `28px`
- Tên vai trò: Orbitron 13px, màu `#0D9488`
- Mô tả: Inter 11px, màu `#a09890`
- Không bo viền card, viền `1.5px solid #3D3C3A`
- Nút "Đăng ký": chữ gạch chân, màu `#a09890`, transparent

---

## Scrollbar tùy chỉnh

```css
::-webkit-scrollbar       { width: 4px }
::-webkit-scrollbar-track { background: transparent }
::-webkit-scrollbar-thumb { background: #3D3C3A; border-radius: 2px }
```

---

## Quy tắc thiết kế

1. **Không bo viền** — tất cả button, input, badge, card dùng `border-radius: 0`
2. **Không dùng icon/emoji** — toàn bộ UI dùng text thuần
3. **Màu cam bị loại bỏ** — thay hoàn toàn bằng teal `#0D9488`
4. **Accent duy nhất** — `#3ddc84` (xanh lá) cho interactive state, active, success
5. **Teal `#0D9488`** — badge warning, màu phụ, label cần chú ý
6. **Đỏ `#ff4d6d`** — chỉ dùng cho hành động nguy hiểm (xóa, từ chối)
