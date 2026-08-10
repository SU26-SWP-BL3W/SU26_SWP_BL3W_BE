"use client";

import { Suspense, useState, type FormEvent } from "react";
import Link from "next/link";
import { useSearchParams } from "next/navigation";
import { useQuery, useMutation } from "@tanstack/react-query";
import type { AxiosError } from "axios";
import { authApi } from "@/services/api";
import type { ApiError } from "@/services/api";
import { useNotify } from "@/components/NotificationProvider";
import { getErrorMessage } from "@/lib/apiError";

// ─── Status view ──────────────────────────────────────────────────────────────

type Status = "loading" | "success" | "invalid" | "error" | "missing";

interface View {
  icon: "spinner" | "check" | "cross";
  title: string;
  message: string;
  /** primary action button — omitted while loading / when a resend form shows */
  action?: { label: string; href: string };
  /** optional secondary link under the button */
  secondary?: { label: string; href: string };
  /** show the "resend verification email" form (expired / invalid link) */
  canResend?: boolean;
}

function viewFor(status: Status): View {
  switch (status) {
    case "loading":
      return {
        icon: "spinner",
        title: "Đang xác thực email…",
        message: "Vui lòng chờ trong giây lát.",
      };
    case "success":
      return {
        icon: "check",
        title: "Xác thực thành công!",
        message: "Email của bạn đã được xác nhận. Bạn có thể đăng nhập ngay bây giờ.",
        action: { label: "Đăng nhập", href: "/auth" },
      };
    case "invalid":
      return {
        icon: "cross",
        title: "Liên kết không hợp lệ",
        message:
          "Liên kết xác thực không đúng hoặc đã hết hạn. Nhập email để nhận lại liên kết xác thực mới.",
        canResend: true,
        secondary: { label: "Quay lại đăng nhập", href: "/auth" },
      };
    case "missing":
      return {
        icon: "cross",
        title: "Thiếu mã xác thực",
        message:
          "Không tìm thấy mã xác thực trong liên kết. Vui lòng mở lại liên kết từ email của bạn.",
        action: { label: "Quay lại đăng nhập", href: "/auth" },
      };
    case "error":
    default:
      return {
        icon: "cross",
        title: "Không thể xác thực",
        message:
          "Đã xảy ra lỗi khi xác thực email. Vui lòng thử lại sau hoặc liên hệ ban tổ chức.",
        action: { label: "Quay lại đăng nhập", href: "/auth" },
      };
  }
}

// ─── Icons ────────────────────────────────────────────────────────────────────

function StatusIcon({ icon }: { icon: View["icon"] }) {
  if (icon === "spinner") {
    return <div className="ve-spinner" />;
  }
  return (
    <div className={`ve-badge ve-badge--${icon === "check" ? "ok" : "bad"}`}>
      {icon === "check" ? (
        <svg width="34" height="34" viewBox="0 0 24 24" fill="none" aria-hidden>
          <path
            d="M4 12.5l5 5L20 6.5"
            stroke="currentColor"
            strokeWidth="2.4"
            strokeLinecap="round"
            strokeLinejoin="round"
          />
        </svg>
      ) : (
        <svg width="30" height="30" viewBox="0 0 24 24" fill="none" aria-hidden>
          <path
            d="M6 6l12 12M18 6L6 18"
            stroke="currentColor"
            strokeWidth="2.4"
            strokeLinecap="round"
          />
        </svg>
      )}
    </div>
  );
}

// ─── Resend-verification form (shown on expired / invalid link) ───────────────

function ResendForm() {
  const [email, setEmail] = useState("");
  const notify = useNotify();

  const resend = useMutation({
    mutationFn: (email: string) => authApi.resendVerification(email),
    onSuccess: () =>
      notify.success("Đã gửi lại email xác thực. Vui lòng kiểm tra hộp thư của bạn."),
    onError: (e) => notify.error(getErrorMessage(e, "Không gửi được email xác thực.")),
  });

  const serverError = (resend.error as AxiosError<ApiError>)?.response?.data
    ?.message;

  function handleSubmit(e: FormEvent) {
    e.preventDefault();
    if (!email.trim()) return;
    resend.mutate(email.trim());
  }

  if (resend.isSuccess) {
    return (
      <p className="ve-resend-done">
        Đã gửi lại email xác thực. Vui lòng kiểm tra hộp thư của bạn.
      </p>
    );
  }

  return (
    <form className="ve-resend" onSubmit={handleSubmit} noValidate>
      <input
        className="text-input ve-resend-input"
        type="email"
        value={email}
        onChange={(e) => setEmail(e.target.value)}
        placeholder="ten@email.com"
        autoComplete="email"
      />
      {serverError && <p className="ve-resend-error">{serverError}</p>}
      <button
        type="submit"
        className="ve-button"
        disabled={resend.isPending || !email.trim()}
      >
        {resend.isPending ? "Đang gửi…" : "Gửi lại email xác thực"}
      </button>
    </form>
  );
}

// ─── Inner (reads the URL token) ──────────────────────────────────────────────

/** Map a query error to a status — 4xx means the token is invalid/expired,
 *  anything else is a server-side problem. */
function statusFromError(err: unknown): Status {
  const code = (err as AxiosError)?.response?.status;
  return code !== undefined && code >= 400 && code < 500 ? "invalid" : "error";
}

