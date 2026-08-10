import { renderHook } from '@testing-library/react';
import { useUserRole } from '../useUserRole';
import * as auth from '@/hooks/useAuth';

jest.mock('@/hooks/useAuth');

const mockUseCurrentUser = auth.useCurrentUser as jest.Mock;

function mockUser(role: 'ADMIN' | 'STUDENT' | 'MENTOR' | null) {
  mockUseCurrentUser.mockReturnValue({
    data: role ? { id: 'u1', email: 'a@b.c', fullName: 'X', role, createdAt: '', stats: { eventsJoined: 0, projectScore: 0, rank: 0 } } : null,
  });
}

describe('useUserRole', () => {
  test('ADMIN → admin', () => { mockUser('ADMIN'); expect(renderHook(() => useUserRole()).result.current).toBe('admin'); });
  test('STUDENT → student', () => { mockUser('STUDENT'); expect(renderHook(() => useUserRole()).result.current).toBe('student'); });
  test('MENTOR → judge', () => { mockUser('MENTOR'); expect(renderHook(() => useUserRole()).result.current).toBe('judge'); });
  test('no user → null', () => { mockUser(null); expect(renderHook(() => useUserRole()).result.current).toBeNull(); });
});
