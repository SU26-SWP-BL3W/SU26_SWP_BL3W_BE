# Homepage — Implementation Plan
**Spec:** `docs/superpowers/specs/2026-06-12-homepage-design.md`
**Date:** 2026-06-12
**Branch:** `kimdtt/dev`

---

## Execution order

Dependencies flow: types → mocks → api → hooks → leaf components → container components → page.
Each step is independently testable before the next begins.

---

## Phase 1 — Types (no deps)

### Step 1 — `src/features/events/types/event.types.ts`
```ts
export type EventStatus = "open" | "closed"
export interface Event {
  id: string
  title: string
  date: string        // ISO 8601
  tags: string[]      // render max 3
  status: EventStatus
  isJoined: boolean
}
```

### Step 2 — `src/features/leaderboard/types/leaderboard.types.ts`
```ts
export type BadgeTier = "gold" | "silver" | "bronze" | null
export interface LeaderboardEntry {
  rank: number
  userId: string
  fullName: string
  avatar: string
  score: number
  badge: BadgeTier
}
```

### Step 3 — Extend `src/services/api/types.ts`
Add `UserStats`, `Announcement`, `ProjectSummary` to existing file:
```ts
export interface UserStats {
  eventsJoined: number
  projectScore: number   // 0–100
  rank: number
}
export interface Announcement {
  id: string
  text: string
  ctaLabel?: string
  ctaUrl?: string
}
export interface ProjectSummary {
  semesterName: string
  projectName: string
  completionPct: number  // 0–100
  teamSize: number
}
// extend UserProfile (already exists) with:
//   stats: UserStats
//   announcement?: Announcement
//   projectSummary?: ProjectSummary
```

---

## Phase 2 — Mock data (depends on types)

### Step 4 — `src/features/user/mocks/user.mock.ts`
One `MOCK_USER_PROFILE` object with all fields populated:
- role: "STUDENT"
- stats: eventsJoined 12, projectScore 78, rank 4
- announcement with text + ctaLabel
- projectSummary with completionPct 65, teamSize 5

### Step 5 — `src/features/events/mocks/events.mock.ts`
`MOCK_EVENTS: Event[]` — 5 items:
- 2 open + not joined
- 1 open + already joined
- 1 closed
- 1 open, many tags (test 3-tag truncation)

`MOCK_MY_EVENTS: Event[]` — subset of above where `isJoined === true`

### Step 6 — `src/features/leaderboard/mocks/leaderboard.mock.ts`
`MOCK_LEADERBOARD: LeaderboardEntry[]` — 10 entries:
- rank 1–3: badge "gold"/"silver"/"bronze"
- rank 4–10: badge null
- one entry `userId` matches MOCK_USER_PROFILE.id (for current-user highlight)

---

## Phase 3 — API layer (depends on types)

### Step 7 — `src/features/user/api/user.ts`
```ts
export const userApi = {
  getProfile: () => apiClient.get<UserProfile>("/users/me/profile").then(r => r.data),
  getProjectSummary: () => apiClient.get<ProjectSummary>("/users/me/project-summary").then(r => r.data),
}
```

### Step 8 — `src/features/events/api/events.ts`
```ts
export const eventsApi = {
  getAll: () => apiClient.get<Event[]>("/events").then(r => r.data),
  getMine: () => apiClient.get<Event[]>("/events?filter=mine").then(r => r.data),
  join: (id: string) => apiClient.post(`/events/${id}/join`).then(r => r.data),
}
```

### Step 9 — `src/features/leaderboard/api/leaderboard.ts`
```ts
export const leaderboardApi = {
  getTop: () => apiClient.get<LeaderboardEntry[]>("/leaderboard").then(r => r.data),
}
```

---

## Phase 4 — Hooks (depends on api + mocks)

### Step 10 — `src/features/user/hooks/useUserProfile.ts`
```ts
const USE_MOCK = process.env.NEXT_PUBLIC_USE_MOCK === "true"
export function useUserProfile() {
  return useQuery({
    queryKey: ["user", "profile"],
    queryFn: USE_MOCK ? () => Promise.resolve(MOCK_USER_PROFILE) : userApi.getProfile,
    staleTime: 5 * 60_000,
  })
}
```

### Step 11 — `src/features/events/hooks/useEvents.ts`
Two hooks: `useAllEvents()` and `useMyEvents()`.
Both follow same mock-flag pattern.
`useJoinEvent()` — `useMutation` calling `eventsApi.join(id)`:
- `onMutate`: optimistic update via `queryClient.setQueryData`
- `onError`: rollback via saved previous data
- `onSettled`: `queryClient.invalidateQueries(["events"])`

### Step 12 — `src/features/leaderboard/hooks/useLeaderboard.ts`
```ts
export function useLeaderboard() {
  return useQuery({
    queryKey: ["leaderboard"],
    queryFn: USE_MOCK ? () => Promise.resolve(MOCK_LEADERBOARD) : leaderboardApi.getTop,
    staleTime: 2 * 60_000,
  })
}
```

---

## Phase 5 — Leaf components (depends on hooks)

### Step 13 — `src/components/Navbar.tsx`
- `"use client"`, monogreen `.utility-bar` + `.primary-nav` classes
- Right: Account button (`.btn-outline`) → `/account`
- On route change: `document.querySelector('main')?.focus()`
- Mobile: hamburger (hidden for now, `display:none` below 768px)

### Step 14 — `src/features/user/components/UserProfileHeader.tsx`
- `"use client"`, calls `useUserProfile()`
- Skeleton: 3 placeholder bars at fixed height while loading
- Avatar: `<img alt={fullName} width=48 height=48 loading="lazy">` + initials fallback `onError`
- Role badge: `aria-label={role}`, `.badge-tag` class
- Stats row: 3 stat cells (label + value)

