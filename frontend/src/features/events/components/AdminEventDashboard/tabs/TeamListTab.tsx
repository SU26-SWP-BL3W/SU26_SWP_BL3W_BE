'use client';

import React, { useState } from 'react';
import { EventRoleBadge } from '@/components/EventRoleBadge';
import { Search } from 'lucide-react';
import { useQuery } from '@tanstack/react-query';
import { useEventRoles, useTeams } from '@/features/events/hooks/useEvents';
import { useNotify } from '@/components/NotificationProvider';
import { useDialog } from '@/components/ConfirmDialogProvider';
import { getErrorMessage } from '@/lib/apiError';
import { useApproveTeamRegistration, useRejectTeamRegistration } from '@/features/teams/hooks/useTeams';
import { teamsApi } from '@/features/teams/api/teams';
import { TeamStatusBadge } from '@/features/teams/components/TeamStatusBadge';
import type { TeamStatus } from '@/features/teams/types/team.types';
import { Card } from '../../EventDashboard/Card';
import { CardSkeleton } from '../../EventDashboard/SkeletonLoaders';

interface TeamListTabProps {
  eventId: string;
}

interface TeamMember {
  userId: string;
  name: string;
  email: string;
  roles: string[];
  studentCode?: string | null;
  isApproved?: boolean;
  isFpt?: boolean;
}

/** Chuẩn hóa roleName tự do của backend để dùng chung bảng màu và nhãn vai trò. */
const roleNameOf = (roleName: string | null): string => {
  switch ((roleName ?? '').toLowerCase()) {
    case 'mentor': return 'Mentor';
    case 'judge': return 'Judge';
    case 'participant':
    case 'member':
    case 'teammember': return 'TeamMember';
    case 'teamleader': return 'TeamLeader';
    case 'admin': return 'Admin';
    case 'eventcoordinator': return 'EventCoordinator';
    default: return roleName || '';
  }
};

