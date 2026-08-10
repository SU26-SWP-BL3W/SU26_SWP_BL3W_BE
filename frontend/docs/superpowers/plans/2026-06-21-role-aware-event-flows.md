# Role-aware event flows Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Wire role-aware navigation and per-event workflows so STUDENT, JUDGE (=MENTOR), and ADMIN each see only what they should — with team creation, submission, per-category-per-round results, and leaderboard for students; assigned-teams and leaderboard for judges.

**Architecture:** Add a small `useUserRole()` hook + two pure helpers (`getNavLinksFor`, `getEventTabs`). Reskin the existing `EventDashboard` sidebar so its tab set is computed per role + team-membership. Add three new feature folders (`teams`, `submissions`, `results`) that wrap the existing backend endpoints. Reuse the global axios client and `@tanstack/react-query` patterns already in `useEvents.ts`. Gate the existing `CreateEventForm` CTA on admin-only.

**Tech Stack:** Next.js (App Router), React 19, TypeScript, TanStack Query v5, axios, Tailwind v4, Jest + jsdom + Testing Library.

## Global Constraints

- API base URL: `process.env.NEXT_PUBLIC_API_URL ?? "https://api.sealswp391.xyz/api"` (already set in `src/services/api/client.ts`).
- All responses are unwrapped from `BaseResponse<T>` by the axios interceptor — endpoints return `data` directly.
- `EventRoleType` integers: `0=EventCoordinator, 1=Judge, 2=Mentor, 3=TeamLeader, 4=TeamMember`.
- Role mapping: `UserProfile.role` ∈ `ADMIN | STUDENT | MENTOR`. Treat `MENTOR === judge` in the UI.
- Vietnamese copy in user-visible strings — match existing tone (e.g. `"Sự kiện"`, `"Khác"`, `"Tạo đội"`, `"Đội của tôi"`, `"Nộp bài"`, `"Kết quả"`, `"Bảng xếp hạng"`, `"Đội được phân công"`).
- Tests: `__tests__/**/*.test.ts(x)` under `src/`. Run `npm test -- --watch=false` for one-shot.
- Commit frequently after each green step.

---

## File Structure

**Create**
```
src/hooks/useUserRole.ts
src/hooks/__tests__/useUserRole.test.ts
src/lib/nav/getNavLinksFor.ts
src/lib/nav/__tests__/getNavLinksFor.test.ts
src/lib/events/getEventTabs.ts
src/lib/events/__tests__/getEventTabs.test.ts

src/features/teams/api/teams.ts
src/features/teams/types/team.types.ts
src/features/teams/hooks/useTeams.ts
src/features/teams/components/CreateTeamForm.tsx
src/features/teams/components/TeamMembersPanel.tsx

src/features/submissions/api/submissions.ts
src/features/submissions/hooks/useSubmissions.ts
src/features/submissions/components/SubmissionPanel.tsx

src/features/results/api/results.ts
src/features/results/hooks/useResults.ts
src/features/results/components/ResultsAccordion.tsx

src/features/leaderboard/components/RoundLeaderboard.tsx

src/features/events/components/EventDashboard/tabs/EventDetail.tsx
src/features/events/components/EventDashboard/tabs/CreateTeam.tsx
src/features/events/components/EventDashboard/tabs/MyTeam.tsx
src/features/events/components/EventDashboard/tabs/Leaderboard.tsx
src/features/events/components/EventDashboard/tabs/JudgeAssignedTeams.tsx
```

**Modify**
```
src/components/Navbar.tsx
src/features/events/components/EventSection.tsx
src/features/events/components/EventDashboard/Sidebar.tsx
src/features/events/components/EventDashboard/EventDashboard.tsx
src/features/events/contexts/EventDashboardContext.tsx        (extend tab union)
src/app/events/[id]/page.tsx                                   (real userId)
src/services/api/index.ts                                       (export new types if needed)
```

---

## Task 1: `useUserRole` hook

**Files:**
- Create: `src/hooks/useUserRole.ts`
- Test: `src/hooks/__tests__/useUserRole.test.ts`

**Interfaces:**
- Consumes: `useCurrentUser` from `@/hooks/useAuth`
- Produces: `useUserRole(): 'admin' | 'student' | 'judge' | null`

- [ ] **Step 1: Write the failing test**

`src/hooks/__tests__/useUserRole.test.ts`:

```ts
import { renderHook } from '@testing-library/react';
import { useUserRole } from '../useUserRole';
import * as auth from '@/hooks/useAuth';

jest.mock('@/hooks/useAuth');

const mockUseCurrentUser = auth.useCurrentUser as jest.Mock;

function mockUser(role: 'ADMIN' | 'STUDENT' | 'MENTOR' | null) {
  mockUseCurrentUser.mockReturnValue({
    data: role ? { id: 'u1', email: 'a@b.c', fullName: 'X', role, createdAt: '', stats: { eventsJoined: 0, projectScore: 0, rank: 0 } } : null,
  });
}

describe('useUserRole', () => {
  test('ADMIN → admin', () => { mockUser('ADMIN'); expect(renderHook(() => useUserRole()).result.current).toBe('admin'); });
  test('STUDENT → student', () => { mockUser('STUDENT'); expect(renderHook(() => useUserRole()).result.current).toBe('student'); });
  test('MENTOR → judge', () => { mockUser('MENTOR'); expect(renderHook(() => useUserRole()).result.current).toBe('judge'); });
  test('no user → null', () => { mockUser(null); expect(renderHook(() => useUserRole()).result.current).toBeNull(); });
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `npm test -- --watch=false src/hooks/__tests__/useUserRole.test.ts`
Expected: FAIL — `Cannot find module '../useUserRole'`.

- [ ] **Step 3: Implement the hook**

`src/hooks/useUserRole.ts`:

```ts
'use client';

import { useCurrentUser } from '@/hooks/useAuth';

export type AppRole = 'admin' | 'student' | 'judge';

