'use client';

import React, { createContext, useCallback, useContext, useMemo, useRef, useState } from 'react';

/**
 * Hộp thoại XÁC NHẬN / NHẬP LIỆU dùng chung cho toàn app — thay cho window.confirm
 * và window.prompt (hộp mặc định của trình duyệt xấu và lệch giao diện).
 *
 * Cách dùng:
 *   const dialog = useDialog();
 *   const ok = await dialog.confirm({ title: 'Xóa sự kiện', message: '...', danger: true });
 *   const reason = await dialog.prompt({ title: 'Từ chối hồ sơ', label: 'Lý do', required: true });
 *   // confirm -> true/false · prompt -> chuỗi đã trim, hoặc null nếu bấm Hủy
 */

interface ConfirmOptions {
  title: string;
  /** Nội dung mô tả — hỗ trợ xuống dòng bằng \n. */
  message?: string;
  confirmText?: string;
  cancelText?: string;
  /** true = hành động nguy hiểm → icon cảnh báo + nút xác nhận màu đỏ. */
  danger?: boolean;
}

interface PromptOptions extends ConfirmOptions {
  /** Nhãn của ô nhập. */
  label?: string;
  placeholder?: string;
  defaultValue?: string;
  /** true = bắt buộc nhập, để trống sẽ không xác nhận được. */
  required?: boolean;
  /** true = ô nhập nhiều dòng (textarea). */
  multiline?: boolean;
}

interface DialogApi {
  confirm: (opts: ConfirmOptions) => Promise<boolean>;
  prompt: (opts: PromptOptions) => Promise<string | null>;
}

type Pending =
  | { kind: 'confirm'; opts: ConfirmOptions; resolve: (v: boolean) => void }
  | { kind: 'prompt'; opts: PromptOptions; resolve: (v: string | null) => void };

const DialogContext = createContext<DialogApi | null>(null);

