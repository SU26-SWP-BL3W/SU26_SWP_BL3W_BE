export const INIT_CRITERIA = [
  {
    id: 'C-001', label: 'Innovation', labelVi: 'Tính sáng tạo', weight: 2.5,
    desc: 'Tính mới mẻ, sáng tạo của giải pháp',
    levels: [
      { range: '0–2',  desc: 'Chưa có ý tưởng' },
      { range: '3–4',  desc: 'Ý tưởng cơ bản' },
      { range: '5–7',  desc: 'Ý tưởng tốt, có nét mới' },
      { range: '8–10', desc: 'Rất sáng tạo, giải pháp độc đáo' },
    ],
  },
  {
    id: 'C-002', label: 'Technical Quality', labelVi: 'Chất lượng kỹ thuật', weight: 3,
    desc: 'Chất lượng code, kiến trúc hệ thống',
    levels: [
      { range: '0–2',  desc: 'Code rối, không hoạt động' },
      { range: '3–4',  desc: 'Chạy được nhưng nhiều lỗi' },
      { range: '5–7',  desc: 'Code sạch, xử lý lỗi ổn' },
      { range: '8–10', desc: 'Kiến trúc tốt, tối ưu, scalable' },
    ],
  },
  {
    id: 'C-003', label: 'Business Impact', labelVi: 'Tính ứng dụng', weight: 2,
    desc: 'Tính ứng dụng thực tế, khả năng thương mại hóa',
    levels: [
      { range: '0–2',  desc: 'Không có giá trị thực tế' },
      { range: '3–4',  desc: 'Ứng dụng hạn chế' },
      { range: '5–7',  desc: 'Có thị trường tiềm năng' },
      { range: '8–10', desc: 'Tác động lớn, tiềm năng startup' },
    ],
  },
  {
    id: 'C-004', label: 'Presentation', labelVi: 'Thuyết trình', weight: 1.5,
    desc: 'Chất lượng thuyết trình, demo, slide',
    levels: [
      { range: '0–2',  desc: 'Demo thất bại, slide kém' },
      { range: '3–4',  desc: 'Trình bày sơ sài' },
      { range: '5–7',  desc: 'Demo rõ ràng, slide ổn' },
      { range: '8–10', desc: 'Thuyết phục, demo ấn tượng' },
    ],
  },
  {
    id: 'C-005', label: 'Teamwork', labelVi: 'Làm việc nhóm', weight: 1,
    desc: 'Phối hợp nhóm, quản lý thời gian',
    levels: [
      { range: '0–2',  desc: 'Không có bằng chứng phối hợp' },
      { range: '3–4',  desc: 'Phối hợp kém, trễ deadline' },
      { range: '5–7',  desc: 'Nhóm ổn, đúng tiến độ' },
      { range: '8–10', desc: 'Phối hợp xuất sắc, commit đều đặn' },
    ],
  },
];

export const INIT_TEAMS = [
  { id:'T-001', name:'Team Alpha',      email:'alpha@gmail.com',         scores:[8,7,8,7,8],     comments:['Giải pháp dùng ML độc đáo','Code sạch, DB cần tối ưu','Tiềm năng startup','Demo rõ','Commit đều'] },
  { id:'T-002', name:'Team Beta',       email:'beta@gmail.com',          scores:[8.5,8,8.5,8,9], comments:['','','','',''] },
  { id:'T-003', name:'Nhóm Đại Dương',  email:'daiduong@gmail.com',      scores:[7,6,7,7,8],     comments:['Ý tưởng tốt','Kỹ thuật cần cải thiện','Ứng dụng cao','Slide ổn','Nhóm đoàn kết'] },
  { id:'T-004', name:'Phoenix Coders',  email:'phoenix.coders@gmail.com',scores:[6.5,7,6.5,6,7], comments:['','','','',''] },
  { id:'T-005', name:'Team Epsilon',    email:'epsilon@gmail.com',       scores:[9.2,9,9,9,9],   comments:['','','','',''] },
  { id:'T-006', name:'Team Zeta',       email:'zeta@gmail.com',          scores:[5,5.5,5,5,6],   comments:['','','','',''] },
];

export const INIT_SUBMISSIONS = [
  { id:'S-001', teamId:'T-001', teamName:'Team Alpha',    projectName:'SmartFarm AI',   repo:'https://github.com/team-alpha/smartfarm-ai',   submittedAt:'07/06/2026 14:32', status:'Đã nhận' },
  { id:'S-002', teamId:'T-002', teamName:'Team Beta',     projectName:'EduBot Platform',repo:'https://github.com/team-beta/edubot-platform',  submittedAt:'07/06/2026 15:10', status:'Đã nhận' },
  { id:'S-003', teamId:'T-004', teamName:'Phoenix Coders',projectName:'HealthTrack',    repo:'https://github.com/phoenix-coders/healthtrack', submittedAt:'08/06/2026 09:05', status:'Đã nhận' },
];

export const MY_TEAM_ID = 'T-003';