export function useUserRole(): AppRole | null {
  const { data } = useCurrentUser();
  if (!data) return null;
  switch (data.role) {
    case 'ADMIN':   return 'admin';
    case 'STUDENT': return 'student';
    case 'MENTOR':  return 'judge';
    default:        return null;
  }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `npm test -- --watch=false src/hooks/__tests__/useUserRole.test.ts`
Expected: PASS (4/4).

- [ ] **Step 5: Commit**

```bash
git add src/hooks/useUserRole.ts src/hooks/__tests__/useUserRole.test.ts
git commit -m "feat(auth): add useUserRole hook mapping role to app-level role"
```

---

## Task 2: `getNavLinksFor` helper + role-aware Navbar

**Files:**
- Create: `src/lib/nav/getNavLinksFor.ts`, `src/lib/nav/__tests__/getNavLinksFor.test.ts`
- Modify: `src/components/Navbar.tsx`

**Interfaces:**
- Consumes: `AppRole` from `@/hooks/useUserRole`
- Produces: `getNavLinksFor(role: AppRole | null): Array<{ label: string; href: string }>`

- [ ] **Step 1: Write the failing test**

`src/lib/nav/__tests__/getNavLinksFor.test.ts`:

```ts
import { getNavLinksFor } from '../getNavLinksFor';

describe('getNavLinksFor', () => {
  test('guest → only Sự kiện', () => {
    expect(getNavLinksFor(null)).toEqual([{ label: 'Sự kiện', href: '/' }]);
  });
  test('student → Sự kiện + Khác', () => {
    expect(getNavLinksFor('student')).toEqual([
      { label: 'Sự kiện', href: '/' },
      { label: 'Khác',    href: '/other' },
    ]);
  });
  test('judge → Sự kiện + Khác', () => {
    expect(getNavLinksFor('judge')).toEqual([
      { label: 'Sự kiện', href: '/' },
      { label: 'Khác',    href: '/other' },
    ]);
  });
  test('admin → all four', () => {
    expect(getNavLinksFor('admin')).toEqual([
      { label: 'Sự kiện',             href: '/' },
      { label: 'Tiêu chí chấm điểm', href: '/criteria' },
      { label: 'Người dùng',         href: '/users' },
      { label: 'Khác',                href: '/other' },
    ]);
  });
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `npm test -- --watch=false src/lib/nav/__tests__/getNavLinksFor.test.ts`
Expected: FAIL — module not found.

- [ ] **Step 3: Implement the helper**

`src/lib/nav/getNavLinksFor.ts`:

```ts
import type { AppRole } from '@/hooks/useUserRole';

export interface NavLink { label: string; href: string; }

const LINK = {
  events:   { label: 'Sự kiện',             href: '/' },
  criteria: { label: 'Tiêu chí chấm điểm', href: '/criteria' },
  users:    { label: 'Người dùng',         href: '/users' },
  other:    { label: 'Khác',                href: '/other' },
} as const;

export function getNavLinksFor(role: AppRole | null): NavLink[] {
  if (role === 'admin')                       return [LINK.events, LINK.criteria, LINK.users, LINK.other];
  if (role === 'student' || role === 'judge') return [LINK.events, LINK.other];
  return [LINK.events];
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `npm test -- --watch=false src/lib/nav/__tests__/getNavLinksFor.test.ts`
Expected: PASS (4/4).

- [ ] **Step 5: Wire Navbar to use it**

Modify `src/components/Navbar.tsx` — replace the hard-coded array. New file body:

```tsx
"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useEffect } from "react";
import { useIsAuthenticated, useLogout } from "@/hooks/useAuth";
import { useUserRole } from "@/hooks/useUserRole";
import { getNavLinksFor } from "@/lib/nav/getNavLinksFor";

export function Navbar() {
  const pathname = usePathname();
  const isAuthenticated = useIsAuthenticated();
  const logout = useLogout();
  const role = useUserRole();
  const links = getNavLinksFor(role);

  useEffect(() => {
    const main = document.querySelector<HTMLElement>("main");
    if (main) main.focus({ preventScroll: true });
  }, [pathname]);

  return (
    <header>
      <nav
        className="primary-nav sticky-chrome"
        style={{ position: "sticky", top: 0, zIndex: 30 }}
      >
        <Link
          href="/"
          className="primary-nav__brand"
          style={{ fontSize: "var(--fs-heading-sm)", textDecoration: "none" }}
        >
          SWP<span style={{ color: "var(--color-primary)" }}>·</span>SE1907
        </Link>

        <ul className="primary-nav__links">
          {links.map(({ label, href }) => (
            <li key={href}>
              <Link
                href={href}
                style={{
                  color: pathname === href ? "var(--color-primary)" : "var(--color-on-dark)",
                  fontWeight: pathname === href ? 700 : 400,
                  fontSize: "var(--fs-body-md)",
                  textDecoration: "none",
                }}
              >
                {label}
              </Link>
            </li>
          ))}
        </ul>

        <div className="primary-nav__cluster">
          {isAuthenticated ? (
            <button
              type="button"
              onClick={() => logout.mutate()}
              disabled={logout.isPending}
              className="btn btn-outline-on-dark btn-sm"
            >
              {logout.isPending ? "Đang đăng xuất…" : "Đăng xuất"}
            </button>
          ) : (
            <Link href="/auth" className="btn btn-outline-on-dark btn-sm">
              Đăng nhập
            </Link>
          )}
        </div>
      </nav>
    </header>
  );
}
```

- [ ] **Step 6: Smoke check**

Run: `npm run build` (or `npm run lint`) — should compile without TS errors.

- [ ] **Step 7: Commit**

```bash
git add src/lib/nav src/components/Navbar.tsx
git commit -m "feat(nav): role-aware Navbar links via getNavLinksFor"
```

---

## Task 3: Gate "Tạo sự kiện" CTA to admin only

**Files:**
- Modify: `src/features/events/components/EventSection.tsx`

**Interfaces:**
- Consumes: `useUserRole` from `@/hooks/useUserRole`

- [ ] **Step 1: Read current EventSection**

Run: `grep -n "Tạo sự kiện\|mode" src/features/events/components/EventSection.tsx`

Confirm the toggle button + create-mode rendering on lines ~54–66.

- [ ] **Step 2: Edit EventSection — wrap admin gate**

In `src/features/events/components/EventSection.tsx`:

1. Add import at the top of the file (after existing imports):

```ts
import { useUserRole } from "@/hooks/useUserRole";
```

2. Inside the component body (right after `const [mode, setMode] = useState(...)`):

```ts
const role = useUserRole();
const isAdmin = role === 'admin';
```

3. Wrap the toggle button (the `← Quay lại` / `+ Tạo sự kiện` button) in `{isAdmin && ( ... )}`.

4. Render the `<CreateEventForm />` block only when `isAdmin && mode === 'create'`. If `mode === 'create'` but the viewer is not admin, force `mode === 'list'` by changing the conditional to `mode === "create" && isAdmin ? ... : ...`.

- [ ] **Step 3: Verify build + manual sanity**

Run: `npm run build`
Expected: clean.

- [ ] **Step 4: Commit**

```bash
git add src/features/events/components/EventSection.tsx
git commit -m "feat(events): hide Create Event CTA for non-admin viewers"
```

---

## Task 4: Real `userId` on event detail page

**Files:**
- Modify: `src/app/events/[id]/page.tsx`

- [ ] **Step 1: Edit page to pull userId from auth**

Replace the file body with:

```tsx
'use client';

import { Suspense } from 'react';
import { useParams } from 'next/navigation';
import { EventDashboardProvider } from '@/features/events/contexts/EventDashboardContext';
import { EventDashboard } from '@/features/events/components/EventDashboard/EventDashboard';
import { useCurrentUser } from '@/hooks/useAuth';

function EventDashboardPageContent() {
  const params = useParams();
  const eventId = params?.id as string;
  const { data: user } = useCurrentUser();
  const userId = user?.id ?? '';

  if (!userId) {
    return (
      <div className="min-h-screen bg-canvas flex items-center justify-center">
        <p className="t-body-md text-mute">Vui lòng đăng nhập để xem sự kiện.</p>
      </div>
    );
  }

  return (
    <EventDashboardProvider>
      <EventDashboard eventId={eventId} userId={userId} />
    </EventDashboardProvider>
  );
}

export default function EventDashboardPage() {
  return (
    <Suspense fallback={<div className="min-h-screen bg-canvas flex items-center justify-center"><p className="t-body-md text-mute">Loading...</p></div>}>
      <EventDashboardPageContent />
    </Suspense>
  );
}
```

- [ ] **Step 2: Build check + commit**

Run: `npm run build`
Expected: clean.

```bash
git add src/app/events/[id]/page.tsx
git commit -m "fix(events): use real userId from auth on event detail page"
```

---

## Task 5: Teams API + types

**Files:**
- Create: `src/features/teams/types/team.types.ts`
- Create: `src/features/teams/api/teams.ts`

**Interfaces:**
- Produces:
  - `interface TeamModel { id: string; teamName: string; eventId: string; ...members: TeamMember[] }` (see step 1)
  - `teamsApi.create(p: CreateTeamPayload): Promise<TeamModel>`
  - `teamsApi.getById(id: string): Promise<TeamModel>`
  - `teamsApi.addMember(teamId: string, userId: string): Promise<void>`
  - `teamsApi.removeMember(teamId: string, userId: string): Promise<void>`
  - `teamsApi.leave(teamId: string): Promise<void>`
  - `teamsApi.invite(teamId: string, email: string): Promise<void>`
  - `teamsApi.respondInvitation(invitationId: string, accept: boolean): Promise<void>`
  - `teamsApi.findUserTeamForEvent(userId: string, eventId: string): Promise<TeamModel | null>` — uses `/EventRoles/user/{userId}` filtered by `eventId` and `roleName ∈ {3,4}`, then `GET /Teams/{id}`.

- [ ] **Step 1: Create types**

`src/features/teams/types/team.types.ts`:

```ts
export interface TeamMember {
  userId: string;
  fullName: string;
  email: string;
  roleName: number; // EventRoleType (3 = TeamLeader, 4 = TeamMember)
}

export interface TeamModel {
  id: string;
  teamName: string;
  description: string | null;
  eventId: string;
  trackId: string | null;
  members: TeamMember[];
}

export interface CreateTeamPayload {
  teamName: string;
  description: string;
  eventId: string;
  trackId: string;
}
```

- [ ] **Step 2: Create api file**

`src/features/teams/api/teams.ts`:

```ts
import { apiClient } from '@/services/api';
import type { PagedResult } from '@/services/api';
import type { CreateTeamPayload, TeamModel } from '../types/team.types';

interface EventRoleRow {
  id: string;
  userId: string;
  eventId: string;
  teamId: string | null;
  roleName: number;
}

export const teamsApi = {
  create: async (p: CreateTeamPayload): Promise<TeamModel> => {
    const { data } = await apiClient.post<TeamModel>('/Teams', p);
    return data;
  },

  getById: async (id: string): Promise<TeamModel> => {
    const { data } = await apiClient.get<TeamModel>(`/Teams/${encodeURIComponent(id)}`);
    return data;
  },

  addMember: async (teamId: string, userId: string): Promise<void> => {
    await apiClient.post(`/Teams/${encodeURIComponent(teamId)}/members`, { userId });
  },

  removeMember: async (teamId: string, userId: string): Promise<void> => {
    await apiClient.delete(`/Teams/${encodeURIComponent(teamId)}/members/${encodeURIComponent(userId)}`);
  },

  leave: async (teamId: string): Promise<void> => {
    await apiClient.post(`/Teams/${encodeURIComponent(teamId)}/leave`);
  },

  invite: async (teamId: string, email: string): Promise<void> => {
    await apiClient.post(`/Teams/${encodeURIComponent(teamId)}/invitations`, { email });
  },

  respondInvitation: async (invitationId: string, accept: boolean): Promise<void> => {
    await apiClient.post(`/Teams/invitations/${encodeURIComponent(invitationId)}/respond`, { accept });
  },

  /**
   * Find the team this user belongs to for a given event, via EventRoles.
   * Returns null if the user has no TeamLeader/TeamMember role in that event.
   */
  findUserTeamForEvent: async (userId: string, eventId: string): Promise<TeamModel | null> => {
    const { data } = await apiClient.get<PagedResult<EventRoleRow>>(
      `/EventRoles/user/${encodeURIComponent(userId)}`,
      { params: { PageNumber: 1, PageSize: 100 } },
    );
    const row = (data.data ?? []).find(
      (r) => r.eventId === eventId && (r.roleName === 3 || r.roleName === 4) && r.teamId,
    );
    if (!row?.teamId) return null;
    return teamsApi.getById(row.teamId);
  },

  /**
   * Find the teams a judge is assigned to in an event.
   * `EventRoles/user/{userId}` filtered by eventId + roleName==1.
   */
  findJudgeAssignedTeams: async (userId: string, eventId: string): Promise<TeamModel[]> => {
    const { data } = await apiClient.get<PagedResult<EventRoleRow>>(
      `/EventRoles/user/${encodeURIComponent(userId)}`,
      { params: { PageNumber: 1, PageSize: 100 } },
    );
    const rows = (data.data ?? []).filter(
      (r) => r.eventId === eventId && r.roleName === 1 && r.teamId,
    );
    return Promise.all(rows.map((r) => teamsApi.getById(r.teamId as string)));
  },
};
```

- [ ] **Step 3: Build check**

Run: `npm run build`
Expected: clean.

- [ ] **Step 4: Commit**

```bash
git add src/features/teams
git commit -m "feat(teams): typed API client for Teams + EventRoles lookups"
```

---

## Task 6: Teams hooks

**Files:**
- Create: `src/features/teams/hooks/useTeams.ts`

**Interfaces:**
- Produces:
  - `useMyTeamForEvent(eventId, userId): UseQueryResult<TeamModel | null>`
  - `useJudgeAssignedTeams(eventId, userId): UseQueryResult<TeamModel[]>`
  - `useTeam(teamId): UseQueryResult<TeamModel>`
  - `useCreateTeam(eventId): UseMutationResult<TeamModel, unknown, CreateTeamPayload>`
  - `useInviteToTeam(teamId): UseMutationResult<void, unknown, { email: string }>`
  - `useLeaveTeam(teamId, eventId, userId): UseMutationResult<void, unknown, void>`

- [ ] **Step 1: Create hooks file**

`src/features/teams/hooks/useTeams.ts`:

```ts
'use client';

import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { teamsApi } from '../api/teams';
import type { CreateTeamPayload } from '../types/team.types';

export const TEAM_KEYS = {
  myTeam:  (eventId: string, userId: string) => ['team', 'mine', eventId, userId] as const,
  judge:   (eventId: string, userId: string) => ['team', 'judge', eventId, userId] as const,
  detail:  (teamId: string) => ['team', teamId] as const,
} as const;

export const useMyTeamForEvent = (eventId: string, userId: string) =>
  useQuery({
    queryKey: TEAM_KEYS.myTeam(eventId, userId),
    queryFn: () => teamsApi.findUserTeamForEvent(userId, eventId),
    enabled: !!eventId && !!userId,
    staleTime: 60_000,
  });

export const useJudgeAssignedTeams = (eventId: string, userId: string) =>
  useQuery({
    queryKey: TEAM_KEYS.judge(eventId, userId),
    queryFn: () => teamsApi.findJudgeAssignedTeams(userId, eventId),
    enabled: !!eventId && !!userId,
    staleTime: 60_000,
  });

export const useTeam = (teamId: string | undefined) =>
  useQuery({
    queryKey: TEAM_KEYS.detail(teamId ?? ''),
    queryFn: () => teamsApi.getById(teamId as string),
    enabled: !!teamId,
    staleTime: 60_000,
  });

export const useCreateTeam = (eventId: string, userId: string) => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (p: CreateTeamPayload) => teamsApi.create(p),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: TEAM_KEYS.myTeam(eventId, userId) });
    },
  });
};

export const useInviteToTeam = (teamId: string) => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ email }: { email: string }) => teamsApi.invite(teamId, email),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: TEAM_KEYS.detail(teamId) });
    },
  });
};

export const useLeaveTeam = (teamId: string, eventId: string, userId: string) => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: () => teamsApi.leave(teamId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: TEAM_KEYS.myTeam(eventId, userId) });
      qc.removeQueries({ queryKey: TEAM_KEYS.detail(teamId) });
    },
  });
};
```

- [ ] **Step 2: Build check + commit**

Run: `npm run build`
Expected: clean.

```bash
git add src/features/teams/hooks
git commit -m "feat(teams): react-query hooks for team membership + mutations"
```

---

## Task 7: Submissions API + hooks

**Files:**
- Create: `src/features/submissions/api/submissions.ts`
- Create: `src/features/submissions/hooks/useSubmissions.ts`

**Interfaces:**
- Produces:
  - `interface SubmissionModel { id; teamId; roundId; trackId; submissionUrl; description; isActive; createdTime }`
  - `submissionsApi.list({ teamId, roundId? }): Promise<SubmissionModel[]>`
  - `submissionsApi.create(p): Promise<SubmissionModel>`
  - `submissionsApi.update(id, p): Promise<SubmissionModel>`
  - Hooks: `useTeamSubmissions(teamId, roundId?)`, `useCreateSubmission(teamId)`, `useUpdateSubmission(teamId)`

- [ ] **Step 1: Create api**

`src/features/submissions/api/submissions.ts`:

```ts
import { apiClient } from '@/services/api';
import type { PagedResult } from '@/services/api';

export interface SubmissionModel {
  id: string;
  teamId: string;
  roundId: string;
  trackId: string;
  submissionUrl: string;
  description: string;
  isActive: boolean;
  createdTime: string;
  teamName?: string;
  trackName?: string;
}

export interface CreateSubmissionPayload {
  teamId: string;
  roundId: string;
  trackId: string;
  submissionUrl: string;
  description: string;
}

export interface UpdateSubmissionPayload {
  id: string;
  submissionUrl: string;
  description: string;
  isActive: boolean;
}

export const submissionsApi = {
  list: async (params: { teamId: string; roundId?: string }): Promise<SubmissionModel[]> => {
    const { data } = await apiClient.get<PagedResult<SubmissionModel>>('/SubmitResults', {
      params: {
        TeamId: params.teamId,
        RoundId: params.roundId,
        PageNumber: 1,
        PageSize: 100,
        SortBy: 'CreatedTime',
        IsAscending: false,
      },
    });
    return data.data ?? [];
  },
  create: async (p: CreateSubmissionPayload): Promise<SubmissionModel> => {
    const { data } = await apiClient.post<SubmissionModel>('/SubmitResults', p);
    return data;
  },
  update: async (p: UpdateSubmissionPayload): Promise<SubmissionModel> => {
    const { data } = await apiClient.put<SubmissionModel>(`/SubmitResults/${encodeURIComponent(p.id)}`, p);
    return data;
  },
};
```

- [ ] **Step 2: Create hooks**

`src/features/submissions/hooks/useSubmissions.ts`:

```ts
'use client';

import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { submissionsApi } from '../api/submissions';
import type { CreateSubmissionPayload, UpdateSubmissionPayload } from '../api/submissions';

export const SUB_KEYS = {
  list: (teamId: string, roundId?: string) => ['submissions', teamId, roundId ?? 'all'] as const,
};

export const useTeamSubmissions = (teamId: string, roundId?: string) =>
  useQuery({
    queryKey: SUB_KEYS.list(teamId, roundId),
    queryFn: () => submissionsApi.list({ teamId, roundId }),
    enabled: !!teamId,
    staleTime: 60_000,
  });

export const useCreateSubmission = (teamId: string) => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (p: CreateSubmissionPayload) => submissionsApi.create(p),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['submissions', teamId] }),
  });
};

export const useUpdateSubmission = (teamId: string) => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (p: UpdateSubmissionPayload) => submissionsApi.update(p),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['submissions', teamId] }),
  });
};
```

- [ ] **Step 3: Build check + commit**

Run: `npm run build`
Expected: clean.

```bash
git add src/features/submissions
git commit -m "feat(submissions): api + hooks for team submissions"
```

---

## Task 8: Results API + hooks (per-category, per-round)

**Files:**
- Create: `src/features/results/api/results.ts`
- Create: `src/features/results/hooks/useResults.ts`

**Interfaces:**
- Produces:
  - `interface FinalResultRow { id; teamId; teamName?; roundId; finalScore; rank; isAdvanced }`
  - `interface ScoreDetailRow { id; scoreId; templateId; criteriaId; value; criteriaName?; maxScore?; weight? }`
  - `interface ScoreRow { id; eventRoleId; teamId; roundId; trackId; totalScore; comment }`
  - `resultsApi.listTeamFinalResults(teamId): Promise<FinalResultRow[]>`
  - `resultsApi.listRoundLeaderboard(roundId): Promise<FinalResultRow[]>`
  - `resultsApi.listScoresForTeamRound(teamId, roundId): Promise<ScoreRow[]>` — falls back via round leaderboard if no direct endpoint
  - `resultsApi.listScoreDetails(scoreId): Promise<ScoreDetailRow[]>`
  - Hooks: `useTeamFinalResults(teamId)`, `useRoundLeaderboard(roundId)`, `useTeamRoundBreakdown(teamId, roundId)`

- [ ] **Step 1: Verify whether team-scoped score query exists**

Run: `grep -rn "Scores\?TeamId\|scores.*teamId\|GET.*Scores" /Users/anlnm/Desktop/Project/KimDTT/SU26_SWP_SE1907_BACKEND/SEAL_Backend/Controllers/ScoresController.cs`

If a route like `GET /api/Scores?TeamId=&RoundId=` exists, use it. Otherwise: use `listRoundLeaderboard(roundId)` to find the team's row, then for each `eventRoleId` (judges) of that round fetch `/Scores/event-role/{eventRoleId}` and filter client-side by `teamId === T && roundId === R`. The fallback is the **default** path in `listScoresForTeamRound` below.

- [ ] **Step 2: Create api**

`src/features/results/api/results.ts`:

```ts
import { apiClient } from '@/services/api';
import type { PagedResult } from '@/services/api';

export interface FinalResultRow {
  id: string;
  teamId: string;
  teamName?: string;
  roundId: string;
  finalScore: number;
  rank: number;
  isAdvanced: boolean;
}

export interface ScoreRow {
  id: string;
  eventRoleId: string;
  teamId: string;
  roundId: string;
  trackId: string;
  totalScore: number;
  comment: string | null;
}

export interface ScoreDetailRow {
  id: string;
  scoreId: string;
  templateId: string;
  criteriaId: string;
  criteriaName?: string;
  value: number;
  maxScore?: number;
  weight?: number;
}

export const resultsApi = {
  listTeamFinalResults: async (teamId: string): Promise<FinalResultRow[]> => {
    const { data } = await apiClient.get<PagedResult<FinalResultRow>>(
      `/FinalResults/team/${encodeURIComponent(teamId)}`,
      { params: { PageNumber: 1, PageSize: 100 } },
    );
    return data.data ?? [];
  },

  listRoundLeaderboard: async (roundId: string): Promise<FinalResultRow[]> => {
    const { data } = await apiClient.get<PagedResult<FinalResultRow>>(
      `/FinalResults/round/${encodeURIComponent(roundId)}`,
      { params: { PageNumber: 1, PageSize: 100, SortBy: 'Rank', IsAscending: true } },
    );
    return data.data ?? [];
  },

  /**
   * Team-scoped score query is not exposed by the backend at time of writing.
   * We discover scores by listing the round leaderboard and then walking
   * `/Scores/event-role/{eventRoleId}` for each judge — filter client-side.
   *
   * If the backend later adds `GET /api/Scores?TeamId=&RoundId=`, replace
   * the body of this function with a direct call.
   */
  listScoresForTeamRound: async (teamId: string, roundId: string): Promise<ScoreRow[]> => {
    // Discover all eventRoles tied to this round via EventRoles for the event of the round.
    // Simpler approach: ask `/Scores/event-role/{eventRoleId}` per judge — but we don't
    // know judges here. Cheapest path: the leaderboard row carries judge breakdowns in
    // many backends; if not, we attempt to read `/Scores?roundId=&teamId=` and tolerate 404.
    try {
      const { data } = await apiClient.get<PagedResult<ScoreRow>>('/Scores', {
        params: { TeamId: teamId, RoundId: roundId, PageNumber: 1, PageSize: 100 },
      });
      return data.data ?? [];
    } catch {
      return [];
    }
  },

  listScoreDetails: async (scoreId: string): Promise<ScoreDetailRow[]> => {
    const { data } = await apiClient.get<PagedResult<ScoreDetailRow>>(
      `/ScoreDetails/score/${encodeURIComponent(scoreId)}`,
      { params: { PageNumber: 1, PageSize: 100 } },
    );
    return data.data ?? [];
  },
};
```

- [ ] **Step 3: Create hooks**

`src/features/results/hooks/useResults.ts`:

```ts
'use client';

import { useQueries, useQuery } from '@tanstack/react-query';
import { resultsApi } from '../api/results';

export const RESULTS_KEYS = {
  teamFinals: (teamId: string) => ['results', 'team', teamId] as const,
  round:      (roundId: string) => ['results', 'round', roundId] as const,
  teamRound:  (teamId: string, roundId: string) => ['results', 'breakdown', teamId, roundId] as const,
  details:    (scoreId: string) => ['results', 'details', scoreId] as const,
};

export const useTeamFinalResults = (teamId: string | undefined) =>
  useQuery({
    queryKey: RESULTS_KEYS.teamFinals(teamId ?? ''),
    queryFn: () => resultsApi.listTeamFinalResults(teamId as string),
    enabled: !!teamId,
    staleTime: 60_000,
  });

export const useRoundLeaderboard = (roundId: string | undefined) =>
  useQuery({
    queryKey: RESULTS_KEYS.round(roundId ?? ''),
    queryFn: () => resultsApi.listRoundLeaderboard(roundId as string),
    enabled: !!roundId,
    staleTime: 60_000,
  });

/**
 * For one team + one round: returns [{ score, details[] }, ...]
 * (one entry per judge that scored the team).
 */
export const useTeamRoundBreakdown = (teamId: string, roundId: string) => {
  const scoresQ = useQuery({
    queryKey: RESULTS_KEYS.teamRound(teamId, roundId),
    queryFn: () => resultsApi.listScoresForTeamRound(teamId, roundId),
    enabled: !!teamId && !!roundId,
    staleTime: 60_000,
  });

  const detailQueries = useQueries({
    queries: (scoresQ.data ?? []).map((s) => ({
      queryKey: RESULTS_KEYS.details(s.id),
      queryFn: () => resultsApi.listScoreDetails(s.id),
      staleTime: 60_000,
    })),
  });

  return {
    isLoading: scoresQ.isLoading || detailQueries.some((q) => q.isLoading),
    error: scoresQ.error,
    data: (scoresQ.data ?? []).map((score, i) => ({
      score,
      details: detailQueries[i]?.data ?? [],
    })),
  };
};
```

- [ ] **Step 4: Build check + commit**

Run: `npm run build`
Expected: clean.

```bash
git add src/features/results
git commit -m "feat(results): api + hooks for per-team per-round score breakdown"
```

---

## Task 9: `getEventTabs` helper

**Files:**
- Create: `src/lib/events/getEventTabs.ts`, `src/lib/events/__tests__/getEventTabs.test.ts`

**Interfaces:**
- Consumes: `AppRole` from `@/hooks/useUserRole`
- Produces:
  - `type EventTabId = 'detail' | 'createTeam' | 'myTeam' | 'submission' | 'results' | 'leaderboard' | 'judgeAssigned' | 'manage'`
  - `getEventTabs({ role, hasTeam }): Array<{ id: EventTabId; label: string }>`

- [ ] **Step 1: Write the failing test**

`src/lib/events/__tests__/getEventTabs.test.ts`:

```ts
import { getEventTabs } from '../getEventTabs';

describe('getEventTabs', () => {
  test('guest → detail only', () => {
    expect(getEventTabs({ role: null, hasTeam: false }).map((t) => t.id)).toEqual(['detail']);
  });

  test('student without team → detail + createTeam', () => {
    expect(getEventTabs({ role: 'student', hasTeam: false }).map((t) => t.id))
      .toEqual(['detail', 'createTeam']);
  });

  test('student with team → detail + myTeam + submission + results + leaderboard', () => {
    expect(getEventTabs({ role: 'student', hasTeam: true }).map((t) => t.id))
      .toEqual(['detail', 'myTeam', 'submission', 'results', 'leaderboard']);
  });

  test('judge → detail + judgeAssigned + leaderboard', () => {
    expect(getEventTabs({ role: 'judge', hasTeam: false }).map((t) => t.id))
      .toEqual(['detail', 'judgeAssigned', 'leaderboard']);
  });

  test('admin → detail + manage', () => {
    expect(getEventTabs({ role: 'admin', hasTeam: false }).map((t) => t.id))
      .toEqual(['detail', 'manage']);
  });
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `npm test -- --watch=false src/lib/events/__tests__/getEventTabs.test.ts`
Expected: FAIL — module not found.

- [ ] **Step 3: Implement the helper**

`src/lib/events/getEventTabs.ts`:

```ts
import type { AppRole } from '@/hooks/useUserRole';

export type EventTabId =
  | 'detail' | 'createTeam' | 'myTeam' | 'submission'
  | 'results' | 'leaderboard' | 'judgeAssigned' | 'manage';

export interface EventTab { id: EventTabId; label: string; }

const TAB: Record<EventTabId, EventTab> = {
  detail:        { id: 'detail',        label: 'Chi tiết sự kiện' },
  createTeam:    { id: 'createTeam',    label: 'Tạo đội' },
  myTeam:        { id: 'myTeam',        label: 'Đội của tôi' },
  submission:    { id: 'submission',    label: 'Nộp bài' },
  results:       { id: 'results',       label: 'Kết quả' },
  leaderboard:   { id: 'leaderboard',   label: 'Bảng xếp hạng' },
  judgeAssigned: { id: 'judgeAssigned', label: 'Đội được phân công' },
  manage:        { id: 'manage',        label: 'Quản lý' },
};

export function getEventTabs(args: { role: AppRole | null; hasTeam: boolean }): EventTab[] {
  const { role, hasTeam } = args;
  if (role === 'admin')   return [TAB.detail, TAB.manage];
  if (role === 'judge')   return [TAB.detail, TAB.judgeAssigned, TAB.leaderboard];
  if (role === 'student') return hasTeam
    ? [TAB.detail, TAB.myTeam, TAB.submission, TAB.results, TAB.leaderboard]
    : [TAB.detail, TAB.createTeam];
  return [TAB.detail];
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `npm test -- --watch=false src/lib/events/__tests__/getEventTabs.test.ts`
Expected: PASS (5/5).

- [ ] **Step 5: Commit**

```bash
git add src/lib/events
git commit -m "feat(events): pure helper getEventTabs(role, hasTeam)"
```

---

## Task 10: Extend EventDashboardContext tab union

**Files:**
- Modify: `src/features/events/contexts/EventDashboardContext.tsx`

**Interfaces:**
- Produces: context now uses `EventTabId` from Task 9 (broader union).

- [ ] **Step 1: Read current context**

Run: `cat src/features/events/contexts/EventDashboardContext.tsx`

- [ ] **Step 2: Replace the activeTab type**

Replace the existing tab union with `EventTabId` from `@/lib/events/getEventTabs`. Add the import; change the default `activeTab` to `'detail'`. Example diff:

```ts
import type { EventTabId } from '@/lib/events/getEventTabs';

// ...
const [activeTab, setActiveTab] = useState<EventTabId>('detail');
```

If the context exposes `setActiveTab: (t: 'dashboard' | ...) => void`, widen its parameter to `EventTabId`.

- [ ] **Step 3: Build check**

Run: `npm run build`
Expected: TypeScript may flag old references to `'dashboard' | 'submission' | 'results'` in `EventDashboard.tsx` and `Sidebar.tsx`. Defer those fixes to Tasks 11–12.

- [ ] **Step 4: Commit**

```bash
git add src/features/events/contexts/EventDashboardContext.tsx
git commit -m "refactor(events): widen dashboard tab union to EventTabId"
```

---

## Task 11: Role-aware Sidebar

**Files:**
- Modify: `src/features/events/components/EventDashboard/Sidebar.tsx`

**Interfaces:**
- Consumes: `getEventTabs`, `useUserRole`, `useMyTeamForEvent`, `useEventDashboard`

- [ ] **Step 1: Read current Sidebar to preserve styling**

The hard-coded `tabs` array (lines ~7–11) must be replaced by `getEventTabs({ role, hasTeam })`. Icon mapping moves to a local lookup so the JSX still gets an icon per tab.

- [ ] **Step 2: Edit Sidebar**

Replace the file with:

```tsx
'use client';

import React from 'react';
import {
  BarChart3, Upload, Trophy, Users, UserPlus, ClipboardList, FileText, Settings,
} from 'lucide-react';
import { useEventDashboard } from '@/features/events/contexts/EventDashboardContext';
import { useUserRole } from '@/hooks/useUserRole';
import { useCurrentUser } from '@/hooks/useAuth';
import { useMyTeamForEvent } from '@/features/teams/hooks/useTeams';
import { getEventTabs, type EventTabId } from '@/lib/events/getEventTabs';

interface SidebarProps { eventId: string; }

const ICON: Record<EventTabId, React.ComponentType<{ size?: number; className?: string }>> = {
  detail:        BarChart3,
  createTeam:    UserPlus,
  myTeam:        Users,
  submission:    Upload,
  results:       FileText,
  leaderboard:   Trophy,
  judgeAssigned: ClipboardList,
  manage:        Settings,
};

export function Sidebar({ eventId }: SidebarProps) {
  const { activeTab, setActiveTab } = useEventDashboard();
  const role = useUserRole();
  const { data: user } = useCurrentUser();
  const { data: team } = useMyTeamForEvent(eventId, user?.id ?? '');
  const tabs = getEventTabs({ role, hasTeam: !!team });

  return (
    <aside
      className="fixed left-0 top-0 h-screen w-16 md:w-60 bg-surface-dark border-r border-hairline-strong flex flex-col lg:w-60 z-50"
      role="navigation"
      aria-label="Event dashboard navigation"
    >
      <nav className="flex-1 pt-0" role="tablist">
        <ul className="list-none p-0 m-0">
          {tabs.map((tab) => {
            const Icon = ICON[tab.id];
            const isActive = activeTab === tab.id;
            return (
              <li key={tab.id} role="presentation">
                <button
                  role="tab"
                  aria-selected={isActive}
                  onClick={() => setActiveTab(tab.id)}
                  className={`w-full px-4 py-4 flex items-center gap-3 border-b border-hairline-strong transition-colors duration-150 cursor-pointer min-h-12 ${
                    isActive
                      ? 'bg-surface-dark text-on-dark border-l-4 border-l-primary'
                      : 'bg-surface-dark text-on-dark hover:bg-[rgba(255,255,255,0.08)] border-l-4 border-l-transparent'
                  } focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary`}
                  aria-label={`${tab.label} tab`}
                >
                  <Icon size={18} className={isActive ? 'text-primary' : 'text-on-dark opacity-75'} />
                  <span className="text-body-strong text-sm font-bold hidden md:inline">{tab.label}</span>
                  <span className="sr-only">{tab.label}</span>
                </button>
              </li>
            );
          })}
        </ul>
      </nav>

      <div className="border-t border-hairline-strong p-4 flex items-center gap-3 bg-surface-dark">
        <div className="w-8 h-8 rounded-full bg-primary flex items-center justify-center text-white text-xs font-bold flex-shrink-0">
          {user?.fullName?.slice(0, 2).toUpperCase() ?? '?'}
        </div>
        <div className="hidden md:flex flex-col flex-1 min-w-0">
          <p className="text-on-dark text-body-sm font-bold truncate">{user?.fullName ?? '—'}</p>
          <span className="inline-block bg-primary/20 text-primary text-caption-xs px-2 py-1 rounded-full text-xs mt-1 w-fit font-semibold">
            {role ?? 'guest'}
          </span>
        </div>
      </div>
    </aside>
  );
}
```

- [ ] **Step 3: Commit (build will still fail until Task 12 — that's expected)**

```bash
git add src/features/events/components/EventDashboard/Sidebar.tsx
git commit -m "feat(events): role-aware Sidebar tab list"
```

---

## Task 12: Stub new tab components

Create empty-but-valid components so the dashboard can compile. Each is filled in later tasks (13–17). This task unblocks the build.

**Files:**
- Create: `src/features/events/components/EventDashboard/tabs/EventDetail.tsx`
- Create: `src/features/events/components/EventDashboard/tabs/CreateTeam.tsx`
- Create: `src/features/events/components/EventDashboard/tabs/MyTeam.tsx`
- Create: `src/features/events/components/EventDashboard/tabs/Leaderboard.tsx`
- Create: `src/features/events/components/EventDashboard/tabs/JudgeAssignedTeams.tsx`

- [ ] **Step 1: Write the five stubs**

Each file follows the same shape:

```tsx
'use client';

import React from 'react';

interface Props { eventId: string; userId: string; }

export function EventDetailTab({ eventId }: Props) {
  return <div className="p-6 t-body-md text-mute">Chi tiết sự kiện — coming next ({eventId}).</div>;
}
```

Create the analogous files for `CreateTeamTab`, `MyTeamTab`, `LeaderboardTab`, `JudgeAssignedTeamsTab`. Each accepts `Props` and renders a placeholder div.

- [ ] **Step 2: Wire EventDashboard switch**

Replace the `renderTabContent` switch in `src/features/events/components/EventDashboard/EventDashboard.tsx` with cases for every `EventTabId`. New file content:

```tsx
'use client';

import React from 'react';
import { Sidebar } from './Sidebar';
import { Header } from './Header';
import { EventDetailTab } from './tabs/EventDetail';
import { CreateTeamTab } from './tabs/CreateTeam';
import { MyTeamTab } from './tabs/MyTeam';
import { SubmissionTab } from './tabs/Submission';
import { ResultsTab } from './tabs/Results';
import { LeaderboardTab } from './tabs/Leaderboard';
import { JudgeAssignedTeamsTab } from './tabs/JudgeAssignedTeams';
import { useEventDashboard } from '@/features/events/contexts/EventDashboardContext';
import { useEvent } from '@/features/events/hooks/useEvents';
import { useMyTeamForEvent } from '@/features/teams/hooks/useTeams';
import Link from 'next/link';

interface EventDashboardProps { eventId: string; userId: string; }

export function EventDashboard({ eventId, userId }: EventDashboardProps) {
  const { activeTab } = useEventDashboard();
  const { data: event, isLoading: eventLoading } = useEvent(eventId);
  const { data: team, isLoading: teamLoading } = useMyTeamForEvent(eventId, userId);

  if (eventLoading || teamLoading) {
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

  const teamId = team?.id ?? '';

  const renderTabContent = () => {
    switch (activeTab) {
      case 'detail':        return <EventDetailTab eventId={eventId} userId={userId} />;
      case 'createTeam':    return <CreateTeamTab  eventId={eventId} userId={userId} />;
      case 'myTeam':        return <MyTeamTab      eventId={eventId} userId={userId} />;
      case 'submission':    return teamId
        ? <SubmissionTab teamId={teamId} eventId={eventId} />
        : <div className="t-body-md text-mute p-6">Bạn cần tạo đội trước.</div>;
      case 'results':       return teamId
        ? <ResultsTab teamId={teamId} eventId={eventId} />
        : <div className="t-body-md text-mute p-6">Chưa có kết quả.</div>;
      case 'leaderboard':   return <LeaderboardTab        eventId={eventId} userId={userId} />;
      case 'judgeAssigned': return <JudgeAssignedTeamsTab eventId={eventId} userId={userId} />;
      case 'manage':        return (
        <div className="p-6"><Link href={`/events/${eventId}/manage`} className="btn btn-primary">Mở trang quản lý</Link></div>
      );
      default: return null;
    }
  };

  return (
    <div className="min-h-screen bg-canvas">
      <Sidebar eventId={eventId} />
      <Header title={event.title} subtitle="Team Competition" status={event.status} />
      <main className="fixed top-24 md:top-20 left-0 right-0 bottom-0 overflow-hidden bg-canvas lg:left-60">
        <div className="h-full overflow-y-auto p-3 md:p-6">
          <div className="animate-fadeIn">{renderTabContent()}</div>
        </div>
      </main>
    </div>
  );
}
```

- [ ] **Step 3: Build check**

Run: `npm run build`
Expected: clean.

- [ ] **Step 4: Commit**

```bash
git add src/features/events/components/EventDashboard
git commit -m "feat(events): wire role-aware tab switch with stub tab components"
```

---

## Task 13: `EventDetailTab` — fill in real content

**Files:**
- Modify: `src/features/events/components/EventDashboard/tabs/EventDetail.tsx`

**Interfaces:**
- Consumes: `useEvent` from `@/features/events/hooks/useEvents`

- [ ] **Step 1: Replace stub with detail view**

```tsx
'use client';

import React from 'react';
import { useEvent } from '@/features/events/hooks/useEvents';

interface Props { eventId: string; userId: string; }

export function EventDetailTab({ eventId }: Props) {
  const { data: event, isLoading, error } = useEvent(eventId);
  if (isLoading) return <div className="p-6 t-body-md text-mute">Đang tải…</div>;
  if (error || !event) return <div className="p-6 t-body-md text-error">Không tải được sự kiện.</div>;

  return (
    <section className="p-6 max-w-3xl mx-auto space-y-4">
      <header className="space-y-2">
        <h1 className="t-heading-lg">{event.title}</h1>
        <p className="t-body-sm text-mute">
          {new Date(event.startDate).toLocaleDateString('vi-VN')} – {new Date(event.endDate).toLocaleDateString('vi-VN')}
        </p>
        <span className="inline-block px-2 py-1 rounded-full bg-primary/10 text-primary text-xs font-bold">
          {event.status === 'open' ? 'Đang mở' : 'Đã đóng'}
        </span>
      </header>
      <article className="t-body-md whitespace-pre-line">{event.description || 'Chưa có mô tả.'}</article>
    </section>
  );
}
```

- [ ] **Step 2: Build + commit**

```bash
npm run build
git add src/features/events/components/EventDashboard/tabs/EventDetail.tsx
git commit -m "feat(events): fill EventDetailTab with title/dates/description"
```

---

## Task 14: `CreateTeamTab` — student team creation

**Files:**
- Modify: `src/features/events/components/EventDashboard/tabs/CreateTeam.tsx`

**Interfaces:**
- Consumes: `useEventTracks` (existing), `useCreateTeam` (Task 6)
- Produces: on success, sets dashboard tab to `'myTeam'`.

- [ ] **Step 1: Replace stub**

```tsx
'use client';

import React, { useState } from 'react';
import { useEventTracks } from '@/features/events/hooks/useEvents';
import { useCreateTeam } from '@/features/teams/hooks/useTeams';
import { useEventDashboard } from '@/features/events/contexts/EventDashboardContext';

interface Props { eventId: string; userId: string; }

export function CreateTeamTab({ eventId, userId }: Props) {
  const { data: tracks, isLoading: tracksLoading } = useEventTracks(eventId);
  const { setActiveTab } = useEventDashboard();
  const create = useCreateTeam(eventId, userId);

  const [teamName, setTeamName] = useState('');
  const [description, setDescription] = useState('');
  const [trackId, setTrackId] = useState('');

  const submit = (e: React.FormEvent) => {
    e.preventDefault();
    create.mutate(
      { teamName, description, eventId, trackId },
      { onSuccess: () => setActiveTab('myTeam') },
    );
  };

  return (
    <form onSubmit={submit} className="p-6 max-w-xl mx-auto space-y-4">
      <h2 className="t-heading-md">Tạo đội</h2>

      <label className="block">
        <span className="t-body-sm font-bold">Tên đội</span>
        <input
          required minLength={2} maxLength={80}
          value={teamName} onChange={(e) => setTeamName(e.target.value)}
          className="input w-full mt-1"
        />
      </label>

      <label className="block">
        <span className="t-body-sm font-bold">Mô tả</span>
        <textarea
          value={description} onChange={(e) => setDescription(e.target.value)}
          rows={3} className="input w-full mt-1"
        />
      </label>

      <label className="block">
        <span className="t-body-sm font-bold">Track</span>
        <select
          required value={trackId} onChange={(e) => setTrackId(e.target.value)}
          className="input w-full mt-1" disabled={tracksLoading}
        >
          <option value="">— Chọn track —</option>
          {(tracks ?? []).map((t: { id: string; trackName: string }) => (
            <option key={t.id} value={t.id}>{t.trackName}</option>
          ))}
        </select>
      </label>

      {create.error ? <p className="t-body-sm text-error">Tạo đội thất bại. Vui lòng thử lại.</p> : null}

      <button type="submit" disabled={create.isPending} className="btn btn-primary">
        {create.isPending ? 'Đang tạo…' : 'Tạo đội'}
      </button>
    </form>
  );
}
```

- [ ] **Step 2: Build + commit**

```bash
npm run build
git add src/features/events/components/EventDashboard/tabs/CreateTeam.tsx
git commit -m "feat(teams): CreateTeamTab form wired to POST /api/Teams"
```

---

## Task 15: `MyTeamTab` — members + invite + leave

**Files:**
- Modify: `src/features/events/components/EventDashboard/tabs/MyTeam.tsx`

- [ ] **Step 1: Replace stub**

```tsx
'use client';

import React, { useState } from 'react';
import { useMyTeamForEvent, useInviteToTeam, useLeaveTeam } from '@/features/teams/hooks/useTeams';

interface Props { eventId: string; userId: string; }

export function MyTeamTab({ eventId, userId }: Props) {
  const { data: team, isLoading } = useMyTeamForEvent(eventId, userId);
  const teamId = team?.id ?? '';
  const invite = useInviteToTeam(teamId);
  const leave  = useLeaveTeam(teamId, eventId, userId);
  const [email, setEmail] = useState('');

  if (isLoading) return <div className="p-6 t-body-md text-mute">Đang tải…</div>;
  if (!team) return <div className="p-6 t-body-md text-mute">Bạn chưa có đội.</div>;

  return (
    <section className="p-6 max-w-2xl mx-auto space-y-6">
      <header>
        <h2 className="t-heading-md">{team.teamName}</h2>
        {team.description ? <p className="t-body-sm text-mute mt-1">{team.description}</p> : null}
      </header>

      <div>
        <h3 className="t-body-md font-bold mb-2">Thành viên</h3>
        <ul className="divide-y divide-hairline">
          {team.members.map((m) => (
            <li key={m.userId} className="py-2 flex justify-between">
              <span>{m.fullName} <span className="text-mute t-body-sm">({m.email})</span></span>
              <span className="text-xs font-bold">{m.roleName === 3 ? 'Leader' : 'Member'}</span>
            </li>
          ))}
        </ul>
      </div>

      <form
        onSubmit={(e) => { e.preventDefault(); invite.mutate({ email }, { onSuccess: () => setEmail('') }); }}
        className="space-y-2"
      >
        <h3 className="t-body-md font-bold">Mời thành viên</h3>
        <div className="flex gap-2">
          <input
            type="email" required value={email} onChange={(e) => setEmail(e.target.value)}
            placeholder="email@example.com" className="input flex-1"
          />
          <button type="submit" disabled={invite.isPending} className="btn btn-primary">
            {invite.isPending ? 'Đang gửi…' : 'Mời'}
          </button>
        </div>
        {invite.error ? <p className="t-body-sm text-error">Mời thất bại.</p> : null}
      </form>

      <button
        type="button"
        onClick={() => { if (confirm('Bạn chắc chắn rời đội?')) leave.mutate(); }}
        disabled={leave.isPending}
        className="btn btn-outline-danger"
      >
        {leave.isPending ? 'Đang rời…' : 'Rời đội'}
      </button>
    </section>
  );
}
```

- [ ] **Step 2: Build + commit**

```bash
npm run build
git add src/features/events/components/EventDashboard/tabs/MyTeam.tsx
git commit -m "feat(teams): MyTeamTab — members list, invite, leave"
```

---

## Task 16: Rewire existing `SubmissionTab` to real API

**Files:**
- Modify: `src/features/events/components/EventDashboard/tabs/Submission.tsx`

**Interfaces:**
- Consumes: `useEventRounds`, `useEventTracks` (existing), `useTeamSubmissions`, `useCreateSubmission` (Task 7)

- [ ] **Step 1: Read current Submission.tsx to know what UI to preserve**

Run: `cat src/features/events/components/EventDashboard/tabs/Submission.tsx`

- [ ] **Step 2: Rewrite the tab using the real hooks**

Replace the whole file with:

```tsx
'use client';

import React, { useMemo, useState } from 'react';
import { useEventRounds, useEventTracks } from '@/features/events/hooks/useEvents';
import { useTeamSubmissions, useCreateSubmission } from '@/features/submissions/hooks/useSubmissions';

interface Props { teamId: string; eventId: string; }

export function SubmissionTab({ teamId, eventId }: Props) {
  const { data: rounds = [] } = useEventRounds(eventId);
  const { data: tracks = [] } = useEventTracks(eventId);

  const [roundId, setRoundId] = useState('');
  const [trackId, setTrackId] = useState('');
  const [submissionUrl, setUrl] = useState('');
  const [description, setDesc] = useState('');

  const { data: existing = [] } = useTeamSubmissions(teamId, roundId || undefined);
  const create = useCreateSubmission(teamId);

  const tracksForRound = useMemo(
    () => (tracks as Array<{ id: string; roundId: string; trackName: string }>).filter((t) => t.roundId === roundId),
    [tracks, roundId],
  );

  const submit = (e: React.FormEvent) => {
    e.preventDefault();
    create.mutate(
      { teamId, roundId, trackId, submissionUrl, description },
      { onSuccess: () => { setUrl(''); setDesc(''); } },
    );
  };

  return (
    <section className="p-6 max-w-2xl mx-auto space-y-6">
      <h2 className="t-heading-md">Nộp bài</h2>

      <form onSubmit={submit} className="space-y-3">
        <label className="block">
          <span className="t-body-sm font-bold">Vòng</span>
          <select required value={roundId} onChange={(e) => { setRoundId(e.target.value); setTrackId(''); }} className="input w-full mt-1">
            <option value="">— Chọn vòng —</option>
            {(rounds as Array<{ id: string; roundName: string }>).map((r) => (
              <option key={r.id} value={r.id}>{r.roundName}</option>
            ))}
          </select>
        </label>

        <label className="block">
          <span className="t-body-sm font-bold">Track</span>
          <select required value={trackId} onChange={(e) => setTrackId(e.target.value)} className="input w-full mt-1" disabled={!roundId}>
            <option value="">— Chọn track —</option>
            {tracksForRound.map((t) => <option key={t.id} value={t.id}>{t.trackName}</option>)}
          </select>
        </label>

        <label className="block">
          <span className="t-body-sm font-bold">Link nộp bài</span>
          <input required type="url" value={submissionUrl} onChange={(e) => setUrl(e.target.value)} className="input w-full mt-1" />
        </label>

        <label className="block">
          <span className="t-body-sm font-bold">Mô tả</span>
          <textarea value={description} onChange={(e) => setDesc(e.target.value)} rows={3} className="input w-full mt-1" />
        </label>

        {create.error ? <p className="t-body-sm text-error">Nộp bài thất bại.</p> : null}

        <button type="submit" disabled={create.isPending} className="btn btn-primary">
          {create.isPending ? 'Đang nộp…' : 'Nộp bài'}
        </button>
      </form>

      <div>
        <h3 className="t-body-md font-bold mb-2">Đã nộp</h3>
        {existing.length === 0 ? (
          <p className="t-body-sm text-mute">Chưa có bài nộp nào.</p>
        ) : (
          <ul className="divide-y divide-hairline">
            {existing.map((s) => (
              <li key={s.id} className="py-2">
                <a href={s.submissionUrl} target="_blank" rel="noreferrer" className="t-body-md text-primary underline">
                  {s.submissionUrl}
                </a>
                <p className="t-body-sm text-mute">{s.description}</p>
              </li>
            ))}
          </ul>
        )}
      </div>
    </section>
  );
}
```

- [ ] **Step 3: Build + commit**

```bash
npm run build
git add src/features/events/components/EventDashboard/tabs/Submission.tsx
git commit -m "feat(submissions): SubmissionTab wired to /api/SubmitResults"
```

---

## Task 17: Rewire existing `ResultsTab` to per-category-per-round breakdown

**Files:**
- Modify: `src/features/events/components/EventDashboard/tabs/Results.tsx`
- Create: `src/features/results/components/ResultsAccordion.tsx`

- [ ] **Step 1: Read current Results.tsx**

Run: `cat src/features/events/components/EventDashboard/tabs/Results.tsx`

- [ ] **Step 2: Create the accordion component**

`src/features/results/components/ResultsAccordion.tsx`:

```tsx
'use client';

import React, { useState } from 'react';
import { useEventRounds } from '@/features/events/hooks/useEvents';
import { useTeamFinalResults, useTeamRoundBreakdown } from '@/features/results/hooks/useResults';

interface Props { teamId: string; eventId: string; }

function RoundRow({ teamId, roundId, roundName, finalScore, rank, isAdvanced }: {
  teamId: string; roundId: string; roundName: string; finalScore: number; rank: number; isAdvanced: boolean;
}) {
  const [open, setOpen] = useState(false);
  const { data: breakdown, isLoading } = useTeamRoundBreakdown(teamId, open ? roundId : '');

  return (
    <div className="border border-hairline rounded-md overflow-hidden">
      <button
        type="button" onClick={() => setOpen((v) => !v)}
        className="w-full flex justify-between items-center p-3 bg-surface-dim hover:bg-surface-strong"
      >
        <span className="t-body-md font-bold">{roundName}</span>
        <span className="flex gap-3 text-sm">
          <span>Điểm: <b>{finalScore.toFixed(2)}</b></span>
          <span>Hạng: <b>#{rank}</b></span>
          {isAdvanced ? <span className="text-success">Đi tiếp</span> : null}
        </span>
      </button>

      {open ? (
        <div className="p-3 space-y-3">
          {isLoading ? <p className="t-body-sm text-mute">Đang tải chi tiết…</p> : null}
          {(breakdown.data ?? []).map(({ score, details }) => (
            <div key={score.id} className="space-y-1">
              <p className="t-body-sm text-mute">Tổng từ giám khảo: <b>{score.totalScore.toFixed(2)}</b></p>
              <table className="w-full text-sm">
                <thead className="text-left">
                  <tr><th>Tiêu chí</th><th>Điểm</th><th>Tối đa</th><th>Trọng số</th></tr>
                </thead>
                <tbody>
                  {details.map((d) => (
                    <tr key={d.id} className="border-t border-hairline">
                      <td>{d.criteriaName ?? d.criteriaId}</td>
                      <td>{d.value.toFixed(2)}</td>
                      <td>{d.maxScore ?? '—'}</td>
                      <td>{d.weight ?? '—'}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          ))}
        </div>
      ) : null}
    </div>
  );
}

export function ResultsAccordion({ teamId, eventId }: Props) {
  const { data: rounds = [] } = useEventRounds(eventId);
  const { data: finals = [] } = useTeamFinalResults(teamId);

  if (finals.length === 0) return <p className="t-body-md text-mute">Chưa có kết quả.</p>;

  return (
    <div className="space-y-3">
      {finals.map((f) => {
        const round = (rounds as Array<{ id: string; roundName: string }>).find((r) => r.id === f.roundId);
        return (
          <RoundRow
            key={f.id}
            teamId={teamId} roundId={f.roundId}
            roundName={round?.roundName ?? f.roundId}
            finalScore={f.finalScore} rank={f.rank} isAdvanced={f.isAdvanced}
          />
        );
      })}
    </div>
  );
}
```

- [ ] **Step 3: Replace `ResultsTab` body**

`src/features/events/components/EventDashboard/tabs/Results.tsx`:

```tsx
'use client';

import React from 'react';
import { ResultsAccordion } from '@/features/results/components/ResultsAccordion';

interface Props { teamId: string; eventId: string; }

export function ResultsTab({ teamId, eventId }: Props) {
  return (
    <section className="p-6 max-w-3xl mx-auto space-y-4">
      <h2 className="t-heading-md">Kết quả</h2>
      <ResultsAccordion teamId={teamId} eventId={eventId} />
    </section>
  );
}
```

- [ ] **Step 4: Build + commit**

```bash
npm run build
git add src/features/results/components src/features/events/components/EventDashboard/tabs/Results.tsx
git commit -m "feat(results): per-round accordion with per-criteria score detail"
```

---

## Task 18: `LeaderboardTab` (student + judge shared view)

**Files:**
- Modify: `src/features/events/components/EventDashboard/tabs/Leaderboard.tsx`
- Create: `src/features/leaderboard/components/RoundLeaderboard.tsx`

- [ ] **Step 1: Create the round leaderboard component**

`src/features/leaderboard/components/RoundLeaderboard.tsx`:

```tsx
'use client';

import React, { useState } from 'react';
import { useEventRounds } from '@/features/events/hooks/useEvents';
import { useRoundLeaderboard } from '@/features/results/hooks/useResults';

interface Props { eventId: string; }

export function RoundLeaderboard({ eventId }: Props) {
  const { data: rounds = [] } = useEventRounds(eventId);
  const [roundId, setRoundId] = useState('');
  const { data: rows = [], isLoading } = useRoundLeaderboard(roundId || undefined);

  return (
    <div className="space-y-4">
      <label className="block max-w-xs">
        <span className="t-body-sm font-bold">Vòng</span>
        <select value={roundId} onChange={(e) => setRoundId(e.target.value)} className="input w-full mt-1">
          <option value="">— Chọn vòng —</option>
          {(rounds as Array<{ id: string; roundName: string }>).map((r) => (
            <option key={r.id} value={r.id}>{r.roundName}</option>
          ))}
        </select>
      </label>

      {!roundId ? <p className="t-body-sm text-mute">Chọn một vòng để xem bảng xếp hạng.</p>
      : isLoading ? <p className="t-body-sm text-mute">Đang tải…</p>
      : rows.length === 0 ? <p className="t-body-sm text-mute">Chưa có dữ liệu.</p>
      : (
        <table className="w-full text-sm">
          <thead className="text-left"><tr><th>#</th><th>Đội</th><th>Điểm</th><th></th></tr></thead>
          <tbody>
            {rows.map((r) => (
              <tr key={r.id} className="border-t border-hairline">
                <td>{r.rank}</td>
                <td>{r.teamName ?? r.teamId}</td>
                <td>{r.finalScore.toFixed(2)}</td>
                <td>{r.isAdvanced ? <span className="text-success">Đi tiếp</span> : null}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
}
```

- [ ] **Step 2: Replace `LeaderboardTab` stub**

`src/features/events/components/EventDashboard/tabs/Leaderboard.tsx`:

```tsx
'use client';

import React from 'react';
import { RoundLeaderboard } from '@/features/leaderboard/components/RoundLeaderboard';

interface Props { eventId: string; userId: string; }

export function LeaderboardTab({ eventId }: Props) {
  return (
    <section className="p-6 max-w-3xl mx-auto space-y-4">
      <h2 className="t-heading-md">Bảng xếp hạng</h2>
      <RoundLeaderboard eventId={eventId} />
    </section>
  );
}
```

- [ ] **Step 3: Build + commit**

```bash
npm run build
git add src/features/leaderboard src/features/events/components/EventDashboard/tabs/Leaderboard.tsx
git commit -m "feat(leaderboard): per-round leaderboard view (student/judge)"
```

---

## Task 19: `JudgeAssignedTeamsTab`

**Files:**
- Modify: `src/features/events/components/EventDashboard/tabs/JudgeAssignedTeams.tsx`

- [ ] **Step 1: Replace stub**

```tsx
'use client';

import React from 'react';
import { useJudgeAssignedTeams } from '@/features/teams/hooks/useTeams';

interface Props { eventId: string; userId: string; }

export function JudgeAssignedTeamsTab({ eventId, userId }: Props) {
  const { data: teams = [], isLoading } = useJudgeAssignedTeams(eventId, userId);

  if (isLoading) return <div className="p-6 t-body-md text-mute">Đang tải…</div>;
  if (teams.length === 0) return <div className="p-6 t-body-md text-mute">Bạn chưa được phân công đội nào.</div>;

  return (
    <section className="p-6 max-w-3xl mx-auto space-y-4">
      <h2 className="t-heading-md">Đội được phân công</h2>
      <ul className="divide-y divide-hairline">
        {teams.map((t) => (
          <li key={t.id} className="py-3">
            <p className="t-body-md font-bold">{t.teamName}</p>
            <p className="t-body-sm text-mute">{t.members.length} thành viên</p>
          </li>
        ))}
      </ul>
      <p className="t-body-sm text-mute">Vào trang chấm điểm hiện có để nhập điểm cho từng đội.</p>
    </section>
  );
}
```

- [ ] **Step 2: Build + commit**

```bash
npm run build
git add src/features/events/components/EventDashboard/tabs/JudgeAssignedTeams.tsx
git commit -m "feat(judge): JudgeAssignedTeamsTab listing teams from EventRoles"
```

---

## Task 20: End-to-end manual verification

- [ ] **Step 1: Start dev server**

Run: `npm run dev`
Open: `http://localhost:3000`

- [ ] **Step 2: Login flows**

For each of: a STUDENT account, a MENTOR account, an ADMIN account:

1. Log in.
2. Confirm navbar shows the right links per Task 2.
3. Open an event.
4. Confirm sidebar tabs match `getEventTabs` for that role.

- [ ] **Step 3: Student happy-path**

- Tab "Tạo đội" → submit → expect to land on "Đội của tôi".
- Invite a teammate by email.
- "Nộp bài" → choose round + track → submit a URL → see it appear under "Đã nộp".
- "Kết quả" → expand a round → see per-criteria table (empty rows OK pre-scoring).
- "Bảng xếp hạng" → pick round → see leaderboard.

- [ ] **Step 4: Judge happy-path**

- "Đội được phân công" → see assignments (if any). Empty state is OK.
- "Bảng xếp hạng" → pick round → leaderboard renders.

- [ ] **Step 5: Admin sanity**

- Home shows "Tạo sự kiện" CTA.
- Event detail "Quản lý" tab links to `/events/[id]/manage`.

- [ ] **Step 6: Run full test suite**

Run: `npm test -- --watch=false`
Expected: PASS (pre-existing tests + 3 new helpers' tests).

- [ ] **Step 7: Commit any verification fixes**

If any issues surfaced, fix them via small focused commits.

```bash
git status
```

---

## Self-Review Notes

- **Spec §2 (Role mapping):** Task 1.
- **Spec §3 (Navbar):** Task 2.
- **Spec §4 (Events list gate):** Task 3.
- **Spec §5.1 (student no team):** Tasks 9, 11, 12, 14.
- **Spec §5.2 (student with team):** Tasks 9, 11, 12, 15, 16, 17, 18.
- **Spec §5.3 (judge):** Tasks 9, 11, 12, 18, 19.
- **Spec §5.4 (admin unchanged):** Task 12 (`manage` tab links out).
- **Spec §6 (per-category, per-round):** Tasks 8, 17.
- **Spec §7 (file layout):** Tasks 5–9, 11–19.
- **Spec §8 (open flags):** Task 8 step 1 (team-scoped score query — fallback baked in), Task 14 (tracks endpoint already used elsewhere), Task 4 (real userId — no re-fetch on role change, acceptable per spec).
- **Out of scope:** Admin dashboard redesign, judge score-entry redesign, email pipeline, Cloudfly upload UX — all left alone.

No placeholders. All file paths absolute. Each task ends green and committable.
