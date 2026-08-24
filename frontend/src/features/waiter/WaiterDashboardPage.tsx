import { useMemo, useState, type CSSProperties } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { useNavigate } from "react-router-dom";
import { getTablesByBranch } from "../tables/api";
import { getOpenOrdersByBranch } from "../orders/api";
import { getComandasByBranch } from "../comandas/api";
import { useAuthStore } from "../../stores/authStore";
import { useThemeStore } from "../../stores/themeStore";
import { useMyFeatures } from "../access/hooks";
import { OrderDrawer } from "../orders/OrderDrawer";
import { CashDrawer } from "../cash/CashDrawer";
import { QueryError } from "../../components/QueryError";
import {
  OrderItemStatus,
  TableStatus,
  formatBRL,
  orderTypeLabel,
} from "../../lib/types";
import type { ComandaResponse, OrderResponse, TableResponse } from "../../lib/types";

// Modo Garçom — quadro operacional em formato de cartão mobile, pensado para
// ser aberto no celular/tablet do garçom durante o turno. É uma tela nova e
// aditiva (rota /garcom): não substitui nem altera o quadro de Mesas/Comandas
// já homologado em OrdersPage.tsx — reaproveita os mesmos dados e o mesmo
// OrderDrawer para abrir um pedido específico.

type BadgeTone = "ready" | "preparing" | "waiting";

interface OrderBadge {
  label: string;
  tone: BadgeTone;
}

// Deriva um status único e acionável a partir dos itens do pedido — o mesmo
// pedido pode ter itens em estágios diferentes, então priorizamos o que mais
// importa para quem está atendendo a mesa: primeiro "pronto" (precisa servir),
// depois "em preparo", depois "aguardando" (acabou de lançar).
function deriveOrderBadge(order: OrderResponse): OrderBadge {
  const statuses = order.items.map((item) => item.orderItemStatusId);
  if (statuses.length === 0) return { label: "Sem itens", tone: "waiting" };
  if (statuses.some((s) => s === OrderItemStatus.Pronto)) return { label: "Pronto", tone: "ready" };
  if (statuses.some((s) => s === OrderItemStatus.EmPreparo || s === OrderItemStatus.EnviadoCozinha))
    return { label: "Em preparo", tone: "preparing" };
  if (statuses.some((s) => s === OrderItemStatus.Lancado)) return { label: "Aguardando", tone: "waiting" };
  return { label: "Entregue", tone: "ready" };
}

function orderLabel(
  order: OrderResponse,
  tablesById: Map<number, TableResponse>,
  comandasById: Map<number, ComandaResponse>,
): string {
  if (order.diningTableId !== null) {
    const table = tablesById.get(order.diningTableId);
    return `Mesa ${table?.number ?? order.diningTableId}`;
  }
  if (order.comandaId !== null) {
    const comanda = comandasById.get(order.comandaId);
    return `Comanda ${comanda?.code ?? order.comandaId}`;
  }
  const type = order.orderTypeId ? orderTypeLabel[order.orderTypeId] : "Pedido";
  return order.customerName ? `${type} · ${order.customerName}` : type;
}

function elapsedLabel(openedAt: string): string {
  const minutes = Math.max(0, Math.floor((Date.now() - new Date(openedAt).getTime()) / 60_000));
  if (minutes < 60) return `há ${minutes} min`;
  const hours = Math.floor(minutes / 60);
  return `há ${hours} h`;
}

// Nome de exibição a partir do userName de login (pode ser um nome completo
// ou um e-mail) — mesma string já mostrada em AppShell, só extraímos o
// primeiro "pedaço" em vez de inventar um cadastro de nome que não existe.
function firstNameFrom(userName: string | null): string {
  if (!userName) return "Garçom";
  const beforeAt = userName.split("@")[0];
  const firstWord = beforeAt.trim().split(/[\s._-]+/)[0];
  if (!firstWord) return "Garçom";
  return firstWord.charAt(0).toUpperCase() + firstWord.slice(1).toLowerCase();
}

