"use client";

import { useEffect, useRef, useState, type ChangeEvent, type FormEvent } from "react";
import { X, MoreHorizontal } from "lucide-react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { StatusBadge } from '@/components/StatusBadge';
import {
  usersApi,
  schoolsApi,
  type UserSummary,
  type SchoolModel,
  type CreateUserPayload,
} from "@/services/api";
import { eventRolesApi } from "@/features/events/api/eventRoles";
import { useIsAuthenticated, useCurrentUser } from "@/hooks/useAuth";
import { useUserRole } from "@/hooks/useUserRole";
import { useAllEvents } from "@/features/events/hooks/useEvents";
import { useNotify } from "@/components/NotificationProvider";
import { useDialog } from "@/components/ConfirmDialogProvider";
import { getErrorMessage } from "@/lib/apiError";
import { EventRoleBadge } from "@/components/EventRoleBadge";

const PAGE_SIZE = 20;

const errMsg = getErrorMessage;

function Badge({
  children,
  tone = "neutral",
}: {
  children: React.ReactNode;
  tone?: "neutral" | "primary" | "success" | "warning" | "danger" | "pending";
}) {
  const map = {
    neutral: { bg: "var(--color-surface-soft)", fg: "var(--color-mute)", bd: "var(--color-hairline)" },
    primary: { bg: "rgba(118,185,0,0.1)", fg: "var(--color-primary)", bd: "var(--color-primary)" },
    success: { bg: "rgba(118,185,0,0.1)", fg: "var(--color-primary)", bd: "var(--color-primary)" },
    warning: { bg: "var(--color-surface-soft)", fg: "var(--color-stone)", bd: "var(--color-hairline-strong)" },
    danger: { bg: "rgba(220,38,38,0.1)", fg: "var(--color-error)", bd: "var(--color-error)" },
    pending: { bg: "rgba(245,158,11,0.14)", fg: "#b45309", bd: "rgba(245,158,11,0.55)" },
  }[tone];
  return (
    <span
      className="badge-tag"
      style={{ background: map.bg, color: map.fg, border: `1px solid ${map.bd}`, fontSize: "var(--fs-utility-xs)" }}
    >
      {children}
    </span>
  );
}

const th: React.CSSProperties = {
  textAlign: "left",
  padding: "12px 10px",
  fontSize: 11,
  fontWeight: 700,
  letterSpacing: "0.06em",
  textTransform: "uppercase",
  color: "var(--color-mute)",
  borderBottom: "1px solid var(--color-hairline-strong)",
  whiteSpace: "nowrap",
};
const td: React.CSSProperties = {
  padding: "12px 10px",
  fontSize: "var(--fs-body-sm)",
  color: "var(--color-ink)",
  borderBottom: "1px solid var(--color-hairline)",
  verticalAlign: "top",
};

// ─── Kebab (…) action menu ───────────────────────────────────────────────────────

type MenuItem = {
  label: string;
  onClick: () => void;
  disabled?: boolean;
  tone?: "primary" | "danger" | "neutral";
};

/** Gom các nút thao tác của một dòng vào menu dấu ba chấm. Dùng position:fixed
 *  để dropdown không bị cắt bởi vùng overflow của bảngh. */
function ActionMenu({ items }: { items: MenuItem[] }) {
  const [open, setOpen] = useState(false);
  const [pos, setPos] = useState<{ top: number; right: number }>({ top: 0, right: 0 });
  const btnRef = useRef<HTMLButtonElement>(null);
  const menuRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!open) return;
    const close = () => setOpen(false);
    const onDoc = (e: MouseEvent) => {
      if (
        menuRef.current && !menuRef.current.contains(e.target as Node) &&
        btnRef.current && !btnRef.current.contains(e.target as Node)
      ) setOpen(false);
    };
    const onEsc = (e: KeyboardEvent) => { if (e.key === "Escape") setOpen(false); };
    document.addEventListener("mousedown", onDoc);
    document.addEventListener("keydown", onEsc);
    window.addEventListener("scroll", close, true);
    window.addEventListener("resize", close);
    return () => {
      document.removeEventListener("mousedown", onDoc);
      document.removeEventListener("keydown", onEsc);
      window.removeEventListener("scroll", close, true);
      window.removeEventListener("resize", close);
    };
  }, [open]);

  if (items.length === 0) return <span style={{ color: "var(--color-mute)" }}>—</span>;

  const toggle = () => {
    const r = btnRef.current?.getBoundingClientRect();
    if (r) setPos({ top: r.bottom + 4, right: Math.max(8, window.innerWidth - r.right) });
    setOpen((o) => !o);
  };

  return (
    <>
      <button
        ref={btnRef}
        type="button"
        onClick={toggle}
        aria-haspopup="menu"
        aria-expanded={open}
        aria-label="Thao tác"
        style={{
          display: "inline-flex",
          alignItems: "center",
          justifyContent: "center",
          width: 32,
          height: 32,
          background: "none",
          border: "1px solid var(--color-hairline-strong)",
          borderRadius: "var(--radius-sm)",
          color: "var(--color-ink)",
          cursor: "pointer",
        }}
      >
        <MoreHorizontal size={16} aria-hidden="true" />
      </button>
      {open && (
        <div
          ref={menuRef}
          role="menu"
          style={{
            position: "fixed",
            top: pos.top,
            right: pos.right,
            minWidth: 168,
            background: "var(--color-canvas)",
            border: "1px solid var(--color-hairline)",
            borderRadius: "var(--radius-sm)",
            boxShadow: "0 8px 24px rgba(0,0,0,.18)",
            padding: "4px 0",
            zIndex: 200,
          }}
        >
          {items.map((it, i) => (
            <button
              key={i}
              type="button"
              role="menuitem"
              disabled={it.disabled}
              onClick={() => { setOpen(false); it.onClick(); }}
              className="t-body-sm"
              style={{
                display: "block",
                width: "100%",
                textAlign: "left",
                background: "none",
                border: "none",
                padding: "8px 14px",
                cursor: it.disabled ? "not-allowed" : "pointer",
                opacity: it.disabled ? 0.5 : 1,
                fontWeight: 600,
                color:
                  it.tone === "danger" ? "var(--color-error)" :
                    it.tone === "primary" ? "var(--color-primary)" :
                      "var(--color-ink)",
                whiteSpace: "nowrap",
              }}
              onMouseEnter={(e) => { if (!it.disabled) e.currentTarget.style.background = "var(--color-surface-soft)"; }}
              onMouseLeave={(e) => (e.currentTarget.style.background = "transparent")}
            >
              {it.label}
            </button>
          ))}
        </div>
      )}
    </>
  );
}