export function TeamListTab({ eventId }: TeamListTabProps) {
  const { data: roles = [], isLoading: rolesLoading, error } = useEventRoles(eventId);
  const { data: allTeams = [], isLoading: teamsLoading, refetch: refetchTeams } = useTeams(eventId);
  const isLoading = rolesLoading || teamsLoading;
  const notify = useNotify();
  const dialog = useDialog();
  const [viewTeamId, setViewTeamId] = useState<string | null>(null);

  const approveMutation = useApproveTeamRegistration();
  const rejectMutation = useRejectTeamRegistration();

  async function handleApprove(team: { id: string; name: string }) {
    const ok = await dialog.confirm({
      title: 'Duyệt đội',
      message: `Duyệt đăng ký của đội "${team.name}"? Đội sẽ chính thức được thi đấu.`,
      confirmText: 'Duyệt đội',
    });
    if (!ok) return;
    try {
      await approveMutation.mutateAsync(team.id);
      notify.success(`Đã duyệt đội "${team.name}" thành công!`);
      refetchTeams();
      refetchStatuses();
    } catch (e) {
      notify.error(getErrorMessage(e, 'Duyệt đội thất bại, vui lòng thử lại.'));
      // Lỗi có thể do trạng thái hiển thị đang cũ (vd đội đã được duyệt/từ chối ở nơi
      // khác từ trước) — tải lại status thật để UI (badge + nút) đồng bộ ngay, tránh
      // người dùng bấm lại vô ích với cùng lỗi.
      refetchStatuses();
    }
  }

  async function handleReject(team: { id: string; name: string }) {
    // Cùng pattern dialog.prompt đã dùng cho từ chối tài khoản ở UsersList.tsx —
    // giữ nhất quán UX. Lý do CHỈ được BE dùng để gửi email cho trưởng nhóm, KHÔNG
    // lưu lại trong app (đã xác nhận qua code RejectTeamRegistrationCommandHandler),
    // nên sau khi từ chối app sẽ không hiển thị lại lý do này ở đâu cả.
    const reason = await dialog.prompt({
      title: 'Từ chối đội',
      message: `Từ chối đăng ký của đội "${team.name}"? Lý do sẽ được gửi email cho trưởng nhóm. Đội sẽ được mở khóa để sửa và chốt lại.`,
      label: 'Lý do từ chối',
      placeholder: 'VD: Thiếu thông tin thành viên, hồ sơ chưa hợp lệ…',
      required: true,
      multiline: true,
      danger: true,
      confirmText: 'Từ chối',
    });
    if (reason === null) return; // hủy
    try {
      await rejectMutation.mutateAsync({ teamId: team.id, reason });
      notify.success(`Đã từ chối đội "${team.name}", email đã được gửi cho trưởng nhóm.`);
      refetchTeams();
      refetchStatuses();
    } catch (e) {
      notify.error(getErrorMessage(e, 'Từ chối đội thất bại, vui lòng thử lại.'));
      refetchStatuses();
    }
  }

  const teamById = new Map(allTeams.map((t) => [t.id, t]));

  // Members per team, deduped by userId (a user may hold several roles in a team).
  const membersByTeam = new Map<string, Map<string, TeamMember>>();
  for (const role of roles) {
    if (!role.teamId || !role.userId) continue;
    if (!membersByTeam.has(role.teamId)) membersByTeam.set(role.teamId, new Map());
    const byUser = membersByTeam.get(role.teamId)!;
    const label = roleNameOf(role.roleName);
    const existing = byUser.get(role.userId);
    if (existing) {
      if (label && !existing.roles.includes(label)) existing.roles.push(label);
    } else {
      byUser.set(role.userId, {
        userId: role.userId,
        name: role.user?.fullName ?? '—',
        email: role.user?.email ?? '',
        roles: label ? [label] : [],
        studentCode: role.user?.studentCode,
        isApproved: role.user?.isApproved,
        isFpt: role.user?.isFpt,
      });
    }
  }
  const memberCount = (teamId: string) => membersByTeam.get(teamId)?.size ?? 0;

  const teamIds = [...new Set(roles.map((r) => r.teamId).filter(Boolean))] as string[];

  // `status` KHÔNG có trong GET /Teams (list, dùng bởi useTeams() ở trên) — chỉ
  // GET /Teams/{id} (detail) mới chắc chắn trả field này (đã xác nhận qua Swagger,
  // xem team.types.ts). Gọi thêm detail cho từng đội để lấy đúng status thật, thay
  // vì tin vào field optional ở TeamItem (list) — tránh nút Duyệt/Từ chối bị ẩn sai
  // do status luôn undefined.
  const { data: statusByTeam = {}, refetch: refetchStatuses } = useQuery({
    queryKey: ['teamStatuses', eventId, teamIds.join(',')],
    queryFn: async () => {
      const entries = await Promise.all(
        teamIds.map((id) =>
          teamsApi.getById(id)
            .then((t) => [id, t.status as TeamStatus | undefined] as const)
            .catch(() => [id, undefined] as const),
        ),
      );
      return Object.fromEntries(entries) as Record<string, TeamStatus | undefined>;
    },
    enabled: teamIds.length > 0,
    // Trạng thái đội có thể đổi bất kỳ lúc nào (chính EC này duyệt/từ chối, hoặc từ
    // phiên khác) — không cache lâu như dữ liệu tĩnh, luôn coi là cũ để tự fetch lại
    // mỗi khi tab được mount/focus lại, tránh hiện sai badge/nút như staleTime cũ.
    staleTime: 0,
  });

  const teams = teamIds.map((id) => ({
    id,
    name: teamById.get(id)?.name ?? id,
    description: teamById.get(id)?.description ?? '',
    memberCount: memberCount(id),
    status: statusByTeam[id],
  }));

  if (error) {
    return (
      <div className="bg-error/10 border border-error rounded-sm p-6 text-center">
        <p className="t-body-md text-error font-bold">Không tải được danh sách đội</p>
      </div>
    );
  }

  if (isLoading) return <CardSkeleton />;

  return (
    <Card title="Danh sách đội">
      <div className="flex flex-col gap-4">
        <div className="flex flex-col sm:flex-row gap-3 sm:items-center sm:justify-between">
          <div className="relative w-full sm:w-72">
            <Search size={16} className="absolute left-3 top-1/2 -translate-y-1/2 text-mute" aria-hidden="true" />
            <input
              type="text"
              placeholder="Tìm đội..."
              className="w-full pl-9 pr-3 py-2 border border-hairline rounded-sm t-body-sm text-ink bg-canvas focus-visible:outline-2 focus-visible:outline-offset-0 focus-visible:outline-primary"
            />
          </div>
          <span className="t-body-sm text-mute">{teams?.length ?? 0} đội</span>
        </div>

        {teams && teams.length > 0 ? (
          <div className="overflow-x-auto">
            <table className="w-full border-collapse">
              <thead>
                <tr className="border-b border-hairline-strong text-left">
                  <th className="t-caption-md text-mute font-bold uppercase py-3 px-2">Tên đội</th>
                  <th className="t-caption-md text-mute font-bold uppercase py-3 px-2">Mô tả</th>
                  <th className="t-caption-md text-mute font-bold uppercase py-3 px-2 text-center">Thành viên</th>
                  <th className="t-caption-md text-mute font-bold uppercase py-3 px-2">Trạng thái</th>
                  <th className="t-caption-md text-mute font-bold uppercase py-3 px-2 text-right">Thao tác</th>
                </tr>
              </thead>
              <tbody>
                {teams.map((team) => {
                  const expanded = viewTeamId === team.id;
                  const members = [...(membersByTeam.get(team.id)?.values() ?? [])];
                  const isPending = team.status === 'PendingApproval';
                  const approveBusy = approveMutation.isPending && approveMutation.variables === team.id;
                  const rejectBusy = rejectMutation.isPending && rejectMutation.variables?.teamId === team.id;
                  return (
                    <React.Fragment key={team.id}>
                      <tr
                        className="border-b border-hairline last:border-b-0"
                        style={isPending ? { background: 'rgba(245,158,11,0.06)' } : undefined}
                      >
                        <td className="t-body-sm font-bold text-ink py-3 px-2">{team.name}</td>
                        <td className="t-body-sm text-body py-3 px-2">
                          {team.description || <span className="text-mute">—</span>}
                        </td>
                        <td className="t-body-sm text-body py-3 px-2 text-center">{team.memberCount}</td>
                        <td className="py-3 px-2">
                          <TeamStatusBadge status={team.status} />
                        </td>
                        <td className="py-3 px-2">
                          <div className="flex justify-end gap-2 flex-wrap">
                            {isPending && (
                              <>
                                <button
                                  type="button"
                                  disabled={approveBusy || rejectBusy}
                                  onClick={() => handleApprove(team)}
                                  className="t-caption-sm font-bold"
                                  style={{
                                    background: 'none', border: '1px solid var(--color-primary)',
                                    color: 'var(--color-primary)', borderRadius: 'var(--radius-sm)',
                                    padding: '4px 10px', whiteSpace: 'nowrap',
                                    cursor: (approveBusy || rejectBusy) ? 'not-allowed' : 'pointer',
                                    opacity: (approveBusy || rejectBusy) ? 0.5 : 1,
                                  }}
                                >
                                  {approveBusy ? 'Đang lưu…' : 'Duyệt đội'}
                                </button>
                                <button
                                  type="button"
                                  disabled={approveBusy || rejectBusy}
                                  onClick={() => handleReject(team)}
                                  className="t-caption-sm font-bold"
                                  style={{
                                    background: 'none', border: '1px solid var(--color-error)',
                                    color: 'var(--color-error)', borderRadius: 'var(--radius-sm)',
                                    padding: '4px 10px', whiteSpace: 'nowrap',
                                    cursor: (approveBusy || rejectBusy) ? 'not-allowed' : 'pointer',
                                    opacity: (approveBusy || rejectBusy) ? 0.5 : 1,
                                  }}
                                >
                                  {rejectBusy ? 'Đang xử lý…' : 'Từ chối'}
                                </button>
                              </>
                            )}
                            <button
                              type="button"
                              onClick={() =>
                                setViewTeamId((cur) => (cur === team.id ? null : team.id))
                              }
                              className="btn btn-view btn-sm disabled:opacity-50"
                              style={{ cursor: 'pointer', whiteSpace: 'nowrap' }}
                            >
                              {expanded ? 'Ẩn thành viên' : 'Xem thành viên'}
                            </button>
                          </div>
                        </td>
                      </tr>
                      {expanded && (
                        <tr className="bg-surface-soft">
                          <td colSpan={5} className="px-2 py-3 align-top">
                            <TeamMembersPanel teamName={team.name} members={members} />
                          </td>
                        </tr>
                      )}
                    </React.Fragment>
                  );
                })}
              </tbody>
            </table>
          </div>
        ) : (
          <p className="t-body-sm text-mute text-center py-8">Chưa có đội nào đăng ký</p>
        )}
      </div>
    </Card>
  );
}

