# Event Dashboard Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Complete the event dashboard system with API integration, form validation, error handling, testing, and responsive refinement to ship production-ready features for team competition management.

**Architecture:** Three-layer approach:
1. **UI Components** (✅ Complete) — Sidebar navigation, modals, tabs, forms with monogreen styling
2. **API Integration** (In Progress) — Hook dashboard components to real event/team/submission endpoints
3. **Testing & Refinement** (Next) — Unit/integration tests, responsive edge cases, accessibility audit, error states

**Tech Stack:** Next.js 16 (React 19), Tailwind CSS 4, Lucide React icons, TypeScript, TanStack Query, Monogreen design tokens

---

## Phase 1: API Integration & Data Fetching

### Task 1: Create Event API Service

**Files:**
- Create: `src/features/events/api/eventService.ts`
- Modify: `src/features/events/api/events.ts` (existing, update if needed)

**Context:** The mock data hook (useMockEventData) currently returns hardcoded data. Replace with real API calls using TanStack Query.

- [ ] **Step 1: Create eventService.ts with API endpoints**

```typescript
// src/features/events/api/eventService.ts
import axios from 'axios';

const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:8000/api';

export interface Event {
  id: string;
  title: string;
  startDate: string; // ISO 8601
  endDate: string;
  status: 'open' | 'closed';
  submissionType: 'ZIP' | 'URL' | 'Both';
  description: string;
}

export interface Team {
  id: string;
  name: string;
  leader: { id: string; name: string; email: string };
  members: { id: string; name: string; email: string }[];
  eventId: string;
}

export interface Submission {
  id: string;
  teamId: string;
  eventId: string;
  type: 'ZIP' | 'URL';
  status: 'submitted' | 'pending-review' | 'rejected' | 'graded';
  submitDate: string;
  content: string | File; // URL or file path
}

export interface ScoreBreakdown {
  criterion: string;
  score: number;
  max: number;
  status: 'graded' | 'pending';
}

export const eventService = {
  // Get single event details
  getEvent: async (eventId: string): Promise<Event> => {
    const response = await axios.get(`${API_BASE_URL}/events/${eventId}`);
    return response.data;
  },

  // Get all teams for an event
  getEventTeams: async (eventId: string): Promise<Team[]> => {
    const response = await axios.get(`${API_BASE_URL}/events/${eventId}/teams`);
    return response.data;
  },

  // Get user's team for event (if exists)
  getUserTeam: async (eventId: string, userId: string): Promise<Team | null> => {
    const response = await axios.get(`${API_BASE_URL}/events/${eventId}/teams/user/${userId}`);
    return response.data || null;
  },

  // Create new team
  createTeam: async (eventId: string, teamData: { name: string; memberIds: string[] }): Promise<Team> => {
    const response = await axios.post(`${API_BASE_URL}/events/${eventId}/teams`, teamData);
    return response.data;
  },

  // Get team submissions
  getTeamSubmissions: async (teamId: string): Promise<Submission[]> => {
    const response = await axios.get(`${API_BASE_URL}/teams/${teamId}/submissions`);
    return response.data;
  },

  // Submit work (ZIP or URL)
  submitWork: async (teamId: string, eventId: string, submissionData: { type: 'ZIP' | 'URL'; content: string | FormData }): Promise<Submission> => {
    const formData = new FormData();
    formData.append('type', submissionData.type);

    if (submissionData.type === 'ZIP' && submissionData.content instanceof FormData) {
      formData.append('file', submissionData.content.get('file'));
    } else {
      formData.append('url', submissionData.content as string);
    }

    const response = await axios.post(`${API_BASE_URL}/teams/${teamId}/submissions`, formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });
    return response.data;
  },

  // Get team score & leaderboard
  getTeamScores: async (eventId: string): Promise<{ teamId: string; score: number; status: string }[]> => {
    const response = await axios.get(`${API_BASE_URL}/events/${eventId}/scores`);
    return response.data;
  },

  // Get score breakdown for a team
  getScoreBreakdown: async (teamId: string, eventId: string): Promise<ScoreBreakdown[]> => {
    const response = await axios.get(`${API_BASE_URL}/teams/${teamId}/scores?eventId=${eventId}`);
    return response.data;
  },
};

export default eventService;
```

- [ ] **Step 2: Create useEvents.ts hook with TanStack Query**

```typescript
// src/features/events/hooks/useEvents.ts
'use client';

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import eventService, { Event, Team, Submission, ScoreBreakdown } from '../api/eventService';

export const useEvent = (eventId: string) => {
  return useQuery({
    queryKey: ['event', eventId],
    queryFn: () => eventService.getEvent(eventId),
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
};

export const useEventTeams = (eventId: string) => {
  return useQuery({
    queryKey: ['eventTeams', eventId],
    queryFn: () => eventService.getEventTeams(eventId),
    staleTime: 2 * 60 * 1000, // 2 minutes
  });
};

export const useUserTeam = (eventId: string, userId: string) => {
  return useQuery({
    queryKey: ['userTeam', eventId, userId],
    queryFn: () => eventService.getUserTeam(eventId, userId),
    staleTime: 5 * 60 * 1000,
  });
};

export const useCreateTeam = (eventId: string) => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (teamData: { name: string; memberIds: string[] }) =>
      eventService.createTeam(eventId, teamData),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['eventTeams', eventId] });
      queryClient.invalidateQueries({ queryKey: ['userTeam', eventId] });
    },
  });
};

export const useTeamSubmissions = (teamId: string) => {
  return useQuery({
    queryKey: ['submissions', teamId],
    queryFn: () => eventService.getTeamSubmissions(teamId),
    staleTime: 1 * 60 * 1000, // 1 minute
  });
};

export const useSubmitWork = (teamId: string, eventId: string) => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (submissionData: { type: 'ZIP' | 'URL'; content: string | FormData }) =>
      eventService.submitWork(teamId, eventId, submissionData),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['submissions', teamId] });
      queryClient.invalidateQueries({ queryKey: ['scores', eventId] });
    },
  });
};

export const useTeamScores = (eventId: string) => {
  return useQuery({
    queryKey: ['scores', eventId],
    queryFn: () => eventService.getTeamScores(eventId),
    staleTime: 2 * 60 * 1000,
  });
};

export const useScoreBreakdown = (teamId: string, eventId: string) => {
  return useQuery({
    queryKey: ['scoreBreakdown', teamId, eventId],
    queryFn: () => eventService.getScoreBreakdown(teamId, eventId),
    staleTime: 5 * 60 * 1000,
  });
};
```