export function UsersList() {
  const isAuthenticated = useIsAuthenticated();
  const isAdmin = useUserRole() === "admin";
  const { data: currentUser } = useCurrentUser();
  const queryClient = useQueryClient();
  const notify = useNotify();
  const dialog = useDialog();
  const [query, setQuery] = useState("");
  const [debounced, setDebounced] = useState("");
  const [filter, setFilter] = useState<"all" | "pending" | "approved" | "rejected">("all");
  const [eventId, setEventId] = useState("");
  const [accountType, setAccountType] = useState<"all" | "non_student" | "student_fpt" | "student_other">("all");
  const [page, setPage] = useState(1);
  const [actionError, setActionError] = useState<string | null>(null);
  const [showCreate, setShowCreate] = useState(false);
  const [viewUser, setViewUser] = useState<UserSummary | null>(null);
  const [preview, setPreview] = useState<{ url: string; name: string } | null>(null);
  const timerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  const eventsQuery = useAllEvents(isAdmin);

  const usersQuery = useQuery({
    queryKey: ["users", "list", filter, debounced, eventId, accountType, page],
    queryFn: () =>
      usersApi.list({
        search: debounced,
        pageNumber: page,
        pageSize: PAGE_SIZE,
        // BE chỉ lọc được IsApproved. "Chờ duyệt" và "Bị từ chối" đều là chưa duyệt +
        // đã nộp hồ sơ → lấy chung rồi tách phía FE theo cờ isRejected (xem `users`).
        isApproved:
          filter === "approved" ? true : filter === "pending" || filter === "rejected" ? false : undefined,
        hasSubmittedProfile: filter === "pending" || filter === "rejected" ? true : undefined,
        eventId: eventId || undefined,
        isStudent: accountType === "non_student" ? false : (accountType.startsWith("student_") ? true : undefined),
        isFpt: accountType === "student_fpt" ? true : (accountType === "student_other" ? false : undefined),
      }),
    enabled: isAuthenticated,
    staleTime: 60_000,
  });

  // Toggle account active state via isApproved (PUT /Users/{id}).
  const toggleMutation = useMutation({
    mutationFn: (u: UserSummary) =>
      usersApi.update(u.id, {
        schoolId: u.schoolId,
        studentCode: u.studentCode,
        fullName: u.fullName,
        isStudent: u.isStudent,
        isAdmin: u.isAdmin,
        isApproved: !u.isApproved,
        isFpt: u.isFpt,
        photoStudentCardUrl: null,
      }),
    onSuccess: (_data, u) => {
      setActionError(null);
      notify.success(u.isApproved ? "Đã thu hồi duyệt tài khoản!" : "Đã kích hoạt tài khoản!");
      queryClient.invalidateQueries({ queryKey: ["users", "list"] });
    },
    onError: (e) => {
      const msg = errMsg(e);
      setActionError(msg);
      notify.error(msg);
    },
  });

  async function handleToggle(u: UserSummary) {
    if (u.isApproved) {
      const ok = await dialog.confirm({
        title: "Thu hồi duyệt tài khoản",
        message: `Thu hồi duyệt tài khoản ${u.fullName || u.email}?\nTài khoản sẽ trở về trạng thái "Chờ duyệt" và không thể tham gia cho đến khi được duyệt lại.`,
        confirmText: "Thu hồi duyệt",
        danger: true,
      });
      if (!ok) return;
    }
    setActionError(null);
    toggleMutation.mutate(u);
  }

  // Duyệt hồ sơ sinh viên (POST /Users/{id}/approve).
  const approveMutation = useMutation({
    mutationFn: (u: UserSummary) => usersApi.approve(u.id),
    onSuccess: () => {
      setActionError(null);
      notify.success("Đã duyệt tài khoản thành công!");
      queryClient.invalidateQueries({ queryKey: ["users", "list"] });
    },
    onError: (e) => {
      const msg = errMsg(e);
      setActionError(msg);
      notify.error(msg);
    },
  });

  // Từ chối hồ sơ sinh viên kèm lý do (POST /Users/{id}/reject).
  const rejectMutation = useMutation({
    mutationFn: (vars: { u: UserSummary; reason: string }) => usersApi.reject(vars.u.id, vars.reason),
    onSuccess: () => {
      setActionError(null);
      notify.success("Đã từ chối tài khoản thành công!");
      queryClient.invalidateQueries({ queryKey: ["users", "list"] });
    },
    onError: (e) => {
      const msg = errMsg(e);
      setActionError(msg);
      notify.error(msg);
    },
  });

  async function handleReject(u: UserSummary) {
    const reason = await dialog.prompt({
      title: "Từ chối hồ sơ",
      message: `Từ chối đăng ký của ${u.fullName || u.email || "tài khoản này"}? Lý do sẽ được gửi cho người đăng ký.`,
      label: "Lý do từ chối",
      placeholder: "VD: Ảnh thẻ sinh viên không hợp lệ…",
      required: true,
      multiline: true,
      danger: true,
      confirmText: "Từ chối",
    });
    if (reason === null) return; // hủy
    setActionError(null);
    rejectMutation.mutate({ u, reason });
  }

  // Xoá tài khoản = soft delete (DELETE /Users/{id}) — chặn truy cập hệ thống.
  const deleteMutation = useMutation({
    mutationFn: (u: UserSummary) => usersApi.remove(u.id),
    onSuccess: () => {
      setActionError(null);
      notify.success("Đã xoá tài khoản thành công!");
      queryClient.invalidateQueries({ queryKey: ["users", "list"] });
    },
    onError: (e) => {
      const msg = errMsg(e);
      setActionError(msg);
      notify.error(msg);
    },
  });

  async function handleDelete(u: UserSummary) {
    const ok = await dialog.confirm({
      title: "Xoá tài khoản",
      message: `Xoá tài khoản ${u.fullName || u.email || "này"}?\nTài khoản sẽ bị chặn truy cập hệ thống.`,
      confirmText: "Xoá tài khoản",
      danger: true,
    });
    if (!ok) return;
    setActionError(null);
    deleteMutation.mutate(u);
  }
  const schoolsQuery = useQuery({
    queryKey: ["schools"],
    queryFn: () => schoolsApi.list(),
    enabled: isAuthenticated,
    staleTime: 5 * 60_000,
  });

  // Khi chọn 1 sự kiện → load roles của event đó để hiển thị vai trò event.
  const eventRolesQuery = useQuery({
    queryKey: ["eventRoles", "byEvent", eventId],
    queryFn: () => eventRolesApi.listByEvent(eventId),
    enabled: isAuthenticated && !!eventId,
    staleTime: 60_000,
  });

  // Map userId → roleName trong event đang chọn.
  const userEventRoleMap: Record<string, string> = {};
  if (eventId && eventRolesQuery.data) {
    for (const r of eventRolesQuery.data) {
      userEventRoleMap[r.userId] = r.roleName;
    }
  }

  // Admin: create a new account.
  const createMutation = useMutation({
    mutationFn: usersApi.create,
    onSuccess: () => {
      setActionError(null);
      setShowCreate(false);
      notify.success("Đã tạo tài khoản thành công!");
      queryClient.invalidateQueries({ queryKey: ["users", "list"] });
    },
    onError: (e) => {
      const msg = errMsg(e);
      setActionError(msg);
      notify.error(msg);
    },
  });

  // Admin: sửa thông tin user (Họ tên, MSSV, Trường) — role/trạng thái giữ nguyên.
  const editMutation = useMutation({
    mutationFn: (vars: {
      id: string;
      user: UserSummary;
      edits: { fullName: string; studentCode: string; schoolId: string };
    }) =>
      usersApi.update(vars.id, {
        schoolId: vars.edits.schoolId || null,
        studentCode: vars.edits.studentCode.trim() || null,
        fullName: vars.edits.fullName.trim(),
        isStudent: vars.user.isStudent,
        isAdmin: vars.user.isAdmin,
        isApproved: vars.user.isApproved,
        isFpt: vars.user.isFpt,
        photoStudentCardUrl: null,
      }),
    onSuccess: () => {
      setActionError(null);
      notify.success("Đã cập nhật tài khoản thành công!");
      queryClient.invalidateQueries({ queryKey: ["users", "list"] });
    },
    onError: (e) => {
      const msg = errMsg(e);
      setActionError(msg);
      notify.error(msg);
    },
  });

  const rawUsers = usersQuery.data?.data ?? [];
  // Tách "Chờ duyệt" / "Bị từ chối" phía FE (BE không lọc được isRejected).
  const users =
    filter === "rejected"
      ? rawUsers.filter((u) => u.isRejected)
      : filter === "pending"
        ? rawUsers.filter((u) => !u.isRejected)
        : rawUsers;
  // Với 2 tab tách phía FE, số đếm/ phân trang dựa trên tập của trang hiện tại.
  const clientSplit = filter === "pending" || filter === "rejected";
  const total = clientSplit ? users.length : usersQuery.data?.totalItems ?? users.length;
  const totalPages = Math.max(1, usersQuery.data?.totalPages ?? 1);
  const currentPage = Math.min(page, totalPages);
  const schoolName = (id: string | null) =>
    id ? schoolsQuery.data?.data?.find((s) => s.id === id)?.schoolName ?? "—" : "—";

  const onType = (e: ChangeEvent<HTMLInputElement>) => {
    const v = e.target.value;
    setQuery(v);
    if (timerRef.current) clearTimeout(timerRef.current);
    timerRef.current = setTimeout(() => {
      setDebounced(v.trim());
      setPage(1);
    }, 300);
  };

  return (
    <main tabIndex={-1} style={{ outline: "none", minHeight: "100vh" }}>
      <div className="container" style={{ paddingTop: "var(--space-section)", maxWidth: "min(1400px, 96vw)" }}>
        <div className="card" style={{ padding: "var(--space-xxl)", gap: "var(--space-xl)" }}>
          {/* Header */}
          <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", flexWrap: "wrap", gap: "var(--space-md)" }}>
            <div>
              <h1 className="t-heading-md" style={{ margin: 0 }}>Người dùng</h1>
              <p className="t-body-sm" style={{ margin: "2px 0 0", color: "var(--color-mute)" }}>
                {!isAuthenticated
                  ? "Danh sách tài khoản trong hệ thống"
                  : filter === "pending"
                    ? `${total} tài khoản chờ duyệt`
                    : filter === "approved"
                      ? `${total} tài khoản đã duyệt`
                      : filter === "rejected"
                        ? `${total} tài khoản bị từ chối`
                        : `${total} người dùng`}
              </p>
            </div>
            <div style={{ display: "flex", alignItems: "center", gap: "var(--space-sm)", flexWrap: "wrap" }}>
              <input
                className="text-input"
                value={query}
                onChange={onType}
                placeholder="Tìm theo tên hoặc email…"
                style={{ width: 280, maxWidth: "100%" }}
              />
              <select
                className="text-input"
                value={accountType}
                onChange={(e) => {
                  setAccountType(e.target.value as typeof accountType);
                  setPage(1);
                }}
                style={{ width: 200, maxWidth: "100%" }}
                aria-label="Lọc theo loại tài khoản"
              >
                <option value="all">Tất cả tài khoản</option>
                <option value="non_student">Tài khoản khách (Khác)</option>
                <option value="student_fpt">Sinh viên FPT</option>
                <option value="student_other">Sinh viên trường khác</option>
              </select>
              <select
                className="text-input"
                value={eventId}
                onChange={(e) => {
                  setEventId(e.target.value);
                  setPage(1);
                }}
                style={{ width: 220, maxWidth: "100%" }}
                aria-label="Lọc theo sự kiện"
              >
                <option value="">Tất cả sự kiện</option>
                {(eventsQuery.data ?? []).map((ev) => (
                  <option key={ev.id} value={ev.id}>{ev.title}</option>
                ))}
              </select>
              {isAdmin && (
                <button
                  type="button"
                  onClick={() => {
                    setActionError(null);
                    setShowCreate(true);
                  }}
                  className="btn btn-create"
                  style={{
                    cursor: "pointer",
                    whiteSpace: "nowrap",
                  }}
                >
                  + Thêm tài khoản
                </button>
              )}
            </div>
          </div>

          {/* Filter tabs */}
          {isAuthenticated && (
            <div style={{ display: "flex", gap: "var(--space-sm)" }}>
              {([
                { id: "all", label: "Tất cả" },
                { id: "pending", label: "Chờ duyệt" },
                { id: "approved", label: "Đã duyệt" },
                { id: "rejected", label: "Bị từ chối" },
              ] as const).map(({ id, label }) => (
                <button
                  key={id}
                  type="button"
                  className={`pill-tab${filter === id ? " is-active" : ""}`}
                  onClick={() => {
                    setFilter(id);
                    setPage(1);
                  }}
                >
                  {label}
                </button>
              ))}
            </div>
          )}

          {!isAuthenticated ? (
            <p className="t-body-sm" style={{ color: "var(--color-mute)", textAlign: "center", padding: "var(--space-xl)" }}>
              Vui lòng đăng nhập để xem danh sách người dùng.
            </p>
          ) : usersQuery.isError ? (
            <p className="t-body-sm" style={{ color: "var(--color-error)", textAlign: "center", padding: "var(--space-xl)" }}>
              Không tải được danh sách người dùng.
            </p>
          ) : usersQuery.isLoading ? (
            <p className="t-body-sm" style={{ color: "var(--color-mute)", textAlign: "center", padding: "var(--space-xl)" }}>
              Đang tải…
            </p>
          ) : users.length === 0 ? (
            <p className="t-body-sm" style={{ color: "var(--color-mute)", textAlign: "center", padding: "var(--space-xl)" }}>
              {filter === "pending"
                ? "Không có tài khoản chờ duyệt."
                : filter === "approved"
                  ? "Không có tài khoản đã duyệt."
                  : filter === "rejected"
                    ? "Không có tài khoản bị từ chối."
                    : "Không có người dùng nào."}
            </p>
          ) : (
            <>
              {actionError && (
                <p className="t-caption-sm" style={{ color: "var(--color-error)", margin: 0 }}>{actionError}</p>
              )}
              <div style={{ overflowX: "auto" }}>
                <table style={{ width: "100%", borderCollapse: "collapse" }}>
                  <thead>
                    <tr>
                      <th style={th}>Ảnh thẻ</th>
                      <th style={th}>Họ tên</th>
                      <th style={th}>Email</th>
                      <th style={th}>MSSV</th>
                      <th style={th}>Trường</th>
                      <th style={th}>Vai trò</th>
                      {filter !== "all" && <th style={th}>Trạng thái</th>}
                      <th style={{ ...th, textAlign: "right" }}>Thao tác</th>
                    </tr>
                  </thead>
                  <tbody>
                    {users.map((u) => {
                      const pending =
                        toggleMutation.isPending && toggleMutation.variables?.id === u.id;
                      const approveBusy =
                        approveMutation.isPending && approveMutation.variables?.id === u.id;
                      const rejectBusy =
                        rejectMutation.isPending && rejectMutation.variables?.u.id === u.id;
                      const deleteBusy =
                        deleteMutation.isPending && deleteMutation.variables?.id === u.id;

                      // Không cho admin tự thu hồi duyệt / tự xoá chính mình (BE cũng chặn 400).
                      const isSelf = !!currentUser?.id && u.id === currentUser.id;
                      // Tài khoản judge/mentor ĐƯỢC MỜI (chưa kích hoạt) — không nộp hồ sơ để duyệt.
                      const isInvited = u.isTemporary === true;

                      // Gom thao tác theo vòng đời tài khoản (chỉ có cờ isApproved):
                      //  • Chờ duyệt  → Duyệt / Từ chối / Sửa
                      //  • Đã duyệt   → Sửa / Vô hiệu hóa
                      //  • Được mời   → không có Duyệt/Từ chối (họ được mời, không cần duyệt)
                      const menuItems: MenuItem[] = [];

                      if (isAdmin) {
                        menuItems.push({
                          label: "Xem chi tiết",
                          onClick: () => { setActionError(null); setViewUser(u); },
                        });
                      }
                      // Tài khoản bị từ chối → cho phép admin duyệt lại.
                      if (isAdmin && !u.isApproved && u.isRejected && !isInvited) {
                        menuItems.push({
                          label: approveBusy ? "Đang lưu…" : "Duyệt lại",
                          tone: "primary",
                          disabled: approveBusy,
                          onClick: () => { setActionError(null); approveMutation.mutate(u); },
                        });
                      }
                      if (u.isApproved && !isSelf) {
                        menuItems.push({
                          label: pending ? "Đang lưu…" : "Thu hồi duyệt",
                          tone: "danger",
                          disabled: pending,
                          onClick: () => handleToggle(u),
                        });
                      } else if (!isAdmin) {
                        menuItems.push({
                          label: pending ? "Đang lưu…" : "Kích hoạt",
                          tone: "primary",
                          disabled: pending,
                          onClick: () => handleToggle(u),
                        });
                      }
                      if (isAdmin && !isSelf) {
                        menuItems.push({
                          label: deleteBusy ? "Đang xoá…" : "Xoá tài khoản",
                          tone: "danger",
                          disabled: deleteBusy,
                          onClick: () => handleDelete(u),
                        });
                      }

                      return (
                        <tr key={u.id}>
                          <td style={td}>
                            {u.photoStudentCardUrl ? (
                              <button
                                type="button"
                                onClick={() => setPreview({ url: u.photoStudentCardUrl!, name: u.fullName ?? "" })}
                                aria-label={`Xem ảnh thẻ của ${u.fullName || u.email || "người dùng"}`}
                                style={{ display: "block", background: "none", border: "none", padding: 0, cursor: "pointer" }}
                              >
                                {/* eslint-disable-next-line @next/next/no-img-element */}
                                <img
                                  src={u.photoStudentCardUrl}
                                  alt="Ảnh thẻ sinh viên"
                                  style={{ width: 56, height: 40, objectFit: "cover", borderRadius: "var(--radius-sm)", border: "1px solid var(--color-hairline)" }}
                                />
                              </button>
                            ) : (
                              <span style={{ color: "var(--color-mute)" }}>—</span>
                            )}
                          </td>
                          <td style={{ ...td, fontWeight: 700 }}>{u.fullName || "—"}</td>
                          <td style={td}>{u.email || "—"}</td>
                          <td style={td}>{u.studentCode || "—"}</td>
                          <td style={td}>
                            {u.isFpt ? "FPT University" : schoolName(u.schoolId)}
                          </td>
                          <td style={td}>
                            {eventId && userEventRoleMap[u.id] ? (
                              <EventRoleBadge roleName={userEventRoleMap[u.id]} />
                            ) : (
                              <Badge tone={u.isAdmin ? "primary" : "neutral"}>
                                {u.isAdmin ? "Quản trị viên" : "Người dùng"}
                              </Badge>
                            )}
                          </td>
                          {filter !== "all" && (
                            <td style={td}>
                              {isInvited ? (
                                <Badge tone="neutral">Được mời</Badge>
                              ) : (
                                <StatusBadge tone={u.isApproved ? "success" : u.isRejected ? "danger" : "pending"}>
                                  {u.isApproved ? "Đã duyệt" : u.isRejected ? "Bị từ chối" : "Chờ duyệt"}
                                </StatusBadge>
                              )}
                            </td>
                          )}
                          <td style={{ ...td, textAlign: "right" }}>
                            {filter === "pending" && isAdmin && !isInvited ? (
                              <div style={{ display: "inline-flex", gap: "var(--space-sm)", justifyContent: "flex-end" }}>
                                <button
                                  type="button"
                                  onClick={() => { setActionError(null); setViewUser(u); }}
                                  className="btn btn-view btn-label-size"
                                  style={{
                                    cursor: "pointer",
                                    whiteSpace: "nowrap",
                                  }}
                                >
                                  Chi tiết
                                </button>
                                <button
                                  type="button"
                                  disabled={approveBusy || rejectBusy}
                                  onClick={() => {
                                    setActionError(null);
                                    approveMutation.mutate(u);
                                  }}
                                  className="t-caption-sm btn-label-size"
                                  style={{
                                    fontWeight: 700,
                                    background: "none",
                                    border: "1px solid var(--color-primary)",
                                    color: "var(--color-primary)",
                                    borderRadius: "var(--radius-sm)",
                                    cursor: approveBusy || rejectBusy ? "not-allowed" : "pointer",
                                    opacity: approveBusy || rejectBusy ? 0.5 : 1,
                                    whiteSpace: "nowrap",
                                  }}
                                >
                                  {approveBusy ? "Đang lưu…" : "Duyệt"}
                                </button>
                                <button
                                  type="button"
                                  disabled={approveBusy || rejectBusy}
                                  onClick={() => handleReject(u)}
                                  className="t-caption-sm btn-label-size"
                                  style={{
                                    fontWeight: 700,
                                    background: "none",
                                    border: "1px solid var(--color-error)",
                                    color: "var(--color-error)",
                                    borderRadius: "var(--radius-sm)",
                                    cursor: approveBusy || rejectBusy ? "not-allowed" : "pointer",
                                    opacity: approveBusy || rejectBusy ? 0.5 : 1,
                                    whiteSpace: "nowrap",
                                  }}
                                >
                                  {rejectBusy ? "Đang xử lý…" : "Từ chối"}
                                </button>
                              </div>
                            ) : (
                              <ActionMenu items={menuItems} />
                            )}
                          </td>
                        </tr>
                      );
                    })}
                  </tbody>
                </table>
              </div>

              {totalPages > 1 && (
                <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", gap: "var(--space-md)" }}>
                  <span className="t-caption-sm" style={{ color: "var(--color-mute)" }}>
                    Trang {currentPage}/{totalPages}
                  </span>
                  <div style={{ display: "flex", gap: "var(--space-sm)" }}>
                    <button
                      type="button"
                      disabled={currentPage <= 1}
                      onClick={() => setPage((p) => Math.max(1, p - 1))}
                      className="t-caption-sm"
                      style={{
                        display: "inline-flex",
                        alignItems: "center",
                        gap: 4,
                        fontWeight: 700,
                        color: "var(--color-ink)",
                        background: "none",
                        border: "1px solid var(--color-hairline-strong)",
                        borderRadius: "var(--radius-sm)",
                        padding: "4px 10px",
                        cursor: currentPage <= 1 ? "not-allowed" : "pointer",
                        opacity: currentPage <= 1 ? 0.4 : 1,
                      }}
                    >
                      Trước
                    </button>
                    <button
                      type="button"
                      disabled={currentPage >= totalPages}
                      onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
                      className="t-caption-sm"
                      style={{
                        display: "inline-flex",
                        alignItems: "center",
                        gap: 4,
                        fontWeight: 700,
                        color: "var(--color-ink)",
                        background: "none",
                        border: "1px solid var(--color-hairline-strong)",
                        borderRadius: "var(--radius-sm)",
                        padding: "4px 10px",
                        cursor: currentPage >= totalPages ? "not-allowed" : "pointer",
                        opacity: currentPage >= totalPages ? 0.4 : 1,
                      }}
                    >
                      Sau
                    </button>
                  </div>
                </div>
              )}
            </>
          )}
        </div>
      </div>

      {isAdmin && showCreate && (
        <CreateUserModal
          schools={schoolsQuery.data?.data ?? []}
          busy={createMutation.isPending}
          onClose={() => setShowCreate(false)}
          onSubmit={(payload) => createMutation.mutate(payload)}
        />
      )}

      {isAdmin && viewUser && (
        <UserDetailModal
          user={viewUser}
          schools={schoolsQuery.data?.data ?? []}
          busy={editMutation.isPending}
          onClose={() => setViewUser(null)}
          onSubmit={(edits) =>
            editMutation.mutate({ id: viewUser.id, user: viewUser, edits })
          }
        />
      )}

      {/* Ảnh thẻ phóng to */}
      {preview && (
        <div
          role="dialog"
          aria-modal="true"
          aria-label="Ảnh thẻ sinh viên"
          onClick={() => setPreview(null)}
          style={{
            position: "fixed",
            inset: 0,
            zIndex: 100,
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
            padding: 16,
            background: "rgba(0,0,0,0.7)",
          }}
        >
          <div style={{ position: "relative", maxWidth: "90vw", maxHeight: "90vh" }} onClick={(e) => e.stopPropagation()}>
            <button
              type="button"
              onClick={() => setPreview(null)}
              aria-label="Đóng"
              style={{
                position: "absolute",
                top: -12,
                right: -12,
                width: 32,
                height: 32,
                borderRadius: "50%",
                background: "var(--color-canvas)",
                border: "1px solid var(--color-hairline)",
                display: "flex",
                alignItems: "center",
                justifyContent: "center",
                color: "var(--color-ink)",
                cursor: "pointer",
              }}
            >
              <X size={16} aria-hidden="true" />
            </button>
            {/* eslint-disable-next-line @next/next/no-img-element */}
            <img
              src={preview.url}
              alt={`Ảnh thẻ của ${preview.name}`}
              style={{ width: "min(480px, 88vw)", height: "auto", maxHeight: "80vh", objectFit: "contain", borderRadius: "var(--radius-sm)" }}
            />
          </div>
        </div>
      )}
    </main>
  );
}

