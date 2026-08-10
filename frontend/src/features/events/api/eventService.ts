import { mockEvents, mockDataStore, getUserTeamFromStore, createTeamInStore, addSubmissionToStore } from './mockData';

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

// Simulate network delay for realistic UX
const delay = (ms: number) => new Promise(resolve => setTimeout(resolve, ms));

export const eventService = {
  // Get all events
  getAllEvents: async (): Promise<Event[]> => {
    await delay(300);
    return mockEvents;
  },

  // Join an event
  joinEvent: async (eventId: string): Promise<{ success: boolean }> => {
    await delay(500);
    return { success: true };
  },

  // Get single event details
  getEvent: async (eventId: string): Promise<Event> => {
    await delay(300);
    const event = mockEvents.find(e => e.id === eventId);
    if (!event) {
      throw new Error(`Event ${eventId} not found`);
    }
    return event;
  },

  // Get all teams for an event
  getEventTeams: async (eventId: string): Promise<Team[]> => {
    await delay(300);
    return mockDataStore.teams[eventId] || [];
  },

  // Get user's team for event (if exists)
  getUserTeam: async (eventId: string, userId: string): Promise<Team | null> => {
    await delay(300);
    return getUserTeamFromStore(eventId, userId);
  },

  // Create new team
  createTeam: async (eventId: string, teamData: { name: string; memberIds: string[] }): Promise<Team> => {
    await delay(500);
    return createTeamInStore(eventId, teamData);
  },

  // Get team submissions
  getTeamSubmissions: async (teamId: string): Promise<Submission[]> => {
    await delay(300);
    return mockDataStore.submissions[teamId] || [];
  },

  // Submit work (ZIP or URL)
  submitWork: async (teamId: string, eventId: string, submissionData: { type: 'ZIP' | 'URL'; content: string | FormData }): Promise<Submission> => {
    await delay(800);

    if (submissionData.type === 'ZIP') {
      if (!(submissionData.content instanceof FormData)) {
        throw new Error('ZIP submission requires FormData');
      }
      const file = submissionData.content.get('file');
      if (!file) {
        throw new Error('ZIP submission missing file');
      }
    }

    const submission = addSubmissionToStore(teamId, eventId, {
      teamId,
      eventId,
      type: submissionData.type,
      status: 'submitted',
      content: submissionData.type === 'ZIP' ? 'submission.zip' : (submissionData.content as string),
    });

    return submission;
  },

  // Get team score & leaderboard
  getTeamScores: async (eventId: string): Promise<{ teamId: string; score: number; status: string }[]> => {
    await delay(300);
    return mockDataStore.scores[eventId] || [];
  },

  // Get score breakdown for a team
  getScoreBreakdown: async (teamId: string, eventId: string): Promise<ScoreBreakdown[]> => {
    await delay(300);
    return mockDataStore.scoreBreakdown[teamId] || [];
  },
};

export default eventService;
