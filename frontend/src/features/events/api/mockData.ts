import { Event, Team, Submission, ScoreBreakdown } from './eventService';

// Mock events data
export const mockEvents: Event[] = [
  {
    id: 'evt-001',
    title: 'React Frontend Challenge 2026',
    startDate: '2026-06-15T10:00:00Z',
    endDate: '2026-06-22T23:59:59Z',
    status: 'open',
    submissionType: 'Both',
    description: 'Build a responsive React dashboard with real-time data updates. Showcase your UI/UX design skills and JavaScript proficiency.',
  },
  {
    id: 'evt-002',
    title: 'Node.js API Development',
    startDate: '2026-07-01T10:00:00Z',
    endDate: '2026-07-15T23:59:59Z',
    status: 'open',
    submissionType: 'ZIP',
    description: 'Design and implement a RESTful API with authentication, validation, and database integration.',
  },
  {
    id: 'evt-003',
    title: 'Mobile App Contest',
    startDate: '2026-08-01T10:00:00Z',
    endDate: '2026-08-20T23:59:59Z',
    status: 'closed',
    submissionType: 'URL',
    description: 'Create an innovative mobile application using React Native or Flutter.',
  },
];

// Mock teams data
export const mockTeams: Record<string, Team[]> = {
  'evt-001': [
    {
      id: 'team-001',
      name: 'abc',
      leader: { id: 'user-1', name: 'Candidate 1', email: 'candidate1@example.com' },
      members: [
        { id: 'user-2', name: 'Trần Thị Bình', email: 'binhtt@example.com' },
        { id: 'user-3', name: 'Lê Văn Cường', email: 'cuonglv@example.com' },
      ],
      eventId: 'evt-001',
    },
    {
      id: 'team-002',
      name: 'xyz',
      leader: { id: 'user-4', name: 'Candidate 2', email: 'candidate2@example.com' },
      members: [
        { id: 'user-5', name: 'Hoàng Văn Em', email: 'emhv@example.com' },
      ],
      eventId: 'evt-001',
    },
    {
      id: 'team-003',
      name: 'swp',
      leader: { id: 'user-6', name: 'Candidate 3', email: 'phongvv@example.com' },
      members: [
        { id: 'user-7', name: 'Đặng Thị Giang', email: 'giangdt@example.com' },
        { id: 'user-8', name: 'Bùi Văn Hải', email: 'haibv@example.com' },
      ],
      eventId: 'evt-001',
    },
  ],
  'evt-002': [
    {
      id: 'team-004',
      name: 'def',
      leader: { id: 'user-9', name: 'Ngô Thị Lan', email: 'lannt@example.com' },
      members: [
        { id: 'user-10', name: 'Đỗ Văn Minh', email: 'minhdv@example.com' },
      ],
      eventId: 'evt-002',
    },
  ],
  'evt-003': [],
};

// Mock submissions data
export const mockSubmissions: Record<string, Submission[]> = {
  'team-001': [
    {
      id: 'sub-001',
      teamId: 'team-001',
      eventId: 'evt-001',
      type: 'ZIP',
      status: 'graded',
      submitDate: '2026-06-20T14:30:00Z',
      content: 'dashboard-v1.zip',
    },
    {
      id: 'sub-002',
      teamId: 'team-001',
      eventId: 'evt-001',
      type: 'ZIP',
      status: 'graded',
      submitDate: '2026-06-21T10:15:00Z',
      content: 'dashboard-v2-final.zip',
    },
  ],
  'team-002': [
    {
      id: 'sub-003',
      teamId: 'team-002',
      eventId: 'evt-001',
      type: 'URL',
      status: 'pending-review',
      submitDate: '2026-06-21T16:45:00Z',
      content: 'https://github.com/diana-lee/frontend-challenge',
    },
  ],
  'team-003': [
    {
      id: 'sub-004',
      teamId: 'team-003',
      eventId: 'evt-001',
      type: 'ZIP',
      status: 'submitted',
      submitDate: '2026-06-22T09:00:00Z',
      content: 'design-showcase.zip',
    },
  ],
  'team-004': [],
};

