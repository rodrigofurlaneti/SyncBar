import { ReactNode } from "react";

interface TabsProps {
  tabs: Array<{
    id: string;
    label: string;
    badge?: string | number;
    disabled?: boolean;
  }>;
  activeTab: string;
  onTabChange: (tabId: string) => void;
  children: ReactNode;
}

export function Tabs({ tabs, activeTab, onTabChange, children }: TabsProps) {
  return (
    <div style={{ display: "grid", gap: 16 }}>
      <div
        style={{
          display: "flex",
          gap: 0,
          borderBottom: "1px solid var(--border)",
          overflowX: "auto",
        }}
        role="tablist"
      >
        {tabs.map((tab) => (
          <button
            key={tab.id}
            type="button"
            role="tab"
            aria-selected={activeTab === tab.id}
            aria-controls={`panel-${tab.id}`}
            disabled={tab.disabled}
            onClick={() => !tab.disabled && onTabChange(tab.id)}
            style={{
              padding: "12px 16px",
              fontSize: "0.95rem",
              fontWeight: activeTab === tab.id ? 600 : 400,
              color: activeTab === tab.id ? "var(--ink)" : "var(--ink-faint)",
              background: "transparent",
              border: "none",
              borderBottom: activeTab === tab.id ? "2px solid var(--accent)" : "none",
              cursor: tab.disabled ? "not-allowed" : "pointer",
              transition: "color 0.2s, border-color 0.2s",
              opacity: tab.disabled ? 0.5 : 1,
              whiteSpace: "nowrap",
              display: "flex",
              gap: 8,
              alignItems: "center",
            }}
          >
            {tab.label}
            {tab.badge && (
              <span
                style={{
                  display: "inline-flex",
                  alignItems: "center",
                  justifyContent: "center",
                  minWidth: 20,
                  height: 20,
                  borderRadius: 10,
                  background: activeTab === tab.id ? "var(--accent)" : "var(--surface-2)",
                  color: activeTab === tab.id ? "white" : "var(--ink)",
                  fontSize: "0.75rem",
                  fontWeight: 600,
                }}
              >
                {tab.badge}
              </span>
            )}
          </button>
        ))}
      </div>

      <div
        id={`panel-${activeTab}`}
        role="tabpanel"
        aria-labelledby={activeTab}
      >
        {children}
      </div>
    </div>
  );
}
