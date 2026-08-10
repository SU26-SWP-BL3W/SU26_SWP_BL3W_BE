import { resolveEventRole, roleNameToNumber, EVENT_ROLE_NUM } from '../eventRole';

const { EventCoordinator, Judge, Mentor, TeamLeader, TeamMember } = EVENT_ROLE_NUM;

describe('resolveEventRole', () => {
  test('no assigned role → participant / "Người tham gia"', () => {
    expect(resolveEventRole([])).toEqual({ role: 'participant', label: 'Người tham gia' });
  });

  test('team member/leader → participant / "Người tham gia"', () => {
    expect(resolveEventRole([TeamMember])).toEqual({ role: 'participant', label: 'Thành viên' });
    expect(resolveEventRole([TeamLeader])).toEqual({ role: 'participant', label: 'Trưởng nhóm' });
  });

  test('judge → judge dashboard / "Giám khảo"', () => {
    expect(resolveEventRole([Judge])).toEqual({ role: 'judge', label: 'Giám khảo' });
  });

  test('mentor → participant dashboard but "Cố vấn" label', () => {
    expect(resolveEventRole([Mentor])).toEqual({ role: 'participant', label: 'Cố vấn' });
  });

  test('event coordinator → participant dashboard but "Ban tổ chức" label', () => {
    expect(resolveEventRole([EventCoordinator])).toEqual({ role: 'participant', label: 'Ban tổ chức' });
  });

  test('multiple roles: judge takes the dashboard, coordinator wins the label', () => {
    expect(resolveEventRole([TeamMember, Judge])).toEqual({ role: 'judge', label: 'Giám khảo' });
    expect(resolveEventRole([EventCoordinator, Judge])).toEqual({ role: 'judge', label: 'Ban tổ chức' });
  });
});

describe('roleNameToNumber (tolerant of name or number)', () => {
  test('enum name (any casing)', () => {
    expect(roleNameToNumber('Judge')).toBe(Judge);
    expect(roleNameToNumber('eventcoordinator')).toBe(EventCoordinator);
    expect(roleNameToNumber('Mentor')).toBe(Mentor);
  });

  test('numeric value or its string form', () => {
    expect(roleNameToNumber(1)).toBe(Judge);
    expect(roleNameToNumber('1')).toBe(Judge);
    expect(roleNameToNumber(0)).toBe(EventCoordinator);
  });

  test('unknown / empty → null', () => {
    expect(roleNameToNumber(null)).toBeNull();
    expect(roleNameToNumber('')).toBeNull();
    expect(roleNameToNumber('Participant')).toBeNull();
  });
});
