import { apiClient } from "@/services/api";
import type { PagedResult } from "@/services/api";

/** Scoring template — a track links to one of these via `templateId`. */
export interface TemplateSummary {
  id: string;
  templateName: string;
  description: string | null;
}

/** One criteria entry inside a template (with weight & maxScore). */
export interface TemplateCriteria {
  criteriaId: string;
  criteriaName: string;
  description: string | null;
  weight: number;
  maxScore: number;
  isActive: boolean;
}

/** Full template returned by GET /api/Templates/{id} */
export interface TemplateDetail extends TemplateSummary {
  isActive: boolean;
  isSystem: boolean;
  criterias: TemplateCriteria[];
}

/** Một tiêu chí trong "kho" dùng chung (GET /api/Criterias). */
export interface CriteriaPoolItem {
  id: string;
  criteriaName: string;
  description: string | null;
  isActive: boolean;
}

export const templatesApi = {
  /** GET /api/Templates — list scoring templates (requires auth). */
  list: async (): Promise<TemplateSummary[]> => {
    const { data } = await apiClient.get<PagedResult<TemplateSummary>>(
      "/Templates",
      { params: { PageNumber: 1, PageSize: 100 } },
    );
    return data.data ?? [];
  },

  /** GET /api/Templates/{id} — lấy template kèm toàn bộ criteria (weight, maxScore). */
  getById: async (id: string): Promise<TemplateDetail> => {
    const { data } = await apiClient.get<TemplateDetail>(`/Templates/${id}`);
    return data;
  },

  /** POST /api/Templates — tạo template mới, trả id. */
  create: async (templateName: string, description: string): Promise<string> => {
    const { data } = await apiClient.post<{ id: string }>("/Templates", { templateName, description });
    return data.id;
  },

  /** PUT /api/Templates/{id} — đổi tên / mô tả template. */
  updateInfo: (id: string, templateName: string, description: string): Promise<void> =>
    apiClient.put(`/Templates/${encodeURIComponent(id)}`, { templateName, description }).then(() => undefined),

  /** POST /api/Templates/{id}/criteria — gắn 1 tiêu chí (từ kho) vào template kèm trọng số/điểm. */
  addCriteria: (
    templateId: string,
    body: { criteriaId: string; weight: number; maxScore: number },
  ): Promise<void> =>
    apiClient.post(`/Templates/${encodeURIComponent(templateId)}/criteria`, body).then(() => undefined),

  /** PUT /api/Templates/{id}/criteria/{criteriaId} — đổi trọng số/điểm của tiêu chí trong template. */
  updateCriteria: (
    templateId: string,
    criteriaId: string,
    body: { weight: number; maxScore: number },
  ): Promise<void> =>
    apiClient
      .put(`/Templates/${encodeURIComponent(templateId)}/criteria/${encodeURIComponent(criteriaId)}`, body)
      .then(() => undefined),

  /** DELETE /api/Templates/{id}/criteria/{criteriaId} — gỡ tiêu chí khỏi template. */
  removeCriteria: (templateId: string, criteriaId: string): Promise<void> =>
    apiClient
      .delete(`/Templates/${encodeURIComponent(templateId)}/criteria/${encodeURIComponent(criteriaId)}`)
      .then(() => undefined),

  /**
   * Tạo bộ mới DỰA TRÊN một bộ có sẵn: tạo template mới với `newName` (phải khác tên
   * các bộ hiện có vì BE chặn trùng tên) rồi copy toàn bộ cấu hình tiêu chí
   * (criteriaId + weight + maxScore). Trả id bộ mới.
   */
  clone: async (baseId: string, newName: string): Promise<string> => {
    const base = await templatesApi.getById(baseId);
    const newId = await templatesApi.create(newName, base.description ?? "");
    for (const c of base.criterias) {
      await templatesApi.addCriteria(newId, { criteriaId: c.criteriaId, weight: c.weight, maxScore: c.maxScore });
    }
    return newId;
  },
};

export const criteriasApi = {
  /** GET /api/Criterias — kho tiêu chí dùng chung (để thêm vào template). */
  list: async (): Promise<CriteriaPoolItem[]> => {
    const { data } = await apiClient.get<PagedResult<CriteriaPoolItem>>(
      "/Criterias",
      { params: { PageNumber: 1, PageSize: 200 } },
    );
    return data.data ?? [];
  },
};
