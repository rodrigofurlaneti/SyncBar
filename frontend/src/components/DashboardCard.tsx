import type { ReactNode } from "react";

interface DashboardCardProps {
  title: string;
  value?: ReactNode;
  icon?: ReactNode;
  subtitle?: string;
  trend?: { direction: "up" | "down" | "neutral"; percentage: number };
  status?: "success" | "warning" | "error" | "info";
  onClick?: () => void;
  loading?: boolean;
  children?: ReactNode;
}

export function DashboardCard({
  title,
  value,
  icon,
  subtitle,
  trend,
  status = "info",
  onClick,
  loading = false,
  children,
}: DashboardCardProps) {
  const statusColors = {
    success: { bg: "#e8f5e9", border: "#4caf50", text: "#1b5e20", icon: "✓" },
    warning: { bg: "#fff3e0", border: "#f57c00", text: "#e65100", icon: "⚠" },
    error: { bg: "#ffebee", border: "#f44336", text: "#b71c1c", icon: "✕" },
    info: { bg: "#e3f2fd", border: "#2196f3", text: "#0d47a1", icon: "ℹ" },
  };

  const colors = statusColors[status];

  return (
    <div
      onClick={onClick}
      onKeyDown={
        onClick
          ? (e) => {
              if (e.key === "Enter" || e.key === " ") {
                e.preventDefault();
                onClick();
              }
            }
          : undefined
      }
      role={onClick ? "button" : undefined}
      tabIndex={onClick ? 0 : undefined}
      style={{
        padding: 16,
        borderRadius: 8,
        background: colors.bg,
        border: `2px solid ${colors.border}`,
        cursor: onClick ? "pointer" : "default",
        transition: onClick ? "transform 0.2s, box-shadow 0.2s" : "none",
        display: "flex",
        flexDirection: "column",
        gap: 12,
      }}
      onMouseEnter={(e) => {
        if (onClick) {
          (e.currentTarget as HTMLElement).style.transform = "translateY(-4px)";
          (e.currentTarget as HTMLElement).style.boxShadow = "0 4px 12px rgba(0,0,0,0.1)";
        }
      }}
      onMouseLeave={(e) => {
        if (onClick) {
          (e.currentTarget as HTMLElement).style.transform = "translateY(0)";
          (e.currentTarget as HTMLElement).style.boxShadow = "none";
        }
      }}
    >
      <div style={{ display: "flex", justifyContent: "space-between", alignItems: "flex-start" }}>
        <div>
          <p style={{ fontSize: "0.75rem", fontWeight: 600, color: colors.text, margin: 0, opacity: 0.8 }}>
            {title}
          </p>
          {subtitle && (
            <p style={{ fontSize: "0.7rem", color: colors.text, margin: "4px 0 0", opacity: 0.6 }}>
              {subtitle}
            </p>
          )}
        </div>
        {icon && <div style={{ fontSize: "1.5rem" }}>{icon}</div>}
      </div>

      {loading ? (
        <div style={{ height: 24, background: "rgba(255,255,255,0.3)", borderRadius: 4 }} />
      ) : (
        <>
          {value && (
            <div
              style={{
                fontSize: "1.5rem",
                fontWeight: 700,
                color: colors.text,
                whiteSpace: "nowrap",
                overflow: "hidden",
                textOverflow: "ellipsis",
              }}
            >
              {value}
            </div>
          )}

          {trend && (
            <div style={{ display: "flex", alignItems: "center", gap: 4 }}>
              <span
                style={{
                  fontSize: "0.85rem",
                  color: trend.direction === "up" ? "#4caf50" : trend.direction === "down" ? "#f44336" : "#999",
                }}
              >
                {trend.direction === "up" ? "↑" : trend.direction === "down" ? "↓" : "→"}
                {trend.percentage.toFixed(1)}%
              </span>
            </div>
          )}
        </>
      )}

      {children}
    </div>
  );
}
