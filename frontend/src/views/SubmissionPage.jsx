import { StatusBadge } from '../components/StatusBadge';

export default function SubmissionPage({ submissions, teams }) {
  const done    = new Set(submissions.map(s => s.teamId));
  const total   = teams.length || submissions.length || 1;
  const pct     = Math.round((submissions.length / total) * 100);
  const hasTeams = teams.length > 0;

  return (
    <div className="animate-fadeUp">
      <div className="flex justify-between items-start mb-7">
        <div>
          <h2 className="text-xl font-bold m-0" style={{ color: '#000' }}>Danh sách bài nộp</h2>
          <p className="text-sm mt-1" style={{ color: '#757575' }}>Xem bài nộp từ các đội thi.</p>
        </div>
        <div className="badge-accent px-4 py-2 text-sm">
          {hasTeams ? `${submissions.length}/${teams.length} đội đã nộp` : `${submissions.length} bài nộp`}
        </div>
      </div>

      {hasTeams && (
        <div className="p-5 mb-6" style={{ background: '#f7f7f7', border: '1px solid #cccccc', borderRadius: 2 }}>
          <div className="flex justify-between mb-2.5">
            <span className="text-sm" style={{ color: '#757575' }}>Tiến độ nộp bài</span>
            <span className="text-sm font-bold" style={{ color: '#76b900' }}>{pct}%</span>
          </div>
          <div className="h-2 overflow-hidden" style={{ background: '#e5e5e5', borderRadius: 2 }}>
            <div style={{ height: '100%', width: `${pct}%`, background: '#76b900', transition: 'width .5s' }} />
          </div>
        </div>
      )}

      {submissions.map((s, i) => (
        <div key={s.id} className="card-hover p-5 mb-2.5"
          style={{ background: '#fff', border: '1px solid #cccccc', borderRadius: 2, animationDelay: `${i * 0.06}s` }}>
          <div className="flex items-center gap-2.5 mb-3">
            {/* Ẩn danh phía chấm: giám khảo chỉ thấy mã bài, không thấy đội/thí sinh nào. */}
            <span className="text-sm font-bold" style={{ color: '#000' }}>Bài nộp #{i + 1}</span>
            <StatusBadge tone="success">{s.status}</StatusBadge>
          </div>
          <div className="grid gap-2.5 mb-2.5" style={{ gridTemplateColumns: '1fr 1fr' }}>
            {[['ID NHÓM', s.projectName], ['THỜI GIAN NỘP', s.submittedAt]].map(([k, v]) => (
              <div key={k} className="p-3" style={{ background: '#f7f7f7', borderRadius: 2 }}>
                <div className="text-[10px] tracking-widest mb-1 font-bold uppercase" style={{ color: '#757575' }}>{k}</div>
                <div className="text-sm font-bold" style={{ color: '#000' }}>{v}</div>
              </div>
            ))}
          </div>
          {/* Bài nộp nhiều link (form động): render từng link theo nhãn.
              s.links do SubmissionsPanel parse sẵn; không có thì fallback 1 link repo như cũ. */}
          {(s.links && s.links.length > 0 ? s.links : [{ label: 'REPO', url: s.repo }]).map((lnk, li) => (
            <div key={li} className="p-3 flex items-center gap-2.5 mb-1" style={{ background: '#f7f7f7', borderRadius: 2 }}>
              <div className="text-[10px] tracking-widest shrink-0 font-bold uppercase" style={{ color: '#757575', minWidth: 50 }}>
                {lnk.label}
              </div>
              <a href={lnk.url} target="_blank" rel="noreferrer" className="text-xs break-all"
                style={{ color: '#76b900', textDecoration: 'none' }}>{lnk.url}</a>
            </div>
          ))}
        </div>
      ))}

      {teams.filter(t => !done.has(t.id)).length > 0 && (
        <div className="mt-5">
          <div className="text-xs font-bold uppercase tracking-widest mb-2.5" style={{ color: '#757575' }}>CHƯA NỘP BÀI</div>
          {teams.filter(t => !done.has(t.id)).map(t => (
            <div key={t.id} className="p-4 mb-2 flex items-center gap-3.5"
              style={{ background: '#f7f7f7', border: '1px solid #e5e5e5', opacity: .65, borderRadius: 2 }}>
              <span className="text-xs font-bold" style={{ color: '#757575' }}>{t.id}</span>
              <span className="text-sm" style={{ color: '#000' }}>{t.name}</span>
              <StatusBadge tone="neutral" className="ml-auto">Chưa nộp</StatusBadge>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
