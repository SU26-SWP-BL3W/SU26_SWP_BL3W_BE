'use client';

import React, { useState } from 'react';
import { useMyTeamForEvent, useInviteToTeam, useLeaveTeam, useTeamInvitations, useTransferLeader, useConfirmRegistration, useRemoveMember } from '@/features/teams/hooks/useTeams';
import { useEventDashboard } from '@/features/events/contexts/EventDashboardContext';
import { useNotify } from '@/components/NotificationProvider';
import { useDialog } from '@/components/ConfirmDialogProvider';
import { EventRoleBadge } from '@/components/EventRoleBadge';
import { StatusBadge, type StatusTone } from '@/components/StatusBadge';
import { TeamStatusBadge } from '@/features/teams/components/TeamStatusBadge';
import { getErrorMessage } from '@/lib/apiError';

interface Props { eventId: string; userId: string; }

/** Màu badge theo trạng thái lời mời (phía người gửi). Nhãn chữ lấy từ BE (`statusLabel`);
 *  `label` ở đây chỉ là fallback khi BE bản cũ chưa trả statusLabel. */
const INVITE_BADGE: Record<string, { label: string; tone: StatusTone }> = {
  PendingAccept: { label: 'Đang chờ xác nhận', tone: 'pending' },
  Accepted: { label: 'Đã tham gia', tone: 'success' },
  Declined: { label: 'Đã từ chối', tone: 'danger' },
  Expired: { label: 'Hết hạn', tone: 'inactive' },
};