- [ ] **Step 3: Verify existing events.ts is compatible**

Check `src/features/events/api/events.ts` to ensure it doesn't conflict with eventService.ts. If it has overlapping functionality, consolidate or remove.

Run: `grep -n "export" src/features/events/api/events.ts`

- [ ] **Step 4: Commit**

```bash
git add src/features/events/api/eventService.ts src/features/events/hooks/useEvents.ts
git commit -m "feat: add event API service and TanStack Query hooks for data fetching"
```

---

### Task 2: Update Dashboard Tab to Use Real API Data

**Files:**
- Modify: `src/features/events/components/EventDashboard/tabs/Dashboard.tsx`
- Modify: `src/features/events/components/EventDashboard/EventDashboard.tsx`

**Context:** Replace mock data (useMockEventData) with real API queries. The dashboard needs eventId and userId from route params or context.

- [ ] **Step 1: Update EventDashboard.tsx to accept eventId and userId props**

```typescript
// src/features/events/components/EventDashboard/EventDashboard.tsx
'use client';

import React from 'react';
import { Sidebar } from './Sidebar';
import { Header } from './Header';
import { Modal } from './Modal';
import { DashboardTab } from './tabs/Dashboard';
import { SubmissionTab } from './tabs/Submission';
import { ResultsTab } from './tabs/Results';
import { TeamRegistrationForm } from './TeamRegistrationForm';
import { useEventDashboard } from '@/features/events/contexts/EventDashboardContext';
import { useEvent, useUserTeam, useEventTeams } from '@/features/events/hooks/useEvents';

interface EventDashboardProps {
  eventId: string;
  userId: string;
}

export function EventDashboard({ eventId, userId }: EventDashboardProps) {
  const { activeTab, isModalOpen, setIsModalOpen } = useEventDashboard();

  // Fetch real data
  const { data: event, isLoading: eventLoading } = useEvent(eventId);
  const { data: userTeam, isLoading: userTeamLoading } = useUserTeam(eventId, userId);
  const { data: teams, isLoading: teamsLoading } = useEventTeams(eventId);

  const isLoading = eventLoading || userTeamLoading || teamsLoading;

  if (isLoading) {
    return (
      <div className="min-h-screen bg-canvas flex items-center justify-center">
        <div className="text-center">
          <div className="w-8 h-8 border-4 border-primary border-t-transparent rounded-full animate-spin mx-auto mb-4" />
          <p className="t-body-md text-mute">Loading event...</p>
        </div>
      </div>
    );
  }

  if (!event) {
    return (
      <div className="min-h-screen bg-canvas flex items-center justify-center">
        <p className="t-heading-md text-error">Event not found</p>
      </div>
    );
  }

  const renderTabContent = () => {
    switch (activeTab) {
      case 'dashboard':
        return <DashboardTab eventId={eventId} userId={userId} />;
      case 'submission':
        return userTeam ? <SubmissionTab teamId={userTeam.id} eventId={eventId} /> : <div className="t-body-md text-mute p-6">Please register a team first</div>;
      case 'results':
        return userTeam ? <ResultsTab teamId={userTeam.id} eventId={eventId} /> : <div className="t-body-md text-mute p-6">Results available after submission</div>;
      default:
        return null;
    }
  };

  return (
    <div className="min-h-screen bg-canvas">
      <Sidebar />
      <Header
        title={event.title}
        subtitle="Team Competition"
        status={event.status}
        submissionType={event.submissionType}
      />
      <main className="fixed top-20 left-60 right-0 bottom-0 overflow-hidden bg-canvas md:left-0">
        <div className="h-full overflow-y-auto p-6">
          <div className="animate-fadeIn">{renderTabContent()}</div>
        </div>
      </main>
      <Modal isOpen={isModalOpen} onClose={() => setIsModalOpen(false)} title="Create New Team">
        <TeamRegistrationForm eventId={eventId} userId={userId} />
      </Modal>
    </div>
  );
}
```

- [ ] **Step 2: Update Dashboard.tsx tab to fetch data**

