'use client';

import React from 'react';
import {
  BarChart3, Upload, Trophy, Users, UserPlus, ClipboardList, FileText, Settings, ClipboardCheck,
  Inbox,
  Scale,
} from 'lucide-react';
import { useEventDashboard } from '@/features/events/contexts/EventDashboardContext';
import { useUserRole } from '@/hooks/useUserRole';
import { useCurrentUser } from '@/hooks/useAuth';
import { useMyTeamForEvent } from '@/features/teams/hooks/useTeams';
import { getEventTabs, type EventTabId } from '@/lib/events/getEventTabs';
import { useRegistration } from '@/features/registration/hooks/useRegistration';
import { useRouter } from 'next/navigation';
import { useUserEventRole } from '@/features/events/hooks/useEvents';
import { resolveEventRole, roleNameToNumber } from '@/lib/events/eventRole';

interface SidebarProps { eventId: string; }

const ICON: Record<EventTabId, React.ComponentType<{ size?: number; className?: string }>> = {
  detail:        BarChart3,
  register:      ClipboardCheck,
  createTeam:    UserPlus,
  myTeam:        Users,
  submission:    Upload,
  results:       FileText,
  appeal:        Scale,
  leaderboard:   Trophy,
  judgeAssigned: ClipboardList,
  manage:        Settings,
  reviewSubmission: Inbox,
};

export function Sidebar({ eventId }: SidebarProps) {
  const router = useRouter();
  const { activeTab, setActiveTab } = useEventDashboard();
  const role = useUserRole();
  const { data: user } = useCurrentUser();
  const { data: eventRole } = useUserEventRole(user?.id ?? '', eventId);
  const { data: team } = useMyTeamForEvent(eventId, user?.id ?? '');
  const { status: registrationStatus } = useRegistration(user?.id ?? '');
  const isAuthenticated = !!user;
  const tabs = getEventTabs({ isAuthenticated, role, eventRoleName: eventRole?.roleName, hasTeam: !!team, registrationStatus });

  const getDisplayRoleLabel = () => {
    if (role === 'admin') return 'Admin';
    const eventRoleNum = roleNameToNumber(eventRole?.roleName);
    const { label } = resolveEventRole(eventRoleNum !== null ? [eventRoleNum] : []);
    return label;
  };

  return (
    <aside
      className="fixed left-0 top-0 h-screen w-16 md:w-60 bg-surface-dark border-r border-hairline-strong flex flex-col lg:w-60 z-50"
      role="navigation"
      aria-label="Event dashboard navigation"
    >
      <nav className="flex-1 pt-0" role="tablist">
        <ul className="list-none p-0 m-0">
          {tabs.map((tab) => {
            const Icon = ICON[tab.id];
            const isActive = activeTab === tab.id;
            return (
              <li key={tab.id} role="presentation">
                <button
                  role="tab"
                  aria-selected={isActive}
                  onClick={() => {
                    if (tab.id === 'manage') {
                      router.push(`/events/${eventId}/manage`);
                    } else {
                      setActiveTab(tab.id);
                    }
                  }}
                  style={{ color: 'var(--color-on-dark)' }}
                  className={`w-full px-4 py-4 flex items-center gap-3 border-b border-hairline-strong transition-colors duration-150 cursor-pointer min-h-12 ${
                    isActive
                      ? 'bg-surface-dark border-l-4 border-l-primary'
                      : 'bg-surface-dark hover:bg-[rgba(255,255,255,0.08)] border-l-4 border-l-transparent'
                  } focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary`}
                  aria-label={`${tab.label} tab`}
                >
                  <Icon size={18} className={isActive ? 'text-primary' : 'opacity-75'} />
                  <span className="text-sm font-bold hidden md:inline">{tab.label}</span>
                  <span className="sr-only">{tab.label}</span>
                </button>
              </li>
            );
          })}
        </ul>
      </nav>

      <div className="border-t border-hairline-strong p-4 flex items-center gap-3 bg-surface-dark">
        <div className="w-8 h-8 rounded-full bg-primary flex items-center justify-center text-white text-xs font-bold flex-shrink-0">
          {user?.fullName?.slice(0, 2).toUpperCase() ?? '?'}
        </div>
        <div className="hidden md:flex flex-col flex-1 min-w-0">
          <p className="text-on-dark text-body-sm font-bold truncate">{user?.fullName ?? '—'}</p>
          <span className="inline-block bg-primary/20 text-primary text-caption-xs px-2 py-1 rounded-full text-xs mt-1 w-fit font-semibold">
            {getDisplayRoleLabel()}
          </span>
        </div>
      </div>
    </aside>
  );
}
