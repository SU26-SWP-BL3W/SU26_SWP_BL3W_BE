"use client";

import {
  useState,
  useRef,
  useEffect,
  type ChangeEvent,
  type FormEvent,
} from "react";
import { useLogin, useRegister, useGoogleLogin } from "@/hooks/useAuth";
import { getErrorMessage } from "@/lib/apiError";
import Link from "next/link";
import { ArrowLeft } from "lucide-react";
import { GoogleOAuthProvider, GoogleLogin } from "@react-oauth/google";

const GOOGLE_CLIENT_ID = process.env.NEXT_PUBLIC_GOOGLE_CLIENT_ID ?? "";

// ─── Types ────────────────────────────────────────────────────────────────────

type Mode = "login" | "register";

interface LoginForm {
  email: string;
  password: string;
}

interface RegisterForm {
  fullName: string;
  email: string;
  password: string;
  confirmPassword?: string;
}

// ─── Tiny field component ─────────────────────────────────────────────────────

function Field({
  label,
  type = "text",
  value,
  onChange,
  placeholder,
  autoComplete,
}: {
  label: string;
  type?: string;
  value: string;
  onChange: (e: ChangeEvent<HTMLInputElement>) => void;
  placeholder?: string;
  autoComplete?: string;
}) {
  return (
    <div style={{ display: "flex", flexDirection: "column", gap: 6 }}>
      <label
        style={{
          fontSize: 11,
          fontWeight: 700,
          letterSpacing: "0.08em",
          textTransform: "uppercase",
          color: "var(--color-mute)",
        }}
      >
        {label}
      </label>
      <input
        className="text-input"
        type={type}
        value={value}
        onChange={onChange}
        placeholder={placeholder}
        autoComplete={autoComplete}
        style={{ height: 44 }}
      />
    </div>
  );
}

// ─── Page ─────────────────────────────────────────────────────────────────────

