'use client';

import { useState } from 'react';
import { ChevronDown } from 'lucide-react';
import { useRouter } from 'next/navigation';
import type { Event } from '@/features/events/types/event.types';
import { resolveEventRole, roleNameToNumber } from '@/lib/events/eventRole';

interface EventSwitcherProps {
  currentEventId: string;
  currentTitle: string;
  events: Event[];
  isLoading: boolean;
  isSystemAdmin?: boolean;
  onSwitch?: () => void;
}

const isManagerRole = (role?: string | null) => {
  const normalized = (role ?? '').trim().toLowerCase();
  return normalized === 'eventcoordinator' || normalized === 'admin';
};

const getRoleLabel = (role?: string | null) => {
  if (!role) return 'Chưa tham gia';
  const roleNumber = roleNameToNumber(role);
  return resolveEventRole(roleNumber !== null ? [roleNumber] : []).label;
};

export function EventSwitcher({
  currentEventId,
  currentTitle,
  events,
  isLoading,
  isSystemAdmin = false,
  onSwitch,
}: EventSwitcherProps) {
  const router = useRouter();
  const [open, setOpen] = useState(false);

  return (
    <div className="relative flex min-w-0 flex-1 flex-col gap-1">
      <button
        type="button"
        className="flex w-fit max-w-full min-w-0 items-center gap-2 text-left text-ink transition-colors duration-200 hover:text-primary focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary"
        title="Đổi sang sự kiện khác"
        aria-expanded={open}
        aria-haspopup="listbox"
        aria-controls="event-switcher-list"
        onClick={() => setOpen((current) => !current)}
      >
        <h1 className="t-heading-md m-0 truncate text-sm md:text-lg lg:text-xl">
          {currentTitle}
        </h1>
        <ChevronDown
          size={20}
          aria-hidden="true"
          className={`shrink-0 text-mute transition-transform duration-200 ${open ? 'rotate-180' : ''}`}
        />
      </button>

      {open ? (
        <>
          <button
            type="button"
            className="fixed inset-0 z-40 cursor-default"
            aria-label="Đóng danh sách sự kiện"
            onClick={() => setOpen(false)}
          />
          <div
            id="event-switcher-list"
            role="listbox"
            aria-label="Chọn sự kiện"
            className="absolute left-0 top-full z-50 mt-2 max-h-80 w-72 max-w-[80vw] overflow-y-auto rounded-sm border border-hairline bg-canvas p-1 shadow-chrome"
          >
            {isLoading ? (
              <p className="px-3 py-2 t-body-sm text-mute">Đang tải sự kiện…</p>
            ) : events.length === 0 ? (
              <p className="px-3 py-2 t-body-sm text-mute">Không có sự kiện để chuyển.</p>
            ) : (
              events.map((event) => {
                const isCurrent = event.id === currentEventId;
                const roleLabel = isSystemAdmin ? 'Quản trị viên' : getRoleLabel(event.myRole);
                return (
                  <button
                    key={event.id}
                    type="button"
                    role="option"
                    aria-selected={isCurrent}
                    className={`block w-full rounded-xs px-3 py-2 text-left t-body-sm transition-colors duration-150 focus-visible:outline-2 focus-visible:outline-offset-[-2px] focus-visible:outline-primary ${
                      isCurrent
                        ? 'bg-surface-soft font-semibold text-primary'
                        : 'text-ink hover:bg-surface-soft'
                    }`}
                    onClick={() => {
                      setOpen(false);
                      if (isCurrent) return;
                      onSwitch?.();
                      const manageEvent = isSystemAdmin || isManagerRole(event.myRole);
                      router.push(`/events/${event.id}${manageEvent ? '/manage' : ''}`);
                    }}
                  >
                    <span className="block truncate">{event.title}</span>
                    <span className="mt-0.5 block truncate text-xs text-mute">
                      {roleLabel}
                    </span>
                  </button>
                );
              })
            )}
          </div>
        </>
      ) : null}
    </div>
  );
}
