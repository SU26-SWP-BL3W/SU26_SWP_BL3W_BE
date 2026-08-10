import type { UserSummary, UserRejectionModel } from '@/services/api';
import type { RegistrationStatus } from './types';

export interface ResolvedRegistration { status: RegistrationStatus; reason: string | null; }

/**
 * approved  -> isApproved.
 * rejected  -> có bản ghi từ chối đang active.
 * pending   -> đã NỘP hồ sơ nhưng chưa được duyệt (đang chờ xét duyệt).
 * unregistered -> chưa nộp hồ sơ thí sinh lần nào (không phải "chờ duyệt").
 */
export function resolveRegistrationStatus(
  profile: UserSummary | null,
  rejections: UserRejectionModel[],
): ResolvedRegistration {
  if (profile?.isApproved) return { status: 'approved', reason: null };

  // Find active rejection
  const activeRejection = rejections.find((r) => r.isActive);
  if (activeRejection) {
    return { status: 'rejected', reason: activeRejection.reason };
  }

  // Chưa nộp hồ sơ (chỉ vừa tạo tài khoản) -> KHÔNG hiển thị "chờ duyệt".
  const hasSubmitted =
    !!profile && (profile.isStudent || !!profile.studentCode || !!profile.photoStudentCardUrl);
  if (!hasSubmitted) return { status: 'unregistered', reason: null };

  return { status: 'pending', reason: null };
}
