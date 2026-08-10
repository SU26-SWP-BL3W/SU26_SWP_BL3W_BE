"use client";

import {
  useEffect,
  useRef,
  useState,
  type ChangeEvent,
  type ReactNode,
} from "react";
import { useRouter } from "next/navigation";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { eventsApi, type CreateEventPayload } from "../api/events";
import { templatesApi, type TemplateSummary } from "../api/templates";
import { TemplateCriteriaModal } from "./TemplateCriteriaModal";
import { TemplateBuilderModal } from "./TemplateBuilderModal";
import { roundsApi, tracksApi } from "../api/roundTrack";
import { usersApi, storageApi, type UserSummary } from "@/services/api";
import { useNotify } from "@/components/NotificationProvider";
import { getErrorMessage } from "@/lib/apiError";
import { DateTimePicker } from "@/components/DateTimePicker";
import { eventKeys, useEventModel, useEventRounds, useEventTracks } from "../hooks/useEvents";

// ─── Form state types (mirror the create-event payload) ───────────────────────

interface InvitedUser {
  id?: string;
  email: string;
  fullName: string;
}

/**
 * Submission requirements captured as discrete checkboxes (multi-select).
 * Serialized into the backend's single `submissionRuleDescription` string at
 * submit time — see `serializeSubmissionRequirements`.
 */
interface SubmissionRequirements {
  /** Đường dẫn repository dự án */
  repo: boolean;
  /** Đường dẫn demo */
  demo: boolean;
  /** Đường dẫn báo cáo/slide */
  reportSlide: boolean;
  /** "Khác" — admin tự điền thêm một yêu cầu */
  otherEnabled: boolean;
  /** Nội dung yêu cầu tùy chỉnh khi "Khác" được tích */
  otherText: string;
}

interface TrackForm {
  /** Backend id when editing an existing track; undefined for a new one. */
  id?: string;
  /** Stable client id — key React & trạng thái mở/đóng accordion hạng mục. */
  uid: string;
  trackName: string;
  description: string;
  templateId: string;
  /** Submission requirement checkboxes (serialized to submissionRuleDescription). */
  submissionRequirements: SubmissionRequirements;
  /**
   * Raw saved requirement string loaded in edit mode, shown read-only as a
   * "đã lưu trước đó" reference. `undefined` in create mode (no reference shown).
   */
  savedSubmissionRule?: string;
  // ── Hai khoảng thời gian thực của hạng mục (BE quản lý đúng 2 phase). ──
  /** Nộp bài: mở → hạn nộp — BE: startDate/endDate. */
  startDate: string;
  endDate: string;
  /** Chấm điểm: mở → đóng — BE: scoringStartDate/scoringEndDate. */
  scoringStartDate: string;
  scoringEndDate: string;
}

/** Fixed URL-based submission options shown as checkboxes, in display order. */
const SUBMISSION_URL_OPTIONS = [
  { key: "repo", label: "Link github repository dự án" },
  { key: "demo", label: "Link demo" },
  { key: "reportSlide", label: "Link báo cáo/slide" },
] as const;

const emptySubmissionRequirements = (): SubmissionRequirements => ({
  repo: false,
  demo: false,
  reportSlide: false,
  otherEnabled: false,
  otherText: "",
});

/** Join the checked requirements into the newline-separated backend string. */
function serializeSubmissionRequirements(req: SubmissionRequirements): string {
  const parts: string[] = [];
  for (const opt of SUBMISSION_URL_OPTIONS) {
    if (req[opt.key]) parts.push(opt.label);
  }
  if (req.otherEnabled && req.otherText.trim()) {
    parts.push(`Khác: ${req.otherText.trim()}`);
  }
  return parts.join("\n");
}

/**
 * Reverse of `serializeSubmissionRequirements` — turns the stored backend string
 * back into checkbox state so the edit form shows what was selected before.
 * Lines matching a known URL label tick that box; a "Khác: …" line (or any
 * unrecognized line) fills the custom "Khác" field. Empty input → all unchecked.
 */
function parseSubmissionRequirements(stored: string | null | undefined): SubmissionRequirements {
  const req = emptySubmissionRequirements();
  if (!stored) return req;

  const otherLines: string[] = [];
  for (const raw of stored.split("\n")) {
    const line = raw.trim();
    if (!line) continue;

    const matched = SUBMISSION_URL_OPTIONS.find((opt) => opt.label === line);
    if (matched) {
      req[matched.key] = true;
    } else if (/^khác\s*:/i.test(line)) {
      otherLines.push(line.replace(/^khác\s*:/i, "").trim());
    } else {
      otherLines.push(line);
    }
  }

  const otherText = otherLines.filter(Boolean).join("\n");
  if (otherText) {
    req.otherEnabled = true;
    req.otherText = otherText;
  }
  return req;
}

interface RoundForm {
  /** Backend id when editing an existing round; undefined for a new one. */
  id?: string;
  /** Stable client id — dùng cho key React & trạng thái mở/đóng accordion (bền qua thêm/xóa). */
  uid: string;
  roundName: string;
  roundNumber: string;
  startDate: string;
  endDate: string;
  advancementRule: string;
  tracks: TrackForm[];
}

let roundUidSeq = 0;
const nextRoundUid = () => `r${++roundUidSeq}`;

interface EventForm {
  eventName: string;
  maxTeams: string;
  season: string;
  year: string;
  startDate: string;
  endDate: string;
  registrationStartDate: string;
  registrationEndDate: string;
  description: string;
  status: boolean;
  photoEventUrl: string | null;
  rounds: RoundForm[];
}

// ─── Factories ────────────────────────────────────────────────────────────────

let trackUidSeq = 0;
const nextTrackUid = () => `t${++trackUidSeq}`;

const emptyTrack = (): TrackForm => ({
  uid: nextTrackUid(),
  trackName: "",
  description: "",
  templateId: "",
  submissionRequirements: emptySubmissionRequirements(),
  startDate: "",
  endDate: "",
  scoringStartDate: "",
  scoringEndDate: "",
});

const emptyRound = (): RoundForm => ({
  uid: nextRoundUid(),
  roundName: "",
  roundNumber: "",
  startDate: "",
  endDate: "",
  advancementRule: "",
  tracks: [emptyTrack()],
});

const emptyEvent = (): EventForm => ({
  eventName: "",
  maxTeams: "",
  season: "",
  year: "",
  startDate: "",
  endDate: "",
  registrationStartDate: "",
  registrationEndDate: "",
  description: "",
  status: true,
  photoEventUrl: null,
  rounds: [emptyRound()],
});

// ─── Helpers ──────────────────────────────────────────────────────────────────

/** Date input → ISO 8601 string. Supports both ISO and the picker display format `dd/MM/yyyy HH:mm`. */
function toIso(value: string): string {
  if (!value) return "";

  const nativeDate = new Date(value);
  if (!Number.isNaN(nativeDate.getTime())) return nativeDate.toISOString();

  const match = value.trim().match(/^(\d{1,2})\/(\d{1,2})\/(\d{4})\s+(\d{1,2}):(\d{2})$/);
  if (!match) return "";

  const [, dayText, monthText, yearText, hourText, minuteText] = match;
  const day = Number(dayText);
  const month = Number(monthText);
  const year = Number(yearText);
  const hour = Number(hourText);
  const minute = Number(minuteText);
  const localDate = new Date(year, month - 1, day, hour, minute);

  // Reject rollover values such as 31/02/2026 instead of silently changing them.
  const isExactDate =
    localDate.getFullYear() === year &&
    localDate.getMonth() === month - 1 &&
    localDate.getDate() === day &&
    localDate.getHours() === hour &&
    localDate.getMinutes() === minute;
  return isExactDate ? localDate.toISOString() : "";
}

/** Existing rounds become immutable after their closing time has passed. */
function isTimeLocked(value?: string | null): boolean {
  if (!value) return false;
  const timestamp = new Date(value).getTime();
  return Number.isFinite(timestamp) && timestamp < Date.now();
}

/** ISO string → `YYYY-MM-DDTHH:mm` for a datetime-local input (local time). */
function isoToLocalInput(iso: string): string {
  if (!iso) return "";
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) return "";
  const local = new Date(d.getTime() - d.getTimezoneOffset() * 60000);
  return local.toISOString().slice(0, 16);
}

// ─── Small field primitives ───────────────────────────────────────────────────

function InlineErrors({ errors }: { errors?: string[] }) {
  if (!errors?.length) return null;
  return (
    <ul
      role="alert"
      className="t-caption-sm"
      style={{ margin: "4px 0 0", paddingLeft: 18, color: "var(--color-error)" }}
    >
      {errors.map((error) => <li key={error}>{error}</li>)}
    </ul>
  );
}

/** Cặp ngày giờ gọn trên một hàng: [bắt đầu] → [kết thúc]. */
function DateRangeInput({
  start,
  end,
  onStart,
  onEnd,
  disabled = false,
  errors,
  startErrors,
  endErrors,
  onStartBlur,
  onEndBlur,
}: {
  start: string;
  end: string;
  onStart: (v: string) => void;
  onEnd: (v: string) => void;
  disabled?: boolean;
  errors?: string[];
  startErrors?: string[];
  endErrors?: string[];
  onStartBlur?: () => void;
  onEndBlur?: () => void;
}) {
  const [startFormatValid, setStartFormatValid] = useState(true);
  const [endFormatValid, setEndFormatValid] = useState(true);

  return (
    <div>
      <div style={{ display: "flex", alignItems: "flex-start", gap: 8 }}>
        <div style={{ flex: 1, minWidth: 0 }}>
          <DateTimePicker value={start} onChange={onStart} onBlur={onStartBlur}
            onValidationChange={setStartFormatValid}
            placeholder="Bắt đầu dd/MM/yyyy HH:mm" disabled={disabled} />
          <InlineErrors errors={startFormatValid ? startErrors : []} />
        </div>
        <span className="t-body-sm" style={{ color: "var(--color-mute)", flexShrink: 0, paddingTop: 10 }}>→</span>
        <div style={{ flex: 1, minWidth: 0 }}>
          <DateTimePicker value={end} onChange={onEnd} onBlur={onEndBlur}
            onValidationChange={setEndFormatValid}
            placeholder="Kết thúc dd/MM/yyyy HH:mm" disabled={disabled} />
          <InlineErrors errors={endFormatValid ? endErrors : []} />
        </div>
      </div>
      <InlineErrors errors={startFormatValid && endFormatValid ? errors : []} />
    </div>
  );
}

