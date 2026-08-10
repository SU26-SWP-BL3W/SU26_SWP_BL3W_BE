'use client';

import React, { useState } from 'react';
import { X } from 'lucide-react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { usersApi, schoolsApi, type UserSummary } from '@/services/api';
import { useNotify } from '@/components/NotificationProvider';
import { useDialog } from '@/components/ConfirmDialogProvider';
import { getErrorMessage } from '@/lib/apiError';
import { Card } from '../../EventDashboard/Card';
import { CardSkeleton } from '../../EventDashboard/SkeletonLoaders';

const PAGE_SIZE = 20;

interface AccountApprovalTabProps {
  eventId: string;
}

export function AccountApprovalTab({ eventId }: AccountApprovalTabProps) {
  const queryClient = useQueryClient();
  const notify = useNotify();
  const dialog = useDialog();
  const [page, setPage] = useState(1);
  const [actionError, setActionError] = useState<string | null>(null);
  const [preview, setPreview] = useState<{ url: string; name: string } | null>(null);

  // Pending accounts that belong to THIS event — server-side filter avoids
  // approving a student from another event (which returns 403 Forbidden).
  const pendingQuery = useQuery({
    queryKey: ['users', 'list', 'pending', eventId, page],
    queryFn: () =>
      usersApi.list({ isApproved: false, eventId, pageNumber: page, pageSize: PAGE_SIZE }),
    enabled: !!eventId,
    staleTime: 2 * 60_000,
  });

  const schoolsQuery = useQuery({
    queryKey: ['schools'],
    queryFn: () => schoolsApi.list(),
    staleTime: 5 * 60_000,
  });
  const schoolName = (u: UserSummary) =>
    u.isFpt
      ? 'FPT University'
      : u.schoolId
        ? schoolsQuery.data?.data?.find((s) => s.id === u.schoolId)?.schoolName ?? '—'
        : '—';

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ['users', 'list'] });
    queryClient.invalidateQueries({ queryKey: ['users', 'profile'] });
    queryClient.invalidateQueries({ queryKey: ['eventRoles', eventId] });
  };
  const onErr = (e: unknown) => {
    const msg = getErrorMessage(e);
    setActionError(msg);
    notify.error(msg);
  };

  // Approve = activate the account globally (POST /Users/{id}/approve).
  const approveMutation = useMutation({
    mutationFn: (u: UserSummary) => usersApi.approve(u.id),
    onSuccess: () => {
      setActionError(null);
      notify.success('Đã duyệt tài khoản thành công!');
      invalidate();
    },
    onError: onErr,
  });

  // Reject = reject the account via dedicated endpoint (POST /Users/{id}/reject).
  const rejectMutation = useMutation({
    mutationFn: ({ u, reason }: { u: UserSummary; reason: string }) =>
      usersApi.reject(u.id, reason),
    onSuccess: () => {
      setActionError(null);
      notify.success('Đã từ chối tài khoản thành công!');
      invalidate();
    },
    onError: onErr,
  });

  const pendingUserId = approveMutation.isPending
    ? approveMutation.variables?.id
    : rejectMutation.isPending
      ? rejectMutation.variables?.u.id
      : undefined;

  const users = pendingQuery.data?.data ?? [];
  const total = pendingQuery.data?.totalItems ?? users.length;
  const totalPages = Math.max(1, pendingQuery.data?.totalPages ?? 1);
  const currentPage = Math.min(page, totalPages);

  if (pendingQuery.isError) {
    return (
      <div className="bg-error/10 border border-error rounded-sm p-6 text-center">
        <p className="t-body-md text-error font-bold">Không tải được danh sách đăng ký</p>
      </div>
    );
  }

  if (pendingQuery.isLoading) return <CardSkeleton />;

  return (
    <Card title="Xét duyệt tài khoản">
      <div className="flex flex-col gap-4">
        <p className="t-body-sm text-mute m-0">{total} tài khoản chờ duyệt</p>
        {actionError && <p className="t-caption-sm text-error m-0">{actionError}</p>}

        {users.length === 0 ? (
          <p className="t-body-sm text-mute text-center py-8">Không có tài khoản chờ duyệt.</p>
        ) : (
          <>
            <div className="overflow-x-auto">
              <table className="w-full border-collapse">
                <thead>
                  <tr className="border-b border-hairline-strong text-left">
                    <th className="t-caption-md text-mute font-bold uppercase py-3 px-2">Ảnh thẻ</th>
                    <th className="t-caption-md text-mute font-bold uppercase py-3 px-2">Họ và tên</th>
                    <th className="t-caption-md text-mute font-bold uppercase py-3 px-2">Email</th>
                    <th className="t-caption-md text-mute font-bold uppercase py-3 px-2">MSSV</th>
                    <th className="t-caption-md text-mute font-bold uppercase py-3 px-2">Trường</th>
                    <th className="t-caption-md text-mute font-bold uppercase py-3 px-2 text-right">Thao tác</th>
                  </tr>
                </thead>
                <tbody>
                  {users.map((u) => {
                    const busy = pendingUserId === u.id;
                    const photo = u.photoStudentCardUrl;
                    return (
                      <tr key={u.id} className="border-b border-hairline last:border-b-0">
                        <td className="py-3 px-2">
                          {photo ? (
                            <button
                              type="button"
                              onClick={() => setPreview({ url: photo, name: u.fullName ?? '' })}
                              className="block focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary"
                              aria-label={`Xem ảnh thẻ của ${u.fullName || u.email || 'sinh viên'}`}
                            >
                              {/* eslint-disable-next-line @next/next/no-img-element */}
                              <img
                                src={photo}
                                alt="Ảnh thẻ sinh viên"
                                className="w-14 h-10 object-cover rounded-sm border border-hairline"
                              />
                            </button>
                          ) : (
                            <span className="text-mute t-caption-sm">—</span>
                          )}
                        </td>
                        <td className="t-body-sm font-bold text-ink py-3 px-2 whitespace-nowrap">{u.fullName || '—'}</td>
                        <td className="t-body-sm text-body py-3 px-2">{u.email || '—'}</td>
                        <td className="t-body-sm text-body py-3 px-2">{u.studentCode || '—'}</td>
                        <td className="t-body-sm text-body py-3 px-2">{schoolName(u)}</td>
                        <td className="py-3 px-2">
                          <div className="flex justify-end gap-2">
                            <button
                              type="button"
                              disabled={busy}
                              onClick={() => {
                                setActionError(null);
                                approveMutation.mutate(u);
                              }}
                              className="t-caption-sm font-bold text-primary disabled:opacity-50"
                              style={{ background: 'none', border: '1px solid var(--color-primary)', borderRadius: 'var(--radius-sm)', padding: '4px 10px', cursor: busy ? 'not-allowed' : 'pointer', whiteSpace: 'nowrap' }}
                            >
                              {busy && approveMutation.isPending ? 'Đang lưu…' : 'Duyệt'}
                            </button>
                            <button
                              type="button"
                              disabled={busy}
                              onClick={async () => {
                                const reason = await dialog.prompt({
                                  title: 'Từ chối hồ sơ',
                                  message: `Từ chối đăng ký của ${u.fullName || u.email || 'tài khoản này'}? Lý do sẽ được gửi cho người đăng ký.`,
                                  label: 'Lý do từ chối',
                                  placeholder: 'VD: Ảnh thẻ sinh viên không hợp lệ…',
                                  required: true,
                                  multiline: true,
                                  danger: true,
                                  confirmText: 'Từ chối',
                                });
                                if (reason === null) return; // hủy
                                setActionError(null);
                                rejectMutation.mutate({ u, reason });
                              }}
                              className="t-caption-sm font-bold text-error disabled:opacity-50"
                              style={{ background: 'none', border: '1px solid var(--color-error)', borderRadius: 'var(--radius-sm)', padding: '4px 10px', cursor: busy ? 'not-allowed' : 'pointer', whiteSpace: 'nowrap' }}
                            >
                              {busy && rejectMutation.isPending ? 'Đang xử lý…' : 'Từ chối'}
                            </button>
                          </div>
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>

            {totalPages > 1 && (
              <div className="flex items-center justify-between gap-3">
                <span className="t-caption-sm text-mute">
                  Trang {currentPage}/{totalPages}
                </span>
                <div className="flex gap-2">
                  <button
                    type="button"
                    disabled={currentPage <= 1}
                    onClick={() => setPage((p) => Math.max(1, p - 1))}
                    className="flex items-center gap-1 t-caption-sm font-bold text-ink disabled:opacity-40"
                    style={{ border: '1px solid var(--color-hairline-strong)', borderRadius: 'var(--radius-sm)', padding: '4px 10px', cursor: currentPage <= 1 ? 'not-allowed' : 'pointer' }}
                  >
                    Trước
                  </button>
                  <button
                    type="button"
                    disabled={currentPage >= totalPages}
                    onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
                    className="flex items-center gap-1 t-caption-sm font-bold text-ink disabled:opacity-40"
                    style={{ border: '1px solid var(--color-hairline-strong)', borderRadius: 'var(--radius-sm)', padding: '4px 10px', cursor: currentPage >= totalPages ? 'not-allowed' : 'pointer' }}
                  >
                    Sau
                  </button>
                </div>
              </div>
            )}
          </>
        )}
      </div>

      {/* Ảnh thẻ phóng to */}
      {preview && (
        <div
          role="dialog"
          aria-modal="true"
          aria-label="Ảnh thẻ sinh viên"
          onClick={() => setPreview(null)}
          className="fixed inset-0 z-[100] flex items-center justify-center bg-black/70 p-4"
        >
          <div className="relative max-w-[90vw] max-h-[90vh]" onClick={(e) => e.stopPropagation()}>
            <button
              type="button"
              onClick={() => setPreview(null)}
              aria-label="Đóng"
              className="absolute -top-3 -right-3 w-8 h-8 rounded-full bg-canvas border border-hairline flex items-center justify-center text-ink"
            >
              <X size={16} aria-hidden="true" />
            </button>
            {/* eslint-disable-next-line @next/next/no-img-element */}
            <img
              src={preview.url}
              alt={`Ảnh thẻ của ${preview.name}`}
              className="h-auto max-h-[80vh] object-contain rounded-sm"
              style={{ width: "min(480px, 88vw)" }}
            />
          </div>
        </div>
      )}
    </Card>
  );
}
