'use client';

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import eventService from '../api/eventService';
import { eventsApi, toUiEvent } from '../api/events';
import { manageApi } from '../api/manage';
import { eventRolesApi } from '../api/eventRoles';
import { useUserRole } from '@/hooks/useUserRole';

export const eventKeys = {
  detail: (eventId: string) => ['events', 'detail', eventId] as const,
};

export const useEventModel = (eventId: string) => {
  return useQuery({
    queryKey: eventKeys.detail(eventId),
    queryFn: () => eventsApi.getModelById(eventId),
    enabled: !!eventId,
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
};

export const useEvent = (eventId: string) => {
  return useQuery({
    queryKey: eventKeys.detail(eventId),
    queryFn: () => eventsApi.getModelById(eventId),
    select: toUiEvent,
    enabled: !!eventId,
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
};

export const useAllEvents = (isAdmin: boolean) => {
  return useQuery({
    queryKey: ['events', 'all', isAdmin],
    queryFn: () => eventsApi.list(isAdmin ? undefined : true), // real API: GET /api/Events
    staleTime: 2 * 60 * 1000, // 2 minutes
  });
};

// ─── Admin manage page (real API) ──────────────────────────────────────────────

/** All roles (judge/mentor/competitor) in an event — admin manage page. */
export const useEventRoles = (eventId: string, roleName?: number) =>
  useQuery({
    queryKey: ['eventRoles', eventId, roleName],
    queryFn: () => manageApi.listEventRoles(eventId, roleName),
    enabled: !!eventId,
    staleTime: 2 * 60 * 1000,
  });

/** Teams (filtered by eventId). */
export const useTeams = (eventId?: string) =>
  useQuery({
    queryKey: ['teams', eventId || 'all'],
    queryFn: () => manageApi.listTeams(eventId),
    staleTime: 2 * 60 * 1000,
  });

/** Rounds of an event. */
export const useEventRounds = (eventId: string) =>
  useQuery({
    queryKey: ['rounds', eventId],
    queryFn: () => manageApi.listEventRounds(eventId),
    enabled: !!eventId,
    staleTime: 5 * 60 * 1000,
  });

/** Tracks of an event (grouped by round client-side). */
export const useEventTracks = (eventId: string) =>
  useQuery({
    queryKey: ['tracks', eventId],
    queryFn: () => manageApi.listEventTracks(eventId),
    enabled: !!eventId,
    staleTime: 5 * 60 * 1000,
  });

/** Final results (leaderboard) of a round. */
export const useRoundFinalResults = (roundId: string | undefined) =>
  useQuery({
    queryKey: ['finalResults', roundId],
    queryFn: () => manageApi.listRoundFinalResults(roundId as string),
    enabled: !!roundId,
    staleTime: 2 * 60 * 1000,
  });

/**
 * FE-01 — Tính kết quả cả vòng (EC/Admin). `topN` tùy chọn (bỏ trống để BE tự
 * quyết). CHỈ tính toán, ở trạng thái NHÁP (isPublished=false) — KHÔNG tự công bố.
 * Làm mới leaderboard của vòng sau khi tính để hiển thị kết quả nháp cho EC rà soát.
 */
export const useCalculateRoundResults = (roundId: string | undefined) => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (topN?: number) =>
      manageApi.calculateRoundResults(roundId as string, topN),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['finalResults', roundId] });
    },
  });
};

/**
 * FE-03 — Đặt trạng thái công bố kết quả cả vòng (EC/Admin), đảo được CẢ HAI CHIỀU
 * qua tham số `isPublished` — KHÔNG mất dữ liệu Rank/FinalScore/IsAdvanced đã tính,
 * khác hẳn `useCancelRoundResults` (xóa sạch, phải tính lại). Dùng cho nút bật/tắt
 * công bố; muốn tính lại từ đầu thì dùng `useCancelRoundResults` + calculate lại.
 */
export const useSetRoundPublishStatus = (roundId: string | undefined) => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (isPublished: boolean) =>
      manageApi.setRoundPublishStatus(roundId as string, isPublished),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['finalResults', roundId] });
    },
  });
};

/**
 * FE-02 — Xóa sạch kết quả cả vòng để tính lại từ đầu (EC/Admin). KHÁC với việc tạm
 * ẩn/hiện bảng xếp hạng (xem `useSetRoundPublishStatus`) — hàm này XÓA HẲN Rank/
 * FinalScore/IsAdvanced đã tính, chỉ dùng khi cần calculate lại (vd phát hiện sai
 * sót, đổi topN). Sau khi xóa, làm mới leaderboard (sẽ rỗng) → UI tự hiện lại nút
 * "Tính kết quả"; form chấm của vòng cũng tự mở lại.
 */
