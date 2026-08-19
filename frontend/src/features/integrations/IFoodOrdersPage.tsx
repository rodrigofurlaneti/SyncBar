import { useState } from "react";
import { Link } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  cancelIFoodOrder,
  getIFoodCancellationReasons,
  getIFoodOrders,
  markIFoodOrderReady,
  startIFoodOrderPreparation,
  type IFoodOrderResponse,
} from "./api";
import { useAuthStore } from "../../stores/authStore";
import { useToast } from "../../ui/Toast";
import { Button } from "../../ui/Button";
import { StatusBadge } from "../../ui/StatusBadge";
import { Modal } from "../../ui/Modal";
import { SelectField } from "../../ui/Field";
import { QueryError } from "../../components/QueryError";
import { EmptyState } from "../../ui/EmptyState";

// Sincronização roda sozinha no backend (polling a cada 30s) — esta tela só refaz fetch com
// frequência pra acompanhar sem precisar recarregar a página manualmente.
const REFRESH_INTERVAL_MS = 15_000;

const STATUS_LABEL: Record<string, string> = {
  PLACED: "Recebido",
  CONFIRMED: "Confirmado",
  PREPARATION_STARTED: "Em preparo",
  READY_TO_PICKUP: "Pronto",
  DISPATCHED: "Despachado",
  CONCLUDED: "Concluído",
  CANCELLED: "Cancelado",
};

const STATUS_COLOR: Record<string, string> = {
  PLACED: "var(--ink-faint)",
  CONFIRMED: "var(--info, #3b82f6)",
  PREPARATION_STARTED: "var(--warn, #d97706)",
  READY_TO_PICKUP: "var(--ok)",
  DISPATCHED: "var(--ok)",
  CONCLUDED: "var(--ink-faint)",
  CANCELLED: "var(--danger)",
};

const TYPE_LABEL: Record<string, string> = {
  DELIVERY: "Delivery",
  TAKEOUT: "Retirada",
  DINE_IN: "Consumo no local",
};

export function IFoodOrdersPage() {
  const queryClient = useQueryClient();
  const toast = useToast();
  const { branchId } = useAuthStore();
  const [cancellingOrder, setCancellingOrder] = useState<IFoodOrderResponse | null>(null);

  const ordersQuery = useQuery({
    queryKey: ["integrations", "ifood", "orders", branchId],
    queryFn: () => getIFoodOrders(branchId),
    refetchInterval: REFRESH_INTERVAL_MS,
  });

  const invalidateOrders = () =>
    void queryClient.invalidateQueries({ queryKey: ["integrations", "ifood", "orders"] });

  const startPreparationMutation = useMutation({
    mutationFn: (id: number) => startIFoodOrderPreparation(id),
    onSuccess: () => {
      toast.success("Preparo iniciado no iFood.");
      invalidateOrders();
    },
    onError: () => toast.error("Não foi possível iniciar o preparo no iFood."),
  });

  const markReadyMutation = useMutation({
    mutationFn: (id: number) => markIFoodOrderReady(id),
    onSuccess: () => {
      toast.success("Pedido marcado como pronto no iFood.");
      invalidateOrders();
    },
    onError: () => toast.error("Não foi possível marcar como pronto no iFood."),
  });

  const orders = ordersQuery.data ?? [];

  return (
    <main style={{ padding: 22, maxWidth: 1000, margin: "0 auto" }}>
      <div className="rise" style={{ marginBottom: 18 }}>
        <Link to="/integracoes/ifood" style={{ color: "var(--ink-faint)", fontSize: "0.85rem" }}>
          ← Integração iFood
        </Link>
        <h2 className="display" style={{ fontSize: "1.7rem" }}>
          Pedidos iFood
        </h2>
        <span style={{ color: "var(--ink-faint)", fontSize: "0.9rem" }}>
          novos pedidos chegam sozinhos e já são confirmados automaticamente — aqui você
          acompanha e avança o preparo
        </span>
      </div>

      {ordersQuery.isError && <QueryError error={ordersQuery.error} what="os pedidos do iFood" />}

      {!ordersQuery.isLoading && orders.length === 0 && !ordersQuery.isError && (
        <EmptyState
          title="Nenhum pedido iFood em aberto"
          description="Assim que um pedido novo chegar do iFood, ele aparece aqui automaticamente."
        />
      )}

      <div style={{ display: "grid", gap: 12 }}>
        {orders.map((order) => (
          <IFoodOrderCard
            key={order.id}
            order={order}
            onStartPreparation={() => startPreparationMutation.mutate(order.id)}
            onMarkReady={() => markReadyMutation.mutate(order.id)}
            onCancel={() => setCancellingOrder(order)}
            startPending={startPreparationMutation.isPending && startPreparationMutation.variables === order.id}
            readyPending={markReadyMutation.isPending && markReadyMutation.variables === order.id}
          />
        ))}
      </div>

      {cancellingOrder && (
        <CancelOrderModal
          order={cancellingOrder}
          onClose={() => setCancellingOrder(null)}
          onCancelled={() => {
            setCancellingOrder(null);
            invalidateOrders();
          }}
        />
      )}
    </main>
  );
}

