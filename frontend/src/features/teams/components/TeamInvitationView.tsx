'use client';

import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useRouter } from 'next/navigation';
import { teamsApi } from '@/features/teams/api/teams';
import { useState } from 'react';
import { Clock, User, Shield } from 'lucide-react';
import { getErrorMessage } from '@/lib/apiError';

interface Props {
  teamId: string;
}

export function TeamInvitationView({ teamId }: Props) {
  const router = useRouter();
  const queryClient = useQueryClient();
  const [errorMsg, setErrorMsg] = useState<string | null>(null);
  const [confirmingAction, setConfirmingAction] = useState<'accept' | 'reject' | null>(null);

  // Fetch team details
  const { data: team, isLoading: isTeamLoading, isError: isTeamError } = useQuery({
    queryKey: ['teams', teamId],
    queryFn: () => teamsApi.getById(teamId),
  });

  // Fetch invitation details
  const { data: invitation, isLoading: isInvLoading } = useQuery({
    queryKey: ['teams', teamId, 'my-invitation'],
    queryFn: () => teamsApi.getMyInvitation(teamId),
  });

  if (typeof window !== 'undefined') {
    console.log('[TeamInvitationView] team:', team);
    console.log('[TeamInvitationView] isTeamLoading:', isTeamLoading);
    console.log('[TeamInvitationView] isTeamError:', isTeamError);
    console.log('[TeamInvitationView] invitation:', invitation);
    console.log('[TeamInvitationView] isInvLoading:', isInvLoading);
  }

  // Respond mutation
  const respondMutation = useMutation({
    mutationFn: async ({ accept }: { accept: boolean }) => {
      if (!invitation?.invitationId) throw new Error('Không tìm thấy ID lời mời');
      await teamsApi.respondInvitation(invitation.invitationId, accept);
    },
    onSuccess: () => {
      setConfirmingAction(null);
      // Invalidate to refresh dashboard/team state
      queryClient.invalidateQueries({ queryKey: ['teams'] });
      queryClient.invalidateQueries({ queryKey: ['events'] });
      // Go to home after responding
      router.push('/');
    },
    onError: (e: unknown) => {
      setErrorMsg(getErrorMessage(e, 'Có lỗi xảy ra khi phản hồi lời mời.'));
      setConfirmingAction(null);
    }
  });

  if (isTeamLoading || isInvLoading) {
    return <div className="text-center p-8 text-mute">Đang tải thông tin đội...</div>;
  }

  if (isTeamError || !team) {
    return (
      <div className="card w-full max-w-[28rem] mx-auto mt-8 p-6 text-center border border-error bg-error-soft">
        <h2 className="t-heading-sm text-error">Lỗi truy cập</h2>
        <p className="t-body-sm text-mute mt-2">
          Không tìm thấy thông tin đội thi này hoặc bạn không có quyền xem.
        </p>
        <button className="btn btn-secondary mt-6 mx-auto" onClick={() => router.push('/')}>
          Về trang chủ
        </button>
      </div>
    );
  }

  return (
    <div className="card w-full max-w-[32rem] mx-auto mt-8 !p-0 overflow-hidden shadow-sm">
      <div className="bg-surface-soft p-6 border-b border-hairline text-center">
        <h2 className="t-heading-lg mb-2">{team.teamName}</h2>
        <p className="t-body-md text-mute">{team.description || 'Chưa có mô tả'}</p>
      </div>

      <div className="p-6">
        <h3 className="t-heading-sm mb-4">Danh sách thành viên hiện tại</h3>
        {team.members.length === 0 ? (
          <p className="t-body-sm text-mute italic text-center py-4">Đội chưa có thành viên nào.</p>
        ) : (
          <ul className="flex flex-col gap-3">
            {team.members.map((m) => (
              <li key={m.userId} className="flex items-center gap-3 p-3 bg-surface-soft rounded-sm">
                <div className="w-8 h-8 rounded-full bg-primary/10 flex items-center justify-center text-primary font-bold uppercase shrink-0">
                  {m.fullName?.charAt(0) || '?'}
                </div>
                <div className="flex-1 min-w-0">
                  <p className="t-body-sm font-bold truncate">{m.fullName}</p>
                  <p className="t-caption-sm text-mute truncate">{m.email}</p>
                </div>
                {m.isLeader && (
                  <span className="badge-tag bg-primary/10 text-primary border-primary/20 text-xs shrink-0">
                    Leader
                  </span>
                )}
              </li>
            ))}
          </ul>
        )}
      </div>

      {invitation ? (
        <div className="p-6 bg-primary/5 border-t border-primary/20 flex flex-col gap-4">
          <div className="text-center">
            <h4 className="t-heading-sm text-primary mb-1">Bạn có 1 lời mời!</h4>
            <p className="t-body-sm text-mute">
              Bạn được mời tham gia đội này. Xác nhận để cùng tham gia tranh tài.
            </p>
          </div>
          
          <div className="bg-white p-4 rounded-sm border border-primary/10 flex flex-col gap-2">
            {invitation.inviterName && (
              <div className="flex items-center gap-2 text-sm text-body-text">
                <User className="w-4 h-4 text-mute" />
                <span className="text-mute w-20">Người mời:</span>
                <span className="font-bold">{invitation.inviterName}</span>
              </div>
            )}
            {invitation.roleName && (
              <div className="flex items-center gap-2 text-sm text-body-text">
                <Shield className="w-4 h-4 text-mute" />
                <span className="text-mute w-20">Vai trò:</span>
                <span className="font-bold">{invitation.roleName}</span>
              </div>
            )}
            {invitation.expiresAt && (
              <div className="flex items-center gap-2 text-sm text-body-text">
                <Clock className="w-4 h-4 text-mute" />
                <span className="text-mute w-20">Hết hạn:</span>
                <span className="font-bold">{new Date(invitation.expiresAt).toLocaleDateString('vi-VN')}</span>
              </div>
            )}
            {!invitation.inviterName && !invitation.roleName && !invitation.expiresAt && (
               <p className="text-sm text-mute italic text-center m-0">Không có thông tin chi tiết lời mời.</p>
            )}
          </div>
          
          {errorMsg && (
            <div className="p-3 bg-error/10 border border-error/20 text-error text-sm rounded-sm text-center">
              {errorMsg}
            </div>
          )}

          {confirmingAction ? (
            <div className="p-4 bg-surface-soft border border-hairline rounded-sm flex flex-col items-center gap-3 mt-2 text-center">
              <p className="t-body-sm font-bold">
                Bạn chắc chắn muốn {confirmingAction === 'accept' ? 'tham gia' : 'từ chối'} lời mời này?
              </p>
              <div className="flex gap-3 w-full">
                <button
                  className="btn btn-outline flex-1"
                  onClick={() => setConfirmingAction(null)}
                  disabled={respondMutation.isPending}
                >
                  Hủy
                </button>
                <button
                  className={`btn flex-1 flex items-center justify-center gap-2 ${confirmingAction === 'accept' ? 'btn-primary' : 'btn-danger'}`}
                  onClick={() => respondMutation.mutate({ accept: confirmingAction === 'accept' })}
                  disabled={respondMutation.isPending}
                >
                  {respondMutation.isPending ? (
                    <>
                      Đang xử lý...
                    </>
                  ) : (
                    'Xác nhận'
                  )}
                </button>
              </div>
            </div>
          ) : (
            <div className="flex gap-3 mt-2">
              <button
                type="button"
                className="btn btn-outline-danger flex-1 flex items-center justify-center gap-2"
                onClick={() => setConfirmingAction('reject')}
              >
                Từ chối
              </button>
              <button
                type="button"
                className="btn btn-primary flex-1 flex items-center justify-center gap-2"
                onClick={() => setConfirmingAction('accept')}
              >
                Tham gia
              </button>
            </div>
          )}
        </div>
      ) : (
        <div className="p-6 bg-surface-soft border-t border-hairline text-center">
          <p className="t-body-sm text-mute">
            Bạn không có lời mời tham gia chờ xác nhận cho đội này.
          </p>
          <button className="btn btn-secondary mt-4 mx-auto" onClick={() => router.push('/')}>
            Quay lại
          </button>
        </div>
      )}
    </div>
  );
}