export const useCancelRoundResults = (roundId: string | undefined) => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => manageApi.cancelRoundResults(roundId as string),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['finalResults', roundId] });
    },
  });
};

/**
 * Events the user has actually joined — derived from their EventRoles
 * (competitor/judge/mentor). We list the user's roles to get the joined
 * event ids, then filter the public events list down to those ids.
 */
export const useMyEvents = (userId?: string) => {
  return useQuery({
    queryKey: ['events', 'my', userId ?? ''],
    enabled: !!userId,
    queryFn: async () => {
      const roles = await manageApi.listUserEventRoles(userId as string);
      // Map mỗi eventId -> vai trò của user (lấy vai trò đầu tiên gặp trong event đó).
      const roleByEvent = new Map<string, string>();
      for (const r of roles) {
        if (r.eventId && r.roleName && !roleByEvent.has(r.eventId)) {
          roleByEvent.set(r.eventId, r.roleName);
        }
      }
      const all = await eventsApi.list();
      // Gắn myRole vào từng event đã tham gia (chỉ danh sách "Của tôi" mới có field này).
      return all
        .filter((e) => roleByEvent.has(e.id))
        .map((e) => ({ ...e, myRole: roleByEvent.get(e.id) ?? null }));
    },
    staleTime: 2 * 60 * 1000, // 2 minutes
  });
};

export const useEventTeams = (eventId: string) => {
  return useQuery({
    queryKey: ['eventTeams', eventId],
    queryFn: () => eventService.getEventTeams(eventId),
    staleTime: 2 * 60 * 1000, // 2 minutes
  });
};

export const useUserTeam = (eventId: string, userId: string) => {
  return useQuery({
    queryKey: ['userTeam', eventId, userId],
    queryFn: () => eventService.getUserTeam(eventId, userId),
    staleTime: 5 * 60 * 1000,
  });
};

export const useCreateTeam = (eventId: string) => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (teamData: { name: string; memberIds: string[] }) =>
      eventService.createTeam(eventId, teamData),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['eventTeams', eventId] });
      // Invalidate with exact key for this event's user team
      queryClient.invalidateQueries({ queryKey: ['userTeam', eventId] });
    },
  });
};

export const useTeamSubmissions = (teamId: string) => {
  return useQuery({
    queryKey: ['submissions', teamId],
    queryFn: () => eventService.getTeamSubmissions(teamId),
    staleTime: 1 * 60 * 1000, // 1 minute
  });
};

/**
 * Submit work for a competition team
 * @param teamId - Team ID
 * @param eventId - Event ID
 * @returns Mutation for submitting work
 *
 * Usage:
 * - ZIP: pass FormData with 'file' key containing the zip file
 * - URL: pass string URL (GitHub, Google Drive, etc.)
 */
export const useSubmitWork = (teamId: string, eventId: string) => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (submissionData: { type: 'ZIP' | 'URL'; content: string | FormData }) =>
      eventService.submitWork(teamId, eventId, submissionData),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['submissions', teamId] });
      queryClient.invalidateQueries({ queryKey: ['scores', eventId] });
    },
  });
};

export const useTeamScores = (eventId: string) => {
  return useQuery({
    queryKey: ['scores', eventId],
    queryFn: () => eventService.getTeamScores(eventId),
    staleTime: 2 * 60 * 1000,
  });
};

export const useScoreBreakdown = (teamId: string, eventId: string) => {
  return useQuery({
    queryKey: ['scoreBreakdown', teamId, eventId],
    queryFn: () => eventService.getScoreBreakdown(teamId, eventId),
    staleTime: 5 * 60 * 1000,
  });
};

export const useJoinEvent = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (eventId: string) =>
      eventService.joinEvent(eventId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['events', 'my'] });
      queryClient.invalidateQueries({ queryKey: ['events', 'all'] });
    },
  });
};

export const useUserEventRole = (userId: string, eventId: string) => {
  const role = useUserRole();
  const isAdmin = role === 'admin';

  return useQuery({
    queryKey: ['userEventRole', userId, eventId],
    queryFn: () => eventRolesApi.getUserRole(userId, eventId),
    enabled: !!userId && !!eventId && !isAdmin,
    retry: false,
    staleTime: 5 * 60 * 1000,
  });
};
