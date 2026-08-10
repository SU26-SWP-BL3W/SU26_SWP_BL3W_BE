import apiClient from "./client";

/** Matches backend FptStudentResponse (flat, not enveloped). */
export interface FptStudentResponse {
  isValid: boolean;
  studentCode: string | null;
  fullName: string | null;
  major: string | null;
  enrollYear: number;
}

export const fptMockApi = {
  /**
   * GET /api/fpt-mock/students/{studentCode}.
   * Returns the student on success; throws a 404 AxiosError if the code does
   * not exist in the FPT system. Used to validate FPT codes before register
   * (an invalid code makes the register endpoint throw a 500).
   */
  validateStudent: async (studentCode: string): Promise<FptStudentResponse> => {
    const { data } = await apiClient.get<FptStudentResponse>(
      `/fpt-mock/students/${encodeURIComponent(studentCode)}`,
    );
    return data;
  },
};
