// ─── Backend envelope ─────────────────────────────────────────────────────────

/** Every backend response is wrapped in this envelope. */
export interface BaseResponse<T> {
  data: T | null;
  message: string;
  statusCode: number;
  success: boolean;
}

export interface PagedResult<T> {
  data: T[];
  currentPage: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

// ─── Auth Request / Response ──────────────────────────────────────────────────

export interface LoginRequest {
  email: string;
  password: string;
}

/** Matches backend RegisterUserRequestModel. */
export interface RegisterRequest {
  schoolId?: string;
  studentCode?: string;
  email: string;
  password: string;
  fullName: string;
  isStudent?: boolean;
  /** Derived from the selected school name (contains "FPT"). */
  isFpt?: boolean;
}

/** Matches backend LoginUserResponseModel (flat shape). */
export interface LoginResponse {
  accessToken: string;
  refreshToken: string;
  userId: string;
  email: string;
  fullName: string;
  isAdmin: boolean;
  isStudent: boolean;
  /** true nếu là tài khoản tạm (được mời vào đội) — FE bắt buộc đổi mật khẩu ngay. */
  mustChangePassword?: boolean;
}

export interface RefreshTokenResponse {
  accessToken: string;
  refreshToken: string;
}

// ─── User (backend UserModel) ─────────────────────────────────────────────────

export interface BackendUserModel {
  id: string;
  schoolId: string;
  studentCode: string | null;
  email: string;
  fullName: string;
  isStudent: boolean;
  isAdmin: boolean;
  isApproved: boolean;
}

// ─── In-app user profile (derived) ────────────────────────────────────────────

export interface UserStats {
  eventsJoined: number;
  projectScore: number;
  rank: number;
}

export interface Announcement {
  id: string;
  text: string;
  ctaLabel?: string;
  ctaUrl?: string;
}

export interface ProjectSummary {
  semesterName: string;
  projectName: string;
  completionPct: number;
  teamSize: number;
}

export interface UserProfile {
  id: string;
  email: string;
  fullName: string;
  role: "STUDENT" | "MENTOR" | "ADMIN";
  avatar?: string;
  createdAt: string;
  stats: UserStats;
  announcement?: Announcement;
  projectSummary?: ProjectSummary;
}

// ─── Schools ──────────────────────────────────────────────────────────────────

export interface SchoolModel {
  id: string;
  schoolName: string;
  address: string | null;
}

// ─── Student profile update ───────────────────────────────────────────────────

export interface UpdateStudentProfileCommand {
  schoolId: string | null;
  studentCode: string;
  photoStudentCardUrl: string | null;
  isFpt: boolean;
  fullName: string;
}

// ─── API Error ────────────────────────────────────────────────────────────────

export interface ApiError {
  message: string;
  statusCode: number;
  errors?: Record<string, string[]>;
  /** Field-level validation payload returned by the backend BaseResponse. */
  details?: unknown;
}

export interface UserRejectionModel {
  id: string;
  userId: string;
  rejectedBy: string;
  reason: string | null;
  isActive: boolean;
  createdTime: string;
  lastUpdatedTime: string;
}

// ─── Generic paginated response (legacy alias) ────────────────────────────────

export interface PaginatedResponse<T> {
  data: T[];
  total: number;
  page: number;
  pageSize: number;
  totalPages: number;
}
