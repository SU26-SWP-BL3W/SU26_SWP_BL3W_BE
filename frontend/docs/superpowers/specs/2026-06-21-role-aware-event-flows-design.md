# Role-aware event flows (Student & Judge)

**Date:** 2026-06-21
**Status:** Draft → awaiting user review
**Scope:** Frontend only. Backend endpoints exist (verified against `SU26_SWP_SE1907_BACKEND` @ pulled HEAD).

---

## 1. Goal

Complete the post-login experience so that **STUDENT** and **JUDGE** (mapped to existing `MENTOR` role) see role-appropriate navigation and per-event workflows. Admin flow is unchanged.

## 2. Role mapping

`UserProfile.role` stays `ADMIN | STUDENT | MENTOR`. `MENTOR` is treated as JUDGE in the UI. A new helper `useUserRole()` returns `'admin' | 'student' | 'judge' | null`.

## 3. Navbar (`src/components/Navbar.tsx`)

Link set is computed from role:

| Role        | Links                                                        |
| ----------- | ------------------------------------------------------------ |
| guest       | Sự kiện · Đăng nhập                                          |
| STUDENT     | Sự kiện · Khác                                               |
| MENTOR (=JUDGE) | Sự kiện · Khác                                          |
| ADMIN       | Sự kiện · Tiêu chí chấm điểm · Người dùng · Khác             |

Implementation: replace the hard-coded array with `getNavLinksFor(role)`.

## 4. Events list (`src/app/page.tsx` + `EventGrid` / `CreateEventForm`)

Gate the "Tạo sự kiện" CTA behind `role === 'admin'`. Non-admins see only the list/filters.

## 5. Event detail (`src/app/events/[id]/page.tsx`)

Convert to a tabbed shell. Tabs come from `getEventTabs({ role, hasTeam })`:

### 5.1 STUDENT — no team yet
- **Chi tiết sự kiện** (existing detail panel)
- **Tạo đội** — form (team name, description, track). Submits `POST /api/Teams`.

### 5.2 STUDENT — has team
Detection: `GET /api/EventRoles/user/{userId}` → filter by `eventId` and `roleName ∈ {3,4}` (TeamLeader, TeamMember). The matching row gives `teamId`.

Tabs:
- **Chi tiết sự kiện**
- **Đội của tôi** — members list (`GET /api/Teams/{id}`), invite (`POST /api/Teams/{teamId}/invitations`), leave (`POST /api/Teams/{teamId}/leave`), accept/reject invitation (`POST /api/Teams/invitations/{invitationId}/respond`).
- **Nộp bài** — per round/track. List `GET /api/SubmitResults?TeamId=&RoundId=`. Create `POST /api/SubmitResults`. Update `PUT /api/SubmitResults/{id}`.
- **Kết quả** — see §6.
- **Bảng xếp hạng** — round selector → `GET /api/FinalResults/round/{roundId}`.

### 5.3 JUDGE (MENTOR)
- **Chi tiết sự kiện**
- **Đội được phân công** — `GET /api/EventRoles/user/{userId}` filtered by `eventId` and `roleName === 1` (Judge), with `teamId` set. For each assigned team, link to score sheet (existing `Scores` + `ScoreDetails` endpoints).
- **Bảng xếp hạng** — same as student's leaderboard tab.

### 5.4 ADMIN
Unchanged. "Quản lý" continues to lead to `/events/[id]/manage`.

## 6. Results view (per-category, per-round)

For the current team:

1. `GET /api/FinalResults/team/{teamId}` → list of `{ roundId, finalScore, rank, isAdvanced }`.
2. For each round, resolve the team's score(s). Open flag — see §8.
3. For each score id, `GET /api/ScoreDetails/score/{scoreId}` → rows `{ criteriaName, value, maxScore, weight }`.

UI: accordion per round (Round 1, Round 2…). Each panel renders a table:

| Tiêu chí | Điểm | Tối đa | Trọng số | Điểm có trọng số |
| -------- | ---- | ------ | -------- | ---------------- |

Footer: total weighted score + rank badge + isAdvanced flag.

## 7. File layout

New:
```
src/features/teams/
  api/teams.ts
  hooks/useTeam.ts useMyTeamForEvent.ts useTeamInvitations.ts
  components/CreateTeamForm.tsx TeamMembersPanel.tsx InvitePanel.tsx
  types/team.types.ts
src/features/submissions/
  api/submissions.ts
  hooks/useSubmissions.ts
  components/SubmissionForm.tsx SubmissionList.tsx
src/features/results/
  api/results.ts
  hooks/useTeamResults.ts
  components/ResultsAccordion.tsx ResultRoundTable.tsx
src/features/leaderboard/
  components/RoundLeaderboard.tsx   (uses existing api if present)
src/hooks/useUserRole.ts
```

Modified:
```
src/components/Navbar.tsx                 — role-filtered links
src/app/page.tsx (+ EventGrid)            — gate Create CTA
src/app/events/[id]/page.tsx              — tabbed shell driven by role + hasTeam
```

## 8. Open flags (resolve while implementing)

- **Team-scoped score query.** No clear `GET /api/Scores?TeamId=&RoundId=` in API_DOCS. Fallback: derive scores per team by fetching `GET /api/FinalResults/round/{roundId}` (per-round leaderboard) and traversing each judge's `Scores/event-role/{eventRoleId}`. Decide at impl time; if missing, file a backend request.
- **Tracks list endpoint.** Team creation needs `trackId`. Verify `GET /api/Tracks/event/{eventId}` (referenced indirectly in API_DOCS §2.8).
- **Auth refresh after role change.** Login currently stores role from login response — no re-fetch. Acceptable; users re-login if their role changes.

## 9. Testing

- Unit: `getNavLinksFor`, `getEventTabs`, `useUserRole`.
- Integration: tab visibility per role with mocked `useCurrentUser`.
- Manual: log in as each role and walk the flows.

## 10. Out of scope

- Admin dashboard changes.
- Judge score-entry UI redesign (existing screens remain).
- Team invitation email pipeline (backend already sends).
- File-upload UX for submissions beyond a URL field (Cloudfly storage exists but wiring it is a separate task).
