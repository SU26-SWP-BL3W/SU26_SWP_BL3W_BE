'use client';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { usersApi, authApi, type UserSummary, type UpdateStudentProfileCommand } from '@/services/api';
import { userRejectionsApi } from '../api/userRejections';
import { resolveRegistrationStatus } from '../status';

export const REGISTRATION_KEYS = {
  profile: ['users', 'profile'] as const,
  rejections: (userId: string) => ['userRejections', userId] as const,
};

export function useRegistration(userId: string) {
  const qc = useQueryClient();
  const enabled = typeof window !== 'undefined' && !!localStorage.getItem('accessToken');

  const profileQ = useQuery({
    queryKey: REGISTRATION_KEYS.profile,
    queryFn: () => usersApi.getProfile(),
    enabled,
    staleTime: 30_000,
  });
  const rejectionsQ = useQuery({
    queryKey: REGISTRATION_KEYS.rejections(userId),
    queryFn: () => userRejectionsApi.listForUser(userId),
    enabled: enabled && !!userId,
    staleTime: 30_000,
  });

  const { status, reason } = resolveRegistrationStatus(profileQ.data ?? null, rejectionsQ.data ?? []);

  const invalidate = () => {
    qc.invalidateQueries({ queryKey: REGISTRATION_KEYS.profile });
    qc.invalidateQueries({ queryKey: REGISTRATION_KEYS.rejections(userId) });
  };

  const submit = async (cmd: UpdateStudentProfileCommand) => {
    const isFirstTime = !profileQ.data?.studentCode;
    if (isFirstTime) {
      await authApi.submitStudentProfile(cmd);
    } else {
      await authApi.updateStudentProfile(cmd);
    }
    invalidate();
  };

  /** Re-apply after a rejection: only refresh. We must NOT delete rejection records —
   *  the >=2-reject block counts TOTAL records, so deleting would reset the count and
   *  let a student be rejected unlimited times (mất dữ liệu đếm số lần từ chối). */
  const clearRejections = async () => {
    invalidate();
  };

  return {
    profile: (profileQ.data ?? null) as UserSummary | null,
    status,
    reason,
    rejectionCount: rejectionsQ.data?.length ?? 0,
    isLoading: profileQ.isLoading || rejectionsQ.isLoading,
    submit,
    clearRejections,
  };
}
