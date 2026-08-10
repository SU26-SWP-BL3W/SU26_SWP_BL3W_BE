'use client';

import React from 'react';
import { Award, FileText, Users, Trophy, LucideIcon, UserCog, Inbox, Scale } from 'lucide-react';
import { useCurrentUser } from '@/hooks/useAuth';

export type AdminTab =
  | 'detail' | 'teams' | 'roles'
  | 'submission' | 'appeal' | 'prizes' | 'leaderboard';

const tabs: { id: AdminTab; label: string; icon: LucideIcon }[] = [
  { id: 'detail', label: 'Chi tiết sự kiện', icon: FileText },
  { id: 'teams', label: 'Danh sách đội', icon: Users },
  { id: 'roles', label: 'Danh sách vai trò', icon: UserCog },
  // Gộp "Chấm điểm" + "Bài nộp" thành 1 tab (xem SubmissionsScoringPanel.jsx).
  { id: 'submission', label: 'Bài nộp', icon: Inbox },
  { id: 'appeal', label: 'Phúc khảo', icon: Scale },
  { id: 'prizes', label: 'Giải thưởng', icon: Award },
  { id: 'leaderboard', label: 'Bảng xếp hạng', icon: Trophy },
];

interface AdminSidebarProps {
  activeTab: AdminTab;
  setActiveTab: (tab: AdminTab) => void;
  role?: string;
}

export function AdminSidebar({ activeTab, setActiveTab, role = 'Admin' }: AdminSidebarProps) {
  const { data: user } = useCurrentUser();
  const displayName = user?.fullName?.trim() || (role.toLowerCase() === 'eventcoordinator' ? 'Ban tổ chức' : 'Quản trị viên');
  const initials = displayName
    .split(/\s+/)
    .filter(Boolean)
    .slice(-2)
    .map((part) => part[0])
    .join('')
    .toUpperCase();

  const displayRole = (() => {
    switch (role.toLowerCase()) {
      case 'eventcoordinator': return 'Ban tổ chức';
      case 'judge': return 'Giám khảo';
      case 'mentor': return 'Cố vấn';
      case 'teamleader': return 'Trưởng nhóm';
      case 'teammember':
      case 'member': return 'Thành viên';
      default: return role;
    }
  })();

  return (
    <aside
      className="fixed left-0 top-0 h-screen w-16 md:w-60 bg-surface-dark border-r border-hairline-strong flex flex-col lg:w-60 z-50"
      role="navigation"
      aria-label="Admin event navigation"
    >
      <nav className="flex-1 pt-0" role="tablist">
        <ul className="list-none p-0 m-0">
          {tabs.map((tab) => {
            const Icon = tab.icon;
            const isActive = activeTab === tab.id;

            return (
              <li key={tab.id} role="presentation">
                <button
                  role="tab"
                  aria-selected={isActive}
                  onClick={() => setActiveTab(tab.id)}
                  className={`w-full px-4 py-4 flex items-center gap-3 border-b border-hairline-strong transition-colors duration-150 cursor-pointer min-h-12 ${isActive
                      ? 'bg-surface-dark text-on-dark border-l-4 border-l-primary'
                      : 'bg-surface-dark text-on-dark hover:bg-[rgba(255,255,255,0.08)] border-l-4 border-l-transparent'
                    } focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary`}
                  aria-label={`${tab.label} tab`}
                >
                  <Icon
                    size={18}
                    className={isActive ? 'text-primary opacity-100' : 'text-on-dark opacity-75'}
                    aria-hidden="true"
                  />
                  <span className={`text-sm font-bold hidden md:inline ${isActive ? 'text-on-dark' : 'text-on-dark-mute'}`}>{tab.label}</span>
                  <span className="sr-only">{tab.label}</span>
                </button>
              </li>
            );
          })}
        </ul>
      </nav>

      <div className="border-t border-hairline-strong p-4 flex items-center gap-3 bg-surface-dark">
        <div className="w-8 h-8 rounded-full bg-primary flex items-center justify-center text-white text-xs font-bold flex-shrink-0">
          {initials || '?'}
        </div>
        <div className="hidden md:flex flex-col flex-1 min-w-0">
          <p className="text-on-dark text-body-sm font-bold truncate text-opacity-100">{displayName}</p>
          <span className="inline-block bg-primary/20 text-primary text-caption-xs px-2 py-1 rounded-full text-xs mt-1 w-fit font-semibold">
            {displayRole}
          </span>
        </div>
      </div>
    </aside>
  );
}
