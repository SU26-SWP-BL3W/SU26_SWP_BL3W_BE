'use client';
import { useState, useEffect } from 'react';
import SubmissionView from '@/views/SubmissionPage';
import { submitResultsApi } from '@/features/events/api/submitResults';
import { getErrorMessage } from '@/lib/apiError';
import { parseSubmissionLinks } from '@/features/submissions/utils/submissionLinks';

/**
 * Danh sách bài nộp — nhúng vào tab dashboard.
 * Dùng API thật từ submitResultsApi (TS) thay vì submissionService.js cũ.
 *
 * Props:
 *  - eventId: bắt buộc — backend cần EventId cho GET /SubmitResults
 *  - trackId: nếu có — chỉ lấy bài nộp của hạng mục đó
 *
 * @param {{ eventId?: string | null, trackId?: string | null }} props
 */
export default function SubmissionsPanel({ eventId = null, trackId = null }) {
  const [submissions, setSubmissions] = useState([]);
  const [loading,     setLoading]     = useState(true);
  const [error,       setError]       = useState(null);

  useEffect(() => {
    // `loading` đã mặc định true — chưa có eventId thì giữ spinner, không setState ở
    // đây (tránh setState đồng bộ trong effect); effect tự chạy lại khi có eventId.
    if (!eventId) return;

    let cancelled = false;

    const load = async () => {
      setLoading(true);
      setError(null);
      try {
        const items = await submitResultsApi.list({
            eventId:  eventId ?? undefined,
            trackId:  trackId ?? undefined,
            pageSize: 100,
          });
        if (cancelled) return;
        // Map từ SubmitResultListItem → shape SubmissionView mong đợi
        setSubmissions(items.map(item => ({
          id:          item.id,
          teamId:      item.teamId,
          teamName:    item.teamName ?? '—',
          projectName: item.teamId?.slice(0, 8).toUpperCase() ?? '—',
          repo:        item.submissionUrl ?? item.repoUrl ?? '',
          // Bài nộp nhiều link (JSON [{label,url}]) -> parse thành danh sách;
          // dữ liệu cũ 1 URL vẫn ra 1 phần tử (tương thích ngược).
          links:       parseSubmissionLinks(item.submissionUrl ?? item.repoUrl ?? ''),
          submittedAt: item.createdTime
          ? new Date(item.createdTime.replace('+00:00', 'Z')).toLocaleString('vi-VN', {
              day: '2-digit', month: '2-digit', year: 'numeric',
              hour: '2-digit', minute: '2-digit',
            })
          : '—',
          status: item.isActive ? 'Đã nhận' : 'Không hoạt động',
        })));
      } catch (err) {
        // Lấy message thật từ backend (nếu có) thay vì text chung chung của axios,
        // để biết đúng lý do thất bại (vd hết hạn nộp, chưa tới vòng...).
        if (!cancelled) setError(getErrorMessage(err, 'Lỗi tải dữ liệu bài nộp.'));
      } finally {
        if (!cancelled) setLoading(false);
      }
    };

    load();
    return () => { cancelled = true; };
  }, [eventId, trackId]);

  if (loading) {
    return (
      <div style={{ color: '#757575', textAlign: 'center', paddingTop: 80, fontSize: 14 }}>
        Đang tải danh sách bài nộp…
      </div>
    );
  }

  if (error) {
    return (
      <div style={{ color: '#d32f2f', textAlign: 'center', paddingTop: 80, fontSize: 14 }}>
        {error}
      </div>
    );
  }

  return <SubmissionView submissions={submissions} teams={[]} />;
}
