"use client";

import { useState } from "react";
import { useUserProfile } from "../hooks/useUserProfile";

function InitialsFallback({ name, size = 48 }: { name: string; size?: number }) {
  const initials = name
    .split(" ")
    .map((w) => w[0])
    .slice(0, 2)
    .join("")
    .toUpperCase();
  return (
    <div
      style={{
        width: size,
        height: size,
        borderRadius: "var(--radius-full)",
        background: "var(--color-primary)",
        color: "#fff",
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        fontSize: size * 0.35,
        fontWeight: 700,
        flexShrink: 0,
      }}
      aria-hidden="true"
    >
      {initials}
    </div>
  );
}

function SkeletonPulse({ w, h }: { w: string; h: number }) {
  return (
    <div
      style={{
        width: w,
        height: h,
        background: "var(--color-surface-elevated)",
        borderRadius: "var(--radius-sm)",
        animation: "pulse 1.4s ease-in-out infinite",
      }}
    />
  );
}

/** The home header only distinguishes admin vs. regular user; detailed roles
 *  (student / mentor / judge) only matter inside an event. */
function homeRoleLabel(role: string): string {
  return role === "ADMIN" ? "admin" : "user";
}

export function UserProfileHeader() {
  const { data: profile, isLoading, error } = useUserProfile();
  const [avatarError, setAvatarError] = useState(false);

  return (
    <>
      <style>{`
        @keyframes pulse {
          0%, 100% { opacity: 1; }
          50%       { opacity: 0.4; }
        }
        .profile-header {
          position: relative;
          background: var(--color-surface-dark);
          overflow: hidden;
          padding: 36px 48px;
        }
        .profile-header::before {
          content: "";
          position: absolute;
          inset: 0;
          background-image: radial-gradient(circle, rgba(118,185,0,0.1) 1px, transparent 1px);
          background-size: 24px 24px;
          pointer-events: none;
        }
        .profile-cs { position: absolute; width: 12px; height: 12px; background: var(--color-primary); }
        .profile-cs-tl { top: 0; left: 0; }
        .profile-cs-br { bottom: 0; right: 0; }
        .stat-block {
          display: flex;
          flex-direction: column;
          align-items: center;
          gap: 2px;
          padding: 0 24px;
          border-left: 1px solid var(--color-hairline-strong);
        }
        .stat-value { font-size: var(--fs-display-lg); font-weight: 700; color: var(--color-primary); line-height: 1; }
        .stat-label { font-size: var(--fs-utility-xs); font-weight: 700; text-transform: uppercase; letter-spacing: 0.08em; color: var(--color-on-dark-mute); }
      `}</style>

      <div className="profile-header">
        <div className="profile-cs profile-cs-tl" />
        <div className="profile-cs profile-cs-br" />

        <div style={{ position: "relative", zIndex: 1, display: "flex", alignItems: "center", gap: 24 }}>
          {/* Avatar */}
          {isLoading ? (
            <div style={{ width: 64, height: 64, borderRadius: "var(--radius-full)", background: "var(--color-surface-elevated)", flexShrink: 0, animation: "pulse 1.4s ease-in-out infinite" }} />
          ) : profile && !avatarError && profile.avatar ? (
            <img
              src={profile.avatar}
              alt={profile.fullName}
              width={64}
              height={64}
              loading="lazy"
              onError={() => setAvatarError(true)}
              style={{ borderRadius: "var(--radius-full)", border: "2px solid var(--color-primary)", flexShrink: 0, objectFit: "cover" }}
            />
          ) : (
            <InitialsFallback name={profile?.fullName ?? "?"} size={64} />
          )}

          {/* Name + role */}
          <div style={{ flex: 1 }}>
            {isLoading ? (
              <div style={{ display: "flex", flexDirection: "column", gap: 8 }}>
                <SkeletonPulse w="180px" h={24} />
                <SkeletonPulse w="80px" h={16} />
              </div>
            ) : error ? (
              <p style={{ color: "var(--color-error)", fontSize: "var(--fs-body-sm)" }}>Không thể tải thông tin người dùng.</p>
            ) : profile ? (
              <>
                <h1 style={{ margin: 0, fontSize: "var(--fs-heading-xl)", fontWeight: 700, color: "var(--color-on-dark)", lineHeight: 1.2 }}>
                  {profile.fullName}
                </h1>
                <span
                  className="badge-tag"
                  aria-label={profile.role}
                  style={{ marginTop: 6, display: "inline-block", background: "rgba(118,185,0,0.15)", color: "var(--color-primary)", border: "1px solid var(--color-primary)" }}
                >
                  {homeRoleLabel(profile.role)}
                </span>
              </>
            ) : null}
          </div>

          
        </div>
      </div>
    </>
  );
}
