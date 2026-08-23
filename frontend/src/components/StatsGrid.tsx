import type { ReactNode } from "react";

interface StatsGridProps {
  children: ReactNode;
  columns?: number;
}

export function StatsGrid({ children, columns = 4 }: StatsGridProps) {
  return (
    <div
      style={{
        display: "grid",
        gridTemplateColumns: `repeat(auto-fit, minmax(${280 / columns}px, 1fr))`,
        gap: 12,
        marginBottom: 20,
      }}
    >
      {children}
    </div>
  );
}

interface StatItemProps {
  label: string;
  value: ReactNode;
  subtext?: string;
  icon?: ReactNode;
  color?: string;
}

export function StatItem({ label, value, subtext, icon, color = "var(--accent)" }: StatItemProps) {
  return (
    <div
      style={{
        padding: 12,
        borderRadius: 6,
        background: "var(--surface-2)",
        border: `1px solid ${color}`,
        display: "flex",
        justifyContent: "space-between",
        alignItems: "center",
      }}
    >
      <div>
        <p style={{ fontSize: "0.75rem", color: "var(--ink-faint)", margin: 0, fontWeight: 600 }}>
          {label}
        </p>
        <div style={{ fontSize: "1.25rem", fontWeight: 700, color: color, marginTop: 4 }}>{value}</div>
        {subtext && (
          <p style={{ fontSize: "0.7rem", color: "var(--ink-faint)", margin: "2px 0 0", opacity: 0.7 }}>
            {subtext}
          </p>
        )}
      </div>
      {icon && <div style={{ fontSize: "1.75rem", opacity: 0.6 }}>{icon}</div>}
    </div>
  );
}
