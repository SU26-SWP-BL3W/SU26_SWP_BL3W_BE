import type { CSSProperties, ReactNode } from 'react';

export type StatusTone = 'success' | 'pending' | 'processing' | 'danger' | 'neutral' | 'inactive' | 'upcoming';

export function StatusBadge({
  children,
  tone = 'neutral',
  className = '',
  style,
}: {
  children: ReactNode;
  tone?: StatusTone;
  className?: string;
  style?: CSSProperties;
}) {
  return <span className={`status-label status-${tone} ${className}`.trim()} style={style}>{children}</span>;
}
