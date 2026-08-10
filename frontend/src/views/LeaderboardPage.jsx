import { useState } from 'react';
import { calcScore, getRankColor, getRankBg, getRankIcon } from '../utils.jsx';

export default function LeaderboardPage({ teams, criteria, sortBy, setSortBy, onEdit, onExport }) {
  const [selectedTeamId, setSelectedTeamId] = useState('');

  const sorted = [...teams].sort((a, b) =>
    sortBy === 'score' ? calcScore(b.scores, criteria) - calcScore(a.scores, criteria) :
      sortBy === 'id' ? a.id.localeCompare(b.id) : a.name.localeCompare(b.name)
  );
  const byScore = [...teams].sort((a, b) => calcScore(b.scores, criteria) - calcScore(a.scores, criteria));
  const ranked = sorted.map(t => ({ ...t, rank: byScore.findIndex(x => x.id === t.id) + 1 }));
  const visibleRanked = selectedTeamId ? ranked.filter(t => t.id === selectedTeamId) : ranked;

  return (
    <div className="animate-fadeUp">
      <div className="flex justify-between items-start mb-7">
        <div>
          <h2 className="text-xl font-bold m-0" style={{ color: '#000' }}>Bảng xếp hạng</h2>
          <p className="text-sm mt-1" style={{ color: '#757575' }}>Tổng hợp điểm số & thứ hạng các đội thi.</p>
        </div>
        <div className="flex gap-2.5">
          <select value={selectedTeamId} onChange={e => setSelectedTeamId(e.target.value)} className="input-field" style={{ width: 'auto', height: 44 }}>
            <option value="">Tất cả đội</option>
            {[...teams].sort((a, b) => a.id.localeCompare(b.id)).map(t => (
              <option key={t.id} value={t.id}>{t.id} — {t.name}</option>
            ))}
          </select>
          <select value={sortBy} onChange={e => setSortBy(e.target.value)} className="input-field" style={{ width: 'auto', height: 44 }}>
            <option value="score">Sắp xếp: Điểm số</option>
            <option value="id">Sắp xếp: Mã đội</option>
            <option value="name">Sắp xếp: Tên đội</option>
          </select>
          <button className="btn btn-primary flex items-center gap-2" onClick={onExport}>Xuất báo cáo</button>
        </div>
      </div>

      {/* ── Stat cards ── */}
      <div className="grid gap-4 mb-6" style={{ gridTemplateColumns: 'repeat(3,1fr)' }}>
        {[
          { l: 'Điểm cao nhất', v: Math.max(...teams.map(t => calcScore(t.scores, criteria))).toFixed(2) },
          { l: 'Điểm trung bình', v: (teams.reduce((a, t) => a + calcScore(t.scores, criteria), 0) / teams.length).toFixed(2) },
          { l: 'Tổng đội thi', v: `${teams.length} đội` },
        ].map((s, i) => (
          <div key={i} className="callout-stat">
            <span className="callout-stat__number">{s.v}</span>
            <span className="callout-stat__caption">{s.l}</span>
          </div>
        ))}
      </div>

      {/* ── Table header ── */}
      <div className="grid gap-1.5 px-4 py-2.5 mb-1 text-xs font-bold uppercase tracking-widest"
        style={{ gridTemplateColumns: `56px 1fr ${criteria.map(() => '72px').join(' ')} 84px 110px`, color: '#757575', borderBottom: '2px solid #000' }}>
        <div>HẠNG</div><div>ĐỘI</div>
        {criteria.map((c, i) => <div key={i} className="text-center">{c.label.split(' ')[0].slice(0, 6).toUpperCase()}</div>)}
        <div className="text-center">TỔNG</div><div>HÀNH ĐỘNG</div>
      </div>

      {/* ── Rows ── */}
      {visibleRanked.map((t, i) => {
        const score = calcScore(t.scores, criteria);
        return (
          <div key={t.id} className="card-hover grid gap-1.5 px-4 py-3.5 mb-1 items-center"
            style={{
              gridTemplateColumns: `56px 1fr ${criteria.map(() => '72px').join(' ')} 84px 110px`,
              background: getRankBg(t.rank),
              border: `1px solid ${t.rank <= 3 ? getRankColor(t.rank) + '55' : '#e5e5e5'}`,
              borderRadius: 2,
              animationDelay: `${i * 0.05}s`,
            }}>
            <div className="text-sm font-bold" style={{ color: '#000' }}>{t.rank}</div>
            <div>
              <div className="text-sm font-bold" style={{ color: '#000' }}>{t.name}</div>
              <div className="text-xs" style={{ color: '#757575' }}>{t.id}</div>
            </div>
            {criteria.map((_, ci) => (
              <div key={ci} className="text-center text-sm font-bold" style={{ color: '#1a1a1a' }}>{(t.scores[ci] || 0).toFixed(1)}</div>
            ))}
            <div className="text-center text-xl font-black" style={{ color: '#76b900' }}>{score.toFixed(2)}</div>
            <button className="btn btn-update btn-sm" onClick={() => onEdit(t)}>
              Sửa điểm
            </button>
          </div>
        );
      })}
    </div>
  );
}
