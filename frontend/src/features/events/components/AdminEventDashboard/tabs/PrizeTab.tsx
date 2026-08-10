'use client';

import { useState, type FormEvent } from 'react';
import { createPortal } from 'react-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { prizesApi, type PrizeModel, type UpsertPrizePayload } from '@/features/events/api/prizes';
import { useNotify } from '@/components/NotificationProvider';
import { useDialog } from '@/components/ConfirmDialogProvider';
import { getErrorMessage } from '@/lib/apiError';
import { formatPrizeValue } from '@/lib/formatPrizeValue';
import { Card } from '../../EventDashboard/Card';
import { CardSkeleton } from '../../EventDashboard/SkeletonLoaders';

interface PrizeTabProps { eventId: string; }

const emptyForm = (): UpsertPrizePayload => ({ prizeName: '', value: '', quantity: 1 });

export function PrizeTab({ eventId }: PrizeTabProps) {
  const notify = useNotify();
  const dialog = useDialog();
  const queryClient = useQueryClient();
  const queryKey = ['prizes', eventId] as const;
  const [editing, setEditing] = useState<PrizeModel | null>(null);
  const [form, setForm] = useState<UpsertPrizePayload>(emptyForm);
  const [formError, setFormError] = useState('');
  const [modalOpen, setModalOpen] = useState(false);

  const prizesQuery = useQuery({
    queryKey,
    queryFn: () => prizesApi.listByEvent(eventId),
    enabled: !!eventId,
  });

  const saveMutation = useMutation({
    mutationFn: async (payload: UpsertPrizePayload) => {
      if (editing) await prizesApi.update(editing.id, payload);
      else await prizesApi.create(eventId, payload);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey });
      notify.success(editing ? 'Đã cập nhật giải thưởng.' : 'Đã thêm giải thưởng.');
      setModalOpen(false);
    },
    onError: (error) => setFormError(getErrorMessage(error, 'Không thể lưu giải thưởng.')),
  });

  const removeMutation = useMutation({
    mutationFn: (id: string) => prizesApi.remove(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey });
      notify.success('Đã xóa giải thưởng.');
    },
    onError: (error) => notify.error(getErrorMessage(error, 'Không thể xóa giải thưởng.')),
  });

  const openCreate = () => {
    setEditing(null);
    setForm(emptyForm());
    setFormError('');
    setModalOpen(true);
  };

  const openEdit = (prize: PrizeModel) => {
    setEditing(prize);
    setForm({ prizeName: prize.prizeName, value: prize.value, quantity: prize.quantity });
    setFormError('');
    setModalOpen(true);
  };

  const submit = (event: FormEvent) => {
    event.preventDefault();
    const prizeName = form.prizeName.trim();
    const value = form.value.trim();
    if (!prizeName || !value || !Number.isInteger(form.quantity) || form.quantity <= 0) {
      setFormError('Vui lòng nhập tên, giá trị và số lượng nguyên dương.');
      return;
    }
    const payload = { prizeName, value, quantity: form.quantity };
    setForm(payload);
    saveMutation.mutate(payload);
  };

  const remove = async (prize: PrizeModel) => {
    const accepted = await dialog.confirm({
      title: 'Xóa giải thưởng',
      message: `Xóa “${prize.prizeName}” khỏi cơ cấu giải thưởng?`,
      confirmText: 'Xóa',
      danger: true,
    });
    if (accepted) removeMutation.mutate(prize.id);
  };

  if (prizesQuery.isLoading) return <CardSkeleton />;

  return (
    <>
      <Card title="Cơ cấu giải thưởng">
        <div className="flex items-start justify-between gap-4 mb-5">
          <p className="t-body-sm text-mute m-0 max-w-2xl">
            Khai báo các giải của sự kiện trước khi trao giải trong bảng xếp hạng.
          </p>
          <button type="button" className="btn btn-create shrink-0" onClick={openCreate}>
            + Thêm giải thưởng
          </button>
        </div>

        {prizesQuery.isError ? (
          <p role="alert" className="t-body-sm text-error py-6 text-center">Không tải được cơ cấu giải thưởng.</p>
        ) : prizesQuery.data?.length ? (
          <div className="overflow-x-auto">
            <table className="w-full border-collapse">
              <thead>
                <tr className="border-b border-hairline-strong text-left">
                  <th className="t-caption-md text-mute font-bold uppercase py-3 px-2">Tên giải</th>
                  <th className="t-caption-md text-mute font-bold uppercase py-3 px-2">Giá trị</th>
                  <th className="t-caption-md text-mute font-bold uppercase py-3 px-2 text-center">Số lượng</th>
                  <th className="t-caption-md text-mute font-bold uppercase py-3 px-2 text-right">Thao tác</th>
                </tr>
              </thead>
              <tbody>
                {prizesQuery.data.map((prize) => (
                  <tr key={prize.id} className="border-b border-hairline last:border-b-0">
                    <td className="t-body-sm font-bold text-ink py-3 px-2">{prize.prizeName}</td>
                    <td className="t-body-sm text-ink py-3 px-2 tabular-nums">{formatPrizeValue(prize.value)}</td>
                    <td className="t-body-sm text-ink py-3 px-2 text-center tabular-nums">{prize.quantity}</td>
                    <td className="py-3 px-2">
                      <div className="flex justify-end gap-2">
                        <button type="button" className="btn btn-update btn-sm" onClick={() => openEdit(prize)}>Sửa</button>
                        <button type="button" className="btn btn-delete btn-sm" disabled={removeMutation.isPending} onClick={() => remove(prize)}>Xóa</button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        ) : (
          <div className="bg-surface-soft border border-hairline rounded-sm py-10 px-5 text-center">
            <p className="t-body-strong text-ink m-0">Chưa có giải thưởng</p>
            <p className="t-body-sm text-mute mt-1 mb-0">Thêm giải để có thể trao cho các đội đạt thứ hạng.</p>
          </div>
        )}
      </Card>

      {modalOpen && createPortal(
        <div className="fixed inset-0 z-50 bg-black/55 flex items-center justify-center p-4" role="presentation" onMouseDown={(e) => e.target === e.currentTarget && setModalOpen(false)}>
          <section role="dialog" aria-modal="true" aria-labelledby="prize-form-title" className="bg-canvas border border-hairline rounded-sm w-full max-w-2xl max-h-[calc(100dvh-2rem)] overflow-y-auto shadow-2xl">
            <form className="p-5 sm:p-7 flex flex-col gap-5" onSubmit={submit}>
              <div>
                <h2 id="prize-form-title" className="t-heading-md m-0">{editing ? 'Sửa giải thưởng' : 'Thêm giải thưởng'}</h2>
                <p className="t-body-sm text-mute mt-1 mb-0">Thông tin này sẽ xuất hiện khi trao giải trong bảng xếp hạng.</p>
              </div>
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                <label className="flex flex-col gap-1"><span className="t-body-sm font-bold">Tên giải</span><input autoFocus className="text-input" value={form.prizeName} onChange={(e) => setForm((current) => ({ ...current, prizeName: e.target.value }))} placeholder="Ví dụ: Giải Nhất" /></label>
                <label className="flex flex-col gap-1">
                  <span className="t-body-sm font-bold">Giá trị</span>
                  <span className="relative block">
                    <input
                      className="text-input tabular-nums"
                      style={{ paddingRight: '4.5rem' }}
                      value={form.value}
                      aria-describedby="prize-value-unit"
                      onChange={(e) => setForm((current) => ({ ...current, value: e.target.value }))}
                      placeholder="Ví dụ: 10.000.000"
                    />
                    <span
                      id="prize-value-unit"
                      className="pointer-events-none absolute inset-y-px right-px flex w-14 items-center justify-center border-l border-hairline bg-surface-soft t-body-sm font-bold text-mute"
                    >
                      VND
                    </span>
                  </span>
                </label>
                <label className="flex flex-col gap-1 sm:max-w-48"><span className="t-body-sm font-bold">Số lượng</span><input className="text-input" type="number" min={1} step={1} value={form.quantity} onChange={(e) => setForm((current) => ({ ...current, quantity: Number(e.target.value) }))} /></label>
              </div>
              {formError && <p role="alert" className="t-body-sm text-error m-0">{formError}</p>}
              <div className="flex justify-end gap-2 pt-2">
                <button type="button" className="btn btn-outline" disabled={saveMutation.isPending} onClick={() => setModalOpen(false)}>Hủy</button>
                <button type="submit" className="btn btn-create" disabled={saveMutation.isPending}>{saveMutation.isPending ? 'Đang lưu…' : editing ? 'Lưu thay đổi' : 'Thêm giải'}</button>
              </div>
            </form>
          </section>
        </div>,
        document.body,
      )}
    </>
  );
}
