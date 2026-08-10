# Mock Data Guide

The event dashboard comes with comprehensive mock data so you can develop and test without a backend server.

## Overview

Mock data is currently enabled in `src/features/events/api/eventService.ts`. All API calls return mock data with simulated network delays (300-800ms) for realistic UX.

## Available Mock Data

### Events (3 samples)
- **evt-001**: React Frontend Challenge 2026 (Open)
- **evt-002**: Node.js API Development (Open)
- **evt-003**: Mobile App Contest (Closed)

### Teams
- Code Warriors (evt-001)
- Frontend Ninjas (evt-001)
- Design Innovators (evt-001)
- Backend Masters (evt-002)

### Submissions & Scores
- Teams have sample submissions with different statuses
- Score breakdowns with grading criteria
- Leaderboards for each event

## Features Supported by Mock Data

✅ Create new teams (stored in-memory)  
✅ Submit work (ZIP files and URLs)  
✅ View submissions history  
✅ View score breakdowns and leaderboards  
✅ Tab navigation and all UI interactions  
✅ Form validation  
✅ Error handling  
✅ Responsive design  
✅ Loading states with skeleton loaders  

## Data Persistence

Mock data is stored in-memory using `mockDataStore` in `src/features/events/api/mockData.ts`:
- New teams created during development are added to the store
- Submissions are tracked when submitted
- Data persists during the session but resets on page refresh

## Testing URLs

To test different events, use these URLs:

```
http://localhost:3000/events/evt-001  # React Frontend Challenge (has teams & submissions)
http://localhost:3000/events/evt-002  # Node.js API (has teams, no submissions yet)
http://localhost:3000/events/evt-003  # Mobile App Contest (closed event, no teams)
```

## Switching to Real Backend

When you have a real backend ready, follow these steps:

### Step 1: Update eventService.ts

Replace the mock-based implementation with API calls. Restore the original code structure:

```typescript
import { apiClient } from '@/services/api';

export const eventService = {
  getAllEvents: async (): Promise<Event[]> => {
    const response = await apiClient.get<Event[]>('/events');
    return response.data || [];
  },

  getEvent: async (eventId: string): Promise<Event> => {
    const response = await apiClient.get<Event>(`/events/${eventId}`);
    if (!response.data) {
      throw new Error(`Event ${eventId} not found`);
    }
    return response.data;
  },

  // ... rest of API calls
};
```

### Step 2: Set Environment Variables

Create `.env.local` (copy from `.env.example`):

```env
NEXT_PUBLIC_API_URL=http://your-backend-url.com/api
```

### Step 3: Verify API Endpoints

Ensure your backend implements these endpoints:

**Events**
- `GET /events` — Get all events
- `GET /events/me` — Get user's joined events
- `GET /events/:eventId` — Get event details
- `POST /events/:eventId/join` — Join an event

**Teams**
- `GET /events/:eventId/teams` — Get all teams for an event
- `GET /events/:eventId/teams/user/:userId` — Get user's team
- `POST /events/:eventId/teams` — Create new team

**Submissions**
- `GET /teams/:teamId/submissions` — Get team submissions
- `POST /teams/:teamId/submissions` — Submit work (supports multipart/form-data for ZIP)

**Scores**
- `GET /events/:eventId/scores` — Get leaderboard scores
- `GET /teams/:teamId/scores?eventId=:eventId` — Get score breakdown

### Step 4: Update Authentication

The mock data assumes user ID `'user-001'`. When switching to real API:

1. Update the `userId` to come from your auth context
2. Ensure auth tokens are sent via `apiClient` (already configured via interceptors in `src/services/api/client.ts`)

### Step 5: Test Integration

After switching to real API:
- Run `npm run dev`
- Navigate to an event dashboard
- Verify all tabs load data correctly
- Test creating teams and submitting work
- Check that error states display properly

## Keeping Mock Data for Offline Development

If you want to keep mock data available for offline development while also supporting real API:

### Option 1: Environment-Based Switching

```typescript
const USE_MOCK = process.env.NEXT_PUBLIC_USE_MOCK === 'true';

export const eventService = {
  getEvent: async (eventId: string) => {
    if (USE_MOCK) {
      return mockGetEvent(eventId);
    }
    return apiClient.get(`/events/${eventId}`);
  },
};
```

### Option 2: Mock Server (Recommended for Production)

Use a tool like `msw` (Mock Service Worker) or `mirage` to intercept API calls:

```bash
npm install msw
```

This allows real API calls in your code but intercepts them for testing.

## Troubleshooting

**Q: Why is data not persisting after refresh?**  
A: Mock data is in-memory only. The page refresh resets it to the initial state. This is by design for development.

**Q: Can I modify mock data?**  
A: Yes! Edit `src/features/events/api/mockData.ts` to add more teams, events, or submissions.

**Q: How do I test error states?**  
A: Temporarily throw an error in `eventService.ts`:

```typescript
getEvent: async (eventId: string) => {
  throw new Error('API error for testing');
}
```

**Q: How do I add more sample data?**  
A: Edit `src/features/events/api/mockData.ts` and add to the `mock*` arrays. The in-memory store will use them.

## API Response Format

All responses must match the TypeScript interfaces in `src/features/events/api/eventService.ts`:

```typescript
interface Event {
  id: string;
  title: string;
  startDate: string;      // ISO 8601
  endDate: string;        // ISO 8601
  status: 'open' | 'closed';
  submissionType: 'ZIP' | 'URL' | 'Both';
  description: string;
}

interface Team {
  id: string;
  name: string;
  leader: { id: string; name: string; email: string };
  members: { id: string; name: string; email: string }[];
  eventId: string;
}

interface Submission {
  id: string;
  teamId: string;
  eventId: string;
  type: 'ZIP' | 'URL';
  status: 'submitted' | 'pending-review' | 'rejected' | 'graded';
  submitDate: string;     // ISO 8601
  content: string | File;
}

interface ScoreBreakdown {
  criterion: string;
  score: number;
  max: number;
  status: 'graded' | 'pending';
}
```

## Support

If you have questions or need to adjust mock data, refer to:
- `src/features/events/api/mockData.ts` — Mock data definitions
- `src/features/events/api/eventService.ts` — Service implementation
- `docs/EVENT_DASHBOARD_API.md` — API documentation
