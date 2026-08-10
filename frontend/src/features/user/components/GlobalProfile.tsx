'use client';

import { useState } from 'react';
import { useRegistration } from '@/features/registration/hooks/useRegistration';
import { RegistrationForm } from '@/features/registration/components/RegistrationForm';
import { RegistrationStatusCard } from '@/features/registration/components/RegistrationStatusCard';
import { ChangePasswordForm } from '@/features/registration/components/ChangePasswordForm';
import { useCurrentUser } from '@/hooks/useAuth';

export function GlobalProfile() {
  const { data: user } = useCurrentUser();
  const { profile, status, reason, rejectionCount, isLoading, submit, clearRejections } = useRegistration(user?.id ?? '');
  const [editing, setEditing] = useState(false);
  const [changingPassword, setChangingPassword] = useState(false);

  if (isLoading) return <div className="t-body-md text-mute p-6 text-center">Đang tải hồ sơ...</div>;

  // Admin không phải thí sinh → không render khối này.
  if (user && user.role === 'ADMIN') {
    return (
      <div className="card max-w-[40rem] mx-auto mt-8 p-6 text-center shadow-sm bg-white rounded-lg">
        <h2 className="t-heading-md mb-2">Hồ sơ tài khoản</h2>
        <p className="t-body-md text-mute m-0">Tài khoản quản trị không cần hồ sơ thí sinh.</p>
      </div>
    );
  }

  if (editing) {
    return (
      <div className="card max-w-[40rem] mx-auto mt-8 p-6 shadow-sm bg-white rounded-lg">
        <h2 className="t-heading-md mb-6">Cập nhật hồ sơ</h2>
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
      <div className="card max-w-[40rem] mx-auto mt-8 p-6 shadow-sm bg-white rounded-lg">
        <h2 className="t-heading-md mb-6">Đổi mật khẩu</h2>
        <ChangePasswordForm
          onSuccess={() => setChangingPassword(false)}
          onBack={() => setChangingPassword(false)}
        />
      </div>
    );
  }

  return (
    <div className="flex justify-center mt-8 px-4">
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
