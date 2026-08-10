import apiClient from './apiClient';

// Response chain sau khi SU26 client unwrap BaseResponse:
//   response.data                   = SubmitResultListItemModelPagedResult
//   response.data.data              = SubmitResultListItemModel[]
//   response.data.totalItems / totalPages / currentPage

const ENDPOINTS = {
  LIST:   '/SubmitResults',
  BY_ID:  (id) => `/SubmitResults/${id}`,
  CREATE: '/SubmitResults',
  UPDATE: (id) => `/SubmitResults/${id}`,
  DELETE: (id) => `/SubmitResults/${id}`,
};

// SubmitResultListItemModel → shape SubmissionPage.jsx mong đợi
function adaptSubmission(item) {
  return {
    id:          item.id,
    teamId:      item.teamId,
    teamName:    item.teamName ?? item.teamId ?? '—',
    projectName: item.description ?? item.solutionTitle ?? '—',
    repo:        item.submissionUrl ?? '',
    submittedAt: item.createdTime
      ? new Date(item.createdTime).toLocaleString('vi-VN', {
          day:    '2-digit',
          month:  '2-digit',
          year:   'numeric',
          hour:   '2-digit',
          minute: '2-digit',
        })
      : '—',
    status: item.isActive === false ? 'Không hoạt động' : 'Đã nhận',
  };
}

// GET /SubmitResults  (Admin / Judge — toàn bộ danh sách)
// params: { TeamId, RoundId, TrackId, PageNumber, PageSize, SortBy, IsAscending }
export const getAllSubmissions = async (params = {}) => {
  const response = await apiClient.get(ENDPOINTS.LIST, { params });
  const paged = response.data;           // SubmitResultListItemModelPagedResult
  return {
    items:       (paged?.data ?? []).map(adaptSubmission),
    totalItems:  paged?.totalItems  ?? 0,
    currentPage: paged?.currentPage ?? 1,
    totalPages:  paged?.totalPages  ?? 1,
    hasNextPage: paged?.hasNextPage  ?? false,
  };
};

// GET /SubmitResults/{id}  (Admin / Judge)
export const getSubmissionById = async (id) => {
  const response = await apiClient.get(ENDPOINTS.BY_ID(id));
  return adaptSubmission(response.data);
};

// POST /SubmitResults  (Team — nộp bài mới)
export const submitProject = async ({ teamId, trackId, submissionUrl, description }) => {
  const response = await apiClient.post(ENDPOINTS.CREATE, {
    teamId,
    trackId,
    submissionUrl,
    description,
  });
  return response.data;
};

// PUT /SubmitResults/{id}  (TeamLeader / EventCoordinator)
export const updateSubmission = async (id, { submissionUrl, description, isActive }) => {
  const response = await apiClient.put(ENDPOINTS.UPDATE(id), {
    id,
    submissionUrl,
    description,
    isActive,
  });
  return response.data;
};

// DELETE /SubmitResults/{id}  (TeamLeader / EventCoordinator)
export const deleteSubmission = async (id) => {
  const response = await apiClient.delete(ENDPOINTS.DELETE(id));
  return response.data;
};
