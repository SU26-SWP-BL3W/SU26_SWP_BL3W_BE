'use client';

import React, { useState } from 'react';
import { X, Search } from 'lucide-react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { usersApi, schoolsApi, type UserSummary } from '@/services/api';
import { Navbar } from '@/components/Navbar';
import Notif from '@/components/Notif';
import { useDialog } from '@/components/ConfirmDialogProvider';
import { getErrorMessage } from '@/lib/apiError';

const PAGE_SIZE = 20;

const errMsg = getErrorMessage;

export default function ApprovalRoute() {
  const queryClient = useQueryClient();
  const dialog = useDialog();
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState('');
  const [searchInput, setSearchInput] = useState('');
  const [actionError, setActionError] = useState<string | null>(null);
  const [preview, setPreview] = useState<{ url: string; name: string } | null>(null);
  const [notif, setNotif] = useState<{ m: string; t?: 's' | 'e' } | null>(null);

  const showNotif = (m: string, t: 's' | 'e' = 's') => {
    setNotif({ m, t });
    setTimeout(() => setNotif(null), 3000);
  };

  const { data: pagedResult, isLoading, error } = useQuery({
    queryKey: ['users', 'pending', page, search],
    queryFn: () => usersApi.list({ isApproved: false, pageNumber: page, pageSize: PAGE_SIZE, search }),
    staleTime: 30_000, // 30s
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
    queryClient.invalidateQueries({ queryKey: ['users', 'pending'] });
  };

  const approveMutation = useMutation({
    mutationFn: (userId: string) => usersApi.approve(userId),
    onSuccess: () => {
      setActionError(null);
      showNotif('Đã duyệt tài khoản thành công!');
      invalidate();
    },
    onError: (e) => setActionError(errMsg(e)),
  });

  const rejectMutation = useMutation({
    mutationFn: ({ userId, reason }: { userId: string; reason: string }) => usersApi.reject(userId, reason),
    onSuccess: () => {
      setActionError(null);
      showNotif('Đã từ chối tài khoản thành công!', 's');
      invalidate();
    },
    onError: (e) => setActionError(errMsg(e)),
  });

  const pendingUserId = approveMutation.isPending
    ? approveMutation.variables
    : rejectMutation.isPending
      ? rejectMutation.variables?.userId
      : undefined;

  const users = pagedResult?.data ?? [];
  const totalItems = pagedResult?.totalItems ?? 0;
  const totalPages = pagedResult?.totalPages ?? 1;

  const handleSearchSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setSearch(searchInput);
    setPage(1);
  };

  return (
    <>
      <Navbar />
      <Notif n={notif} />
      <main className="min-h-screen bg-canvas pt-[100px] pb-12 px-4 sm:px-6 lg:px-8">
        <div className="max-w-[70rem] mx-auto">
          <div className="flex flex-col md:flex-row justify-between items-start md:items-center gap-4 mb-8">
            <div>
              <h1 className="t-heading-lg m-0">Phê duyệt tài khoản</h1>
              <p className="t-body-md text-mute mt-1">Quản lý và xét duyệt các tài khoản chờ phê duyệt trên toàn hệ thống.</p>
            </div>
            <div className="flex gap-3">
              <div className="bg-primary/10 border border-primary/20 px-4 py-2 rounded-sm text-center min-w-[5rem]">
                <div className="t-heading-md text-primary">{totalItems}</div>
                <div className="t-caption-sm text-mute">Chờ duyệt</div>
              </div>
            </div>
          </div>

          <div className="card p-4 sm:p-6 mb-8">
            <div className="flex flex-col sm:flex-row justify-between gap-4 mb-6">
              <form onSubmit={handleSearchSubmit} className="relative flex-1 max-w-sm">
                <Search size={18} className="absolute left-3 top-1/2 -translate-y-1/2 text-mute" />
                <input
                  type="text"
                  placeholder="Tìm kiếm theo email, tên..."
                  value={searchInput}
                  onChange={(e) => setSearchInput(e.target.value)}
                  className="w-full pl-10 pr-4 py-2 border border-hairline-strong rounded-sm text-sm focus:outline-none focus:border-primary"
                />
              </form>
            </div>

            {actionError && (
              <div className="bg-error/10 border border-error rounded-sm p-4 mb-6 text-error text-sm font-bold">
                {actionError}
              </div>
            )}

            {error ? (
              <div className="bg-error/10 border border-error rounded-sm p-8 text-center text-error font-bold">
                Không tải được danh sách tài khoản
              </div>
            ) : isLoading ? (
              <div className="py-20 flex flex-col items-center justify-center text-mute gap-4">
                <div className="w-8 h-8 border-4 border-hairline-strong border-t-primary rounded-full animate-spin"></div>
                <div className="text-sm font-bold">Đang tải danh sách...</div>
              </div>
            ) : totalItems === 0 ? (
              <div className="py-20 text-center text-mute">
                <div className="text-sm font-bold">Không có tài khoản nào chờ duyệt.</div>
              </div>
            ) : (
              <>
                <div className="overflow-x-auto">
                  <table className="w-full border-collapse">
                    <thead>
                      <tr className="border-b border-hairline-strong text-left">
                        <th className="t-caption-md text-mute font-bold uppercase py-4 px-3">Ảnh thẻ</th>
                        <th className="t-caption-md text-mute font-bold uppercase py-4 px-3">Họ và tên</th>
                        <th className="t-caption-md text-mute font-bold uppercase py-4 px-3">Email</th>
                        <th className="t-caption-md text-mute font-bold uppercase py-4 px-3">MSSV</th>
                        <th className="t-caption-md text-mute font-bold uppercase py-4 px-3">Trường</th>
                        <th className="t-caption-md text-mute font-bold uppercase py-4 px-3 text-right">Thao tác</th>
                      </tr>
                    </thead>
                    <tbody>
                      {users.map((r) => {
                        const busy = pendingUserId === r.id;
                        const photo = r.photoStudentCardUrl;
                        return (
                          <tr key={r.id} className="border-b border-hairline last:border-b-0 hover:bg-black/[0.02] transition-colors">
                            <td className="py-3 px-3">
                              {photo ? (
                                <button
                                  type="button"
                                  onClick={() => setPreview({ url: photo, name: r.fullName ?? '' })}
                                  className="block focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary"
                                  aria-label={`Xem ảnh thẻ của ${r.fullName || r.email}`}
                                >
                                  {/* eslint-disable-next-line @next/next/no-img-element */}
                                  <img
                                    src={photo}
                                    alt="Ảnh thẻ"
                                    className="w-14 h-10 object-cover rounded-sm border border-hairline"
                                  />
                                </button>
                              ) : (
                                <span className="text-mute t-caption-sm">—</span>
                              )}
                            </td>
                            <td className="t-body-sm font-bold text-ink py-3 px-3 whitespace-nowrap">{r.fullName || '—'}</td>
                            <td className="t-body-sm text-body py-3 px-3">{r.email || '—'}</td>
                            <td className="t-body-sm text-body py-3 px-3">{r.studentCode || '—'}</td>
                            <td className="t-body-sm text-body py-3 px-3">{schoolName(r)}</td>
                            <td className="py-3 px-3">
                              <div className="flex justify-end gap-2">
                                <button
                                  type="button"
                                  disabled={busy}
                                  onClick={() => {
                                    setActionError(null);
                                    approveMutation.mutate(r.id);
                                  }}
                                  className="t-caption-sm font-bold text-primary disabled:opacity-50 btn-hover"
                                  style={{ background: 'var(--color-primary-soft)', border: '1px solid var(--color-primary-soft)', borderRadius: 'var(--radius-sm)', padding: '6px 14px', cursor: busy ? 'not-allowed' : 'pointer', whiteSpace: 'nowrap' }}
                                >
                                  {busy && approveMutation.isPending ? 'Đang lưu…' : 'Duyệt'}
                                </button>
                                <button
                                  type="button"
                                  disabled={busy}
                                  onClick={async () => {
                                    const reason = await dialog.prompt({
                                      title: 'Từ chối hồ sơ',
                                      message: `Từ chối đăng ký của ${r.fullName || r.email}? Lý do sẽ được gửi cho người đăng ký.`,
                                      label: 'Lý do từ chối',
                                      placeholder: 'VD: Ảnh thẻ sinh viên không hợp lệ…',
                                      required: true,
                                      multiline: true,
                                      danger: true,
                                      confirmText: 'Từ chối',
                                    });
                                    if (reason === null) return; // hủy
                                    setActionError(null);
                                    rejectMutation.mutate({ userId: r.id, reason });
                                  }}
                                  className="t-caption-sm font-bold text-error disabled:opacity-50 btn-hover"
                                  style={{ background: 'var(--color-error-soft)', border: '1px solid var(--color-error-soft)', borderRadius: 'var(--radius-sm)', padding: '6px 14px', cursor: busy ? 'not-allowed' : 'pointer', whiteSpace: 'nowrap' }}
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
                  <div className="flex items-center justify-between gap-3 mt-6">
                    <span className="t-caption-sm text-mute font-bold">
                      Trang {page} / {totalPages}
                    </span>
                    <div className="flex gap-2">
                      <button
                        type="button"
                        disabled={page <= 1}
                        onClick={() => setPage((p) => Math.max(1, p - 1))}
                        className="flex items-center gap-1 t-caption-sm font-bold text-ink disabled:opacity-40 hover:bg-black/5 px-3 py-1.5 rounded-sm transition-colors"
                        style={{ border: '1px solid var(--color-hairline-strong)' }}
                      >
                        Trước
                      </button>
                      <button
                        type="button"
                        disabled={page >= totalPages}
                        onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
                        className="flex items-center gap-1 t-caption-sm font-bold text-ink disabled:opacity-40 hover:bg-black/5 px-3 py-1.5 rounded-sm transition-colors"
                        style={{ border: '1px solid var(--color-hairline-strong)' }}
                      >
                        Sau
                      </button>
                    </div>
                  </div>
                )}
              </>
            )}
          </div>
        </div>
      </main>

      {/* Ảnh thẻ phóng to */}
      {preview && (
        <div
          role="dialog"
          aria-modal="true"
          aria-label="Ảnh thẻ sinh viên"
          onClick={() => setPreview(null)}
          className="fixed inset-0 z-[100] flex items-center justify-center bg-black/70 p-4 animate-fadeUp"
        >
          <div className="relative max-w-[90vw] max-h-[90vh]" onClick={(e) => e.stopPropagation()}>
            <button
              type="button"
              onClick={() => setPreview(null)}
              aria-label="Đóng"
              className="absolute -top-3 -right-3 w-8 h-8 rounded-full bg-canvas border border-hairline flex items-center justify-center text-ink hover:scale-105 transition-transform"
            >
              <X size={16} aria-hidden="true" />
            </button>
            {/* eslint-disable-next-line @next/next/no-img-element */}
            <img
              src={preview.url}
              alt={`Ảnh thẻ của ${preview.name}`}
              className="max-w-[90vw] max-h-[90vh] object-contain rounded-sm shadow-xl"
            />
          </div>
        </div>
      )}
    </>
  );
}