function VerifyEmailInner() {
  const token = useSearchParams()?.get("token") ?? null;

  const query = useQuery({
    queryKey: ["auth", "verify-email", token],
    queryFn: () => authApi.verifyEmail(token as string),
    enabled: !!token,
    retry: false,
    staleTime: Infinity,
  });

  const status: Status = !token
    ? "missing"
    : query.isLoading
      ? "loading"
      : query.isError
        ? statusFromError(query.error)
        : query.data === true
          ? "success"
          : "invalid";

  const view = viewFor(status);

  return (
    <div className="ve-card">
      <div className="ve-cs-tl" />
      <div className="ve-cs-br" />

      <span className="ve-tag">HACKATHON · FPT UNIVERSITY</span>

      <StatusIcon icon={view.icon} />

      <h1 className="ve-title">{view.title}</h1>
      <p className="ve-message">{view.message}</p>

      {view.canResend && <ResendForm />}

      {view.action && (
        <Link href={view.action.href} className="ve-button">
          {view.action.label}
        </Link>
      )}

      {view.secondary && (
        <Link href={view.secondary.href} className="ve-link">
          {view.secondary.label}
        </Link>
      )}
    </div>
  );
}

// ─── Page ─────────────────────────────────────────────────────────────────────

export default function VerifyEmailPage() {
  return (
    <>
      <style>{`
        .ve-root {
          position: fixed;
          inset: 0;
          display: flex;
          align-items: center;
          justify-content: center;
          padding: 24px;
          background: var(--color-surface-soft);
          font-family: var(--font-sans);
          background-image: radial-gradient(
            circle,
            rgba(118, 185, 0, 0.08) 1px,
            transparent 1px
          );
          background-size: 28px 28px;
        }

        .ve-card {
          position: relative;
          width: 100%;
          max-width: 440px;
          background: var(--color-canvas);
          padding: 48px 40px 40px;
          display: flex;
          flex-direction: column;
          align-items: center;
          text-align: center;
          gap: 16px;
          border: 1px solid var(--color-hairline-strong);
        }

        .ve-cs-tl,
        .ve-cs-br {
          position: absolute;
          width: 14px;
          height: 14px;
          background: var(--color-primary);
        }
        .ve-cs-tl { top: 0; left: 0; }
        .ve-cs-br { bottom: 0; right: 0; }

        .ve-tag {
          font-size: 10px;
          font-weight: 700;
          letter-spacing: 0.12em;
          text-transform: uppercase;
          color: var(--color-primary);
        }

        .ve-title {
          font-size: 24px;
          font-weight: 700;
          line-height: 1.2;
          color: var(--color-ink);
          margin: 0;
        }

        .ve-message {
          font-size: 14px;
          line-height: 1.65;
          color: var(--color-mute);
          margin: 0;
          max-width: 320px;
        }

        .ve-button {
          margin-top: 8px;
          height: 44px;
          padding: 0 28px;
          display: inline-flex;
          align-items: center;
          justify-content: center;
          background: var(--color-primary);
          color: #fff;
          border: none;
          border-radius: 2px;
          font-size: 14px;
          font-weight: 700;
          letter-spacing: 0.06em;
          text-transform: uppercase;
          text-decoration: none;
          cursor: pointer;
          transition: background 80ms linear;
        }
        .ve-button:hover:not(:disabled) {
          background: var(--color-primary-dark);
        }
        .ve-button:disabled {
          background: var(--color-surface-soft);
          color: var(--color-ash);
          cursor: not-allowed;
        }

        /* ── resend form ───────────────────────────────────────────────────── */
        .ve-resend {
          width: 100%;
          margin-top: 8px;
          display: flex;
          flex-direction: column;
          gap: 10px;
        }
        .ve-resend-input {
          height: 44px;
          width: 100%;
        }
        .ve-resend .ve-button {
          margin-top: 0;
          width: 100%;
        }
        .ve-resend-error {
          font-size: 13px;
          color: var(--color-error);
          margin: 0;
          text-align: left;
        }
        .ve-resend-done {
          width: 100%;
          margin: 8px 0 0;
          padding: 12px 14px;
          font-size: 14px;
          line-height: 1.55;
          color: var(--color-success-deep, #3f8500);
          background: rgba(118, 185, 0, 0.08);
          border-left: 2px solid var(--color-primary);
          text-align: left;
        }

        /* ── secondary link ────────────────────────────────────────────────── */
        .ve-link {
          margin-top: 4px;
          font-size: 13px;
          font-weight: 700;
          color: var(--color-primary);
          text-decoration: none;
        }
        .ve-link:hover {
          text-decoration: underline;
        }

        /* ── status badges ─────────────────────────────────────────────────── */
        .ve-badge {
          width: 64px;
          height: 64px;
          border-radius: 50%;
          display: flex;
          align-items: center;
          justify-content: center;
        }
        .ve-badge--ok {
          background: rgba(118, 185, 0, 0.12);
          color: var(--color-primary);
        }
        .ve-badge--bad {
          background: rgba(229, 32, 32, 0.08);
          color: var(--color-error);
        }

        .ve-spinner {
          width: 40px;
          height: 40px;
          border-radius: 50%;
          border: 3px solid var(--color-hairline);
          border-top-color: var(--color-primary);
          animation: ve-spin 0.6s linear infinite;
        }
        @keyframes ve-spin {
          to { transform: rotate(360deg); }
        }
      `}</style>

      <div className="ve-root">
        <Suspense
          fallback={
            <div className="ve-card">
              <div className="ve-spinner" />
            </div>
          }
        >
          <VerifyEmailInner />
        </Suspense>
      </div>
    </>
  );
}
