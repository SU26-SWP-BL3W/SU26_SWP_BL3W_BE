'use client';

import React from 'react';
import { ShieldCheck, Zap, Users } from 'lucide-react';

const FEATURES = [
  {
    icon: Zap,
    title: 'Cập nhật thời gian thực',
    desc: 'Bảng xếp hạng (leaderboard) và tiến độ các vòng thi được cập nhật liên tục, tạo không khí thi đấu sôi động.',
  },
  {
    icon: ShieldCheck,
    title: 'Đánh giá minh bạch',
    desc: 'Thang điểm chi tiết theo các bộ tiêu chí rõ ràng từ hội đồng giám khảo, hiển thị trực quan và công bằng.',
  },
  {
    icon: Users,
    title: 'Hỗ trợ đội nhóm tối đa',
    desc: 'Tìm kiếm đồng đội, quản lý nhóm và gửi sản phẩm dự thi (ZIP/URL) dễ dàng chỉ trong vài cú click chuột.',
  },
];

export function FeaturesSection() {
  return (
    <>
      <style>{`
        .features-root {
          background: var(--color-canvas);
          padding: 64px 24px;
          border-bottom: 1px solid var(--color-hairline);
        }

        .features-container {
          max-width: var(--container-max);
          margin: 0 auto;
          display: flex;
          flex-direction: column;
          gap: var(--space-xxl);
        }

        .features-header {
          text-align: center;
          max-width: 600px;
          margin: 0 auto;
          display: flex;
          flex-direction: column;
          gap: var(--space-sm);
        }

        .features-header h2 {
          font-size: var(--fs-display-lg);
          font-weight: 700;
          color: var(--color-ink);
          margin: 0;
          text-transform: uppercase;
        }

        .features-header p {
          font-size: var(--fs-body-sm);
          color: var(--color-mute);
          margin: 0;
        }

        .features-grid {
          display: grid;
          grid-template-columns: repeat(3, 1fr);
          gap: var(--space-xl);
        }

        .feature-card {
          position: relative;
          background: var(--color-surface-soft);
          border: 1px solid var(--color-hairline);
          padding: var(--space-xl);
          display: flex;
          flex-direction: column;
          gap: var(--space-md);
          transition: all 150ms ease;
        }

        .feature-card:hover {
          border-color: var(--color-primary);
          transform: translateY(-2px);
          background: #ffffff;
          box-shadow: 0 4px 20px rgba(118, 185, 0, 0.08);
        }

        /* NVIDIA-style corner square indicator */
        .feature-card::before {
          content: "";
          position: absolute;
          top: -1px;
          left: -1px;
          width: 8px;
          height: 8px;
          background: var(--color-primary);
          opacity: 0;
          transition: opacity 150ms ease;
        }

        .feature-card:hover::before {
          opacity: 1;
        }

        .feature-icon-wrapper {
          width: 44px;
          height: 44px;
          display: flex;
          align-items: center;
          justify-content: center;
          background: rgba(118, 185, 0, 0.1);
          color: var(--color-primary);
          border-radius: var(--radius-sm);
        }

        .feature-card h3 {
          font-size: var(--fs-heading-sm);
          font-weight: 700;
          color: var(--color-ink);
          margin: 0;
        }

        .feature-card p {
          font-size: var(--fs-caption-md);
          color: var(--color-mute);
          line-height: 1.6;
          margin: 0;
        }

        @media (max-width: 992px) {
          .features-grid {
            grid-template-columns: 1fr;
            gap: var(--space-md);
          }
          .features-header h2 {
            font-size: var(--fs-heading-xl);
          }
        }
      `}</style>

      <section className="features-root">
        <div className="features-container">
          <div className="features-header">
            <h2>Nền tảng thi đấu chuyên nghiệp</h2>
            <p>Trải nghiệm môi trường Hackathon chuẩn mực, công khai và kịch tính đến từng giây phút.</p>
          </div>

          <div className="features-grid">
            {FEATURES.map((item, index) => {
              const Icon = item.icon;
              return (
                <div key={index} className="feature-card">
                  <div className="feature-icon-wrapper">
                    <Icon size={20} />
                  </div>
                  <h3>{item.title}</h3>
                  <p>{item.desc}</p>
                </div>
              );
            })}
          </div>
        </div>
      </section>
    </>
  );
}
