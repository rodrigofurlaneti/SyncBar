import { ReactNode } from "react";

interface AlertProps {
  variant: "info" | "success" | "warning" | "error";
  title?: string;
  message: ReactNode;
  action?: {
    label: string;
    onClick: () => void;
  };
  onClose?: () => void;
}

export function Alert({ variant, title, message, action, onClose }: AlertProps) {
  const colors = {
    info: { bg: "#e3f2fd", border: "#1976d2", icon: "ℹ️", text: "#0d47a1" },
    success: { bg: "#e8f5e9", border: "#388e3c", icon: "✓", text: "#1b5e20" },
    warning: { bg: "#fff3e0", border: "#f57c00", icon: "⚠️", text: "#e65100" },
    error: { bg: "#ffebee", border: "#d32f2f", icon: "✕", text: "#b71c1c" },
  };

  const color = colors[variant];

  return (
    <div
      style={{
        padding: 12,
        borderRadius: 6,
        background: color.bg,
        border: `1px solid ${color.border}`,
        display: "grid",
        gap: 8,
        gridAutoFlow: "column",
        alignItems: "start",
        justifyContent: "space-between",
      }}
      role={variant === "error" ? "alert" : "status"}
    >
      <div style={{ display: "flex", gap: 8 }}>
        <div style={{ fontSize: "1.2rem", flexShrink: 0 }}>{color.icon}</div>
        <div style={{ display: "grid", gap: 4, color: color.text }}>
          {title && <strong style={{ fontSize: "0.95rem" }}>{title}</strong>}
          <div style={{ fontSize: "0.9rem" }}>{message}</div>
          {action && (
            <button
              type="button"
              onClick={action.onClick}
              style={{
                background: "transparent",
                border: "none",
                color: color.border,
                cursor: "pointer",
                fontSize: "0.85rem",
                fontWeight: 600,
                marginTop: 4,
                textDecoration: "underline",
              }}
            >
              {action.label}
            </button>
          )}
        </div>
      </div>

      {onClose && (
        <button
          type="button"
          onClick={onClose}
          style={{
            background: "transparent",
            border: "none",
            color: color.text,
            cursor: "pointer",
            fontSize: "1rem",
            padding: 0,
            lineHeight: 1,
          }}
          aria-label="Fechar alerta"
        >
          ✕
        </button>
      )}
    </div>
  );
}
