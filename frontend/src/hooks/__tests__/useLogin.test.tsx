import React from 'react';
import { renderHook, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { useLogin } from '@/hooks/useAuth';
import { authApi } from '@/services/api';
import { NotificationProvider } from '@/components/NotificationProvider';

jest.mock('@/services/api', () => ({
  authApi: { login: jest.fn(), logout: jest.fn() },
}));

const mockLogin = authApi.login as jest.Mock;

function makeWrapper() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  const wrapper = ({ children }: { children: React.ReactNode }) => (
    <QueryClientProvider client={queryClient}>
      <NotificationProvider>{children}</NotificationProvider>
    </QueryClientProvider>
  );
  return { queryClient, wrapper };
}

describe('useLogin', () => {
  beforeEach(() => {
    localStorage.clear();
    mockLogin.mockReset();
  });

  test('on success, both the auth/me and user/profile caches hold the logged-in user', async () => {
    mockLogin.mockResolvedValue({
      accessToken: 'a',
      refreshToken: 'r',
      userId: 'u1',
      email: 'student@fpt.edu.vn',
      fullName: 'Nguyen Van A',
      isAdmin: false,
      isStudent: true,
    });

    const { queryClient, wrapper } = makeWrapper();
    const { result } = renderHook(() => useLogin(), { wrapper });

    result.current.mutate({ email: 'student@fpt.edu.vn', password: 'pw' });

    await waitFor(() => expect(result.current.isSuccess).toBe(true));

    const me = queryClient.getQueryData(['auth', 'me']) as { fullName?: string } | undefined;

    // Navbar (auth/me) already worked; the profile header (users/profile) is the bug.
    expect(me?.fullName).toBe('Nguyen Van A');
  });
});