```typescript
// src/features/events/components/EventDashboard/tabs/Dashboard.tsx
'use client';

import React from 'react';
import { useEvent, useEventTeams, useUserTeam } from '@/features/events/hooks/useEvents';
import { useEventDashboard } from '@/features/events/contexts/EventDashboardContext';
import { Card } from '../Card';
import { Button } from '../Button';

interface DashboardTabProps {
  eventId: string;
  userId: string;
}

export function DashboardTab({ eventId, userId }: DashboardTabProps) {
  const { setIsModalOpen } = useEventDashboard();
  const { data: event, isLoading: eventLoading, error: eventError } = useEvent(eventId);
  const { data: teams, isLoading: teamsLoading, error: teamsError } = useEventTeams(eventId);
  const { data: userTeam } = useUserTeam(eventId, userId);

  const formatDate = (date: string) => new Date(date).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });

  if (eventError || teamsError) {
    return (
      <div className="bg-error/10 border border-error rounded-sm p-6 text-center">
        <p className="t-body-md text-error font-bold">Failed to load event</p>
        <p className="t-body-sm text-mute mt-2">Please refresh and try again</p>
      </div>
    );
  }

  if (eventLoading || teamsLoading) {
    return (
      <div className="space-y-6">
        <div className="h-32 bg-surface-soft rounded-sm animate-pulse" />
        <div className="h-48 bg-surface-soft rounded-sm animate-pulse" />
      </div>
    );
  }

  if (!event) return <div className="t-body-md text-mute">Event not found</div>;

  return (
    <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
      <Card title="Event Details" className="lg:col-span-2">
        <div className="space-y-4">
          <p className="t-body-md text-body">{event.description}</p>
          <div className="space-y-3">
            <div className="flex justify-between items-baseline">
              <span className="t-body-sm text-mute">Start Date</span>
              <span className="t-body-strong text-ink">{formatDate(event.startDate)}</span>
            </div>
            <div className="flex justify-between items-baseline border-t border-hairline pt-3">
              <span className="t-body-sm text-mute">End Date</span>
              <span className="t-body-strong text-ink">{formatDate(event.endDate)}</span>
            </div>
            <div className="flex justify-between items-baseline border-t border-hairline pt-3">
              <span className="t-body-sm text-mute">Status</span>
              <span className={`inline-block px-3 py-1 rounded-sm t-caption-sm font-bold uppercase ${event.status === 'open' ? 'bg-primary text-on-primary' : 'bg-stone text-on-dark'}`}>
                {event.status}
              </span>
            </div>
            <div className="flex justify-between items-baseline border-t border-hairline pt-3">
              <span className="t-body-sm text-mute">Submission Type</span>
              <span className="t-body-strong text-ink">{event.submissionType}</span>
            </div>
            <div className="flex justify-between items-baseline border-t border-hairline pt-3">
              <span className="t-body-sm text-mute">Total Teams</span>
              <span className="t-body-strong text-ink">{teams?.length || 0}</span>
            </div>
          </div>
        </div>
      </Card>

      <Card title="Registered Teams" className="lg:col-span-1">
        <div className="space-y-3 max-h-72 overflow-y-auto">
          {teams && teams.length > 0 ? (
            teams.map((team) => (
              <div key={team.id} className="pb-3 border-b border-hairline last:border-b-0">
                <p className="t-body-sm font-bold text-ink truncate">{team.name}</p>
                <p className="t-caption-sm text-mute">{team.leader.name}</p>
                <p className="t-caption-sm text-mute">{team.members.length + 1} members</p>
              </div>
            ))
          ) : (
            <p className="t-body-sm text-mute text-center py-4">No teams registered yet</p>
          )}
        </div>
      </Card>

      <div className="lg:col-span-3">
        {userTeam ? (
          <div className="bg-surface-soft border border-hairline rounded-sm p-4">
            <p className="t-body-strong text-ink mb-2">You're registered as: {userTeam.name}</p>
            <p className="t-body-sm text-mute">You can now submit your work and view results</p>
          </div>
        ) : (
          <Button onClick={() => setIsModalOpen(true)} size="lg" className="w-full">
            Register Team
          </Button>
        )}
      </div>
    </div>
  );
}
```

- [ ] **Step 3: Update page.tsx to pass props**

```typescript
// src/app/events/dashboard/page.tsx
'use client';

import { useSearchParams } from 'next/navigation';
import { EventDashboardProvider } from '@/features/events/contexts/EventDashboardContext';
import { EventDashboard } from '@/features/events/components/EventDashboard/EventDashboard';

export default function EventDashboardPage() {
  const searchParams = useSearchParams();
  const eventId = searchParams.get('eventId') || 'evt-001'; // Default for testing
  const userId = searchParams.get('userId') || 'user-001'; // Will come from auth

  return (
    <EventDashboardProvider>
      <EventDashboard eventId={eventId} userId={userId} />
    </EventDashboardProvider>
  );
}
```

- [ ] **Step 4: Commit**

```bash
git add src/features/events/components/EventDashboard/tabs/Dashboard.tsx \
        src/features/events/components/EventDashboard/EventDashboard.tsx \
        src/app/events/dashboard/page.tsx
git commit -m "feat: integrate event API data fetching with TanStack Query in Dashboard tab"
```

---

### Task 3: Update Team Registration Form to Submit to API

**Files:**
- Modify: `src/features/events/components/EventDashboard/TeamRegistrationForm.tsx`

**Context:** The form currently closes the modal but doesn't actually create the team. Wire it to useCreateTeam hook.

- [ ] **Step 1: Update TeamRegistrationForm.tsx to use API**

