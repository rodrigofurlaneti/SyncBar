import { ReactNode } from "react";

interface PageHeaderProps {
  title: string;
  subtitle?: string;
  actions?: ReactNode;
  breadcrumb?: Array<{ label: string; href: string }>;
}

export function PageHeader({ title, subtitle, actions, breadcrumb }: PageHeaderProps) {
  return (
    <header style={{ marginBottom: 24, display: "grid", gap: 16 }}>
      {breadcrumb && breadcrumb.length > 0 && (
        <nav style={{ fontSize: "0.85rem" }}>
          {breadcrumb.map((crumb, idx) => (
            <span key={idx}>
              {idx > 0 && <span style={{ margin: "0 8px", color: "var(--ink-faint)" }}>/</span>}
              <a href={crumb.href} style={{ color: "var(--ink)", textDecoration: "none" }}>
                {crumb.label}
              </a>
            </span>
          ))}
        </nav>
      )}

      <div style={{ display: "grid", gap: 4 }}>
        <h1 style={{ fontSize: "2rem", fontWeight: 600, margin: 0 }}>{title}</h1>
        {subtitle && (
          <p style={{ fontSize: "0.95rem", color: "var(--ink-faint)", margin: 0 }}>
            {subtitle}
          </p>
        )}
      </div>

      {actions && (
        <div style={{ display: "flex", gap: 12, justifyContent: "flex-end" }}>
          {actions}
        </div>
      )}
    </header>
  );
}
