'use client';

import React, { useState } from 'react';
import { useEventRounds } from '@/features/events/hooks/useEvents';
import { useTeamFinalResults, useTeamRoundBreakdown } from '@/features/results/hooks/useResults';

interface Props { teamId: string; eventId: string; }

function RoundRow({ teamId, roundId, roundName, finalScore, rank, isAdvanced }: {
  teamId: string; roundId: string; roundName: string; finalScore: number; rank: number; isAdvanced: boolean;
}) {
  const [open, setOpen] = useState(false);
  const { data: breakdown, isLoading } = useTeamRoundBreakdown(teamId, open ? roundId : '');

  return (
    <div className="border border-hairline rounded-md overflow-hidden">
      <button
        type="button" onClick={() => setOpen((v) => !v)}
        className="w-full flex justify-between items-center p-3 bg-surface-dim hover:bg-surface-strong"
      >
        <span className="t-body-md font-bold">{roundName}</span>
        <span className="flex gap-3 text-sm">
          <span>Điểm: <b>{finalScore.toFixed(2)}</b></span>
          <span>Hạng: <b>#{rank}</b></span>
          {isAdvanced ? <span className="text-success">Đi tiếp</span> : null}
        </span>
      </button>

      {open ? (
        <div className="p-3 space-y-3">
          {isLoading ? <p className="t-body-sm text-mute">Đang tải chi tiết…</p> : null}
          {(breakdown ?? []).length === 0 && !isLoading ? (
            <p className="t-body-sm text-mute">Chưa có phiếu chấm cho vòng này.</p>
          ) : null}
          {(breakdown ?? []).map(({ score, details, trackName }) => {
            const totalMax = Math.round(details.reduce((acc, d) => acc + Number(d.maxScore ?? 0), 0) * 100) / 100;
            return (
              <div key={score.id} className="space-y-1">
                <p className="t-body-sm">
                  Giám khảo <b>{score.judgeName}</b> — tổng <b>{score.totalScore.toFixed(2)}</b>/{totalMax}
                  {trackName && (
                    <span className="text-mute ml-2">
                      (Hạng mục: {trackName})
                    </span>
                  )}
                </p>
                <table className="w-full text-sm">
                  <thead className="text-left">
                    <tr><th>Tiêu chí</th><th>Điểm</th><th>Tối đa</th><th>Trọng số</th><th>Điểm đóng góp</th></tr>
                  </thead>
                  <tbody>
                    {details.map((d) => (
                      <tr key={d.id} className="border-t border-hairline">
                        <td>{d.criteriaName}</td>
                        <td>{d.value.toFixed(2)}</td>
                        <td>{d.maxScore ?? '—'}</td>
                        <td>{d.weight ?? '—'}</td>
                        <td>{d.value.toFixed(2)}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
                {score.comment ? (
                  <p className="t-body-sm text-mute">Nhận xét: {score.comment}</p>
                ) : null}
              </div>
            );
          })}
        </div>
      ) : null}
    </div>
  );
}

export function ResultsAccordion({ teamId, eventId }: Props) {
  const { data: rounds = [] } = useEventRounds(eventId);
  const { data: finals = [] } = useTeamFinalResults(teamId);

  if (finals.length === 0) return <p className="t-body-md text-mute">Chưa có kết quả.</p>;

  return (
    <div className="space-y-3">
      {finals.map((f) => {
        const round = (rounds as Array<{ id: string; roundName: string }>).find((r) => r.id === f.roundId);
        return (
          <RoundRow
            key={f.id}
            teamId={teamId} roundId={f.roundId}
            roundName={round?.roundName ?? f.roundId}
            finalScore={f.finalScore} rank={f.rank} isAdvanced={f.isAdvanced}
          />
        );
      })}
    </div>
  );
}