```typescript
// src/features/events/components/EventDashboard/TeamRegistrationForm.tsx
'use client';

import React, { useState } from 'react';
import { X } from 'lucide-react';
import { Button } from './Button';
import { useEventDashboard } from '@/features/events/contexts/EventDashboardContext';
import { useCreateTeam } from '@/features/events/hooks/useEvents';

interface TeamMember {
  id: string;
  name: string;
  email: string;
}

const mockUsers: TeamMember[] = [
  { id: 'user-1', name: 'Alice Johnson', email: 'alice@example.com' },
  { id: 'user-2', name: 'Bob Smith', email: 'bob@example.com' },
  { id: 'user-3', name: 'Charlie Wilson', email: 'charlie@example.com' },
  { id: 'user-4', name: 'Diana Lee', email: 'diana@example.com' },
  { id: 'user-5', name: 'Eve Martinez', email: 'eve@example.com' },
];

interface TeamRegistrationFormProps {
  eventId: string;
  userId: string;
}

export function TeamRegistrationForm({ eventId, userId }: TeamRegistrationFormProps) {
  const { setIsModalOpen } = useEventDashboard();
  const createTeamMutation = useCreateTeam(eventId);

  const [teamName, setTeamName] = useState('');
  const [selectedMembers, setSelectedMembers] = useState<TeamMember[]>([]);
  const [searchInput, setSearchInput] = useState('');
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [showMemberDropdown, setShowMemberDropdown] = useState(false);
  const [submitError, setSubmitError] = useState<string>('');

  const filteredUsers = mockUsers.filter(
    (user) =>
      user.id !== userId &&
      !selectedMembers.find((m) => m.id === user.id) &&
      (user.name.toLowerCase().includes(searchInput.toLowerCase()) ||
        user.email.toLowerCase().includes(searchInput.toLowerCase()))
  );

  const handleAddMember = (member: TeamMember) => {
    setSelectedMembers([...selectedMembers, member]);
    setSearchInput('');
    setShowMemberDropdown(false);
  };

  const handleRemoveMember = (memberId: string) => {
    setSelectedMembers(selectedMembers.filter((m) => m.id !== memberId));
  };

  const validateForm = () => {
    const newErrors: Record<string, string> = {};

    if (!teamName.trim()) {
      newErrors.teamName = 'Team name is required';
    }

    if (selectedMembers.length === 0) {
      newErrors.members = 'Add at least one team member';
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSubmitError('');

    if (!validateForm()) return;

    const memberIds = [userId, ...selectedMembers.map((m) => m.id)];

    try {
      await createTeamMutation.mutateAsync({
        name: teamName,
        memberIds,
      });

      setTeamName('');
      setSelectedMembers([]);
      setErrors({});
      setIsModalOpen(false);
    } catch (error) {
      setSubmitError(error instanceof Error ? error.message : 'Failed to create team');
    }
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      {submitError && (
        <div className="bg-error/10 border border-error rounded-sm p-3">
          <p className="t-body-sm text-error font-bold">{submitError}</p>
        </div>
      )}

      {/* Team Name */}
      <div>
        <label htmlFor="teamName" className="block t-body-strong text-ink mb-2">
          Team Name
        </label>
        <input
          id="teamName"
          type="text"
          placeholder="Enter team name"
          value={teamName}
          onChange={(e) => {
            setTeamName(e.target.value);
            if (errors.teamName) setErrors({ ...errors, teamName: '' });
          }}
          aria-invalid={!!errors.teamName}
          aria-describedby={errors.teamName ? 'teamName-error' : undefined}
          className={`w-full px-4 py-2 border rounded-sm text-body-md focus:outline-none transition-colors duration-150 ${
            errors.teamName ? 'border-error focus:border-error' : 'border-hairline focus:border-primary'
          }`}
        />
        {errors.teamName && (
          <p id="teamName-error" className="t-body-sm text-error mt-1" role="alert">
            {errors.teamName}
          </p>
        )}
      </div>

      {/* Team Members */}
      <div>
        <label className="block t-body-strong text-ink mb-2">Add Members</label>

        {/* Current User (Leader) */}
        <div className="mb-3 p-3 bg-surface-soft rounded-sm">
          <p className="t-body-sm text-mute uppercase font-bold text-xs">Team Leader (You)</p>
          <p className="t-body-strong text-ink">Current User</p>
        </div>

        {/* Member Search */}
        <div className="relative mb-3">
          <input
            type="text"
            placeholder="Search by name or email"
            value={searchInput}
            onChange={(e) => setSearchInput(e.target.value)}
            onFocus={() => setShowMemberDropdown(true)}
            className="w-full px-4 py-2 border border-hairline rounded-sm text-body-md focus:border-primary focus:outline-none"
          />

          {showMemberDropdown && filteredUsers.length > 0 && (
            <div className="absolute top-full left-0 right-0 mt-1 bg-canvas border border-hairline rounded-sm shadow-lg z-10">
              {filteredUsers.map((user) => (
                <button
                  key={user.id}
                  type="button"
                  onClick={() => handleAddMember(user)}
                  className="w-full text-left px-4 py-2 hover:bg-surface-soft transition-colors duration-150 border-b border-hairline last:border-b-0"
                >
                  <p className="t-body-sm font-bold text-ink">{user.name}</p>
                  <p className="t-caption-sm text-mute">{user.email}</p>
                </button>
              ))}
            </div>
          )}
        </div>

        {/* Selected Members */}
        {selectedMembers.length > 0 && (
          <div className="flex flex-wrap gap-2 mb-3">
            {selectedMembers.map((member) => (
              <div
                key={member.id}
                className="inline-flex items-center gap-2 bg-surface-soft text-ink px-3 py-1 rounded-full text-caption-xs"
              >
                <span>{member.name}</span>
                <button
                  type="button"
                  onClick={() => handleRemoveMember(member.id)}
                  className="ml-1 hover:text-error transition-colors duration-150 focus-visible:outline-1 focus-visible:outline-primary"
                  aria-label={`Remove ${member.name} from team`}
                >
                  <X size={12} aria-hidden="true" />
                </button>
              </div>
            ))}
          </div>
        )}

        {errors.members && (
          <p className="t-body-sm text-error" role="alert">
            {errors.members}
          </p>
        )}
      </div>

      {/* Action Buttons */}
      <div className="flex gap-3 justify-end pt-4 border-t border-hairline">
        <Button variant="secondary" onClick={() => setIsModalOpen(false)} disabled={createTeamMutation.isPending}>
          Cancel
        </Button>
        <Button type="submit" isLoading={createTeamMutation.isPending}>
          Create Team
        </Button>
      </div>
    </form>
  );
}
```

- [ ] **Step 2: Commit**

```bash
git add src/features/events/components/EventDashboard/TeamRegistrationForm.tsx
git commit -m "feat: wire team registration form to createTeam API mutation"
```

---

## Phase 2: Form Validation & Error Handling

### Task 4: Add Client-Side Validation for Submission Form

**Files:**
- Modify: `src/features/events/components/EventDashboard/tabs/Submission.tsx`
- Create: `src/features/events/utils/validationUtils.ts`

**Context:** Submission form needs real validation — ZIP file size limits, URL format checking, required fields.

- [ ] **Step 1: Create validation utilities**

```typescript
// src/features/events/utils/validationUtils.ts
export interface ValidationResult {
  isValid: boolean;
  errors: Record<string, string>;
}

export const zipValidation = {
  maxSize: 50 * 1024 * 1024, // 50MB

  validate: (file: File): ValidationResult => {
    const errors: Record<string, string> = {};

    if (!file) {
      errors.file = 'Please select a ZIP file';
      return { isValid: false, errors };
    }

    if (file.type !== 'application/zip' && !file.name.endsWith('.zip')) {
      errors.file = 'File must be a ZIP archive';
    }

    if (file.size > zipValidation.maxSize) {
      errors.file = `File size must be less than 50MB (current: ${(file.size / 1024 / 1024).toFixed(2)}MB)`;
    }

    return {
      isValid: Object.keys(errors).length === 0,
      errors,
    };
  },
};

export const urlValidation = {
  validate: (url: string): ValidationResult => {
    const errors: Record<string, string> = {};

    if (!url.trim()) {
      errors.url = 'Please enter a submission link';
      return { isValid: false, errors };
    }

    try {
      new URL(url);
    } catch {
      errors.url = 'Please enter a valid URL (starting with http:// or https://)';
      return { isValid: false, errors };
    }

    const validDomains = ['github.com', 'drive.google.com', 'gitlab.com', 'bitbucket.org'];
    const isValidDomain = validDomains.some((domain) => url.includes(domain));

    if (!isValidDomain) {
      errors.url = 'URL must be from GitHub, Google Drive, GitLab, or Bitbucket';
    }

    return {
      isValid: Object.keys(errors).length === 0,
      errors,
    };
  },
};
```

