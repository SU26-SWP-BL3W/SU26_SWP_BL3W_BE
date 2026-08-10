'use client';

import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { schoolsApi, authApi, type UserSummary } from '@/services/api';
import { StatusBadge, type StatusTone } from '@/components/StatusBadge';

interface Props {
  status: 'unregistered' | 'pending' | 'approved' | 'rejected';
  reason: string | null;
  profile: UserSummary | null;
  onChangePassword: () => void;
  onEdit: () => void;
  onResubmit: () => Promise<void> | void;
  rejectionCount?: number;
}

const BADGE: Record<Props['status'], { label: string; tone: StatusTone }> = {
  unregistered: { label: 'Chưa đăng ký hồ sơ', tone: 'neutral' },
  pending: { label: 'Chờ xét duyệt', tone: 'pending' },
  approved: { label: 'Đã được duyệt', tone: 'success' },
  rejected: { label: 'Tài khoản bị từ chối', tone: 'danger' },
};

function Row({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex justify-between gap-4 py-2 border-b border-hairline last:border-b-0">
      <span className="t-caption-sm text-mute">{label}</span>
      <span className="t-body-sm text-ink font-bold text-right">{value}</span>
    </div>
  );
}

export function RegistrationStatusCard({
  status,
  reason,
  profile,
  onChangePassword,
  onEdit,
  onResubmit,
  rejectionCount,
}: Props) {
  const badge = BADGE[status];

  const { data: schoolsData } = useQuery({
    queryKey: ['schools'],
    queryFn: () => schoolsApi.list(1000),
    staleTime: 5 * 60_000,
  });
  const schools = schoolsData?.data ?? [];

  const schoolLabel = profile
    ? profile.isFpt
      ? 'FPT University'
      : (schools.find((s) => s.id === profile.schoolId)?.schoolName ?? '—')
    : '—';

  // Bị khóa cập nhật hồ sơ khi bị từ chối >= 2 lần (đồng bộ với chặn ở BE).
  const isBlocked = (rejectionCount ?? 0) >= 2;
  const [unblock, setUnblock] = useState<'idle' | 'sending' | 'sent'>('idle');
  const handleUnblock = async () => {
    if (!profile?.email) return;
    setUnblock('sending');
    try {
      await authApi.requestUnblock(profile.email);
    } finally {
      setUnblock('sent'); // endpoint luôn trả thông báo chung (chống dò email) nên coi như đã gửi
    }
  };

  return (
    <div className="card flex w-full max-w-[40rem] flex-col gap-4 mx-auto" style={{ padding: 'var(--space-xl)' }}>
      <div className="flex items-center justify-between gap-3 flex-wrap">
        <h2 className="t-heading-md m-0">Hồ sơ cá nhân</h2>
        <div className="flex items-center gap-2">
          {rejectionCount !== undefined && rejectionCount > 0 && status !== 'approved' && (
            <span
              className="badge-tag"
              style={{
                background: 'rgba(229,32,32,0.05)',
                color: 'var(--color-error)',
                border: '1px solid rgba(229,32,32,0.2)',
              }}
            >
              Bị từ chối {rejectionCount} lần
            </span>
          )}
          <StatusBadge tone={badge.tone}>{badge.label}</StatusBadge>
        </div>
      </div>

      {status === 'rejected' && reason && (
        <p className="t-body-sm m-0" style={{ color: 'var(--color-error)' }}>
          Lý do: {reason}
        </p>
      )}

      {profile && (
        <div className="flex flex-col">
          <Row label="Họ và tên"  value={profile.fullName  || '—'} />
          <Row label="Email"       value={profile.email      || '—'} />
          <Row label="MSSV"        value={profile.studentCode || '—'} />
          <Row label="Trường"      value={schoolLabel} />
          {profile.photoStudentCardUrl && (
            <div className="py-2">
              {/* eslint-disable-next-line @next/next/no-img-element */}
              <img
                src={profile.photoStudentCardUrl}
                alt="Ảnh thẻ sinh viên"
                className="w-40 h-28 object-cover rounded-sm border border-hairline"
              />
            </div>
          )}
        </div>
      )}

      {status === 'unregistered' && (
        <>
          <p className="t-body-sm m-0 text-mute">
            Bạn chưa đăng ký hồ sơ cá nhân. Hãy đăng ký để có thể tham gia sự kiện.
          </p>
          <button type="button" className="btn btn-update w-fit" onClick={onEdit}>
            Đăng ký hồ sơ
          </button>
        </>
      )}
      {status === 'pending' && (
        <button type="button" className="btn btn-secondary w-fit" onClick={onEdit}>
          Cập nhật hồ sơ
        </button>
      )}
      {status === 'approved' && (
        <div className="flex gap-2">
          <button type="button" className="btn btn-update w-fit" onClick={onEdit}>
            Cập nhật hồ sơ
          </button>
          <button type="button" className="btn btn-secondary w-fit" onClick={onChangePassword}>
            Đổi mật khẩu
          </button>
        </div>
      )}
      {status === 'rejected' && !isBlocked && (
        <button type="button" className="btn btn-secondary w-fit" onClick={onResubmit}>
          Gửi lại
        </button>
      )}
      {status === 'rejected' && isBlocked && (
        <div className="flex flex-col gap-2">
          <p className="t-body-sm m-0" style={{ color: 'var(--color-error)' }}>
            Hồ sơ đã bị từ chối {rejectionCount} lần nên không thể tự cập nhật. Hãy gửi yêu cầu để ban tổ chức hỗ trợ gỡ khóa.
          </p>
          {unblock === 'sent' ? (
            <p className="t-body-sm m-0 text-mute">
              ✓ Đã gửi yêu cầu gỡ khóa. Vui lòng theo dõi email để nhận phản hồi từ ban tổ chức.
            </p>
          ) : (
            <button
              type="button"
              className="btn btn-primary w-fit"
              onClick={handleUnblock}
              disabled={unblock === 'sending'}
            >
              {unblock === 'sending' ? 'Đang gửi…' : 'Yêu cầu gỡ khóa'}
            </button>
          )}
        </div>
      )}
    </div>
  );
}
