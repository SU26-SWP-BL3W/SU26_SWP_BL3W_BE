'use client';

import React, { useState } from 'react';
import { X, Search } from 'lucide-react';
import type { AxiosError } from 'axios';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { schoolsApi } from '@/services/api';
import { Navbar } from '@/components/Navbar';
import Notif from '@/components/Notif';

const errMsg = (e: unknown): string => {
  const res = (e as AxiosError<{ message?: string; errors?: Record<string, string[]> }>)?.response;
  const data = res?.data;
  const fieldMsgs = data?.errors ? Object.values(data.errors).flat() : [];
  if (fieldMsgs.length) return fieldMsgs.join(' ');
  if (data?.message && !/Exception|was thrown/i.test(data.message)) return data.message;
  return `Thao tác thất bại (lỗi ${res?.status ?? '?'}). Vui lòng thử lại.`;
};

interface SchoolForm { schoolName: string; address: string; }
const EMPTY_FORM: SchoolForm = { schoolName: '', address: '' };

export default function SchoolsPage() {
  const queryClient = useQueryClient();
  const [search, setSearch]           = useState('');
  const [searchInput, setSearchInput] = useState('');
  const [notif, setNotif]             = useState<{ m: string; t?: 's' | 'e' } | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);

  const [modal, setModal] = useState<
    | { mode: 'create' }
    | { mode: 'edit'; id: string; initial: SchoolForm }
    | null
  >(null);
  const [form, setForm] = useState<SchoolForm>(EMPTY_FORM);

  const [deleteTarget, setDeleteTarget] = useState<{ id: string; name: string } | null>(null);

  const showNotif = (m: string, t: 's' | 'e' = 's') => {
    setNotif({ m, t });
    setTimeout(() => setNotif(null), 3000);
  };

  const invalidate = () =>
    queryClient.invalidateQueries({ queryKey: ['schools'] });

  const { data: pagedResult, isLoading, error } = useQuery({
    queryKey: ['schools'],
    queryFn: () => schoolsApi.list(100),
    staleTime: 5 * 60_000,
  });

  const schools = (pagedResult?.data ?? []).filter((s) =>
    s.schoolName.toLowerCase().includes(search.toLowerCase()),
  );

  const createMutation = useMutation({
    mutationFn: (payload: SchoolForm) => schoolsApi.create(payload),
    onSuccess: () => {
      showNotif('Đã thêm trường học thành công!');
      setModal(null);
      invalidate();
    },
    onError: (e) => setActionError(errMsg(e)),
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: SchoolForm }) =>
      schoolsApi.update(id, payload),
    onSuccess: () => {
      showNotif('Đã cập nhật trường học!');
      setModal(null);
      invalidate();
    },
    onError: (e) => setActionError(errMsg(e)),
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => schoolsApi.delete(id),
    onSuccess: () => {
      showNotif('Đã xóa trường học!');
      setDeleteTarget(null);
      invalidate();
    },
    onError: (e) => setActionError(errMsg(e)),
  });

  const openCreate = () => {
    setForm(EMPTY_FORM);
    setActionError(null);
    setModal({ mode: 'create' });
  };

  const openEdit = (s: { id: string; schoolName: string; address?: string | null }) => {
    const initial = { schoolName: s.schoolName, address: s.address ?? '' };
    setForm(initial);
    setActionError(null);
    setModal({ mode: 'edit', id: s.id, initial });
  };

  const handleSubmit = () => {
    if (!form.schoolName.trim()) {
      setActionError('Tên trường không được để trống.');
      return;
    }
    setActionError(null);
    if (modal?.mode === 'create') {
      createMutation.mutate(form);
    } else if (modal?.mode === 'edit') {
      updateMutation.mutate({ id: modal.id, payload: form });
    }
  };

  const isSaving = createMutation.isPending || updateMutation.isPending;

  return (
    <>
      <Navbar />
      <Notif n={notif} />

      <main className="min-h-screen bg-canvas pt-[100px] pb-12 px-4 sm:px-6 lg:px-8">
        <div className="max-w-[60rem] mx-auto">

          {/* Header */}
          <div className="flex flex-col md:flex-row justify-between items-start md:items-center gap-4 mb-8">
            <div>
              <h1 className="t-heading-lg m-0">Quản lý trường học</h1>
              <p className="t-body-md text-mute mt-1">
                Danh sách các trường trong hệ thống. Admin có thể thêm, sửa, xóa.
              </p>
            </div>
            <div className="flex items-center gap-3">
              <div className="bg-primary/10 border border-primary/20 px-4 py-2 rounded-sm text-center min-w-[5rem]">
                <div className="t-heading-md text-primary">{schools.length}</div>
                <div className="t-caption-sm text-mute">Trường</div>
              </div>
              <button
                type="button"
                onClick={openCreate}
                className="btn btn-create flex items-center gap-2"
              >
                Thêm trường
              </button>
            </div>
          </div>

          <div className="card p-4 sm:p-6">
            {/* Search */}
            <div className="relative mb-6">
              <Search size={18} className="absolute left-3 top-1/2 -translate-y-1/2 text-mute pointer-events-none" />
              <input
                type="text"
                placeholder="Tìm kiếm theo tên trường..."
                value={searchInput}
                onChange={(e) => { setSearchInput(e.target.value); setSearch(e.target.value); }}
                className="w-full pl-10 pr-4 py-2.5 border border-hairline-strong rounded-sm text-sm focus:outline-none focus:border-primary"
              />
            </div>

            {actionError && (
              <div className="bg-error/10 border border-error rounded-sm p-4 mb-4 text-error text-sm font-bold">
                {actionError}
              </div>
            )}

            {/* Table */}
            {error ? (
              <div className="py-16 text-center text-error font-bold">
                Không tải được danh sách trường học.
              </div>
            ) : isLoading ? (
              <div className="py-20 flex flex-col items-center gap-4 text-mute">
                <div className="w-8 h-8 border-4 border-hairline-strong border-t-primary rounded-full animate-spin" />
                <div className="text-sm font-bold">Đang tải...</div>
              </div>
            ) : schools.length === 0 ? (
              <div className="py-20 text-center text-mute text-sm font-bold">
                {search ? 'Không tìm thấy trường nào khớp.' : 'Chưa có trường nào trong hệ thống.'}
              </div>
            ) : (
              <div className="overflow-x-auto">
                <table className="w-full border-collapse">
                  <thead>
                    <tr className="border-b border-hairline-strong text-left">
                      <th className="t-caption-md text-mute font-bold uppercase py-4 px-3">Tên trường</th>
                      <th className="t-caption-md text-mute font-bold uppercase py-4 px-3">Địa chỉ</th>
                      <th className="t-caption-md text-mute font-bold uppercase py-4 px-3 text-right">Thao tác</th>
                    </tr>
                  </thead>
                  <tbody>
                    {schools.map((s) => (
                      <tr
                        key={s.id}
                        className="border-b border-hairline last:border-b-0 hover:bg-black/[0.02] transition-colors"
                      >
                        <td className="t-body-sm font-bold text-ink py-3 px-3">{s.schoolName}</td>
                        <td className="t-body-sm text-body py-3 px-3">{s.address || '—'}</td>
                        <td className="py-3 px-3">
                          <div className="flex justify-end gap-2">
                            <button
                              type="button"
                              onClick={() => openEdit({ id: s.id, schoolName: s.schoolName, address: s.address })}
                              className="btn btn-update btn-sm flex items-center gap-1.5"
                            >
                              Sửa
                            </button>
                            <button
                              type="button"
                              onClick={() => setDeleteTarget({ id: s.id, name: s.schoolName })}
                              className="btn btn-delete btn-sm flex items-center gap-1.5"
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
            )}
          </div>
        </div>
      </main>

      {/* Modal Thêm / Sửa */}
      {modal && (
        <div
          role="dialog"
          aria-modal="true"
          className="fixed inset-0 z-[100] flex items-center justify-center bg-black/60 p-4"
          onClick={() => setModal(null)}
        >
          <div
            className="rounded-sm shadow-xl p-6"
            style={{ background: '#ffffff', width: '100%', maxWidth: '32rem' }}
            onClick={(e) => e.stopPropagation()}
          >
            <div className="flex items-center justify-between mb-6">
              <h2 className="t-heading-sm m-0">
                {modal.mode === 'create' ? 'Thêm trường học mới' : 'Chỉnh sửa trường học'}
              </h2>
              <button type="button" onClick={() => setModal(null)} aria-label="Đóng">
                <X size={20} className="text-mute hover:text-ink transition-colors" />
              </button>
            </div>

            {actionError && (
              <div className="bg-error/10 border border-error rounded-sm p-3 mb-4 text-error text-sm font-bold">
                {actionError}
              </div>
            )}

            <div className="flex flex-col gap-4 mb-6">
              <div>
                <label className="block t-caption-md text-mute font-bold uppercase tracking-wider mb-1.5">
                  Tên trường *
                </label>
                <input
                  type="text"
                  value={form.schoolName}
                  onChange={(e) => setForm((f) => ({ ...f, schoolName: e.target.value }))}
                  placeholder="VD: Đại học FPT, Đại học Bách Khoa..."
                  className="w-full px-3 py-2 border border-hairline-strong rounded-sm text-sm focus:outline-none focus:border-primary"
                />
              </div>
              <div>
                <label className="block t-caption-md text-mute font-bold uppercase tracking-wider mb-1.5">
                  Địa chỉ
                </label>
                <input
                  type="text"
                  value={form.address}
                  onChange={(e) => setForm((f) => ({ ...f, address: e.target.value }))}
                  placeholder="VD: Hà Nội, TP. Hồ Chí Minh..."
                  className="w-full px-3 py-2 border border-hairline-strong rounded-sm text-sm focus:outline-none focus:border-primary"
                />
              </div>
            </div>

            <div className="flex gap-3">
              <button
                type="button"
                onClick={() => setModal(null)}
                className="btn btn-outline flex-1"
              >
                Hủy
              </button>
              <button
                type="button"
                onClick={handleSubmit}
                disabled={isSaving || !form.schoolName.trim()}
                className={`btn flex-1 disabled:opacity-50 ${modal.mode === 'create' ? 'btn-create' : 'btn-update'}`}
              >
                {isSaving
                  ? 'Đang lưu...'
                  : modal.mode === 'create'
                  ? 'Thêm trường'
                  : 'Lưu thay đổi'}
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Confirm Delete */}
      {deleteTarget && (
        <div
          role="dialog"
          aria-modal="true"
          className="fixed inset-0 z-[100] flex items-center justify-center bg-black/60 p-4"
          onClick={() => setDeleteTarget(null)}
        >
          <div
            className="rounded-sm shadow-xl p-6"
            style={{ background: '#ffffff', width: '100%', maxWidth: '24rem' }}
            onClick={(e) => e.stopPropagation()}
          >
            <h2 className="t-heading-sm m-0 mb-3">Xác nhận xóa</h2>
            <p className="t-body-sm text-body mb-6">
              Bạn có chắc muốn xóa trường{' '}
              <span className="font-bold text-ink">&ldquo;{deleteTarget.name}&rdquo;</span>?
              Hành động này không thể hoàn tác.
            </p>
            <div className="flex gap-3">
              <button
                type="button"
                onClick={() => setDeleteTarget(null)}
                className="btn btn-outline flex-1"
              >
                Hủy
              </button>
              <button
                type="button"
                disabled={deleteMutation.isPending}
                onClick={() => deleteMutation.mutate(deleteTarget.id)}
                className="btn btn-delete flex-1 font-bold disabled:opacity-50"
              >
                {deleteMutation.isPending ? 'Đang xóa...' : 'Xóa trường'}
              </button>
            </div>
          </div>
        </div>
      )}
    </>
  );
}