- [ ] **Step 2: Update Submission.tsx with validation**

```typescript
// src/features/events/components/EventDashboard/tabs/Submission.tsx
'use client';

import React, { useState } from 'react';
import { useTeamSubmissions, useSubmitWork } from '@/features/events/hooks/useEvents';
import { zipValidation, urlValidation } from '@/features/events/utils/validationUtils';
import { Card } from '../Card';
import { Button } from '../Button';
import { Badge } from '../Badge';
import { Upload, Link as LinkIcon, AlertCircle } from 'lucide-react';

interface SubmissionTabProps {
  teamId: string;
  eventId: string;
}

export function SubmissionTab({ teamId, eventId }: SubmissionTabProps) {
  const { data: submissions, isLoading, error } = useTeamSubmissions(teamId);
  const submitMutation = useSubmitWork(teamId, eventId);

  const [submissionType, setSubmissionType] = useState<'zip' | 'url'>('zip');
  const [zipFile, setZipFile] = useState<File | null>(null);
  const [urlInput, setUrlInput] = useState('');
  const [validationErrors, setValidationErrors] = useState<Record<string, string>>({});

  const handleZipSelect = (file: File | null) => {
    if (file) {
      const result = zipValidation.validate(file);
      if (result.isValid) {
        setZipFile(file);
        setValidationErrors({});
      } else {
        setValidationErrors(result.errors);
        setZipFile(null);
      }
    }
  };

  const handleZipDrop = (e: React.DragEvent) => {
    e.preventDefault();
    const files = e.dataTransfer.files;
    if (files.length > 0) handleZipSelect(files[0]);
  };

  const handleUrlChange = (value: string) => {
    setUrlInput(value);
    if (value.trim()) {
      const result = urlValidation.validate(value);
      setValidationErrors(result.errors);
    } else {
      setValidationErrors({});
    }
  };

  const handleSubmit = async () => {
    if (submissionType === 'zip') {
      if (!zipFile) {
        setValidationErrors({ file: 'Please select a ZIP file' });
        return;
      }
      const formData = new FormData();
      formData.append('file', zipFile);
      await submitMutation.mutateAsync({ type: 'ZIP', content: formData });
    } else {
      const result = urlValidation.validate(urlInput);
      if (!result.isValid) {
        setValidationErrors(result.errors);
        return;
      }
      await submitMutation.mutateAsync({ type: 'URL', content: urlInput });
    }

    setZipFile(null);
    setUrlInput('');
    setValidationErrors({});
  };

  const getStatusBadge = (status: string) => {
    const statusMap: Record<string, { variant: any; label: string }> = {
      submitted: { variant: 'primary', label: 'Submitted' },
      'pending-review': { variant: 'warning', label: 'Pending Review' },
      rejected: { variant: 'error', label: 'Rejected' },
      graded: { variant: 'success', label: 'Graded' },
    };
    const config = statusMap[status] || { variant: 'default', label: 'Unknown' };
    return <Badge variant={config.variant}>{config.label}</Badge>;
  };

  const formatDate = (date: string) => new Date(date).toLocaleDateString('en-US', { month: 'short', day: 'numeric' });

  if (error) {
    return (
      <div className="bg-error/10 border border-error rounded-sm p-6">
        <p className="t-body-md text-error font-bold">Failed to load submissions</p>
      </div>
    );
  }

  return (
    <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
      <Card title="Submit for Event">
        <div className="space-y-4">
          {submissionType === 'zip' ? (
            <div
              onDrop={handleZipDrop}
              onDragOver={(e) => e.preventDefault()}
              className={`border-2 border-dashed rounded-sm p-8 text-center cursor-pointer transition-colors duration-150 ${
                validationErrors.file
                  ? 'border-error bg-error/5'
                  : 'border-hairline bg-surface-soft hover:border-primary'
              }`}
            >
              <Upload size={32} className={`mx-auto mb-2 ${validationErrors.file ? 'text-error' : 'text-mute'}`} />
              <label className="block">
                <input
                  type="file"
                  accept=".zip"
                  onChange={(e) => handleZipSelect(e.target.files?.[0] || null)}
                  className="sr-only"
                  aria-label="Upload ZIP file"
                />
                <span className="t-body-md text-ink cursor-pointer">Drag ZIP file here or click to browse</span>
              </label>
              <p className="t-caption-sm text-mute mt-2">Max 50MB</p>
              {zipFile && <p className="t-body-sm text-primary mt-2 font-bold">✓ {zipFile.name}</p>}
            </div>
          ) : (
            <div className="space-y-2">
              <label htmlFor="urlInput" className="t-body-strong text-ink block">
                Submission Link
              </label>
              <div className="relative">
                <input
                  id="urlInput"
                  type="url"
                  placeholder="https://github.com/... or https://drive.google.com/..."
                  value={urlInput}
                  onChange={(e) => handleUrlChange(e.target.value)}
                  className={`w-full px-4 py-2 border rounded-sm text-body-md focus:outline-none transition-colors duration-150 ${
                    validationErrors.url ? 'border-error focus:border-error' : 'border-hairline focus:border-primary'
                  }`}
                  aria-invalid={!!validationErrors.url}
                  aria-describedby={validationErrors.url ? 'url-error' : undefined}
                />
                <LinkIcon size={16} className="absolute right-3 top-1/2 transform -translate-y-1/2 text-mute" />
              </div>
              <p className="t-caption-sm text-mute">Paste GitHub, Drive, or similar link</p>
            </div>
          )}

          {validationErrors.file && (
            <div className="flex gap-2 bg-error/10 border border-error rounded-sm p-3">
              <AlertCircle size={16} className="text-error flex-shrink-0 mt-0.5" />
              <p id="file-error" className="t-body-sm text-error">{validationErrors.file}</p>
            </div>
          )}

          {validationErrors.url && (
            <div className="flex gap-2 bg-error/10 border border-error rounded-sm p-3">
              <AlertCircle size={16} className="text-error flex-shrink-0 mt-0.5" />
              <p id="url-error" className="t-body-sm text-error">{validationErrors.url}</p>
            </div>
          )}

          <Button
            onClick={handleSubmit}
            isLoading={submitMutation.isPending}
            disabled={!zipFile && !urlInput}
            className="w-full"
            size="lg"
          >
            Submit
          </Button>
        </div>
      </Card>

      <Card title="Submission History">
        <div className="space-y-3">
          {!isLoading && submissions && submissions.length === 0 ? (
            <div className="text-center py-8">
              <Upload size={32} className="text-mute mx-auto mb-2 opacity-50" />
              <p className="t-body-md text-mute">No submissions yet</p>
            </div>
          ) : (
            submissions?.map((sub) => (
              <div key={sub.id} className="pb-3 border-b border-hairline last:border-b-0">
                <div className="flex items-center justify-between gap-2 mb-1">
                  <p className="t-caption-sm text-mute">{formatDate(sub.submitDate)}</p>
                  {getStatusBadge(sub.status)}
                </div>
                <p className="t-body-sm text-ink font-bold">{sub.type} Submission</p>
              </div>
            ))
          )}
        </div>
      </Card>
    </div>
  );
}
```

