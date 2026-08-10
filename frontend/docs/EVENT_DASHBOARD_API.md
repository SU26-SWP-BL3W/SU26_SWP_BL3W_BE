# Event Dashboard API Integration

## Environment Variables

Add to `.env.local`:

```env
NEXT_PUBLIC_API_URL=http://localhost:8000/api
```

## Required API Endpoints

### Events
- `GET /api/events` — Get all events
- `GET /api/events?filter=mine` — Get user's joined events
- `POST /api/events/:id/join` — Join an event

### Teams
- `GET /api/events/:eventId/teams` — Get all teams for an event
- `GET /api/events/:eventId/teams/user/:userId` — Get user's team
- `POST /api/events/:eventId/teams` — Create team

### Submissions
- `GET /api/teams/:teamId/submissions` — Get team submissions
- `POST /api/teams/:teamId/submissions` — Submit work (multipart for ZIP)

### Scores
- `GET /api/events/:eventId/scores` — Get leaderboard scores
- `GET /api/teams/:teamId/scores?eventId=:eventId` — Get score breakdown

## Response Formats

### Event
```typescript
interface Event {
  id: string;
  title: string;
  startDate: string; // ISO 8601
  endDate: string;
  status: "open" | "closed";
  submissionType: "ZIP" | "URL" | "Both";
  description: string;
}
```

### User Profile
```typescript
interface UserProfile {
  id: string;
  email: string;
  fullName: string;
  role: "STUDENT" | "MENTOR" | "ADMIN";
  avatar?: string;
  createdAt: string;
  stats: {
    eventsJoined: number;
    projectScore: number;  // 0–100
    rank: number;
  };
  announcement?: {
    id: string;
    text: string;
    ctaLabel?: string;
    ctaUrl?: string;
  };
  projectSummary?: {
    semesterName: string;
    projectName: string;
    completionPct: number;  // 0–100
    teamSize: number;
  };
}
```

### API Error
```typescript
interface ApiError {
  message: string;
  statusCode: number;
  errors?: Record<string, string[]>;
}
```

### Paginated Response
```typescript
interface PaginatedResponse<T> {
  data: T[];
  total: number;
  page: number;
  pageSize: number;
  totalPages: number;
}
```

## Error Handling

API errors are caught by TanStack Query and stored in hook's `error` property. Display error state in tabs with user-friendly error messages. See `src/features/*/hooks/use*.ts` for hook implementation examples.

## Caching Strategy

- Events: 5 min cache
- Teams: 2 min cache
- Submissions: 1 min cache
- Scores: 2 min cache

Invalidation happens automatically after mutations (create team, submit work) via TanStack Query's `invalidateQueries`.

## Development Setup

1. Ensure `.env.local` contains `NEXT_PUBLIC_API_URL=http://localhost:8000/api`
2. Start the backend API server on port 8000
3. Run `npm run dev` to start the frontend
4. API calls will be made from the browser to the backend server

## Testing

To test API integration locally with mock data:

1. Set `NEXT_PUBLIC_USE_MOCK=true` in `.env.local`
2. Mock data is located in `src/features/*/mocks/*.mock.ts`
3. Hooks automatically use mock data when mock mode is enabled
