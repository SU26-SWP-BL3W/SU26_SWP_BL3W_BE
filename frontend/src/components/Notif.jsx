export default function Notif({ n }) {
  if (!n) return null;
  return (
    <div className="animate-slideDown" style={{
      position:'fixed', top:20, right:20, zIndex:9999,
      background: n.t==='e' ? '#ff4d6d' : '#3ddc84',
      color:'#000', padding:'12px 20px', fontWeight:700, fontSize:13,
      fontFamily:'Inter,sans-serif', boxShadow:'0 4px 24px rgba(0,0,0,.5)'
    }}>
      {n.m}
    </div>
  );
}
