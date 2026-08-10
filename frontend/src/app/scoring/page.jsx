'use client';
import { Suspense, useState, useEffect } from 'react';
import { useSearchParams, useRouter } from 'next/navigation';
import { Navbar } from '@/components/Navbar';
import SubmissionsScoringPanel from '@/features/events/components/shared/SubmissionsScoringPanel';
import { useCurrentUser } from '@/hooks/useAuth';
import { eventsApi } from '@/features/events/api/events';

// ─── Inner component (needs useSearchParams → must be inside Suspense) ────────

function ScoringInner() {
  const searchParams = useSearchParams();
  const router       = useRouter();
  const eventId      = searchParams.get('eventId');

  const { data: currentUser } = useCurrentUser();

  // Khi không có eventId — event selector
  const [events, setEvents]               = useState([]);
  const [eventsLoading, setEventsLoading] = useState(true);

  // Load danh sách sự kiện khi chưa có eventId
  useEffect(() => {
    if (eventId || !currentUser?.id) return;
    eventsApi.list()
      .then(setEvents)
      .catch(console.error)
      .finally(() => setEventsLoading(false));
  }, [eventId, currentUser?.id]);

  // ── Chưa chọn sự kiện ─────────────────────────────────────────────────────
  if (!eventId) {
    return (
      <>
        <Navbar />
        <main style={{ minHeight: '100vh', background: '#f7f7f7', padding: '28px 32px', fontFamily: 'Inter, sans-serif' }}>
          <div className="animate-fadeUp" style={{ maxWidth: 640 }}>
            <h2 className="text-xl font-bold mb-1" style={{ color: '#000' }}>Chấm điểm</h2>
            <p className="text-sm mb-6" style={{ color: '#757575' }}>
              Chọn sự kiện bạn muốn chấm điểm.
            </p>

            {eventsLoading ? (
              <div className="flex items-center gap-3 py-10">
                <span className="loading loading-spinner" style={{ color: '#76b900' }} />
                <span className="text-sm" style={{ color: '#757575' }}>Đang tải danh sách sự kiện...</span>
              </div>
            ) : events.length === 0 ? (
              <div className="text-center py-10">
                <p className="text-sm" style={{ color: '#757575' }}>Không có sự kiện nào.</p>
              </div>
            ) : (
              <div className="flex flex-col gap-3">
                {events.map(e => (
                  <button
                    key={e.id}
                    className="card-hover p-5 text-left w-full"
                    style={{ background: '#fff', border: '1px solid #cccccc', borderRadius: 2 }}
                    onClick={() => router.push(`/scoring?eventId=${e.id}`)}
                  >
                    <div className="text-sm font-bold" style={{ color: '#000' }}>{e.title}</div>
                    <div className="text-xs mt-1" style={{ color: '#757575' }}>
                      {new Date(e.startDate).toLocaleDateString('vi-VN')}
                      {' – '}
                      {new Date(e.endDate).toLocaleDateString('vi-VN')}
                    </div>
                  </button>
                ))}
              </div>
            )}
          </div>
        </main>
      </>
    );
  }

  // ── Đã có eventId: dùng chung panel với dashboard sự kiện ─────────────────
  return (
    <>
      <Navbar />
      <main style={{ minHeight: '100vh', background: '#f7f7f7', padding: '28px 32px', fontFamily: 'Inter, sans-serif' }}>
        <SubmissionsScoringPanel eventId={eventId} />
        <div className="flex justify-start mt-6">
          <button
            className="btn btn-outline text-sm"
            onClick={() => router.push('/scoring')}
          >
            ← Chọn sự kiện khác
          </button>
        </div>
      </main>
    </>
  );
}

// ─── Page export — Suspense required by useSearchParams ───────────────────────

export default function ScoringRoute() {
  return (
    <Suspense fallback={
      <div style={{ minHeight: '100vh', background: '#f7f7f7', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
        <span className="text-sm" style={{ color: '#757575' }}>Đang tải...</span>
      </div>
    }>
      <ScoringInner />
    </Suspense>
  );
}
