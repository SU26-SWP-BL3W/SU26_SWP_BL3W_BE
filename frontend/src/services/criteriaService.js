import apiClient from './api/client';

// Backend: /api/Criterias + /api/Templates/{id}
// Client đã unwrap BaseResponse<T> → response.data = env.data trực tiếp.

const ENDPOINTS = {
  LIST:     '/Criterias',
  DETAIL:   (id) => `/Criterias/${id}`,
  CREATE:   '/Criterias',
  UPDATE:   (id) => `/Criterias/${id}`,
  DELETE:   (id) => `/Criterias/${id}`,
  TOGGLE:   (id) => `/Criterias/${id}/toggle-status`,
  TEMPLATE: (id) => `/Templates/${id}`,
};

function adaptCriteria(backendItem, templateCriteria) {
  if (!backendItem) return null;
  return {
    id:       backendItem.id,
    label:    backendItem.criteriaName,
    labelVi:  backendItem.criteriaName,
    desc:     backendItem.description ?? '',
    isActive: backendItem.isActive,
    weight:   templateCriteria?.weight   ?? 0,
    maxScore: templateCriteria?.maxScore ?? 0,
  };
}

/** GET /api/Criterias — lấy danh sách tiêu chí (kèm weight nếu truyền templateId) */
export const getCriteria = async (templateId) => {
  // const res   = await apiClient.get(ENDPOINTS.LIST); // cũ: backend trả mặc định 10 items
  const res   = await apiClient.get(ENDPOINTS.LIST, { params: { PageNumber: 1, PageSize: 1000 } });
  const items = res.data?.data ?? [];   // PagedResult.data = []

  if (!templateId) {
    return items.map(item => adaptCriteria(item, null));
  }

  const tmplRes  = await apiClient.get(ENDPOINTS.TEMPLATE(templateId));
  const tmplList = tmplRes.data?.criterias ?? [];
  const tmplMap  = Object.fromEntries(
    tmplList.map(tc => [tc.criteriaId ?? tc.id, tc])
  );

  return items.map(item => adaptCriteria(item, tmplMap[item.id]));
};

/** POST /api/Criterias — tạo tiêu chí mới */
export const createCriteria = async ({ criteriaName, description = '', isActive = true }) => {
  const res = await apiClient.post(ENDPOINTS.CREATE, { criteriaName, description, isActive });
  return adaptCriteria(res.data, null);
};

/** PUT /api/Criterias/{id} — cập nhật tiêu chí */
export const updateCriteria = async (id, { criteriaName, description = '', isActive = true }) => {
  const res = await apiClient.put(ENDPOINTS.UPDATE(id), { criteriaName, description, isActive });
  return adaptCriteria(res.data, null);
};

/** DELETE /api/Criterias/{id} — xóa tiêu chí */
export const deleteCriteria = async (id) => {
  const res = await apiClient.delete(ENDPOINTS.DELETE(id));
  return res.data;
};

/** PATCH /api/Criterias/{id}/toggle-status — bật/tắt trạng thái tiêu chí */
export const toggleCriteriaStatus = async (id) => {
  const res = await apiClient.patch(ENDPOINTS.TOGGLE(id));
  return res.data;
};