function initialsFrom(userName: string | null): string {
  if (!userName) return "GC";
  const base = userName.split("@")[0].trim();
  const parts = base.split(/[\s._-]+/).filter(Boolean);
  if (parts.length >= 2) return (parts[0][0] + parts[1][0]).toUpperCase();
  return base.slice(0, 2).toUpperCase();
}

const badgeToneVar: Record<BadgeTone, string> = {
  ready: "var(--w-ok)",
  preparing: "var(--w-info)",
  waiting: "var(--w-warn)",
};

type QuickActionKey = "nova" | "transferir" | "mesas" | "conta" | "dividir" | "turno";

const quickActions: { key: QuickActionKey; icon: string; label: string }[] = [
  { key: "nova", icon: "🧾", label: "Nova" },
  { key: "transferir", icon: "🔀", label: "Transferir" },
  { key: "mesas", icon: "🍽️", label: "Mesas" },
  { key: "conta", icon: "💳", label: "Conta" },
  { key: "dividir", icon: "➗", label: "Dividir" },
  { key: "turno", icon: "🕒", label: "Turno" },
];

type TabKey = "inicio" | "mesas" | "pedidos" | "mensagens" | "perfil";

const tabs: { key: TabKey; icon: string; label: string }[] = [
  { key: "inicio", icon: "🏠", label: "Início" },
  { key: "mesas", icon: "🍽️", label: "Mesas" },
  { key: "pedidos", icon: "🧾", label: "Pedidos" },
  { key: "mensagens", icon: "💬", label: "Mensagens" },
  { key: "perfil", icon: "👤", label: "Perfil" },
];

