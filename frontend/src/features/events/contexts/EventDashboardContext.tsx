'use client';

import React, { createContext, useContext, useState } from 'react';
import type { EventTabId } from '@/lib/events/getEventTabs';

interface EventDashboardContextType {
  activeTab: EventTabId;
  setActiveTab: (tab: EventTabId) => void;
  isModalOpen: boolean;
  setIsModalOpen: (open: boolean) => void;
  isLoading: boolean;
  setIsLoading: (loading: boolean) => void;
}

const EventDashboardContext = createContext<EventDashboardContextType | undefined>(undefined);

export function EventDashboardProvider({ children }: { children: React.ReactNode }) {
  const [activeTab, setActiveTab] = useState<EventTabId>('detail');
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [isLoading, setIsLoading] = useState(false);

  return (
    <EventDashboardContext.Provider value={{ activeTab, setActiveTab, isModalOpen, setIsModalOpen, isLoading, setIsLoading }}>
      {children}
    </EventDashboardContext.Provider>
  );
}

export function useEventDashboard() {
  const context = useContext(EventDashboardContext);
  if (!context) throw new Error('useEventDashboard must be used within EventDashboardProvider');
  return context;
}
