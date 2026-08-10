import React, { useId, useRef, useState } from "react";
import { CalendarDays } from "lucide-react";

interface Props {
  value: string;
  onChange: (isoValue: string) => void;
  placeholder?: string;
  id?: string;
  disabled?: boolean;
  onBlur?: () => void;
  onValidationChange?: (isValid: boolean) => void;
}

function toDisplayString(iso: string): string {
  if (!iso) return "";
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) return "";
  const pad = (part: number) => String(part).padStart(2, "0");
  return `${pad(date.getDate())}/${pad(date.getMonth() + 1)}/${date.getFullYear()} ${pad(date.getHours())}:${pad(date.getMinutes())}`;
}

function toNativeString(iso: string): string {
  if (!iso) return "";
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) return "";
  const offsetMs = date.getTimezoneOffset() * 60 * 1000;
  return new Date(date.getTime() - offsetMs).toISOString().slice(0, 16);
}

type ParseResult =
  | { iso: string; error: null }
  | { iso: null; error: string };

function parseDisplayString(value: string): ParseResult {
  const match = value.trim().match(
    /^(\d{2})\/(\d{2})\/(\d{4}) (\d{2}):(\d{2})$/,
  );
  if (!match) {
    return {
      iso: null,
      error: "Sai định dạng. Hãy nhập đủ dd/MM/yyyy HH:mm, ví dụ 31/07/2026 14:30.",
    };
  }

  const [, dayText, monthText, yearText, hourText, minuteText] = match;
  const day = Number(dayText);
  const month = Number(monthText);
  const year = Number(yearText);
  const hour = Number(hourText);
  const minute = Number(minuteText);

  if (hour < 0 || hour > 23 || minute < 0 || minute > 59) {
    return {
      iso: null,
      error: "Giờ không hợp lệ. Giờ phải từ 00–23 và phút từ 00–59.",
    };
  }

  const date = new Date(year, month - 1, day, hour, minute);
  const isExact =
    date.getFullYear() === year &&
    date.getMonth() === month - 1 &&
    date.getDate() === day &&
    date.getHours() === hour &&
    date.getMinutes() === minute;

  if (!isExact) {
    return {
      iso: null,
      error: "Ngày không tồn tại. Hãy kiểm tra ngày, tháng và năm đã nhập.",
    };
  }

  return { iso: date.toISOString(), error: null };
}

export function DateTimePicker({
  value,
  onChange,
  placeholder,
  id,
  disabled = false,
  onBlur,
  onValidationChange,
}: Props) {
  const generatedId = useId();
  const hintId = `${id ?? generatedId}-format-hint`;
  const pickerRef = useRef<HTMLInputElement>(null);
  const [draft, setDraft] = useState(() => toDisplayString(value));
  const [editing, setEditing] = useState(false);
  const [validationError, setValidationError] = useState<string | null>(null);

  const commit = (nextValue: string, keepEditing = false) => {
    if (!nextValue.trim()) {
      setValidationError(null);
      setEditing(keepEditing);
      onValidationChange?.(true);
      onChange("");
      return;
    }
    const result = parseDisplayString(nextValue);
    setValidationError(result.error);
    onValidationChange?.(Boolean(result.iso));
    if (result.iso) {
      setDraft(toDisplayString(result.iso));
      setEditing(keepEditing);
      onChange(result.iso);
    }
  };

  const openNativePicker = () => {
    const picker = pickerRef.current;
    if (!picker || disabled) return;
    try {
      picker.showPicker();
    } catch {
      picker.focus();
      picker.click();
    }
  };

  return (
    <div className="w-full">
      <div className="relative">
        <input
        type="text"
        id={id}
        className="text-input"
        style={{ width: "100%", paddingRight: 42 }}
        disabled={disabled}
        value={editing ? draft : toDisplayString(value)}
        inputMode="text"
        autoComplete="off"
        aria-invalid={Boolean(validationError)}
        aria-describedby={hintId}
        title={validationError ?? undefined}
        placeholder={placeholder || "dd/MM/yyyy HH:mm"}
        onFocus={() => {
          if (!editing) setDraft(toDisplayString(value));
          setEditing(true);
        }}
        onChange={(event) => {
          setDraft(event.target.value);
          setValidationError(null);
        }}
        onBlur={() => {
          commit(draft);
          onBlur?.();
        }}
        onKeyDown={(event) => {
          if (event.key === "Enter") {
            event.preventDefault();
            commit(draft, true);
            onBlur?.();
          }
        }}
        />
        <button
        type="button"
        className="absolute right-1 top-1/2 flex h-8 w-8 -translate-y-1/2 items-center justify-center rounded-xs text-mute transition-colors hover:bg-surface-soft hover:text-ink focus-visible:outline-2 focus-visible:outline-primary disabled:cursor-not-allowed disabled:opacity-40"
        disabled={disabled}
        aria-label="Mở lịch chọn ngày giờ"
        onClick={openNativePicker}
        >
          <CalendarDays size={17} aria-hidden="true" />
        </button>
        <input
        ref={pickerRef}
        type="datetime-local"
        tabIndex={-1}
        aria-hidden="true"
        className="pointer-events-none absolute bottom-0 right-0 h-px w-px opacity-0"
        disabled={disabled}
        value={toNativeString(value)}
        onChange={(event) => {
          const localValue = event.target.value;
          if (!localValue) {
            setDraft("");
            setEditing(false);
            setValidationError(null);
            onValidationChange?.(true);
            onChange("");
            return;
          }
          const date = new Date(localValue);
          if (!Number.isNaN(date.getTime())) {
            const iso = date.toISOString();
            setDraft(toDisplayString(iso));
            setEditing(false);
            setValidationError(null);
            onValidationChange?.(true);
            onChange(iso);
          }
        }}
        />
      </div>
      <p
        id={hintId}
        role={validationError ? "alert" : undefined}
        className={`mt-1 t-caption-sm ${validationError ? "text-error" : "text-mute"}`}
      >
        {validationError ?? ""}
      </p>
    </div>
  );
}