export default function AuthPage() {
  const [mode, setMode] = useState<Mode>("login");

  useEffect(() => {
    const timer = window.setTimeout(() => {
      const params = new URLSearchParams(window.location.search);
      if (params.get("mode") === "register") {
        setMode("register");
      }
    }, 0);
    return () => window.clearTimeout(timer);
  }, []);

  const [loginForm, setLoginForm] = useState<LoginForm>({
    email: "",
    password: "",
  });
  const [registerForm, setRegisterForm] = useState<RegisterForm>({
    fullName: "",
    email: "",
    password: "",
    confirmPassword: "",
  });
  const [clientError, setClientError] = useState("");
  const [successMessage, setSuccessMessage] = useState("");
  const [isSwitching, setIsSwitching] = useState(false);
  const switchTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  const loginMutation = useLogin();
  const googleMutation = useGoogleLogin();
  const registerMutation = useRegister();

  const isPending = loginMutation.isPending || registerMutation.isPending;
  const serverError = loginMutation.error
    ? getErrorMessage(loginMutation.error)
    : registerMutation.error
      ? getErrorMessage(registerMutation.error)
      : undefined;

  const isRegister = mode === "register";

  // ── sliding transform values ───────────────────────────────────────────────
  // The dark (intro) panel starts left and slides right on register mode.
  // The form panel starts right and slides left on register mode.
  // The spine (72 px) sits fixed at 50% and has z-index:20 so panels slide behind it.
  const SPINE = 72;
  const darkTransform = isRegister
    ? `translateX(calc(100% + ${SPINE}px))`
    : "translateX(0)";
  const formTransform = isRegister
    ? `translateX(calc(-100% - ${SPINE}px))`
    : "translateX(0)";
  const EASE = "transform 0.55s cubic-bezier(0.76, 0, 0.24, 1)";

  // ── handlers ──────────────────────────────────────────────────────────────

  function switchMode() {
    if (isSwitching) return;
    setClientError("");
    setSuccessMessage("");
    loginMutation.reset();
    registerMutation.reset();

    setIsSwitching(true);

    // wait one paint for the spinner to appear before swapping form content
    if (switchTimerRef.current) clearTimeout(switchTimerRef.current);
    switchTimerRef.current = setTimeout(() => {
      setMode((m) => (m === "login" ? "register" : "login"));
    }, 100);

    // hide spinner after slide animation completes (550ms) + fade buffer
    setTimeout(() => setIsSwitching(false), 600);
  }

  async function handleLogin(e: FormEvent) {
    e.preventDefault();
    setClientError("");
    setSuccessMessage("");
    loginMutation.mutate(loginForm);
  }

  async function handleRegister(e: FormEvent) {
    e.preventDefault();
    setClientError("");
    setSuccessMessage("");

    const fullName = registerForm.fullName.trim();
    if (!fullName) {
      setClientError("Vui lòng nhập họ và tên.");
      return;
    }
    const email = registerForm.email.trim();
    if (!email) {
      setClientError("Vui lòng nhập email.");
      return;
    }
    if (!registerForm.password) {
      setClientError("Vui lòng nhập mật khẩu.");
      return;
    }
    if (registerForm.password !== registerForm.confirmPassword) {
      setClientError("Mật khẩu nhập lại không khớp.");
      return;
    }

    registerMutation.mutate(
      {
        email,
        password: registerForm.password,
        fullName,
        // Đăng ký công khai luôn là tài khoản thí sinh (student). Gửi rõ cờ này để BE
        // không suy ra nhầm vai trò khi payload thiếu isStudent (tránh vô tình thành admin).
        isStudent: true,
      },
      {
        onSuccess: () => {
          // Backend returns the user but no tokens and sends a verification
          // email — prompt the user to confirm via the link before logging in.
          setSuccessMessage(
            "Đăng ký thành công. Vui lòng kiểm tra email để xác nhận tài khoản trước khi đăng nhập.",
          );
          setLoginForm({
            email,
            password: "",
          });
          setRegisterForm({
            fullName: "",
            email: "",
            password: "",
            confirmPassword: "",
          });
          setMode("login");
        },
      },
    );
  }

  // ─────────────────────────────────────────────────────────────────────────

  return (
    <>
      {/* ── scoped styles ──────────────────────────────────────────────────── */}
      <style>{`
        .auth-root {
          position: fixed;
          inset: 0;
          overflow: hidden;
          background: var(--color-surface-dark);
          font-family: var(--font-sans);
        }

        /* ── dark intro panel ─────────────────────────────────────────────── */
        .auth-dark {
          position: absolute;
          top: 0;
          left: 0;
          width: calc(50% - ${SPINE / 2}px);
          height: 100%;
          /* nền đen + ambient glow xanh (2 nguồn sáng) cho chiều sâu, hiện đại hơn nền phẳng */
          background:
            radial-gradient(115% 80% at 12% 0%, rgba(118, 185, 0, 0.14), transparent 52%),
            radial-gradient(90% 60% at 100% 100%, rgba(118, 185, 0, 0.07), transparent 55%),
            var(--color-surface-dark);
          display: flex;
          flex-direction: column;
          justify-content: center;
          padding: 64px 56px;
          z-index: 15;
          transition: ${EASE};
          will-change: transform;
          overflow: hidden;
        }

        /* subtle dot-grid background on dark panel */
        .auth-dark::before {
          content: "";
          position: absolute;
          inset: 0;
          background-image: radial-gradient(
            circle,
            rgba(118, 185, 0, 0.12) 1px,
            transparent 1px
          );
          background-size: 28px 28px;
          pointer-events: none;
        }

        /* ── corner squares ───────────────────────────────────────────────── */
        .auth-cs-tl {
          position: absolute;
          top: 0;
          left: 0;
          width: 14px;
          height: 14px;
          background: var(--color-primary);
          z-index: 1;
        }
        .auth-cs-br {
          position: absolute;
          bottom: 0;
          right: 0;
          width: 14px;
          height: 14px;
          background: var(--color-primary);
          z-index: 1;
        }

        /* ── spine ────────────────────────────────────────────────────────── */
        .auth-spine {
          position: absolute;
          top: 0;
          left: calc(50% - ${SPINE / 2}px);
          width: ${SPINE}px;
          height: 100%;
          background: var(--color-surface-elevated);
          z-index: 20;
          display: flex;
          flex-direction: column;
          align-items: center;
          justify-content: center;
          gap: 10px;
          border-left: 1px solid var(--color-hairline-strong);
          border-right: 1px solid var(--color-hairline-strong);
        }

        .auth-spine-line {
          width: 1px;
          flex: 1;
          background: var(--color-hairline-strong);
        }

        .auth-spine-btn {
          display: flex;
          flex-direction: column;
          align-items: center;
          gap: 8px;
          cursor: pointer;
          border: none;
          background: transparent;
          padding: 16px 0;
        }

        .auth-spine-arrow {
          width: 36px;
          height: 36px;
          border-radius: 2px;
          background: var(--color-primary);
          color: #fff;
          display: flex;
          align-items: center;
          justify-content: center;
          font-size: 16px;
          font-weight: 700;
          transition: background 80ms linear;
        }
        .auth-spine-btn:hover .auth-spine-arrow {
          background: var(--color-primary-dark);
        }

        .auth-spine-label {
          font-size: 10px;
          font-weight: 700;
          letter-spacing: 0.06em;
          text-transform: uppercase;
          color: var(--color-on-dark-mute);
          writing-mode: vertical-rl;
        }

        /* ── form panel ───────────────────────────────────────────────────── */
        .auth-form-panel {
          position: absolute;
          top: 0;
          right: 0;
          width: calc(50% - ${SPINE / 2}px);
          height: 100%;
          background: var(--color-canvas);
          display: flex;
          justify-content: center;
          z-index: 10;
          transition: ${EASE};
          will-change: transform;
          /* allow scrolling when the (taller) register form overflows … */
          overflow-y: auto;
          /* … but hide the scrollbar */
          scrollbar-width: none; /* Firefox */
          -ms-overflow-style: none; /* IE/Edge */
        }
        .auth-form-panel::-webkit-scrollbar {
          display: none; /* Chrome/Safari */
        }

        /* ── switching spinner overlay ────────────────────────────────────── */
        .auth-spinner-overlay {
          position: absolute;
          inset: 0;
          background: var(--color-canvas);
          display: flex;
          align-items: center;
          justify-content: center;
          z-index: 4;
          opacity: 0;
          pointer-events: none;
          transition: opacity 120ms ease;
        }
        .auth-spinner-overlay.is-active {
          opacity: 1;
          pointer-events: auto;
        }
        .auth-spinner {
          width: 26px;
          height: 26px;
          border-radius: 50%;
          border: 2px solid var(--color-hairline);
          border-top-color: var(--color-primary);
          animation: auth-spin 0.55s linear infinite;
        }
        @keyframes auth-spin {
          to { transform: rotate(360deg); }
        }

        .auth-form-inner {
          width: 100%;
          max-width: 380px;
          /* margin:auto centers vertically when it fits, and lets the panel
             scroll without clipping the top when content overflows */
          margin: auto;
          padding: 40px 32px;
          display: flex;
          flex-direction: column;
          gap: 20px;
        }

        /* ── brand on dark panel ──────────────────────────────────────────── */
        .auth-brand-tag {
          font-size: 10px;
          font-weight: 700;
          letter-spacing: 0.12em;
          text-transform: uppercase;
          color: var(--color-primary);
          margin-bottom: 4px;
        }

        .auth-brand-name {
          font-size: 34px;
          font-weight: 700;
          line-height: 1.12;
          letter-spacing: -0.02em;
          color: var(--color-on-dark);
          margin: 0 0 16px 0;
        }

        .auth-brand-desc {
          font-size: 15px;
          font-weight: 400;
          line-height: 1.65;
          color: var(--color-on-dark-mute);
          margin: 0;
          max-width: 320px;
        }

        .auth-feature-list {
          list-style: none;
          padding: 0;
          margin: 24px 0 0 0;
          display: flex;
          flex-direction: column;
          gap: 10px;
        }

        .auth-feature-item {
          display: flex;
          align-items: center;
          gap: 10px;
          font-size: 13px;
          font-weight: 400;
          color: var(--color-on-dark-mute);
        }

        .auth-feature-dot {
          width: 6px;
          height: 6px;
          background: var(--color-primary);
          border-radius: 0;
          flex-shrink: 0;
        }

        /* ── form heading ─────────────────────────────────────────────────── */
        .auth-form-heading {
          font-size: 32px;
          font-weight: 700;
          line-height: 1.2;
          letter-spacing: -0.02em;
          color: var(--color-ink);
          margin: 0;
        }

        .auth-form-sub {
          font-size: 14px;
          color: var(--color-mute);
          margin: 2px 0 0 0;
        }

        /* ── error ────────────────────────────────────────────────────────── */
        .auth-error {
          font-size: 13px;
          color: var(--color-error);
          padding: 10px 12px;
          background: rgba(229, 32, 32, 0.06);
          border-left: 2px solid var(--color-error);
          border-radius: 0;
        }

        /* ── submit button override ───────────────────────────────────────── */
        .auth-submit {
          width: 100%;
          height: 46px;
          background: var(--color-primary);
          color: #fff;
          border: none;
          border-radius: 2px;
          font-size: 14px;
          font-weight: 700;
          letter-spacing: 0.06em;
          text-transform: uppercase;
          cursor: pointer;
          /* colored shadow (tinted theo accent) + press feedback cho cảm giác vật lý */
          box-shadow: 0 2px 10px rgba(118, 185, 0, 0.25);
          transition: background 140ms ease, box-shadow 140ms ease, transform 90ms ease;
          margin-top: 4px;
        }
        .auth-submit:hover:not(:disabled) {
          background: var(--color-primary-dark);
          box-shadow: 0 4px 16px rgba(118, 185, 0, 0.34);
        }
        .auth-submit:active:not(:disabled) {
          transform: translateY(1px);
          box-shadow: 0 1px 5px rgba(118, 185, 0, 0.22);
        }
        .auth-submit:disabled {
          background: var(--color-surface-soft);
          color: var(--color-ash);
          box-shadow: none;
          cursor: not-allowed;
        }

        /* ── inputs: focus ring + autofill fix (chỉ trong form auth) ───────── */
        .auth-form-inner .text-input {
          transition: border-color 140ms ease, box-shadow 140ms ease;
        }
        .auth-form-inner .text-input:hover:not(:focus) {
          border-color: var(--color-hairline-strong);
        }
        .auth-form-inner .text-input:focus {
          box-shadow: 0 0 0 3px rgba(118, 185, 0, 0.14);
        }
        /* autofill: giữ nền trắng + chữ đen thay vì nền xanh mặc định của browser */
        .auth-form-inner .text-input:-webkit-autofill,
        .auth-form-inner .text-input:-webkit-autofill:hover,
        .auth-form-inner .text-input:-webkit-autofill:focus {
          -webkit-text-fill-color: var(--color-ink);
          -webkit-box-shadow: 0 0 0 1000px var(--color-canvas) inset;
          caret-color: var(--color-ink);
        }

        /* ── divider "hoặc" có 2 đường kẻ ─────────────────────────────────── */
        .auth-divider {
          display: flex;
          align-items: center;
          gap: 12px;
          width: 100%;
        }
        .auth-divider::before,
        .auth-divider::after {
          content: "";
          flex: 1;
          height: 1px;
          background: var(--color-hairline);
        }
        .auth-divider span {
          font-size: 11px;
          font-weight: 700;
          letter-spacing: 0.1em;
          text-transform: uppercase;
          color: var(--color-ash);
        }

        /* ── link hover ───────────────────────────────────────────────────── */
        .auth-link {
          transition: color 120ms ease;
        }
        .auth-link:hover {
          text-decoration: underline;
        }

        /* ── form content fade when mode changes ──────────────────────────── */
        .auth-form-content {
          display: flex;
          flex-direction: column;
          gap: 20px;
        }

        /* ── school select ────────────────────────────────────────────────── */
        .auth-select {
          box-sizing: border-box;
          height: 44px;
          width: 100%;
          padding: 0 36px 0 12px;
          font-size: 14px;
          font-family: inherit;
          color: var(--color-ink);
          background-color: var(--color-canvas);
          border: var(--border-hairline);
          border-radius: 2px;
          cursor: pointer;
          appearance: none;
          background-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='12' height='8' viewBox='0 0 12 8'%3E%3Cpath d='M1 1l5 5 5-5' stroke='%23999' stroke-width='1.6' fill='none' stroke-linecap='round'/%3E%3C/svg%3E");
          background-repeat: no-repeat;
          background-position: right 14px center;
        }
        .auth-select:focus {
          outline: none;
          border: var(--border-primary-2);
          padding: 0 35px 0 11px;
        }

        /* ── student-card upload ──────────────────────────────────────────── */
        .auth-upload-drop {
          display: flex;
          flex-direction: column;
          align-items: center;
          justify-content: center;
          gap: 4px;
          width: 100%;
          padding: 20px 12px;
          background: var(--color-canvas);
          border: 1px dashed var(--color-hairline-strong);
          border-radius: 2px;
          cursor: pointer;
          font-size: 13px;
          color: var(--color-mute);
          transition: border-color 80ms linear, background 80ms linear;
        }
        .auth-upload-drop:hover {
          border-color: var(--color-primary);
          background: rgba(118, 185, 0, 0.04);
        }
        .auth-upload-plus {
          font-size: 22px;
          font-weight: 700;
          color: var(--color-primary);
          line-height: 1;
        }
        .auth-upload-hint {
          font-size: 11px;
          color: var(--color-ash);
        }
        .auth-upload-preview {
          display: flex;
          flex-direction: column;
          gap: 8px;
          padding: 8px;
          border: 1px solid var(--color-hairline-strong);
          border-radius: 2px;
          background: var(--color-canvas);
        }
        .auth-upload-preview img {
          width: 100%;
          max-height: 180px;
          object-fit: contain;
          border-radius: 2px;
          background: var(--color-surface-soft);
        }
        .auth-upload-meta {
          display: flex;
          align-items: center;
          justify-content: space-between;
          gap: 12px;
        }
        .auth-upload-name {
          font-size: 12px;
          color: var(--color-mute);
          overflow: hidden;
          text-overflow: ellipsis;
          white-space: nowrap;
        }
        .auth-upload-remove {
          flex-shrink: 0;
          background: none;
          border: none;
          padding: 0;
          font-size: 12px;
          font-weight: 700;
          color: var(--color-error);
          cursor: pointer;
        }

        .hover-primary {
          transition: color 100ms linear;
        }
        .hover-primary:hover {
          color: var(--color-primary) !important;
        }

        /* ── responsive ───────────────────────────────────────────────────── */
        @media (max-width: 768px) {
          .auth-dark { display: none; }
          .auth-spine { display: none; }
          .auth-form-panel {
            width: 100% !important;
            transform: none !important;
          }
        }
      `}</style>

      <div className="auth-root">
        {/* ── Dark intro panel ───────────────────────────────────────────── */}
        <div className="auth-dark" style={{ transform: darkTransform }}>
          <div className="auth-cs-tl" />
          <div className="auth-cs-br" />

          <div style={{ position: "relative", zIndex: 1 }}>
            <p className="auth-brand-tag">SEAL-HACKATHON</p>
            <h1 className="auth-brand-name">
              HACKATHON
              <br />
              FPT UNIVERSITY
            </h1>
            <p className="auth-brand-desc">
              Nền tảng quản lý cuộc thi hackathon
            </p>

            <ul className="auth-feature-list">
              {[
                "Quản lý cuộc thi",
                "Theo dõi tiến độ theo thời gian thực",
                "Giao diện trực quan, dễ sử dụng",
                "Tối ưu trải nghiệm thi đấu",
              ].map((f) => (
                <li key={f} className="auth-feature-item">
                  <span className="auth-feature-dot" />
                  {f}
                </li>
              ))}
            </ul>
          </div>
        </div>

        {/* ── Center spine ───────────────────────────────────────────────── */}
        <div className="auth-spine">
          <div className="auth-spine-line" />

          <button
            className="auth-spine-btn"
            onClick={switchMode}
            aria-label={isRegister ? "Chuyển sang đăng nhập" : "Chuyển sang đăng ký"}
          >
            <div className="auth-spine-arrow">
              {isRegister ? "«" : "»"}
            </div>
            <span className="auth-spine-label">
              {isRegister ? "Đăng nhập" : "Đăng kí"}
            </span>
          </button>

          <div className="auth-spine-line" />
        </div>

        {/* ── Form panel ─────────────────────────────────────────────────── */}
        <div className="auth-form-panel" style={{ transform: formTransform }}>
          {/* spinner masks form content during panel slide */}
          <div className={`auth-spinner-overlay${isSwitching ? " is-active" : ""}`}>
            <div className="auth-spinner" />
          </div>

          <div className="auth-form-inner">
            {/* Back to Home */}
            <Link
              href="/"
              className="hover-primary"
              style={{
                display: "inline-flex",
                alignItems: "center",
                gap: 6,
                fontSize: 13,
                color: "var(--color-mute)",
                textDecoration: "none",
                marginBottom: 8,
                width: "fit-content",
              }}
            >
              <ArrowLeft size={16} />
              Quay lại trang chủ
            </Link>

            {/* heading */}
            <div>
              <h2 className="auth-form-heading">
                {isRegister ? "Đăng ký" : "Đăng nhập"}
              </h2>
              <p className="auth-form-sub">
                {isRegister
                  ? "Tạo tài khoản mới để bắt đầu"
                  : "Chào mừng trở lại"}
              </p>
            </div>

            {/* error */}
            {(clientError || serverError) && (
              <div className="auth-error">{clientError || serverError}</div>
            )}
            {successMessage && !clientError && !serverError && (
              <div
                className="auth-error"
                style={{
                  color: "var(--color-successDeep, #3f8500)",
                  background: "rgba(118, 185, 0, 0.08)",
                  borderLeftColor: "var(--color-primary)",
                }}
              >
                {successMessage}
              </div>
            )}

            {/* ── Login form ─────────────────────────────────────────────── */}
            {!isRegister && (
              <form
                className="auth-form-content"
                onSubmit={handleLogin}
                noValidate
              >
                <Field
                  label="Email"
                  type="email"
                  value={loginForm.email}
                  onChange={(e) =>
                    setLoginForm((f) => ({ ...f, email: e.target.value }))
                  }
                  placeholder="ten@email.com"
                  autoComplete="email"
                />
                <Field
                  label="Mật khẩu"
                  type="password"
                  value={loginForm.password}
                  onChange={(e) =>
                    setLoginForm((f) => ({ ...f, password: e.target.value }))
                  }
                  placeholder="••••••••"
                  autoComplete="current-password"
                />

                <div
                  style={{
                    display: "flex",
                    justifyContent: "flex-end",
                    marginTop: -8,
                  }}
                >
                  <a
                    href="/auth/forgot-password"
                    className="auth-link"
                    style={{
                      fontSize: 13,
                      fontWeight: 500,
                      color: "var(--color-primary)",
                      textDecoration: "none",
                    }}
                  >
                    Quên mật khẩu?
                  </a>
                </div>

                <button
                  type="submit"
                  className="auth-submit"
                  disabled={isPending}
                >
                  {loginMutation.isPending ? "Đang đăng nhập…" : "Đăng nhập"}
                </button>
              </form>
            )}

            {/* ── Đăng nhập bằng Google ──────────────────────────────────── */}
            {!isRegister && GOOGLE_CLIENT_ID && (
              <div style={{ marginTop: 8, display: "flex", flexDirection: "column", gap: 16 }}>
                <div className="auth-divider">
                  <span>hoặc</span>
                </div>
                {/* min-height giữ sẵn chỗ: iframe nút Google load chậm nên phải reserve
                    không gian, tránh layout nhảy (pop-in) khi nút xuất hiện. */}
                <div
                  style={{
                    minHeight: 44,
                    width: "100%",
                    display: "flex",
                    justifyContent: "center",
                  }}
                >
                  <GoogleOAuthProvider clientId={GOOGLE_CLIENT_ID}>
                    <GoogleLogin
                      onSuccess={(cred) => {
                        setClientError("");
                        if (cred.credential) googleMutation.mutate(cred.credential);
                      }}
                      onError={() =>
                        setClientError(
                          "Đăng nhập Google thất bại. Nếu trình duyệt đang chặn cửa sổ popup, vui lòng cho phép popup cho trang này rồi thử lại.",
                        )
                      }
                      size="large"
                      width="316"
                      shape="rectangular"
                      text="continue_with"
                      logo_alignment="center"
                    />
                  </GoogleOAuthProvider>
                </div>
              </div>
            )}

            {/* ── Register form ──────────────────────────────────────────── */}
            {isRegister && (
              <form
                className="auth-form-content"
                onSubmit={handleRegister}
                noValidate
              >
                <Field
                  label="Họ và tên"
                  value={registerForm.fullName}
                  onChange={(e) =>
                    setRegisterForm((f) => ({ ...f, fullName: e.target.value }))
                  }
                  placeholder="Họ và tên"
                  autoComplete="name"
                />
                <Field
                  label="Email"
                  type="email"
                  value={registerForm.email}
                  onChange={(e) =>
                    setRegisterForm((f) => ({ ...f, email: e.target.value }))
                  }
                  placeholder="ten@email.com"
                  autoComplete="email"
                />

                <Field
                  label="Mật khẩu"
                  type="password"
                  value={registerForm.password}
                  onChange={(e) =>
                    setRegisterForm((f) => ({
                      ...f,
                      password: e.target.value,
                    }))
                  }
                  placeholder="Tối thiểu 6 ký tự"
                  autoComplete="new-password"
                />

                <Field
                  label="Nhập lại mật khẩu"
                  type="password"
                  value={registerForm.confirmPassword || ""}
                  onChange={(e) =>
                    setRegisterForm((f) => ({
                      ...f,
                      confirmPassword: e.target.value,
                    }))
                  }
                  placeholder="Nhập lại mật khẩu để xác nhận"
                  autoComplete="new-password"
                />

                <button
                  type="submit"
                  className="auth-submit"
                  disabled={isPending}
                >
                  {registerMutation.isPending
                    ? "Đang đăng ký…"
                    : "Tạo tài khoản"}
                </button>
              </form>
            )}

            {/* mobile fallback switch */}
            <p
              style={{
                fontSize: 13,
                color: "var(--color-mute)",
                textAlign: "center",
                display: "none",
              }}
              className="mobile-switch"
            >
              {isRegister ? "Đã có tài khoản? " : "Chưa có tài khoản? "}
              <button
                onClick={switchMode}
                style={{
                  background: "none",
                  border: "none",
                  color: "var(--color-primary)",
                  cursor: "pointer",
                  fontWeight: 700,
                  padding: 0,
                  fontSize: "inherit",
                }}
              >
                {isRegister ? "Đăng nhập" : "Đăng ký"}
              </button>
            </p>
          </div>
        </div>
      </div>

      {/* show mobile switch link only on small screens */}
      <style>{`
        @media (max-width: 768px) {
          .mobile-switch { display: block !important; }
        }
      `}</style>
    </>
  );
}
