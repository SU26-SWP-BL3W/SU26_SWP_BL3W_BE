import apiClient from "./client";

/**
 * File storage is served by the deployed API (not the local backend).
 * Absolute URL overrides apiClient's baseURL for these requests only.
 */
const STORAGE_BASE =
  process.env.NEXT_PUBLIC_STORAGE_API_URL ??
  "https://api.sealswp391.xyz/api/Storage";

export const storageApi = {
  /**
   * Upload a file and return its full URL (consumable by `/Storage/download`).
   * Used at registration to attach a non-FPT student's card photo.
   */
  upload: async (file: File): Promise<string> => {
    const form = new FormData();
    form.append("file", file);
    const { data } = await apiClient.post<unknown>(
      `${STORAGE_BASE}/upload`,
      form,
      // Let the browser set multipart/form-data + boundary (override the
      // client's default application/json header).
      { headers: { "Content-Type": undefined } },
    );
    // Response is the file URL string; tolerate an object envelope defensively.
    if (typeof data === "string") return data;
    if (data && typeof data === "object") {
      const obj = data as Record<string, unknown>;
      const url = obj.fileUrl ?? obj.url ?? obj.path;
      if (typeof url === "string") return url;
    }
    throw new Error("Upload không trả về đường dẫn tệp hợp lệ.");
  },
};