export function MyTeamTab({ eventId, userId }: Props) {
  const { setActiveTab } = useEventDashboard();
  const { data: team, isLoading } = useMyTeamForEvent(eventId, userId);
  const teamId = team?.id ?? '';
  const invite = useInviteToTeam(teamId);
  const leave  = useLeaveTeam(teamId, eventId, userId);
  const transfer = useTransferLeader(teamId, eventId, userId);
  const confirm  = useConfirmRegistration(teamId, eventId, userId);
  const removeMember = useRemoveMember(teamId, eventId, userId);

  // 3 trạng thái đội (xem TeamStatus trong team.types.ts): Forming (đang lập, sửa được)
  // -> PendingApproval (đã chốt, chờ EC duyệt, KHÓA sửa) -> Registered (EC đã duyệt).
  // "Locked" = không còn sửa được thành viên/gửi lời mời/chốt lại — đúng cho cả 2
  // trạng thái PendingApproval và Registered, chỉ Forming mới sửa được.
  const isPending  = team?.status === 'PendingApproval';
  const isApproved = team?.status === 'Registered';
  const isLocked   = isPending || isApproved;
  const [email, setEmail] = useState('');
  const notify = useNotify();
  const dialog = useDialog();

  // Chỉ trưởng nhóm mới xem được danh sách lời mời đã gửi (BE 403 với thành viên thường).
  const isLeader = team?.members.find((m) => m.userId === userId)?.isLeader ?? false;
  const { data: invitations = [] } = useTeamInvitations(teamId, isLeader);

  if (isLoading) return <div className="p-6 t-body-md text-mute">Đang tải…</div>;
  if (!team) return <div className="p-6 t-body-md text-mute">Bạn chưa có đội.</div>;

  // "Lời mời đã gửi" chỉ hiển thị lời mời VÀO ĐỘI. Yêu cầu chuyển quyền tái dùng chung
  // bảng TeamInvitations (BE không có field phân biệt loại) nhưng luôn gửi cho người ĐÃ
  // trong đội → lọc bỏ các yêu cầu chưa Accepted theo membership để không lẫn danh sách.
  // Lời mời Accepted vẫn giữ lại để hiển thị "Đã tham gia".
  // Chuẩn hoá chữ thường + đối chiếu cả userId lẫn email để tránh trượt do GUID khác hoa/thường.
  const norm = (s?: string | null) => (s ?? '').trim().toLowerCase();
  const memberIds = new Set(team.members.map((m) => norm(m.userId)).filter(Boolean));
  const memberEmails = new Set(team.members.map((m) => norm(m.email)).filter(Boolean));
  const teamInvitations = invitations.filter(
    (inv) =>
      inv.status === 'Accepted' ||
      (!memberIds.has(norm(inv.invitedUserId)) &&
        !memberEmails.has(norm(inv.invitedUserEmail))),
  );

  // Một email có thể được mời lại sau khi từ chối/hết hạn. Chỉ giữ lần mời mới
  // nhất để UI phản ánh trạng thái hiện tại: Declined → PendingAccept → Accepted.
  const latestInvitationByRecipient = new Map<string, (typeof invitations)[number]>();
  for (const invitation of teamInvitations) {
    const recipientKey =
      norm(invitation.invitedUserEmail) ||
      norm(invitation.invitedUserId) ||
      invitation.invitationId;
    const current = latestInvitationByRecipient.get(recipientKey);
    const invitationTime = new Date(invitation.createdTime).getTime();
    const currentTime = current ? new Date(current.createdTime).getTime() : Number.NEGATIVE_INFINITY;
    if (
      !current ||
      invitationTime > currentTime ||
      (invitationTime === currentTime && invitation.invitationId > current.invitationId)
    ) {
      latestInvitationByRecipient.set(recipientKey, invitation);
    }
  }
  const sentInvitations = [...latestInvitationByRecipient.values()].sort(
    (a, b) => new Date(b.createdTime).getTime() - new Date(a.createdTime).getTime(),
  );

  return (
    <section className="p-6 max-w-2xl mx-auto space-y-6">
      <div
        className="border border-hairline rounded-sm bg-canvas p-4 md:p-6 space-y-4"
      >
        <header>
          <div className="flex items-center gap-3 flex-wrap">
            <h2 className="t-heading-md">{team.teamName}</h2>
            <TeamStatusBadge status={team.status} />
          </div>
          {team.description ? <p className="t-body-sm text-mute mt-1">{team.description}</p> : null}
          {team.lastRejectReason ? (
            <div
              className="mt-3 rounded-sm border border-error/20 bg-error/10 p-3 text-error"
              role="alert"
            >
              <strong className="mb-1 block t-body-sm">Lý do từ chối duyệt đội:</strong>
              <p className="t-body-sm whitespace-pre-wrap">{team.lastRejectReason}</p>
            </div>
          ) : null}
        </header>

        <div>
          <h3 className="t-body-md font-bold mb-2">Thành viên</h3>
          <ul className="divide-y divide-hairline">
            {team.members.length === 0 ? (
              <li className="py-2 t-body-sm text-mute">Chưa có thành viên.</li>
            ) : (
              team.members.map((m) => (
                <li key={m.userId} className="py-2 flex items-center justify-between gap-3">
                  <span>{m.fullName} <span className="text-mute t-body-sm">({m.email})</span></span>
                  <span className="flex items-center gap-2">
                    {isLeader && !m.isLeader && (
                      <button
                        type="button"
                        className="btn btn-secondary btn-sm"
                        disabled={transfer.isPending}
                        onClick={async () => {
                          const ok = await dialog.confirm({
                            title: 'Chuyển quyền trưởng nhóm',
                            message: `Gửi yêu cầu chuyển quyền trưởng nhóm cho ${m.fullName}?\n${m.fullName} cần xác nhận ở chuông thông báo thì mới thành trưởng nhóm.`,
                            confirmText: 'Gửi yêu cầu',
                          });
                          if (!ok) return;
                          transfer.mutate(m.userId, {
                            onSuccess: () => notify.success(`Đã gửi yêu cầu chuyển quyền trưởng nhóm, chờ ${m.fullName} xác nhận.`),
                            onError: (err: unknown) => notify.error(getErrorMessage(err, 'Gửi yêu cầu chuyển quyền thất bại.')),
                          });
                        }}
                      >
                        Chuyển quyền
                      </button>
                    )}
                    {isLeader && !m.isLeader && !isLocked && (
                      <button
                        type="button"
                        className="btn btn-secondary btn-sm"
                        disabled={removeMember.isPending}
                        onClick={async () => {
                          // Lý do KHÔNG bắt buộc — được gửi kèm trong email thông báo cho người bị mời rời.
                          const reason = await dialog.prompt({
                            title: `Mời ${m.fullName} rời đội`,
                            message: 'Hệ thống sẽ gửi email thông báo cho thành viên này.',
                            label: 'Lý do (có thể để trống)',
                            placeholder: 'VD: Không tham gia hoạt động của đội…',
                            multiline: true,
                            danger: true,
                            confirmText: 'Mời rời đội',
                          });
                          if (reason === null) return; // bấm Hủy
                          removeMember.mutate({ memberUserId: m.userId, reason: reason || undefined }, {
                            onSuccess: () => notify.success(`${m.fullName} đã rời đội. Đã gửi email thông báo${reason ? ' kèm lý do' : ''}.`),
                            onError: (err: unknown) => notify.error(getErrorMessage(err, 'Không thể mời thành viên rời đội.')),
                          });
                        }}
                      >
                        Mời rời đội
                      </button>
                    )}
                    <EventRoleBadge roleName={m.isLeader ? 'TeamLeader' : 'TeamMember'} />
                  </span>
                </li>
              ))
            )}
          </ul>
        </div>
      </div>

      {/* Chỉ trưởng nhóm mới mời được (BE chặn 403 với thành viên thường). */}
      {isLeader && !isLocked && (
      <form
        onSubmit={(e) => {
          e.preventDefault();
          invite.mutate({ email }, {
            onSuccess: (res) => {
              setEmail('');
              const isNewTempUser = res?.isNewTemporaryUser ?? res?.data?.isNewTemporaryUser ?? false;
              if (isNewTempUser) {
                notify.success('Người này chưa có tài khoản. Hệ thống đã gửi email kích hoạt và đã lưu lời mời. Sau khi họ kích hoạt tài khoản, cập nhật hồ sơ và được duyệt, họ sẽ chấp nhận được lời mời này.');
              } else {
                notify.success('Đã gửi lời mời thành công!');
              }
            },
            onError: (err: unknown) => notify.error(getErrorMessage(err, 'Mời thất bại.')),
          });
        }}
        className="space-y-2 border border-hairline rounded-sm bg-canvas p-4 md:p-6"
      >
        <h3 className="t-body-md font-bold">Mời thành viên</h3>
        <div className="flex gap-2">
          <input
            type="email" required value={email} onChange={(e) => setEmail(e.target.value)}
            placeholder="email@example.com" className="input flex-1"
          />
          <button type="submit" disabled={invite.isPending} className="btn btn-primary">
            {invite.isPending ? 'Đang gửi…' : 'Mời'}
          </button>
        </div>
      </form>
      )}

      {isLeader && (
        <div className="border border-hairline rounded-sm bg-canvas p-4 md:p-6 space-y-3">
          <h3 className="t-body-md font-bold">Lời mời đã gửi</h3>
          {sentInvitations.length === 0 ? (
            <p className="t-body-sm text-mute">Chưa gửi lời mời nào.</p>
          ) : (
            <ul className="divide-y divide-hairline">
              {sentInvitations.map((inv) => {
                // Accepted + vẫn còn trong membership = đã tham gia. Chỉ đổi thành
                // "Đã rời đội" khi lời mời đã Accepted nhưng người đó không còn trong đội.
                const isCurrentMember =
                  memberIds.has(norm(inv.invitedUserId)) ||
                  memberEmails.has(norm(inv.invitedUserEmail));
                const hasLeft = inv.status === 'Accepted' && !isCurrentMember;
                const badge = hasLeft
                  ? { label: 'Đã rời đội', tone: 'inactive' as StatusTone }
                  : (INVITE_BADGE[inv.status] ?? { label: inv.status, tone: 'neutral' as StatusTone });
                // Ưu tiên nhãn tiếng Việt từ BE; fallback về map cục bộ nếu BE cũ chưa trả.
                const statusText = hasLeft ? 'Đã rời đội' : (inv.statusLabel || badge.label);
                return (
                  <li key={inv.invitationId} className="py-2 flex items-center justify-between gap-3">
                    <span className="t-body-sm">
                      {inv.invitedUserFullName || inv.invitedUserEmail}
                      {inv.invitedUserFullName && inv.invitedUserEmail && inv.invitedUserFullName !== inv.invitedUserEmail ? (
                        <span className="text-mute"> ({inv.invitedUserEmail})</span>
                      ) : null}
                    </span>
                    <span className="flex items-center gap-2">
                      {/* Mời lại: chỉ với người đã rời / lời mời hết hạn / đã từ chối, khi đội chưa chốt */}
                      {!isLocked && !!inv.invitedUserEmail
                        && (hasLeft || inv.status === 'Expired' || inv.status === 'Declined') && (
                        <button
                          type="button"
                          className="btn btn-secondary btn-sm"
                          disabled={invite.isPending}
                          onClick={() => {
                            invite.mutate({ email: inv.invitedUserEmail }, {
                              onSuccess: () => notify.success(`Đã gửi lại lời mời tới ${inv.invitedUserEmail}.`),
                              onError: (err: unknown) => notify.error(getErrorMessage(err, 'Mời lại thất bại.')),
                            });
                          }}
                        >
                          Mời lại
                        </button>
                      )}
                      <StatusBadge tone={badge.tone}>{statusText}</StatusBadge>
                    </span>
                  </li>
                );
              })}
            </ul>
          )}
        </div>
      )}

      <div className="flex flex-wrap gap-3">

        {/* Nút chốt danh sách — chỉ trưởng nhóm, chưa chốt */}
        {isLeader && !isLocked && (
          <button
            type="button"
            disabled={confirm.isPending}
            onClick={async () => {
              const ok = await dialog.confirm({
                title: 'Chốt danh sách đội?',
                message:
                  'Sau khi chốt:\n' +
                  '• Không thể thêm hoặc xóa thành viên\n' +
                  '• Thành viên không thể tự rời đội\n' +
                  '• Yêu cầu đủ 3–5 thành viên đã được duyệt',
                confirmText: 'Chốt danh sách',
                danger: true,
              });
              if (!ok) return;
              confirm.mutate(undefined, {
                onSuccess: () => notify.success('Đã chốt danh sách đội thành công!'),
                onError: (err: unknown) => notify.error(getErrorMessage(
                  err,
                  'Chốt danh sách thất bại. Kiểm tra đủ 3–5 thành viên đã duyệt.',
                )),
              });
            }}
            className="btn btn-primary"
          >
            {confirm.isPending ? 'Đang chốt…' : '✓ Chốt danh sách'}
          </button>
        )}

        {/* Nút rời đội — ẩn khi đã chốt */}
        {!isLocked && (
          <button
            type="button"
            onClick={async () => {
              const ok = await dialog.confirm({
                title: 'Rời đội',
                message: 'Bạn chắc chắn muốn rời đội này? Trưởng nhóm sẽ nhận được email thông báo.',
                confirmText: 'Rời đội',
                danger: true,
              });
              if (ok) leave.mutate(undefined, { onSuccess: () => setActiveTab('createTeam') });
            }}
            disabled={leave.isPending}
            className="btn btn-outline-danger"
          >
            {leave.isPending ? 'Đang rời…' : 'Rời đội'}
          </button>
        )}

      </div>
    </section>
  );
}
