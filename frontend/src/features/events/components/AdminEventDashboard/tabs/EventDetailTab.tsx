'use client';

import React, { useState } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useRouter } from 'next/navigation';
import { useEvent, useEventRoles, useUserEventRole } from '@/features/events/hooks/useEvents';
import { useUserRole } from '@/hooks/useUserRole';
import { useCurrentUser } from '@/hooks/useAuth';
import { eventsApi } from '@/features/events/api/events';
import { useNotify } from '@/components/NotificationProvider';
import { useDialog } from '@/components/ConfirmDialogProvider';
import { getErrorMessage } from '@/lib/apiError';
import { Card } from '../../EventDashboard/Card';
import { Button } from '../../EventDashboard/Button';
import { CardSkeleton } from '../../EventDashboard/SkeletonLoaders';
import { CreateEventForm } from '../../CreateEventForm';
import { EventTimeline } from '../../EventDashboard/EventTimeline';
import { EventPhoto } from '../../EventPhoto';
import { formatPrizeValue } from '@/lib/formatPrizeValue';

interface EventDetailTabProps {
  eventId: string;
}

export function EventDetailTab({ eventId }: EventDetailTabProps) {
  const { data: event, isLoading, error } = useEvent(eventId);
  const { data: roles = [] } = useEventRoles(eventId);
  const [isEditing, setIsEditing] = useState(false);
  const { data: currentUser } = useCurrentUser();
  const { data: eventRole } = useUserEventRole(currentUser?.id ?? '', eventId);
  const globalRole = useUserRole();
  const [now] = useState(() => Date.now());
  // Sự kiện đã kết thúc (quá ngày kết thúc) → khóa chỉnh sửa.
  const isEnded = !!event && new Date(event.endDate).getTime() < now;
  const canManage = globalRole === 'admin' || eventRole?.roleName === 'EventCoordinator' || eventRole?.roleName === 'Admin';
  const canEdit = canManage && !isEnded;
  const router = useRouter();
  const queryClient = useQueryClient();
  const notify = useNotify();
  const dialog = useDialog();

  // teamCount vẫn cần cho ràng buộc xóa; judge/mentor tổng đã chuyển vào StatsStrip.
  const teamCount = new Set(roles.map((r) => r.teamId).filter(Boolean)).size;

  const deleteMutation = useMutation({
    mutationFn: () => eventsApi.remove(eventId),
    onSuccess: () => {
      notify.success("Xóa sự kiện thành công!");
      queryClient.invalidateQueries({ queryKey: ['events'] });
      router.push('/');
    },
    onError: (e) => {
      notify.error(getErrorMessage(e, "Không thể xóa sự kiện. Vui lòng thử lại."));
    }
  });

  const handleDelete = async () => {
    if (!event) return;

    if (event.status === 'open') {
      notify.error("Sự kiện đang ở trạng thái công khai. Vui lòng chỉnh sửa và chuyển sự kiện về trạng thái Ẩn trước khi xóa.");
      return;
    }
    if (teamCount > 0) {
      notify.error("Không thể xóa sự kiện đã có đội thi đăng ký để bảo vệ thông tin đăng ký của sinh viên.");
      return;
    }

    const ok = await dialog.confirm({
      title: 'Xóa sự kiện',
      message: 'Bạn có chắc chắn muốn xóa sự kiện này không?\nHành động này không thể hoàn tác.',
      confirmText: 'Xóa sự kiện',
      danger: true,
    });
    if (ok) deleteMutation.mutate();
  };

  if (error) {
    return (
      <div className="bg-error/10 border border-error rounded-sm p-6 text-center">
        <p className="t-body-md text-error font-bold">Không tải được sự kiện</p>
        <p className="t-body-sm text-mute mt-2">Vui lòng thử lại</p>
      </div>
    );
  }

  if (isLoading) {
    return (
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <CardSkeleton />
        <CardSkeleton />
        <CardSkeleton />
      </div>
    );
  }

  if (!event) return <div className="t-body-md text-mute">Không tìm thấy sự kiện</div>;

  if (isEditing && canEdit) {
    return (
      <Card title="Chỉnh sửa sự kiện">
        <CreateEventForm eventId={eventId} onCancel={() => setIsEditing(false)} />
      </Card>
    );
  }

  return (
    <div className="flex flex-col gap-4 md:gap-6">
      <Card className="border-transparent">
        <div className="space-y-4">
          <EventPhoto url={event.photoEventUrl} alt={event.title} />
          <h1 className="text-ink text-center font-bold m-0 leading-tight text-4xl md:text-5xl">
            {event.title}
          </h1>
          {(event.season || event.year) && (
            <p className="text-ink text-center m-0 font-bold text-lg md:text-xl">
              {[event.season, event.year].filter(Boolean).join(' · ')}
            </p>
          )}
          <p className="text-body whitespace-pre-line leading-relaxed max-w-2xl mx-auto text-center font-normal text-base md:text-lg">
            {event.description || 'Chưa có mô tả.'}
          </p>

          {event.prizes && event.prizes.length > 0 && (
            <div className="mt-8 border-t border-line pt-6 w-full">
              <h2 className="text-xl md:text-2xl font-bold text-ink text-center mb-6">Cơ cấu giải thưởng</h2>
              <div className="flex flex-wrap justify-center gap-4">
                {event.prizes.map((prize) => (
                  <div key={prize.id} className="bg-surface border border-line rounded-lg p-4 text-center min-w-[200px] flex-1 max-w-[250px]">
                    <h3 className="font-bold text-lg text-primary m-0">{prize.prizeName}</h3>
                    <p className="text-ink font-semibold m-0 mt-2 tabular-nums">{formatPrizeValue(prize.value)}</p>
                    <p className="text-mute text-sm m-0 mt-1">Số lượng: {prize.quantity}</p>
                  </div>
                ))}
              </div>
            </div>
          )}
        </div>
      </Card>

      <div className="w-full max-w-7xl mx-auto">
        <EventTimeline eventId={eventId} variant="admin" />
      </div>

      {/* Canh thẳng với vạch spine chính của timeline: cùng cột max-w-7xl,
          thụt trái = padding card (p-4/p-6) + vị trí spine (12px). */}
      <div className="w-full max-w-7xl mx-auto pt-2 flex flex-wrap items-center gap-3 pl-7 md:pl-9">
        {canManage && (
          <Button
            variant="update"
            size="md"
            style={{ color: "#fff" }}
            disabled={isEnded}
            title={isEnded ? "Sự kiện đã kết thúc — không thể chỉnh sửa." : undefined}
            onClick={() => setIsEditing(true)}
          >
            Chỉnh sửa sự kiện
          </Button>
        )}
        <Button
          variant="delete"
          size="md"
          onClick={handleDelete}
          isLoading={deleteMutation.isPending}
        >
          Xóa sự kiện
        </Button>
      </div>
    </div>
  );
}
