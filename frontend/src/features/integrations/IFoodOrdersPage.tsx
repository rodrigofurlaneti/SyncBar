import { useState } from "react";
import { Link } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  assignIFoodDriver,
  cancelIFoodOrder,
  dispatchIFoodLogistics,
  getIFoodCancellationReasons,
  getIFoodLogisticsDeliveries,
  getIFoodOrders,
  markIFoodArrivedAtDestination,
  markIFoodArrivedAtOrigin,
  markIFoodGoingToOrigin,
  markIFoodOrderReady,
  startIFoodOrderPreparation,
  verifyIFoodDeliveryCode,
  type IFoodLogisticsDeliveryResponse,
  type IFoodOrderResponse,
} from "./api";
import { useAuthStore } from "../../stores/authStore";
import { useToast } from "../../ui/Toast";
import { Button } from "../../ui/Button";
import { StatusBadge } from "../../ui/StatusBadge";
import { Modal } from "../../ui/Modal";
import { SelectField, TextField } from "../../ui/Field";
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

// Logística por frota própria (fase 7) — só oferecida quando o pedido é DELIVERY e a entrega não
// é feita pela logística do próprio iFood (deliveredBy diferente de "IFOOD" e diferente de nulo).
const isOwnFleetEligible = (order: IFoodOrderResponse) =>
  order.ifoodOrderType === "DELIVERY" &&
  !!order.deliveredBy &&
  order.deliveredBy !== "IFOOD" &&
  order.status !== "CANCELLED";

const LOGISTICS_STATUS_LABEL: Record<string, string> = {
  DRIVER_ASSIGNED: "Entregador atribuído",
  GOING_TO_ORIGIN: "A caminho da loja",
  ARRIVED_AT_ORIGIN: "Na loja",
  DISPATCHED: "A caminho do cliente",
  ARRIVED_AT_DESTINATION: "No endereço do cliente",
  DELIVERY_CODE_VERIFIED: "Entrega concluída",
};

