import type { ReactNode } from "react";

interface Props {
  title: string;
  onClose: () => void;
  children: ReactNode;
  wide?: boolean;
}

export function Overlay({ title, onClose, children, wide = false }: Props) {
  return (
    <div
      onClick={(e) => {
        if (e.target === e.currentTarget) onClose();
      }}
      style={{
        position: "fixed",
        inset: 0,
        background: "rgba(10, 10, 12, 0.66)",
        backdropFilter: "blur(3px)",
        display: "grid",
        placeItems: wide ? "stretch end" : "center",
        zIndex: 50,
      }}
    >
      <div
        className="rise"
        style={{
          background: "var(--bg-raise)",
          border: "1px solid var(--line)",
          borderRadius: wide ? "16px 0 0 16px" : 16,
          padding: 24,
          display: "grid",
          gap: 16,
          alignContent: "start",
          width: wide ? "min(480px, 96vw)" : "min(420px, 92vw)",
          height: wide ? "100%" : "auto",
          overflowY: "auto",
          justifySelf: wide ? "end" : "center",
        }}
      >
        <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
          <h3 className="display" style={{ fontSize: "1.5rem" }}>
            {title}
          </h3>
          <button className="btn-ghost" onClick={onClose} style={{ minHeight: 38, padding: "0 12px" }}>
            ✕
          </button>
        </div>
        {children}
      </div>
    </div>
  );
}
