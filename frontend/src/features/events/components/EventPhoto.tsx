'use client';

import { useEffect, useState } from 'react';
import { createPortal } from 'react-dom';

/**
 * Ảnh hero của sự kiện (tỉ lệ 2.35:1), giới hạn bề rộng cho vừa mắt (không full
 * cả trang). Bấm vào ảnh để phóng to (lightbox). Khi không có URL hoặc ảnh lỗi
 * tải, hiển thị placeholder căn giữa thay vì để chữ `alt` dồn ở góc trên trái.
 */
export function EventPhoto({ url, alt }: { url?: string | null; alt: string }) {
  const [errored, setErrored] = useState(false);
  const [zoom, setZoom] = useState(false);
  const ok = Boolean(url) && !errored;

  useEffect(() => {
    if (!zoom) return;
    const onEsc = (e: KeyboardEvent) => {
      if (e.key === 'Escape') setZoom(false);
    };
    document.addEventListener('keydown', onEsc);
    document.body.style.overflow = 'hidden';
    return () => {
      document.removeEventListener('keydown', onEsc);
      document.body.style.overflow = '';
    };
  }, [zoom]);

  return (
    <>
      <div className="w-full max-w-2xl mx-auto aspect-[2.35/1] rounded-sm overflow-hidden relative bg-surface-soft flex items-center justify-center text-center">
        {ok ? (
          <img
            src={url as string}
            alt={alt}
            className="w-full h-full object-cover cursor-zoom-in"
            onError={() => setErrored(true)}
            onClick={() => setZoom(true)}
          />
        ) : (
          <span className="t-body-sm text-mute px-4">Chưa có ảnh sự kiện</span>
        )}
      </div>

      {zoom && ok && typeof document !== 'undefined' &&
        createPortal(
          <div
            role="dialog"
            aria-modal="true"
            aria-label={alt}
            onClick={() => setZoom(false)}
            style={{ position: 'fixed', inset: 0, zIndex: 1000, display: 'flex', alignItems: 'center', justifyContent: 'center', padding: 16, background: 'rgba(0,0,0,0.75)', cursor: 'zoom-out' }}
          >
            <img
              src={url as string}
              alt={alt}
              style={{ maxWidth: '92vw', maxHeight: '88vh', objectFit: 'contain', borderRadius: 'var(--radius-sm)' }}
            />
          </div>,
          document.body,
        )}
    </>
  );
}
