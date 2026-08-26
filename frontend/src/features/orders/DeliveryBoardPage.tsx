import { useEffect, useMemo, useRef, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { cancelOrder, getOpenOrdersByBranch, getOrder, updateItemStatus } from "./api";
import { useAuthStore } from "../../stores/authStore";
import { useToast } from "../../ui/Toast";
import { useDialog } from "../../ui/Dialog";
import { ApiError } from "../../lib/apiClient";
import { QueryError } from "../../components/QueryError";
import { OrderDrawer } from "./OrderDrawer";
import { OpenDeliveryOrderDialog } from "./OpenDeliveryOrderDialog";
import {
  OrderItemStatus,
  OrderStatus,
  OrderType,
  formatBRL,
} from "../../lib/types";
import type { OrderResponse } from "../../lib/types";

// ---------------------------------------------------------------------------
// Quadro de Delivery/Retirada — dois modos de visualização (Simples/Completo)
// sobre os pedidos SEM mesa/comanda (abertos via "+ Retirada / Delivery" em
// OrdersPage.tsx / OpenDeliveryOrderDialog.tsx).
//
// Importante — limites conhecidos deste primeiro recorte (só frontend, ver
// conversa): o backend ainda não tem um campo de "estágio de entrega"
// (aguardando → saiu para entrega → entregue) nem "Agendamento". Este quadro:
//   1. deriva Novos/Cozinha/Aguardando a partir de OrderStatus + OrderItemStatus
//      (dados reais, já existentes);
//   2. marca "Em rota" com um estado só do navegador (localStorage) — não
//      sincroniza entre operadores/dispositivos. Fica como próximo passo de
//      backend se o time quiser um campo real (ex.: DeliveryStatus);
//   3. mostra "Agendamento" como coluna vazia/desabilitada — recurso ainda não
//      implementado, mantida só para bater com o layout de referência.
// ---------------------------------------------------------------------------

type Stage = "novo" | "cozinha" | "aguardando" | "rota" | "entregue" | "cancelado";
type ViewMode = "simples" | "completo";
type ChannelFilter = "todos" | "delivery" | "retirada";

const VIEW_MODE_KEY = "syncbar:delivery-view-mode";
const ON_ROUTE_KEY = "syncbar:delivery-on-route";
const GHOST_LIMIT = 40;

const CHANNEL_FILTER_LABELS: Record<ChannelFilter, string> = {
  todos: "Todos",
  delivery: "Delivery",
  retirada: "Retirada",
};

function loadViewMode(): ViewMode {
  try {
    const stored = localStorage.getItem(VIEW_MODE_KEY);
    return stored === "completo" ? "completo" : "simples";
  } catch {
    return "simples";
  }
}

function loadOnRoute(): Set<number> {
  try {
    const stored = localStorage.getItem(ON_ROUTE_KEY);
    if (!stored) return new Set();
    return new Set(JSON.parse(stored) as number[]);
  } catch {
    return new Set();
  }
}

function persistOnRoute(ids: Set<number>) {
  try {
    localStorage.setItem(ON_ROUTE_KEY, JSON.stringify([...ids]));
  } catch {
  }
}

const isDeliveryBoardOrder = (order: OrderResponse) =>
  order.diningTableId === null && order.comandaId === null;

const getChannel = (order: OrderResponse): "delivery" | "retirada" =>
  order.orderTypeId === OrderType.Delivery ? "delivery" : "retirada";

const READY_ITEM_STATUSES = new Set<number>([OrderItemStatus.Pronto, OrderItemStatus.Entregue]);

function deriveStage(order: OrderResponse, onRoute: Set<number>): Stage {
  if (order.orderStatusId === OrderStatus.Cancelado) return "cancelado";
  if (order.orderStatusId === OrderStatus.Pago) return "entregue";

  const activeItems = order.items.filter((i) => i.orderItemStatusId !== OrderItemStatus.Cancelado);
  const allReady = activeItems.length > 0 && activeItems.every((i) => READY_ITEM_STATUSES.has(i.orderItemStatusId));
  const anyStarted = activeItems.some((i) => i.orderItemStatusId >= OrderItemStatus.EnviadoCozinha);

  if (order.orderStatusId === OrderStatus.AguardandoPagamento || allReady)
    return onRoute.has(order.id) ? "rota" : "aguardando";
  if (anyStarted) return "cozinha";
  return "novo";
}

function elapsedLabel(openedAt: string): string {
  const opened = new Date(openedAt).getTime();
  const minutes = Math.max(0, Math.round((Date.now() - opened) / 60_000));
  if (minutes < 60) return `${minutes} min`;
  const hours = Math.floor(minutes / 60);
  return `${hours}h${String(minutes % 60).padStart(2, "0")}`;
}

interface ColumnDef {
  id: Stage | "agendamento";
  label: string;
  hint: string;
  placeholder?: boolean;
}

const SIMPLE_COLUMNS: ColumnDef[] = [
  { id: "novo", label: "Novos pedidos", hint: "Aguardando aceite" },
  { id: "cozinha", label: "Cozinha", hint: "Aguardando preparo" },
  { id: "aguardando", label: "Aguardando entrega", hint: "Pronto pra sair" },
  { id: "rota", label: "Em rota", hint: "Com o entregador" },
  { id: "entregue", label: "Entregue", hint: "Pedidos finalizados" },
];

const FULL_COLUMNS: ColumnDef[] = [
  { id: "novo", label: "Novos pedidos", hint: "Aguardando aceite" },
  { id: "cozinha", label: "Cozinha", hint: "Aguardando preparo" },
  { id: "aguardando", label: "Aguardando entrega", hint: "Separação e conferência" },
  { id: "rota", label: "Saiu para entrega", hint: "Pedidos em rota" },
  { id: "entregue", label: "Entregue", hint: "Última hora" },
  { id: "agendamento", label: "Agendamento", hint: "Em breve", placeholder: true },
  { id: "cancelado", label: "Cancelados", hint: "Hoje" },
];

function OrderCard({
  order,
  stage,
  dense,
  onOpen,
  onSendToKitchen,
  onMarkReady,
  onMarkOnRoute,
  onCancel,
  busy,
}: {
  order: OrderResponse;
  stage: Stage;
  dense: boolean;
  onOpen: () => void;
  onSendToKitchen: () => void;
  onMarkReady: () => void;
  onMarkOnRoute: () => void;
  onCancel: () => void;
  busy: boolean;
}) {
  const customerName = order.customerName?.trim() || `Pedido #${order.id}`;
  const channelLabel = getChannel(order) === "delivery" ? "DELIVERY" : "RETIRADA";

  const stop = (fn: () => void) => (e: React.MouseEvent) => {
    e.stopPropagation();
    fn();
  };

  return (
    <button type="button" className="kanban-card" onClick={onOpen}>
      <div className="kanban-card-head">
        <span className="mono-num" style={{ fontWeight: 700 }}>
          #{order.id}
        </span>
        <span className="chip" style={{ "--dot": "var(--reserved)" } as React.CSSProperties}>
          {channelLabel}
        </span>
      </div>

      <div style={{ display: "grid", gap: 2, textAlign: "left" }}>
        <span style={{ fontWeight: 600 }}>{customerName}</span>
        {!dense && order.deliveryAddress && (
          <span style={{ fontSize: "0.8rem", color: "var(--ink-faint)" }}>
            {order.deliveryAddress}
          </span>
        )}
      </div>

      <div style={{ display: "flex", justifyContent: "space-between", fontSize: "0.82rem", color: "var(--ink-dim)" }}>
        <span>{order.items.length} {order.items.length === 1 ? "item" : "itens"}</span>
        <span className="mono-num">{formatBRL(order.totalAmount)}</span>
      </div>

      <div style={{ fontSize: "0.75rem", color: "var(--ink-faint)" }}>
        aberto há {elapsedLabel(order.openedAt)}
      </div>

      {stage === "novo" && (
        <button type="button" className="btn-primary btn-sm" disabled={busy} onClick={stop(onSendToKitchen)}>
          {busy ? "Enviando…" : "Enviar p/ cozinha"}
        </button>
      )}
      {stage === "cozinha" && (
        <button type="button" className="btn-primary btn-sm" disabled={busy} onClick={stop(onMarkReady)}>
          {busy ? "Atualizando…" : "Pronto p/ saída"}
        </button>
      )}
      {stage === "aguardando" && (
        <button type="button" className="btn-primary btn-sm" disabled={busy} onClick={stop(onMarkOnRoute)}>
          Saiu para entrega
        </button>
      )}
      {stage === "rota" && (
        <button type="button" className="btn-primary btn-sm" disabled={busy} onClick={stop(onOpen)}>
          Confirmar entrega
        </button>
      )}
      {(stage === "novo" || stage === "cozinha" || stage === "aguardando") && (
        <button
          type="button"
          className="btn-danger btn-sm"
          disabled={busy}
          onClick={stop(onCancel)}
        >
          Cancelar pedido
        </button>
      )}
    </button>
  );
}

export function DeliveryBoardPage() {
  const queryClient = useQueryClient();
  const toast = useToast();
  const dialog = useDialog();
  const { branchId, employeeId } = useAuthStore();

  const [viewMode, setViewMode] = useState<ViewMode>(loadViewMode);
  const [channelFilter, setChannelFilter] = useState<ChannelFilter>("todos");
  const [search, setSearch] = useState("");
  const [onRoute, setOnRoute] = useState<Set<number>>(loadOnRoute);
  const [selectedOrderId, setSelectedOrderId] = useState<number | null>(null);
  const [openingNew, setOpeningNew] = useState(false);
  const [pendingOrderId, setPendingOrderId] = useState<number | null>(null);
  const [ghosts, setGhosts] = useState<Map<number, OrderResponse>>(new Map());
  const knownRef = useRef<Map<number, OrderResponse>>(new Map());
  const fetchingRef = useRef<Set<number>>(new Set());

  useEffect(() => {
    localStorage.setItem(VIEW_MODE_KEY, viewMode);
  }, [viewMode]);

  const ordersQuery = useQuery({
    queryKey: ["orders", "open", branchId],
    queryFn: () => getOpenOrdersByBranch(branchId),
    refetchInterval: 15_000,
  });

  useEffect(() => {
    if (!ordersQuery.data) return;
    const boardOrders = ordersQuery.data.filter(isDeliveryBoardOrder);
    const currentIds = new Set(boardOrders.map((o) => o.id));

    const vanished: number[] = [];
    for (const id of knownRef.current.keys()) {
      if (!currentIds.has(id)) vanished.push(id);
    }
    for (const order of boardOrders) knownRef.current.set(order.id, order);

    vanished.forEach((id) => {
      if (fetchingRef.current.has(id)) return;
      fetchingRef.current.add(id);
      knownRef.current.delete(id);
      getOrder(id)
        .then((finalOrder) => {
          setGhosts((prev) => {
            const next = new Map(prev);
            next.set(id, finalOrder);
            while (next.size > GHOST_LIMIT) {
              const oldest = next.keys().next().value;
              if (oldest === undefined) break;
              next.delete(oldest);
            }
            return next;
          });
          setOnRoute((prevRoute) => {
            if (!prevRoute.has(id)) return prevRoute;
            const next = new Set(prevRoute);
            next.delete(id);
            persistOnRoute(next);
            return next;
          });
        })
        .catch(() => {
        })
        .finally(() => fetchingRef.current.delete(id));
    });
  }, [ordersQuery.data]);

  const boardOrders = useMemo(() => {
    const map = new Map<number, OrderResponse>();
    for (const order of ghosts.values()) if (isDeliveryBoardOrder(order)) map.set(order.id, order);
    for (const order of ordersQuery.data ?? []) if (isDeliveryBoardOrder(order)) map.set(order.id, order);
    return [...map.values()];
  }, [ordersQuery.data, ghosts]);

  const filteredOrders = useMemo(() => {
    const term = search.trim().toLowerCase();
    return boardOrders.filter((order) => {
      if (channelFilter !== "todos" && getChannel(order) !== channelFilter) return false;
      if (term === "") return true;
      const haystack = [
        String(order.id),
        order.customerName ?? "",
        order.deliveryAddress ?? "",
        order.customerPhone ?? "",
      ]
        .join(" ")
        .toLowerCase();
      return haystack.includes(term);
    });
  }, [boardOrders, channelFilter, search]);

  const ordersByStage = useMemo(() => {
    const map: Record<Stage, OrderResponse[]> = {
      novo: [], cozinha: [], aguardando: [], rota: [], entregue: [], cancelado: [],
    };
    for (const order of filteredOrders) map[deriveStage(order, onRoute)].push(order);
    for (const list of Object.values(map)) list.sort((a, b) => a.id - b.id);
    return map;
  }, [filteredOrders, onRoute]);

  const refresh = () => void queryClient.invalidateQueries({ queryKey: ["orders"] });

  const onErr = (fallback: string) => (error: unknown) =>
    toast.error(error instanceof ApiError ? error.message : fallback);

  const sendToKitchen = useMutation({
    mutationFn: async (order: OrderResponse) => {
      const pending = order.items.filter((i) => i.orderItemStatusId === OrderItemStatus.Lancado);
      await Promise.all(
        pending.map((i) => updateItemStatus(order.id, i.id, OrderItemStatus.EnviadoCozinha, employeeId)),
      );
    },
    onSuccess: (_data, order) => {
      toast.success(`Pedido #${order.id} enviado para a cozinha.`);
      setPendingOrderId(null);
      refresh();
    },
    onError: (e) => { onErr("Falha ao enviar pedido para a cozinha.")(e); setPendingOrderId(null); },
  });

  // Simplificação proposital: o quadro de delivery avança o pedido direto pra "Pronto" (não passa
  // por EmPreparo aqui) — o timing fino por item já é coberto pela tela dedicada de KDS (/preparo).
  const markReady = useMutation({
    mutationFn: async (order: OrderResponse) => {
      const pending = order.items.filter(
        (i) => i.orderItemStatusId !== OrderItemStatus.Cancelado && !READY_ITEM_STATUSES.has(i.orderItemStatusId),
      );
      await Promise.all(pending.map((i) => updateItemStatus(order.id, i.id, OrderItemStatus.Pronto, employeeId)));
    },
    onSuccess: (_data, order) => {
      toast.success(`Pedido #${order.id} pronto para saída.`);
      setPendingOrderId(null);
      refresh();
    },
    onError: (e) => { onErr("Falha ao marcar pedido como pronto.")(e); setPendingOrderId(null); },
  });

  const cancelMutation = useMutation({
    mutationFn: (orderId: number) => cancelOrder(orderId),
    onSuccess: (_data, orderId) => {
      toast.success(`Pedido #${orderId} cancelado.`);
      setPendingOrderId(null);
      refresh();
    },
    onError: (e) => { onErr("Falha ao cancelar pedido.")(e); setPendingOrderId(null); },
  });

  const handleSendToKitchen = (order: OrderResponse) => {
    setPendingOrderId(order.id);
    sendToKitchen.mutate(order);
  };
  const handleMarkReady = (order: OrderResponse) => {
    setPendingOrderId(order.id);
    markReady.mutate(order);
  };
  const handleMarkOnRoute = (order: OrderResponse) => {
    setOnRoute((prev) => {
      const next = new Set(prev).add(order.id);
      persistOnRoute(next);
      return next;
    });
  };
  const handleCancel = async (order: OrderResponse) => {
    const ok = await dialog.confirm({
      title: "Cancelar pedido",
      message: `Cancelar o pedido #${order.id}${order.customerName ? ` de ${order.customerName}` : ""}?`,
      confirmLabel: "Cancelar pedido",
      cancelLabel: "Voltar",
      danger: true,
    });
    if (!ok) return;
    setPendingOrderId(order.id);
    cancelMutation.mutate(order.id);
  };

  const columns = viewMode === "completo" ? FULL_COLUMNS : SIMPLE_COLUMNS;
  const dense = viewMode === "simples";

  return (
    <>
      <main style={{ padding: 22, maxWidth: 1560, margin: "0 auto" }}>
        <div className="rise ui-row ui-row-wrap" style={{ alignItems: "baseline", gap: 14, marginBottom: 6 }}>
          <h2 className="display" style={{ fontSize: "1.7rem" }}>Delivery</h2>
          <span style={{ color: "var(--ink-faint)", fontSize: "0.9rem" }}>
            mova cada pedido pelas etapas até a entrega
          </span>
          <span className="ui-spacer" />
          <div style={{ display: "flex", gap: 6 }}>
            <button
              type="button"
              className={viewMode === "simples" ? "btn-primary btn-sm" : "btn-ghost btn-sm"}
              onClick={() => setViewMode("simples")}
            >
              Simples
            </button>
            <button
              type="button"
              className={viewMode === "completo" ? "btn-primary btn-sm" : "btn-ghost btn-sm"}
              onClick={() => setViewMode("completo")}
            >
              Completo
            </button>
          </div>
          <button type="button" className="btn-primary" onClick={() => setOpeningNew(true)}>
            + Novo pedido
          </button>
        </div>

        {viewMode === "completo" && (
          <div className="rise rise-1 ui-row ui-row-wrap" style={{ gap: 10, marginBottom: 18 }}>
            <div style={{ display: "flex", gap: 6 }}>
              {(["todos", "delivery", "retirada"] as ChannelFilter[]).map((c) => (
                <button
                  key={c}
                  type="button"
                  className={channelFilter === c ? "btn-primary btn-sm" : "btn-ghost btn-sm"}
                  onClick={() => setChannelFilter(c)}
                >
                  {CHANNEL_FILTER_LABELS[c]}
                </button>
              ))}
            </div>
            <input
              placeholder="Buscar pedido, cliente ou endereço…"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              style={{ maxWidth: 280 }}
            />
            <span className="ui-spacer" />
            <button
              type="button"
              className="btn-ghost btn-sm"
              disabled
              title="Otimização de rotas ainda não implementada — próxima fase de Logística."
            >
              🗺 Roteirizar (em breve)
            </button>
            <span style={{ color: "var(--ink-faint)", fontSize: "0.8rem" }}>
              atualizando a cada 15s
            </span>
          </div>
        )}

        {viewMode === "simples" && (
          <div style={{ marginBottom: 14 }}>
            <input
              placeholder="Buscar pedido, cliente ou endereço…"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              style={{ maxWidth: 320 }}
            />
          </div>
        )}

        {ordersQuery.isError && <QueryError error={ordersQuery.error} what="os pedidos" />}
        {ordersQuery.isLoading && <p style={{ color: "var(--ink-dim)" }}>Carregando pedidos…</p>}

        <div className="kanban-board rise rise-2">
          {columns.map((col) => {
            const items = col.id === "agendamento" ? [] : ordersByStage[col.id];
            return (
              <div key={col.id} className={`kanban-column${col.placeholder ? " is-placeholder" : ""}`}>
                <div className="kanban-column-head">
                  <div className="kanban-column-head-row">
                    <span className="kanban-column-title">{col.label}</span>
                    <span className="kanban-column-count">{items.length}</span>
                  </div>
                  <span className="kanban-column-hint">{col.hint}</span>
                </div>
                <div className="kanban-column-body">
                  {col.placeholder ? (
                    <div className="kanban-card-empty">
                      Agendamento de pedidos ainda não existe no SyncBar — coluna reservada
                      para quando essa funcionalidade for implementada.
                    </div>
                  ) : items.length === 0 ? (
                    <div className="kanban-card-empty">Sem pedidos</div>
                  ) : (
                    items.map((order) => (
                      <OrderCard
                        key={order.id}
                        order={order}
                        stage={col.id as Stage}
                        dense={dense}
                        busy={pendingOrderId === order.id}
                        onOpen={() => setSelectedOrderId(order.id)}
                        onSendToKitchen={() => handleSendToKitchen(order)}
                        onMarkReady={() => handleMarkReady(order)}
                        onMarkOnRoute={() => handleMarkOnRoute(order)}
                        onCancel={() => void handleCancel(order)}
                      />
                    ))
                  )}
                </div>
              </div>
            );
          })}
        </div>
      </main>

      {selectedOrderId !== null && (
        <OrderDrawer
          orderId={selectedOrderId}
          onClose={() => {
            setSelectedOrderId(null);
            refresh();
          }}
        />
      )}

      {openingNew && (
        <OpenDeliveryOrderDialog
          onClose={() => setOpeningNew(false)}
          onOpened={(orderId) => {
            setOpeningNew(false);
            refresh();
            setSelectedOrderId(orderId);
          }}
        />
      )}
    </>
  );
}
