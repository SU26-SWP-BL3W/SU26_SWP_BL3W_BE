"use client";

import { useEffect, useRef, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { templatesApi, criteriasApi, type TemplateCriteria, type TemplateSummary } from "../api/templates";
import {
  calculateMaxScores,
  inferScoreScale,
  resolveScoreScale,
  type ScoreScaleType,
} from "@/lib/scoringScale";

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
  padding: "6px 8px",
  borderBottom: "1px solid var(--color-hairline)",
  verticalAlign: "middle",
};
const numInput: React.CSSProperties = { width: 80, padding: "4px 8px" };

/** Một hàng tiêu chí — maxScore được tính tự động từ trọng số và thang điểm tổng. */
function CriteriaRow({
  c,
  busy,
  onSave,
  onRemove,
}: {
  c: TemplateCriteria;
  busy: boolean;
  onSave: (weight: number) => void;
  onRemove: () => void;
}) {
  const [weight, setWeight] = useState(String(c.weight));

  const commit = () => {
    const w = Number(weight);
    if (Number.isNaN(w)) return;
    if (w !== c.weight) onSave(w);
  };

  return (
    <tr>
      <td style={cell}>
        <span className="t-body-sm" style={{ color: "var(--color-ink)", fontWeight: 600 }}>{c.criteriaName}</span>
        {c.description && <p className="t-caption-sm" style={{ color: "var(--color-mute)", margin: "2px 0 0" }}>{c.description}</p>}
      </td>
      <td style={{ ...cell, textAlign: "right" }}>
        <input className="text-input" style={numInput} type="number" min="0.01" max="100" step="0.01" value={weight}
          onChange={(e) => setWeight(e.target.value)} onBlur={commit} disabled={busy} />
      </td>
      <td className="t-body-sm" style={{ ...cell, textAlign: "center", color: "var(--color-mute)" }}>{c.maxScore}</td>
      <td style={{ ...cell, textAlign: "right" }}>
        <button type="button" onClick={onRemove} disabled={busy} aria-label={`Gỡ ${c.criteriaName}`}
          className="btn btn-delete btn-sm"
          style={{ cursor: busy ? "not-allowed" : "pointer" }}>
          Gỡ
        </button>
      </td>
    </tr>
  );
}

/**
 * Tạo MỚI một bộ tiêu chí ngay trong luồng tạo sự kiện (Option B), hoặc sửa tiếp
 * bộ vừa tạo. Cho đặt/đổi tên template, thêm tiêu chí từ kho dùng chung, chỉnh
 * trọng số/điểm, gỡ tiêu chí. Mọi thao tác gọi API ngay.
 *
 * - Chưa có `initialTemplateId` → chế độ TẠO: nhập tên rồi bấm "Tạo bộ tiêu chí".
 * - Sau khi tạo (hoặc mở sẵn một template) → chế độ SỬA: sửa tên + tiêu chí.
 */
