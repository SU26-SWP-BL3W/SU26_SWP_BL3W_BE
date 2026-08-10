'use client';

import React from 'react';
import { Navbar } from '@/components/Navbar';
import { TeamInvitationView } from '@/features/teams/components/TeamInvitationView';
import { useCurrentUser } from '@/hooks/useAuth';
import { useParams } from 'next/navigation';

export default function TeamDetailPage() {
  const params = useParams();
  const teamId = typeof params.id === 'string' ? params.id : Array.isArray(params.id) ? params.id[0] : '';
  const { data: user, isLoading } = useCurrentUser();

  const [mounted, setMounted] = React.useState(false);

  React.useEffect(() => {
    const timer = window.setTimeout(() => setMounted(true), 0);
    return () => window.clearTimeout(timer);
  }, []);

  if (!mounted || isLoading) {
    return (
      <div className="min-h-screen flex flex-col bg-slate-50">
        <Navbar />
        <div className="p-8 text-center text-mute mt-8">Đang tải...</div>
      </div>
    );
  }

  if (!user) {
    return (
      <div className="min-h-screen flex flex-col bg-slate-50">
        <Navbar />
        <div className="p-8 text-center text-mute mt-8">
          Vui lòng đăng nhập để xem thông tin đội và xác nhận lời mời.
        </div>
      </div>
    );
  }

  if (!teamId) {
    return (
      <div className="min-h-screen flex flex-col bg-slate-50">
        <Navbar />
        <div className="p-8 text-center text-error mt-8">Đường dẫn không hợp lệ.</div>
      </div>
    );
  }

  return (
    <div className="min-h-screen flex flex-col bg-slate-50">
      <Navbar />
      <main className="flex-1 py-8 px-4">
        <div className="w-full max-w-3xl mx-auto px-6">
          <TeamInvitationView teamId={teamId} />
        </div>
      </main>
    </div>
  );
}