/** Small muted helper line under a field. */
function Hint({ children }: { children: ReactNode }) {
  return (
    <span className="t-caption-sm" style={{ color: "var(--color-mute)", marginTop: -2 }}>
      {children}
    </span>
  );
}

/** Search users by email/name in the database and add them as chips (stores ids). */
function UserSearchSelect({
  label,
  values,
  onChange,
  placeholder,
  hint,
}: {
  label: string;
  values: InvitedUser[];
  onChange: (next: InvitedUser[]) => void;
  placeholder?: string;
  hint?: ReactNode;
}) {
  const [query, setQuery] = useState("");
  const [debounced, setDebounced] = useState("");
  const [open, setOpen] = useState(false);
  const [manualFullName, setManualFullName] = useState("");
  const timerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const wrapperRef = useRef<HTMLDivElement>(null);
  const [labels, setLabels] = useState<Record<string, string>>({});

  const searchQuery = useQuery({
    queryKey: ["userSearch", debounced],
    enabled: debounced.length >= 2,
    queryFn: () => usersApi.search(debounced),
    staleTime: 30_000,
  });
  const results = (searchQuery.data ?? []).filter((u) => !values.some((v) => v.id === u.id || v.email === u.email));

  const labelFor = (user: InvitedUser) => {
    const fallback = user.fullName || user.email || user.id || "(không tên)";
    return labels[user.id ?? user.email] ?? fallback;
  };

  const onType = (v: string) => {
    setQuery(v);
    setOpen(true);
    if (timerRef.current) clearTimeout(timerRef.current);
    timerRef.current = setTimeout(() => setDebounced(v.trim()), 250);
  };

  const addUser = (u: UserSummary) => {
    const inviteUser: InvitedUser = {
      id: u.id,
      email: u.email ?? "",
      fullName: u.fullName ?? "",
    };
    setLabels((prev) => ({ ...prev, [u.id]: u.email ?? u.fullName ?? u.id }));
    if (!values.some((v) => v.id === u.id || v.email === inviteUser.email)) {
      onChange([...values, inviteUser]);
    }
    setQuery("");
    setDebounced("");
    setOpen(false);
  };

  const addManualUser = () => {
    const normalizedEmail = query.trim();
    const normalizedFullName = manualFullName.trim();
    if (!normalizedEmail) return;

    const inviteUser: InvitedUser = {
      id: undefined,
      email: normalizedEmail,
      fullName: normalizedFullName || normalizedEmail,
    };

    if (!values.some((v) => v.email === inviteUser.email)) {
      onChange([...values, inviteUser]);
    }

    setLabels((prev) => ({ ...prev, [normalizedEmail]: inviteUser.fullName }));
    setQuery("");
    setDebounced("");
    setManualFullName("");
    setOpen(false);
  };

  const removeAt = (i: number) => onChange(values.filter((_, idx) => idx !== i));

  useEffect(() => {
    if (!open) return;

    const handlePointerDown = (event: MouseEvent) => {
      if (wrapperRef.current && !wrapperRef.current.contains(event.target as Node)) {
        setOpen(false);
      }
    };

    document.addEventListener("mousedown", handlePointerDown);
    return () => document.removeEventListener("mousedown", handlePointerDown);
  }, [open]);

  return (
    <div style={{ display: "flex", flexDirection: "column", gap: 6 }}>
      <span className="t-caption-xs" style={{ color: "var(--color-mute)" }}>
        {label}
      </span>

      {values.length > 0 && (
        <div style={{ display: "flex", flexWrap: "wrap", gap: 6 }}>
          {values.map((v, i) => (
            <span
              key={`${v.email}-${i}`}
              style={{
                display: "inline-flex",
                alignItems: "center",
                gap: 6,
                padding: "4px 8px",
                background: "var(--color-surface-soft)",
                border: "var(--border-hairline)",
                borderRadius: "var(--radius-sm)",
                fontSize: "var(--fs-caption-sm)",
              }}
            >
              {labelFor(v)}
              <button
                type="button"
                onClick={() => removeAt(i)}
                aria-label={`Xóa ${labelFor(v)}`}
                style={{
                  background: "none",
                  border: "none",
                  padding: 0,
                  lineHeight: 1,
                  fontSize: 15,
                  color: "var(--color-mute)",
                  cursor: "pointer",
                }}
              >
                ×
              </button>
            </span>
          ))}
        </div>
      )}

      <div ref={wrapperRef} style={{ position: "relative" }}>
        <input
          className="text-input"
          value={query}
          placeholder={placeholder ?? "Nhập email để tìm…"}
          onChange={(e: ChangeEvent<HTMLInputElement>) => onType(e.target.value)}
          onFocus={() => setOpen(true)}
          style={{ width: "100%" }}
        />

        {open && debounced.length >= 2 && (
          <div
            style={{
              position: "absolute",
              top: "calc(100% + 4px)",
              left: 0,
              right: 0,
              zIndex: 20,
              background: "var(--color-canvas)",
              border: "var(--border-hairline)",
              borderRadius: "var(--radius-sm)",
              boxShadow: "0 6px 20px rgba(0,0,0,0.08)",
              maxHeight: 220,
              overflowY: "auto",
            }}
          >
            {searchQuery.isLoading ? (
              <div className="t-caption-sm" style={{ padding: "10px 12px", color: "var(--color-mute)" }}>
                Đang tìm…
              </div>
            ) : results.length === 0 ? (
              <div style={{ padding: "10px 12px", display: "flex", flexDirection: "column", gap: 8 }}>
                <div className="t-caption-sm" style={{ color: "var(--color-mute)" }}>
                  Không tìm thấy người dùng. Bạn có thể nhập thủ công email và tên đầy đủ để mời.
                </div>
                <input
                  className="text-input"
                  value={manualFullName}
                  onChange={(e) => setManualFullName(e.target.value)}
                  placeholder="Nhập họ và tên"
                  style={{ width: "100%" }}
                />
                <button
                  type="button"
                  onMouseDown={(e) => {
                    e.preventDefault();
                    addManualUser();
                  }}
                  style={{
                    alignSelf: "flex-start",
                    padding: "6px 10px",
                    background: "var(--color-surface-soft)",
                    border: "1px solid var(--color-hairline)",
                    borderRadius: "var(--radius-sm)",
                    cursor: "pointer",
                    fontSize: "var(--fs-caption-sm)",
                    color: "var(--color-primary)",
                  }}
                >
                  + Thêm người này
                </button>
              </div>
            ) : (
              results.map((u) => (
                <button
                  key={u.id}
                  type="button"
                  onMouseDown={(e) => {
                    e.preventDefault();
                    addUser(u);
                  }}
                  style={{
                    display: "flex",
                    flexDirection: "column",
                    gap: 2,
                    width: "100%",
                    textAlign: "left",
                    padding: "8px 12px",
                    background: "none",
                    border: "none",
                    borderBottom: "var(--border-hairline)",
                    cursor: "pointer",
                  }}
                >
                  <span className="t-body-sm" style={{ color: "var(--color-ink)" }}>
                    {u.email ?? "(không có email)"}
                  </span>
                  {u.fullName && (
                    <span className="t-caption-sm" style={{ color: "var(--color-mute)" }}>
                      {u.fullName}
                    </span>
                  )}
                </button>
              ))
            )}
          </div>
        )}
      </div>

      {hint && <Hint>{hint}</Hint>}
    </div>
  );
}

// ─── Image Upload ─────────────────────────────────────────────────────────────

