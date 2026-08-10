'use client';

import React from 'react';
import { Trophy, Users, Award, Briefcase } from 'lucide-react';

const STATS = [
  {
    icon: Users,
    value: '500+',
    label: 'Thí sinh đăng ký',
  },
  {
    icon: Trophy,
    value: '12+',
    label: 'Cuộc thi Hackathon',
  },
  {
    icon: Briefcase,
    value: '30+',
    label: 'Cố vấn & Giám khảo',
  },
  {
    icon: Award,
    value: '100M+',
    label: 'Tổng giá trị giải thưởng',
  },
];

export function StatsSection() {
  return (
    <>
      <style>{`
        .stats-root {
          background: var(--color-surface-dark);
          color: var(--color-on-dark);
          padding: 56px 24px;
          border-bottom: 1px solid var(--color-hairline-strong);
        }

        .stats-container {
          max-width: var(--container-max);
          margin: 0 auto;
          display: grid;
          grid-template-columns: repeat(4, 1fr);
          gap: var(--space-xl);
        }

        .stat-item {
          display: flex;
          align-items: center;
          gap: var(--space-lg);
          padding: var(--space-md) var(--space-lg);
          border-left: 1px solid var(--color-hairline-strong);
        }

        .stat-item:first-child {
          border-left: none;
        }

        .stat-icon {
          color: var(--color-primary);
          flex-shrink: 0;
        }

        .stat-info {
          display: flex;
          flex-direction: column;
          gap: 2px;
        }

        .stat-num {
          font-size: var(--fs-display-lg);
          font-weight: 700;
          color: var(--color-primary);
          line-height: 1;
        }

        .stat-lbl {
          font-size: var(--fs-caption-sm);
          color: var(--color-on-dark-mute);
          font-weight: 700;
          text-transform: uppercase;
          letter-spacing: 0.05em;
        }

        @media (max-width: 992px) {
          .stats-container {
            grid-template-columns: repeat(2, 1fr);
            gap: var(--space-lg);
          }
          .stat-item {
            border-left: none;
            padding: var(--space-sm) 0;
          }
        }

        @media (max-width: 576px) {
          .stats-container {
            grid-template-columns: 1fr;
            gap: var(--space-md);
          }
        }
      `}</style>

      <section className="stats-root">
        <div className="stats-container">
          {STATS.map((item, index) => {
            const Icon = item.icon;
            return (
              <div key={index} className="stat-item">
                <div className="stat-icon">
                  <Icon size={28} />
                </div>
                <div className="stat-info">
                  <span className="stat-num">{item.value}</span>
                  <span className="stat-lbl">{item.label}</span>
                </div>
              </div>
            );
          })}
        </div>
      </section>
    </>
  );
}
