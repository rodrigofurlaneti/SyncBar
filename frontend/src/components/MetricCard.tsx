import { ReactNode } from "react";

interface MetricCardProps {
  label: string;
  value: string | number;
  subtitle?: string;
  icon?: ReactNode;
  trend?: "up" | "down" | "neutral";
  trendValue?: string;
  onClick?: () => void;
  loading?: boolean;
}

export function MetricCard({
  label,
  value,
  subtitle,
  icon,
  trend,
  trendValue,
  onClick,
  loading,
}: MetricCardProps) {
  const trendColor =
    trend === "up"
      ? "var(--success)"
      : trend === "down"
        ? "var(--error)"
        : "var(--ink-faint)";

  const trendSymbol = trend === "up" ? "↑" : trend === "down" ? "↓" : "–";

  return (
    <div
      onClick={onClick}
      style={{
        padding: 16,
        borderRadius: 8,
        background: "var(--surface-2)",
        border: onClick ? "1px solid var(--border)" : "none",
        cursor: onClick ? "pointer" : "default",
        display: "grid",
        gap: 8,
        transition: "all 0.2s",
      }}
      onMouseEnter={(e) => {
        if (onClick) {
          e.currentTarget.style.background = "var(--surface-3)";
          e.currentTarget.style.borderColor = "var(--ink-faint)";
        }
      }}
      onMouseLeave={(e) => {
        e.currentTarget.style.background = "var(--surface-2)";
        e.currentTarget.style.borderColor = "transparent";
      }}
    >
      <div style={{ display: "flex", gap: 8, alignItems: "flex-start", justifyContent: "space-between" }}>
        <div>
          <p style={{ fontSize: "0.85rem", color: "var(--ink-faint)", margin: 0, marginBottom: 4 }}>
            {label}
          </p>
          {loading ? (
            <div
              style={{
                width: 100,
                height: 28,
                background: "var(--surface-1)",
                borderRadius: 4,
                animation: "pulse 2s infinite",
              }}
            />
          ) : (
            <div style={{ fontSize: "1.75rem", fontWeight: 600, margin: 0 }}>
              {value}
            </div>
          )}
          {subtitle && (
            <p style={{ fontSize: "0.75rem", color: "var(--ink-faint)", margin: "4px 0 0" }}>
              {subtitle}
            </p>
          )}
        </div>
        {icon && <div style={{ opacity: 0.6 }}>{icon}</div>}
      </div>

      {trendValue && (
        <div style={{ fontSize: "0.85rem", color: trendColor, display: "flex", gap: 4 }}>
          <span>{trendSymbol}</span>
          <span>{trendValue}</span>
        </div>
      )}
    </div>
  );
}
