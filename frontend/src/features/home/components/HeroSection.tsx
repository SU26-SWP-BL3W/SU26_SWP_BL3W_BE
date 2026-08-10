'use client';

import React from 'react';
import Link from 'next/link';
import { Sparkles } from 'lucide-react';
import { useCurrentUser } from '@/hooks/useAuth';

export function HeroSection() {
  const { data: user } = useCurrentUser();
  const isLoggedIn = !!user?.id;

  const scrollToEvents = (e: React.MouseEvent<HTMLButtonElement>) => {
    e.preventDefault();
    const element = document.getElementById('events-section');
    if (element) {
      element.scrollIntoView({ behavior: 'smooth' });
    }
  };

  return (
    <>
      <style>{`
        .hero-root {
          position: relative;
          background: var(--color-surface-dark);
          color: var(--color-on-dark);
          padding: 80px 24px;
          display: flex;
          flex-direction: column;
          align-items: center;
          justify-content: center;
          text-align: center;
          min-height: 70vh;
          overflow: hidden;
          border-bottom: 1px solid var(--color-hairline-strong);
        }

        /* Glow grid background */
        .hero-root::before {
          content: "";
          position: absolute;
          inset: 0;
          background-image: radial-gradient(
            circle,
            rgba(118, 185, 0, 0.15) 1px,
            transparent 1px
          );
          background-size: 32px 32px;
          pointer-events: none;
          z-index: 0;
        }

        /* Large neon glow blob */
        .hero-glow-blob {
          position: absolute;
          width: 400px;
          height: 400px;
          background: radial-gradient(circle, rgba(118, 185, 0, 0.08) 0%, transparent 70%);
          top: 50%;
          left: 50%;
          transform: translate(-50%, -50%);
          pointer-events: none;
          z-index: 0;
        }

        .hero-content {
          position: relative;
          z-index: 1;
          max-width: 800px;
          display: flex;
          flex-direction: column;
          align-items: center;
          gap: var(--space-lg);
        }

        .hero-tag {
          display: inline-flex;
          align-items: center;
          gap: 6px;
          font-size: var(--fs-caption-xs);
          font-weight: 700;
          letter-spacing: 0.15em;
          text-transform: uppercase;
          color: var(--color-primary);
          background: rgba(118, 185, 0, 0.1);
          padding: 6px 14px;
          border: 1px solid rgba(118, 185, 0, 0.3);
          border-radius: var(--radius-sm);
        }

        .hero-title {
          font-size: var(--fs-display-xl);
          font-weight: 700;
          line-height: 1.1;
          color: var(--color-on-dark);
          margin: 0;
          text-transform: uppercase;
          letter-spacing: -0.02em;
        }

        .hero-title span {
          color: var(--color-primary);
          text-shadow: 0 0 20px rgba(118, 185, 0, 0.35);
        }

        .hero-desc {
          font-size: var(--fs-body-md);
          line-height: 1.6;
          color: var(--color-on-dark-mute);
          margin: 0;
          max-width: 620px;
        }

        .hero-actions {
          display: flex;
          gap: var(--space-md);
          margin-top: var(--space-sm);
          flex-wrap: wrap;
          justify-content: center;
        }

        .hero-btn-primary {
          display: inline-flex;
          align-items: center;
          gap: 8px;
          background: var(--color-primary);
          color: var(--color-on-primary);
          padding: 12px 28px;
          border-radius: var(--radius-sm);
          font-size: var(--fs-button-md);
          font-weight: 700;
          text-transform: uppercase;
          letter-spacing: 0.05em;
          text-decoration: none;
          transition: background 150ms ease;
          border: none;
          cursor: pointer;
        }

        .hero-btn-primary:hover {
          background: var(--color-primary-dark);
        }

        .hero-btn-secondary {
          background: transparent;
          color: var(--color-on-dark);
          border: 1px solid var(--color-hairline-strong);
          padding: 12px 28px;
          border-radius: var(--radius-sm);
          font-size: var(--fs-button-md);
          font-weight: 700;
          text-transform: uppercase;
          letter-spacing: 0.05em;
          text-decoration: none;
          transition: all 150ms ease;
          cursor: pointer;
        }

        .hero-btn-secondary:hover {
          background: rgba(255, 255, 255, 0.05);
          border-color: var(--color-on-dark);
        }

        @media (max-width: 768px) {
          .hero-title {
            font-size: 32px;
          }
          .hero-desc {
            font-size: var(--fs-body-sm);
          }
          .hero-btn-primary, .hero-btn-secondary {
            padding: 10px 20px;
            font-size: var(--fs-button-sm);
            width: 100%;
            justify-content: center;
          }
          .hero-actions {
            width: 100%;
            flex-direction: column;
            gap: var(--space-sm);
          }
        }
      `}</style>

      <section className="hero-root">
        <div className="hero-glow-blob" />

        <div className="hero-content">
          <div className="hero-tag">
            <Sparkles size={12} />
            SEAL HACKATHON
          </div>

          <h1 className="hero-title">
            Nơi ý tưởng công nghệ<br />
            <span>Bứt Phá Giới Hạn</span>
          </h1>

          <p className="hero-desc">
            Sân chơi lập trình đỉnh cao dành cho sinh viên FPT University. Tham gia tranh tài, xây dựng những dự án đột phá, nhận hỗ trợ từ các cố vấn hàng đầu và khẳng định bản thân.
          </p>

          <div className="hero-actions">
            <button className="hero-btn-primary" onClick={scrollToEvents}>
              Khám phá sự kiện
            </button>

            {!isLoggedIn && (
              <Link href="/auth" className="hero-btn-secondary">
                Đăng ký tham gia
              </Link>
            )}
          </div>
        </div>
      </section>
    </>
  );
}
