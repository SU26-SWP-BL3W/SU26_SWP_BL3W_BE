"use client";

import { useState, type FormEvent } from "react";
import Link from "next/link";
import { authApi } from "@/services/api";
import { useNotify } from "@/components/NotificationProvider";
import { getErrorMessage } from "@/lib/apiError";

export default function ForgotPasswordPage() {
  const [email, setEmail] = useState("");
  const [sent, setSent] = useState(false);
  const [loading, setLoading] = useState(false);
  const notify = useNotify();

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    if (!email.trim()) return;
    setLoading(true);
    try {
      await authApi.forgotPassword(email.trim());
      // BE luôn trả thông báo chung (chống dò email) -> FE luôn hiện màn xác nhận.
      setSent(true);
    } catch (err) {
      notify.error(getErrorMessage(err, "Gửi yêu cầu thất bại. Vui lòng thử lại."));
    } finally {
      setLoading(false);
    }
  }

  return (
    <main style={{ minHeight: "100dvh", display: "flex", alignItems: "center", justifyContent: "center", padding: "var(--space-xl)" }}>
      <div className="card flex flex-col gap-4" style={{ padding: "var(--space-xl)", width: "100%", maxWidth: "26rem" }}>
        <h1 className="t-heading-md m-0">Quên mật khẩu</h1>

        {sent ? (
          <>
            <p className="t-body-sm m-0 text-mute">
              Nếu email hợp lệ, chúng tôi đã gửi liên kết đặt lại mật khẩu tới hộp thư của bạn.
              Vui lòng kiểm tra email (kể cả mục spam). Liên kết hết hạn sau 1 giờ.
            </p>
            <Link href="/auth" className="btn btn-primary w-fit">Về đăng nhập</Link>
          </>
        ) : (
          <>
            <p className="t-body-sm m-0 text-mute">
              Nhập email tài khoản của bạn. Chúng tôi sẽ gửi liên kết để đặt lại mật khẩu.
            </p>
            <form onSubmit={handleSubmit} className="flex flex-col gap-4">
              <div className="flex flex-col gap-1">
                <label className="t-caption-xs" style={{ fontWeight: 700, letterSpacing: "0.08em", textTransform: "uppercase", color: "var(--color-mute)" }}>
                  Email
                </label>
                <input
                  className="text-input"
                  type="email"
                  required
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  placeholder="ten@email.com"
                  autoComplete="email"
                  style={{ height: 44 }}
                />
              </div>
              <button type="submit" className="btn btn-primary" disabled={loading}>
                {loading ? "Đang gửi…" : "Gửi liên kết đặt lại"}
              </button>
            </form>
          </>
        )}

        <Link href="/auth" className="t-body-sm inline-flex items-center gap-1" style={{ color: "var(--color-primary)", textDecoration: "none" }}>
          Quay lại đăng nhập
        </Link>
      </div>
    </main>
  );
}
