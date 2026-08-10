import React, { useState, useRef, useEffect } from 'react';
import { Bell, Check, X, Loader2 } from 'lucide-react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { invitationsApi } from '@/features/invitations/api/invitationsApi';
import { teamsApi } from '@/features/teams/api/teams';
import { useNotify } from '@/components/NotificationProvider';
import { useRejectionNotifications } from '@/hooks/useRejectionNotifications';
import { useApprovalNotification } from '@/hooks/useApprovalNotification';
import Link from 'next/link';
import { getErrorMessage } from '@/lib/apiError';

export function NotificationBell() {
  const [isOpen, setIsOpen] = useState(false);
  const bellRef = useRef<HTMLDivElement>(null);
  const queryClient = useQueryClient();
  const notify = useNotify();

  const { data: inviteData, isLoading: isInviteLoading, refetch: refetchInvites } = useQuery({
    queryKey: ['my-invitations'],
    queryFn: invitationsApi.getMyInvitations,
    refetchInterval: 30000, // Polling mỗi 30s
    // Chuyển quyền lần 2 làm lời mời cũ Expired + tạo lời mời mới → luôn lấy bản mới
    // nhất khi cửa sổ focus lại, tránh bấm nhầm lời mời đã chết (BE trả 400).
    refetchOnWindowFocus: true,
    staleTime: 0,
  });

  const { activeRejections, rejectionCount, isLoading: isRejLoading, refetch: refetchRej } = useRejectionNotifications();
  const { showApproval, dismissApproval, isLoading: isApprovalLoading, refetch: refetchApproval } = useApprovalNotification();

  // Mở chuông = lấy lại danh sách mới nhất
  useEffect(() => {
    if (isOpen) {
      refetchInvites();
      refetchRej();
      refetchApproval();
    }
  }, [isOpen, refetchInvites, refetchRej, refetchApproval]);

  // Đóng dropdown khi click ra ngoài
  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (bellRef.current && !bellRef.current.contains(event.target as Node)) {
        setIsOpen(false);
      }
    }
    if (isOpen) {
      document.addEventListener('mousedown', handleClickOutside);
    }
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, [isOpen]);

  const respondTeamMut = useMutation({
    mutationFn: ({ id, accept }: { id: string; accept: boolean }) => teamsApi.respondInvitation(id, accept),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['my-invitations'] });
      notify.success('Đã xử lý lời mời thành công!');
    },
    onError: (err: unknown) => {
      notify.error(getErrorMessage(err, 'Đã xảy ra lỗi khi xử lý lời mời.'));
      // Lời mời có thể đã hết hạn/không tồn tại → refetch để item chết biến mất.
      queryClient.invalidateQueries({ queryKey: ['my-invitations'] });
    },
  });

  const respondRoleMut = useMutation({
    mutationFn: ({ id, accept }: { id: string; accept: boolean }) => invitationsApi.respondEventRole(id, accept),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['my-invitations'] });
      notify.success('Đã xử lý lời mời thành công!');
    },
    onError: (err: unknown) => {
      notify.error(getErrorMessage(err, 'Đã xảy ra lỗi khi xử lý lời mời.'));
      queryClient.invalidateQueries({ queryKey: ['my-invitations'] });
    },
  });

  const handleRespond = (invitationId: string, type: string, accept: boolean) => {
    // TEAM và TEAM_LEADER_TRANSFER dùng chung endpoint respond của Teams.
    if (type === 'EVENT_ROLE') {
      respondRoleMut.mutate({ id: invitationId, accept });
    } else {
      respondTeamMut.mutate({ id: invitationId, accept });
    }
  };

  const roleLabel = (role: string) => {
    switch (role) {
      case 'EventCoordinator': return 'Ban tổ chức';
      case 'Judge': return 'Giám khảo';
      case 'Mentor': return 'Cố vấn';
      case 'TeamLeader': return 'Trưởng nhóm';
      case 'TeamMember':
      case 'Member': return 'Thành viên';
      default: return role;
    }
  };

  const trackChip = (name?: string | null) =>
    name ? (
      <span className="inline-flex items-center px-2 py-0.5 text-[11px] font-semibold rounded-sm bg-primary/15 text-primary border border-primary/30">
        Hạng mục: {name}
      </span>
    ) : null;

  const totalPending = (inviteData?.totalPending || 0) + rejectionCount + (showApproval ? 1 : 0);
  const invitations = inviteData?.invitations ?? [];
  const isPending = respondTeamMut.isPending || respondRoleMut.isPending;
  const isLoading = isInviteLoading || isRejLoading || isApprovalLoading;

  return (
    <div className="relative" ref={bellRef}>
      <button
        onClick={() => setIsOpen(!isOpen)}
        className="p-2 text-on-dark hover:text-primary transition-colors relative"
        style={{ color: "var(--color-on-dark-mute)" }}
      >
        <Bell size={20} />
        {totalPending > 0 && (
          <span className="absolute top-1 right-1 w-4 h-4 bg-error text-white text-[10px] font-bold rounded-full flex items-center justify-center border border-surface-dark">
            {totalPending > 9 ? '9+' : totalPending}
          </span>
        )}
      </button>

      {isOpen && (
        <div className="absolute right-0 mt-2 w-80 bg-surface-dark border border-hairline-strong rounded-sm shadow-xl z-50 overflow-hidden">
          <div className="p-3 border-b border-hairline-strong bg-surface-elevated font-bold text-sm text-on-dark">
            Lời mời & thông báo{totalPending > 0 ? ` (${totalPending} chờ)` : ''}
          </div>
          <div className="max-h-96 overflow-y-auto">
            {isLoading ? (
              <div className="p-6 flex justify-center items-center text-on-dark-mute">
                <Loader2 className="animate-spin w-5 h-5" />
              </div>
            ) : invitations.length === 0 && activeRejections.length === 0 && !showApproval ? (
              <div className="p-6 text-center text-sm text-on-dark-mute">
                Bạn không có thông báo nào.
              </div>
            ) : (
              <div className="flex flex-col">
                {/* Thông báo được duyệt hồ sơ */}
                {showApproval && (
                  <div className="p-3 border-b border-hairline-strong flex items-start gap-2 hover:bg-surface-elevated transition-colors bg-primary/5">
                    <Check size={16} className="text-primary mt-0.5 shrink-0" />
                    <div className="flex-1">
                      <p className="text-sm text-on-dark leading-snug mb-1">
                        <span className="font-bold text-primary">Hồ sơ của bạn đã được duyệt!</span>
                      </p>
                      <p className="text-xs text-on-dark-mute">Bạn hiện đã có thể tạo hoặc tham gia nhóm.</p>
                    </div>
                    <button 
                      onClick={dismissApproval}
                      className="p-1 text-on-dark-mute hover:text-error transition-colors rounded-full hover:bg-error/10"
                      title="Đóng thông báo"
                    >
                      <X size={14} />
                    </button>
                  </div>
                )}

                {/* Thông báo bị từ chối hồ sơ */}
                {activeRejections.map(rej => (
                  <div key={rej.id} className="p-3 border-b border-hairline-strong flex items-start gap-2 hover:bg-surface-elevated transition-colors bg-error/5">
                    <X size={16} className="text-error mt-0.5 shrink-0" />
                    <div>
                      <p className="text-sm text-on-dark leading-snug mb-1">
                        <span className="font-bold text-error">Hồ sơ của bạn đã bị từ chối!</span>
                      </p>
                      <p className="text-xs text-on-dark-mute mb-2">Lý do: {rej.reason}</p>
                      <Link href="/profile" className="text-xs font-semibold text-primary hover:underline">
                        Cập nhật hồ sơ ngay →
                      </Link>
                    </div>
                  </div>
                ))}

                {invitations.map((inv) => {
                  const responded = inv.status === 'Accepted' || inv.status === 'Declined';

                  // Thông báo lịch sử: đã đồng ý / đã từ chối (không còn nút)
                  if (responded) {
                    const accepted = inv.status === 'Accepted';
                    return (
                      <div key={inv.invitationId} className="p-3 border-b border-hairline-strong flex items-start gap-2">
                        {accepted
                          ? <Check size={16} className="text-primary mt-0.5 shrink-0" />
                          : <X size={16} className="text-error mt-0.5 shrink-0" />}
                        <p className="text-sm text-on-dark-mute leading-snug">
                          {accepted ? (
                            <>Bạn đã <span className="font-bold text-primary">nhận</span> vai trò{' '}
                              <span className="font-bold text-on-dark">{roleLabel(inv.role)}</span>
                              {inv.trackName && <> — hạng mục <span className="font-bold text-on-dark">{inv.trackName}</span></>}{' '}
                              cho sự kiện <span className="font-bold text-on-dark">{inv.targetName}</span>.</>
                          ) : (
                            <>Bạn đã <span className="font-bold text-error">từ chối</span> lời mời{' '}
                              <span className="font-bold text-on-dark">{roleLabel(inv.role)}</span>
                              {inv.trackName && <> — hạng mục <span className="font-bold text-on-dark">{inv.trackName}</span></>}{' '}
                              của sự kiện <span className="font-bold text-on-dark">{inv.targetName}</span>.</>
                          )}
                        </p>
                      </div>
                    );
                  }

                  // Lời mời còn chờ: hiển thị nút Đồng ý / Từ chối
                  return (
                    <div key={inv.invitationId} className="p-3 border-b border-hairline-strong hover:bg-surface-elevated transition-colors">
                      <p className="text-sm text-on-dark leading-snug mb-1">
                        {inv.type === 'TEAM_LEADER_TRANSFER' ? (
                          <>
                            <span className="font-bold text-primary">{inv.inviterName || 'Hệ thống'}</span> muốn chuyển quyền
                            <span className="font-bold"> Trưởng nhóm</span> đội
                            <span className="font-bold"> {inv.targetName}</span> cho bạn.
                          </>
                        ) : (
                          <>
                            <span className="font-bold text-primary">{inv.inviterName || 'Hệ thống'}</span> mời bạn tham gia
                            <span className="font-bold"> {inv.targetName}</span> với vai trò
                            <span className="font-bold text-primary"> {roleLabel(inv.role)}</span>.
                          </>
                        )}
                      </p>
                      {inv.trackName && <div className="mb-2">{trackChip(inv.trackName)}</div>}
                      <div className="flex justify-end gap-2 mt-2">
                        <button
                          onClick={() => handleRespond(inv.invitationId, inv.type, false)}
                          disabled={isPending}
                          className="btn-hover px-2 py-1 text-xs flex items-center gap-1 border border-error/50 text-error rounded-sm hover:bg-error/10 disabled:opacity-50"
                        >
                          Từ chối
                        </button>
                        <button
                          onClick={() => handleRespond(inv.invitationId, inv.type, true)}
                          disabled={isPending}
                          className="btn-hover px-2 py-1 text-xs flex items-center gap-1 bg-primary text-white rounded-sm hover:bg-primary-dark disabled:opacity-50"
                        >
                          Đồng ý
                        </button>
                      </div>
                    </div>
                  );
                })}
              </div>
            )}
          </div>
        </div>
      )}
    </div>
  );
}
