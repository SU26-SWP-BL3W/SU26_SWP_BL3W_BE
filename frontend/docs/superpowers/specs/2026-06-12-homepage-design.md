# Homepage — Design Spec
**Date:** 2026-06-12
**Route:** `/` (Next.js App Router `src/app/page.tsx`)
**Stack:** Next.js 16 · TanStack Query v5 · Axios · monogreen design system

---

## Overview

User-facing homepage composed of three independent feature areas. Data fetched from REST API; mock data used during development via env flag. Page is a server component shell — each feature section is a client component that owns its own data fetching.

---

## Architecture

Feature-based folder structure. Each feature is self-contained: components, hooks, API layer, types, and mocks live together.

```
src/
├── app/
│   └── page.tsx                      ← server component, assembles features
├── components/
│   └── Navbar.tsx                    ← shared nav, used across all routes
└── features/
    ├── user/
    │   ├── components/
    │   │   ├── UserProfileHeader.tsx
    │   │   ├── AnnouncementBanner.tsx
    │   │   └── ProjectSummaryCard.tsx
    │   ├── hooks/useUserProfile.ts
    │   ├── api/user.ts
    │   └── mocks/user.mock.ts
    ├── events/
    │   ├── components/
    │   │   ├── EventSection.tsx
    │   │   ├── EventFilterTabs.tsx
    │   │   ├── EventGrid.tsx
    │   │   └── EventCard.tsx
    │   ├── hooks/useEvents.ts
    │   ├── api/events.ts
    │   ├── types/event.types.ts
    │   └── mocks/events.mock.ts
    └── leaderboard/
        ├── components/
        │   ├── LeaderboardSection.tsx
        │   └── LeaderboardRow.tsx
        ├── hooks/useLeaderboard.ts
        ├── api/leaderboard.ts
        ├── types/leaderboard.types.ts
        └── mocks/leaderboard.mock.ts
```

---

## Page Assembly (`src/app/page.tsx`)

Server component. Imports feature components; no client-side logic here.

```tsx
<Navbar />
<main>
  <UserProfileHeader />
  <AnnouncementBanner />
  <ProjectSummaryCard />
  <EventSection />
  <LeaderboardSection />
</main>
```

Layout follows monogreen section rhythm: `var(--space-section)` (64px) vertical gap between major blocks.

---

## Feature: User

### UserProfileHeader
- Displays: avatar, full name, role badge, 3 stats (events joined, project score, rank)
- Data source: `useUserProfile()` → `GET /users/me/profile`
- Skeleton loader while fetching; skeleton fixed at same dimensions as loaded content to prevent layout shift
- Avatar: `alt={fullName}`, `width=48 height=48`, `loading="lazy"`, fallback = initials div on `onError`
- Role badge: `aria-label={role}` for screen readers

### AnnouncementBanner
- Displays: platform-level announcement text, optional CTA link
- Data source: `useUserProfile()` (same call, announcement field) or dedicated `GET /announcements/active`
- Dismissible (persisted to localStorage key `dismissed-announcement-{announcement.id}`)

### ProjectSummaryCard
- Displays: current semester name, current project name, completion percentage, team size
- Data source: `useUserProfile()` → `GET /users/me/project-summary`
- Shows "No active project" empty state if no project assigned

### Mock data shape
```ts
UserProfile {
  id: string
  fullName: string
  avatar: string           // URL
  role: "STUDENT" | "MENTOR" | "ADMIN"
  stats: {
    eventsJoined: number
    projectScore: number   // 0–100
    rank: number
  }
  announcement?: {
    id: string
    text: string
    ctaLabel?: string
    ctaUrl?: string
  }
  projectSummary?: {
    semesterName: string
    projectName: string
    completionPct: number  // 0–100
    teamSize: number
  }
}
```

---

## Feature: Events

### EventSection
- Container. Holds tabs + grid. Manages active filter state (`"all" | "my"`).
- "Create Event" button top-right → navigates to `/events/create`

### EventFilterTabs
- Two tabs: **All** | **My Event**
- Controlled by `EventSection` state
- Switching tab re-queries (different query key)

### EventGrid
- 3-column grid (desktop), 2-col tablet, 1-col mobile
- Renders list of `EventCard`
- Shows skeleton cards (3) while loading; skeleton cards fixed height `200px` to prevent layout shift
- Empty state: "No events found" when list is empty; rendered in card-area with same min-height