- [ ] **Step 3: Commit**

```bash
git add src/features/events/utils/validationUtils.ts \
        src/features/events/components/EventDashboard/tabs/Submission.tsx
git commit -m "feat: add client-side validation for ZIP and URL submissions"
```

---

## Phase 3: Testing & Accessibility

### Task 5: Write Unit Tests for API Service

**Files:**
- Create: `src/features/events/__tests__/api/eventService.test.ts`

**Context:** Ensure API calls are properly mocked and error handling works.

- [ ] **Step 1: Create test file for eventService**

```typescript
// src/features/events/__tests__/api/eventService.test.ts
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import axios from 'axios';
import eventService from '@/features/events/api/eventService';

vi.mock('axios');
const mockedAxios = axios as unknown as { get: any; post: any };

describe('eventService', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('getEvent', () => {
    it('should fetch event by ID', async () => {
      const mockEvent = {
        id: 'evt-001',
        title: 'Test Event',
        startDate: '2026-06-15',
        endDate: '2026-06-30',
        status: 'open',
        submissionType: 'ZIP',
        description: 'Test event description',
      };

      mockedAxios.get.mockResolvedValue({ data: mockEvent });

      const result = await eventService.getEvent('evt-001');

      expect(result).toEqual(mockEvent);
      expect(mockedAxios.get).toHaveBeenCalledWith('http://localhost:8000/api/events/evt-001');
    });

    it('should handle API errors', async () => {
      mockedAxios.get.mockRejectedValue(new Error('API Error'));

      await expect(eventService.getEvent('evt-001')).rejects.toThrow('API Error');
    });
  });

  describe('createTeam', () => {
    it('should create a team with members', async () => {
      const mockTeam = {
        id: 'team-001',
        name: 'Test Team',
        leader: { id: 'user-1', name: 'Leader', email: 'leader@test.com' },
        members: [{ id: 'user-2', name: 'Member', email: 'member@test.com' }],
        eventId: 'evt-001',
      };

      mockedAxios.post.mockResolvedValue({ data: mockTeam });

      const result = await eventService.createTeam('evt-001', {
        name: 'Test Team',
        memberIds: ['user-1', 'user-2'],
      });

      expect(result).toEqual(mockTeam);
      expect(mockedAxios.post).toHaveBeenCalledWith(
        'http://localhost:8000/api/events/evt-001/teams',
        { name: 'Test Team', memberIds: ['user-1', 'user-2'] }
      );
    });

    it('should validate team data before submission', async () => {
      mockedAxios.post.mockRejectedValue(new Error('Team name required'));

      await expect(
        eventService.createTeam('evt-001', { name: '', memberIds: ['user-1'] })
      ).rejects.toThrow();
    });
  });

  describe('submitWork', () => {
    it('should submit URL submission', async () => {
      const mockSubmission = {
        id: 'sub-001',
        teamId: 'team-001',
        eventId: 'evt-001',
        type: 'URL' as const,
        status: 'submitted' as const,
        submitDate: '2026-06-20',
        content: 'https://github.com/test/repo',
      };

      mockedAxios.post.mockResolvedValue({ data: mockSubmission });

      const result = await eventService.submitWork('team-001', 'evt-001', {
        type: 'URL',
        content: 'https://github.com/test/repo',
      });

      expect(result).toEqual(mockSubmission);
    });
  });
});
```

- [ ] **Step 2: Run tests**

```bash
npm run test -- src/features/events/__tests__/api/eventService.test.ts
```

Expected: PASS (or adjust implementation based on test failures)

- [ ] **Step 3: Commit**

```bash
git add src/features/events/__tests__/api/eventService.test.ts
git commit -m "test: add unit tests for event service API calls"
```

---

### Task 6: Accessibility Audit & Keyboard Navigation Test

**Files:**
- Create: `src/features/events/__tests__/components/accessibility.test.tsx`

**Context:** Verify keyboard navigation, focus management, ARIA labels work correctly.

- [ ] **Step 1: Create accessibility test file**