// ─── Team members panel ───────────────────────────────────────────────────────────

function TeamMembersPanel({
  teamName,
  members,
}: {
  teamName: string;
  members: TeamMember[];
}) {
  const [expandedUserId, setExpandedUserId] = useState<string | null>(null);

  return (
    <div className="flex flex-col gap-3">
      <p className="t-body-strong text-ink m-0">Thành viên đội: {teamName}</p>
      {members.length === 0 ? (
        <p className="t-caption-sm text-mute m-0">Đội chưa có thành viên.</p>
      ) : (
        <ul className="flex flex-col gap-2 m-0 p-0 list-none">
          {members.map((m) => {
            const isExpanded = expandedUserId === m.userId;
            return (
              <li
                key={m.userId}
                className="flex flex-col gap-2 bg-canvas border border-hairline rounded-sm px-3 py-2 cursor-pointer hover:bg-surface-soft transition-colors"
                onClick={() => setExpandedUserId(prev => prev === m.userId ? null : m.userId)}
              >
                <div className="flex items-center justify-between gap-3">
                  <div className="flex flex-col">
                    <span className="t-body-sm font-bold text-ink hover:text-primary transition-colors">{m.name}</span>
                    {m.email && <span className="t-caption-sm text-mute">{m.email}</span>}
                  </div>
                  <div className="flex flex-wrap justify-end gap-1">
                    {m.roles.map((r) => <EventRoleBadge key={r} roleName={r} />)}
                  </div>
                </div>
                {isExpanded && (
                  <div className="pt-2 mt-1 border-t border-hairline flex flex-col gap-1 t-caption-sm text-ink" onClick={e => e.stopPropagation()}>
                    <p><strong>MSSV:</strong> {m.studentCode || <span className="text-mute italic">Không có</span>}</p>
                    <p><strong>Loại tài khoản:</strong> {m.isFpt ? 'Sinh viên FPT' : 'Khách'}</p>
                    <p><strong>Hồ sơ người dùng:</strong> {m.isApproved ? <span className="text-success font-bold">Đã duyệt</span> : <span className="text-danger font-bold">Chưa duyệt</span>}</p>
                  </div>
                )}
              </li>
            );
          })}
        </ul>
      )}
    </div>
  );
}
