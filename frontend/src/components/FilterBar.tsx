import { ReactNode } from "react";

interface FilterBarProps {
  children: ReactNode;
  compact?: boolean;
}

export function FilterBar({ children, compact = false }: FilterBarProps) {
  return (
    <div
      style={{
        display: "grid",
        gap: compact ? 12 : 16,
        padding: compact ? 12 : 16,
        background: "var(--surface-2)",
        borderRadius: 8,
        border: "1px solid var(--border)",
        gridAutoFlow: "dense",
        gridAutoColumns: "minmax(120px, 1fr)",
        alignItems: "flex-end",
      }}
    >
      {children}
    </div>
  );
}

export function FilterItem({ children }: { children: ReactNode }) {
  return <div>{children}</div>;
}
