import { calcScore } from '../utils.jsx';

export default function ScoringPage({ teams, criteria, onEdit, disabled = false }) {
  const totalMax = Math.round(criteria.reduce((sum, c) => sum + Number(c.maxScore || 0), 0) * 100) / 100;
  return (
    <div className="animate-fadeUp">
      <div className="mb-7">
        <h2 className="text-xl font-bold m-0" style={{ color: '#000' }}>Bảng chấm điểm</h2>
        <p className="text-sm mt-1" style={{ color: '#757575' }}>
          Chấm và quản lý điểm số các đội thi · {criteria.length} Bộ tiêu chí
        </p>
      </div>

      <div className="grid gap-4" style={{ gridTemplateColumns: '1fr 1fr' }}>
        {teams.map((t, i) => {
          const score = calcScore(t.scores, criteria);
          return (
            <div key={t.id} className="card-hover p-6"
              style={{ background: '#fff', border: '1px solid #cccccc', borderRadius: 2, animationDelay: `${i * 0.07}s` }}>
              <div className="flex justify-between items-start">
                <div className="text-xs font-bold uppercase tracking-wider" style={{ color: '#757575' }}>{String(t.teamId || t.id).slice(0, 8).toUpperCase()}</div>
                <div className="text-right">
                  <div className="text-3xl font-black leading-none" style={{ color: '#76b900' }}>{score.toFixed(2)}</div>
                  <div className="text-xs" style={{ color: '#757575' }}>/{totalMax} · {criteria.length} Bộ tiêu chí</div>
                </div>
              </div>
              {/* Ẩn danh phía chấm: giám khảo chấm theo mã bài, không thấy tên đội. */}
              <div className="text-sm font-bold mt-1" style={{ color: '#000' }}>Bài nộp #{i + 1}</div>

              <div className="mt-4 flex flex-col gap-2">
                {criteria.slice(0, 3).map((c, ci) => (
                  <div key={ci} className="flex items-center gap-2">
                    <div className="text-xs shrink-0" style={{ color: '#757575', width: 96 }}>{c.label}</div>
                    <div className="flex-1 h-1.5" style={{ background: '#e5e5e5', borderRadius: 2 }}>
                      <div style={{ height: '100%', width: `${c.maxScore > 0 ? ((t.scores[ci] || 0) / c.maxScore) * 100 : 0}%`, background: '#76b900' }} />
                    </div>
                    <div className="text-xs font-bold w-6 text-right" style={{ color: '#000' }}>{t.scores[ci] || 0}</div>
                  </div>
                ))}
                {criteria.length > 3 && (
                  <div className="text-xs" style={{ color: '#757575' }}>+{criteria.length - 3} Bộ tiêu chí khác...</div>
                )}
              </div>

              <div className="flex justify-end mt-4">
                <button className="btn-hover flex items-center gap-2 px-4 py-2 text-xs font-bold"
                  onClick={() => onEdit(t)}
                  disabled={disabled}
                  style={{
                    background: '#f7f7f7',
                    border: '1px solid #cccccc',
                    color: disabled ? '#aaaaaa' : '#000',
                    borderRadius: 2,
                    cursor: disabled ? 'not-allowed' : 'pointer',
                    opacity: disabled ? 0.6 : 1,
                  }}>
                  ≡ Chấm / Sửa
                </button>
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
}
