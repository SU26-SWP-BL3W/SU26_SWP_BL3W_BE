import { getEventTabs } from '../getEventTabs';

describe('getEventTabs', () => {
  test('guest → detail only', () => {
    expect(getEventTabs({ isAuthenticated: false, role: null, hasTeam: false }).map((t) => t.id)).toEqual(['detail']);
  });

  test('student not approved → detail + register (no createTeam)', () => {
    expect(getEventTabs({ isAuthenticated: true, role: 'student', hasTeam: false, registrationStatus: 'pending' }).map((t) => t.id))
      .toEqual(['detail', 'register']);
    expect(getEventTabs({ isAuthenticated: true, role: 'student', hasTeam: false, registrationStatus: null }).map((t) => t.id))
      .toEqual(['detail', 'register']);
    expect(getEventTabs({ isAuthenticated: true, role: 'student', hasTeam: false, registrationStatus: 'rejected' }).map((t) => t.id))
      .toEqual(['detail', 'register']);
  });

  test('student approved without team → detail + register + createTeam + leaderboard', () => {
    expect(getEventTabs({ isAuthenticated: true, role: 'student', hasTeam: false, registrationStatus: 'approved' }).map((t) => t.id))
      .toEqual(['detail', 'register', 'createTeam', 'leaderboard']);
  });

  test('student with team → detail + register + myTeam + submission + results + appeal + leaderboard', () => {
    expect(getEventTabs({ isAuthenticated: true, role: 'student', hasTeam: true, registrationStatus: 'approved' }).map((t) => t.id))
      .toEqual(['detail', 'register', 'myTeam', 'submission', 'results', 'appeal', 'leaderboard']);
  });

  test('judge event role → detail + reviewSubmission + leaderboard', () => {
    expect(getEventTabs({ isAuthenticated: true, role: 'student', eventRoleName: 'Judge', hasTeam: false }).map((t) => t.id))
      .toEqual(['detail', 'reviewSubmission', 'leaderboard']);
  });

  test('mentor event role → detail + reviewSubmission + leaderboard', () => {
    expect(getEventTabs({ isAuthenticated: true, role: 'student', eventRoleName: 'Mentor', hasTeam: false }).map((t) => t.id))
      .toEqual(['detail', 'reviewSubmission', 'leaderboard']);
  });

  test('judge and mentor never receive the appeal tab', () => {
    for (const eventRoleName of ['Judge', 'Mentor']) {
      expect(getEventTabs({ isAuthenticated: true, role: 'student', eventRoleName, hasTeam: true }).some((t) => t.id === 'appeal')).toBe(false);
    }
  });

  test('admin or EC event role → detail + manage', () => {
    expect(getEventTabs({ isAuthenticated: true, role: 'student', eventRoleName: 'EventCoordinator', hasTeam: false }).map((t) => t.id))
      .toEqual(['detail', 'manage']);
    expect(getEventTabs({ isAuthenticated: true, role: 'admin', hasTeam: false }).map((t) => t.id))
      .toEqual(['detail', 'manage']);
  });

  test('global mentor but no event role → detail + createTeam', () => {
    expect(getEventTabs({ isAuthenticated: true, role: 'judge', hasTeam: false }).map((t) => t.id))
      .toEqual(['detail', 'createTeam']);
  });
});
