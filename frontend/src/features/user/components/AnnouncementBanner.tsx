"use client";

import { useState, useSyncExternalStore } from "react";
import Link from "next/link";
import { useUserProfile } from "../hooks/useUserProfile";

export function AnnouncementBanner() {
  const { data: profile } = useUserProfile();
  const ann = profile?.announcement;
  const [manuallyDismissed, setManuallyDismissed] = useState(false);

  const storageKey = ann ? `dismissed-announcement-${ann.id}` : null;
  const storedDismissed = useSyncExternalStore(
    () => () => {},
    () => (storageKey ? localStorage.getItem(storageKey) === "true" : false),
    () => false,
  );

  if (!ann || manuallyDismissed || storedDismissed) return null;

  function dismiss() {
    localStorage.setItem(storageKey!, "true");
    setManuallyDismissed(true);
  }

  return (
    <div
      role="banner"
      style={{
        display: "flex",
        alignItems: "center",
        gap: 12,
        padding: "12px 48px",
        background: "rgba(118,185,0,0.06)",
        borderLeft: "3px solid var(--color-primary)",
        borderBottom: "var(--border-hairline)",
      }}
    >
      <div style={{ width: 6, height: 6, background: "var(--color-primary)", flexShrink: 0 }} />
      <p style={{ margin: 0, flex: 1, fontSize: "var(--fs-body-sm)", color: "var(--color-body-text)", lineHeight: "var(--lh-body)" }}>
        {ann.text}
      </p>
      {ann.ctaLabel && ann.ctaUrl && (
        <Link
          href={ann.ctaUrl}
          style={{ fontSize: "var(--fs-caption-md)", fontWeight: 700, color: "var(--color-primary)", textDecoration: "none", whiteSpace: "nowrap" }}
        >
          {ann.ctaLabel} →
        </Link>
      )}
      <button
        onClick={dismiss}
        aria-label="Đóng thông báo"
        style={{
          background: "none",
          border: "none",
          cursor: "pointer",
          color: "var(--color-mute)",
          fontSize: 18,
          lineHeight: 1,
          padding: "4px 6px",
          flexShrink: 0,
        }}
      >
        ×
      </button>
    </div>
  );
}
