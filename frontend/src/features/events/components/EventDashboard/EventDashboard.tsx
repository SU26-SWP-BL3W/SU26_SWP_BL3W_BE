'use client';

import React, { useEffect, useRef } from 'react';
import dynamic from 'next/dynamic';
import { Sidebar } from './Sidebar';
import { Header } from './Header';
import { useEventDashboard } from '@/features/events/contexts/EventDashboardContext';
import { useAllEvents, useEvent, useMyEvents, useUserEventRole } from '@/features/events/hooks/useEvents';
import { useMyTeamForEvent } from '@/features/teams/hooks/useTeams';
import { useUserRole } from '@/hooks/useUserRole';
import { useRouter } from 'next/navigation';

const tabLoading = () => <p className="t-body-md text-mute p-6">Đang tải nội dung…</p>;
const EventDetailTab = dynamic(() => import('./tabs/EventDetail').then((mod) => mod.EventDetailTab), { loading: tabLoading });
const CreateTeamTab = dynamic(() => import('./tabs/CreateTeam').then((mod) => mod.CreateTeamTab), { loading: tabLoading });
const MyTeamTab = dynamic(() => import('./tabs/MyTeam').then((mod) => mod.MyTeamTab), { loading: tabLoading });
const SubmissionTab = dynamic(() => import('./tabs/Submission').then((mod) => mod.SubmissionTab), { loading: tabLoading });
const ResultsTab = dynamic(() => import('./tabs/Results').then((mod) => mod.ResultsTab), { loading: tabLoading });
const LeaderboardTab = dynamic(() => import('./tabs/Leaderboard').then((mod) => mod.LeaderboardTab), { loading: tabLoading });
const JudgeAssignedTeamsTab = dynamic(() => import('./tabs/JudgeAssignedTeams').then((mod) => mod.JudgeAssignedTeamsTab), { loading: tabLoading });
const SubmissionsScoringPanel = dynamic(() => import('../shared/SubmissionsScoringPanel'), { loading: tabLoading });
const AppealsPanel = dynamic(() => import('../shared/AppealsPanel'), { loading: tabLoading });
const CompetitionRegistrationTab = dynamic(
  () => import('@/features/registration/components/CompetitionRegistrationTab').then((mod) => mod.CompetitionRegistrationTab),
  { loading: tabLoading },
);

interface EventDashboardProps { eventId: string; userId: string; }

/** Giai đoạn hiển thị ở header: theo mốc thời gian, có xét cả trạng thái đóng. */
function eventPhase(event: {
  startDate: string;
  endDate: string;
  status: 'open' | 'closed';
}): 'active' | 'upcoming' | 'ended' {
  const now = Date.now();
  const start = new Date(event.startDate).getTime();
  const end = new Date(event.endDate).getTime();
  if (!Number.isNaN(start) && now < start) return 'upcoming';
  if (event.status === 'closed' || (!Number.isNaN(end) && now > end)) return 'ended';
  return 'active';
}

export function EventDashboard({ eventId, userId }: EventDashboardProps) {
  const router = useRouter();
  const { activeTab, setActiveTab } = useEventDashboard();
  const { data: event, isLoading: eventLoading } = useEvent(eventId);
  const allEvents = useAllEvents(true);
  const { data: myEvents = [], isLoading: myEventsLoading } = useMyEvents(userId);
  const roleByEvent = new Map(myEvents.map((item) => [item.id, item.myRole]));
  const visibleEvents = (allEvents.data ?? []).map((item) => ({
    ...item,
    myRole: roleByEvent.get(item.id) ?? null,
  }));
  const visibleEventIds = new Set(visibleEvents.map((item) => item.id));
  const switcherEvents = [
    ...visibleEvents,
    ...myEvents.filter((item) => !visibleEventIds.has(item.id)),
  ];
  const { data: team } = useMyTeamForEvent(eventId, userId);
  const role = useUserRole();
  const { data: eventRole } = useUserEventRole(userId, eventId);
  const isManager = role === 'admin' || eventRole?.roleName === 'EventCoordinator' || eventRole?.roleName === 'Admin';
  const adminRedirected = useRef(false);

  // Admins or ECs manage an event on its dedicated page — skip the dashboard entirely.
  useEffect(() => {
    if (!adminRedirected.current && isManager) {
      adminRedirected.current = true;
      router.replace(`/events/${eventId}/manage`);
    }
  }, [isManager, eventId, router]);

  if (eventLoading) {
    return (
      <div className="min-h-screen bg-canvas flex items-center justify-center">
        <div className="text-center">
          <div className="w-8 h-8 border-4 border-primary border-t-transparent rounded-full animate-spin mx-auto mb-4" />
          <p className="t-body-md text-mute">Loading event...</p>
        </div>
      </div>
    );
  }
  if (!event) {
    return (
      <div className="min-h-screen bg-canvas flex items-center justify-center">
        <p className="t-heading-md text-error">Event not found</p>
      </div>
    );
  }

  const teamId = team?.id ?? '';

  const renderTabContent = () => {
    switch (activeTab) {
      case 'detail':        return <EventDetailTab eventId={eventId} userId={userId} />;
      case 'register':      return <CompetitionRegistrationTab userId={userId} />;
      case 'createTeam':    return <CreateTeamTab  eventId={eventId} userId={userId} />;
      case 'myTeam':        return <MyTeamTab      eventId={eventId} userId={userId} />;
      case 'submission':    return teamId
        ? <SubmissionTab teamId={teamId} eventId={eventId} />
        : <div className="t-body-md text-mute p-6">Bạn cần tạo đội trước.</div>;
      case 'results':       return teamId
        ? <ResultsTab teamId={teamId} eventId={eventId} />
        : <div className="t-body-md text-mute p-6">Chưa có kết quả.</div>;
      case 'appeal':        return teamId
        ? <AppealsPanel eventId={eventId} teamId={teamId} isLeader={team?.members.some((member) => member.userId === userId && member.isLeader) ?? false} mode="team" />
        : <div className="t-body-md text-mute p-6">Bạn cần tham gia đội trước.</div>;
      case 'leaderboard':   return <LeaderboardTab        eventId={eventId} userId={userId} />;
      case 'judgeAssigned': return <JudgeAssignedTeamsTab eventId={eventId} userId={userId} />;
      // Không ép trackId ở đây — SubmissionsScoringPanel tự tra tất cả track được giao
      // (1 user/Judge có thể có nhiều track trong cùng event) và cho chọn qua dropdown.
      case 'reviewSubmission': return <SubmissionsScoringPanel eventId={eventId} />;
      default: return null;
    }
  };

  return (
    <div className="min-h-screen bg-canvas">
      <Sidebar eventId={eventId} />
      <Header
        eventId={eventId}
        title={event.title}
        status={eventPhase(event)}
        events={switcherEvents}
        eventsLoading={allEvents.isLoading || myEventsLoading}
        onSwitch={() => setActiveTab('detail')}
      />
      <main className="fixed top-24 md:top-20 left-0 right-0 bottom-0 overflow-hidden bg-canvas lg:left-60">
        <div className="h-full overflow-y-auto p-3 md:p-6">
          <div className="animate-fadeIn">{renderTabContent()}</div>
        </div>
      </main>
    </div>
  );
}
