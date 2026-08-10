import { apiClient } from '@/services/api';
import type { PagedResult } from '@/services/api';

export interface FinalResultRow {
  id: string;
  teamId: string;
  teamName?: string;
  roundId: string;
  finalScore: number;
  rank: number;
  isAdvanced: boolean;
  prizeId?: string | null;
}

// ─── Team score breakdown (C9 — thí sinh xem điểm từng giám khảo × tiêu chí) ───
export interface CriterionLine {
  criteriaName: string;
  value: number;
  maxScore: number;
  weight: number;
}
export interface JudgeBreakdown {
  judgeName: string;
  totalScore: number;
  comment: string | null;
  criteria: CriterionLine[];
}
export interface SubmissionBreakdown {
  submitResultId: string;
  trackName: string;
  roundId: string;
  roundName: string;
  roundPublished: boolean;
  judgeScores: JudgeBreakdown[];
}
export interface TeamScoreBreakdown {
  teamId: string;
  teamName: string;
  submissions: SubmissionBreakdown[];
}

export const resultsApi = {
  /** GET /api/Scores/team/{teamId}/breakdown — điểm theo bài nộp → giám khảo → tiêu chí.
   *  BE trả BaseResponse<obj> (interceptor đã bóc .data) nên đọc thẳng `data`. */
  getTeamBreakdown: async (teamId: string): Promise<TeamScoreBreakdown> => {
    const { data } = await apiClient.get<TeamScoreBreakdown>(
      `/Scores/team/${encodeURIComponent(teamId)}/breakdown`,
    );
    return data ?? { teamId, teamName: '', submissions: [] };
  },

  listTeamFinalResults: async (teamId: string): Promise<FinalResultRow[]> => {
    const { data } = await apiClient.get<PagedResult<FinalResultRow>>(
      `/FinalResults/team/${encodeURIComponent(teamId)}`,
      { params: { PageNumber: 1, PageSize: 100 } },
    );
    return data.data ?? [];
  },

  listRoundLeaderboard: async (roundId: string): Promise<FinalResultRow[]> => {
    const { data } = await apiClient.get<PagedResult<FinalResultRow>>(
      `/FinalResults/round/${encodeURIComponent(roundId)}`,
      { params: { PageNumber: 1, PageSize: 100, SortBy: 'Rank', IsAscending: true } },
    );
    return data.data ?? [];
  },

};