function IFoodOrderCard({
  order,
  onStartPreparation,
  onMarkReady,
  onCancel,
  startPending,
  readyPending,
}: {
  order: IFoodOrderResponse;
  onStartPreparation: () => void;
  onMarkReady: () => void;
  onCancel: () => void;
  startPending: boolean;
  readyPending: boolean;
}) {
  const canStartPreparation = order.status === "CONFIRMED";
  const canMarkReady = order.status === "CONFIRMED" || order.status === "PREPARATION_STARTED";
  const canCancel = order.status !== "CANCELLED" && order.status !== "CONCLUDED";

  return (
    <section className="ticket rise" style={{ padding: 18, display: "grid", gap: 10 }}>
      <div className="ui-row ui-row-wrap" style={{ justifyContent: "space-between", gap: 12 }}>
        <div style={{ display: "grid", gap: 2 }}>
          <span style={{ fontWeight: 700, fontSize: "1.05rem" }}>
            #{order.displayId ?? order.ifoodOrderId} — {order.customerName}
          </span>
          <span style={{ color: "var(--ink-faint)", fontSize: "0.85rem" }}>
            {TYPE_LABEL[order.ifoodOrderType] ?? order.ifoodOrderType}
            {order.customerPhone ? ` · ${order.customerPhone}` : ""}
          </span>
          {order.deliveryAddress && (
            <span style={{ color: "var(--ink-dim)", fontSize: "0.85rem" }}>{order.deliveryAddress}</span>
          )}
        </div>
        <div style={{ display: "grid", gap: 6, justifyItems: "end" }}>
          <StatusBadge color={STATUS_COLOR[order.status] ?? "var(--ink-faint)"}>
            {STATUS_LABEL[order.status] ?? order.status}
          </StatusBadge>
          <span style={{ fontWeight: 600 }}>
            {order.totalAmount.toLocaleString("pt-BR", { style: "currency", currency: "BRL" })}
          </span>
        </div>
      </div>

      {order.hasUnmappedItems && (
        <span style={{ color: "var(--warn, #d97706)", fontSize: "0.85rem" }}>
          ⚠ Este pedido tem itens que não foram encontrados no cardápio (código de barras não
          cadastrado) — confira o pedido na tela normal de Pedidos antes de preparar.
        </span>
      )}

      <div className="ui-row" style={{ gap: 8, justifyContent: "flex-end" }}>
        {canStartPreparation && (
          <Button variant="ghost" size="sm" loading={startPending} onClick={onStartPreparation}>
            Iniciar preparo
          </Button>
        )}
        {canMarkReady && (
          <Button variant="primary" size="sm" loading={readyPending} onClick={onMarkReady}>
            Marcar pronto
          </Button>
        )}
        {canCancel && (
          <Button variant="danger" size="sm" onClick={onCancel}>
            Cancelar
          </Button>
        )}
      </div>
    </section>
  );
}

function CancelOrderModal({
  order,
  onClose,
  onCancelled,
}: {
  order: IFoodOrderResponse;
  onClose: () => void;
  onCancelled: () => void;
}) {
  const toast = useToast();
  const [reasonCode, setReasonCode] = useState("");

  const reasonsQuery = useQuery({
    queryKey: ["integrations", "ifood", "cancellation-reasons", order.id],
    queryFn: () => getIFoodCancellationReasons(order.id),
  });

  const cancelMutation = useMutation({
    mutationFn: () => cancelIFoodOrder(order.id, reasonCode),
    onSuccess: () => {
      toast.success("Cancelamento solicitado ao iFood — o pedido some da lista quando o iFood confirmar.");
      onCancelled();
    },
    onError: () => toast.error("Não foi possível solicitar o cancelamento."),
  });

  const reasons = reasonsQuery.data ?? [];

  return (
    <Modal onClose={onClose} title={`Cancelar pedido #${order.displayId ?? order.ifoodOrderId}`}>
      <div style={{ display: "grid", gap: 14, minWidth: 320 }}>
        {reasonsQuery.isError && <QueryError error={reasonsQuery.error} what="os motivos de cancelamento" />}

        <SelectField
          label="Motivo do cancelamento"
          value={reasonCode}
          onChange={(e) => setReasonCode(e.target.value)}
          disabled={reasonsQuery.isLoading}
        >
          <option value="">Selecione um motivo…</option>
          {reasons.map((reason) => (
            <option key={reason.code} value={reason.code}>
              {reason.description}
            </option>
          ))}
        </SelectField>

        <div style={{ display: "flex", gap: 10, justifyContent: "flex-end" }}>
          <Button variant="ghost" onClick={onClose}>
            Voltar
          </Button>
          <Button
            variant="danger"
            disabled={!reasonCode}
            loading={cancelMutation.isPending}
            onClick={() => cancelMutation.mutate()}
          >
            Confirmar cancelamento
          </Button>
        </div>
      </div>
    </Modal>
  );
}
