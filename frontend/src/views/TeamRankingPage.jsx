import { calcScore, getRankColor, getRankBg } from '../utils.jsx';

export default function TeamRankingPage({ teams, criteria, myId }) {
  const totalMax = Math.round(criteria.reduce((sum, c) => sum + Number(c.maxScore || 0), 0) * 100) / 100;
  const by     = [...teams].sort((a, b) => calcScore(b.scores, criteria) - calcScore(a.scores, criteria));
  const ranked = by.map((t, i) => ({ ...t, rank: i + 1 }));
  const me     = ranked.find(t => t.id === myId);

  return (
    <div className="animate-fadeUp">
      <div className="mb-6">
        <h2 className="text-xl font-bold m-0" style={{ color: '#000' }}>Xem xếp hạng</h2>
        <p className="text-sm mt-1" style={{ color: '#757575' }}>Thứ hạng của đội bạn và toàn bộ bảng xếp hạng.</p>
      </div>

      {/* ── My team highlight ── */}
      {me && (
        <div className="p-7 mb-7"
          style={{ background: '#fff', border: '2px solid #76b900', borderRadius: 2, borderLeft: '4px solid #76b900' }}>
          <div className="text-xs font-bold uppercase tracking-widest mb-1.5" style={{ color: '#757575' }}>ĐỘI CỦA BẠN</div>
          <div className="flex justify-between items-center">
            <div>
              <div className="text-xl font-bold" style={{ color: '#76b900' }}>{me.name}</div>
              <div className="text-sm mt-0.5" style={{ color: '#757575' }}>{me.id}</div>
              <div className="mt-3 text-2xl font-bold" style={{ color: getRankColor(me.rank) }}>Hạng {me.rank}</div>
            </div>
            <div className="text-right">
              <div className="font-black leading-none" style={{ fontSize: 56, color: '#76b900' }}>
                {calcScore(me.scores, criteria).toFixed(2)}
              </div>
              <div className="text-xs" style={{ color: '#757575' }}>/{totalMax} điểm</div>
            </div>
          </div>
          <div className="grid gap-2.5 mt-6" style={{ gridTemplateColumns: `repeat(${Math.min(criteria.length, 5)}, 1fr)` }}>
            {criteria.map((c, i) => (
              <div key={i} className="p-3" style={{ background: '#f7f7f7', borderRadius: 2 }}>
                <div className="text-xs mb-1" style={{ color: '#757575' }}>{c.label}</div>
                <div className="text-xl font-bold" style={{ color: '#76b900' }}>{me.scores[i] || 0}</div>
                <div className="text-xs" style={{ color: '#757575' }}>tối đa {c.maxScore ?? 0}</div>
                {me.comments?.[i] && (
                  <div className="text-xs mt-1 italic leading-snug" style={{ color: '#757575' }}>&ldquo;{me.comments[i]}&rdquo;</div>
                )}
              </div>
            ))}
          </div>
        </div>
      )}

      <div className="text-sm font-bold mb-3" style={{ color: '#76b900' }}>Bảng xếp hạng toàn đoàn</div>
      {ranked.map((t, i) => (
        <div key={t.id} className="p-4 mb-2 flex items-center gap-4"
          style={{
            background: t.id === myId ? 'rgba(118,185,0,.06)' : getRankBg(t.rank),
            border: `1.5px solid ${t.id === myId ? '#76b900' : t.rank <= 3 ? getRankColor(t.rank) + '55' : '#e5e5e5'}`,
            borderRadius: 2,
            animationDelay: `${i * 0.06}s`,
          }}>
          <div className="w-11 text-center text-sm font-bold" style={{ color: '#000' }}>
            {t.rank}
          </div>
          <div className="flex-1">
            <div className="text-sm font-bold" style={{ color: t.id === myId ? '#76b900' : '#000' }}>
              {t.name}
              {t.id === myId && <span className="badge-accent ml-2">Đội bạn</span>}
            </div>
            <div className="text-xs" style={{ color: '#757575' }}>{t.id}</div>
          </div>
          <div className="text-2xl font-black" style={{ color: getRankColor(t.rank) }}>
            {calcScore(t.scores, criteria).toFixed(2)}
          </div>
        </div>
      ))}
    </div>
  );
}
