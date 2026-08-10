'use client';

import React, { useState } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { useCreateTeam, TEAM_KEYS } from '@/features/teams/hooks/useTeams';
import { useEventDashboard } from '@/features/events/contexts/EventDashboardContext';
import { useCurrentUser } from '@/hooks/useAuth';
import { useRegistration } from '@/features/registration/hooks/useRegistration';
import { useUserRole } from '@/hooks/useUserRole';
import Link from 'next/link';
import { getErrorMessage } from '@/lib/apiError';

interface Props { eventId: string; userId: string; }

export function CreateTeamTab({ eventId, userId }: Props) {
  const qc = useQueryClient();
  const { setActiveTab } = useEventDashboard();
  const { data: me } = useCurrentUser();
  const role = useUserRole();
  const create = useCreateTeam(eventId, userId);
  const { status: registrationStatus } = useRegistration(userId);

  const [teamName, setTeamName] = useState('');
  const [description, setDescription] = useState('');

  const submit = (e: React.FormEvent) => {
    e.preventDefault();
    create.mutate(
      { name: teamName, description, eventId, leaderId: userId },
      {
        onSuccess: (created) => {
          // Show the team immediately with the creator as leader — the real
          // roster reconciles on the next fetch.
          qc.setQueryData(TEAM_KEYS.myTeam(eventId, userId), {
            id: created.id,
            teamName: created.name?.trim() || teamName,
            description: created.description ?? description,
            members: me
              ? [{ userId: me.id, fullName: me.fullName, email: me.email, isLeader: true }]
              : [],
          });
          setActiveTab('myTeam');
        },
      },
    );
  };

  if (role !== 'student') {
    return (
      <div className="p-6 max-w-[36rem] mx-auto border border-hairline rounded-sm bg-canvas flex flex-col gap-3">
        <h2 className="t-heading-md m-0">Tạo đội</h2>
        <p className="t-body-sm text-mute m-0">
          Phần này chỉ dành cho những sinh viên trường Đại học FPT và sinh viên ngoài trường đã được xét duyệt, nếu muốn tham gia thi và tạo đội hãy cập nhật đầy đủ hồ sơ.
        </p>
        <Link href="/profile" className="btn btn-primary w-fit">
          Cập nhật hồ sơ
        </Link>
      </div>
    );
  }

  if (registrationStatus !== 'approved') {
    return (
      <div className="p-6 max-w-[36rem] mx-auto border border-hairline rounded-sm bg-canvas flex flex-col gap-3">
        <h2 className="t-heading-md m-0">Tạo đội</h2>
        <p className="t-body-sm text-mute m-0">
          Bạn cần được duyệt hồ sơ thí sinh trước khi tạo đội.
        </p>
        <button type="button" className="btn btn-primary w-fit" onClick={() => setActiveTab('register')}>
          Tới hồ sơ thí sinh
        </button>
      </div>
    );
  }

  return (
    <form onSubmit={submit} className="p-6 w-full max-w-[36rem] mx-auto space-y-4 border border-hairline rounded-sm bg-canvas">
      <h2 className="t-heading-md">Tạo đội</h2>

      <label className="block">
        <span className="t-body-sm font-bold">Tên đội</span>
        <input
          required minLength={2} maxLength={80}
          value={teamName} onChange={(e) => setTeamName(e.target.value)}
          className="input w-full mt-1"
        />
      </label>

      <label className="block">
        <span className="t-body-sm font-bold">Mô tả</span>
        <textarea
          value={description} onChange={(e) => setDescription(e.target.value)}
          rows={3} className="input w-full mt-1"
        />
      </label>

      {create.error ? <p className="t-body-sm text-error">{getErrorMessage(create.error, 'Không thể tạo đội.')}</p> : null}

      <button type="submit" disabled={create.isPending} className="btn btn-create">
        {create.isPending ? 'Đang tạo…' : 'Tạo đội'}
      </button>
    </form>
  );
}
