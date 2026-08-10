"use client";

import { Suspense, useEffect, useRef, useState } from "react";
import Link from "next/link";
import { useParams, useSearchParams, useRouter } from "next/navigation";
import { useMutation } from "@tanstack/react-query";
import { Check, X, Loader2, Mail } from "lucide-react";
import { invitationsApi } from "@/features/invitations/api/invitationsApi";
import { getErrorMessage, getHttpStatus } from "@/lib/apiError";

// Trang đích cho link Đồng ý / Từ chối trong email mời vai trò sự kiện.
// URL: /invitations/{invitationId}?action=accept|decline
// - Chưa đăng nhập  -> lưu đường dẫn quay lại rồi chuyển tới /auth.
// - Đã đăng nhập    -> tự động gọi API phản hồi theo ?action, hoặc hiện 2 nút nếu thiếu action.

type Phase =
  | "checking"
  | "need-login"
  | "choose"
  | "working"
  | "accepted"
  | "declined"
  | "wrong-account"
  | "error";

function InvitationInner() {
  const params = useParams();
  const search = useSearchParams();
  const router = useRouter();
  const id = String(params?.id ?? "");
  const actionParam = search?.get("action"); // "accept" | "decline" | null

  // Đọc trực tiếp lúc render (không qua effect/setState) — component này chỉ render
  // trên client (bọc Suspense vì dùng useSearchParams) nên không có rủi ro hydration mismatch.
  const hasToken = typeof window !== "undefined" && !!localStorage.getItem("accessToken");
  const needsLogin = !hasToken && actionParam !== "decline";

  const [phase, setPhase] = useState<Phase>("checking");
  const [errorMsg, setErrorMsg] = useState("");
  const startedRef = useRef(false);

  const respond = useMutation({
    mutationFn: (accept: boolean) => invitationsApi.respondEventRole(id, accept),
    onSuccess: (_data, accept) => setPhase(accept ? "accepted" : "declined"),
    onError: (e: unknown) => {
      // 403 = đang đăng nhập bằng tài khoản KHÔNG phải người được mời -> yêu cầu đổi đúng tài khoản.
      if (getHttpStatus(e) === 403) {
        setPhase("wrong-account");
        return;
      }
      setErrorMsg(
        getErrorMessage(
          e,
          "Không thể xử lý lời mời. Có thể lời mời đã hết hạn hoặc đã được phản hồi trước đó.",
        ),
      );
      setPhase("error");
    },
  });

  const doRespond = (accept: boolean) => {
    setPhase("working");
    respond.mutate(accept);
  };

  // Đăng xuất tài khoản đang đăng nhập (sai người) rồi quay lại link này để đăng nhập ĐÚNG người được mời.
  const switchAccount = () => {
    if (typeof window !== "undefined") {
      localStorage.removeItem("accessToken");
      localStorage.removeItem("refreshToken");
      localStorage.removeItem("currentUser");
      localStorage.setItem("postLoginRedirect", `/invitations/${id}?action=${actionParam ?? "accept"}`);
    }
    router.replace("/auth");
  };

  // Từ chối qua email: công khai, KHÔNG cần đăng nhập -> hiện lời cảm ơn ngay.
  const declineMut = useMutation({
    mutationFn: () => invitationsApi.declineEventRolePublic(id),
    onSuccess: () => setPhase("declined"),
    onError: (e) => {
      setErrorMsg(
        getErrorMessage(
          e,
          "Không thể xử lý lời mời. Có thể lời mời đã hết hạn hoặc đã được phản hồi trước đó.",
        ),
      );
      setPhase("error");
    },
  });
  useEffect(() => {
    if (startedRef.current || !id) return;
    startedRef.current = true;

    // TỪ CHỐI qua email: công khai, KHÔNG cần đăng nhập -> hiện lời cảm ơn ngay.
    // Gọi mutate() trực tiếp (không setPhase trước) — phase vẫn "checking", render
    // giống hệt "working" nên không cần chuyển state đồng bộ trong effect.
    if (actionParam === "decline") {
      declineMut.mutate();
      return;
    }

    if (!hasToken) {
      // "need-login" giờ được suy ra từ `needsLogin` lúc render, effect chỉ lo phần
      // side effect thật sự (ghi localStorage), không tự set phase nữa.
      const returnUrl = actionParam ? `/invitations/${id}?action=${actionParam}` : "/";
      localStorage.setItem("postLoginRedirect", returnUrl);
      return;
    }

    if (actionParam === "accept") respond.mutate(true);
    // Đã đăng nhập nhưng không có action (vào từ link) -> mở chuông thông báo ở trang chủ.
    else router.replace("/");
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [id, actionParam, hasToken]);

  // ── Nội dung theo từng trạng thái ─────────────────────────────────────────
  let icon: React.ReactNode;
  let title = "";
  let message = "";
  let actions: React.ReactNode = null;

  const spinner = <Loader2 className="w-10 h-10 animate-spin" style={{ color: "var(--color-primary)" }} />;
  const okBadge = (
    <span className="w-16 h-16 rounded-full flex items-center justify-center" style={{ background: "rgba(118,185,0,0.12)" }}>
      <Check className="w-8 h-8" style={{ color: "var(--color-primary)" }} />
    </span>
  );
  const badBadge = (
    <span className="w-16 h-16 rounded-full flex items-center justify-center" style={{ background: "rgba(229,32,32,0.08)" }}>
      <X className="w-8 h-8" style={{ color: "var(--color-error)" }} />
    </span>
  );
  const mailBadge = (
    <span className="w-16 h-16 rounded-full flex items-center justify-center" style={{ background: "rgba(118,185,0,0.12)" }}>
      <Mail className="w-8 h-8" style={{ color: "var(--color-primary)" }} />
    </span>
  );
  const homeLink = (
    <Link href="/" className="inv-btn">Về trang chủ</Link>
  );

  // "need-login" được suy ra trực tiếp từ hasToken/actionParam thay vì lưu vào `phase`
  // (tránh setState đồng bộ trong effect); chỉ áp dụng khi chưa có phản hồi thật nào xảy ra.
  const effectivePhase: Phase =
    needsLogin && (phase === "checking" || phase === "working") ? "need-login" : phase;

  switch (effectivePhase) {
    case "checking":
    case "working":
      icon = spinner;
      title = "Đang xử lý lời mời…";
      message = "Vui lòng chờ trong giây lát.";
      break;
    case "need-login":
      icon = mailBadge;
      title = "Vui lòng đăng nhập";
      message = "Bạn cần đăng nhập để phản hồi lời mời này. Sau khi đăng nhập, bạn sẽ được đưa lại đây để hoàn tất.";
      actions = <Link href="/auth" className="inv-btn">Đăng nhập để tiếp tục</Link>;
      break;
    case "choose":
      icon = mailBadge;
      title = "Phản hồi lời mời";
      message = "Bạn muốn chấp nhận hay từ chối lời mời vai trò này?";
      actions = (
        <div className="inv-actions">
          <button onClick={() => doRespond(false)} className="inv-btn inv-btn--ghost">
            Từ chối
          </button>
          <button onClick={() => doRespond(true)} className="inv-btn">
            Đồng ý
          </button>
        </div>
      );
      break;
    case "accepted":
      icon = okBadge;
      title = "Đã nhận vai trò!";
      message = "Bạn đã chấp nhận lời mời. Vai trò đã được thêm vào tài khoản của bạn — hãy kiểm tra ở chuông thông báo.";
      actions = homeLink;
      break;
    case "declined":
      icon = badBadge;
      title = "Cảm ơn bạn đã phản hồi";
      message = "Bạn đã từ chối lời mời vai trò này nên sẽ không có vai trò nào được tạo. Cảm ơn bạn đã dành thời gian phản hồi — hẹn gặp lại ở những sự kiện sau!";
      actions = homeLink;
      break;
    case "wrong-account":
      icon = mailBadge;
      title = "Sai tài khoản đăng nhập";
      message = "Lời mời này dành cho một tài khoản khác. Bạn đang đăng nhập bằng tài khoản KHÔNG được mời nên không thể chấp nhận. Vui lòng đăng nhập đúng tài khoản đã nhận lời mời (email nhận thư này).";
      actions = <button onClick={switchAccount} className="inv-btn">Đăng nhập tài khoản khác</button>;
      break;
    case "error":
    default:
      icon = badBadge;
      title = "Không thể xử lý lời mời";
      message = errorMsg || "Đã xảy ra lỗi. Vui lòng thử lại sau.";
      actions = homeLink;
      break;
  }

  return (
    <div className="inv-card">
      <span className="inv-tag">SEAL · Lời mời vai trò sự kiện</span>
      {icon}
      <h1 className="inv-title">{title}</h1>
      <p className="inv-message">{message}</p>
      {actions}
    </div>
  );
}

export default function InvitationResponsePage() {
  return (
    <>
      <style>{`
        .inv-root {
          position: fixed;
          inset: 0;
          display: flex;
          align-items: center;
          justify-content: center;
          padding: 24px;
          background: var(--color-surface-soft);
          font-family: var(--font-sans);
          background-image: radial-gradient(circle, rgba(118,185,0,0.08) 1px, transparent 1px);
          background-size: 28px 28px;
        }
        .inv-card {
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
        .inv-tag {
          font-size: 10px;
          font-weight: 700;
          letter-spacing: 0.12em;
          text-transform: uppercase;
          color: var(--color-primary);
        }
        .inv-title {
          font-size: 24px;
          font-weight: 700;
          line-height: 1.2;
          color: var(--color-ink);
          margin: 0;
        }
        .inv-message {
          font-size: 14px;
          line-height: 1.65;
          color: var(--color-mute);
          margin: 0;
          max-width: 340px;
        }
        .inv-actions {
          display: flex;
          gap: 12px;
          margin-top: 8px;
        }
        .inv-btn {
          margin-top: 8px;
          height: 44px;
          padding: 0 24px;
          display: inline-flex;
          align-items: center;
          justify-content: center;
          gap: 8px;
          background: var(--color-primary);
          color: #fff;
          border: 2px solid var(--color-primary);
          border-radius: 2px;
          font-size: 14px;
          font-weight: 700;
          letter-spacing: 0.04em;
          text-decoration: none;
          cursor: pointer;
          transition: background 80ms linear;
        }
        .inv-btn:hover { background: var(--color-primary-dark); border-color: var(--color-primary-dark); }
        .inv-btn--ghost {
          background: #fff;
          color: var(--color-error);
          border-color: var(--color-error);
        }
        .inv-btn--ghost:hover { background: rgba(229,32,32,0.06); }
      `}</style>

      <div className="inv-root">
        <Suspense fallback={<div className="inv-card"><Loader2 className="w-10 h-10 animate-spin" style={{ color: "var(--color-primary)" }} /></div>}>
          <InvitationInner />
        </Suspense>
      </div>
    </>
  );
}
