import type { ReactNode } from "react";

interface StatsGridProps {
  children: ReactNode;
  columns?: number;
}

const GAP = 12;
const MIN_CARD_WIDTH = 200;

// `columns` é o teto de colunas em telas largas: cada faixa pede a fração exata de
// 1/columns da linha, mas nunca menos que MIN_CARD_WIDTH — em telas estreitas o
// auto-fit quebra pra menos colunas.
export function StatsGrid({ children, columns = 4 }: StatsGridProps) {
  const track = `max(${MIN_CARD_WIDTH}px, calc((100% - ${(columns - 1) * GAP}px) / ${columns}))`;
  return (
    <div
      style={{
        display: "grid",
        gridTemplateColumns: `repeat(auto-fit, minmax(${track}, 1fr))`,
        gap: GAP,
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
