'use client';

import React from 'react';

export function CardSkeleton() {
  return (
    <div className="bg-canvas border border-hairline rounded-sm p-6">
      <div className="h-6 bg-surface-soft rounded-sm mb-4 w-1/3 animate-pulse" />
      <div className="space-y-3">
        <div className="h-4 bg-surface-soft rounded-sm w-full animate-pulse" />
        <div className="h-4 bg-surface-soft rounded-sm w-5/6 animate-pulse" />
        <div className="h-4 bg-surface-soft rounded-sm w-4/6 animate-pulse" />
      </div>
    </div>
  );
}

export function TableRowSkeleton() {
  return (
    <div className="py-3 border-b border-hairline space-y-2">
      <div className="flex justify-between">
        <div className="h-4 bg-surface-soft rounded-sm w-1/3 animate-pulse" />
        <div className="h-4 bg-surface-soft rounded-sm w-1/4 animate-pulse" />
      </div>
    </div>
  );
}

export function FormSkeleton() {
  return (
    <div className="space-y-4">
      <div className="h-10 bg-surface-soft rounded-sm animate-pulse" />
      <div className="h-32 bg-surface-soft rounded-sm animate-pulse" />
      <div className="h-10 bg-surface-soft rounded-sm animate-pulse" />
    </div>
  );
}
