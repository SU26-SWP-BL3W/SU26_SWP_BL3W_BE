'use client';

import React, { useState } from 'react';
import dynamic from 'next/dynamic';
import { ArrowLeft } from 'lucide-react';
import { useRouter } from 'next/navigation';
import { useAllEvents, useEvent, useMyEvents } from '@/features/events/hooks/useEvents';
import { useCurrentUser } from '@/hooks/useAuth';
import { useUserRole } from '@/hooks/useUserRole';
import { AdminSidebar, AdminTab } from './AdminSidebar';
import { EventSwitcher } from '../EventSwitcher';

const tabLoading = () => <p className="t-body-md text-mute p-6">Đang tải nội dung…</p>;
const EventDetailTab = dynamic(() => import('./tabs/EventDetailTab').then((mod) => mod.EventDetailTab), { loading: tabLoading });
const TeamListTab = dynamic(() => import('./tabs/TeamListTab').then((mod) => mod.TeamListTab), { loading: tabLoading });
const RoleListTab = dynamic(() => import('./tabs/RoleListTab').then((mod) => mod.RoleListTab), { loading: tabLoading });
const LeaderboardTab = dynamic(() => import('./tabs/LeaderboardTab').then((mod) => mod.LeaderboardTab), { loading: tabLoading });
const PrizeTab = dynamic(() => import('./tabs/PrizeTab').then((mod) => mod.PrizeTab), { loading: tabLoading });
const SubmissionsScoringPanel = dynamic(() => import('../shared/SubmissionsScoringPanel'), { loading: tabLoading });
const AppealsPanel = dynamic(() => import('../shared/AppealsPanel'), { loading: tabLoading });

interface AdminEventDashboardProps {
  eventId: string;
  /** Role label shown in the sidebar — "Admin" or "EC". */
  role?: string;
}

/** Trạng thái sự kiện hiển thị ở header (đồng bộ với view participant). */
const PHASE = {
  active: { label: 'Đang diễn ra', cls: 'bg-primary text-on-primary' },
  upcoming: { label: 'Sắp tới', cls: 'bg-surface-soft text-ink' },
  ended: { label: 'Đã kết thúc', cls: 'bg-stone text-on-dark' },
} as const;

function eventPhase(event: {
  startDate: string;
  endDate: string;
  status: 'open' | 'closed';
}): keyof typeof PHASE {
  const now = Date.now();
  const start = new Date(event.startDate).getTime();
  const end = new Date(event.endDate).getTime();
  if (!Number.isNaN(start) && now < start) return 'upcoming';
  if (event.status === 'closed' || (!Number.isNaN(end) && now > end)) return 'ended';
  return 'active';
}

export function AdminEventDashboard({ eventId, role = 'Admin' }: AdminEventDashboardProps) {
  const router = useRouter();
  const [activeTab, setActiveTab] = useState<AdminTab>('detail');
  const { data: event, isLoading } = useEvent(eventId);
  const { data: currentUser } = useCurrentUser();
  const systemRole = useUserRole();
  const isSystemAdmin = systemRole === 'admin';
  const allEvents = useAllEvents(true);
  const myEvents = useMyEvents(currentUser?.id);
  const roleByEvent = new Map((myEvents.data ?? []).map((item) => [item.id, item.myRole]));
  const visibleEvents = (allEvents.data ?? []).map((item) => ({
    ...item,
    myRole: roleByEvent.get(item.id) ?? null,
  }));
  const visibleEventIds = new Set(visibleEvents.map((item) => item.id));
  const eventsList = isSystemAdmin
    ? (allEvents.data ?? [])
    : [...visibleEvents, ...(myEvents.data ?? []).filter((item) => !visibleEventIds.has(item.id))];
  const eventsLoading = allEvents.isLoading || (!isSystemAdmin && myEvents.isLoading);

  if (isLoading) {
    return (
      <div className="min-h-screen bg-canvas flex items-center justify-center">
        <div className="text-center">
          <div className="w-8 h-8 border-4 border-primary border-t-transparent rounded-full animate-spin mx-auto mb-4" />
          <p className="t-body-md text-mute">Đang tải sự kiện...</p>
        </div>
      </div>
    );
  }

  if (!event) {
    return (
      <div className="min-h-screen bg-canvas flex items-center justify-center">
        <p className="t-heading-md text-error">Không tìm thấy sự kiện</p>
      </div>
    );
  }

  const renderTab = () => {
    switch (activeTab) {
      case 'detail':
        return <EventDetailTab eventId={eventId} />;
      case 'teams':
        return <TeamListTab eventId={eventId} />;
      case 'roles':
        return <RoleListTab eventId={eventId} />;
      case 'submission':
        return <SubmissionsScoringPanel eventId={eventId} />;
      case 'appeal':
        return <AppealsPanel eventId={eventId} mode="manager" />;
      case 'prizes':
        return <PrizeTab eventId={eventId} />;
      case 'leaderboard':
        return <LeaderboardTab eventId={eventId} />;
      default:
        return null;
    }
  };

  return (
    <div className="min-h-screen bg-canvas">
      <AdminSidebar activeTab={activeTab} setActiveTab={setActiveTab} role={role} />

      <header className="fixed top-0 left-0 right-0 h-auto md:h-20 bg-canvas border-b border-hairline flex items-center px-4 md:px-6 gap-3 md:gap-4 lg:left-60 z-40 py-4 md:py-0">
        <button
          onClick={() => router.push('/')}
          className="flex items-center justify-center w-9 h-9 rounded-sm border border-hairline text-ink hover:bg-surface-soft transition-colors duration-150 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary flex-shrink-0"
          aria-label="Quay lại danh sách sự kiện"
        >
          <ArrowLeft size={18} aria-hidden="true" />
        </button>
        <EventSwitcher
          currentEventId={event.id}
          currentTitle={event.title}
          events={eventsList}
          isLoading={eventsLoading}
          isSystemAdmin={isSystemAdmin}
          onSwitch={() => setActiveTab('detail')}
        />
        <span
          className={`inline-block px-2 md:px-3 py-1 rounded-sm text-xs md:text-body-sm font-bold uppercase whitespace-nowrap flex-shrink-0 ${PHASE[eventPhase(event)].cls}`}
        >
          {PHASE[eventPhase(event)].label}
        </span>
      </header>

      <main className="fixed top-24 md:top-20 left-0 right-0 bottom-0 overflow-hidden bg-canvas lg:left-60">
        <div className="h-full overflow-y-auto p-3 md:p-6">
          <div className="animate-fadeIn">{renderTab()}</div>
        </div>
      </main>
    </div>
  );
}
