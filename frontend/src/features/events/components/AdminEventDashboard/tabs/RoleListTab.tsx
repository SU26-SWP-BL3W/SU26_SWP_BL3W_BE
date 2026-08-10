'use client';

import React, { useRef, useState, useMemo } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useEventRoles, useEventTracks } from '@/features/events/hooks/useEvents';
import { manageApi, EVENT_ROLE } from '@/features/events/api/manage';
import type { TrackItem } from '@/features/events/api/manage';
import { usersApi } from '@/services/api';
import { useNotify } from '@/components/NotificationProvider';
import { useDialog } from '@/components/ConfirmDialogProvider';
import { getErrorMessage } from '@/lib/apiError';
import { Card } from '../../EventDashboard/Card';
import { Button } from '../../EventDashboard/Button';
import { CardSkeleton } from '../../EventDashboard/SkeletonLoaders';
import { EventRoleBadge } from '@/components/EventRoleBadge';

interface RoleListTabProps {
  eventId: string;
}

interface RoleRow {
  id: string; // userId
  name: string;
  email: string;
  roleName: string;
  roleId: string;
  trackName: string | null;
}

const ROLE_LABELS: Record<string, string> = {
  EventCoordinator: 'Ban tổ chức',
  Judge: 'Giám khảo',
  Mentor: 'Cố vấn',
  TeamLeader: 'Trưởng nhóm',
  TeamMember: 'Thành viên',
};

const FILTER_OPTIONS = [
  { label: 'Tất cả vai trò', value: 'all' },
  { label: 'Ban tổ chức', value: EVENT_ROLE.EventCoordinator },
  { label: 'Giám khảo', value: EVENT_ROLE.Judge },
  { label: 'Cố vấn', value: EVENT_ROLE.Mentor },
  { label: 'Trưởng nhóm', value: EVENT_ROLE.TeamLeader },
  { label: 'Thành viên', value: EVENT_ROLE.TeamMember },
];

