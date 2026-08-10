'use client';

import React from 'react';

interface CardProps {
  children: React.ReactNode;
  title?: string;
  className?: string;
}

export function Card({ children, title, className = '' }: CardProps) {
  return (
    <div className={`bg-canvas border border-hairline rounded-sm p-4 md:p-6 ${className}`}>
      {title && <h2 className="t-heading-sm text-ink m-0 mb-4 text-base md:text-lg">{title}</h2>}
      {children}
    </div>
  );
}