```typescript
// src/features/events/__tests__/components/accessibility.test.tsx
import { describe, it, expect } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { EventDashboardProvider } from '@/features/events/contexts/EventDashboardContext';
import { Sidebar } from '@/features/events/components/EventDashboard/Sidebar';

describe('Accessibility - Sidebar Navigation', () => {
  it('should have proper semantic HTML structure', () => {
    render(
      <EventDashboardProvider>
        <Sidebar />
      </EventDashboardProvider>
    );

    // Verify nav element exists
    const nav = screen.getByRole('navigation', { name: /event dashboard navigation/i });
    expect(nav).toBeInTheDocument();

    // Verify tablist role
    const tablist = screen.getByRole('tablist');
    expect(tablist).toBeInTheDocument();

    // Verify tabs have proper roles
    const tabs = screen.getAllByRole('tab');
    expect(tabs.length).toBe(3);
  });

  it('should support keyboard navigation with Tab key', async () => {
    const user = userEvent.setup();
    render(
      <EventDashboardProvider>
        <Sidebar />
      </EventDashboardProvider>
    );

    const firstTab = screen.getByRole('tab', { name: /event dashboard/i });
    const secondTab = screen.getByRole('tab', { name: /submission/i });

    // Focus first tab
    firstTab.focus();
    expect(document.activeElement).toBe(firstTab);

    // Tab to next element
    await user.tab();
    expect(document.activeElement).toBe(secondTab);
  });

  it('should show visible focus state', () => {
    const { container } = render(
      <EventDashboardProvider>
        <Sidebar />
      </EventDashboardProvider>
    );

    const firstTab = screen.getByRole('tab', { name: /event dashboard/i });
    firstTab.focus();

    const styles = window.getComputedStyle(firstTab);
    expect(styles.outline).toBeTruthy();
    expect(styles.outlineColor).toBeTruthy();
  });

  it('should have ARIA labels on icon-only buttons', () => {
    const { container } = render(
      <EventDashboardProvider>
        <Sidebar />
      </EventDashboardProvider>
    );

    // Check that settings icon button has aria-label
    const settingsButton = screen.getByRole('button', { name: /user settings/i });
    expect(settingsButton).toHaveAttribute('aria-label', 'User settings');
  });

  it('should announce active tab state to screen readers', () => {
    render(
      <EventDashboardProvider>
        <Sidebar />
      </EventDashboardProvider>
    );

    const activeTab = screen.getByRole('tab', { name: /event dashboard/i });
    expect(activeTab).toHaveAttribute('aria-selected', 'true');

    const inactiveTab = screen.getByRole('tab', { name: /submission/i });
    expect(inactiveTab).toHaveAttribute('aria-selected', 'false');
  });
});
```

- [ ] **Step 2: Run accessibility tests**

```bash
npm run test -- src/features/events/__tests__/components/accessibility.test.tsx
```

Expected: PASS (verify all accessibility attributes are present)

- [ ] **Step 3: Test in browser with keyboard only**

Manually test: Open dashboard, press Tab repeatedly to verify you can navigate all interactive elements without mouse.

- [ ] **Step 4: Commit**

```bash
git add src/features/events/__tests__/components/accessibility.test.tsx
git commit -m "test: add accessibility tests for keyboard navigation and ARIA labels"
```

---

## Phase 4: Responsive Refinement & Polish

### Task 7: Test & Fix Mobile Responsive Layout

**Files:**
- Modify: `src/features/events/components/EventDashboard/Sidebar.tsx`
- Modify: `src/features/events/components/EventDashboard/Header.tsx`
- Modify: `src/app/globals.css` (if needed for mobile tweaks)

**Context:** Manually test sidebar collapse, content reflow, form inputs on mobile viewports (375px, 480px, 768px).

- [ ] **Step 1: Test on 375px viewport (narrow mobile)**

Use browser dev tools: Set viewport to 375x812 (iPhone SE)

Expected behavior:
- Sidebar becomes 60px icon-only ✓
- Header title truncates or reduces font size ✓
- Content reflows to single column ✓
- Register button full-width ✓
- Modal fits with padding ✓
- Form inputs accessible ✓

If issues found, document them for Task 8.

- [ ] **Step 2: Test on 480px viewport (larger mobile)**

Set viewport to 480x800

Expected: Same as 375px but with more breathing room

- [ ] **Step 3: Test on 768px viewport (tablet)**

Set viewport to 768x1024

Expected:
- Sidebar 200px (not collapsed yet) ✓
- Content reflows appropriately ✓
- 2-column layouts stack to 1 column ✓

- [ ] **Step 4: Test touch interactions**

On mobile/tablet:
- Tab buttons are 44x44px minimum ✓
- Form inputs have 16px font (prevents zoom) ✓
- Buttons are easily tappable ✓
- Modal has close button accessible ✓

- [ ] **Step 5: Test horizontal scroll**

Ensure no horizontal scrolling on any viewport.

- [ ] **Step 6: Document findings**

If responsive issues found, create GitHub issues or add to Task 8 checklist.

- [ ] **Step 7: Commit (if changes made)**

```bash
git add src/features/events/components/EventDashboard/Sidebar.tsx \
        src/features/events/components/EventDashboard/Header.tsx
git commit -m "refactor: refine mobile responsive layout for 375px-1440px viewports"
```

---

### Task 8: Fix Responsive Issues & Missing Details

**Files:**
- Modify: (As needed based on Task 7 findings)

**Context:** Address any gaps found in responsive testing.

Common fixes:
- Font size reductions on mobile (e.g., heading-sm → body-md)
- Padding adjustments for narrow viewports
- Touch target sizing
- Modal max-width adjustments

- [ ] **Step 1: (Conditional) Fix identified issues**

Example fix (if header title is too large):

```typescript
<h1 className="t-heading-md text-ink m-0 md:t-heading-sm">
  {event.title}
</h1>
```

- [ ] **Step 2: Retest on all viewports**

375px, 480px, 768px, 1024px, 1440px

- [ ] **Step 3: Commit**

```bash
git add -A
git commit -m "fix: resolve mobile responsive issues and touch target sizing"
```

---

### Task 9: Add Loading Skeleton States

**Files:**
- Create: `src/features/events/components/EventDashboard/SkeletonLoaders.tsx`
- Modify: `src/features/events/components/EventDashboard/tabs/Dashboard.tsx`
- Modify: `src/features/events/components/EventDashboard/tabs/Submission.tsx`
- Modify: `src/features/events/components/EventDashboard/tabs/Results.tsx`

**Context:** While data is loading, show skeleton screens instead of spinners for better UX.

- [ ] **Step 1: Create skeleton loader components**

```typescript
// src/features/events/components/EventDashboard/SkeletonLoaders.tsx
'use client';

import React from 'react';

export function CardSkeleton() {
  return (
    <div className="bg-canvas border border-hairline rounded-sm p-6">
      <div className="h-6 bg-surface-soft rounded-sm mb-4 w-1/3" />
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
```

- [ ] **Step 2: Use skeleton in Dashboard tab**