function EventPhotoUpload({
  value,
  onChange,
}: {
  value: string | null;
  onChange: (url: string | null) => void;
}) {
  const [isUploading, setIsUploading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  // URL bị lỗi tải — so với `value` để tự reset khi ảnh đổi (không cần effect).
  const [erroredUrl, setErroredUrl] = useState<string | null>(null);
  const imgError = value != null && value === erroredUrl;
  const fileInputRef = useRef<HTMLInputElement>(null);

  const handleFileChange = async (e: ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    if (!file.type.startsWith("image/")) {
      setError("Chỉ chấp nhận tệp hình ảnh (.png, .jpg, .jpeg, ...)");
      return;
    }

    setIsUploading(true);
    setError(null);
    try {
      const url = await storageApi.upload(file);
      onChange(url);
    } catch (err) {
      setError((err as Error).message || "Tải ảnh lên thất bại.");
    } finally {
      setIsUploading(false);
      if (fileInputRef.current) {
        fileInputRef.current.value = "";
      }
    }
  };

  const handleDrop = async (e: React.DragEvent<HTMLDivElement>) => {
    e.preventDefault();
    const file = e.dataTransfer.files?.[0];
    if (!file) return;
    if (!file.type.startsWith("image/")) {
      setError("Chỉ chấp nhận tệp hình ảnh (.png, .jpg, .jpeg, ...)");
      return;
    }
    setIsUploading(true);
    setError(null);
    try {
      const url = await storageApi.upload(file);
      onChange(url);
    } catch (err) {
      setError((err as Error).message || "Tải ảnh lên thất bại.");
    } finally {
      setIsUploading(false);
    }
  };

  return (
    <div style={{ display: "flex", flexDirection: "column", gap: 6 }}>
      <span className="t-body-strong" style={{ color: "var(--color-ink)", letterSpacing: "0.05em" }}>
        Ảnh đại diện sự kiện
      </span>
      {value ? (
        <div style={{ position: "relative", width: "100%", aspectRatio: "2.35 / 1", borderRadius: "var(--radius-sm)", overflow: "hidden", border: "1px solid var(--color-hairline)" }}>
          {imgError ? (
            <div style={{ width: "100%", height: "100%", display: "flex", alignItems: "center", justifyContent: "center", textAlign: "center", background: "var(--color-surface-soft)" }}>
              <span className="t-body-sm" style={{ color: "var(--color-mute)" }}>Không tải được ảnh</span>
            </div>
          ) : (
            <img src={value} alt="Event Photo" onError={() => setErroredUrl(value)} style={{ width: "100%", height: "100%", objectFit: "cover" }} />
          )}
          <div style={{ position: "absolute", top: 8, right: 8, display: "flex", gap: 8 }}>
            <button
              type="button"
              className="btn btn-sm btn-primary"
              style={{ cursor: "pointer" }}
              onClick={() => fileInputRef.current?.click()}
            >
              Thay đổi
            </button>
            <button
              type="button"
              className="btn btn-sm btn-outline"
              style={{ background: "rgba(255,255,255,0.9)", border: "none" }}
              onClick={() => onChange(null)}
            >
              Xóa ảnh
            </button>
          </div>
          <input
            type="file"
            accept="image/*"
            style={{ display: "none" }}
            ref={fileInputRef}
            onChange={handleFileChange}
          />
        </div>
      ) : (
        <div
          onDragOver={(e) => e.preventDefault()}
          onDrop={handleDrop}
          onClick={() => fileInputRef.current?.click()}
          style={{
            border: "1px dashed var(--color-ink)",
            borderRadius: "var(--radius-sm)",
            padding: "var(--space-lg)",
            aspectRatio: "2.35 / 1",
            textAlign: "center",
            cursor: isUploading ? "not-allowed" : "pointer",
            background: "transparent",
            transition: "all 0.2s ease",
            display: "flex",
            flexDirection: "column",
            alignItems: "center",
            justifyContent: "center",
            gap: 8,
            opacity: isUploading ? 0.7 : 1,
          }}
        >
          <span className="t-body-strong" style={{ color: "var(--color-mute)" }}>
            {isUploading ? "Đang tải ảnh lên..." : "Nhấn để chọn hoặc kéo thả ảnh vào đây"}
          </span>
          <span className="t-caption-sm" style={{ color: "var(--color-mute)" }}>
            Hỗ trợ PNG, JPG, JPEG. Tối đa 5MB.
          </span>
          <input
            type="file"
            accept="image/*"
            style={{ display: "none" }}
            ref={fileInputRef}
            onChange={handleFileChange}
            disabled={isUploading}
          />
        </div>
      )}
      {error && (
        <span className="t-caption-sm" style={{ color: "var(--color-error)", marginTop: 4 }}>
          {error}
        </span>
      )}
    </div>
  );
}

// ─── Submission requirements (vertical checkboxes) ────────────────────────────

function SubmissionRequirementsField({
  value,
  onChange,
  disabled = false,
}: {
  value: SubmissionRequirements;
  onChange: (next: SubmissionRequirements) => void;
  disabled?: boolean;
}) {
  const checkboxRow = (
    key: string,
    checked: boolean,
    label: string,
    onToggle: () => void,
  ) => (
    <label
      key={key}
      style={{
        display: "flex",
        alignItems: "center",
        gap: 10,
        cursor: disabled ? "not-allowed" : "pointer",
        opacity: disabled ? 0.6 : 1,
      }}
    >
      <input
        type="checkbox"
        checked={checked}
        disabled={disabled}
        onChange={onToggle}
        style={{ width: 16, height: 16, cursor: disabled ? "not-allowed" : "pointer", accentColor: "var(--color-primary)" }}
      />
      <span className="t-body-sm" style={{ color: "var(--color-ink)" }}>
        {label}
      </span>
    </label>
  );

  return (
    <div style={{ display: "flex", flexDirection: "column", gap: 6 }}>
      <span className="t-body-strong" style={{ color: "var(--color-ink)", letterSpacing: "0.05em" }}>
        Yêu cầu nộp bài
      </span>

      <div style={{ display: "flex", flexDirection: "column", gap: 10, marginTop: 2 }}>
        {SUBMISSION_URL_OPTIONS.map((opt) =>
          checkboxRow(opt.key, value[opt.key], opt.label, () =>
            onChange({ ...value, [opt.key]: !value[opt.key] }),
          ),
        )}
        {checkboxRow("other", value.otherEnabled, "Khác", () =>
          onChange({ ...value, otherEnabled: !value.otherEnabled }),
        )}
        {value.otherEnabled && (
          <input
            className="text-input"
            value={value.otherText}
            disabled={disabled}
            placeholder="Nhập yêu cầu nộp bài khác…"
            onChange={(e: ChangeEvent<HTMLInputElement>) =>
              onChange({ ...value, otherText: e.target.value })
            }
            style={{ marginLeft: 26, width: "calc(100% - 26px)" }}
          />
        )}
      </div>
      <Hint>Chọn ít nhất một yêu cầu thí sinh cần nộp (có thể chọn nhiều).</Hint>
    </div>
  );
}

// ─── Round editor ─────────────────────────────────────────────────────────────

function parseAdvancementRule(rule: string) {
  const trimmed = (rule || "").trim();
  if (trimmed.startsWith('top:')) {
    return { type: 'top', value: trimmed.replace('top:', '') };
  }
  if (trimmed.toLowerCase().startsWith('percent:')) {
    return { type: 'percent', value: trimmed.replace(/percent:/i, '') };
  }
  if (trimmed.toLowerCase().startsWith('minscore:')) {
    return { type: 'minScore', value: trimmed.replace(/minscore:/i, '') };
  }
  // Mặc định là top nếu rỗng hoặc không khớp
  return { type: 'top', value: '' };
}

// ─── Track input card (cùng format thẻ với màn hiển thị timeline) ──────────────

/** Nhãn + cặp ngày giờ cho một pha trong thẻ hạng mục. */
function PhaseInput({
  label,
  start,
  end,
  onStart,
  onEnd,
  disabled = false,
  errors,
  onStartBlur,
  onEndBlur,
}: {
  label: string;
  start: string;
  end: string;
  onStart: (v: string) => void;
  onEnd: (v: string) => void;
  disabled?: boolean;
  errors?: string[];
  onStartBlur?: () => void;
  onEndBlur?: () => void;
}) {
  return (
    <div className="flex flex-col sm:flex-row sm:items-center gap-1 sm:gap-3">
      <span className="t-body-strong text-ink shrink-0 sm:w-40 tracking-wider">{label}</span>
      <div className="flex-1 min-w-0">
        <DateRangeInput start={start} end={end} onStart={onStart} onEnd={onEnd}
          onStartBlur={onStartBlur} onEndBlur={onEndBlur} disabled={disabled} errors={errors} />
      </div>
    </div>
  );
}

/** Một hạng mục dạng thẻ nhập liệu — viền + border trái, chứa tên/mô tả/template/
 *  yêu cầu nộp bài và đúng 2 pha thời gian (Nộp bài, Chấm điểm). */
function TrackFormCard({
  track,
  index,
  templates,
  templatesLoading,
  onChange,
  disabled = false,
  validationErrors = [],
  onFieldBlur,
}: {
  track: TrackForm;
  index: number;
  templates: TemplateSummary[];
  templatesLoading: boolean;
  onChange: (patch: Partial<TrackForm>) => void;
  disabled?: boolean;
  validationErrors?: string[];
  onFieldBlur?: (field: keyof TrackForm) => void;
}) {
  const qc = useQueryClient();
  const notify = useNotify();
  const [showCriteria, setShowCriteria] = useState(false);
  const [showBuilder, setShowBuilder] = useState(false);
  // Khi "chỉnh sửa tiêu chí": clone sẵn rồi mở trình sửa với id + tên bộ mới.
  const [builderTemplateId, setBuilderTemplateId] = useState<string | undefined>(undefined);
  const [builderName, setBuilderName] = useState("");
  const [deriving, setDeriving] = useState(false);

  // Bấm "Chỉnh sửa tiêu chí" trong popup xem: clone bộ đang xem → mở trình sửa
  // với tiêu chí đã copy sẵn.
  async function handleDerive() {
    if (!track.templateId) return;
    setDeriving(true);
    try {
      const detail = await templatesApi.getById(track.templateId);
      if (detail.isSystem) {
        // Bộ dùng chung (hệ thống) → tạo bản RIÊNG (tên duy nhất, BE chặn trùng tên)
        // để không ảnh hưởng sự kiện khác.
        const existing = new Set(templates.map((t) => t.templateName));
        let n = 2;
        let uniqueName = `${detail.templateName} (${n})`;
        while (existing.has(uniqueName)) uniqueName = `${detail.templateName} (${++n})`;
        const newId = await templatesApi.clone(track.templateId, uniqueName);
        onChange({ templateId: newId });
        qc.invalidateQueries({ queryKey: ["templates"] });
        setBuilderTemplateId(newId);
        setBuilderName(uniqueName);
      } else {
        // Bộ riêng (không phải hệ thống) → SỬA TRỰC TIẾP, dùng lại, không tạo mới.
        setBuilderTemplateId(track.templateId);
        setBuilderName(detail.templateName);
      }
      setShowCriteria(false);
      setShowBuilder(true);
    } catch (e) {
      notify.error(getErrorMessage(e, "Không mở được trình chỉnh sửa tiêu chí. Vui lòng thử lại."));
    } finally {
      setDeriving(false);
    }
  }

  function openCreateBlank() {
    setBuilderTemplateId(undefined);
    setBuilderName("");
    setShowBuilder(true);
  }

  return (
    <>
      <div className="flex items-center gap-3">
        <span className="t-body-strong text-ink shrink-0 tracking-wider sm:w-40">Tên hạng mục</span>
        <input
          className="text-input flex-1 min-w-0"
          style={{ fontWeight: 700 }}
          value={track.trackName}
          disabled={disabled}
          placeholder={`Tên hạng mục ${index + 1}`}
          onChange={(e) => onChange({ trackName: e.target.value })}
          onBlur={() => onFieldBlur?.("trackName")}
        />
      </div>

      <div className="flex items-start gap-3">
        <span className="t-body-strong text-ink shrink-0 tracking-wider sm:w-40 pt-2">Mô tả</span>
        <textarea
          className="text-input flex-1 min-w-0"
          rows={2}
          value={track.description}
          disabled={disabled}
          placeholder="Mô tả hạng mục"
          style={{ height: "auto", resize: "vertical", fontFamily: "inherit" }}
          onChange={(e) => onChange({ description: e.target.value })}
        />
      </div>

      <div className="flex items-center gap-3">
        <span className="t-body-strong text-ink shrink-0 tracking-wider sm:w-40">Bộ tiêu chí</span>
        <div className="flex-1 min-w-0" style={{ display: "flex", gap: 8, alignItems: "stretch" }}>
          <select
            className="text-input"
            style={{ flex: 1, minWidth: 0 }}
            value={track.templateId}
            disabled={templatesLoading || disabled}
            onChange={(e) => onChange({ templateId: e.target.value })}
          >
            <option value="">{templatesLoading ? "Đang tải bộ tiêu chí…" : "— Chọn bộ tiêu chí chấm điểm —"}</option>
            {templates.map((t) => (
              <option key={t.id} value={t.id}>{t.templateName}</option>
            ))}
          </select>
          <button
            type="button"
            className={`btn btn-sm shrink-0 ${track.templateId ? 'btn-view' : 'btn-create'}`}
            style={{ height: "auto", alignSelf: "stretch", cursor: "pointer", whiteSpace: "nowrap" }}
            disabled={disabled && !track.templateId}
            onClick={() => (track.templateId ? setShowCriteria(true) : openCreateBlank())}
          >
            {track.templateId ? "Xem tiêu chí" : "Tạo bộ tiêu chí"}
          </button>
        </div>
      </div>
      {showCriteria && track.templateId && (
        <TemplateCriteriaModal
          templateId={track.templateId}
          onClose={() => setShowCriteria(false)}
          onDerive={disabled ? undefined : handleDerive}
          deriveBusy={deriving}
        />
      )}
      {showBuilder && (
        <TemplateBuilderModal
          initialTemplateId={builderTemplateId}
          initialName={builderName || undefined}
          templates={templates}
          onTemplateReady={(id) => onChange({ templateId: id })}
          onClose={() => setShowBuilder(false)}
        />
      )}

      <div onBlur={() => onFieldBlur?.("submissionRequirements")}>
        <SubmissionRequirementsField
          value={track.submissionRequirements}
          onChange={(next) => onChange({ submissionRequirements: next })}
          disabled={disabled}
        />
      </div>
      <InlineErrors errors={validationErrors.filter((error) => error.includes("yêu cầu"))} />

      {/* Hai pha thời gian — cùng thứ tự với màn hiển thị. */}
      <div className="flex flex-col gap-2">
        <PhaseInput label="Nộp bài" start={track.startDate} end={track.endDate}
          disabled={disabled}
          errors={validationErrors.filter((error) => error.includes("nộp bài") || error.includes("thời gian của vòng"))}
          onStartBlur={() => onFieldBlur?.("startDate")} onEndBlur={() => onFieldBlur?.("endDate")}
          onStart={(v) => onChange({ startDate: v })} onEnd={(v) => onChange({ endDate: v })} />
        <PhaseInput label="Chấm điểm" start={track.scoringStartDate} end={track.scoringEndDate}
          disabled={disabled}
          errors={validationErrors.filter((error) => error.includes("chấm điểm") || error.includes("thời gian của vòng"))}
          onStartBlur={() => onFieldBlur?.("scoringStartDate")} onEndBlur={() => onFieldBlur?.("scoringEndDate")}
          onStart={(v) => onChange({ scoringStartDate: v })} onEnd={(v) => onChange({ scoringEndDate: v })} />
      </div>
    </>
  );
}

// ─── Edit-mode loader: fetches the event then renders the prefilled form ───────

export function CreateEventForm({
  onCancel,
  eventId,
}: {
  onCancel: () => void;
  /** When provided, the form edits an existing event instead of creating. */
  eventId?: string;
}) {
  if (!eventId) {
    return (
      <EventFormBody
        onCancel={onCancel}
        initialForm={emptyEvent()}
        initialRoundIds={[]}
        initialTrackIds={[]}
      />
    );
  }
  return <EditEventLoader eventId={eventId} onCancel={onCancel} />;
}

function EditEventLoader({
  eventId,
  onCancel,
}: {
  eventId: string;
  onCancel: () => void;
}) {
  const eventQuery = useEventModel(eventId);
  const roundsQuery = useEventRounds(eventId);
  const tracksQuery = useEventTracks(eventId);

  if (eventQuery.isError || roundsQuery.isError || tracksQuery.isError) {
    return (
      <p className="t-body-md" style={{ color: "var(--color-error)" }}>
        Không tải được sự kiện để chỉnh sửa.
      </p>
    );
  }
  if (!eventQuery.data || !roundsQuery.data || !tracksQuery.data) {
    return (
      <p className="t-body-md" style={{ color: "var(--color-mute)" }}>
        Đang tải sự kiện…
      </p>
    );
  }

  const event = eventQuery.data;
  const rounds = roundsQuery.data;
  const tracks = tracksQuery.data;

  // `submissionRuleDescription` AND the two per-track time windows are only
  // returned nested inside GET /Events/{id} (EventModel.rounds[].tracks[]), not by
  // the flat /Tracks/event list — build a trackId → {rule, dates} lookup so the
  // edit form can pre-fill the requirement checkboxes and the Nộp bài/Chấm điểm times.
  interface NestedTrackMeta {
    submissionRule: string;
    startDate: string;
    endDate: string;
    scoringStartDate: string;
    scoringEndDate: string;
  }
  const trackMetaById = new Map<string, NestedTrackMeta>();
  const nestedRounds =
    (event as {
      rounds?: Array<{
        tracks?: Array<{
          id?: string;
          submissionRuleDescription?: string | null;
          startDate?: string | null;
          endDate?: string | null;
          scoringStartDate?: string | null;
          scoringEndDate?: string | null;
        }>;
      }>;
    }).rounds ?? [];
  for (const r of nestedRounds) {
    for (const t of r.tracks ?? []) {
      if (!t.id) continue;
      trackMetaById.set(t.id, {
        submissionRule: t.submissionRuleDescription ?? "",
        startDate: t.startDate ?? "",
        endDate: t.endDate ?? "",
        scoringStartDate: t.scoringStartDate ?? "",
        scoringEndDate: t.scoringEndDate ?? "",
      });
    }
  }

  const orderedRounds = [...rounds].sort((a, b) => a.roundNumber - b.roundNumber);
  const initialForm: EventForm = {
    eventName: event.eventName ?? "",
    maxTeams: event.maxTeams != null ? String(event.maxTeams) : "",
    season: event.season ?? "",
    year: String(event.year ?? ""),
    startDate: isoToLocalInput(event.startDate),
    endDate: isoToLocalInput(event.endDate),
    registrationStartDate: isoToLocalInput(event.registrationStartDate ?? ""),
    registrationEndDate: isoToLocalInput(event.registrationEndDate ?? ""),
    description: event.description ?? "",
    status: event.status ?? false,
    photoEventUrl: event.photoEventUrl ?? null,
    rounds: orderedRounds.map((r) => ({
      id: r.id,
      uid: nextRoundUid(),
      roundName: r.roundName ?? "",
      roundNumber: String(r.roundNumber ?? ""),
      startDate: isoToLocalInput(r.startDate),
      endDate: isoToLocalInput(r.endDate),
      advancementRule: r.advancementRule ?? "",
      tracks: tracks
        .filter((t) => t.roundId === r.id)
        .map((t) => {
          const meta = trackMetaById.get(t.id);
          return {
            id: t.id,
            uid: nextTrackUid(),
            trackName: t.trackName ?? "",
            description: t.description ?? "",
            templateId: t.templateId ?? "",
            // Pre-fill the requirement checkboxes from the saved rule (joined via id).
            submissionRequirements: parseSubmissionRequirements(meta?.submissionRule),
            // Keep the raw saved string to show as a read-only reference in edit mode.
            savedSubmissionRule: meta?.submissionRule ?? "",
            // Hai khoảng thời gian thực của hạng mục (từ model lồng GET /Events/{id}).
            startDate: isoToLocalInput(meta?.startDate ?? ""),
            endDate: isoToLocalInput(meta?.endDate ?? ""),
            scoringStartDate: isoToLocalInput(meta?.scoringStartDate ?? ""),
            scoringEndDate: isoToLocalInput(meta?.scoringEndDate ?? ""),
          };
        }),
    })),
  };

  return (
    <EventFormBody
      onCancel={onCancel}
      eventId={eventId}
      initialForm={initialForm}
      initialRoundIds={rounds.map((r) => r.id).filter(Boolean)}
      initialTrackIds={tracks.map((t) => t.id).filter(Boolean)}
    />
  );
}

// ─── Accordion section (vỏ chứa từng khối của form) ────────────────────────────

/** Một khối accordion: header (chevron + tiêu đề + chấm cảnh báo + hành động)
 *  và phần thân mở/đóng được. */
/** Bảng màu viền trái phân biệt các khối. Thông tin/Đăng ký cố định 2 màu đầu;
 *  mỗi Vòng lấy màu xoay vòng từ phần còn lại. */
const SECTION_ACCENTS = ["#0046a4", "#b45309", "#0d9488", "#7c3aed", "#dc2626", "#65a30d", "#0891b2", "#c2410c"];

function AccordionSection({
  title,
  summary,
  warn,
  open,
  onToggle,
  actions,
  accent,
  inset,
  children,
}: {
  title: string;
  summary?: string;
  warn?: boolean;
  open: boolean;
  onToggle: () => void;
  actions?: ReactNode;
  /** Màu viền trái để phân biệt khối. */
  accent?: string;
  /** Thụt thêm phần thân để input thẳng hàng với input hạng mục (lồng sâu hơn). */
  inset?: boolean;
  children: ReactNode;
}) {
  return (
    <div
      className="rounded-md border border-ink bg-canvas overflow-hidden"
      style={accent ? { borderLeftWidth: 4, borderLeftColor: accent } : undefined}
    >
      <div className="flex items-center gap-2 px-4 py-3">
        <button
          type="button"
          onClick={onToggle}
          aria-expanded={open}
          className="flex flex-1 min-w-0 items-center gap-2.5"
          style={{ background: "none", border: "none", cursor: "pointer", textAlign: "left", padding: 0 }}
        >
          <span style={{ fontSize: 11, color: "var(--color-mute)", transform: open ? "rotate(90deg)" : "none", transition: "transform .15s", flexShrink: 0 }}>▶</span>
          <span className="t-body-strong text-ink shrink-0">{title}</span>
          {!open && summary && <span className="t-caption-sm text-mute truncate">· {summary}</span>}
        </button>
        {warn && (
          <span
            title="Còn thiếu thông tin"
            aria-label="Còn thiếu thông tin"
            style={{ width: 8, height: 8, borderRadius: "50%", background: "var(--color-error)", flexShrink: 0 }}
          />
        )}
        {actions && <div style={{ flexShrink: 0 }}>{actions}</div>}
      </div>
      {open && (
        <div className={`px-4 pb-4 pt-1 border-t border-hairline flex flex-col gap-3${inset ? " sm:pl-9 sm:pr-8.25" : ""}`}>
          {children}
        </div>
      )}
    </div>
  );
}

// ─── Main form body ─────────────────────────────────────────────────────────────

function EventFormBody({
  onCancel,
  eventId,
  initialForm,
  initialRoundIds,
  initialTrackIds,
}: {
  onCancel: () => void;
  eventId?: string;
  initialForm: EventForm;
  initialRoundIds: string[];
  initialTrackIds: string[];
}) {
  const isEdit = !!eventId;
  const router = useRouter();
  const [form, setForm] = useState<EventForm>(() => initialForm);
  // Keep an immutable snapshot for date locks even after the form state changes.
  const [showValidation, setShowValidation] = useState(false);
  const [validationReferenceTime, setValidationReferenceTime] = useState<number | null>(null);
  const [touchedFields, setTouchedFields] = useState<Set<string>>(() => new Set());
  const touchFields = (...keys: string[]) => {
    setTouchedFields((current) => {
      const next = new Set(current);
      keys.forEach((key) => next.add(key));
      return next;
    });
  };
  // Trạng thái mở/đóng các khối accordion (mở nhiều tùy ý). Mặc định mở "Thông tin".
  const [openSections, setOpenSections] = useState<Set<string>>(() => new Set(["info"]));
  const toggleSection = (id: string) =>
    setOpenSections((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  const originalRoundIds = useRef<string[]>(initialRoundIds);
  const originalTrackIds = useRef<string[]>(initialTrackIds);
  const queryClient = useQueryClient();
  const notify = useNotify();

  const templatesQuery = useQuery({
    queryKey: ["templates"],
    queryFn: () => templatesApi.list(),
    staleTime: 5 * 60_000,
  });
  const templates = templatesQuery.data ?? [];

  const createMutation = useMutation({
    mutationFn: async (payload: CreateEventPayload) => {
      const created = await eventsApi.create(payload);
      // Wait for rounds/tracks creation logic handled in backend (or keep checking for validity, though backend handles creation of rounds/tracks directly from payload)
      return created;
    },
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: ["events"] });
      notify.success("Tạo sự kiện thành công!");
      if (data?.id) {
        router.push(`/events/${data.id}/manage`);
      }
    },
    onError: (e) => notify.error(getErrorMessage(e, "Tạo sự kiện thất bại. Vui lòng thử lại.")),
  });

  const editMutation = useMutation({
    mutationFn: async () => {
      const id = eventId as string;
      // Snapshot the server-backed ids at the start of this save. New rounds/tracks
      // receive ids during the mutation and must be retained in local form state so
      // a second save in the same open form updates them instead of creating duplicates.
      const previouslySavedRoundIds = [...originalRoundIds.current];
      const previouslySavedTrackIds = [...originalTrackIds.current];
      const savedRoundIds = new Set<string>();
      const savedTrackIds = new Set<string>();
      const oldStart = new Date(initialForm.startDate).getTime();
      const oldEnd = new Date(initialForm.endDate).getTime();
      const newStart = new Date(form.startDate).getTime();
      const newEnd = new Date(form.endDate).getTime();

      // Create a temporary bounding box that encloses BOTH the old and new timelines.
      // This prevents the backend from rejecting the Event update if the new bounds
      // shrink to exclude old rounds, or rejecting Round updates if they expand outside old bounds.
      const tempStart = newStart < oldStart ? toIso(form.startDate) : toIso(initialForm.startDate);
      const tempEnd = newEnd > oldEnd ? toIso(form.endDate) : toIso(initialForm.endDate);
      const requiresTempWidening = tempStart !== toIso(form.startDate) || tempEnd !== toIso(form.endDate);

      const finalPayload = {
        eventName: form.eventName.trim(),
        maxTeams: Number(form.maxTeams),
        season: form.season.trim(),
        year: Number(form.year) || 0,
        startDate: toIso(form.startDate),
        endDate: toIso(form.endDate),
        registrationStartDate: toIso(form.registrationStartDate) || null,
        registrationEndDate: toIso(form.registrationEndDate) || null,
        description: form.description.trim(),
        status: form.status,
        photoEventUrl: form.photoEventUrl || null,
      };

      // 1. Update Event to the widest bounds temporarily (if needed)
      if (requiresTempWidening) {
        await eventsApi.update(id, { ...finalPayload, startDate: tempStart, endDate: tempEnd });
      } else {
        await eventsApi.update(id, finalPayload);
      }

      // 2. Update Rounds and Tracks to their new times
      for (let ri = 0; ri < form.rounds.length; ri++) {
        const r = form.rounds[ri];
        const roundPayload = {
          eventId: id,
          roundName: r.roundName.trim(),
          roundNumber: ri + 1,
          startDate: toIso(r.startDate),
          endDate: toIso(r.endDate),
          advancementRule: r.advancementRule.trim(),
        };
        let roundId = r.id;
        if (roundId) {
          if (!isTimeLocked(r.endDate)) {
            await roundsApi.update(roundId, roundPayload);
          }
        }
        else {
          roundId = (await roundsApi.create(roundPayload)).id;
          const createdRoundId = roundId;
          originalRoundIds.current = Array.from(
            new Set([...originalRoundIds.current, createdRoundId]),
          );
          // Persist only the new id, preserving any field edits made while the
          // request was in flight.
          setForm((current) => ({
            ...current,
            rounds: current.rounds.map((round) =>
              round.uid === r.uid ? { ...round, id: createdRoundId } : round,
            ),
          }));
        }
        savedRoundIds.add(roundId);

        for (const t of r.tracks) {
          const trackPayload = {
            eventId: id,
            roundId: roundId as string,
            trackName: t.trackName.trim(),
            templateId: t.templateId.trim() || null,
            description: t.description.trim(),
            submissionRuleDescription: serializeSubmissionRequirements(t.submissionRequirements),
            startDate: toIso(t.startDate) || null,
            endDate: toIso(t.endDate) || null,
            scoringStartDate: toIso(t.scoringStartDate) || null,
            scoringEndDate: toIso(t.scoringEndDate) || null,
          };
          const trackId = t.id;
          if (trackId) {
            await tracksApi.update(trackId, trackPayload);
            savedTrackIds.add(trackId);
          } else {
            // POST /Tracks (CreateTrackRequestModel) chưa nhận submissionRuleDescription
            // → tạo xong update ngay để lưu yêu cầu nộp bài cho hạng mục mới thêm khi sửa.
            const createdId = (await tracksApi.create(trackPayload)).id;
            savedTrackIds.add(createdId);
            originalTrackIds.current = Array.from(
              new Set([...originalTrackIds.current, createdId]),
            );
            // Keep the form usable after saving: subsequent saves must PUT this
            // track, and removing it later in the same session must DELETE it.
            setForm((current) => ({
              ...current,
              rounds: current.rounds.map((round) =>
                round.uid === r.uid
                  ? {
                    ...round,
                    tracks: round.tracks.map((track) =>
                      track.uid === t.uid ? { ...track, id: createdId } : track,
                    ),
                  }
                  : round,
              ),
            }));
            if (trackPayload.submissionRuleDescription.trim()) {
              await tracksApi.update(createdId, trackPayload);
            }
          }
        }
      }

      for (const tid of previouslySavedTrackIds) {
        if (!savedTrackIds.has(tid)) await tracksApi.remove(tid);
      }
      for (const rid of previouslySavedRoundIds) {
        const originalRound = initialForm.rounds.find((round) => round.id === rid);
        const roundLocked = originalRound && isTimeLocked(originalRound.endDate);
        if (!savedRoundIds.has(rid) && !roundLocked) await roundsApi.remove(rid);
      }

      // 3. Finalize Event to its actual bounds
      if (requiresTempWidening) {
        await eventsApi.update(id, finalPayload);
      }

      // These refs are also the deletion baseline for the next save while the
      // user remains in this form.
      originalRoundIds.current = [...savedRoundIds];
      originalTrackIds.current = [...savedTrackIds];
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["events"] });
      queryClient.invalidateQueries({ queryKey: eventKeys.detail(eventId as string) });
      queryClient.invalidateQueries({ queryKey: ["rounds", eventId] });
      queryClient.invalidateQueries({ queryKey: ["tracks", eventId] });
      notify.success("Cập nhật sự kiện thành công!");
    },
    onError: (e) => notify.error(getErrorMessage(e, "Cập nhật sự kiện thất bại. Vui lòng thử lại.")),
  });

  const activeMutation = isEdit ? editMutation : createMutation;

  type EventStringKey = "eventName" | "maxTeams" | "season" | "year" | "startDate" | "endDate" | "registrationStartDate" | "registrationEndDate" | "description";
  const setField = (key: EventStringKey, value: string) => {
    setForm((f) => {
      const updated = { ...f, [key]: value };
      // Keep linked defaults in sync until the admin explicitly customises the
      // destination field. A destination that still equals the previous source
      // is considered linked; a different value is a manual override.
      if (key === "startDate") {
        if (!f.registrationStartDate || f.registrationStartDate === f.startDate) {
          updated.registrationStartDate = value;
        }
        if (value) {
          const date = new Date(value);
          const year = date.getFullYear();
          if (year && !Number.isNaN(year)) {
            updated.year = String(year);
          }

          // Auto-derive FPT University Academic Season based on user requirements:
          // Spring: Jan to Apr (0 to 3)
          // Summer: May to Aug (4 to 7)
          // Fall: Sep to Dec (8 to 11)
          const month = date.getMonth(); // 0-11
          if (month >= 0 && month <= 3) {
            updated.season = "Spring";
          } else if (month >= 4 && month <= 7) {
            updated.season = "Summer";
          } else {
            updated.season = "Fall";
          }
        } else {
          updated.year = "";
          updated.season = "";
        }
      }
      // Kết thúc đăng ký = bắt đầu Vòng 1 (mặc định, vẫn sửa tay được).
      if (key === "registrationEndDate" && updated.rounds[0]) {
        const firstRound = updated.rounds[0];
        if (!firstRound.startDate || firstRound.startDate === f.registrationEndDate) {
          updated.rounds[0] = {
            ...firstRound,
            startDate: value,
            tracks: firstRound.tracks.map((track) => ({
              ...track,
              startDate:
                !track.startDate || track.startDate === firstRound.startDate
                  ? value
                  : track.startDate,
            })),
          };
        }
      }
      return updated;
    });
  };

  const setStatus = (status: boolean) => setForm((f) => ({ ...f, status }));

  // ── round operations ──
  const addRound = () => {
    const newR = { ...emptyRound() };
    const lastRound = form.rounds[form.rounds.length - 1];
    const eventEnd = new Date(toIso(form.endDate)).getTime();
    const roundEndCandidates = [
      lastRound?.endDate,
      ...(lastRound?.tracks.map((track) => track.scoringEndDate) ?? []),
    ]
      .map((date) => new Date(toIso(date ?? "")).getTime())
      .filter(Number.isFinite);
    const latestRoundEnd = roundEndCandidates.length
      ? Math.max(...roundEndCandidates)
      : Number.NaN;

    if (Number.isFinite(eventEnd) && Number.isFinite(latestRoundEnd) && latestRoundEnd >= eventEnd) {
      notify.error(
        "Quỹ thời gian của Sự kiện đã hết, không thể tạo thêm vòng thi mới. Vui lòng kéo dài thời gian kết thúc Sự kiện.",
      );
      return;
    }

    newR.startDate = lastRound ? lastRound.endDate : form.registrationEndDate || form.startDate;
    // Track đầu tiên của vòng mới kế thừa thời gian bắt đầu vòng.
    newR.tracks = newR.tracks.map((track) => ({ ...track, startDate: newR.startDate }));
    setForm((current) => ({ ...current, rounds: [...current.rounds, newR] }));
    setOpenSections((prev) => new Set(prev).add(`round-${newR.uid}`)); // vòng mới tự mở
  };

  const removeRound = (ri: number) =>
    setForm((f) => ({ ...f, rounds: f.rounds.filter((_, i) => i !== ri) }));

  const updateRound = (ri: number, key: keyof Omit<RoundForm, "tracks">, value: string) =>
    setForm((f) => {
      const updatedRounds = f.rounds.map((r, i) => {
        if (i !== ri) return r;
        const nextRound = { ...r, [key]: value };
        // Bắt đầu nộp bài = bắt đầu vòng, cho tới khi admin đặt ngoại lệ.
        if (key === "startDate") {
          nextRound.tracks = r.tracks.map((t) => ({
            ...t,
            startDate: !t.startDate || t.startDate === r.startDate ? value : t.startDate,
          }));
        }
        return nextRound;
      });
      // Kết thúc vòng = bắt đầu vòng kế tiếp, trừ khi đã được sửa tay.
      if (key === "endDate" && updatedRounds[ri + 1]) {
        const nextRound = updatedRounds[ri + 1];
        if (!nextRound.startDate || nextRound.startDate === f.rounds[ri].endDate) {
          updatedRounds[ri + 1] = {
            ...nextRound,
            startDate: value,
            tracks: nextRound.tracks.map((track) => ({
              ...track,
              startDate:
                !track.startDate || track.startDate === nextRound.startDate
                  ? value
                  : track.startDate,
            })),
          };
        }
      }
      return { ...f, rounds: updatedRounds };
    });

  // ── track operations ──
  const addTrack = (ri: number) => {
    const uid = nextTrackUid();
    setForm((f) => ({
      ...f,
      rounds: f.rounds.map((r, i) =>
        i === ri
          ? {
            ...r,
            // Hạng mục mới điền sẵn: nộp bài mở = bắt đầu vòng, chấm điểm mở = kết thúc vòng.
            tracks: [
              ...r.tracks,
              { ...emptyTrack(), uid, startDate: r.startDate, scoringStartDate: r.endDate },
            ],
          }
          : r,
      ),
    }));
    setOpenSections((prev) => new Set(prev).add(`track-${uid}`)); // hạng mục mới tự mở
  };

  const removeTrack = (ri: number, ti: number) =>
    setForm((f) => ({
      ...f,
      rounds: f.rounds.map((r, i) =>
        i === ri ? { ...r, tracks: r.tracks.filter((_, j) => j !== ti) } : r,
      ),
    }));

  const updateTrack = (ri: number, ti: number, patch: Partial<TrackForm>) =>
    setForm((f) => ({
      ...f,
      rounds: f.rounds.map((r, i) =>
        i === ri
          ? {
            ...r,
            tracks: r.tracks.map((t, j) => {
              if (j !== ti) return t;
              const nextTrack = { ...t, ...patch };
              // Kết thúc nộp bài = bắt đầu chấm điểm, cho tới khi admin đặt ngoại lệ.
              if (
                patch.endDate !== undefined &&
                (!t.scoringStartDate || t.scoringStartDate === t.endDate)
              ) {
                nextTrack.scoringStartDate = patch.endDate;
              }
              return nextTrack;
            }),
          }
          : r,
      ),
    }));

  // `templateId` sent as null when unset — empty string makes the backend throw a 500.
  // For invite-based flow, create-event payload should only build the event/round/track structure.
  // Judge/mentor role creation and mail sending happen later through invite APIs.
  function buildPayload(): CreateEventPayload {
    const hasCompleteRegistration = Boolean(
      form.registrationStartDate && form.registrationEndDate,
    );
    return {
      eventName: form.eventName.trim(),
      ...(form.maxTeams ? { maxTeams: Number(form.maxTeams) } : {}),
      season: form.season.trim(),
      year: Number(form.year) || 0,
      startDate: toIso(form.startDate),
      endDate: toIso(form.endDate),
      registrationStartDate: hasCompleteRegistration
        ? toIso(form.registrationStartDate) || null
        : null,
      registrationEndDate: hasCompleteRegistration
        ? toIso(form.registrationEndDate) || null
        : null,
      description: form.description.trim(),
      status: form.status,
      photoEventUrl: form.photoEventUrl || null,
      // Rounds and tracks are optional while an event is being created. Only
      // include entries that are complete enough for the backend contract;
      // untouched draft rows must not make the event creation fail.
      rounds: form.rounds
        .filter((r) =>
          r.roundName.trim() &&
          r.startDate &&
          r.endDate &&
          r.advancementRule.trim(),
        )
        .map((r, ri) => ({
          roundName: r.roundName.trim(),
          roundNumber: ri + 1,
          startDate: toIso(r.startDate),
          endDate: toIso(r.endDate),
          advancementRule: r.advancementRule.trim(),
          tracks: r.tracks.filter((t) => t.trackName.trim()).map((t) => ({
            trackName: t.trackName.trim(),
            description: t.description.trim(),
            templateId: t.templateId.trim() || null,
            submissionRuleDescription: serializeSubmissionRequirements(t.submissionRequirements),
            startDate: toIso(t.startDate) || null,
            endDate: toIso(t.endDate) || null,
            scoringStartDate: toIso(t.scoringStartDate) || null,
            scoringEndDate: toIso(t.scoringEndDate) || null,
          })),
        })),
    };
  }

  function validate(referenceTime?: number): string[] {
    const errors: string[] = [];
    const addError = (message: string) => {
      if (!errors.includes(message)) errors.push(message);
    };

    if (!form.eventName.trim()) addError("Vui lòng nhập tên sự kiện.");
    if (!form.startDate) addError("Vui lòng chọn ngày bắt đầu sự kiện.");
    if (!form.endDate) addError("Vui lòng chọn ngày kết thúc sự kiện.");

    // Khi tạo mới, các trường ngoài ba trường trên là tùy chọn. Nếu người dùng
    // có nhập thời gian thì các quy tắc thời gian bên dưới vẫn phải hợp lệ.
    if (isEdit) {
      const maxTeams = Number(form.maxTeams);
      if (!Number.isInteger(maxTeams) || maxTeams <= 0) {
        addError("Số lượng đội tham gia phải là số nguyên dương.");
      }
    }

    const eventStart = new Date(toIso(form.startDate)).getTime();
    const eventEnd = new Date(toIso(form.endDate)).getTime();

    if (form.startDate && Number.isNaN(eventStart)) addError("Ngày bắt đầu sự kiện không hợp lệ.");
    if (form.endDate && Number.isNaN(eventEnd)) addError("Ngày kết thúc sự kiện không hợp lệ.");

    if (!isEdit) {
      const enteredDates: Array<[label: string, value: string]> = [
        ["Thời gian bắt đầu sự kiện", form.startDate],
        ["Thời gian kết thúc sự kiện", form.endDate],
        ["Thời gian mở đăng ký", form.registrationStartDate],
        ["Thời gian kết thúc đăng ký", form.registrationEndDate],
      ];
      form.rounds.forEach((round, roundIndex) => {
        const roundLabel = `Vòng ${roundIndex + 1}`;
        enteredDates.push(
          [`${roundLabel}: thời gian bắt đầu`, round.startDate],
          [`${roundLabel}: thời gian kết thúc`, round.endDate],
        );
        round.tracks.forEach((track, trackIndex) => {
          const trackLabel = `${roundLabel} – Hạng mục ${trackIndex + 1}`;
          enteredDates.push(
            [`${trackLabel}: mở nộp bài`, track.startDate],
            [`${trackLabel}: hạn nộp bài`, track.endDate],
            [`${trackLabel}: bắt đầu chấm điểm`, track.scoringStartDate],
            [`${trackLabel}: kết thúc chấm điểm`, track.scoringEndDate],
          );
        });
      });

      for (const [label, value] of enteredDates) {
        if (!value) continue;
        const time = new Date(toIso(value)).getTime();
        if (Number.isNaN(time)) addError(`${label} không hợp lệ.`);
        else if (referenceTime !== undefined && time < referenceTime) {
          addError(`${label} không thể nằm trong quá khứ.`);
        }
      }
    }

    if (Number.isFinite(eventStart) && Number.isFinite(eventEnd) && eventEnd <= eventStart) {
      addError("Ngày kết thúc sự kiện phải sau ngày bắt đầu.");
    }

    const isWithinEvent = (value: string) => {
      if (!Number.isFinite(eventStart) || !Number.isFinite(eventEnd)) return true;
      const time = new Date(value).getTime();
      return time >= eventStart && time <= eventEnd;
    };

    // Các mốc đăng ký là tùy chọn, nhưng nếu có thì phải nằm trọn trong
    // khoảng thời gian diễn ra sự kiện.
    const registrationWasEdited =
      touchedFields.has("registrationStartDate") ||
      touchedFields.has("registrationEndDate");
    if (
      !!form.registrationStartDate !== !!form.registrationEndDate &&
      (isEdit || registrationWasEdited)
    ) {
      addError("Vui lòng nhập đủ thời gian mở và kết thúc đăng ký.");
    }
    if (form.registrationStartDate && form.registrationEndDate) {
      const registrationStart = new Date(toIso(form.registrationStartDate)).getTime();
      const registrationEnd = new Date(toIso(form.registrationEndDate)).getTime();

      if (Number.isNaN(registrationStart) || Number.isNaN(registrationEnd)) {
        addError("Thời gian đăng ký không hợp lệ.");
      }
      if (registrationEnd <= registrationStart) {
        addError("Thời gian kết thúc đăng ký phải sau thời gian mở đăng ký.");
      }
      if (registrationStart < eventStart || registrationEnd > eventEnd) {
        addError("Thời gian đăng ký phải nằm trong khoảng thời gian diễn ra sự kiện.");
      }
    }

    for (let i = 0; i < form.rounds.length; i++) {
      const round = form.rounds[i];
      const roundLabel = `Vòng ${i + 1}`;
      if (!!round.startDate !== !!round.endDate) {
        addError(`${roundLabel}: vui lòng nhập đủ thời gian bắt đầu và kết thúc.`);
      }

      if (round.startDate && !isWithinEvent(round.startDate)) {
        addError(`${roundLabel}: thời gian bắt đầu phải nằm trong khoảng thời gian diễn ra sự kiện.`);
      }
      if (round.endDate && !isWithinEvent(round.endDate)) {
        addError(`${roundLabel}: thời gian kết thúc phải nằm trong khoảng thời gian diễn ra sự kiện.`);
      }
      if (
        round.startDate &&
        round.endDate &&
        new Date(round.endDate).getTime() <= new Date(round.startDate).getTime()
      ) {
        addError(`${roundLabel}: thời gian kết thúc phải sau thời gian bắt đầu.`);
      }

      if (
        i === 0 &&
        round.startDate &&
        form.registrationEndDate &&
        new Date(round.startDate).getTime() < new Date(form.registrationEndDate).getTime()
      ) {
        addError("Thời gian bắt đầu Vòng 1 phải sau thời gian kết thúc đăng ký.");
      }
      const previousRoundEnd = i > 0 ? form.rounds[i - 1].endDate : "";
      if (
        i > 0 &&
        round.startDate &&
        previousRoundEnd &&
        new Date(round.startDate).getTime() < new Date(previousRoundEnd).getTime()
      ) {
        addError(`Thời gian bắt đầu ${roundLabel} phải sau khi Vòng ${i} kết thúc.`);
      }

      for (let j = 0; j < round.tracks.length; j++) {
        const track = round.tracks[j];
        const trackLabel = `${roundLabel} – Hạng mục ${j + 1}`;
        if (!!track.startDate !== !!track.endDate) {
          addError(`${trackLabel}: vui lòng nhập đủ thời gian mở và hạn nộp bài.`);
        }
        if (!!track.scoringStartDate !== !!track.scoringEndDate) {
          addError(`${trackLabel}: vui lòng nhập đủ thời gian bắt đầu và kết thúc chấm điểm.`);
        }

        if (isEdit && track.trackName.trim()) {
          const requirements = track.submissionRequirements;
          const hasFixedRequirement =
            requirements.repo || requirements.demo || requirements.reportSlide;
          const hasOtherRequirement =
            requirements.otherEnabled && requirements.otherText.trim().length > 0;

          if (requirements.otherEnabled && !requirements.otherText.trim()) {
            addError(`${trackLabel}: vui lòng nhập nội dung cho yêu cầu “Khác”.`);
          }
          if (!hasFixedRequirement && !hasOtherRequirement) {
            addError(`${trackLabel}: vui lòng chọn ít nhất một yêu cầu nộp bài.`);
          }
        }

        const trackTimes = [
          track.startDate,
          track.endDate,
          track.scoringStartDate,
          track.scoringEndDate,
        ].filter(Boolean);

        // Nếu nhập bất kỳ mốc nào của hạng mục, cần có đủ khung thời gian vòng
        // để bảo đảm các mốc nộp bài/chấm điểm thuộc đúng vòng đó.
        if (trackTimes.length && (!round.startDate || !round.endDate)) {
          addError(`${trackLabel}: cần nhập thời gian bắt đầu và kết thúc của vòng trước.`);
        }

        if (trackTimes.length) {
          const roundStart = new Date(round.startDate).getTime();
          const roundEnd = new Date(round.endDate).getTime();
          if (trackTimes.some((value) => {
            const time = new Date(value).getTime();
            return time < roundStart || time > roundEnd;
          })) {
            addError(`${trackLabel}: thời gian nộp bài và chấm điểm phải nằm trong thời gian của vòng.`);
          }
        }
        if (
          track.startDate &&
          track.endDate &&
          new Date(track.endDate).getTime() <= new Date(track.startDate).getTime()
        ) {
          addError(`${trackLabel}: hạn nộp bài phải sau thời gian mở nộp bài.`);
        }
        if (
          track.scoringStartDate &&
          track.scoringEndDate &&
          new Date(track.scoringEndDate).getTime() <= new Date(track.scoringStartDate).getTime()
        ) {
          addError(`${trackLabel}: thời gian kết thúc chấm điểm phải sau thời gian bắt đầu chấm điểm.`);
        }
      }
    }

    return errors;
  }

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    const referenceTime = Date.now();
    const errors = validate(referenceTime);
    setShowValidation(true);
    setValidationReferenceTime(referenceTime);
    if (errors.length) return;

    if (isEdit) editMutation.mutate();
    else createMutation.mutate(buildPayload());
  }

  // Sau lần submit đầu tiên, lỗi được tính lại ở mỗi lần form thay đổi để
  // danh sách biến mất/cập nhật ngay khi người dùng sửa đúng input.
  const formErrors = showValidation
    ? validate(validationReferenceTime ?? undefined)
    : [];
  const liveErrors = showValidation ? formErrors : validate();
  const inlineErrors = (keys: string[], predicate: (message: string) => boolean) =>
    showValidation || keys.some((key) => touchedFields.has(key))
      ? liveErrors.filter(predicate)
      : [];

  // ── Cảnh báo (chấm đỏ) cho header các khối accordion ──
  const infoWarn =
    !form.eventName.trim() ||
    !form.startDate ||
    !form.endDate ||
    (isEdit && !form.maxTeams);
  const regWarn = false;

  return (
    <form className="create-event-form" onSubmit={handleSubmit} style={{ display: "flex", flexDirection: "column", gap: "var(--space-xl)" }}>
      {/* Các khối accordion (mở nhiều tùy ý). Nút hành động cách xa bên dưới. */}
      <div style={{ display: "flex", flexDirection: "column", gap: "var(--space-sm)" }}>

        {/* Khối 1: Thông tin sự kiện */}
        <AccordionSection
          title="Thông tin sự kiện"
          warn={infoWarn}
          accent={SECTION_ACCENTS[0]}
          inset
          open={openSections.has("info")}
          onToggle={() => toggleSection("info")}
        >
          {/* Thứ tự khớp trang chi tiết: ảnh → tên → mô tả → thời gian */}
          <EventPhotoUpload
            value={form.photoEventUrl}
            onChange={(url) => setForm(f => ({ ...f, photoEventUrl: url }))}
          />

          {/* Tên & mô tả căn giữa, không nhãn — vị trí giống title/desc ở trang chi tiết */}
          <div>
            <input
              className="text-input"
              value={form.eventName}
              placeholder="Tên sự kiện"
              aria-invalid={inlineErrors(["eventName"], (error) => error.includes("tên sự kiện")).length > 0}
              onChange={(e) => setField("eventName", e.target.value)}
              onBlur={() => touchFields("eventName")}
              style={{ textAlign: "center", fontSize: "1.75rem", fontWeight: 700 }}
            />
            <InlineErrors errors={inlineErrors(["eventName"], (error) => error.includes("tên sự kiện"))} />
          </div>
          <textarea
            className="text-input"
            rows={2}
            value={form.description}
            placeholder="Mô tả sự kiện"
            onChange={(e) => setField("description", e.target.value)}
            style={{ textAlign: "center", height: "auto", resize: "vertical", fontFamily: "inherit", width: "100%" }}
          />

          <div className="flex flex-col sm:flex-row sm:items-center gap-1 sm:gap-3">
            <span className="t-body-strong text-ink shrink-0 tracking-wider sm:w-40">
              Thời gian sự kiện
            </span>
            <div className="flex-1 min-w-0">
              <DateRangeInput
                start={form.startDate}
                end={form.endDate}
                onStart={(v) => setField("startDate", v)}
                onEnd={(v) => setField("endDate", v)}
                onStartBlur={() => touchFields("startDate")}
                onEndBlur={() => touchFields("endDate")}
                startErrors={inlineErrors(
                  ["startDate"],
                  (error) => error.toLowerCase().includes("bắt đầu sự kiện"),
                )}
                endErrors={inlineErrors(
                  ["endDate"],
                  (error) => error.toLowerCase().includes("kết thúc sự kiện"),
                )}
              />
            </div>
          </div>

          {/* Mùa + Năm — suy tự động từ ngày bắt đầu; BE là nguồn chính, chỉ hiển thị. */}
          <div style={{ display: "flex", gap: "var(--space-xl)", flexWrap: "wrap" }}>
            <div style={{ display: "flex", flexDirection: "column", gap: 4 }}>
              <span className="t-body-strong" style={{ color: "var(--color-ink)", letterSpacing: "0.05em" }}>Mùa</span>
              <span className="t-body-sm" style={{ color: form.season ? "var(--color-ink)" : "var(--color-mute)", fontWeight: 600 }}>
                {form.season || "—"}
              </span>
            </div>
            <div style={{ display: "flex", flexDirection: "column", gap: 4 }}>
              <span className="t-body-strong" style={{ color: "var(--color-ink)", letterSpacing: "0.05em" }}>Năm</span>
              <span className="t-body-sm" style={{ color: form.year ? "var(--color-ink)" : "var(--color-mute)", fontWeight: 600 }}>
                {form.year || "—"}
              </span>
            </div>
          </div>

          <div className="flex flex-col sm:flex-row sm:items-center gap-1 sm:gap-3">
            <label htmlFor="event-max-teams" className="t-body-strong text-ink shrink-0 tracking-wider sm:w-40">
              Số đội tối đa
            </label>
            <div className="flex-1 min-w-0">
              <input
                id="event-max-teams"
                className="text-input w-full"
                type="number"
                min="1"
                step="1"
                inputMode="numeric"
                value={form.maxTeams}
                placeholder="Nhập số đội tối đa"
                onChange={(e) => setField("maxTeams", e.target.value)}
                onBlur={() => touchFields("maxTeams")}
              />
              <InlineErrors errors={inlineErrors(["maxTeams"], (error) => error.includes("Số lượng đội"))} />
            </div>
          </div>

          <div style={{ display: "flex", flexDirection: "column", gap: 6 }}>
            <span className="t-body-strong" style={{ color: "var(--color-ink)", letterSpacing: "0.05em" }}>
              Trạng thái hiển thị
            </span>
            <div style={{ display: 'flex', alignItems: 'center', gap: 12, marginTop: 4 }}>
              <button
                type="button"
                onClick={() => setStatus(!form.status)}
                style={{
                  width: 48,
                  height: 24,
                  background: form.status ? 'var(--color-primary)' : 'var(--color-surface-elevated)',
                  border: '1px solid var(--color-hairline-strong)',
                  borderRadius: 2,
                  position: 'relative',
                  cursor: 'pointer',
                  transition: 'background-color 150ms ease',
                  padding: 0,
                }}
                aria-label={form.status ? "Ẩn sự kiện" : "Hiện sự kiện"}
              >
                {/* transform:translateX thay vì left — animate trên GPU (compositor), tránh reflow mỗi frame. */}
                <span
                  style={{
                    width: 18,
                    height: 18,
                    background: '#fff',
                    borderRadius: 1,
                    position: 'absolute',
                    top: 2,
                    left: 2,
                    transform: `translateX(${form.status ? 24 : 0}px)`,
                    transition: 'transform 150ms ease',
                  }}
                />
              </button>
              <span style={{ fontSize: 'var(--fs-body-sm)', fontWeight: 700, color: form.status ? 'var(--color-primary)' : 'var(--color-mute)' }}>
                {form.status ? 'HIỆN' : 'ẨN'}
              </span>
            </div>
          </div>
          {/* <Hint>Sự kiện tự chuyển sang “Đã kết thúc” (vẫn hiện cho mọi người) sau ngày kết thúc.</Hint> */}
        </AccordionSection>

        {/* Khối 2: Đăng ký */}
        <AccordionSection
          title="Đăng ký"
          warn={regWarn}
          accent={SECTION_ACCENTS[1]}
          inset
          open={openSections.has("reg")}
          onToggle={() => toggleSection("reg")}
        >
          <div className="flex flex-col sm:flex-row sm:items-center gap-1 sm:gap-3">
            <span className="t-body-strong text-ink shrink-0 tracking-wider sm:w-40">Thời gian đăng ký</span>
            <div className="flex-1 min-w-0">
              <DateRangeInput
                start={form.registrationStartDate} end={form.registrationEndDate}
                onStart={(v) => setField("registrationStartDate", v)} onEnd={(v) => setField("registrationEndDate", v)}
                onStartBlur={() => touchFields("registrationStartDate")}
                onEndBlur={() => touchFields("registrationEndDate")}
                errors={inlineErrors(
                  ["registrationStartDate", "registrationEndDate"],
                  (error) => error.toLowerCase().includes("đăng ký"),
                )}
              />
            </div>
          </div>
        </AccordionSection>

        {/* Khối 3..N: từng Vòng */}
        {form.rounds.map((round, ri) => {
          const { type, value } = parseAdvancementRule(round.advancementRule);
          const roundLocked = !!round.id && isTimeLocked(round.endDate);
          return (
            <AccordionSection
              key={round.uid}
              title={`Vòng ${ri + 1}${round.roundName.trim() ? ": " + round.roundName.trim() : ""}`}
              warn={false}
              accent={SECTION_ACCENTS[(2 + ri) % SECTION_ACCENTS.length]}
              open={openSections.has(`round-${round.uid}`)}
              onToggle={() => toggleSection(`round-${round.uid}`)}
              actions={form.rounds.length > 1 && !roundLocked ? (
                <button type="button" className="btn btn-delete btn-sm" style={{ cursor: "pointer" }} onClick={() => removeRound(ri)}>Xóa vòng</button>
              ) : undefined}
            >
              <div className="flex flex-col sm:flex-row sm:items-center gap-1 sm:gap-3 sm:pl-5 sm:pr-4.25">
                <span className="t-body-strong text-ink shrink-0 tracking-wider sm:w-40">Thời gian vòng {ri + 1}</span>
                <div className="flex-1 min-w-0">
                  <DateRangeInput
                    start={round.startDate} end={round.endDate}
                    disabled={roundLocked}
                    onStart={(v) => updateRound(ri, "startDate", v)} onEnd={(v) => updateRound(ri, "endDate", v)}
                    onStartBlur={() => touchFields(`round.${ri}.startDate`)}
                    onEndBlur={() => touchFields(`round.${ri}.endDate`)}
                    errors={inlineErrors(
                      [`round.${ri}.startDate`, `round.${ri}.endDate`],
                      (error) =>
                        error.startsWith(`Vòng ${ri + 1}:`) ||
                        error.includes(`bắt đầu Vòng ${ri + 1}`),
                    )}
                  />
                </div>
              </div>
              <div className="flex flex-col sm:flex-row sm:items-center gap-1 sm:gap-3 sm:pl-5 sm:pr-4.25">
                <span className="t-body-strong text-ink shrink-0 tracking-wider sm:w-40">Tên vòng</span>
                <div className="flex-1 min-w-0 flex items-center gap-2">
                  <input className="text-input min-w-0" style={{ flex: "0.93 1 0%" }} value={round.roundName}
                    disabled={roundLocked}
                    placeholder={`Tên vòng ${ri + 1}`} onChange={(e) => updateRound(ri, "roundName", e.target.value)} />
                  <span className="t-body-sm" style={{ flexShrink: 0, visibility: "hidden" }} aria-hidden>→</span>
                  <div className="flex-1 min-w-0 flex items-center gap-2">
                    <span className="t-body-strong text-ink shrink-0 tracking-wider">Lên vòng</span>
                    <select className="text-input flex-1 min-w-0" value={type}
                      disabled={roundLocked}
                      onChange={(e) => { const nt = e.target.value; const dv = nt === "minScore" ? "7.5" : nt === "percent" ? "50" : "10"; updateRound(ri, "advancementRule", `${nt}:${dv}`); }}>
                      <option value="top">Top số đội</option>
                      <option value="percent">Phần trăm</option>
                      <option value="minScore">Điểm tối thiểu</option>
                    </select>
                    <input className="text-input shrink-0" style={{ width: 90 }} type="number" value={value}
                      disabled={roundLocked}
                      onChange={(e) => updateRound(ri, "advancementRule", `${type}:${e.target.value}`)} />
                  </div>
                </div>
              </div>
              <div className="flex flex-col gap-2">
                {round.tracks.map((track, ti) => {
                  return (
                  <AccordionSection
                    key={track.uid}
                    title={`Hạng mục ${ti + 1}${track.trackName.trim() ? ": " + track.trackName.trim() : ""}`}
                    warn={false}
                    accent="var(--color-ink)"
                    open={openSections.has(`track-${track.uid}`)}
                    onToggle={() => toggleSection(`track-${track.uid}`)}
                    actions={round.tracks.length > 1 && !roundLocked ? (
                      <button type="button" className="btn btn-delete btn-sm" style={{ cursor: "pointer" }} onClick={() => removeTrack(ri, ti)}>Xóa</button>
                    ) : undefined}
                  >
                    <TrackFormCard
                      track={track}
                      index={ti}
                      templates={templates}
                      templatesLoading={templatesQuery.isLoading}
                      disabled={roundLocked}
                      onFieldBlur={(field) => touchFields(`round.${ri}.track.${ti}.${field}`)}
                      validationErrors={inlineErrors(
                        [
                          `round.${ri}.track.${ti}.startDate`,
                          `round.${ri}.track.${ti}.endDate`,
                          `round.${ri}.track.${ti}.scoringStartDate`,
                          `round.${ri}.track.${ti}.scoringEndDate`,
                          `round.${ri}.track.${ti}.submissionRequirements`,
                        ],
                        (error) => error.startsWith(`Vòng ${ri + 1} – Hạng mục ${ti + 1}:`),
                      )}
                      onChange={(patch) => updateTrack(ri, ti, patch)}
                    />
                  </AccordionSection>
                  );
                })}
                <button type="button" className="btn btn-create btn-sm" disabled={roundLocked} style={{ alignSelf: "flex-start", cursor: roundLocked ? "not-allowed" : "pointer" }} onClick={() => addTrack(ri)}>
                  + Thêm hạng mục
                </button>
              </div>
            </AccordionSection>
          );
        })}

        {/* + Thêm vòng */}
        <button type="button" className="btn btn-create btn-sm" style={{ alignSelf: "flex-start", cursor: "pointer" }} onClick={addRound}>
          + Thêm vòng
        </button>
      </div>

      {/* Actions */}
      <div style={{ display: "flex", gap: "var(--space-md)" }}>
        <button
          type="submit"
          className={`btn ${isEdit ? 'btn-update' : 'btn-create'}`}
          disabled={activeMutation.isPending}
          style={{
            cursor: activeMutation.isPending ? "not-allowed" : "pointer",
            opacity: activeMutation.isPending ? 0.6 : 1,
          }}
        >
          {isEdit
            ? activeMutation.isPending ? "Đang lưu..." : "Lưu thay đổi"
            : activeMutation.isPending ? "Đang tạo..." : "Tạo sự kiện"}
        </button>
        <button type="button" className="btn btn-outline" onClick={onCancel} style={{ cursor: "pointer" }}>
          {activeMutation.isSuccess ? "Đóng" : "Hủy"}
        </button>
      </div>

      {/* Success */}
      {activeMutation.isSuccess && (
        <div
          role="status"
          style={{
            padding: "var(--space-md)",
            background: "var(--color-surface-soft)",
            border: "1px solid var(--color-primary)",
            borderRadius: "var(--radius-sm)",
          }}
          className="t-body-sm"
        >
          {isEdit ? (
            <>Đã cập nhật sự kiện thành công.</>
          ) : (
            <>
              Đã tạo sự kiện{" "}
              <strong>{createMutation.data?.eventName || form.eventName}</strong> thành công.
            </>
          )}
        </div>
      )}
    </form>
  );
}
