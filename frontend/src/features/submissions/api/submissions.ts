import { apiClient } from '@/services/api';
import type { PagedResult } from '@/services/api';

export interface SubmissionModel {
  id: string;
  teamId: string;
  trackId: string;
  submissionUrl: string;
  description: string;
  isActive: boolean;
  createdTime: string;
  teamName?: string;
  trackName?: string;
}

export interface CreateSubmissionPayload {
  teamId: string;
  trackId: string; // Vòng (round) do BE tự suy ra từ track — không gửi roundId
  submissionUrl: string;
  description: string;
}

export interface UpdateSubmissionPayload {
  id: string;
  submissionUrl: string;
  description: string;
  /** BẮT BUỘC gửi đúng giá trị hiện tại: chỉ Event Coordinator được đổi IsActive (BE chặn nếu đổi). */
  isActive: boolean;
}

export const submissionsApi = {
  /** eventId BẮT BUỘC — filter phân quyền BE cần EventId để xác định sự kiện của truy vấn. */
  list: async (params: { teamId: string; eventId: string; roundId?: string }): Promise<SubmissionModel[]> => {
    const { data } = await apiClient.get<PagedResult<SubmissionModel>>('/SubmitResults', {
      params: {
        TeamId: params.teamId,
        EventId: params.eventId,
        RoundId: params.roundId,
        PageNumber: 1,
        PageSize: 100,
        SortBy: 'CreatedTime',
        IsAscending: false,
      },
    });
    return data.data ?? [];
  },
  create: async (p: CreateSubmissionPayload): Promise<SubmissionModel> => {
    const { data } = await apiClient.post<SubmissionModel>('/SubmitResults', p);
    return data;
  },
  update: async (p: UpdateSubmissionPayload): Promise<SubmissionModel> => {
    const { data } = await apiClient.put<SubmissionModel>(`/SubmitResults/${encodeURIComponent(p.id)}`, p);
    return data;
  },
};