### Step 15 — `src/features/user/components/AnnouncementBanner.tsx`
- `"use client"`
- Check `localStorage.getItem("dismissed-announcement-{id}")` on mount
- If dismissed → render nothing
- Dismiss button: sets localStorage + hides banner
- CTA link: render only if `ctaLabel` exists

### Step 16 — `src/features/user/components/ProjectSummaryCard.tsx`
- `"use client"`, calls `useUserProfile()`
- If `!projectSummary` → "No active project" empty state card
- Progress bar: `width: {completionPct}%`, `background: var(--color-primary)`
- Team size + semester name as metadata row

### Step 17 — `src/features/events/components/EventCard.tsx`
- Props: `event: Event`, `onJoin: (id: string) => void`, `isJoining: boolean`
- Structure: `<article>` with `.card` class
  - `<Link href="/events/{id}">` wraps title + date + tags ONLY
  - Join `<button>` outside the link, sibling
- Tags: `.badge-tag` × max 3, `slice(0, 3)`
- Status badge: "OPEN" / "CLOSED" uppercase `.badge-tag`
- Join disabled when `isJoined || status === "closed" || isJoining`
- `cursor-pointer` on card hover area

### Step 18 — `src/features/events/components/EventFilterTabs.tsx`
- Props: `active: "all" | "my"`, `onChange: (tab) => void`
- `role="tablist"`, each tab `role="tab"` `aria-selected={active === tab}`
- `.pill-tab` + `.is-active` classes
- Tab panel `id` referenced by `aria-controls`

### Step 19 — `src/features/events/components/EventGrid.tsx`
- Props: `events: Event[]`, `isLoading: boolean`, `filter: "all"|"my"`
- Loading: 3 skeleton cards `min-height: 200px`, `.card` with pulsing opacity
- Empty: "No events found" centered in min-height 200px area
- Grid: `.grid-3` class (monogreen responsive grid)

### Step 20 — `src/features/leaderboard/components/LeaderboardRow.tsx`
- Props: `entry: LeaderboardEntry`, `isCurrentUser: boolean`
- If `isCurrentUser`: `border-left: 2px solid var(--color-primary)`, append " (You)" to name, `aria-label="Your entry, rank {rank}"`
- Avatar: `<img alt={fullName} loading="lazy">` + initials fallback
- Badge: gold=#f59e0b, silver=#94a3b8, bronze=#b45309 — colored square (`.corner-square` 10px)
- Score: `.t-body-strong`

### Step 21 — `src/features/leaderboard/components/LeaderboardSection.tsx`
- `"use client"`, calls `useLeaderboard()` + `useCurrentUser()` (for userId comparison)
- Section heading `.t-heading-md`, `.card` container
- Renders `LeaderboardRow` × 10
- Loading: skeleton rows at `height: 44px`

---

## Phase 6 — Container components

### Step 22 — `src/features/events/components/EventSection.tsx`
- `"use client"`
- State: `filter: "all" | "my"` (default "all")
- Calls `useAllEvents()` or `useMyEvents()` based on filter
- Calls `useJoinEvent()` for join mutation
- Layout: header row (title + Create Event button) + `EventFilterTabs` + `EventGrid`
- Create Event: `.btn-outline` → `router.push("/events/create")`
- Pass `onJoin` + `isJoining` down to `EventGrid` → `EventCard`

---

## Phase 7 — Page + env

### Step 23 — Update `src/app/page.tsx`
Replace boilerplate with feature assembly:
```tsx
import { Navbar } from "@/components/Navbar"
import { UserProfileHeader } from "@/features/user/components/UserProfileHeader"
import { AnnouncementBanner } from "@/features/user/components/AnnouncementBanner"
import { ProjectSummaryCard } from "@/features/user/components/ProjectSummaryCard"
import { EventSection } from "@/features/events/components/EventSection"
import { LeaderboardSection } from "@/features/leaderboard/components/LeaderboardSection"

export default function HomePage() {
  return (
    <>
      <Navbar />
      <main tabIndex={-1} style={{ outline: "none" }}>
        <UserProfileHeader />
        <AnnouncementBanner />
        <ProjectSummaryCard />
        <EventSection />
        <LeaderboardSection />
      </main>
    </>
  )
}
```

### Step 24 — `.env.local`
```
NEXT_PUBLIC_USE_MOCK=true
```

---

## Checklist

- [ ] Step 1 — event.types.ts
- [ ] Step 2 — leaderboard.types.ts
- [ ] Step 3 — extend services/api/types.ts
- [ ] Step 4 — user.mock.ts
- [ ] Step 5 — events.mock.ts
- [ ] Step 6 — leaderboard.mock.ts
- [ ] Step 7 — user api
- [ ] Step 8 — events api
- [ ] Step 9 — leaderboard api
- [ ] Step 10 — useUserProfile hook
- [ ] Step 11 — useEvents hook (+ useJoinEvent optimistic)
- [ ] Step 12 — useLeaderboard hook
- [ ] Step 13 — Navbar
- [ ] Step 14 — UserProfileHeader
- [ ] Step 15 — AnnouncementBanner
- [ ] Step 16 — ProjectSummaryCard
- [ ] Step 17 — EventCard
- [ ] Step 18 — EventFilterTabs
- [ ] Step 19 — EventGrid
- [ ] Step 20 — LeaderboardRow
- [ ] Step 21 — LeaderboardSection
- [ ] Step 22 — EventSection
- [ ] Step 23 — page.tsx
- [ ] Step 24 — .env.local
