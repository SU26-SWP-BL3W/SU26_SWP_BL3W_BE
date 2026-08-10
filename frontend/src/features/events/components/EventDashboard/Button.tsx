'use client';

import React from 'react';

interface ButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: 'primary' | 'secondary' | 'outline' | 'danger' | 'create' | 'view' | 'update' | 'delete';
  size?: 'sm' | 'md' | 'lg';
  children: React.ReactNode;
  isLoading?: boolean;
}

export function Button({
  variant = 'primary',
  size = 'md',
  children,
  isLoading = false,
  disabled = false,
  className = '',
  ...props
}: ButtonProps) {
  const baseClass = 'inline-flex items-center justify-center gap-2 rounded-sm font-bold transition-colors duration-150 cursor-pointer focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary disabled:opacity-60 disabled:cursor-not-allowed min-h-11';

  const variants = {
    primary: 'bg-primary text-on-primary hover:bg-primary-dark active:bg-primary-dark',
    secondary: 'bg-surface-soft text-ink border border-hairline hover:bg-canvas active:bg-hairline',
    outline: 'bg-transparent text-ink border border-hairline hover:bg-surface-soft active:bg-hairline',
    // Nút nguy hiểm (xóa, từ chối): viền đỏ + chữ đỏ; hover đảo thành nền đỏ, chữ trắng.
    danger:
      'bg-transparent text-ink border-2 border-error active:bg-error/15',
    create: 'bg-transparent text-ink border-2 border-primary active:bg-primary/15',
    view: 'bg-primary text-on-primary hover:bg-primary-dark active:bg-primary-dark',
    update: 'bg-primary text-on-primary hover:bg-primary-dark active:bg-primary-dark',
    delete: 'bg-transparent text-ink border-2 border-error active:bg-error/15',
  };

  const sizes = {
    sm: 'px-3 py-2 text-body-sm',
    md: 'px-6 py-3 md:py-2 text-body-strong',
    lg: 'px-8 py-3 text-button-md',
  };

  return (
    <button
      className={`${baseClass} ${variants[variant]} ${sizes[size]} ${className}`}
      disabled={disabled || isLoading}
      {...props}
    >
      {isLoading ? (
        <>
          <div className="w-4 h-4 border-2 border-transparent border-t-current rounded-full animate-spin" />
          {children}
        </>
      ) : (
        children
      )}
    </button>
  );
}