// Mock score breakdown data
export const mockScoreBreakdown: Record<string, ScoreBreakdown[]> = {
  'team-001': [
    { criterion: 'Design & UX', score: 9, max: 10, status: 'graded' },
    { criterion: 'Functionality', score: 9, max: 10, status: 'graded' },
    { criterion: 'Code Quality', score: 8, max: 10, status: 'graded' },
    { criterion: 'Performance', score: 8, max: 10, status: 'graded' },
    { criterion: 'Documentation', score: 7, max: 10, status: 'pending' },
  ],
  'team-002': [
    { criterion: 'Design & UX', score: 8, max: 10, status: 'graded' },
    { criterion: 'Functionality', score: 7, max: 10, status: 'graded' },
    { criterion: 'Code Quality', score: 7, max: 10, status: 'pending' },
    { criterion: 'Performance', score: 0, max: 10, status: 'pending' },
    { criterion: 'Documentation', score: 0, max: 10, status: 'pending' },
  ],
  'team-003': [
    { criterion: 'Design & UX', score: 8, max: 10, status: 'graded' },
    { criterion: 'Functionality', score: 6, max: 10, status: 'graded' },
    { criterion: 'Code Quality', score: 6, max: 10, status: 'graded' },
    { criterion: 'Performance', score: 7, max: 10, status: 'pending' },
    { criterion: 'Documentation', score: 0, max: 10, status: 'pending' },
  ],
};

// Mock team scores (leaderboard)
export const mockTeamScores: Record<string, { teamId: string; score: number; status: string }[]> = {
  'evt-001': [
    { teamId: 'team-001', score: 41, status: 'graded' },
    { teamId: 'team-003', score: 27, status: 'pending' },
    { teamId: 'team-002', score: 22, status: 'pending' },
  ],
  'evt-002': [
    { teamId: 'team-004', score: 0, status: 'pending' },
  ],
};

// In-memory storage for user-created data (simulating backend state)
export const mockDataStore = {
  teams: JSON.parse(JSON.stringify(mockTeams)) as Record<string, Team[]>,
  submissions: JSON.parse(JSON.stringify(mockSubmissions)) as Record<string, Submission[]>,
  scores: JSON.parse(JSON.stringify(mockTeamScores)) as Record<string, { teamId: string; score: number; status: string }[]>,
  scoreBreakdown: JSON.parse(JSON.stringify(mockScoreBreakdown)) as Record<string, ScoreBreakdown[]>,
};

// Helper function to get user team (with new teams created by current user)
export function getUserTeamFromStore(eventId: string, userId: string): Team | null {
  const teams = mockDataStore.teams[eventId] || [];
  return teams.find(team =>
    team.leader.id === userId ||
    team.members.some(m => m.id === userId)
  ) || null;
}

// Helper function to create a new team in store
export function createTeamInStore(eventId: string, teamData: { name: string; memberIds: string[] }): Team {
  const teamId = `team-${Date.now()}`;
  const currentUserId = 'user-001'; // This should come from auth context in real app

  const newTeam: Team = {
    id: teamId,
    name: teamData.name,
    leader: {
      id: currentUserId,
      name: 'Current User',
      email: 'user@example.com'
    },
    members: teamData.memberIds
      .filter(id => id !== currentUserId)
      .map(id => ({
        id,
        name: `User ${id}`,
        email: `${id}@example.com`,
      })),
    eventId,
  };

  if (!mockDataStore.teams[eventId]) {
    mockDataStore.teams[eventId] = [];
  }

  mockDataStore.teams[eventId].push(newTeam);

  // Initialize empty submissions and scores for new team
  mockDataStore.submissions[teamId] = [];
  mockDataStore.scores[eventId] = mockDataStore.scores[eventId] || [];
  mockDataStore.scores[eventId].push({ teamId, score: 0, status: 'pending' });
  mockDataStore.scoreBreakdown[teamId] = [
    { criterion: 'Design & UX', score: 0, max: 10, status: 'pending' },
    { criterion: 'Functionality', score: 0, max: 10, status: 'pending' },
    { criterion: 'Code Quality', score: 0, max: 10, status: 'pending' },
    { criterion: 'Performance', score: 0, max: 10, status: 'pending' },
    { criterion: 'Documentation', score: 0, max: 10, status: 'pending' },
  ];

  return newTeam;
}

// Helper function to add submission to store
export function addSubmissionToStore(teamId: string, eventId: string, submission: Omit<Submission, 'id' | 'submitDate'>): Submission {
  const submissionId = `sub-${Date.now()}`;
  const newSubmission: Submission = {
    ...submission,
    id: submissionId,
    submitDate: new Date().toISOString(),
  };

  if (!mockDataStore.submissions[teamId]) {
    mockDataStore.submissions[teamId] = [];
  }

  mockDataStore.submissions[teamId].push(newSubmission);
  return newSubmission;
}
