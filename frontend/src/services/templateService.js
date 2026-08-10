import apiClient from './api/client';

const ENDPOINTS = {
  LIST:            '/Templates',
  DETAIL:          (id)           => `/Templates/${id}`,
  CREATE:          '/Templates',
  UPDATE:          (id)           => `/Templates/${id}`,
  DELETE:          (id)           => `/Templates/${id}`,
  ADD_CRITERIA:    (id)           => `/Templates/${id}/criteria`,
  UPDATE_CRITERIA: (id, cid)      => `/Templates/${id}/criteria/${cid}`,
  REMOVE_CRITERIA: (id, cid)      => `/Templates/${id}/criteria/${cid}`,
};

function adaptTemplateCriteria(tc) {
  return {
    criteriaId:   tc.criteriaId ?? tc.id,
    criteriaName: tc.criteriaName ?? tc.name ?? '',
    description:  tc.description ?? '',
    weight:       tc.weight   ?? 0,
    maxScore:     tc.maxScore ?? 0,
    isActive:     tc.isActive,
  };
}

function adaptTemplate(t) {
  return {
    id:          t.id,
    name:        t.templateName ?? t.name ?? '',
    description: t.description ?? '',
    isActive:    t.isActive,
    isSystem:    t.isSystem ?? false,
    criterias:   (t.criterias ?? []).map(adaptTemplateCriteria),
  };
}

/** GET /api/Templates — danh sách bộ tiêu chí (phân trang) */
export const getTemplates = async () => {
  const res   = await apiClient.get(ENDPOINTS.LIST, { params: { PageNumber: 1, PageSize: 50 } });
  const items = res.data?.data ?? [];
  return items.map(adaptTemplate);
};

/** GET /api/Templates/{id} — chi tiết bộ tiêu chí (kèm danh sách tiêu chí) */
export const getTemplateDetail = async (id) => {
  const res = await apiClient.get(ENDPOINTS.DETAIL(id));
  return adaptTemplate(res.data);
};

/** POST /api/Templates — tạo bộ tiêu chí mới */
export const createTemplate = async ({ templateName, description = '' }) => {
  const res = await apiClient.post(ENDPOINTS.CREATE, { templateName, description });
  return adaptTemplate(res.data);
};

/** PUT /api/Templates/{id} — cập nhật bộ tiêu chí */
export const updateTemplate = async (id, { templateName, description = '' }) => {
  const res = await apiClient.put(ENDPOINTS.UPDATE(id), { templateName, description });
  return adaptTemplate(res.data);
};

/** DELETE /api/Templates/{id} — xóa bộ tiêu chí */
export const deleteTemplate = async (id) => {
  const res = await apiClient.delete(ENDPOINTS.DELETE(id));
  return res.data;
};

/** POST /api/Templates/{id}/criteria — thêm tiêu chí vào bộ */
export const addCriteriaToTemplate = async (templateId, { criteriaId, weight, maxScore }) => {
  const res = await apiClient.post(ENDPOINTS.ADD_CRITERIA(templateId), {
    criteriaId,
    weight:   Number(weight),
    maxScore: Number(maxScore),
  });
  return res.data;
};

/** PUT /api/Templates/{id}/criteria/{criteriaId} — cập nhật cấu hình tiêu chí trong bộ */
export const updateTemplateCriteria = async (templateId, criteriaId, { weight, maxScore }) => {
  const res = await apiClient.put(ENDPOINTS.UPDATE_CRITERIA(templateId, criteriaId), {
    weight:   Number(weight),
    maxScore: Number(maxScore),
  });
  return adaptTemplateCriteria(res.data);
};

/** DELETE /api/Templates/{id}/criteria/{criteriaId} — gỡ tiêu chí khỏi bộ */
export const removeFromTemplate = async (templateId, criteriaId) => {
  const res = await apiClient.delete(ENDPOINTS.REMOVE_CRITERIA(templateId, criteriaId));
  return res.data;
};
