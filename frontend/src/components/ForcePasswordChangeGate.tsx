"use client";

import { useEffect, useRef, useState, type FormEvent } from "react";
import { useMustChangePassword, useChangePassword, useLogout } from "@/hooks/useAuth";
import { useNotify } from "@/components/NotificationProvider";

/**
 * Cổng chặn toàn màn hình cho TÀI KHOẢN TẠM (được mời vào đội).
 * Khi đăng nhập trả về mustChangePassword=true, cổng này phủ kín màn hình và
 * bắt buộc người dùng đổi mật khẩu trước khi làm bất cứ việc gì khác.
 * Đổi xong -> BE gỡ cờ IsTemporary, cờ tắt -> cổng biến mất.
 */
export function ForcePasswordChangeGate() {
  const mustChange = useMustChangePassword();
  const changePassword = useChangePassword();
  const logout = useLogout();
  const notify = useNotify();

  const [oldPassword, setOldPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [confirm, setConfirm] = useState("");
  const dialogRef = useRef<HTMLDivElement>(null);

  // Chỉ hiện khi thực sự đang đăng nhập (có token) — tránh overlay đè trang /auth
  // nếu cờ còn sót lại sau khi đăng xuất / hết phiên.
  const hasToken = typeof window !== "undefined" && !!localStorage.getItem("accessToken");
  const isOpen = mustChange && hasToken;

  // Bẫy focus trong hộp thoại + đưa con trỏ vào ô đầu tiên khi mở, để người dùng
  // bàn phím / trình đọc màn hình không Tab ra các thành phần ở nền (đúng aria-modal).
  useEffect(() => {
    if (!isOpen) return;
    const root = dialogRef.current;
    if (!root) return;
    const focusables = () =>
      Array.from(
        root.querySelectorAll<HTMLElement>('input, button, [href], [tabindex]:not([tabindex="-1"])'),
      ).filter((el) => !el.hasAttribute("disabled"));
    focusables()[0]?.focus();
    const onKeyDown = (e: KeyboardEvent) => {
      if (e.key !== "Tab") return;
      const els = focusables();
      if (els.length === 0) return;
      const first = els[0];
      const last = els[els.length - 1];
      if (e.shiftKey && document.activeElement === first) {
        e.preventDefault();
        last.focus();
      } else if (!e.shiftKey && document.activeElement === last) {
        e.preventDefault();
        first.focus();
      }
    };
    root.addEventListener("keydown", onKeyDown);
    return () => root.removeEventListener("keydown", onKeyDown);
  }, [isOpen]);

  if (!isOpen) return null;

  function handleSubmit(e: FormEvent) {
    e.preventDefault();
    if (!oldPassword) {
      notify.error("Vui lòng nhập mật khẩu tạm bạn vừa dùng để đăng nhập.");
      return;
    }
    if (newPassword.length < 6) {
      notify.error("Mật khẩu mới phải có ít nhất 6 ký tự.");
      return;
    }
    if (newPassword !== confirm) {
      notify.error("Mật khẩu nhập lại không khớp.");
      return;
    }
    if (newPassword === oldPassword) {
      notify.error("Mật khẩu mới phải khác mật khẩu tạm.");
      return;
    }
    changePassword.mutate({ oldPassword, newPassword, confirmNewPassword: confirm });
  }

  const labelStyle = {
    fontWeight: 700,
    letterSpacing: "0.08em",
    textTransform: "uppercase",
    color: "var(--color-mute)",
  } as const;

  return (
    <div
      ref={dialogRef}
      role="dialog"
      aria-modal="true"
      style={{
        position: "fixed",
        inset: 0,
        zIndex: 9999,
        background: "rgba(0,0,0,0.55)",
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        padding: "var(--space-xl)",
      }}
    >
      <div className="card flex flex-col gap-4" style={{ padding: "var(--space-xl)", width: "100%", maxWidth: "26rem" }}>
        <h1 className="t-heading-md m-0">Bắt buộc đổi mật khẩu</h1>
        <p className="t-body-sm m-0 text-mute">
          Đây là tài khoản tạm được tạo từ lời mời tham gia đội. Vui lòng đặt mật khẩu mới để tiếp tục —
          sau khi đổi, tài khoản của bạn sẽ trở thành tài khoản chính thức.
        </p>

        <form onSubmit={handleSubmit} className="flex flex-col gap-4">
          <div className="flex flex-col gap-1">
            <label className="t-caption-xs" style={labelStyle}>Mật khẩu tạm (vừa dùng để đăng nhập)</label>
            <input
              className="text-input" type="password" required
              value={oldPassword} onChange={(e) => setOldPassword(e.target.value)}
              placeholder="Mật khẩu tạm trong email" autoComplete="current-password" style={{ height: 44 }}
            />
          </div>
          <div className="flex flex-col gap-1">
            <label className="t-caption-xs" style={labelStyle}>Mật khẩu mới</label>
            <input
              className="text-input" type="password" required minLength={6}
              value={newPassword} onChange={(e) => setNewPassword(e.target.value)}
              placeholder="Ít nhất 6 ký tự" autoComplete="new-password" style={{ height: 44 }}
            />
          </div>
          <div className="flex flex-col gap-1">
            <label className="t-caption-xs" style={labelStyle}>Nhập lại mật khẩu mới</label>
            <input
              className="text-input" type="password" required
              value={confirm} onChange={(e) => setConfirm(e.target.value)}
              placeholder="••••••••" autoComplete="new-password" style={{ height: 44 }}
            />
          </div>
          <button type="submit" className="btn btn-primary" disabled={changePassword.isPending}>
            {changePassword.isPending ? "Đang đổi…" : "Đổi mật khẩu"}
          </button>
        </form>

        {/* Lối thoát: máy dùng chung / vào nhầm tài khoản -> đăng xuất về trang đăng nhập. */}
        <button
          type="button"
          onClick={() =>
            logout.mutate(undefined, {
              // Điều hướng mềm không khiến cổng (ở layout) re-render -> reload cứng
              // để trang tải lại sạch (không token/cờ) và overlay biến mất chắc chắn.
              onSettled: () => {
                if (typeof window !== "undefined") window.location.href = "/auth";
              },
            })
          }
          disabled={logout.isPending}
          className="t-body-sm"
          style={{
            background: "none", border: "none", cursor: "pointer", padding: 0,
            color: "var(--color-mute)", textDecoration: "underline", alignSelf: "center",
          }}
        >
          Không phải bạn? Đăng xuất
        </button>
      </div>
    </div>
  );
}