export function ConfirmDialogProvider({ children }: { children: React.ReactNode }) {
  const [pending, setPending] = useState<Pending | null>(null);
  const [value, setValue] = useState('');
  const [showRequired, setShowRequired] = useState(false);
  const cardRef = useRef<HTMLDivElement>(null);

  const confirm = useCallback(
    (opts: ConfirmOptions) =>
      new Promise<boolean>((resolve) => {
        setValue('');
        setShowRequired(false);
        setPending({ kind: 'confirm', opts, resolve });
      }),
    [],
  );

  const prompt = useCallback(
    (opts: PromptOptions) =>
      new Promise<string | null>((resolve) => {
        setValue(opts.defaultValue ?? '');
        setShowRequired(false);
        setPending({ kind: 'prompt', opts, resolve });
      }),
    [],
  );

  const api = useMemo<DialogApi>(() => ({ confirm, prompt }), [confirm, prompt]);

  /** Đóng hộp thoại và trả kết quả về cho lời gọi await. */
  const close = useCallback(
    (accepted: boolean) => {
      setPending((cur) => {
        if (!cur) return null;
        if (cur.kind === 'confirm') cur.resolve(accepted);
        else cur.resolve(accepted ? value.trim() : null);
        return null;
      });
    },
    [value],
  );

  const handleConfirm = useCallback(() => {
    if (pending?.kind === 'prompt' && pending.opts.required && !value.trim()) {
      setShowRequired(true);
      return;
    }
    close(true);
  }, [pending, value, close]);

  // Đưa focus vào hộp thoại khi mở: prompt -> ô nhập; confirm -> nút Hủy
  // (để lỡ tay Enter không kích hoạt nhầm hành động nguy hiểm).
  const focusOnMount = useCallback((node: HTMLDivElement | null) => {
    cardRef.current = node;
    if (!node) return;
    const target =
      node.querySelector<HTMLElement>('input, textarea') ??
      node.querySelector<HTMLElement>('button[data-cancel]');
    target?.focus();
  }, []);

  // Escape = Hủy · Tab bị bẫy trong hộp thoại (không lọt ra nền phía sau).
  const onKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Escape') {
      e.preventDefault();
      close(false);
      return;
    }
    if (e.key !== 'Tab' || !cardRef.current) return;
    const els = Array.from(
      cardRef.current.querySelectorAll<HTMLElement>('input, textarea, button'),
    ).filter((el) => !el.hasAttribute('disabled'));
    if (els.length === 0) return;
    const first = els[0];
    const last = els[els.length - 1];
    if (e.shiftKey && document.activeElement === first) {
      e.preventDefault();
      last.focus();
    } else if (!e.shiftKey && document.activeElement === last) {
      e.preventDefault();
      first.focus();
    }
  };

  const opts = pending?.opts;
  const isDanger = !!opts?.danger;
  const promptOpts = pending?.kind === 'prompt' ? (pending.opts as PromptOptions) : null;

  return (
    <DialogContext.Provider value={api}>
      {children}
      {pending && opts && (
        <div
          role="presentation"
          onKeyDown={onKeyDown}
          onMouseDown={(e) => {
            // Bấm ra vùng tối phía sau = Hủy
            if (e.target === e.currentTarget) close(false);
          }}
          style={{
            position: 'fixed',
            inset: 0,
            zIndex: 9998, // dưới toast (9999) để thông báo vẫn nổi lên trên
            background: 'rgba(0,0,0,0.55)',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            padding: 16,
          }}
        >
          <div
            ref={focusOnMount}
            role="dialog"
            aria-modal="true"
            aria-labelledby="app-dialog-title"
            className="animate-slideDown"
            style={{
              background: 'var(--color-canvas, #ffffff)',
              border: '1px solid var(--color-hairline, #e5e5e5)',
              borderRadius: 'var(--radius-sm, 6px)',
              width: '100%',
              maxWidth: 440,
              boxShadow: '0 12px 48px rgba(0,0,0,.35)',
            }}
          >
            <form
              onSubmit={(e) => {
                e.preventDefault();
                handleConfirm();
              }}
              style={{ padding: 24, display: 'flex', flexDirection: 'column', gap: 14 }}
            >
              {/* Tiêu đề + icon */}
              <div style={{ display: 'flex', alignItems: 'flex-start', gap: 12 }}>
                <span
                  aria-hidden
                  style={{
                    width: 36,
                    height: 36,
                    borderRadius: '50%',
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'center',
                    fontSize: 17,
                    fontWeight: 700,
                    flexShrink: 0,
                    background: isDanger ? 'rgba(255,77,109,.12)' : 'rgba(118,185,0,.14)',
                    color: isDanger ? 'var(--color-error, #e11d48)' : '#5a8d00',
                  }}
                >
                  {isDanger ? '⚠' : '?'}
                </span>
                <div style={{ minWidth: 0 }}>
                  <h2 id="app-dialog-title" className="font-bold" style={{ fontSize: 16, margin: 0 }}>
                    {opts.title}
                  </h2>
                  {opts.message && (
                    <p className="t-body-sm text-mute" style={{ whiteSpace: 'pre-line', margin: '6px 0 0' }}>
                      {opts.message}
                    </p>
                  )}
                </div>
              </div>

              {/* Ô nhập (chỉ với prompt) */}
              {promptOpts && (
                <label className="block">
                  {promptOpts.label && (
                    <span className="t-body-sm font-bold">
                      {promptOpts.label}
                      {promptOpts.required && <span style={{ color: 'var(--color-error, #e11d48)' }}> *</span>}
                    </span>
                  )}
                  {promptOpts.multiline ? (
                    <textarea
                      rows={3}
                      className="input w-full mt-1"
                      value={value}
                      placeholder={promptOpts.placeholder}
                      onChange={(e) => {
                        setValue(e.target.value);
                        if (showRequired) setShowRequired(false);
                      }}
                    />
                  ) : (
                    <input
                      type="text"
                      className="input w-full mt-1"
                      value={value}
                      placeholder={promptOpts.placeholder}
                      onChange={(e) => {
                        setValue(e.target.value);
                        if (showRequired) setShowRequired(false);
                      }}
                    />
                  )}
                  {showRequired && (
                    <span className="t-body-sm" style={{ color: 'var(--color-error, #e11d48)' }}>
                      Vui lòng nhập nội dung này.
                    </span>
                  )}
                </label>
              )}

              {/* Nút hành động */}
              <div style={{ display: 'flex', justifyContent: 'flex-end', gap: 8 }}>
                <button type="button" data-cancel className="btn" onClick={() => close(false)}>
                  {opts.cancelText ?? 'Hủy'}
                </button>
                <button
                  type="submit"
                  className={isDanger ? 'btn' : 'btn btn-primary'}
                  style={
                    isDanger
                      ? {
                          background: 'var(--color-error, #e11d48)',
                          borderColor: 'var(--color-error, #e11d48)',
                          color: '#ffffff',
                        }
                      : undefined
                  }
                >
                  {opts.confirmText ?? 'Xác nhận'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </DialogContext.Provider>
  );
}

/** Lấy API hộp thoại dùng chung. Phải nằm dưới <ConfirmDialogProvider> (đã gắn ở root layout). */
export function useDialog(): DialogApi {
  const ctx = useContext(DialogContext);
  if (!ctx) {
    throw new Error('useDialog must be used within a ConfirmDialogProvider');
  }
  return ctx;
}
