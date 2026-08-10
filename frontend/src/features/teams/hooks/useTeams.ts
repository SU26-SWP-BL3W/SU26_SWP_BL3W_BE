'use client';

import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { teamsApi } from '../api/teams';
import type { CreateTeamPayload } from '../types/team.types';

export const TEAM_KEYS = {
  myTeam:  (eventId: string, userId: string) => ['team', 'mine', eventId, userId] as const,
  judge:   (eventId: string, userId: string) => ['team', 'judge', eventId, userId] as const,
  detail:  (teamId: string) => ['team', teamId] as const,
  invitations: (teamId: string) => ['team', 'invitations', teamId] as const,
} as const;

export const useMyTeamForEvent = (eventId: string, userId: string) =>
  useQuery({
    queryKey: TEAM_KEYS.myTeam(eventId, userId),
    queryFn: () => teamsApi.findUserTeamForEvent(userId, eventId),
    enabled: !!eventId && !!userId,
    staleTime: 60_000,
  });

export const useJudgeAssignedTeams = (eventId: string, userId: string) =>
  useQuery({
    queryKey: TEAM_KEYS.judge(eventId, userId),
    queryFn: () => teamsApi.findJudgeAssignedTeams(userId, eventId),
    enabled: !!eventId && !!userId,
    staleTime: 60_000,
  });

export const useTeam = (teamId: string | undefined) =>
  useQuery({
    queryKey: TEAM_KEYS.detail(teamId ?? ''),
    queryFn: () => teamsApi.getById(teamId as string),
    enabled: !!teamId,
    staleTime: 60_000,
  });

/** Danh sách lời mời đã gửi của đội (chỉ bật khi người xem là leader — BE 403 với member thường). */
export const useTeamInvitations = (teamId: string, enabled = true) =>
  useQuery({
    queryKey: TEAM_KEYS.invitations(teamId),
    queryFn: () => teamsApi.getTeamInvitations(teamId),
    enabled: !!teamId && enabled,
    staleTime: 30_000,
  });

export const useConfirmRegistration = (teamId: string, eventId: string, userId: string) => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: () => teamsApi.confirmRegistration(teamId),
    onSuccess: () => {
      // Làm mới team để cập nhật status → "PendingApproval" (KHÔNG còn nhảy thẳng
      // "Registered" — đã xác nhận qua code thật ConfirmTeamRegistrationCommandHandler).
      qc.invalidateQueries({ queryKey: TEAM_KEYS.myTeam(eventId, userId) });
      qc.invalidateQueries({ queryKey: TEAM_KEYS.detail(teamId) });
    },
  });
};

/** EC/Admin duyệt đội đang "PendingApproval" -> "Registered". Chỉ EventCoordinator
 *  được BE cho phép gọi (403 nếu sai role) — xem teamsApi.approveTeam.
 *  Dùng CHUNG cho cả danh sách nhiều đội (vd TeamListTab.tsx) — nhận `teamId` lúc
 *  gọi `.mutate(teamId)`, KHÔNG nhận lúc tạo hook (tránh gọi hook trong `.map()`
 *  theo từng dòng — vi phạm Rules of Hooks). */
export const useApproveTeamRegistration = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (teamId: string) => teamsApi.approveTeam(teamId),
    onSuccess: (_data, teamId) => {
      qc.invalidateQueries({ queryKey: TEAM_KEYS.detail(teamId) });
    },
  });
};

/** EC/Admin từ chối đội đang "PendingApproval" -> "Forming" (kèm lý do bắt buộc,
 *  BE gửi email cho trưởng nhóm, KHÔNG lưu lại lý do — xem teamsApi.rejectTeam).
 *  Dùng chung cho danh sách — gọi `.mutate({ teamId, reason })`. */
export const useRejectTeamRegistration = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ teamId, reason }: { teamId: string; reason: string }) =>
      teamsApi.rejectTeam(teamId, reason),
    onSuccess: (_data, vars) => {
      qc.invalidateQueries({ queryKey: TEAM_KEYS.detail(vars.teamId) });
    },
  });
};

export const useCreateTeam = (_eventId: string, _userId: string) => {
  // The caller seeds the freshly-created team into the myTeam cache so the team
  // view shows immediately; invalidating here would race a refetch that could
  // overwrite that seed before the backend roster is consistent.
  return useMutation({
    mutationFn: (p: CreateTeamPayload) => teamsApi.create(p),
  });
};

export const useInviteToTeam = (teamId: string) => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ email }: { email: string }) => teamsApi.invite(teamId, email),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: TEAM_KEYS.detail(teamId) });
      qc.invalidateQueries({ queryKey: TEAM_KEYS.invitations(teamId) });
    },
  });
};

export const useTransferLeader = (teamId: string, eventId: string, userId: string) => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (newLeaderUserId: string) => teamsApi.transferLeader(teamId, newLeaderUserId),
    onSuccess: () => {
      // Vai trò trong đội đổi -> làm mới đội của tôi + chi tiết đội.
      qc.invalidateQueries({ queryKey: TEAM_KEYS.myTeam(eventId, userId) });
      qc.invalidateQueries({ queryKey: TEAM_KEYS.detail(teamId) });
    },
  });
};

export const useLeaveTeam = (teamId: string, eventId: string, userId: string) => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: () => teamsApi.leave(teamId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: TEAM_KEYS.myTeam(eventId, userId) });
      qc.removeQueries({ queryKey: TEAM_KEYS.detail(teamId) });
    },
  });
};

/** Leader mời một thành viên rời đội (kèm lý do tuỳ chọn — BE gửi email cho người bị mời rời). */
export const useRemoveMember = (teamId: string, eventId: string, userId: string) => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ memberUserId, reason }: { memberUserId: string; reason?: string }) =>
      teamsApi.removeMember(teamId, memberUserId, reason),
    onSuccess: () => {
      // Làm mới danh sách thành viên + lời mời (dòng của người vừa rời sẽ hiện "Đã rời đội")
      qc.invalidateQueries({ queryKey: TEAM_KEYS.myTeam(eventId, userId) });
      qc.invalidateQueries({ queryKey: TEAM_KEYS.detail(teamId) });
      qc.invalidateQueries({ queryKey: TEAM_KEYS.invitations(teamId) });
    },
  });
};
