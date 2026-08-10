'use client';

import React from 'react';
import { RoundLeaderboard } from '@/features/leaderboard/components/RoundLeaderboard';

interface Props { eventId: string; userId: string; }

export function LeaderboardTab({ eventId }: Props) {
  return (
    <section className="p-6 max-w-3xl mx-auto space-y-4">
      <h2 className="t-heading-md">Bảng xếp hạng</h2>
      <RoundLeaderboard eventId={eventId} />
    </section>
  );
}
