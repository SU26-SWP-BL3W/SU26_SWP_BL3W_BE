"use client";

import { EventCard } from "./EventCard";
import type { Event } from "../types/event.types";

interface Props {
  events: Event[];
  isLoading: boolean;
  joiningId: string | null;
  joinError: string | null;
  onJoin: (id: string) => void;
}

function SkeletonCard() {
  return (
    <div
      className="card"
      style={{ minHeight: 200, gap: "var(--space-md)", padding: "var(--space-xl)" }}
    >
      <style>{`@keyframes pulse{0%,100%{opacity:1}50%{opacity:.35}}`}</style>
      {[80, "100%", 60, 40].map((w, i) => (
        <div
          key={i}
          style={{
            height: i === 1 ? 20 : 12,
            width: w,
            background: "var(--color-surface-soft)",
            borderRadius: "var(--radius-sm)",
            animation: `pulse 1.4s ease-in-out ${i * 0.1}s infinite`,
          }}
        />
      ))}
      <div style={{ marginTop: "auto", height: 44, background: "var(--color-surface-soft)", borderRadius: "var(--radius-sm)", animation: "pulse 1.4s ease-in-out 0.4s infinite" }} />
    </div>
  );
}

export function EventGrid({ events, isLoading, joiningId, joinError, onJoin }: Props) {
  return (
    <div
      id="tabpanel-events"
      role="tabpanel"
      className="grid-auto grid-3"
      style={{ minHeight: 200 }}
    >
      {isLoading ? (
        [1, 2, 3].map((i) => <SkeletonCard key={i} />)
      ) : events.length === 0 ? (
        <div
          style={{
            gridColumn: "1 / -1",
            minHeight: 200,
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
            color: "var(--color-mute)",
            fontSize: "var(--fs-body-sm)",
            border: "var(--border-hairline)",
            borderRadius: "var(--radius-sm)",
          }}
        >
          Không có sự kiện nào.
        </div>
      ) : (
        events.map((event) => (
          <EventCard
            key={event.id}
            event={event}
            onJoin={onJoin}
            isJoining={joiningId === event.id}
            joinError={joiningId === event.id ? joinError : null}
          />
        ))
      )}
    </div>
  );
}
