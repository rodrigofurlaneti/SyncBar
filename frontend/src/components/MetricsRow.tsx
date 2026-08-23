import type { ReactNode } from "react";

interface MetricsRowProps {
  metric: string;
  value: ReactNode;
  change?: number;
  unit?: string;
  icon?: ReactNode;
}

export function MetricsRow({ metric, value, change, unit = "", icon }: MetricsRowProps) {
  const isPositive = change !== undefined && change >= 0;

  return (
    <div
      style={{
        display: "flex",
        justifyContent: "space-between",
        alignItems: "center",
        padding: "8px 0",
        borderBottom: "1px solid var(--border)",
      }}
    >
      <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
        {icon && <div style={{ fontSize: "1.2rem" }}>{icon}</div>}
        <span style={{ color: "var(--ink-faint)", fontSize: "0.9rem" }}>{metric}</span>
      </div>
      <div style={{ display: "flex", alignItems: "center", gap: 8, justifyContent: "flex-end" }}>
        <span style={{ fontWeight: 700, fontSize: "1.1rem" }}>
          {value}
          {unit && <span style={{ fontSize: "0.8rem", color: "var(--ink-faint)" }}> {unit}</span>}
        </span>
        {change !== undefined && (
          <span
            style={{
              fontSize: "0.8rem",
              padding: "2px 6px",
              borderRadius: 3,
              background: isPositive ? "#e8f5e9" : "#ffebee",
              color: isPositive ? "#1b5e20" : "#b71c1c",
              fontWeight: 600,
              minWidth: 50,
              textAlign: "center",
            }}
          >
            {isPositive ? "+" : ""}{change.toFixed(1)}%
          </span>
        )}
      </div>
    </div>
  );
}
