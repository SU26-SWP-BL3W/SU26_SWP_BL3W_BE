"use client";

import { useState } from "react";
import dynamic from "next/dynamic";
import type { AxiosError } from "axios";
import { useAllEvents, useMyEvents, useJoinEvent } from "../hooks/useEvents";
import { useUserProfile } from "@/features/user/hooks/useUserProfile";
import { useUserRole } from "@/hooks/useUserRole";
import { EventFilterTabs } from "./EventFilterTabs";
import { EventGrid } from "./EventGrid";
import type { ApiError } from "@/services/api";

const CreateEventForm = dynamic(
  () => import("./CreateEventForm").then((mod) => mod.CreateEventForm),
  { loading: () => <p className="t-body-md text-mute">Đang tải biểu mẫu…</p> },
);

type Filter = "all" | "my";
type Mode = "list" | "create";

export function EventSection() {
  const [mode, setMode] = useState<Mode>("list");
  const [filter, setFilter] = useState<Filter>("all");
  const [joiningId, setJoiningId] = useState<string | null>(null);
  const [joinError, setJoinError] = useState<string | null>(null);

  const role = useUserRole();
  const isAdmin = role === "admin";
  const isLoggedIn = !!role;
  const { data: profile } = useUserProfile();
  // Admins don't have a "Của tôi" list — always show all events.
  const effectiveFilter: Filter = isAdmin || !isLoggedIn ? "all" : filter;

  const myQuery = useMyEvents(profile?.id);
  const allQuery = useAllEvents(isAdmin);

  const active = effectiveFilter === "all" ? allQuery : myQuery;
  const events = active.data ?? [];
  const isLoading = active.isLoading;

  const joinMutation = useJoinEvent();

  function handleJoin(id: string) {
    setJoiningId(id);
    setJoinError(null);
    joinMutation.mutate(id, {
      onSuccess: () => setJoiningId(null),
      onError: (err) => {
        setJoiningId(null);
        const msg = (err as AxiosError<ApiError>)?.response?.data?.message ?? "Tham gia thất bại. Thử lại sau.";
        setJoinError(msg);
      },
    });
  }

  return (
    <div className="container" style={{ paddingTop: "var(--space-section)" }}>
      <div
        className="card"
        style={{
          padding: "var(--space-xxl)",
          gap: "var(--space-xl)",
          ...(mode === "create" ? { border: "none" } : {}),
        }}
      >
        {/* Header row */}
        <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", flexWrap: "wrap", gap: "var(--space-md)" }}>
          <h2 className="t-heading-md" style={{ margin: 0 }}>
            {mode === "create" ? "Tạo sự kiện" : "Sự kiện"}
          </h2>
          {isAdmin && (
            <button
              className="btn btn-outline btn-sm"
              onClick={() => setMode((m) => (m === "create" ? "list" : "create"))}
              style={{ cursor: "pointer" }}
            >
              {mode === "create" ? "← Quay lại" : "+ Tạo sự kiện"}
            </button>
          )}
        </div>

        {mode === "create" && isAdmin ? (
          <CreateEventForm onCancel={() => setMode("list")} />
        ) : (
          <>
            {/* Tabs */}
            <EventFilterTabs active={effectiveFilter} onChange={setFilter} hideMy={isAdmin || !isLoggedIn} />

            {/* Grid */}
            <EventGrid
              events={events}
              isLoading={isLoading}
              joiningId={joiningId}
              joinError={joinError}
              onJoin={handleJoin}
            />
          </>
        )}
      </div>
    </div>
  );
}
