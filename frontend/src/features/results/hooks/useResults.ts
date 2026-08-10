'use client';

import { useQuery } from '@tanstack/react-query';
import { resultsApi } from '../api/results';

export const RESULTS_KEYS = {
  teamFinals: (teamId: string) => ['results', 'team', teamId] as const,
  round:      (roundId: string) => ['results', 'round', roundId] as const,
};

export const useTeamFinalResults = (teamId: string | undefined) =>
  useQuery({
    queryKey: RESULTS_KEYS.teamFinals(teamId ?? ''),
    queryFn: () => resultsApi.listTeamFinalResults(teamId as string),
    enabled: !!teamId,
    staleTime: 60_000,
  });

export const useRoundLeaderboard = (roundId: string | undefined) =>
  useQuery({
    queryKey: RESULTS_KEYS.round(roundId ?? ''),
    queryFn: () => resultsApi.listRoundLeaderboard(roundId as string),
    enabled: !!roundId,
    staleTime: 60_000,
  });

/**
 * For one team + one round: returns [{ score, details[] }, ...]
 * (one entry per judge that scored the team's submission in that round).
 *
 * Nguồn: GET /api/Scores/team/{teamId}/breakdown (C9) — endpoint contestant xem được;
 * fetch 1 lần theo team, lọc theo round + làm phẳng (giám khảo → tiêu chí) tại FE.
 */
export const useTeamRoundBreakdown = (teamId: string, roundId: string) => {
  const q = useQuery({
    queryKey: ['results', 'team-breakdown', teamId] as const,
    queryFn: () => resultsApi.getTeamBreakdown(teamId),
    enabled: !!teamId && !!roundId,
    staleTime: 60_000,
  });

  const data = (q.data?.submissions ?? [])
    .filter((s) => s.roundId === roundId)
    .flatMap((s) =>
      s.judgeScores.map((js, i) => ({
        trackName: s.trackName,
        score: {
          id: `${s.submitResultId}-${i}`,
          judgeName: js.judgeName,
          totalScore: js.totalScore,
          comment: js.comment,
        },
        details: js.criteria.map((c, ci) => ({
          id: `${s.submitResultId}-${i}-${ci}`,
          criteriaName: c.criteriaName,
          value: c.value,
          maxScore: c.maxScore,
          weight: c.weight,
        })),
      })),
    );

  return { isLoading: q.isLoading, error: q.error, data };
};