// ─── Modal shell ────────────────────────────────────────────────────────────────

function Modal({
  title,
  onClose,
  children,
}: {
  title: string;
  onClose: () => void;
  children: React.ReactNode;
}) {
  useEffect(() => {
    const onEsc = (e: KeyboardEvent) => {
      if (e.key === "Escape") onClose();
    };
    document.addEventListener("keydown", onEsc);
    document.body.style.overflow = "hidden";
    return () => {
      document.removeEventListener("keydown", onEsc);
      document.body.style.overflow = "";
    };
  }, [onClose]);

  return (
    <div
      onClick={onClose}
      style={{
        position: "fixed",
        inset: 0,
        zIndex: 100,
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        padding: 16,
        background: "rgba(0,0,0,0.6)",
      }}
    >
      <div
        role="dialog"
        aria-modal="true"
        onClick={(e) => e.stopPropagation()}
        style={{
          width: "100%",
          maxWidth: 440,
          maxHeight: "90vh",
          overflowY: "auto",
          background: "var(--color-canvas)",
          border: "1px solid var(--color-hairline)",
          borderRadius: "var(--radius-sm)",
        }}
      >
        <div
          style={{
            display: "flex",
            alignItems: "center",
            justifyContent: "space-between",
            gap: 12,
            padding: "18px 20px",
            borderBottom: "1px solid var(--color-hairline)",
          }}
        >
          <h2 className="t-heading-sm" style={{ margin: 0 }}>{title}</h2>
          <button
            type="button"
            onClick={onClose}
            aria-label="Đóng"
            style={{ background: "none", border: "none", cursor: "pointer", color: "var(--color-mute)", padding: 4, display: "flex" }}
          >
            <X size={20} aria-hidden="true" />
          </button>
        </div>
        <div style={{ padding: 20 }}>{children}</div>
      </div>
    </div>
  );
}

