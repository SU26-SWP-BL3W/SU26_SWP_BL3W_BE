'use client';

import React from 'react';
import { ArrowLeft } from 'lucide-react';
import { useRouter } from 'next/navigation';
import type { Event } from '@/features/events/types/event.types';
import { EventSwitcher } from '../EventSwitcher';

type EventPhase = 'active' | 'upcoming' | 'ended';

const PHASE: Record<EventPhase, { label: string; cls: string }> = {
  active: { label: 'Đang diễn ra', cls: 'bg-primary text-on-primary' },
  upcoming: { label: 'Sắp tới', cls: 'bg-surface-soft text-ink' },
  ended: { label: 'Đã kết thúc', cls: 'bg-stone text-on-dark' },
};

interface HeaderProps {
  title: string;
  status?: EventPhase;
  submissionType?: string;
  eventId: string;
  events: Event[];
  eventsLoading: boolean;
  onSwitch?: () => void;
}

export function Header({ title, status, submissionType, eventId, events, eventsLoading, onSwitch }: HeaderProps) {
  const router = useRouter();

  return (
    <header className="fixed top-0 left-0 right-0 h-auto md:h-20 bg-canvas border-b border-hairline flex items-center px-4 md:px-6 gap-3 md:gap-4 lg:left-60 z-40 py-4 md:py-0">
      <button
        onClick={() => router.push('/')}
        className="flex items-center justify-center w-9 h-9 rounded-sm border border-hairline text-ink hover:bg-surface-soft transition-colors duration-150 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary flex-shrink-0"
        aria-label="Quay lại danh sách sự kiện"
      >
        <ArrowLeft size={18} aria-hidden="true" />
      </button>
      <EventSwitcher
        currentEventId={eventId}
        currentTitle={title}
        events={events}
        isLoading={eventsLoading}
        onSwitch={onSwitch}
      />

      <div className="flex items-center gap-2 md:gap-3 ml-auto flex-shrink-0">
        {status && (
          <span
            className={`inline-block px-2 md:px-3 py-1 rounded-sm text-xs md:text-body-sm font-bold uppercase whitespace-nowrap ${PHASE[status].cls}`}
          >
            {PHASE[status].label}
          </span>
        )}

        {submissionType && (
          <span className="inline-block bg-surface-soft text-ink px-2 md:px-3 py-1 rounded-sm text-caption-xs md:text-caption-sm font-bold uppercase whitespace-nowrap">
            {submissionType}
          </span>
        )}

      </div>
    </header>
  );
}
