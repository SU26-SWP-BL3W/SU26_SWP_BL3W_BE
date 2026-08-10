'use client';

import React, { useState } from 'react';
import { ChevronDown } from 'lucide-react';
import { useQueries } from '@tanstack/react-query';
import { useEventRounds, useTeams } from '@/features/events/hooks/useEvents';
import { manageApi, type FinalResult } from '@/features/events/api/manage';
import { Card } from './EventDashboard/Card';
import { CardSkeleton } from './EventDashboard/SkeletonLoaders';
import { StatusBadge } from '@/components/StatusBadge';

interface Props { eventId: string; }

/**
 * The single leaderboard shared by every role in an event (admin, judge,
 * participant). Pick a round (defaults to the newest round that actually has
 * results) and see the FinalResults ranking.
 */
export function EventLeaderboard({ eventId }: Props) {
  const { data: rounds = [], isLoading: roundsLoading, error: roundsError } = useEventRounds(eventId);
  const { data: teams = [], isLoading: teamsLoading } = useTeams();

  // Newest round first.
  const sortedRounds = [...rounds].sort((a, b) => b.roundNumber - a.roundNumber);

  // Fetch every round's results so we can default to a round that has data
  // (the newest round is often still empty).
  const resultQueries = useQueries({
    queries: sortedRounds.map((r) => ({
      queryKey: ['finalResults', r.id],
      queryFn: () => manageApi.listRoundFinalResults(r.id),
      enabled: !!r.id,
      staleTime: 60_000,
    })),
  });

  const resultsByRound = new Map<string, FinalResult[]>();
  sortedRounds.forEach((r, i) => resultsByRound.set(r.id, resultQueries[i]?.data ?? []));

  // Default = newest round that has results; fall back to the newest round.
  const defaultRoundId =
    sortedRounds.find((r) => (resultsByRound.get(r.id)?.length ?? 0) > 0)?.id ??
    sortedRounds[0]?.id ??
    '';

  const [picked, setPicked] = useState('');
  const roundId = picked || defaultRoundId;
  const results = resultsByRound.get(roundId) ?? [];
  const resultsLoading = resultQueries.some((q) => q.isLoading);

  const teamName = (teamId: string | null) =>
    teams.find((t) => t.id === teamId)?.name ?? teamId ?? '—';

  // Use the backend rank when present, otherwise sort by score desc.
  const ranked = [...results].sort(
    (a, b) => (a.rank || 9999) - (b.rank || 9999) || b.finalScore - a.finalScore,
  );

  if (roundsError) {
    return (
      <div className="bg-error/10 border border-error rounded-sm p-6 text-center">
        <p className="t-body-md text-error font-bold">Không tải được bảng xếp hạng</p>
      </div>
    );
  }
  if (roundsLoading) return <CardSkeleton />;

  return (
    <Card title="Bảng xếp hạng">
      <div className="space-y-4">
        {/* Round picker — block layout so it can't collapse; chevron sits inside on the right. */}
        <div style={{ maxWidth: 240 }}>
          <span className="t-caption-xs text-mute uppercase font-bold block mb-1.5">Vòng thi</span>
          <div className="relative">
            <select
              className="text-input"
              value={roundId}
              onChange={(e) => setPicked(e.target.value)}
              disabled={sortedRounds.length === 0}
              style={{ width: '100%', appearance: 'none', WebkitAppearance: 'none', MozAppearance: 'none', paddingTop: 0, paddingBottom: 0, paddingRight: 40, lineHeight: 'normal', textOverflow: 'ellipsis' }}
            >
              {sortedRounds.length === 0 ? (
                <option value="">Chưa có vòng thi</option>
              ) : (
                sortedRounds.map((r) => (
                  <option key={r.id} value={r.id}>
                    Vòng {r.roundNumber}{r.roundName ? ` — ${r.roundName}` : ''}
                  </option>
                ))
              )}
            </select>
            <ChevronDown
              size={20}
              aria-hidden="true"
              className="absolute right-3 top-1/2 -translate-y-1/2 text-ink pointer-events-none"
            />
          </div>
        </div>

        {resultsLoading || teamsLoading ? (
          <p className="t-body-sm text-mute py-8 text-center">Đang tải…</p>
        ) : ranked.length > 0 ? (
          <div className="overflow-x-auto">
            <table className="w-full border-collapse">
              <thead>
                <tr className="border-b border-hairline-strong text-left">
                  <th className="t-caption-md text-mute font-bold uppercase py-3 px-2 text-center w-16">Hạng</th>
                  <th className="t-caption-md text-mute font-bold uppercase py-3 px-2">Tên đội</th>
                  <th className="t-caption-md text-mute font-bold uppercase py-3 px-2 text-center">Điểm</th>
                  <th className="t-caption-md text-mute font-bold uppercase py-3 px-2 text-right">Vào vòng tiếp</th>
                </tr>
              </thead>
              <tbody>
                {ranked.map((row, idx) => {
                  const rank = row.rank || idx + 1;
                  return (
                    <tr key={row.id} className="border-b border-hairline last:border-b-0">
                      <td className="py-3 px-2 text-center">
                        <span className="t-body-strong font-bold text-ink">{rank}</span>
                      </td>
                      <td className="t-body-sm font-bold text-ink py-3 px-2">{teamName(row.teamId)}</td>
                      <td className="t-heading-sm text-primary font-bold py-3 px-2 text-center">{row.finalScore}</td>
                      <td className="py-3 px-2 text-right">
                        <StatusBadge tone={row.isAdvanced ? 'success' : 'pending'}>
                          {row.isAdvanced ? 'Đi tiếp' : 'Dừng lại'}
                        </StatusBadge>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        ) : (
          <p className="t-body-sm text-mute text-center py-8">Chưa có dữ liệu xếp hạng</p>
        )}
      </div>
    </Card>
  );
}
