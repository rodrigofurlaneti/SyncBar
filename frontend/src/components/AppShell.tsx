import { useState } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { NavLink, Outlet, useNavigate } from "react-router-dom";
import { useAuthStore } from "../stores/authStore";
import { useThemeStore } from "../stores/themeStore";
import { CashDrawer } from "../features/cash/CashDrawer";
import { useMyFeatures } from "../features/access/hooks";
import { IFoodAlertsBell } from "./IFoodAlertsBell";

// Somente o operacional fica no topo. Os itens administrativos ficam
// agrupados dentro de "Config." (só gerente/admin).
const links = [
  { to: "/", label: "Salão", feature: "Salao" },
  { to: "/delivery", label: "Delivery", feature: "Salao" },
  { to: "/reservas", label: "Reservas", feature: "Salao" },
  { to: "/clientes", label: "Clientes", feature: "Salao" },
  { to: "/produtos", label: "Cardápio", feature: "Cardapio" },
  { to: "/complementos", label: "Complementos", feature: "Cardapio" },
  { to: "/estoque", label: "Estoque", feature: "Estoque" },
  { to: "/compras", label: "Compras", feature: "Estoque" },
  { to: "/preparo", label: "Preparo", feature: "Preparo" },
];

export function AppShell() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { userName, branchId, companyId, clear } = useAuthStore();
  const { theme, toggleTheme } = useThemeStore();
  const [cashOpen, setCashOpen] = useState(false);
  const [navOpen, setNavOpen] = useState(false);
  const featuresQuery = useMyFeatures();
  const access = featuresQuery.data;
  // Fail-closed: sem resposta de acessos, nenhum link aparece.
  const canSee = (feature: string) =>
    access !== undefined && (access.canManageAccess || access.features.includes(feature));

  const closeNav = () => setNavOpen(false);

  return (
    <>
      {/* Achado de revisão (web-design-guidelines): sem skip link, quem navega só por teclado
          precisa tabular pelo menu inteiro do topbar toda vez que troca de tela. Primeiro
          elemento focável da página — invisível até receber foco. */}
      <a href="#main-content" className="skip-link">
        Pular para o conteúdo
      </a>
      <header className="topbar">
        <button
          type="button"
          className="nav-toggle"
          aria-label={navOpen ? "Fechar menu" : "Abrir menu"}
          aria-expanded={navOpen}
          aria-controls="topbar-nav"
          onClick={() => setNavOpen((open) => !open)}
        >
          {navOpen ? "✕" : "☰"}
        </button>
        <span className="brand">
          SYNC<em>BAR</em>
        </span>
        <nav id="topbar-nav" className={`topbar-nav${navOpen ? " is-open" : ""}`}>
          {links.filter((link) => canSee(link.feature)).map((link) => (
            <NavLink
              key={link.to}
              to={link.to}
              end={link.to === "/"}
              onClick={closeNav}
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
              to="/integracoes/ifood"
              onClick={closeNav}
              title="Central de integrações iFood — pedidos, cardápio, financeiro, logística e mais"
              style={({ isActive }) => ({
                padding: "8px 14px",
                borderRadius: 8,
                textDecoration: "none",
                fontFamily: "var(--font-cond)",
                fontWeight: 600,
                letterSpacing: "0.05em",
                textTransform: "uppercase" as const,
                fontSize: "0.85rem",
                color: isActive ? "#fff" : "#EA1D2C",
                background: isActive ? "#EA1D2C" : "transparent",
                border: "1px solid #EA1D2C",
              })}
            >
              🍔 iFood
            </NavLink>
          )}
          {access?.canManageAccess && (
            <NavLink
              to="/configuracoes"
              onClick={closeNav}
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
              Config.
            </NavLink>
          )}
        </nav>
        <span style={{ flex: 1 }} />
        <span
          className="chip topbar-branch-chip"
          style={{ "--dot": "var(--free)" } as React.CSSProperties}
        >
          Filial {branchId}
        </span>
        {canSee("Caixa") && (
          <button type="button" className="btn-ghost" onClick={() => setCashOpen(true)}>
            Caixa
          </button>
        )}
        {access?.canManageAccess && <IFoodAlertsBell companyId={companyId} />}
        <button
          type="button"
          className="btn-ghost btn-icon"
          aria-label={theme === "dark" ? "Ativar tema claro" : "Ativar tema escuro"}
          title={theme === "dark" ? "Tema claro" : "Tema escuro"}
          onClick={toggleTheme}
        >
          {theme === "dark" ? "☀" : "🌙"}
        </button>
        <span className="topbar-username" style={{ color: "var(--ink-dim)", fontSize: "0.92rem" }}>
          {userName}
        </span>
        <button
          type="button"
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
        <p className="error-text" role="alert" style={{ padding: "10px 22px", margin: 0 }}>
          Falha ao carregar seus acessos — a API está atualizada e rodando? (Reinicie-a
          se acabou de aplicar a funcionalidade de acessos.)
        </p>
      )}

      {/* tabIndex=-1: alvo do skip link acima — várias telas já têm seu próprio <main>
          (ex.: ProductsPage), então este é um div focável simples em vez de outro <main>
          aninhado (landmark duplicado seria inválido). */}
      <div id="main-content" tabIndex={-1}>
        <Outlet />
      </div>

      {cashOpen && <CashDrawer onClose={() => setCashOpen(false)} />}
    </>
  );
}
