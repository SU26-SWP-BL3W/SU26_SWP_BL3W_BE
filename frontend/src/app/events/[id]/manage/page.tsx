'use client';

import { Suspense } from 'react';
import Link from 'next/link';
import { useParams } from 'next/navigation';
import { AdminEventDashboard } from '@/features/events/components/AdminEventDashboard/AdminEventDashboard';
import { useUserRole } from '@/hooks/useUserRole';
import { useCurrentUser } from '@/hooks/useAuth';
import { useUserEventRole } from '@/features/events/hooks/useEvents';

function AdminEventPageContent() {
  const params = useParams();
  const eventId = params?.id as string;
  const role = useUserRole();
  const { data: currentUser } = useCurrentUser();
  const userId = currentUser?.id ?? '';
  const { data: eventRole, isLoading: eventRoleLoading } = useUserEventRole(userId, eventId);

  if (role === null) {
    return (
      <div className="min-h-screen bg-canvas flex items-center justify-center">
        <p className="t-body-md text-mute">Vui lòng đăng nhập.</p>
      </div>
    );
  }

  if (role !== 'admin' && eventRoleLoading) {
    return (
      <div className="min-h-screen bg-canvas flex items-center justify-center">
        <p className="t-body-md text-mute">Đang kiểm tra quyền trong sự kiện…</p>
      </div>
    );
  }

  const canManage = role === 'admin' || eventRole?.roleName === 'EventCoordinator' || eventRole?.roleName === 'Admin';

  if (!canManage) {
    return (
      <div className="min-h-screen bg-canvas flex flex-col items-center justify-center gap-3">
        <p className="t-heading-md text-error">Bạn không có quyền truy cập trang quản lý.</p>
        <Link href={`/events/${eventId}`} className="btn btn-primary">
          Về trang sự kiện
        </Link>
      </div>
    );
  }

  const displayRole = eventRole?.roleName ?? (role === 'admin' ? 'Admin' : 'Guest');
  return <AdminEventDashboard eventId={eventId} role={displayRole} />;
}

export default function AdminEventPage() {
  return (
    <Suspense
      fallback={
        <div className="min-h-screen bg-canvas flex items-center justify-center">
          <p className="t-body-md text-mute">Đang tải...</p>
        </div>
      }
    >
      <AdminEventPageContent />
    </Suspense>
  );
}