### EventCard
- Fields: title, date (formatted), tags (up to 3), status badge (`open`/`closed`), "Join" CTA button
- Join button disabled when `status === "closed"` or `isJoined === true`
- **No nested interactive elements:** only title + date + tags wrapped in `<Link href="/events/:id">`. Join button is a sibling outside the link — never `<a>` inside `<a>`
- Join button: optimistic update (`isJoined = true` immediately); revert + show inline error string on API failure
- Join button min touch target: 44×44px (`.btn-primary` height `var(--h-control)` = 44px ✓; min-width enforce in component)

### API
```ts
GET /events              // all events, paginated
GET /events?filter=mine  // user's events
POST /events/:id/join    // join event
```

### Types
```ts
EventStatus = "open" | "closed"

Event {
  id: string
  title: string
  date: string           // ISO 8601
  tags: string[]         // max 3 shown
  status: EventStatus
  isJoined: boolean
}
```

### Mock data
5 events: mix of open/closed, varied tags, some marked as joined.

---

## Feature: Leaderboard

### LeaderboardSection
- Section heading + ordered list of `LeaderboardRow`
- Shows top 10 entries
- Data source: `useLeaderboard()` → `GET /leaderboard`

### LeaderboardRow
- Fields: rank number, avatar, full name, score, badge icon
- Badge: gold (rank 1), silver (rank 2), bronze (rank 3), none (rank 4+)
- Current user row: green left border + `"(You)"` text label appended to name + `aria-label="Your entry, rank {rank}"` on the `<tr>`/row element — not color-only (WCAG AA)
- Avatar: `alt={fullName}`, `loading="lazy"`, fallback = initials div on `onError`

### Types
```ts
BadgeTier = "gold" | "silver" | "bronze" | null

LeaderboardEntry {
  rank: number
  userId: string
  fullName: string
  avatar: string
  score: number
  badge: BadgeTier
}
```

### Mock data
10 entries. Ranks 1–3 have badge tiers. One entry matches current user id.

---

## Mock Strategy

Single env flag controls all features:

```
NEXT_PUBLIC_USE_MOCK=true   # .env.local during development
```

Each hook pattern:
```ts
const USE_MOCK = process.env.NEXT_PUBLIC_USE_MOCK === "true"

export function useAllEvents() {
  return useQuery({
    queryKey: ["events", "all"],
    queryFn: USE_MOCK ? () => Promise.resolve(MOCK_EVENTS) : eventsApi.getAll,
  })
}
```

Remove mock files and env flag when real API is ready. No code changes to components or hooks.

---

## Error Handling

- API errors: show inline error message within each section (not full-page error)
- Network offline: TanStack Query retry (1 attempt, default)
- Empty states: defined per component above
- Auth error (401): handled globally by axios interceptor → redirect `/auth`
- Join event failure: revert optimistic update, show inline error below Join button
- Avatar load failure: replace `<img>` with initials div (`background: var(--color-surface-soft)`, initials in `var(--color-mute)`)

## Accessibility

- All avatar `<img>` tags: `alt={fullName}`, `loading="lazy"`
- Role badge: `aria-label={role}`
- Current user leaderboard row: `aria-label="Your entry, rank {rank}"` + `"(You)"` text — not color-only
- EventFilterTabs: `role="tablist"`, each tab `role="tab"`, panel `role="tabpanel"` with `aria-labelledby`; tab switch announces via `aria-selected`
- Navbar route change: `document.querySelector('main')?.focus()` on navigation (add `tabIndex={-1}` to `<main>`)
- Touch targets: all interactive elements ≥ 44×44px

---

## Styling

Follows monogreen design system (`src/app/globals.css`):
- Section containers: `.card` class with `var(--border-hairline)`, `var(--radius-sm)` (2px)
- Section headings: `.t-heading-md` (20px 700)
- Join button: `.btn-primary`
- Create Event: `.btn-outline`
- Tab active state: `.pill-tab.is-active`
- Leaderboard current user highlight: `border-left: 2px solid var(--color-primary)`

---

## Out of Scope

- Event creation form (`/events/create`) — separate spec
- Pagination / infinite scroll for events — next iteration
- Real-time leaderboard updates — future
- Notification system — future