const LOGISTICS_STATUS_COLOR: Record<string, string> = {
  DRIVER_ASSIGNED: "var(--ink-faint)",
  GOING_TO_ORIGIN: "var(--info, #3b82f6)",
  ARRIVED_AT_ORIGIN: "var(--info, #3b82f6)",
  DISPATCHED: "var(--warn, #d97706)",
  ARRIVED_AT_DESTINATION: "var(--warn, #d97706)",
  DELIVERY_CODE_VERIFIED: "var(--ok)",
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

  const deliveriesQuery = useQuery({
    queryKey: ["integrations", "ifood", "logistics", branchId],
    queryFn: () => getIFoodLogisticsDeliveries(branchId),
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
  const deliveries = deliveriesQuery.data ?? [];
  const deliveriesByIFoodOrderId = new Map(deliveries.map((d) => [d.ifoodOrderId, d]));

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
          acompanha e avança o preparo (e, para entregas por frota própria, a logística)
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
            delivery={deliveriesByIFoodOrderId.get(order.id)}
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
  delivery,
  onStartPreparation,
  onMarkReady,
  onCancel,
  startPending,
  readyPending,
}: {
  order: IFoodOrderResponse;
  delivery: IFoodLogisticsDeliveryResponse | undefined;
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

      {isOwnFleetEligible(order) && <LogisticsPanel ifoodOrderId={order.id} delivery={delivery} />}

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

// Logística por frota própria (fase 7) — atribuir entregador e acompanhar os passos (saiu pra
// origem → chegou na origem → despachou → chegou no destino → verificar código de entrega).
// Sem entregador atribuído ainda, oferece só o botão de atribuição; com entregador, mostra o
// status atual e o próximo passo disponível.
function LogisticsPanel({
  ifoodOrderId,
  delivery,
}: {
  ifoodOrderId: number;
  delivery: IFoodLogisticsDeliveryResponse | undefined;
}) {
  const queryClient = useQueryClient();
  const toast = useToast();
  const [assigning, setAssigning] = useState(false);
  const [verifyingCode, setVerifyingCode] = useState(false);

  const invalidateDeliveries = () =>
    void queryClient.invalidateQueries({ queryKey: ["integrations", "ifood", "logistics"] });

  const goingToOriginMutation = useMutation({
    mutationFn: () => markIFoodGoingToOrigin(ifoodOrderId),
    onSuccess: () => {
      toast.success("Entregador a caminho da loja.");
      invalidateDeliveries();
    },
    onError: () => toast.error("Não foi possível registrar a saída para a origem."),
  });

  const arrivedAtOriginMutation = useMutation({
    mutationFn: () => markIFoodArrivedAtOrigin(ifoodOrderId),
    onSuccess: () => {
      toast.success("Chegada na loja registrada.");
      invalidateDeliveries();
    },
    onError: () => toast.error("Não foi possível registrar a chegada na loja."),
  });

  const dispatchMutation = useMutation({
    mutationFn: () => dispatchIFoodLogistics(ifoodOrderId),
    onSuccess: () => {
      toast.success("Entrega despachada.");
      invalidateDeliveries();
    },
    onError: () => toast.error("Não foi possível despachar a entrega."),
  });

  const arrivedAtDestinationMutation = useMutation({
    mutationFn: () => markIFoodArrivedAtDestination(ifoodOrderId),
    onSuccess: () => {
      toast.success("Chegada no destino registrada.");
      invalidateDeliveries();
    },
    onError: () => toast.error("Não foi possível registrar a chegada no destino."),
  });

  if (!delivery) {
    return (
      <div className="ui-row" style={{ gap: 8, justifyContent: "flex-end" }}>
        <Button variant="ghost" size="sm" onClick={() => setAssigning(true)}>
          🛵 Atribuir entregador
        </Button>
        {assigning && (
          <AssignDriverModal
            ifoodOrderId={ifoodOrderId}
            onClose={() => setAssigning(false)}
            onAssigned={() => {
              setAssigning(false);
              invalidateDeliveries();
            }}
          />
        )}
      </div>
    );
  }

  return (
    <section
      style={{
        display: "grid",
        gap: 8,
        padding: 10,
        borderRadius: 10,
        border: "1px dashed var(--border, rgba(0,0,0,0.12))",
      }}
    >
      <div className="ui-row ui-row-wrap" style={{ justifyContent: "space-between", gap: 8 }}>
        <div style={{ display: "grid", gap: 2 }}>
          <span style={{ fontWeight: 600, fontSize: "0.9rem" }}>
            🛵 {delivery.driverName} · {delivery.driverPhone}
          </span>
          <span style={{ color: "var(--ink-faint)", fontSize: "0.8rem" }}>{delivery.driverVehicleType}</span>
        </div>
        <StatusBadge color={LOGISTICS_STATUS_COLOR[delivery.status] ?? "var(--ink-faint)"}>
          {LOGISTICS_STATUS_LABEL[delivery.status] ?? delivery.status}
        </StatusBadge>
      </div>

      <div className="ui-row" style={{ gap: 8, justifyContent: "flex-end" }}>
        {delivery.status === "DRIVER_ASSIGNED" && (
          <Button
            variant="ghost"
            size="sm"
            loading={goingToOriginMutation.isPending}
            onClick={() => goingToOriginMutation.mutate()}
          >
            Saiu para retirada
          </Button>
        )}
        {delivery.status === "GOING_TO_ORIGIN" && (
          <Button
            variant="ghost"
            size="sm"
            loading={arrivedAtOriginMutation.isPending}
            onClick={() => arrivedAtOriginMutation.mutate()}
          >
            Chegou na loja
          </Button>
        )}
        {delivery.status === "ARRIVED_AT_ORIGIN" && (
          <Button variant="primary" size="sm" loading={dispatchMutation.isPending} onClick={() => dispatchMutation.mutate()}>
            Despachar
          </Button>
        )}
        {delivery.status === "DISPATCHED" && (
          <Button
            variant="ghost"
            size="sm"
            loading={arrivedAtDestinationMutation.isPending}
            onClick={() => arrivedAtDestinationMutation.mutate()}
          >
            Chegou no destino
          </Button>
        )}
        {delivery.status === "ARRIVED_AT_DESTINATION" && (
          <Button variant="primary" size="sm" onClick={() => setVerifyingCode(true)}>
            Verificar código de entrega
          </Button>
        )}
        {delivery.status === "DELIVERY_CODE_VERIFIED" && (
          <span style={{ color: "var(--ok)", fontSize: "0.85rem", fontWeight: 600 }}>✓ Entrega concluída</span>
        )}
      </div>

      {verifyingCode && (
        <VerifyDeliveryCodeModal
          ifoodOrderId={ifoodOrderId}
          onClose={() => setVerifyingCode(false)}
          onVerified={() => {
            setVerifyingCode(false);
            invalidateDeliveries();
          }}
        />
      )}
    </section>
  );
}

function AssignDriverModal({
  ifoodOrderId,
  onClose,
  onAssigned,
}: {
  ifoodOrderId: number;
  onClose: () => void;
  onAssigned: () => void;
}) {
  const toast = useToast();
  const [driverName, setDriverName] = useState("");
  const [driverPhone, setDriverPhone] = useState("");
  const [driverVehicleType, setDriverVehicleType] = useState("");

  const assignMutation = useMutation({
    mutationFn: () => assignIFoodDriver(ifoodOrderId, { driverName, driverPhone, driverVehicleType }),
    onSuccess: () => {
      toast.success("Entregador atribuído no iFood.");
      onAssigned();
    },
    onError: () => toast.error("Não foi possível atribuir o entregador — confira os dados e tente de novo."),
  });

  const canSubmit = driverName.trim().length > 0 && driverPhone.trim().length > 0 && driverVehicleType.trim().length > 0;

  return (
    <Modal onClose={onClose} title="Atribuir entregador">
      <div style={{ display: "grid", gap: 14, minWidth: 320 }}>
        <TextField
          label="Nome do entregador"
          value={driverName}
          onChange={(e) => setDriverName(e.target.value)}
          autoFocus
        />
        <TextField label="Telefone" value={driverPhone} onChange={(e) => setDriverPhone(e.target.value)} />
        <TextField
          label="Veículo"
          placeholder="ex.: moto, bike, carro"
          value={driverVehicleType}
          onChange={(e) => setDriverVehicleType(e.target.value)}
        />
        <div style={{ display: "flex", gap: 10, justifyContent: "flex-end" }}>
          <Button variant="ghost" onClick={onClose}>
            Voltar
          </Button>
          <Button
            variant="primary"
            disabled={!canSubmit}
            loading={assignMutation.isPending}
            onClick={() => assignMutation.mutate()}
          >
            Atribuir
          </Button>
        </div>
      </div>
    </Modal>
  );
}

function VerifyDeliveryCodeModal({
  ifoodOrderId,
  onClose,
  onVerified,
}: {
  ifoodOrderId: number;
  onClose: () => void;
  onVerified: () => void;
}) {
  const toast = useToast();
  const [code, setCode] = useState("");

  const verifyMutation = useMutation({
    mutationFn: () => verifyIFoodDeliveryCode(ifoodOrderId, code),
    onSuccess: (result) => {
      if (result.codeMatched) {
        toast.success("Código confirmado — entrega concluída.");
        onVerified();
      } else {
        toast.error("Código incorreto — confira com o cliente e tente de novo.");
      }
    },
    onError: () => toast.error("Não foi possível verificar o código no iFood."),
  });

  return (
    <Modal onClose={onClose} title="Verificar código de entrega">
      <div style={{ display: "grid", gap: 14, minWidth: 280 }}>
        <TextField
          label="Código informado pelo cliente"
          value={code}
          onChange={(e) => setCode(e.target.value)}
          autoFocus
        />
        <div style={{ display: "flex", gap: 10, justifyContent: "flex-end" }}>
          <Button variant="ghost" onClick={onClose}>
            Voltar
          </Button>
          <Button
            variant="primary"
            disabled={!code.trim()}
            loading={verifyMutation.isPending}
            onClick={() => verifyMutation.mutate()}
          >
            Verificar
          </Button>
        </div>
      </div>
    </Modal>
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
