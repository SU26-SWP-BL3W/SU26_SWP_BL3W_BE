"use client";

import { useEffect } from "react";
import { useQuery } from "@tanstack/react-query";
import { templatesApi } from "../api/templates";

const cellHead: React.CSSProperties = {
  padding: "8px 8px",
  fontSize: 11,
  fontWeight: 700,
  letterSpacing: "0.05em",
  textTransform: "uppercase",
  color: "var(--color-mute)",
  borderBottom: "1px solid var(--color-hairline-strong)",
  whiteSpace: "nowrap",
};
const cell: React.CSSProperties = {
  padding: "8px 8px",
  borderBottom: "1px solid var(--color-hairline)",
  verticalAlign: "top",
};

/** Popup chỉ-đọc hiển thị chi tiết bộ tiêu chí (template) đã chọn cho hạng mục:
 *  từng tiêu chí kèm trọng số và điểm tối đa. Tải qua GET /Templates/{id}. */
export function TemplateCriteriaModal({
  templateId,
  onClose,
  onDerive,
  deriveBusy,
}: {
  templateId: string;
  onClose: () => void;
  /** Khi có: hiện nút "Dựa trên bộ này tạo mới" ở chân popup. */
  onDerive?: () => void;
  /** Đang tạo bộ mới từ bộ này — khoá nút + hiện trạng thái. */
  deriveBusy?: boolean;
}) {
  const { data, isLoading, isError } = useQuery({
    queryKey: ["templateDetail", templateId],
    queryFn: () => templatesApi.getById(templateId),
    staleTime: 5 * 60_000,
  });

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
        zIndex: 120,
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
        aria-label="Chi tiết bộ tiêu chí"
        onClick={(e) => e.stopPropagation()}
        style={{
          width: "100%",
          maxWidth: 560,
          maxHeight: "85vh",
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
            padding: "16px 20px",
            borderBottom: "1px solid var(--color-hairline)",
          }}
        >
          <h3 className="t-heading-sm" style={{ margin: 0 }}>
            {data?.templateName || "Bộ tiêu chí chấm điểm"}
          </h3>
          <button
            type="button"
            onClick={onClose}
            aria-label="Đóng"
            style={{ background: "none", border: "none", cursor: "pointer", color: "var(--color-mute)", fontSize: 22, lineHeight: 1, padding: 0 }}
          >
            ×
          </button>
        </div>

        <div style={{ padding: 20 }}>
          {isLoading ? (
            <p className="t-body-sm" style={{ color: "var(--color-mute)", margin: 0 }}>Đang tải bộ tiêu chí…</p>
          ) : isError || !data ? (
            <p className="t-body-sm" style={{ color: "var(--color-error)", margin: 0 }}>Không tải được bộ tiêu chí.</p>
          ) : (
            <>
              {data.description && (
                <p className="t-body-sm" style={{ color: "var(--color-mute)", margin: "0 0 12px" }}>{data.description}</p>
              )}
              {data.criterias.length === 0 ? (
                <p className="t-body-sm" style={{ color: "var(--color-mute)", margin: 0 }}>Bộ tiêu chí này chưa có tiêu chí nào.</p>
              ) : (
                <table style={{ width: "100%", borderCollapse: "collapse" }}>
                  <thead>
                    <tr>
                      <th style={{ ...cellHead, textAlign: "left" }}>Tiêu chí</th>
                      <th style={{ ...cellHead, textAlign: "right" }}>Trọng số</th>
                      <th style={{ ...cellHead, textAlign: "right" }}>Điểm tối đa</th>
                    </tr>
                  </thead>
                  <tbody>
                    {data.criterias.map((c) => (
                      <tr key={c.criteriaId}>
                        <td style={cell}>
                          <span className="t-body-sm" style={{ color: "var(--color-ink)", fontWeight: 600 }}>{c.criteriaName}</span>
                          {c.description && (
                            <p className="t-caption-sm" style={{ color: "var(--color-mute)", margin: "2px 0 0" }}>{c.description}</p>
                          )}
                        </td>
                        <td className="t-body-sm" style={{ ...cell, textAlign: "right", whiteSpace: "nowrap" }}>{c.weight}</td>
                        <td className="t-body-sm" style={{ ...cell, textAlign: "right", whiteSpace: "nowrap" }}>{c.maxScore}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              )}
            </>
          )}
        </div>

        {onDerive && (
          <div
            role="note"
            style={{
              margin: "0 20px 14px",
              padding: "10px 12px",
              color: "var(--color-stone)",
              background: "var(--color-surface-dim)",
              border: "1px solid var(--color-hairline)",
              borderLeft: "3px solid var(--color-primary)",
              borderRadius: "var(--radius-sm)",
            }}
          >
            <p className="t-caption-sm" style={{ margin: 0 }}>
              Chỉnh sửa bộ tiêu chí sẽ ảnh hưởng đến các sự kiện đang sử dụng.
            </p>
          </div>
        )}

        <div style={{ display: "flex", justifyContent: "flex-end", gap: 8, padding: "14px 20px", borderTop: "1px solid var(--color-hairline)" }}>
          <button type="button" onClick={onClose} className="btn btn-outline btn-sm" style={{ cursor: "pointer" }}>
            Đóng
          </button>
          {onDerive && (
            <button
              type="button"
              onClick={onDerive}
              disabled={deriveBusy}
              className="btn btn-update btn-sm"
              style={{ cursor: deriveBusy ? "not-allowed" : "pointer", opacity: deriveBusy ? 0.6 : 1, whiteSpace: "nowrap" }}
            >
              {deriveBusy ? "Đang mở…" : "Chỉnh sửa tiêu chí"}
            </button>
          )}
        </div>
      </div>
    </div>
  );
}
