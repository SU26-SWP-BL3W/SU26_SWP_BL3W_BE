// Modal xem chi tiết chấm điểm — CHỈ XEM, dùng cho các vai trò không phải Judge
// (Mentor/EventCoordinator/Admin...). Hiển thị điểm + nhận xét của TẤT CẢ giám khảo
// đã chấm cho 1 bài nộp, lấy từ GET /Scores/team/{teamId}/breakdown (xem scores.ts).
//
// Khác với EditModal.jsx (dành cho Judge — 1 phiếu chấm của chính họ, có thể sửa),
// modal này hiển thị NHIỀU phiếu chấm (mỗi giám khảo 1 phiếu) và không có ô nhập nào.

export default function ScoreBreakdownModal({ teamLabel, submission, onClose }) {
  const judgeScores = submission?.judgeScores ?? [];
  const hasScores = judgeScores.length > 0;

  return (
    <div className="modal-overlay" style={{ padding: '24px 16px', zIndex: 200, alignItems: 'flex-start', paddingTop: '80px' }}>
      <div className="modal-box" style={{ maxWidth: 680 }}>

        {/* Header */}
        <div className="flex justify-between items-start mb-6">
          <div>
            <div className="text-lg font-bold" style={{ color: '#000' }}>Chi tiết chấm điểm</div>
            <div className="text-xs mt-1" style={{ color: '#757575' }}>
              Chỉ xem — điểm và nhận xét từ (các) giám khảo đã chấm bài nộp này.
            </div>
            <div className="text-sm mt-1.5 font-bold" style={{ color: '#76b900' }}>{teamLabel}</div>
          </div>
          <button
            className="btn-hover text-xl leading-none"
            onClick={onClose}
            style={{ background: 'transparent', border: 'none', color: '#757575' }}
          >✕</button>
        </div>

        {!hasScores ? (
          <div
            className="p-8 text-center text-sm"
            style={{ background: '#f7f7f7', border: '1px dashed #cccccc', borderRadius: 2, color: '#757575' }}
          >
            Chưa có giám khảo nào chấm bài nộp này.
          </div>
        ) : (
          judgeScores.map((js, ji) => {
            const totalMax = Math.round((js.criteria ?? []).reduce((sum, c) => sum + Number(c.maxScore || 0), 0) * 100) / 100;
            return (
            <div
              key={ji}
              className="mb-4 p-5"
              style={{ background: '#f7f7f7', border: '1px solid #cccccc', borderRadius: 2 }}
            >
              {/* Judge header */}
              <div className="flex justify-between items-start mb-3">
                <div className="text-sm font-bold" style={{ color: '#000' }}>
                  {js.judgeName || 'Giám khảo'}
                </div>
                <div className="text-right">
                  <div className="text-2xl font-black leading-none" style={{ color: '#76b900' }}>
                    {(js.totalScore ?? 0).toFixed(2)}
                    <span className="text-sm font-bold" style={{ color: '#757575' }}> / {totalMax}</span>
                  </div>
                </div>
              </div>

              {/* Criteria breakdown */}
              {(js.criteria ?? []).map((c, ci) => (
                <div
                  key={ci}
                  className="grid gap-2 items-center py-2"
                  style={{ gridTemplateColumns: '1fr 90px 100px', borderTop: ci === 0 ? '1px solid #e5e5e5' : 'none' }}
                >
                  <div className="flex items-center gap-2 flex-wrap">
                    <div className="text-xs font-bold" style={{ color: '#000' }}>{c.criteriaName}</div>
                    <span
                      className="text-[10px] font-bold px-1.5 py-0.5"
                      style={{ background: 'rgba(118,185,0,.1)', color: '#5a8d00', borderRadius: 2 }}
                    >
                      trọng số {c.weight}%
                    </span>
                  </div>
                  <div className="text-xs text-center" style={{ color: '#757575' }}>tối đa {c.maxScore}</div>
                  <div className="text-sm font-bold text-right" style={{ color: '#000' }}>
                    {(c.value ?? 0).toFixed(2)} / {c.maxScore}
                  </div>
                </div>
              ))}

              {/* Comment */}
              {js.comment && (
                <div className="mt-3 p-3 text-xs leading-relaxed" style={{ background: '#fff', border: '1px solid #e5e5e5', borderRadius: 2, color: '#333' }}>
                  <div className="text-[10px] tracking-widest mb-1 font-bold uppercase" style={{ color: '#757575' }}>
                    Nhận xét
                  </div>
                  {js.comment}
                </div>
              )}
            </div>
            );
          })
        )}

        {/* Actions */}
        <div className="flex justify-end mt-2">
          <button className="btn btn-outline" onClick={onClose}>Đóng</button>
        </div>

      </div>
    </div>
  );
}