// ─── Shared field bits ──────────────────────────────────────────────────────────

function Field({
  label,
  children,
}: {
  label: string;
  children: React.ReactNode;
}) {
  return (
    <label style={{ display: "flex", flexDirection: "column", gap: 6 }}>
      <span
        style={{
          fontSize: 11,
          fontWeight: 700,
          letterSpacing: "0.06em",
          textTransform: "uppercase",
          color: "var(--color-mute)",
        }}
      >
        {label}
      </span>
      {children}
    </label>
  );
}

const FORM_GAP: React.CSSProperties = { display: "flex", flexDirection: "column", gap: 14 };

function PrimaryButton({
  children,
  disabled,
}: {
  children: React.ReactNode;
  disabled?: boolean;
}) {
  return (
    <button
      type="submit"
      disabled={disabled}
      className="btn btn-create"
      style={{
        cursor: disabled ? "not-allowed" : "pointer",
        opacity: disabled ? 0.6 : 1,
      }}
    >
      {children}
    </button>
  );
}

// ─── Create-user modal ──────────────────────────────────────────────────────────

function CreateUserModal({
  schools,
  busy,
  onClose,
  onSubmit,
}: {
  schools: SchoolModel[];
  busy: boolean;
  onClose: () => void;
  onSubmit: (payload: CreateUserPayload) => void;
}) {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [fullName, setFullName] = useState("");
  const [schoolId, setSchoolId] = useState("");
  const [studentCode, setStudentCode] = useState("");
  const [role, setRole] = useState<"student" | "admin">("student");
  const [error, setError] = useState("");

  function handleSubmit(e: FormEvent) {
    e.preventDefault();
    if (!email.trim()) return setError("Vui lòng nhập email.");
    if (!password) return setError("Vui lòng nhập mật khẩu.");
    if (!fullName.trim()) return setError("Vui lòng nhập họ và tên.");
    if (!schoolId) return setError("Vui lòng chọn trường.");
    setError("");
    onSubmit({
      email: email.trim(),
      password,
      fullName: fullName.trim(),
      schoolId,
      studentCode: studentCode.trim() || null,
      isStudent: role === "student",
      isAdmin: role === "admin",
    });
  }

  return (
    <Modal title="Thêm tài khoản mới" onClose={onClose}>
      <form onSubmit={handleSubmit} style={FORM_GAP} noValidate>
        <Field label="Email">
          <input className="text-input" type="email" value={email} onChange={(e) => setEmail(e.target.value)} placeholder="ten@email.com" autoComplete="off" />
        </Field>
        <Field label="Mật khẩu">
          <input className="text-input" type="password" value={password} onChange={(e) => setPassword(e.target.value)} placeholder="Tối thiểu 6 ký tự" autoComplete="new-password" />
        </Field>
        <Field label="Họ và tên">
          <input className="text-input" value={fullName} onChange={(e) => setFullName(e.target.value)} placeholder="Nguyễn Văn A" />
        </Field>
        <Field label="Trường">
          <select className="text-input" value={schoolId} onChange={(e) => setSchoolId(e.target.value)}>
            <option value="">— Chọn trường —</option>
            {schools.map((s) => (
              <option key={s.id} value={s.id}>{s.schoolName}</option>
            ))}
          </select>
        </Field>
        <Field label="Mã số sinh viên (tùy chọn)">
          <input className="text-input" value={studentCode} onChange={(e) => setStudentCode(e.target.value)} placeholder="VD: SE123456" />
        </Field>
        <Field label="Vai trò">
          <select className="text-input" value={role} onChange={(e) => setRole(e.target.value as "student" | "admin")}>
            <option value="student">Sinh viên</option>
            <option value="admin">Quản trị viên</option>
          </select>
        </Field>

        {error && <p className="t-caption-sm" style={{ color: "var(--color-error)", margin: 0 }}>{error}</p>}

        <div style={{ display: "flex", justifyContent: "flex-end", gap: 8, marginTop: 4 }}>
          <PrimaryButton disabled={busy}>{busy ? "Đang tạo…" : "Tạo tài khoản"}</PrimaryButton>
        </div>
      </form>
    </Modal>
  );
}

