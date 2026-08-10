'use client';

import { useState } from 'react';
import { useRegistration } from '../hooks/useRegistration';
import { RegistrationForm } from './RegistrationForm';
import { RegistrationStatusCard } from './RegistrationStatusCard';
import { ChangePasswordForm } from './ChangePasswordForm';
import { useCurrentUser } from '@/hooks/useAuth';

interface Props { userId: string; }

export function CompetitionRegistrationTab({ userId }: Props) {
  const { data: user } = useCurrentUser();
  const { profile, status, reason, rejectionCount, isLoading, submit, clearRejections } = useRegistration(userId);
  const [editing, setEditing] = useState(false);
  const [changingPassword, setChangingPassword] = useState(false);

  if (isLoading) return <div className="t-body-md text-mute p-6">Đang tải…</div>;

  if (editing) {
    return (
      <div className="p-1 md:p-2">
        <h2 className="t-heading-md mb-4">{profile ? 'Cập nhật hồ sơ' : 'Đăng ký hồ sơ thí sinh'}</h2>
        <RegistrationForm
          defaults={{
            fullName: profile?.fullName ?? user?.fullName ?? '',
            studentCode: profile?.studentCode ?? '',
            isFpt: profile?.isFpt ?? true,
            schoolId: profile?.schoolId ?? null,
            photoStudentCardUrl: profile?.photoStudentCardUrl ?? null,
          }}
          onSubmit={async (cmd) => {
            await submit(cmd);
            setEditing(false);
          }}
          onBack={() => setEditing(false)}
        />
      </div>
    );
  }

  if (changingPassword) {
    return (
      <div className="p-1 md:p-2">
        <h2 className="t-heading-md mb-4">Đổi mật khẩu</h2>
        <ChangePasswordForm
          onSuccess={() => setChangingPassword(false)}
          onBack={() => setChangingPassword(false)}
        />
      </div>
    );
  }

  return (
    <div className="p-1 md:p-2">
      <RegistrationStatusCard
        status={status}
        reason={reason}
        profile={profile}
        rejectionCount={rejectionCount}
        onChangePassword={() => setChangingPassword(true)}
        onEdit={() => setEditing(true)}
        onResubmit={async () => {
          await clearRejections();
          setEditing(true);
        }}
      />
    </div>
  );
}
