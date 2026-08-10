'use client';

import { useCurrentUser } from '@/hooks/useAuth';

export type AppRole = 'admin' | 'student' | 'judge';

export function useUserRole(): AppRole | null {
  const { data } = useCurrentUser();
  if (!data) return null;
  switch (data.role) {
    case 'ADMIN':   return 'admin';
    case 'STUDENT': return 'student';
    case 'MENTOR':  return 'judge';
    default:        return null;
  }
}