```typescript
// In Dashboard.tsx DashboardTab function, replace the loading state:
if (eventLoading || teamsLoading) {
  return (
    <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
      <CardSkeleton />
      <CardSkeleton />
      <CardSkeleton />
    </div>
  );
}
```

- [ ] **Step 3: Use skeleton in Submission tab**

Similarly update Submission.tsx

- [ ] **Step 4: Use skeleton in Results tab**

Similarly update Results.tsx

- [ ] **Step 5: Verify animation respects prefers-reduced-motion**

Add to skeleton styles:
```css
@media (prefers-reduced-motion: reduce) {
  .animate-pulse {
    animation: none;
    opacity: 0.5;
  }
}
```

- [ ] **Step 6: Commit**

```bash
git add src/features/events/components/EventDashboard/SkeletonLoaders.tsx \
        src/features/events/components/EventDashboard/tabs/Dashboard.tsx \
        src/features/events/components/EventDashboard/tabs/Submission.tsx \
        src/features/events/components/EventDashboard/tabs/Results.tsx
git commit -m "feat: add skeleton loaders for better loading state UX"
```

---

## Phase 5: Documentation & Final Testing

### Task 10: Document API Integration & Environment Setup

**Files:**
- Create: `docs/EVENT_DASHBOARD_API.md`
- Create: `.env.example`

**Context:** Document how to connect to real API, environment variables, expected API responses.

- [ ] **Step 1: Create API documentation**

```markdown
# Event Dashboard API Integration

## Environment Variables

Add to `.env.local`:

\`\`\`env
NEXT_PUBLIC_API_URL=http://localhost:8000/api
\`\`\`

## Required API Endpoints

### Events
- `GET /api/events/:eventId` — Get event details
- `GET /api/events/:eventId/teams` — Get all teams
- `GET /api/events/:eventId/teams/user/:userId` — Get user's team
- `POST /api/events/:eventId/teams` — Create team
- `GET /api/events/:eventId/scores` — Get leaderboard scores

### Submissions
- `GET /api/teams/:teamId/submissions` — Get team submissions
- `POST /api/teams/:teamId/submissions` — Submit work (multipart for ZIP)

### Scores
- `GET /api/teams/:teamId/scores?eventId=:eventId` — Get score breakdown

## Response Formats

See `src/features/events/api/eventService.ts` for TypeScript interfaces.

## Error Handling

API errors are caught by TanStack Query and stored in hook's `error` property.
Display error state in tabs (see Dashboard.tsx for example).

## Caching Strategy

- Event: 5 min cache
- Teams: 2 min cache
- Submissions: 1 min cache
- Scores: 2 min cache

Invalidation: Happens automatically after mutations (create team, submit work).
\`\`\`

- [ ] **Step 2: Create .env.example**

```bash
# .env.example
NEXT_PUBLIC_API_URL=http://localhost:8000/api
```

- [ ] **Step 3: Commit**

```bash
git add docs/EVENT_DASHBOARD_API.md .env.example
git commit -m "docs: add API integration documentation and environment setup"
```

---

### Task 11: Final End-to-End Testing

**Files:**
- None (testing only)

**Context:** Comprehensive testing of all features in dashboard.

- [ ] **Step 1: Test Event Dashboard Tab**

Flow:
1. Load dashboard with valid eventId and userId
2. See event details loaded ✓
3. See team list populated ✓
4. Click "Register Team" → modal opens ✓
5. Enter team name, add members ✓
6. Submit → team appears in list ✓
7. "You're registered" message shows ✓

- [ ] **Step 2: Test Submission Tab**

Flow (with registered team):
1. Click Submission tab ✓
2. See submission form ✓
3. For ZIP: Drag-drop file → validation passes ✓
4. Submit → submission appears in history ✓
5. For URL: Paste GitHub link → validation passes ✓
6. Submit → submission appears in history ✓

Invalid tests:
- Try ZIP >50MB → error shows ✓
- Try invalid URL → error shows ✓
- Try non-ZIP file → error shows ✓

- [ ] **Step 3: Test Results Tab**

Flow (with submitted work):
1. Click Results tab ✓
2. See team ranking ✓
3. See score breakdown with Graded/Pending icons ✓
4. See comparison vs average ✓
5. See leaderboard table with all teams ✓
6. Current team highlighted ✓

- [ ] **Step 4: Test Error States**

- [ ] Disconnect API → see error card in each tab
- [ ] Invalid eventId → see "Event not found"
- [ ] API timeout → see error with retry option

- [ ] **Step 5: Test Mobile (375px)**

Navigate through all tabs, forms, modals on narrow mobile.

- [ ] **Step 6: Test Keyboard Navigation**

Tab through all interactive elements without mouse.

- [ ] **Step 7: Test Screen Reader**

Use VoiceOver (Mac) or NVDA (Windows) to verify all content is announced.

---

### Task 12: Final Code Cleanup & Commit

**Files:**
- All modified files

**Context:** Remove console logs, ensure no warnings in build.

- [ ] **Step 1: Check for console logs**

```bash
grep -r "console\." src/features/events --include="*.tsx" --include="*.ts"
```

Remove any found.

- [ ] **Step 2: Run build and check for warnings**

```bash
npm run build 2>&1 | grep -i warning
```

Fix any TypeScript or build warnings.

- [ ] **Step 3: Run linter**

```bash
npm run lint
```

Fix any linting errors.

- [ ] **Step 4: Final commit**

```bash
git add -A
git commit -m "chore: cleanup console logs, fix linting warnings, final polish"
```

- [ ] **Step 5: Push to remote**

```bash
git push origin kimdtt/dev
```

---

## Summary

**Completed:**
- ✓ UI Components (Phase 1 baseline)
- ✓ API Integration with TanStack Query
- ✓ Form Validation (ZIP/URL)
- ✓ Error Handling
- ✓ Accessibility (WCAG AA)
- ✓ Responsive Design (375px–1440px)
- ✓ Testing (unit, integration, e2e)
- ✓ Documentation

**Next Steps After Implementation:**
1. Code review from team
2. QA testing on real devices
3. Performance profiling (Core Web Vitals)
4. Analytics setup (track submissions, conversions)
5. User feedback loop & iteration
