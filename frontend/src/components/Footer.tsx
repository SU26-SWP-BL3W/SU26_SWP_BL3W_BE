'use client';

import React from 'react';
import Link from 'next/link';

export function Footer() {
  return (
    <>
      <style>{`
        .footer-root {
          background: var(--color-surface-dark);
          color: var(--color-on-dark);
          border-top: 1px solid var(--color-hairline-strong);
          padding: 48px 24px 24px 24px;
          font-family: var(--font-sans);
        }

        .footer-container {
          max-width: var(--container-max);
          margin: 0 auto;
          display: flex;
          flex-direction: column;
          gap: 40px;
        }

        .footer-top {
          display: flex;
          justify-content: space-between;
          flex-wrap: wrap;
          gap: 32px;
        }

        .footer-brand {
          max-width: 320px;
          display: flex;
          flex-direction: column;
          gap: 12px;
        }

        .footer-brand-title {
          font-size: var(--fs-heading-sm);
          font-weight: 700;
          color: var(--color-primary);
          text-transform: uppercase;
          letter-spacing: 0.1em;
          margin: 0;
        }

        .footer-brand-desc {
          font-size: var(--fs-caption-md);
          color: var(--color-on-dark-mute);
          line-height: 1.6;
          margin: 0;
        }

        .footer-links-group {
          display: flex;
          gap: 64px;
          flex-wrap: wrap;
        }

        .footer-links-col {
          display: flex;
          flex-direction: column;
          gap: 14px;
        }

        .footer-links-title {
          font-size: var(--fs-caption-xs);
          font-weight: 700;
          text-transform: uppercase;
          letter-spacing: 0.1em;
          color: var(--color-on-dark);
          margin: 0;
        }

        .footer-list {
          list-style: none;
          padding: 0;
          margin: 0;
          display: flex;
          flex-direction: column;
          gap: 10px;
        }

        .footer-link {
          font-size: var(--fs-caption-md);
          color: var(--color-on-dark-mute);
          text-decoration: none;
          transition: color 150ms ease;
        }

        .footer-link:hover {
          color: var(--color-primary);
        }

        .footer-bottom {
          border-top: 1px solid rgba(255, 255, 255, 0.1);
          padding-top: 24px;
          display: flex;
          justify-content: space-between;
          align-items: center;
          flex-wrap: wrap;
          gap: 16px;
        }

        .footer-copy {
          font-size: var(--fs-caption-sm);
          color: var(--color-on-dark-mute);
          margin: 0;
        }

        .footer-socials {
          display: flex;
          gap: var(--space-md);
        }

        @media (max-width: 768px) {
          .footer-top {
            flex-direction: column;
            gap: 40px;
          }
          .footer-links-group {
            gap: 40px;
            width: 100%;
            justify-content: space-between;
          }
          .footer-bottom {
            flex-direction: column;
            align-items: flex-start;
          }
        }
      `}</style>

      <footer className="footer-root">
        <div className="footer-container">
          <div className="footer-top">
            <div className="footer-brand">
              <h3 className="footer-brand-title">FPT University Hackathon</h3>
              <p className="footer-brand-desc">
                Nền tảng quản lý cuộc thi hackathon hàng đầu dành cho sinh viên FPT. Tối ưu hóa quy trình, nâng cao trải nghiệm thi đấu.
              </p>
            </div>

            <div className="footer-links-group">
              <div className="footer-links-col">
                <h4 className="footer-links-title font-bold">Liên kết</h4>
                <ul className="footer-list">
                  <li>
                    <Link href="/" className="footer-link">Trang chủ</Link>
                  </li>
                  <li>
                    <Link href="/criteria" className="footer-link">Tiêu chí chấm</Link>
                  </li>
                </ul>
              </div>

              <div className="footer-links-col">
                <h4 className="footer-links-title font-bold">Hệ thống</h4>
                <ul className="footer-list">
                  <li>
                    <Link href="/auth" className="footer-link">Đăng nhập</Link>
                  </li>
                  <li>
                    <Link href="/auth" className="footer-link">Tạo tài khoản</Link>
                  </li>
                </ul>
              </div>
            </div>
          </div>

          <div className="footer-bottom">
            <p className="footer-copy">
              &copy; {new Date().getFullYear()} FPT University Hackathon Portal. All rights reserved.
            </p>
            <div className="footer-socials">
              <span style={{ fontSize: 'var(--fs-caption-sm)', color: 'var(--color-on-dark-mute)' }}>
                Powered by SWP SE1907 Team
              </span>
            </div>
          </div>
        </div>
      </footer>
    </>
  );
}