// ─── InfoRow: dùng trong modal xem chi tiết ──────────────────────────────────────

function InfoRow({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <div
      style={{
        display: "flex",
        justifyContent: "space-between",
        alignItems: "flex-start",
        padding: "10px 0",
        borderBottom: "1px solid var(--color-hairline)",
        gap: 12,
      }}
    >
      <span
        style={{
          fontSize: 12,
          fontWeight: 700,
          color: "var(--color-mute)",
          textTransform: "uppercase",
          letterSpacing: "0.05em",
          flexShrink: 0,
        }}
      >
        {label}
      </span>
      <span style={{ fontSize: "var(--fs-body-sm)", color: "var(--color-ink)", fontWeight: 600, textAlign: "right" }}>
        {value || "—"}
      </span>
    </div>
  );
}

// ─── User detail modal — xem + sửa trong cùng 1 popup ────────────────────────────

function UserDetailModal({
  user,
  schools,
  busy,
  onClose,
  onSubmit,
}: {
  user: UserSummary;
  schools: SchoolModel[];
  busy: boolean;
  onClose: () => void;
  onSubmit: (edits: { fullName: string; studentCode: string; schoolId: string }) => void;
}) {
  const [mode, setMode] = useState<"view" | "edit">("view");
  const [fullName, setFullName] = useState(user.fullName ?? "");
  const [studentCode, setStudentCode] = useState(user.studentCode ?? "");
  const [schoolId, setSchoolId] = useState(user.schoolId ?? "");
  const [formError, setFormError] = useState("");

  const schoolLabel = user.isFpt
    ? "FPT University"
    : schools.find((s) => s.id === user.schoolId)?.schoolName ?? "—";

  function handleSubmit(e: FormEvent) {
    e.preventDefault();
    if (!fullName.trim()) { setFormError("Vui lòng nhập họ và tên."); return; }
    setFormError("");
    onSubmit({ fullName, studentCode, schoolId });
    setMode("view");
  }

  function handleCancel() {
    setFullName(user.fullName ?? "");
    setStudentCode(user.studentCode ?? "");
    setSchoolId(user.schoolId ?? "");
    setFormError("");
    setMode("view");
  }

  return (
    <Modal title="Chi tiết tài khoản" onClose={onClose}>
      {mode === "view" && (
        <div style={{ display: "flex", flexDirection: "column", gap: 0 }}>
          {user.photoStudentCardUrl && (
            <div style={{ marginBottom: 16, textAlign: "center" }}>
              {/* eslint-disable-next-line @next/next/no-img-element */}
              <img
                src={user.photoStudentCardUrl}
                alt="Ảnh thẻ sinh viên"
                style={{
                  width: 120,
                  height: 80,
                  objectFit: "cover",
                  borderRadius: "var(--radius-sm)",
                  border: "1px solid var(--color-hairline)",
                  display: "inline-block",
                }}
              />
              <p
                style={{
                  fontSize: 11,
                  color: "var(--color-mute)",
                  margin: "6px 0 0",
                  textTransform: "uppercase",
                  letterSpacing: "0.05em",
                }}
              >
                Ảnh thẻ sinh viên
              </p>
            </div>
          )}

          <InfoRow label="Họ và tên" value={user.fullName} />
          <InfoRow label="Email" value={user.email} />
          <InfoRow label="MSSV" value={user.studentCode} />
          <InfoRow label="Trường" value={schoolLabel} />
          <InfoRow
            label="Vai trò"
            value={
              <span
                style={{
                  fontSize: 12,
                  fontWeight: 700,
                  padding: "2px 8px",
                  borderRadius: "var(--radius-sm)",
                  background: user.isAdmin ? "rgba(118,185,0,0.1)" : "var(--color-surface-soft)",
                  color: user.isAdmin ? "var(--color-primary)" : "var(--color-mute)",
                  border: `1px solid ${user.isAdmin ? "var(--color-primary)" : "var(--color-hairline)"}`,
                }}
              >
                {user.isAdmin ? "Quản trị viên" : "Người dùng"}
              </span>
            }
          />
          <InfoRow
            label="Trạng thái"
            value={
              <span
                style={{
                  fontSize: 12,
                  fontWeight: 700,
                  padding: "2px 8px",
                  borderRadius: "var(--radius-sm)",
                  background: user.isApproved ? "rgba(118,185,0,0.1)" : "var(--color-surface-soft)",
                  color: user.isApproved ? "var(--color-primary)" : "var(--color-stone)",
                  border: `1px solid ${user.isApproved ? "var(--color-primary)" : "var(--color-hairline-strong)"}`,
                }}
              >
                {user.isApproved ? "Đã duyệt" : "Chờ duyệt"}
              </span>
            }
          />
          <InfoRow label="Loại tài khoản" value={user.isFpt ? "Sinh viên FPT" : "Trường khác"} />

          <div style={{ display: "flex", justifyContent: "flex-end", gap: 8, marginTop: 20 }}>
            <button
              type="button"
              onClick={onClose}
              className="t-body-sm"
              style={{
                fontWeight: 700,
                background: "none",
                border: "1px solid var(--color-hairline-strong)",
                borderRadius: "var(--radius-sm)",
                padding: "8px 20px",
                cursor: "pointer",
                color: "var(--color-ink)",
              }}
            >
              Đóng
            </button>
          </div>
        </div>
      )}

      {mode === "edit" && (
        <form onSubmit={handleSubmit} style={{ display: "flex", flexDirection: "column", gap: 14 }} noValidate>
          <Field label="Email">
            <input
              className="text-input"
              value={user.email ?? ""}
              disabled
              style={{ opacity: 0.6, cursor: "not-allowed" }}
            />
          </Field>

          <Field label="Họ và tên *">
            <input
              className="text-input"
              value={fullName}
              onChange={(e) => setFullName(e.target.value)}
              placeholder="Nguyễn Văn A"
            />
          </Field>

          <Field label="Mã số sinh viên">
            <input
              className="text-input"
              value={studentCode}
              onChange={(e) => setStudentCode(e.target.value)}
              placeholder="VD: SE123456"
            />
          </Field>

          <Field label="Trường">
            <select
              className="text-input"
              value={schoolId}
              onChange={(e) => setSchoolId(e.target.value)}
            >
              <option value="">— Chọn trường —</option>
              {schools.map((s) => (
                <option key={s.id} value={s.id}>{s.schoolName}</option>
              ))}
            </select>
          </Field>

          {formError && (
            <p className="t-caption-sm" style={{ color: "var(--color-error)", margin: 0 }}>
              {formError}
            </p>
          )}

          <div style={{ display: "flex", justifyContent: "flex-end", gap: 8, marginTop: 4 }}>
            <button
              type="button"
              onClick={handleCancel}
              className="t-body-sm"
              style={{
                fontWeight: 700,
                background: "none",
                border: "1px solid var(--color-hairline-strong)",
                borderRadius: "var(--radius-sm)",
                padding: "8px 20px",
                cursor: "pointer",
                color: "var(--color-ink)",
              }}
            >
              Hủy
            </button>
            <button
              type="submit"
              disabled={busy}
              className="btn btn-update"
              style={{
                cursor: busy ? "not-allowed" : "pointer",
                opacity: busy ? 0.6 : 1,
              }}
            >
              {busy ? "Đang lưu…" : "Lưu thay đổi"}
            </button>
          </div>
        </form>
      )}
    </Modal>
  );
}
