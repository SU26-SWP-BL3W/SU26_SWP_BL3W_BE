'use client';

import React, { createContext, useCallback, useContext, useMemo, useRef, useState } from 'react';

/** 's' = thông báo thành công · 'e' = thông báo lỗi · 'w' = CẢNH BÁO (rủi ro/cần chú ý). */
type NotifType = 's' | 'e' | 'w';

interface NotifItem {
  id: number;
  m: string;
  t: NotifType;
}

interface NotifyApi {
  /** Thông báo thành công (xanh). */
  success: (message: string) => void;
  /** Thông báo lỗi (đỏ). */
  error: (message: string) => void;
  /** Cảnh báo (vàng) — dùng cho thao tác rủi ro / thiếu điều kiện, KHÁC với thông báo. */
  warning: (message: string) => void;
  /** Hiện toast với loại chỉ định ('s' | 'e' | 'w'). */
  show: (message: string, type?: NotifType) => void;
}

const NotificationContext = createContext<NotifyApi | null>(null);

const AUTO_DISMISS_MS = 3000;
// Cảnh báo ở lâu hơn thông báo để người dùng kịp đọc & cân nhắc.
const WARNING_DISMISS_MS = 6000;

/** Kiểu hiển thị theo loại: màu nền, màu chữ, icon và vai trò accessibility. */
const STYLE: Record<NotifType, { bg: string; fg: string; icon: string; role: 'status' | 'alert' }> = {
  s: { bg: '#3ddc84', fg: '#000000', icon: '✓', role: 'status' },
  e: { bg: '#ff4d6d', fg: '#000000', icon: '✕', role: 'alert' },
  w: { bg: '#ffb020', fg: '#1a1a1a', icon: '⚠', role: 'alert' },
};

export function NotificationProvider({ children }: { children: React.ReactNode }) {
  const [items, setItems] = useState<NotifItem[]>([]);
  const nextId = useRef(1);

  const remove = useCallback((id: number) => {
    setItems((prev) => prev.filter((n) => n.id !== id));
  }, []);

  const show = useCallback(
    (message: string, type: NotifType = 's') => {
      if (!message) return;
      const id = nextId.current++;
      setItems((prev) => [...prev, { id, m: message, t: type }]);
      setTimeout(() => remove(id), type === 'w' ? WARNING_DISMISS_MS : AUTO_DISMISS_MS);
    },
    [remove],
  );

  // `show` is stable (memoised), so this api object is stable too.
  const api = useMemo<NotifyApi>(
    () => ({
      success: (m: string) => show(m, 's'),
      error: (m: string) => show(m, 'e'),
      warning: (m: string) => show(m, 'w'),
      show,
    }),
    [show],
  );

  return (
    <NotificationContext.Provider value={api}>
      {children}
      <div
        style={{
          position: 'fixed',
          top: 20,
          right: 20,
          zIndex: 9999,
          display: 'flex',
          flexDirection: 'column',
          gap: 10,
          pointerEvents: 'none',
        }}
      >
        {items.map((n) => {
          const s = STYLE[n.t];
          return (
            <div
              key={n.id}
              className="animate-slideDown"
              role={s.role}
              onClick={() => remove(n.id)}
              style={{
                display: 'flex',
                alignItems: 'center',
                gap: 8,
                background: s.bg,
                color: s.fg,
                padding: '12px 20px',
                fontWeight: 700,
                fontSize: 13,
                fontFamily: 'Inter,sans-serif',
                borderRadius: 6,
                maxWidth: 360,
                boxShadow: '0 4px 24px rgba(0,0,0,.5)',
                cursor: 'pointer',
                pointerEvents: 'auto',
              }}
            >
              <span aria-hidden style={{ fontSize: 15, lineHeight: 1 }}>{s.icon}</span>
              <span>{n.m}</span>
            </div>
          );
        })}
      </div>
    </NotificationContext.Provider>
  );
}

/**
 * Access the global notification API. Must be used under <NotificationProvider>
 * (mounted once in the root layout).
 */
export function useNotify(): NotifyApi {
  const ctx = useContext(NotificationContext);
  if (!ctx) {
    throw new Error('useNotify must be used within a NotificationProvider');
  }
  return ctx;
}
