import { ReactNode } from "react";

interface DataTableProps {
  columns: Array<{
    key: string;
    label: string;
    width?: string;
    align?: "left" | "center" | "right";
    render?: (value: any) => ReactNode;
  }>;
  data: any[];
  rowKey: string | ((item: any, idx: number) => string);
  loading?: boolean;
  emptyMessage?: string;
  onRowClick?: (item: any) => void;
  stickyHeader?: boolean;
  compact?: boolean;
}

export function DataTable({
  columns,
  data,
  rowKey,
  loading,
  emptyMessage = "Nenhum dado encontrado",
  onRowClick,
  stickyHeader = false,
  compact = false,
}: DataTableProps) {
  const getRowKey = (item: any, idx: number) =>
    typeof rowKey === "function" ? rowKey(item, idx) : item[rowKey];

  const paddingY = compact ? 8 : 12;
  const fontSize = compact ? "0.9rem" : "1rem";

  return (
    <div
      style={{
        overflowX: "auto",
        borderRadius: 8,
        border: "1px solid var(--border)",
        background: "var(--surface-1)",
      }}
    >
      <table
        style={{
          width: "100%",
          borderCollapse: "collapse",
          fontSize,
        }}
      >
        <thead
          style={{
            background: "var(--surface-2)",
            position: stickyHeader ? "sticky" : undefined,
            top: 0,
            zIndex: 1,
          }}
        >
          <tr>
            {columns.map((col) => (
              <th
                key={col.key}
                style={{
                  padding: `${paddingY}px 12px`,
                  textAlign: col.align || "left",
                  fontWeight: 600,
                  fontSize: "0.9rem",
                  color: "var(--ink-faint)",
                  borderBottom: "1px solid var(--border)",
                  width: col.width,
                }}
              >
                {col.label}
              </th>
            ))}
          </tr>
        </thead>
        <tbody>
          {loading && (
            <tr>
              <td colSpan={columns.length} style={{ textAlign: "center", padding: "24px 12px" }}>
                <div style={{ color: "var(--ink-faint)" }}>Carregando dados...</div>
              </td>
            </tr>
          )}

          {!loading && data.length === 0 && (
            <tr>
              <td colSpan={columns.length} style={{ textAlign: "center", padding: "24px 12px" }}>
                <div style={{ color: "var(--ink-faint)" }}>{emptyMessage}</div>
              </td>
            </tr>
          )}

          {!loading &&
            data.map((item, idx) => (
              <tr
                key={getRowKey(item, idx)}
                onClick={() => onRowClick?.(item)}
                style={{
                  cursor: onRowClick ? "pointer" : "default",
                  borderBottom: "1px solid var(--border)",
                  transition: "background 0.2s",
                }}
                onMouseEnter={(e) => {
                  if (onRowClick) {
                    e.currentTarget.style.background = "var(--surface-2)";
                  }
                }}
                onMouseLeave={(e) => {
                  e.currentTarget.style.background = "transparent";
                }}
              >
                {columns.map((col) => (
                  <td
                    key={col.key}
                    style={{
                      padding: `${paddingY}px 12px`,
                      textAlign: col.align || "left",
                      borderBottom: "1px solid var(--border)",
                    }}
                  >
                    {col.render ? col.render(item[col.key]) : item[col.key]}
                  </td>
                ))}
              </tr>
            ))}
        </tbody>
      </table>
    </div>
  );
}
