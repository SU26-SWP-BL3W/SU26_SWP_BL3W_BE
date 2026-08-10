import { calcScore } from '../utils.jsx';

export default function ExportModal({ ranked, criteria, onClose, sn }) {
  const doCSV = () => {
    const header = ['Hạng','Mã đội','Tên đội',...criteria.map(c=>c.label),'Tổng'];
    const rows   = ranked.map(t => [t.rank,t.id,t.name,...(t.scores||[]).map(s=>(s||0).toFixed(2)),calcScore(t.scores,criteria).toFixed(2)]);
    const csv    = [header,...rows].map(r=>r.join(',')).join('\n');
    const a = document.createElement('a');
    a.href     = URL.createObjectURL(new Blob(['﻿'+csv],{type:'text/csv;charset=utf-8;'}));
    a.download = 'bang_xep_hang.csv';
    a.click();
    sn('Đã xuất báo cáo CSV!'); onClose();
  };
  return (
    <div className="modal-overlay items-center" onClick={onClose} style={{ padding:0 }}>
      <div className="modal-box" style={{ maxWidth:400 }} onClick={e=>e.stopPropagation()}>
        <div className="text-lg font-bold mb-1" style={{ color:'#3ddc84' }}>Xuất báo cáo</div>
        <div className="text-sm mb-6" style={{ color:'#7da88a' }}>Chọn định dạng xuất bảng xếp hạng</div>
        <button className="btn-hover w-full p-4 flex items-center gap-3 text-left text-sm font-bold mb-3"
          style={{ background:'rgba(61,220,132,.12)', border:'1.5px solid #3ddc84', color:'#3ddc84' }} onClick={doCSV}>
          <div><div>Xuất CSV</div><div className="text-[11px] font-normal" style={{ color:'#7da88a' }}>Mở bằng Excel, Google Sheets</div></div>
        </button>
        <button className="btn-ghost w-full mt-1" onClick={onClose}>Hủy</button>
      </div>
    </div>
  );
}
