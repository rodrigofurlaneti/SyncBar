import { useState } from "react";
import { NavLink, Outlet, useNavigate } from "react-router-dom";
import { useAuthStore } from "../stores/authStore";
import { CashDrawer } from "../features/cash/CashDrawer";

const links = [
  { to: "/", label: "Salão" },
  { to: "/produtos", label: "Cardápio" },
  { to: "/estoque", label: "Estoque" },
  { to: "/equipe", label: "Equipe" },
  { to: "/usuarios", label: "Usuários" },
];

export function AppShell() {
  const navigate = useNavigate();
  const { userName, branchId, clear } = useAuthStore();
  const [cashOpen, setCashOpen] = useState(false);

  return (
    <>
      <header className="topbar">
        <span className="brand">
          SYNC<em>BAR</em>
        </span>
        <nav style={{ display: "flex", gap: 4 }}>
          {links.map((link) => (
            <NavLink
              key={link.to}
              to={link.to}
              end={link.to === "/"}
              style={({ isActive }) => ({
                padding: "8px 14px",
                borderRadius: 8,
                textDecoration: "none",
                fontFamily: "var(--font-cond)",
                fontWeight: 600,
                letterSpacing: "0.05em",
                textTransform: "uppercase" as const,
                fontSize: "0.85rem",
                color: isActive ? "var(--amber-ink)" : "var(--ink-dim)",
                background: isActive ? "var(--amber)" : "transparent",
              })}
            >
              {link.label}
            </NavLink>
          ))}
        </nav>
        <span style={{ flex: 1 }} />
        <span className="chip" style={{ "--dot": "var(--free)" } as React.CSSProperties}>
          Filial {branchId}
        </span>
        <button className="btn-ghost" onClick={() => setCashOpen(true)}>
          Caixa
        </button>
        <span style={{ color: "var(--ink-dim)", fontSize: "0.92rem" }}>{userName}</span>
        <button
          className="btn-ghost"
          onClick={() => {
            clear();
            navigate("/login", { replace: true });
          }}
        >
          Sair
        </button>
      </header>

      <Outlet />

      {cashOpen && <CashDrawer onClose={() => setCashOpen(false)} />}
    </>
  );
}
