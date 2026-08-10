/** Simple placeholder shown on pages that are not built yet. */
export function ComingSoon({ title }: { title?: string }) {
  return (
    <main
      tabIndex={-1}
      style={{
        outline: "none",
        minHeight: "calc(100vh - 64px)",
        display: "flex",
        flexDirection: "column",
        alignItems: "center",
        justifyContent: "center",
        gap: "var(--space-sm)",
        textAlign: "center",
        padding: "var(--space-xxl)",
      }}
    >
      {title && (
        <h1 className="t-heading-md" style={{ margin: 0, color: "var(--color-ink)" }}>
          {title}
        </h1>
      )}
      <p className="t-body-md" style={{ margin: 0, color: "var(--color-mute)" }}>
        {"<Đang cập nhật>"}
      </p>
    </main>
  );
}