export function RoleListTab({ eventId }: RoleListTabProps) {
  const [filterRole, setFilterRole] = useState<number | 'all'>('all');
  
  const { data: roles = [], isLoading, error } = useEventRoles(
    eventId, 
    filterRole === 'all' ? undefined : filterRole
  );

  const { data: tracks = [] } = useEventTracks(eventId);
  
  const queryClient = useQueryClient();
  const notify = useNotify();
  const dialog = useDialog();
  const [adding, setAdding] = useState(false);
  const [actionError, setActionError] = useState<string | null>(null);

  const invalidate = () =>
    queryClient.invalidateQueries({ queryKey: ['eventRoles', eventId] });

  const onMutationOk = () => {
    setActionError(null);
    notify.success('Đã gỡ vai trò thành công!');
    invalidate();
  };

  const onMutationError = (e: unknown) => {
    const msg = getErrorMessage(e);
    setActionError(msg);
    notify.error(msg);
  };

  const removeRoleMutation = useMutation({
    mutationFn: async (roleId: string) => {
      await manageApi.removeRole(roleId);
    },
    onSuccess: onMutationOk,
    onError: onMutationError,
  });
  
  const busy = removeRoleMutation.isPending;

  const trackMap = useMemo(() => {
    return new Map(tracks.map((t) => [t.id, t.trackName ?? '(không tên)']));
  }, [tracks]);

  const roleRows = useMemo(() => {
    const rows: RoleRow[] = [];
    for (const r of roles) {
      if (!r.userId) continue;
      rows.push({
        id: r.userId,
        name: r.user?.fullName ?? '—',
        email: r.user?.email ?? '',
        roleName: (() => {
          const lower = (r.roleName ?? '').toLowerCase();
          if (lower === 'eventcoordinator') return 'EventCoordinator';
          if (lower === 'judge') return 'Judge';
          if (lower === 'mentor') return 'Mentor';
          if (lower === 'teamleader') return 'TeamLeader';
          if (lower === 'teammember' || lower === 'member') return 'TeamMember';
          return r.roleName ?? 'Unknown';
        })(),
        roleId: r.id,
        trackName: r.trackId ? (trackMap.get(r.trackId) ?? 'Hạng mục đã xóa') : null,
      });
    }
    return rows;
  }, [roles, trackMap]);

  async function handleRemoveRole(row: RoleRow) {
    const roleLabel = ROLE_LABELS[row.roleName] ?? row.roleName;
    const ok = await dialog.confirm({
      title: 'Xóa vai trò',
      message: `Xóa vai trò ${roleLabel} của ${row.name}?`,
      confirmText: 'Xóa vai trò',
      danger: true,
    });
    if (!ok) return;
    removeRoleMutation.mutate(row.roleId);
  }

  if (error) {
    return (
      <div className="bg-error/10 border border-error rounded-sm p-6 text-center">
        <p className="t-body-md text-error font-bold">Không tải được danh sách vai trò</p>
      </div>
    );
  }
  if (isLoading) return <CardSkeleton />;

  return (
    <Card title="Danh sách vai trò">
      <div className="flex flex-col gap-4">
        <div className="flex items-center justify-between gap-4 flex-wrap">
          <div className="flex items-center gap-3">
            <span className="t-body-sm text-mute">{roleRows.length} tài khoản</span>
            <select
              className="text-input t-body-sm"
              style={{ padding: '6px 12px', minWidth: '180px' }}
              value={filterRole}
              onChange={(e) => {
                const val = e.target.value;
                setFilterRole(val === 'all' ? 'all' : Number(val));
              }}
            >
              {FILTER_OPTIONS.map((opt) => (
                <option key={opt.value} value={opt.value}>
                  {opt.label}
                </option>
              ))}
            </select>
          </div>
          
          <Button
            variant="create"
            size="sm"
            onClick={() => {
              setActionError(null);
              setAdding((v) => !v);
            }}
          >
            {adding ? 'Đóng' : '+ Thêm vai trò'}
          </Button>
        </div>

        {actionError && <p className="t-caption-sm text-error m-0">{actionError}</p>}

        {adding && (
          <AddRolePanel
            eventId={eventId}
            tracks={tracks}
            onSuccess={() => {
              setAdding(false);
              invalidate();
            }}
          />
        )}

        {roleRows.length > 0 ? (
          <div className="overflow-x-auto">
            <table className="w-full border-collapse">
              <thead>
                <tr className="border-b border-hairline-strong text-left">
                  <th className="t-caption-md text-mute font-bold uppercase py-3 px-2">Tên</th>
                  <th className="t-caption-md text-mute font-bold uppercase py-3 px-2">Email</th>
                  <th className="t-caption-md text-mute font-bold uppercase py-3 px-2">Vai trò</th>
                  <th className="t-caption-md text-mute font-bold uppercase py-3 px-2">Hạng mục</th>
                  <th className="t-caption-md text-mute font-bold uppercase py-3 px-2 text-right">Thao tác</th>
                </tr>
              </thead>
              <tbody>
                {roleRows.map((row) => (
                  <tr key={row.roleId} className="border-b border-hairline last:border-b-0">
                    <td className="t-body-sm font-bold text-ink py-3 px-2">{row.name}</td>
                    <td className="t-body-sm text-body py-3 px-2">{row.email}</td>
                    <td className="py-3 px-2">
                      <EventRoleBadge roleName={row.roleName} />
                    </td>
                    <td className="t-body-sm text-body py-3 px-2">
                      {row.trackName ?? <span className="text-mute">—</span>}
                    </td>
                    <td className="py-3 px-2">
                      <div className="flex justify-end">
                        <button
                          type="button"
                          disabled={busy}
                          onClick={() => handleRemoveRole(row)}
                          className="btn btn-delete btn-sm disabled:opacity-50"
                          style={{ cursor: busy ? 'not-allowed' : 'pointer' }}
                        >
                          Xóa
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        ) : (
          <p className="t-body-sm text-mute text-center py-8">Không có dữ liệu</p>
        )}
      </div>
    </Card>
  );
}

function AddRolePanel({
  eventId,
  tracks,
  onSuccess,
}: {
  eventId: string;
  tracks: TrackItem[];
  onSuccess: () => void;
}) {
  const [query, setQuery] = useState('');
  const [debounced, setDebounced] = useState('');
  const [manualFullName, setManualFullName] = useState('');
  const [selectedRole, setSelectedRole] = useState<number>(EVENT_ROLE.EventCoordinator);
  const [selectedTrackId, setSelectedTrackId] = useState<string>('');
  const [actionError, setActionError] = useState<string | null>(null);
  const notify = useNotify();

  const timerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  const searchQuery = useQuery({
    queryKey: ['userSearch', debounced],
    enabled: debounced.length >= 2,
    queryFn: () => usersApi.search(debounced),
    staleTime: 30_000,
  });
  
  const results = searchQuery.data ?? [];

  const onType = (v: string) => {
    setQuery(v);
    if (timerRef.current) clearTimeout(timerRef.current);
    timerRef.current = setTimeout(() => setDebounced(v.trim()), 250);
  };

  const onInviteError = (e: unknown) => {
    const msg = getErrorMessage(e, 'Không thể thêm người này.');
    setActionError(msg);
    notify.error(msg);
  };

  const inviteCoordinatorMutation = useMutation({
    mutationFn: manageApi.inviteEventCoordinator,
    onSuccess: () => {
      notify.success('Đã mời Ban tổ chức thành công!');
      onSuccess();
    },
    onError: onInviteError,
  });

  const inviteJudgeMutation = useMutation({
    mutationFn: manageApi.inviteJudge,
    onSuccess: () => {
      notify.success('Đã mời Giám khảo thành công!');
      onSuccess();
    },
    onError: onInviteError,
  });

  const inviteMentorMutation = useMutation({
    mutationFn: manageApi.inviteMentor,
    onSuccess: () => {
      notify.success('Đã mời Cố vấn thành công!');
      onSuccess();
    },
    onError: onInviteError,
  });

  const busy = inviteCoordinatorMutation.isPending || inviteJudgeMutation.isPending || inviteMentorMutation.isPending;

  const handleInvite = (email: string, fullName: string) => {
    setActionError(null);
    if (selectedRole !== EVENT_ROLE.EventCoordinator && !selectedTrackId) {
      setActionError("Vui lòng chọn một hạng mục.");
      return;
    }

    if (selectedRole === EVENT_ROLE.EventCoordinator) {
      inviteCoordinatorMutation.mutate({
        eventId,
        coordinatorEmail: email,
        coordinatorFullName: fullName || email,
        notes: ""
      });
    } else if (selectedRole === EVENT_ROLE.Judge) {
      inviteJudgeMutation.mutate({
        eventId,
        trackId: selectedTrackId,
        judgeEmail: email,
        judgeFullName: fullName || email,
        notes: ""
      });
    } else if (selectedRole === EVENT_ROLE.Mentor) {
      inviteMentorMutation.mutate({
        eventId,
        trackId: selectedTrackId,
        mentorEmail: email,
        mentorFullName: fullName || email,
        notes: ""
      });
    }
  };

  const handleManualInvite = () => {
    const email = query.trim();
    const name = manualFullName.trim();
    if (!email) {
      setActionError("Vui lòng nhập email hợp lệ.");
      return;
    }
    handleInvite(email, name);
  };

  const requiresTrack = selectedRole === EVENT_ROLE.Judge || selectedRole === EVENT_ROLE.Mentor;

  return (
    <div className="border border-hairline-strong rounded-sm p-4 flex flex-col gap-4 bg-surface-soft">
      <div className="flex justify-between items-center">
        <p className="t-body-strong text-ink m-0">Thêm vai trò mới</p>
      </div>
      
      {actionError && <p className="t-caption-sm text-error m-0">{actionError}</p>}

      <div className="flex flex-col md:flex-row gap-3">
        <div className="flex-1">
          <label className="block t-caption-sm font-bold text-mute mb-1">Vai trò gán</label>
          <select 
            className="text-input"
            style={{ width: '100%' }}
            value={selectedRole}
            onChange={(e) => setSelectedRole(Number(e.target.value))}
          >
            <option value={EVENT_ROLE.EventCoordinator}>Ban tổ chức</option>
            <option value={EVENT_ROLE.Judge}>Giám khảo</option>
            <option value={EVENT_ROLE.Mentor}>Cố vấn</option>
          </select>
        </div>
        
        {requiresTrack && (
          <div className="flex-1">
            <label className="block t-caption-sm font-bold text-mute mb-1">Hạng mục</label>
            {tracks.length === 0 ? (
              <p className="t-caption-sm text-error m-0" style={{ marginTop: '8px' }}>
                Sự kiện chưa có hạng mục nào. Hãy tạo hạng mục trước.
              </p>
            ) : (
              <select 
                className="text-input"
                style={{ width: '100%' }}
                value={selectedTrackId}
                onChange={(e) => setSelectedTrackId(e.target.value)}
              >
                <option value="">-- Chọn hạng mục --</option>
                {tracks.map(t => (
                  <option key={t.id} value={t.id}>{t.trackName ?? '(không tên)'}</option>
                ))}
              </select>
            )}
          </div>
        )}
      </div>

      <div>
        <label className="block t-caption-sm font-bold text-mute mb-1">Email người dùng</label>
        <input
          className="text-input"
          value={query}
          placeholder="Nhập email để tìm tài khoản…"
          onChange={(e) => onType(e.target.value)}
          style={{ width: '100%' }}
        />
      </div>

      {debounced.length >= 2 && (
        <div className="flex flex-col gap-2 mt-2">
          {searchQuery.isLoading ? (
            <p className="t-caption-sm text-mute m-0">Đang tìm…</p>
          ) : results.length === 0 ? (
            <div className="flex flex-col gap-3">
              <p className="t-caption-sm text-mute m-0">Không tìm thấy người dùng. Vui lòng nhập họ tên để mời email này.</p>
              <input
                className="text-input"
                value={manualFullName}
                onChange={(e) => setManualFullName(e.target.value)}
                placeholder="Nhập họ và tên..."
                style={{ width: '100%' }}
              />
              <button
                type="button"
                disabled={busy}
                onClick={handleManualInvite}
                className="self-start px-3 py-2 bg-primary text-white t-caption-sm font-bold rounded-sm disabled:opacity-50 cursor-pointer"
                style={{ border: 'none' }}
              >
                {busy ? 'Đang xử lý...' : 'Gửi lời mời'}
              </button>
            </div>
          ) : (
            results.map((u) => (
              <div
                key={u.id}
                className="flex items-center justify-between gap-3 bg-canvas border border-hairline rounded-sm px-3 py-2"
              >
                <div className="flex flex-col">
                  <span className="t-body-sm text-ink">{u.email ?? '(không có email)'}</span>
                  {u.fullName && <span className="t-caption-sm text-mute">{u.fullName}</span>}
                </div>
                <button
                  type="button"
                  disabled={busy}
                  onClick={() => handleInvite(u.email ?? '', u.fullName ?? '')}
                  className="inline-flex items-center gap-1 t-caption-sm font-bold text-primary disabled:opacity-50"
                  style={{ background: 'none', border: 'none', cursor: busy ? 'not-allowed' : 'pointer' }}
                >
                  {busy ? 'Đang xử lý...' : 'Gửi lời mời'}
                </button>
              </div>
            ))
          )}
        </div>
      )}
    </div>
  );
}
