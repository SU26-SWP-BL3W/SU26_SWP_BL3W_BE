'use client';

import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { submissionsApi } from '../api/submissions';
import type { CreateSubmissionPayload, UpdateSubmissionPayload } from '../api/submissions';

export const SUB_KEYS = {
  list: (teamId: string, eventId: string) => ['submissions', teamId, eventId] as const,
};

/** Bài nộp của đội trong một sự kiện. eventId bắt buộc (filter phân quyền BE cần EventId). */
export const useTeamSubmissions = (teamId: string, eventId: string) =>
  useQuery({
    queryKey: SUB_KEYS.list(teamId, eventId),
    queryFn: () => submissionsApi.list({ teamId, eventId }),
    enabled: !!teamId && !!eventId,
    staleTime: 60_000,
  });

export const useCreateSubmission = (teamId: string) => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (p: CreateSubmissionPayload) => submissionsApi.create(p),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['submissions', teamId] }),
  });
};

export const useUpdateSubmission = (teamId: string) => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (p: UpdateSubmissionPayload) => submissionsApi.update(p),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['submissions', teamId] }),
  });
};
