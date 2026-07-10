import { useState } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { NavLink, Outlet, useNavigate } from "react-router-dom";
import { useAuthStore } from "../stores/authStore";
import { CashDrawer } from "../features/cash/CashDrawer";
import { useMyFeatures } from "../features/access/hooks";

const links = [
  { to: "/", label: "Salão", feature: "Salao" },
  { to: "/produtos", label: "Cardápio", feature: "Cardapio" },
  { to: "/estoque", label: "Estoque", feature: "Estoque" },
  { to: "/equipe", label: "Equipe", feature: "Equipe" },
  { to: "/usuarios", label: "Usuários", feature: "Usuarios" },
  { to: "/faturamento", label: "Faturamento", feature: "Faturamento" },
  { to: "/cenarios", label: "Cenários", feature: "Faturamento" },
  { to: "/preparo", label: "Preparo", feature: "Preparo" },
  { to: "/fechamentos", label: "Fechamentos", feature: "Caixa" },
];

export function AppShell() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { userName, branchId, clear } = useAuthStore();
  const [cashOpen, setCashOpen] = useState(false);
  const featuresQuery = useMyFeatures();
  const access = featuresQuery.data;
  // Fail-closed: sem resposta de acessos, nenhum link aparece.
  const canSee = (feature: string) =>
    access !== undefined && (access.canManageAccess || access.features.includes(feature));

  return (
    <>
      <header className="topbar">
        <span className="brand">
          SYNC<em>BAR</em>
        </span>
        <nav style={{ display: "flex", gap: 4 }}>
          {links.filter((link) => canSee(link.feature)).map((link) => (
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
          {access?.canManageAccess && (
            <NavLink
              to="/acessos"
              style={({ isActive }) => ({
                padding: "8px 14px",
                borderRadius: 8,
                textDecoration: "none",
                fontFamily: "var(--font-cond)",
                fontWeight: 600,
                letterSpacing: "0.05em",
                textTransform: "uppercase" as const,
                fontSize: "0.85rem",
                color: isActive ? "var(--amber-ink)" : "var(--amber)",
                background: isActive ? "var(--amber)" : "transparent",
                border: "1px dashed var(--amber-deep)",
              })}
            >
              Acessos
            </NavLink>
          )}
        </nav>
        <span style={{ flex: 1 }} />
        <span className="chip" style={{ "--dot": "var(--free)" } as React.CSSProperties}>
          Filial {branchId}
        </span>
        {canSee("Caixa") && (
          <button className="btn-ghost" onClick={() => setCashOpen(true)}>
            Caixa
          </button>
        )}
        <span style={{ color: "var(--ink-dim)", fontSize: "0.92rem" }}>{userName}</span>
        <button
          className="btn-ghost"
          onClick={() => {
            queryClient.clear();
            clear();
            navigate("/login", { replace: true });
          }}
        >
          Sair
        </button>
      </header>

      {featuresQuery.isError && (
        <p className="error-text" style={{ padding: "10px 22px", margin: 0 }}>
          Falha ao carregar seus acessos — a API está atualizada e rodando? (Reinicie-a
          se acabou de aplicar a funcionalidade de acessos.)
        </p>
      )}

      <Outlet />

      {cashOpen && <CashDrawer onClose={() => setCashOpen(false)} />}
    </>
  );
}
