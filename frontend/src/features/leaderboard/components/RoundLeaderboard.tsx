'use client';

import React, { useState } from 'react';
import { useEventRounds, useTeams } from '@/features/events/hooks/useEvents';
import { useRoundLeaderboard } from '@/features/results/hooks/useResults';
import { useEvent } from '@/features/events/hooks/useEvents';
import { StatusBadge } from '@/components/StatusBadge';

interface Props { eventId: string; }

const roundLabel = (r: { id: string; roundName: string | null }) =>
  r.roundName?.trim() || `Vòng ${r.id.slice(0, 4)}`;

export function RoundLeaderboard({ eventId }: Props) {
  const { data: event } = useEvent(eventId);
  const { data: rounds = [] } = useEventRounds(eventId);
  const { data: teams = [] } = useTeams();
  const [picked, setPicked] = useState('');

  // Default to the first round so a leaderboard shows immediately, until the
  // user picks another. Derived rather than synced via an effect.
  const roundId = picked || rounds[0]?.id || '';

  const { data: rows = [], isLoading } = useRoundLeaderboard(roundId || undefined);

  // The round leaderboard rarely carries teamName, so resolve it from the teams
  // list and fall back to whatever the row provides.
  const teamName = (row: { teamId: string; teamName?: string }) =>
    row.teamName ?? teams.find((t) => t.id === row.teamId)?.name ?? row.teamId;

  const sorted = [...rows].sort(
    (a, b) => (a.rank || 9999) - (b.rank || 9999) || b.finalScore - a.finalScore,
  );

  const getPrizeName = (prizeId?: string | null) => {
    if (!prizeId || !event?.prizes) return null;
    const prize = event.prizes.find(p => p.id === prizeId);
    return prize ? prize.prizeName : null;
  };

  return (
    <div className="border border-hairline rounded-sm bg-canvas p-4 md:p-6 space-y-4">
      <div className="flex items-center justify-between gap-3 flex-wrap">
        <h2 className="t-heading-md m-0">Bảng xếp hạng</h2>
        <select
          value={roundId}
          onChange={(e) => setPicked(e.target.value)}
          className="border border-hairline rounded-sm px-3 py-2 t-body-sm text-ink bg-canvas font-bold min-w-56"
          aria-label="Chọn vòng để xem bảng xếp hạng"
        >
          {rounds.length === 0 && <option value="">— Chưa có vòng —</option>}
          {(rounds as Array<{ id: string; roundName: string | null }>).map((r) => (
            <option key={r.id} value={r.id}>{roundLabel(r)}</option>
          ))}
        </select>
      </div>

      {!roundId ? (
        <p className="t-body-sm text-mute text-center py-8">Chưa có vòng nào để xem bảng xếp hạng.</p>
      ) : isLoading ? (
        <p className="t-body-sm text-mute text-center py-8">Đang tải…</p>
      ) : sorted.length === 0 ? (
        <p className="t-body-sm text-mute text-center py-8">Chưa có dữ liệu xếp hạng.</p>
      ) : (
        <div className="overflow-x-auto">
          <table className="w-full border-collapse">
            <thead>
              <tr className="border-b border-hairline-strong text-left">
                <th className="t-caption-md text-mute font-bold uppercase py-3 px-2 text-center w-16">Hạng</th>
                <th className="t-caption-md text-mute font-bold uppercase py-3 px-2">Đội</th>
                <th className="t-caption-md text-mute font-bold uppercase py-3 px-2 text-center">Điểm</th>
                <th className="t-caption-md text-mute font-bold uppercase py-3 px-2 text-center">Giải thưởng</th>
                <th className="t-caption-md text-mute font-bold uppercase py-3 px-2 text-center">Trạng thái</th>
              </tr>
            </thead>
            <tbody>
              {sorted.map((r, idx) => {
                const rank = r.rank || idx + 1;
                return (
                  <tr key={r.id} className="border-b border-hairline last:border-b-0">
                    <td className="py-3 px-2 text-center">
                      <span className="t-body-strong font-bold text-ink">{rank}</span>
                    </td>
                    <td className="t-body-sm font-bold text-ink py-3 px-2">{teamName(r)}</td>
                    <td className="py-3 px-2 text-center">
                      <span className="t-heading-sm text-primary font-bold">{r.finalScore.toFixed(2)}</span>
                    </td>
                    <td className="py-3 px-2 text-center">
                      {getPrizeName(r.prizeId) ? (
                        <span className="inline-block px-2 py-1 bg-primary/10 text-primary font-bold rounded text-sm">
                          {getPrizeName(r.prizeId)}
                        </span>
                      ) : (
                        <span className="text-mute text-sm">—</span>
                      )}
                    </td>
                    <td className="py-3 px-2 text-center">
                      <StatusBadge tone={r.isAdvanced ? 'success' : 'pending'}>
                        {r.isAdvanced ? 'Đi tiếp' : 'Dừng lại'}
                      </StatusBadge>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
