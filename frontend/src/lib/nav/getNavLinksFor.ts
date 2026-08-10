import type { AppRole } from '@/hooks/useUserRole';

export interface NavLink { label: string; href: string; }

const LINK = {
  events:   { label: 'Sự kiện',             href: '/' },
  criteria: { label: 'Tiêu chí chấm điểm', href: '/criteria' },
  users:    { label: 'Người dùng',         href: '/users' },
  other:    { label: 'Khác',                href: '/other' },
} as const;

export function getNavLinksFor(role: AppRole | null): NavLink[] {
  if (role === 'admin')                       return [LINK.events, LINK.criteria, LINK.users, LINK.other];
  if (role === 'student' || role === 'judge') return [LINK.events, LINK.other];
  return [LINK.events];
}