export function TemplateBuilderModal({
  initialTemplateId,
  initialName,
  templates = [],
  baseTemplateId,
  onTemplateReady,
  onClose,
}: {
  initialTemplateId?: string;
  initialName?: string;
  /** Danh sách template để chọn làm nền tảng khi tạo mới. */
  templates?: TemplateSummary[];
  /** Mặc định "dựa trên bộ tiêu chí" (thường là bộ đang chọn/xem). */
  baseTemplateId?: string;
  /** Gọi khi template đã sẵn sàng (vừa tạo) để form gắn vào hạng mục. */
  onTemplateReady: (id: string) => void;
  onClose: () => void;
}) {
  const qc = useQueryClient();
  const [templateId, setTemplateId] = useState<string | null>(initialTemplateId ?? null);
  const [name, setName] = useState(initialName ?? "");
  const [baseId, setBaseId] = useState(baseTemplateId ?? "");
  const [error, setError] = useState<string | null>(null);
  const [scaleType, setScaleType] = useState<ScoreScaleType>("10");
  const [customScaleValue, setCustomScaleValue] = useState("10");
  const initializedTemplateRef = useRef<string | null>(null);

  const [addId, setAddId] = useState("");
  const [addWeight, setAddWeight] = useState("1");

  const detailQ = useQuery({
    queryKey: ["templateDetail", templateId],
    queryFn: () => templatesApi.getById(templateId as string),
    enabled: !!templateId,
    staleTime: 0,
  });
  const poolQ = useQuery({
    queryKey: ["criteriaPool"],
    queryFn: () => criteriasApi.list(),
    staleTime: 5 * 60_000,
  });

  const refreshTemplates = () => qc.invalidateQueries({ queryKey: ["templates"] });
  const invalidateDetail = () => qc.invalidateQueries({ queryKey: ["templateDetail", templateId] });

  const createM = useMutation({
    mutationFn: async (v: { name: string; baseId: string }) => {
      const id = await templatesApi.create(v.name, "");
      // Dùng bộ tiêu chí cũ làm nền: copy cấu hình (tiêu chí + trọng số + điểm).
      if (v.baseId) {
        const base = await templatesApi.getById(v.baseId);
        const calculated = calculateMaxScores(base.criterias, getScale());
        for (const c of calculated) {
          await templatesApi.addCriteria(id, { criteriaId: c.criteriaId, weight: c.weight, maxScore: c.maxScore });
        }
      }
      return id;
    },
    onSuccess: (id) => {
      // Bộ vừa tạo tiếp tục dùng thang người dùng đã chọn, kể cả khi chưa có
      // tiêu chí để có thể suy ngược thang điểm từ maxScore.
      initializedTemplateRef.current = id;
      setTemplateId(id);
      onTemplateReady(id);
      refreshTemplates();
    },
    onError: () => setError("Không tạo được bộ tiêu chí — tên có thể đã trùng. Đổi tên khác rồi thử lại."),
  });
  const renameM = useMutation({
    mutationFn: (n: string) => templatesApi.updateInfo(templateId as string, n, detailQ.data?.description ?? ""),
    onSuccess: () => { refreshTemplates(); invalidateDetail(); },
    onError: () => setError("Không đổi được tên — tên có thể đã trùng với bộ khác."),
  });
  const getScale = (type = scaleType, custom = customScaleValue) =>
    resolveScoreScale(type, Number(custom));

  const persistCriteria = async (items: TemplateCriteria[], scale = getScale()) => {
    const calculated = calculateMaxScores(items, scale);
    await Promise.all(calculated.map((item) =>
      templatesApi.updateCriteria(templateId as string, item.criteriaId, {
        weight: item.weight,
        maxScore: item.maxScore,
      })
    ));
  };

  const updateM = useMutation({
    mutationFn: (v: { criteriaId: string; weight: number }) =>
      persistCriteria((detailQ.data?.criterias ?? []).map((item) =>
        item.criteriaId === v.criteriaId ? { ...item, weight: v.weight } : item
      )),
    onSuccess: invalidateDetail,
    onError: () => setError("Không lưu được trọng số."),
  });
  const removeM = useMutation({
    mutationFn: async (criteriaId: string) => {
      await templatesApi.removeCriteria(templateId as string, criteriaId);
      await persistCriteria((detailQ.data?.criterias ?? []).filter((item) => item.criteriaId !== criteriaId));
    },
    onSuccess: invalidateDetail,
    onError: () => setError("Không gỡ được tiêu chí."),
  });
  const addM = useMutation({
    mutationFn: async (v: { criteriaId: string; weight: number }) => {
      const poolItem = (poolQ.data ?? []).find((item) => item.id === v.criteriaId);
      const next = [
        ...(detailQ.data?.criterias ?? []),
        {
          criteriaId: v.criteriaId,
          criteriaName: poolItem?.criteriaName ?? "",
          description: poolItem?.description ?? null,
          isActive: poolItem?.isActive ?? true,
          weight: v.weight,
          maxScore: 0,
        },
      ];
      const calculated = calculateMaxScores(next, getScale());
      const added = calculated[calculated.length - 1];
      await templatesApi.addCriteria(templateId as string, {
        criteriaId: added.criteriaId,
        weight: added.weight,
        maxScore: added.maxScore,
      });
      await persistCriteria(calculated.slice(0, -1));
    },
    onSuccess: () => { setAddId(""); setAddWeight("1"); invalidateDetail(); },
    onError: () => setError("Không thêm được tiêu chí."),
  });
  const scaleM = useMutation({
    mutationFn: (v: { type: ScoreScaleType; custom: string }) => {
      const scale = resolveScoreScale(v.type, Number(v.custom));
      if (!Number.isFinite(scale) || scale <= 0) throw new Error("invalid-scale");
      return persistCriteria(detailQ.data?.criterias ?? [], scale);
    },
    onSuccess: invalidateDetail,
    onError: () => setError("Không lưu được thang điểm. Vui lòng nhập một số dương."),
  });

  const busy = createM.isPending || renameM.isPending || updateM.isPending || removeM.isPending || addM.isPending || scaleM.isPending;

  const detail = detailQ.data;
  useEffect(() => {
    if (!templateId || !detail || initializedTemplateRef.current === templateId) return;
    const inferred = inferScoreScale(detail.criterias);
    setScaleType(inferred.scaleType);
    setCustomScaleValue(String(inferred.customScaleValue));
    initializedTemplateRef.current = templateId;
  }, [detail, templateId]);

  const inTemplate = new Set(detail?.criterias.map((c) => c.criteriaId) ?? []);
  const available = (poolQ.data ?? []).filter((p) => p.isActive && !inTemplate.has(p.id));
  const totalWeight = (detail?.criterias ?? []).reduce((s, c) => s + (c.weight ?? 0), 0);
  const displayedCriteria = calculateMaxScores(detail?.criterias ?? [], getScale());

  const handleCreate = () => {
    setError(null);
    if (!name.trim()) { setError("Nhập tên bộ tiêu chí."); return; }
    const scale = getScale();
    if (!Number.isFinite(scale) || scale <= 0) {
      setError("Thang điểm tùy chỉnh phải là số dương.");
      return;
    }
    createM.mutate({ name: name.trim(), baseId });
  };
  const handleRename = () => {
    if (!templateId) return;
    const n = name.trim();
    if (!n || n === detail?.templateName) return;
    setError(null);
    renameM.mutate(n);
  };
  const handleAdd = () => {
    setError(null);
    const w = Number(addWeight);
    if (!addId) { setError("Chọn một tiêu chí để thêm."); return; }
    if (Number.isNaN(w) || w <= 0 || w > 100) { setError("Trọng số phải lớn hơn 0 và không vượt quá 100."); return; }
    if (totalWeight + w > 100) { setError(`Tổng trọng số sẽ là ${totalWeight + w}, vượt quá 100.`); return; }
    addM.mutate({ criteriaId: addId, weight: w });
  };
  const handleDone = () => {
    if (templateId && detail?.criterias.length && Math.abs(totalWeight - 100) > 0.000_001) {
      setError("Tổng trọng số của bộ tiêu chí phải bằng chính xác 100 trước khi hoàn tất.");
      return;
    }
    const scale = getScale();
    if (scaleType === "custom" && (!Number.isFinite(scale) || scale <= 0)) {
      setError("Thang điểm tùy chỉnh phải là số dương.");
      return;
    }
    onClose();
  };

  useEffect(() => {
    const onEsc = (e: KeyboardEvent) => {
      if (e.key !== "Escape") return;
      if (templateId && detail?.criterias.length && Math.abs(totalWeight - 100) > 0.000_001) {
        setError("Tổng trọng số của bộ tiêu chí phải bằng chính xác 100 trước khi hoàn tất.");
        return;
      }
      const scale = resolveScoreScale(scaleType, Number(customScaleValue));
      if (scaleType === "custom" && (!Number.isFinite(scale) || scale <= 0)) {
        setError("Thang điểm tùy chỉnh phải là số dương.");
        return;
      }
      onClose();
    };
    document.addEventListener("keydown", onEsc);
    document.body.style.overflow = "hidden";
    return () => { document.removeEventListener("keydown", onEsc); document.body.style.overflow = ""; };
  }, [customScaleValue, detail?.criterias.length, onClose, scaleType, templateId, totalWeight]);

  return (
    <div
      onClick={templateId ? handleDone : onClose}
      style={{ position: "fixed", inset: 0, zIndex: 120, display: "flex", alignItems: "center", justifyContent: "center", padding: 16, background: "rgba(0,0,0,0.6)" }}
    >
      <div
        role="dialog"
        aria-modal="true"
        aria-label="Bộ tiêu chí chấm điểm"
        onClick={(e) => e.stopPropagation()}
        style={{ width: "100%", maxWidth: 640, maxHeight: "88vh", overflowY: "auto", background: "var(--color-canvas)", border: "1px solid var(--color-hairline)", borderRadius: "var(--radius-sm)" }}
      >
        <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", gap: 12, padding: "16px 20px", borderBottom: "1px solid var(--color-hairline)" }}>
          <h3 className="t-heading-sm" style={{ margin: 0 }}>{templateId ? "Sửa bộ tiêu chí" : "Tạo bộ tiêu chí mới"}</h3>
          <button type="button" onClick={templateId ? handleDone : onClose} aria-label="Đóng" style={{ background: "none", border: "none", cursor: "pointer", color: "var(--color-mute)", fontSize: 22, lineHeight: 1, padding: 0 }}>×</button>
        </div>

        <div style={{ padding: 20, display: "flex", flexDirection: "column", gap: 14 }}>
          {error && <p className="t-caption-sm" style={{ color: "var(--color-error)", margin: 0 }}>{error}</p>}

          {/* Dựa trên bộ tiêu chí có sẵn — copy làm nền (chỉ khi đang tạo mới). */}
          {!templateId && (
            <div style={{ display: "flex", flexDirection: "column", gap: 6 }}>
              <span className="t-caption-xs" style={{ color: "var(--color-mute)" }}>Dựa trên bộ tiêu chí có sẵn (tùy chọn)</span>
              <select className="text-input" value={baseId} onChange={(e) => setBaseId(e.target.value)} disabled={createM.isPending}>
                <option value="">— Tạo trống —</option>
                {templates.map((t) => (<option key={t.id} value={t.id}>{t.templateName}</option>))}
              </select>
              <span className="t-caption-sm" style={{ color: "var(--color-mute)" }}>Chọn một bộ để copy toàn bộ tiêu chí + trọng số làm nền, rồi chỉnh lại.</span>
            </div>
          )}

          {/* Tên template — nhập khi tạo, đổi tên khi sửa (lưu lúc rời ô). */}
          <div style={{ display: "flex", flexDirection: "column", gap: 6 }}>
            <span className="t-caption-xs" style={{ color: "var(--color-mute)" }}>Tên bộ tiêu chí</span>
            <div style={{ display: "flex", gap: 8 }}>
              <input
                className="text-input"
                style={{ flex: 1, minWidth: 0 }}
                value={name}
                placeholder="VD: Rubric Hackathon 2026"
                disabled={busy}
                onChange={(e) => setName(e.target.value)}
                onBlur={templateId ? handleRename : undefined}
              />
              {!templateId && (
                <button type="button" className="btn btn-create btn-sm shrink-0" onClick={handleCreate} disabled={busy || !name.trim()}
                  style={{ cursor: busy || !name.trim() ? "not-allowed" : "pointer", opacity: busy || !name.trim() ? 0.6 : 1, whiteSpace: "nowrap" }}>
                  {createM.isPending ? "Đang tạo…" : "Tạo bộ tiêu chí"}
                </button>
              )}
            </div>
            {!templateId && <span className="t-caption-sm" style={{ color: "var(--color-mute)" }}>Đặt tên rồi bấm &ldquo;Tạo bộ tiêu chí&rdquo; để thêm các tiêu chí.</span>}
          </div>

          <div style={{ padding: 12, border: "1px solid var(--color-hairline)", display: "flex", flexDirection: "column", gap: 8 }}>
            <span className="t-caption-xs" style={{ color: "var(--color-mute)", fontWeight: 700, textTransform: "uppercase", letterSpacing: "0.05em" }}>Thang điểm tổng</span>
            <div style={{ display: "flex", alignItems: "center", gap: 8, flexWrap: "nowrap", width: "100%" }}>
              <select
                className="text-input"
                style={{ flex: "1 1 auto", minWidth: 0 }}
                value={scaleType}
                disabled={busy}
                onChange={(e) => {
                  const type = e.target.value as ScoreScaleType;
                  setScaleType(type);
                  setError(null);
                  if (templateId && type !== "custom") scaleM.mutate({ type, custom: customScaleValue });
                }}
              >
                <option value="10">Thang 10</option>
                <option value="100">Thang 100</option>
                <option value="custom">Tùy chỉnh</option>
              </select>
              {scaleType === "custom" && (
                <input
                  className="text-input"
                  style={{ flex: "0 0 140px", width: 140, minWidth: 0 }}
                  type="number"
                  min="0.01"
                  step="0.01"
                  value={customScaleValue}
                  disabled={busy}
                  aria-label="Tổng điểm tối đa tùy chỉnh"
                  onChange={(e) => setCustomScaleValue(e.target.value)}
                  onBlur={() => {
                    const value = Number(customScaleValue);
                    if (!Number.isFinite(value) || value <= 0) {
                      setError("Thang điểm tùy chỉnh phải là số dương.");
                      return;
                    }
                    setError(null);
                    if (templateId) scaleM.mutate({ type: "custom", custom: customScaleValue });
                  }}
                />
              )}
            </div>
            <span className="t-caption-sm" style={{ color: "var(--color-mute)" }}>Điểm tối đa của từng tiêu chí được tính tự động từ trọng số và được lưu với 2 chữ số thập phân.</span>
          </div>

          {/* Danh sách tiêu chí — chỉ hiện sau khi đã có template. */}
          {templateId && (
            detailQ.isLoading ? (
              <p className="t-body-sm" style={{ color: "var(--color-mute)", margin: 0 }}>Đang tải…</p>
            ) : detailQ.isError || !detail ? (
              <p className="t-body-sm" style={{ color: "var(--color-error)", margin: 0 }}>Không tải được bộ tiêu chí.</p>
            ) : (
              <>
                <table style={{ width: "100%", borderCollapse: "collapse" }}>
                  <thead>
                    <tr>
                      <th style={{ ...cellHead, textAlign: "left" }}>Tiêu chí</th>
                      <th style={{ ...cellHead, textAlign: "right" }}>Trọng số</th>
                      <th style={{ ...cellHead, textAlign: "right" }}>Điểm tối đa</th>
                      <th style={{ ...cellHead, textAlign: "right" }}></th>
                    </tr>
                  </thead>
                  <tbody>
                    {detail.criterias.length === 0 ? (
                      <tr><td colSpan={4} style={{ ...cell, color: "var(--color-mute)" }} className="t-body-sm">Chưa có tiêu chí nào — thêm từ kho bên dưới.</td></tr>
                    ) : (
                      displayedCriteria.map((c) => (
                        <CriteriaRow
                          key={c.criteriaId}
                          c={c}
                          busy={busy}
                          onSave={(weight) => {
                            if (!Number.isFinite(weight) || weight <= 0 || weight > 100) {
                              setError("Trọng số phải lớn hơn 0 và không vượt quá 100.");
                              return;
                            }
                            const others = detail.criterias
                              .filter((x) => x.criteriaId !== c.criteriaId)
                              .reduce((s, x) => s + (x.weight ?? 0), 0);
                            if (others + weight > 100) { setError(`Tổng trọng số sẽ là ${others + weight}, vượt quá 100.`); return; }
                            setError(null);
                            updateM.mutate({ criteriaId: c.criteriaId, weight });
                          }}
                          onRemove={() => { setError(null); removeM.mutate(c.criteriaId); }}
                        />
                      ))
                    )}
                  </tbody>
                </table>

                <div style={{ paddingTop: 12, borderTop: "1px solid var(--color-hairline)", display: "flex", flexDirection: "column", gap: 8 }}>
                  <div style={{ display: "flex", justifyContent: "space-between", alignItems: "baseline", gap: 8, flexWrap: "wrap" }}>
                    <p className="t-caption-xs" style={{ margin: 0, color: "var(--color-mute)", fontWeight: 700, textTransform: "uppercase", letterSpacing: "0.05em" }}>Thêm tiêu chí từ kho</p>
                    <span className="t-caption-sm" style={{ color: totalWeight === 100 ? "var(--color-primary)" : "var(--color-stone)", fontWeight: 700 }}>
                      Tổng trọng số: {totalWeight} / 100
                      {totalWeight !== 100 && (
                        <span style={{ fontWeight: 400, marginLeft: 4 }}>
                          ({totalWeight < 100 ? `thiếu ${100 - totalWeight}` : `thừa ${totalWeight - 100}`})
                        </span>
                      )}
                    </span>
                  </div>
                  <div style={{ display: "flex", gap: 8, flexWrap: "wrap", alignItems: "center" }}>
                    <select className="text-input" style={{ flex: 1, minWidth: 180 }} value={addId} onChange={(e) => setAddId(e.target.value)} disabled={busy || poolQ.isLoading}>
                      <option value="">{poolQ.isLoading ? "Đang tải kho…" : available.length === 0 ? "Kho đã hết tiêu chí để thêm" : "— Chọn tiêu chí —"}</option>
                      {available.map((p) => (<option key={p.id} value={p.id}>{p.criteriaName}</option>))}
                    </select>
                    <input className="text-input" style={{ ...numInput, marginLeft: 4 }} type="number" min="0.01" max="100" step="0.01" value={addWeight} onChange={(e) => setAddWeight(e.target.value)} disabled={busy} aria-label="Trọng số (%)" placeholder="TS %" />
                    <button type="button" className="btn btn-create btn-sm" onClick={handleAdd} disabled={busy || !addId} style={{ cursor: busy || !addId ? "not-allowed" : "pointer", opacity: busy || !addId ? 0.6 : 1 }}>
                      Thêm
                    </button>
                  </div>
                  <span className="t-caption-sm" style={{ color: "var(--color-mute)" }}>Mỗi trọng số phải lớn hơn 0 và tổng trọng số cả bộ phải bằng 100.</span>
                </div>
              </>
            )
          )}

          <div style={{ display: "flex", justifyContent: "flex-end" }}>
            <button type="button" onClick={templateId ? handleDone : onClose} className="btn btn-outline btn-sm" style={{ cursor: "pointer" }}>
              {templateId ? "Xong" : "Đóng"}
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}
