'use client';

import React, { useRef, useState } from 'react';
import { ChevronDown } from 'lucide-react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useEventRoles, useEventTracks } from '@/features/events/hooks/useEvents';
import { isEventCoordinatorRole, isJudgeRole, isMentorRole, manageApi, EVENT_ROLE } from '@/features/events/api/manage';
import { usersApi, type UserSummary } from '@/services/api';
import { useNotify } from '@/components/NotificationProvider';
import { useDialog } from '@/components/ConfirmDialogProvider';
import { getErrorMessage } from '@/lib/apiError';
import { Card } from '../../EventDashboard/Card';
import { Button } from '../../EventDashboard/Button';
import { CardSkeleton } from '../../EventDashboard/SkeletonLoaders';

interface RoleAssignmentTabProps {
  eventId: string;
}

/** The 3 roles an admin can assign. Ban tổ chức is event-level; the rest are per-track. */
const ASSIGNABLE_ROLES = [
  { value: EVENT_ROLE.EventCoordinator, label: 'Ban tổ chức', needsTrack: false },
  { value: EVENT_ROLE.Judge, label: 'Giám khảo', needsTrack: true },
  { value: EVENT_ROLE.Mentor, label: 'Cố vấn', needsTrack: true },
] as const;

export function RoleAssignmentTab({ eventId }: RoleAssignmentTabProps) {
  const { data: roles = [], isLoading: rolesLoading, error } = useEventRoles(eventId);
  const { data: tracks = [], isLoading: tracksLoading } = useEventTracks(eventId);
  const isLoading = rolesLoading || tracksLoading;
  const queryClient = useQueryClient();
  const notify = useNotify();
  const dialog = useDialog();
  const [adding, setAdding] = useState(false);
  const [actionError, setActionError] = useState<string | null>(null);
  const [activeKey, setActiveKey] = useState<'coordinator' | 'judge' | 'mentor'>('coordinator');

  const invalidate = () =>
    queryClient.invalidateQueries({ queryKey: ['eventRoles', eventId] });
  const onOk = () => {
    setActionError(null);
    invalidate();
  };
  const showErr = (e: unknown, fallback?: string) => {
    const msg = getErrorMessage(e, fallback);
    setActionError(msg);
    notify.error(msg);
  };
  const onErr = (e: unknown) => showErr(e);

  const assignMutation = useMutation({
    mutationFn: manageApi.assignRole,
    onSuccess: () => {
      notify.success('Đã phân vai trò thành công!');
      onOk();
    },
    onError: (e) => showErr(e, 'Không thể thêm người này.'),
  });
  const removeMutation = useMutation({
    mutationFn: (roleId: string) => manageApi.removeRole(roleId),
    onSuccess: () => {
      notify.success('Đã gỡ vai trò thành công!');
      onOk();
    },
    onError: onErr,
  });
  const busy = assignMutation.isPending || removeMutation.isPending;

  const trackName = new Map(tracks.map((t) => [t.id, t.trackName ?? '(không tên)']));

  // One row per role assignment (a user may hold several: judge on one track, mentor on another).
  // Team-scoped roles (teamId set) belong to the "Danh sách đội" tab, so skip them here.
  const toRow = (r: (typeof roles)[number]): RoleRow => ({
    roleId: r.id,
    name: r.user?.fullName ?? '—',
    email: r.user?.email ?? '',
    track: r.trackId ? (trackName.get(r.trackId) ?? '(hạng mục đã xóa)') : null,
  });
  const eligible = roles.filter((r) => r.userId && !r.teamId);
  const coordinators = eligible.filter(isEventCoordinatorRole).map(toRow);
  const judges = eligible.filter(isJudgeRole).map(toRow);
  const mentors = eligible.filter(isMentorRole).map(toRow);
  const total = coordinators.length + judges.length + mentors.length;

  const groups = [
    { key: 'coordinator' as const, label: 'Ban tổ chức', rows: coordinators, showTrack: false },
    { key: 'judge' as const, label: 'Giám khảo', rows: judges, showTrack: true },
    { key: 'mentor' as const, label: 'Cố vấn', rows: mentors, showTrack: true },
  ];
  const activeGroup = groups.find((g) => g.key === activeKey) ?? groups[0];

  async function handleRemove(roleId: string, name: string, roleLabel: string) {
    const ok = await dialog.confirm({
      title: 'Gỡ vai trò',
      message: `Gỡ vai trò "${roleLabel}" của ${name}?`,
      confirmText: 'Gỡ vai trò',
      danger: true,
    });
    if (!ok) return;
    removeMutation.mutate(roleId);
  }

  if (error) {
    return (
      <div className="bg-error/10 border border-error rounded-sm p-6 text-center">
        <p className="t-body-md text-error font-bold">Không tải được danh sách phân quyền</p>
      </div>
    );
  }
  if (isLoading) return <CardSkeleton />;

  return (
    <Card title="Phân quyền">
      <div className="flex flex-col gap-4">
        <div className="flex items-center justify-between gap-2 flex-wrap">
          <span className="t-body-sm text-mute">{total} lượt phân quyền</span>
          <Button
            variant="create"
            size="sm"
            onClick={() => {
              setActionError(null);
              setAdding((v) => !v);
            }}
          >
            {adding ? 'Đóng' : 'Phân quyền'}
          </Button>
        </div>

        {actionError && <p className="t-caption-sm text-error m-0">{actionError}</p>}

        {adding && (
          <AssignRolePanel
            tracks={tracks.map((t) => ({ id: t.id, name: t.trackName ?? '(không tên)' }))}
            busy={busy}
            onAssign={(payload) => assignMutation.mutate({ ...payload, eventId })}
          />
        )}

        {/* Sub-tab selector: pick a role group to view its list. */}
        <div className="flex flex-wrap gap-2" role="tablist" aria-label="Chọn nhóm vai trò">
          {groups.map((g) => {
            const isActive = g.key === activeGroup.key;
            return (
              <button
                key={g.key}
                type="button"
                role="tab"
                aria-selected={isActive}
                onClick={() => setActiveKey(g.key)}
                className="t-body-strong font-bold"
                style={{
                  flex: '1 1 160px',
                  minHeight: 52,
                  padding: '14px 20px',
                  borderRadius: 'var(--radius-sm)',
                  border: `1px solid ${isActive ? 'var(--color-primary)' : 'var(--color-hairline-strong)'}`,
                  background: isActive ? 'var(--color-primary)' : 'transparent',
                  color: isActive ? 'var(--color-on-primary)' : 'var(--color-ink)',
                  cursor: 'pointer',
                }}
              >
                {g.label} ({g.rows.length})
              </button>
            );
          })}
        </div>

        <RoleTable
          rows={activeGroup.rows}
          showTrack={activeGroup.showTrack}
          busy={busy}
          onRemove={(id, name) => handleRemove(id, name, activeGroup.label)}
        />
      </div>
    </Card>
  );
}

