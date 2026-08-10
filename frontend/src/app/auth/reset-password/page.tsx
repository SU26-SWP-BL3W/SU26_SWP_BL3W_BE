"use client";

import { Suspense, useState, type FormEvent } from "react";
import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { authApi } from "@/services/api";
import { useNotify } from "@/components/NotificationProvider";
import { getErrorMessage } from "@/lib/apiError";

function ResetPasswordInner() {
  const params = useSearchParams();
  const token = params.get("token") ?? "";
  const router = useRouter();
  const notify = useNotify();

  const [password, setPassword] = useState("");
  const [confirm, setConfirm] = useState("");
  const [loading, setLoading] = useState(false);
  const [done, setDone] = useState(false);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    if (password.length < 6) {
      notify.error("Mật khẩu mới phải có ít nhất 6 ký tự.");
      return;
    }
    if (password !== confirm) {
      notify.error("Mật khẩu nhập lại không khớp.");
      return;
    }
    setLoading(true);
    try {
      await authApi.resetPassword(token, password);
      setDone(true);
      notify.success("Đặt lại mật khẩu thành công. Vui lòng đăng nhập lại.");
      setTimeout(() => router.push("/auth"), 1800);
    } catch (err) {
      notify.error(getErrorMessage(err, "Token không hợp lệ hoặc đã hết hạn. Hãy yêu cầu lại."));
    } finally {
      setLoading(false);
    }
  }

  const cardStyle = { padding: "var(--space-xl)", width: "100%", maxWidth: "26rem" } as const;

  if (!token) {
    return (
      <div className="card flex flex-col gap-4" style={cardStyle}>
        <h1 className="t-heading-md m-0">Liên kết không hợp lệ</h1>
        <p className="t-body-sm m-0 text-mute">
          Liên kết đặt lại mật khẩu thiếu mã token hoặc đã bị thay đổi. Vui lòng yêu cầu gửi lại.
        </p>
        <Link href="/auth/forgot-password" className="btn btn-primary w-fit">Yêu cầu lại</Link>
      </div>
    );
  }

  return (
    <div className="card flex flex-col gap-4" style={cardStyle}>
      <h1 className="t-heading-md m-0">Đặt lại mật khẩu</h1>

      {done ? (
        <p className="t-body-sm m-0 text-mute">
          ✓ Mật khẩu đã được đặt lại. Đang chuyển tới trang đăng nhập…
        </p>
      ) : (
        <form onSubmit={handleSubmit} className="flex flex-col gap-4">
          <div className="flex flex-col gap-1">
            <label className="t-caption-xs" style={{ fontWeight: 700, letterSpacing: "0.08em", textTransform: "uppercase", color: "var(--color-mute)" }}>
              Mật khẩu mới
            </label>
            <input
              className="text-input" type="password" required minLength={6}
              value={password} onChange={(e) => setPassword(e.target.value)}
              placeholder="Ít nhất 6 ký tự" autoComplete="new-password" style={{ height: 44 }}
            />
          </div>
          <div className="flex flex-col gap-1">
            <label className="t-caption-xs" style={{ fontWeight: 700, letterSpacing: "0.08em", textTransform: "uppercase", color: "var(--color-mute)" }}>
              Nhập lại mật khẩu
            </label>
            <input
              className="text-input" type="password" required
              value={confirm} onChange={(e) => setConfirm(e.target.value)}
              placeholder="••••••••" autoComplete="new-password" style={{ height: 44 }}
            />
          </div>
          <button type="submit" className="btn btn-primary" disabled={loading}>
            {loading ? "Đang đặt lại…" : "Đặt lại mật khẩu"}
          </button>
        </form>
      )}

      <Link href="/auth" className="t-body-sm inline-flex items-center gap-1" style={{ color: "var(--color-primary)", textDecoration: "none" }}>
        Quay lại đăng nhập
      </Link>
    </div>
  );
}

export default function ResetPasswordPage() {
  return (
    <main style={{ minHeight: "100dvh", display: "flex", alignItems: "center", justifyContent: "center", padding: "var(--space-xl)" }}>
      <Suspense fallback={<div className="t-body-md text-mute">Đang tải…</div>}>
        <ResetPasswordInner />
      </Suspense>
    </main>
  );
}
