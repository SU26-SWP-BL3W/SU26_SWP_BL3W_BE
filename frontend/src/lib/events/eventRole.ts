/**
 * Dashboard role = the user's effective role *within a single event*, derived
 * from their EventRole (GET /api/EventRoles/user-role). This is distinct from
 * the account-level role (admin/student/mentor) in `useUserRole`.
 *
 * Scope (per product decision): only Judge gets a distinct dashboard. Everyone
 * else — TeamLeader, TeamMember, Mentor, EventCoordinator, or no assigned role
 * at all — is a "participant" ("Người tham gia"). Global admins never reach the
 * student dashboard (they are redirected to /manage).
 */
export type DashboardRole = 'participant' | 'judge' | 'admin';

/** EventRoleType numeric values (GET /api/EventRoles/types). */
export const EVENT_ROLE_NUM = {
  EventCoordinator: 0,
  Judge: 1,
  Mentor: 2,
  TeamLeader: 3,
  TeamMember: 4,
} as const;

/**
 * Normalise an EventRole.roleName to its numeric EventRoleType. The API may send
 * the enum name ("Judge"), the numeric value (1), or its string form ("1");
 * returns null when there is no recognisable role.
 */
export function roleNameToNumber(roleName: string | number | null | undefined): number | null {
  if (typeof roleName === 'number') return Number.isNaN(roleName) ? null : roleName;
  switch ((roleName ?? '').toString().trim().toLowerCase()) {
    case 'eventcoordinator': case '0': return EVENT_ROLE_NUM.EventCoordinator;
    case 'judge':            case '1': return EVENT_ROLE_NUM.Judge;
    case 'mentor':           case '2': return EVENT_ROLE_NUM.Mentor;
    case 'teamleader':       case '3': return EVENT_ROLE_NUM.TeamLeader;
    case 'teammember':       case '4': return EVENT_ROLE_NUM.TeamMember;
    default: return null;
  }
}

/**
 * Resolve a user's role within an event from the numeric roleName values that
 * the EventRoles API returns (a user may hold several). Produces:
 *  - `role`: the dashboard role driving the tab set — judge if they judge here,
 *    otherwise participant (mentor/coordinator use the participant layout for now)
 *  - `label`: the granular Vietnamese label shown under the account name, keeping
 *    coordinator/judge/mentor distinct; team members and the unassigned show as
 *    "Người tham gia".
 */
export function resolveEventRole(roleNumbers: number[]): { role: DashboardRole; label: string } {
  const has = (n: number) => roleNumbers.includes(n);
  const role: DashboardRole = has(EVENT_ROLE_NUM.Judge) ? 'judge' : 'participant';
  const label = has(EVENT_ROLE_NUM.EventCoordinator)
    ? 'Ban tổ chức'
    : has(EVENT_ROLE_NUM.Judge)
      ? 'Giám khảo'
      : has(EVENT_ROLE_NUM.Mentor)
        ? 'Cố vấn'
        : has(EVENT_ROLE_NUM.TeamLeader)
          ? 'Trưởng nhóm'
          : has(EVENT_ROLE_NUM.TeamMember)
            ? 'Thành viên'
            : 'Người tham gia';
  return { role, label };
}