export function WaiterDashboardPage() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { branchId, userName, clear } = useAuthStore();
  const { theme, toggleTheme } = useThemeStore();
  const featuresQuery = useMyFeatures();
  const canSeeCaixa =
    featuresQuery.data?.canManageAccess || featuresQuery.data?.features.includes("Caixa");

  const [selectedOrderId, setSelectedOrderId] = useState<number | null>(null);
  const [cashOpen, setCashOpen] = useState(false);
  const [profileOpen, setProfileOpen] = useState(false);
  const [toast, setToast] = useState<string | null>(null);

  const showToast = (message: string) => {
    setToast(message);
    window.setTimeout(() => setToast((current) => (current === message ? null : current)), 2500);
  };

  const tablesQuery = useQuery({
    queryKey: ["tables", branchId],
    queryFn: () => getTablesByBranch(branchId),
    refetchInterval: 15_000,
  });

  const comandasQuery = useQuery({
    queryKey: ["comandas", branchId],
    queryFn: () => getComandasByBranch(branchId),
    refetchInterval: 15_000,
  });

  const ordersQuery = useQuery({
    queryKey: ["orders", "open", branchId],
    queryFn: () => getOpenOrdersByBranch(branchId),
    refetchInterval: 15_000,
  });

  const tablesById = useMemo(() => {
    const map = new Map<number, TableResponse>();
    for (const table of tablesQuery.data ?? []) map.set(table.id, table);
    return map;
  }, [tablesQuery.data]);

  const comandasById = useMemo(() => {
    const map = new Map<number, ComandaResponse>();
    for (const comanda of comandasQuery.data ?? []) map.set(comanda.id, comanda);
    return map;
  }, [comandasQuery.data]);

  const openTablesCount = useMemo(
    () =>
      (tablesQuery.data ?? []).filter(
        (t) => t.tableStatusId === TableStatus.Ocupada || t.tableStatusId === TableStatus.EmFechamento,
      ).length,
    [tablesQuery.data],
  );

  const totalTables = tablesQuery.data?.length ?? 0;
  const activeOrders = ordersQuery.data ?? [];
  const totalOpenAmount = useMemo(
    () => activeOrders.reduce((sum, order) => sum + order.totalAmount, 0),
    [activeOrders],
  );

  const readyItemsCount = useMemo(
    () =>
      activeOrders.reduce(
        (sum, order) => sum + order.items.filter((i) => i.orderItemStatusId === OrderItemStatus.Pronto).length,
        0,
      ),
    [activeOrders],
  );

  const latestOrders = useMemo(
    () =>
      [...activeOrders]
        .sort((a, b) => new Date(b.openedAt).getTime() - new Date(a.openedAt).getTime())
        .slice(0, 3),
    [activeOrders],
  );

  const refresh = () => {
    void queryClient.invalidateQueries({ queryKey: ["tables"] });
    void queryClient.invalidateQueries({ queryKey: ["orders"] });
    void queryClient.invalidateQueries({ queryKey: ["comandas"] });
  };

  const handleQuickAction = (key: QuickActionKey) => {
    switch (key) {
      case "nova":
      case "mesas":
      case "conta":
      case "dividir":
        navigate("/");
        break;
      case "transferir":
        showToast("Transferência de mesa — em breve.");
        break;
      case "turno":
        setCashOpen(true);
        break;
    }
  };

  const handleTab = (key: TabKey) => {
    switch (key) {
      case "inicio":
        break;
      case "mesas":
      case "pedidos":
        navigate("/");
        break;
      case "mensagens":
        showToast("Mensagens internas — em breve.");
        break;
      case "perfil":
        setProfileOpen(true);
        break;
    }
  };

  return (
    <div className="waiter-view">
      <div className="waiter-shell">
        <header className="waiter-header">
          <div className="waiter-header-top">
            <div className="waiter-avatar" aria-hidden="true">
              {initialsFrom(userName)}
            </div>
            <div className="waiter-greeting">
              <span className="waiter-greeting-hello">Olá, {firstNameFrom(userName)} 👋</span>
              <span className="waiter-online-chip">
                <span className="waiter-online-dot" /> Garçom Online
              </span>
            </div>
            <span className="waiter-spacer" />
            <button
              type="button"
              className="waiter-icon-btn"
              aria-label={theme === "dark" ? "Ativar tema claro" : "Ativar tema escuro"}
              title={theme === "dark" ? "Tema claro" : "Tema escuro"}
              onClick={toggleTheme}
            >
              {theme === "dark" ? "☀" : "🌙"}
            </button>
            <button
              type="button"
              className="waiter-icon-btn waiter-bell"
              aria-label={
                readyItemsCount > 0
                  ? `${readyItemsCount} itens prontos para servir`
                  : "Nenhum item pronto no momento"
              }
              title="Itens prontos para servir"
              onClick={() => document.getElementById("waiter-latest-orders")?.scrollIntoView({ behavior: "smooth" })}
            >
              🔔
              {readyItemsCount > 0 && <span className="waiter-bell-badge">{readyItemsCount}</span>}
            </button>
          </div>
        </header>

        <main className="waiter-body">
          {(tablesQuery.isError || ordersQuery.isError || comandasQuery.isError) && (
            <div className="waiter-card">
              {tablesQuery.isError && <QueryError error={tablesQuery.error} what="as mesas" />}
              {ordersQuery.isError && <QueryError error={ordersQuery.error} what="os pedidos abertos" />}
              {comandasQuery.isError && <QueryError error={comandasQuery.error} what="as comandas" />}
            </div>
          )}

          <div className="waiter-stats-row">
            <div className="waiter-stat-card">
              <span className="waiter-stat-value mono-num">
                {openTablesCount}
                <small>/{totalTables || "—"}</small>
              </span>
              <span className="waiter-stat-label">Mesas abertas no salão</span>
            </div>
            <div className="waiter-stat-card">
              <span className="waiter-stat-value mono-num">{activeOrders.length}</span>
              <span className="waiter-stat-label">Pedidos ativos em andamento</span>
            </div>
          </div>

          <div className="waiter-highlight-card">
            <span className="waiter-highlight-label">Comandas em aberto</span>
            <span className="waiter-highlight-value mono-num">{formatBRL(totalOpenAmount)}</span>
            <span className="waiter-highlight-sub">
              {activeOrders.length} pedido{activeOrders.length === 1 ? "" : "s"} em aberto agora
            </span>
          </div>

          <section id="waiter-latest-orders" className="waiter-section">
            <div className="waiter-section-head">
              <h2 className="waiter-section-title">Últimos pedidos</h2>
              <button type="button" className="waiter-link-btn" onClick={() => navigate("/")}>
                Ver todos
              </button>
            </div>

            {latestOrders.length === 0 ? (
              <p className="waiter-empty">Nenhum pedido em aberto no momento.</p>
            ) : (
              <div className="waiter-order-list">
                {latestOrders.map((order) => {
                  const badge = deriveOrderBadge(order);
                  return (
                    <button
                      key={order.id}
                      type="button"
                      className="waiter-order-row"
                      onClick={() => setSelectedOrderId(order.id)}
                    >
                      <span className="waiter-order-info">
                        <span className="waiter-order-title">{orderLabel(order, tablesById, comandasById)}</span>
                        <span className="waiter-order-meta">
                          {order.items.length} {order.items.length === 1 ? "item" : "itens"} · {elapsedLabel(order.openedAt)}
                        </span>
                      </span>
                      <span
                        className="waiter-order-badge"
                        style={{ "--w-badge": badgeToneVar[badge.tone] } as CSSProperties}
                      >
                        {badge.label}
                      </span>
                    </button>
                  );
                })}
              </div>
            )}
          </section>

          <button type="button" className="waiter-cta" onClick={() => navigate("/")}>
            + Nova comanda
          </button>

          <section className="waiter-section">
            <h2 className="waiter-section-title">Ações rápidas</h2>
            <div className="waiter-quick-grid">
              {quickActions
                .filter((action) => action.key !== "turno" || canSeeCaixa)
                .map((action) => (
                  <button
                    key={action.key}
                    type="button"
                    className="waiter-quick-tile"
                    onClick={() => handleQuickAction(action.key)}
                  >
                    <span className="waiter-quick-icon" aria-hidden="true">{action.icon}</span>
                    <span className="waiter-quick-label">{action.label}</span>
                  </button>
                ))}
            </div>
          </section>
        </main>

        <nav className="waiter-tabbar" aria-label="Navegação do Modo Garçom">
          {tabs.map((tab) => (
            <button
              key={tab.key}
              type="button"
              className={`waiter-tab${tab.key === "inicio" ? " is-active" : ""}`}
              onClick={() => handleTab(tab.key)}
            >
              <span aria-hidden="true">{tab.icon}</span>
              <span>{tab.label}</span>
            </button>
          ))}
        </nav>
      </div>

      {toast && (
        <div className="waiter-toast" role="status">
          {toast}
        </div>
      )}

      {selectedOrderId !== null && (
        <OrderDrawer
          orderId={selectedOrderId}
          onClose={() => {
            setSelectedOrderId(null);
            refresh();
          }}
        />
      )}

      {cashOpen && <CashDrawer onClose={() => setCashOpen(false)} />}

      {profileOpen && (
        <div className="modal-backdrop is-center" onClick={() => setProfileOpen(false)}>
          <div className="modal-panel is-center" onClick={(e) => e.stopPropagation()}>
            <div className="modal-head">
              <span className="display" style={{ fontSize: "1.3rem" }}>Perfil</span>
              <button type="button" className="btn-ghost btn-icon" aria-label="Fechar" onClick={() => setProfileOpen(false)}>
                ✕
              </button>
            </div>
            <div style={{ display: "grid", gap: 4 }}>
              <span style={{ color: "var(--ink-dim)", fontSize: "0.85rem" }}>Usuário</span>
              <span>{userName ?? "—"}</span>
            </div>
            <div style={{ display: "grid", gap: 4 }}>
              <span style={{ color: "var(--ink-dim)", fontSize: "0.85rem" }}>Filial</span>
              <span>Filial {branchId}</span>
            </div>
            <button
              type="button"
              className="btn-danger btn-block"
              onClick={() => {
                queryClient.clear();
                clear();
                navigate("/login", { replace: true });
              }}
            >
              Sair
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