// ─── One role group's list ──────────────────────────────────────────────────────

interface RoleRow {
  roleId: string;
  name: string;
  email: string;
  track: string | null;
}

function RoleTable({
  rows,
  showTrack,
  busy,
  onRemove,
}: {
  rows: RoleRow[];
  showTrack: boolean;
  busy: boolean;
  onRemove: (roleId: string, name: string) => void;
}) {
  if (rows.length === 0) {
    return <p className="t-body-sm text-mute text-center py-8">Chưa có tài khoản nào trong nhóm này</p>;
  }

  return (
    <div className="overflow-x-auto">
      <table className="w-full border-collapse">
        <thead>
          <tr className="border-b border-hairline-strong text-left">
            <th className="t-caption-md text-mute font-bold uppercase py-3 px-2">Tài khoản</th>
            {showTrack && <th className="t-caption-md text-mute font-bold uppercase py-3 px-2">Hạng mục</th>}
            <th className="t-caption-md text-mute font-bold uppercase py-3 px-2 text-right">Thao tác</th>
          </tr>
        </thead>
        <tbody>
          {rows.map((row) => (
            <tr key={row.roleId} className="border-b border-hairline last:border-b-0">
              <td className="py-3 px-2">
                <span className="block t-body-sm font-bold text-ink">{row.name}</span>
                <span className="t-caption-sm text-mute">{row.email}</span>
              </td>
              {showTrack && (
                <td className="t-body-sm text-body py-3 px-2">
                  {row.track ?? <span className="text-mute">—</span>}
                </td>
              )}
              <td className="py-3 px-2">
                <div className="flex justify-end">
                  <button
                    type="button"
                    disabled={busy}
                    onClick={() => onRemove(row.roleId, row.name)}
                    className="btn btn-delete btn-sm disabled:opacity-50"
                    style={{ minHeight: 32, cursor: busy ? 'not-allowed' : 'pointer' }}
                  >
                    Gỡ
                  </button>
                </div>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

// ─── Assign-role panel ──────────────────────────────────────────────────────────

interface AssignPayload {
  userId: string;
  roleName: number;
  trackId: string | null;
}

function AssignRolePanel({
  tracks,
  busy,
  onAssign,
}: {
  tracks: { id: string; name: string }[];
  busy: boolean;
  onAssign: (payload: AssignPayload) => void;
}) {
  const [query, setQuery] = useState('');
  const [debounced, setDebounced] = useState('');
  const [picked, setPicked] = useState<UserSummary | null>(null);
  const [roleValue, setRoleValue] = useState<number>(ASSIGNABLE_ROLES[0].value);
  const [trackId, setTrackId] = useState<string>('');
  const timerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  const searchQuery = useQuery({
    queryKey: ['userSearch', debounced],
    enabled: debounced.length >= 2 && !picked,
    queryFn: () => usersApi.search(debounced),
    staleTime: 30_000,
  });
  const results = searchQuery.data ?? [];

  const onType = (v: string) => {
    setQuery(v);
    if (timerRef.current) clearTimeout(timerRef.current);
    timerRef.current = setTimeout(() => setDebounced(v.trim()), 250);
  };

  const role = ASSIGNABLE_ROLES.find((r) => r.value === roleValue)!;
  const needsTrack = role.needsTrack;
  const noTracks = needsTrack && tracks.length === 0;
  const canAssign =
    !!picked && !busy && (!needsTrack || (!!trackId && tracks.some((t) => t.id === trackId)));

  const reset = () => {
    setPicked(null);
    setQuery('');
    setDebounced('');
    setTrackId('');
  };

  return (
    <div className="border border-hairline-strong rounded-sm p-4 flex flex-col gap-3 bg-surface-soft">
      <p className="t-body-strong text-ink m-0">Phân quyền cho tài khoản</p>

      {/* 1. Pick an account */}
      {picked ? (
        <div className="flex items-center justify-between gap-3 bg-canvas border border-hairline rounded-sm px-3 py-2">
          <div className="flex flex-col">
            <span className="t-body-sm text-ink">{picked.email ?? '(không có email)'}</span>
            {picked.fullName && <span className="t-caption-sm text-mute">{picked.fullName}</span>}
          </div>
          <button
            type="button"
            onClick={reset}
            className="t-caption-sm font-bold text-mute"
            style={{ background: 'none', border: 'none', cursor: 'pointer' }}
          >
            Đổi
          </button>
        </div>
      ) : (
        <>
          <input
            className="text-input"
            value={query}
            placeholder="Nhập email để tìm tài khoản…"
            onChange={(e) => onType(e.target.value)}
            style={{ width: '100%' }}
          />
          {debounced.length >= 2 && (
            <div className="flex flex-col gap-2">
              {searchQuery.isLoading ? (
                <p className="t-caption-sm text-mute m-0">Đang tìm…</p>
              ) : results.length === 0 ? (
                <p className="t-caption-sm text-mute m-0">Không tìm thấy người dùng.</p>
              ) : (
                results.map((u) => (
                  <button
                    key={u.id}
                    type="button"
                    onClick={() => setPicked(u)}
                    className="w-full text-left flex items-center justify-between gap-3 bg-canvas border border-hairline rounded-sm px-3 py-2 cursor-pointer transition-colors duration-150 hover:bg-surface-soft hover:border-primary focus-visible:outline-2 focus-visible:outline-offset-0 focus-visible:outline-primary"
                  >
                    <span className="flex flex-col">
                      <span className="t-body-sm text-ink">{u.email ?? '(không có email)'}</span>
                      {u.fullName && <span className="t-caption-sm text-mute">{u.fullName}</span>}
                    </span>
                  </button>
                ))
              )}
            </div>
          )}
        </>
      )}

      {/* 2. Pick a role */}
      <div className="flex flex-col gap-2">
        <span className="t-caption-xs text-mute">Vai trò</span>
        <div className="flex flex-wrap gap-2">
          {ASSIGNABLE_ROLES.map((r) => {
            const active = r.value === roleValue;
            return (
              <button
                key={r.value}
                type="button"
                onClick={() => {
                  setRoleValue(r.value);
                  if (!r.needsTrack) setTrackId('');
                }}
                className="t-caption-sm font-bold"
                style={{
                  padding: '6px 12px',
                  borderRadius: 'var(--radius-sm)',
                  border: `1px solid ${active ? 'var(--color-primary)' : 'var(--color-hairline-strong)'}`,
                  background: active ? 'var(--color-primary)' : 'transparent',
                  color: active ? 'var(--color-on-primary)' : 'var(--color-ink)',
                  cursor: 'pointer',
                }}
              >
                {r.label}
              </button>
            );
          })}
        </div>
      </div>

      {/* 3. Pick a hạng mục (only for per-hạng-mục roles) */}
      {needsTrack && (
        <div className="flex flex-col gap-2">
          <span className="t-caption-xs text-mute">Hạng mục</span>
          {noTracks ? (
            <p className="t-caption-sm text-error m-0">Sự kiện chưa có hạng mục nào. Hãy tạo hạng mục trước.</p>
          ) : (
            <div className="relative">
              <select
                className="text-input"
                value={trackId}
                onChange={(e) => setTrackId(e.target.value)}
                style={{ appearance: 'none', WebkitAppearance: 'none', paddingRight: 44, width: '100%' }}
              >
                <option value="">— Chọn hạng mục —</option>
                {tracks.map((t) => (
                  <option key={t.id} value={t.id}>{t.name}</option>
                ))}
              </select>
              <ChevronDown
                size={24}
                aria-hidden="true"
                className="absolute right-3 top-1/2 -translate-y-1/2 text-ink pointer-events-none"
              />
            </div>
          )}
        </div>
      )}

      {/* 4. Submit */}
      <div className="flex justify-end">
        <Button
          variant="create"
          size="sm"
          disabled={!canAssign}
          onClick={() => {
            if (!picked) return;
            onAssign({
              userId: picked.id,
              roleName: roleValue,
              trackId: needsTrack ? trackId : null,
            });
            reset();
          }}
        >
          Phân quyền
        </Button>
      </div>
    </div>
  );
}
