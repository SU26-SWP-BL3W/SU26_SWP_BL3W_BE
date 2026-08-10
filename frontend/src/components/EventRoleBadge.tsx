const ROLE_STYLE: Record<string, { label: string; bg: string; fg: string; bd: string }> = {
  Judge: { label: 'Giám khảo', bg: 'rgba(0,70,164,0.08)', fg: '#0046a4', bd: 'rgba(0,70,164,0.3)' },
  EventCoordinator: { label: 'Ban tổ chức', bg: 'rgba(149,47,198,0.08)', fg: '#952fc6', bd: 'rgba(149,47,198,0.3)' },
  TeamLeader: { label: 'Trưởng nhóm', bg: 'rgba(118,185,0,0.1)', fg: 'var(--color-primary)', bd: 'var(--color-primary)' },
  Member: { label: 'Thành viên', bg: 'var(--color-surface-soft)', fg: 'var(--color-mute)', bd: 'var(--color-hairline)' },
  TeamMember: { label: 'Thành viên', bg: 'var(--color-surface-soft)', fg: 'var(--color-mute)', bd: 'var(--color-hairline)' },
  Mentor: { label: 'Cố vấn', bg: 'rgba(13,148,136,0.08)', fg: '#0D9488', bd: 'rgba(13,148,136,0.3)' },
};

export function EventRoleBadge({ roleName }: { roleName: string }) {
  const style = ROLE_STYLE[roleName] ?? {
    label: roleName,
    bg: 'var(--color-surface-soft)',
    fg: 'var(--color-mute)',
    bd: 'var(--color-hairline)',
  };

  return (
    <span
      className="badge-tag"
      style={{
        background: style.bg,
        color: style.fg,
        border: `1px solid ${style.bd}`,
        fontSize: 'var(--fs-utility-xs)',
        fontWeight: 700,
      }}
    >
      {style.label}
    </span>
  );
}
