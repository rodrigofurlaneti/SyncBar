import { useEffect, useRef, useState } from "react";
import { Link } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  acceptIFoodDeliveryAddressChange,
  acceptIFoodDispute,
  assignIFoodDriver,
  cancelIFoodOrder,
  cancelIFoodOrderDriverRequest,
  confirmIFoodUserAddress,
  denyIFoodDeliveryAddressChange,
  dispatchIFoodLogistics,
  getIFoodCancellationReasons,
  getIFoodLogisticsDeliveries,
  getIFoodOrderTracking,
  getIFoodOrders,
  getIFoodOrderVirtualBag,
  markIFoodArrivedAtDestination,
  markIFoodArrivedAtOrigin,
  markIFoodGoingToOrigin,
  markIFoodOrderReady,
  rejectIFoodDispute,
  requestIFoodDeliveryAddressChange,
  requestIFoodDisputeAlternative,
  requestIFoodOrderDriver,
  startIFoodOrderPreparation,
  validateIFoodPickupCode,
  verifyIFoodDeliveryCode,
  verifyIFoodOrderDeliveryCode,
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

// Fase 9b — rastreamento só faz sentido pra entregas feitas pela logística do PRÓPRIO iFood
// (deliveredBy === "IFOOD"); frota própria já tem seu próprio painel de status (LogisticsPanel
// acima) e não usa o endpoint de tracking do módulo Order.
const isIFoodTrackingEligible = (order: IFoodOrderResponse) =>
  order.ifoodOrderType === "DELIVERY" &&
  order.deliveredBy === "IFOOD" &&
  (order.status === "DISPATCHED" || order.status === "READY_TO_PICKUP");

// Código de retirada — pedidos de retirada no balcão (TAKEOUT), quando já estão prontos.
const isPickupCodeEligible = (order: IFoodOrderResponse) =>
  order.ifoodOrderType === "TAKEOUT" && order.status === "READY_TO_PICKUP";

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

// Fase 12 — aviso sonoro/visual de pedido novo. A sincronização em si já roda no backend
// (IFoodOrderPollingBackgroundService, a cada 30s) e esta tela já reconsulta a lista a cada 15s
// (REFRESH_INTERVAL_MS); faltava só avisar o operador quando um pedido novo aparece na lista, em
// vez de depender de alguém ficar olhando a tela. Toca dois beeps curtos via Web Audio API — sem
// depender de nenhum arquivo de áudio externo. Falha silenciosamente se o navegador bloquear
// áudio (ex.: antes de qualquer interação do usuário na página) — o toast visual ainda avisa.
function playNewOrderChime() {
  try {
    const AudioCtxCtor = window.AudioContext ?? (window as unknown as { webkitAudioContext?: typeof AudioContext }).webkitAudioContext;
    if (!AudioCtxCtor) return;
    const ctx = new AudioCtxCtor();
    const playBeep = (startTime: number, frequency: number) => {
      const oscillator = ctx.createOscillator();
      const gain = ctx.createGain();
      oscillator.type = "sine";
      oscillator.frequency.value = frequency;
      gain.gain.setValueAtTime(0.0001, startTime);
      gain.gain.exponentialRampToValueAtTime(0.3, startTime + 0.02);
      gain.gain.exponentialRampToValueAtTime(0.0001, startTime + 0.25);
      oscillator.connect(gain);
      gain.connect(ctx.destination);
      oscillator.start(startTime);
      oscillator.stop(startTime + 0.3);
    };
    const now = ctx.currentTime;
    playBeep(now, 880);
    playBeep(now + 0.3, 1046.5);
    setTimeout(() => void ctx.close(), 800);
  } catch {
    // Ambiente sem suporte a Web Audio, ou áudio bloqueado — segue só com o toast visual.
  }
}

export function IFoodOrdersPage() {
  const queryClient = useQueryClient();
  const toast = useToast();
  const { branchId } = useAuthStore();
  const [cancellingOrder, setCancellingOrder] = useState<IFoodOrderResponse | null>(null);
  const [trackingOrder, setTrackingOrder] = useState<IFoodOrderResponse | null>(null);
  const [pickupCodeOrder, setPickupCodeOrder] = useState<IFoodOrderResponse | null>(null);
  const [advancedOrder, setAdvancedOrder] = useState<IFoodOrderResponse | null>(null);

  const ordersQuery = useQuery({
    queryKey: ["integrations", "ifood", "orders", branchId],
    queryFn: () => getIFoodOrders(branchId),
    refetchInterval: REFRESH_INTERVAL_MS,
  });

  // Snapshot dos IDs de pedido já vistos — null enquanto a tela ainda não terminou o primeiro
  // carregamento (nesse caso não alerta nada, senão todo pedido em aberto viraria um "pedido
  // novo" assim que a tela abre).
  const knownOrderIdsRef = useRef<Set<number> | null>(null);

  // Troca de filial: descarta o snapshot antigo pra não comparar pedidos de lojas diferentes.
  useEffect(() => {
    knownOrderIdsRef.current = null;
  }, [branchId]);

  useEffect(() => {
    const orders = ordersQuery.data;
    if (!orders) return;

    const currentIds = new Set(orders.map((order) => order.id));

    if (knownOrderIdsRef.current === null) {
      knownOrderIdsRef.current = currentIds;
      return;
    }

    const previouslyKnown = knownOrderIdsRef.current;
    const newOrders = orders.filter((order) => !previouslyKnown.has(order.id));
    knownOrderIdsRef.current = currentIds;

    if (newOrders.length === 0) return;

    playNewOrderChime();
    for (const order of newOrders) {
      const typeLabel = TYPE_LABEL[order.ifoodOrderType] ?? order.ifoodOrderType;
      toast.info(
        `🔔 Novo pedido iFood — ${order.customerName} · ${typeLabel} · R$ ${order.totalAmount.toFixed(2)}`,
      );
    }
  }, [ordersQuery.data, toast]);

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
            onTrack={() => setTrackingOrder(order)}
            onValidatePickupCode={() => setPickupCodeOrder(order)}
            onAdvanced={() => setAdvancedOrder(order)}
            startPending={startPreparationMutation.isPending && startPreparationMutation.variables === order.id}
            readyPending={markReadyMutation.isPending && markReadyMutation.variables === order.id}
          />
        ))}
      </div>

      <DisputesSection branchId={branchId} />

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

      {trackingOrder && <TrackOrderModal order={trackingOrder} onClose={() => setTrackingOrder(null)} />}

      {pickupCodeOrder && (
        <ValidatePickupCodeModal
          order={pickupCodeOrder}
          onClose={() => setPickupCodeOrder(null)}
          onValidated={() => setPickupCodeOrder(null)}
        />
      )}

      {advancedOrder && <OrderAdvancedActionsModal order={advancedOrder} onClose={() => setAdvancedOrder(null)} />}
    </main>
  );
}

function IFoodOrderCard({
  order,
  delivery,
  onStartPreparation,
  onMarkReady,
  onCancel,
  onTrack,
  onValidatePickupCode,
  onAdvanced,
  startPending,
  readyPending,
}: {
  order: IFoodOrderResponse;
  delivery: IFoodLogisticsDeliveryResponse | undefined;
  onStartPreparation: () => void;
  onMarkReady: () => void;
  onCancel: () => void;
  onTrack: () => void;
  onValidatePickupCode: () => void;
  onAdvanced: () => void;
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
          {order.orderTiming === "SCHEDULED" && order.preparationStartDateTime && (
            <span style={{ color: "var(--info, #3b82f6)", fontSize: "0.85rem", fontWeight: 600 }}>
              📅 Agendado para{" "}
              {new Date(order.preparationStartDateTime).toLocaleString("pt-BR", {
                day: "2-digit",
                month: "2-digit",
                hour: "2-digit",
                minute: "2-digit",
              })}
            </span>
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
        {isIFoodTrackingEligible(order) && (
          <Button variant="ghost" size="sm" onClick={onTrack}>
            Rastrear entregador
          </Button>
        )}
        {isPickupCodeEligible(order) && (
          <Button variant="ghost" size="sm" onClick={onValidatePickupCode}>
            Validar código de retirada
          </Button>
        )}
        <Button variant="ghost" size="sm" onClick={onAdvanced}>
          Mais ações
        </Button>
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

// Fase 9b — rastreamento (posição do entregador) pra pedidos entregues pela logística do próprio
// iFood. Mesmo padrão de tela usado em TrackingModal (IFoodShippingPage.tsx), mas lendo de
// GET order/v1.0/orders/{id}/tracking.
function TrackOrderModal({ order, onClose }: { order: IFoodOrderResponse; onClose: () => void }) {
  const trackingQuery = useQuery({
    queryKey: ["integrations", "ifood", "order-tracking", order.id],
    queryFn: () => getIFoodOrderTracking(order.id),
    refetchInterval: 15_000,
  });

  const tracking = trackingQuery.data;

  return (
    <Modal onClose={onClose} title={`Rastrear #${order.displayId ?? order.ifoodOrderId}`}>
      <div style={{ display: "grid", gap: 12, minWidth: 300 }}>
        {trackingQuery.isError && <QueryError error={trackingQuery.error} what="o rastreamento" />}
        {trackingQuery.isLoading && <span style={{ color: "var(--ink-faint)" }}>Carregando…</span>}
        {tracking && (
          <div className="ticket" style={{ padding: 14, display: "grid", gap: 6 }}>
            {tracking.latitude != null && tracking.longitude != null ? (
              <span>
                Posição do entregador: {tracking.latitude.toFixed(5)}, {tracking.longitude.toFixed(5)}
              </span>
            ) : (
              <span style={{ color: "var(--ink-faint)" }}>Posição ainda não disponível.</span>
            )}
            {tracking.expectedDelivery && (
              <span>Previsão de entrega: {new Date(tracking.expectedDelivery).toLocaleTimeString("pt-BR")}</span>
            )}
          </div>
        )}
        <div style={{ display: "flex", justifyContent: "flex-end" }}>
          <Button variant="ghost" onClick={onClose}>
            Fechar
          </Button>
        </div>
      </div>
    </Modal>
  );
}

// Fase 9b — confirma o código de retirada informado pelo cliente no balcão (pedidos TAKEOUT).
function ValidatePickupCodeModal({
  order,
  onClose,
  onValidated,
}: {
  order: IFoodOrderResponse;
  onClose: () => void;
  onValidated: () => void;
}) {
  const toast = useToast();
  const [code, setCode] = useState("");

  const mutation = useMutation({
    mutationFn: () => validateIFoodPickupCode(order.id, code.trim()),
    onSuccess: (result) => {
      if (result.codeMatched) {
        toast.success("Código confirmado — pode entregar o pedido.");
        onValidated();
      } else {
        toast.error("Código incorreto — confira com o cliente e tente de novo.");
      }
    },
    onError: () => toast.error("Não foi possível validar o código no iFood."),
  });

  return (
    <Modal onClose={onClose} title={`Validar retirada — #${order.displayId ?? order.ifoodOrderId}`}>
      <div style={{ display: "grid", gap: 14, minWidth: 280 }}>
        <TextField label="Código informado pelo cliente" value={code} onChange={(e) => setCode(e.target.value)} autoFocus />
        <div style={{ display: "flex", gap: 10, justifyContent: "flex-end" }}>
          <Button variant="ghost" onClick={onClose}>
            Voltar
          </Button>
          <Button variant="primary" disabled={!code.trim()} loading={mutation.isPending} onClick={() => mutation.mutate()}>
            Validar
          </Button>
        </div>
      </div>
    </Modal>
  );
}

// Fase 9c — reúne num só lugar os gaps do módulo Order fechados na auditoria de 2026-08-20/21:
// virtual bag (Grocery), requestDriver/cancelRequestDriver/verifyDeliveryCode do PRÓPRIO módulo
// Order (distintos dos homônimos em Shipping/Logistics já cobertos no resto da tela). São
// endpoints pouco usados no fluxo do dia a dia do SyncBar (só vende FOOD/FOOD_SELF_SERVICE, não
// Grocery), por isso ficam agrupados aqui em vez de espalhados pelo card do pedido.
function OrderAdvancedActionsModal({ order, onClose }: { order: IFoodOrderResponse; onClose: () => void }) {
  const toast = useToast();
  const [showBag, setShowBag] = useState(false);
  const [verifyingCode, setVerifyingCode] = useState(false);
  const [code, setCode] = useState("");

  // Fase 11 — troca de endereço de entrega em andamento (módulo Shipping).
  const [showAddressChangeForm, setShowAddressChangeForm] = useState(false);
  const [addrStreetNumber, setAddrStreetNumber] = useState("");
  const [addrStreetName, setAddrStreetName] = useState("");
  const [addrComplement, setAddrComplement] = useState("");
  const [addrNeighborhood, setAddrNeighborhood] = useState("");
  const [addrCity, setAddrCity] = useState("");
  const [addrState, setAddrState] = useState("");
  const [addrReference, setAddrReference] = useState("");

  const bagQuery = useQuery({
    queryKey: ["integrations", "ifood", "virtual-bag", order.id],
    queryFn: () => getIFoodOrderVirtualBag(order.id),
    enabled: showBag,
  });

  const requestDriverMutation = useMutation({
    mutationFn: () => requestIFoodOrderDriver(order.id),
    onSuccess: () => toast.success("Entregador solicitado (módulo Order) no iFood."),
    onError: () => toast.error("Não foi possível solicitar o entregador no iFood."),
  });

  const cancelDriverMutation = useMutation({
    mutationFn: () => cancelIFoodOrderDriverRequest(order.id),
    onSuccess: () => toast.success("Solicitação de entregador cancelada (módulo Order) no iFood."),
    onError: () => toast.error("Não foi possível cancelar a solicitação no iFood."),
  });

  const verifyCodeMutation = useMutation({
    mutationFn: () => verifyIFoodOrderDeliveryCode(order.id, code.trim()),
    onSuccess: (result) => {
      if (result.codeMatched) {
        toast.success("Código confirmado no iFood.");
        setVerifyingCode(false);
        setCode("");
      } else {
        toast.error("Código incorreto — confira com o cliente e tente de novo.");
      }
    },
    onError: () => toast.error("Não foi possível verificar o código no iFood."),
  });

  const requestAddressChangeMutation = useMutation({
    mutationFn: () =>
      requestIFoodDeliveryAddressChange(order.id, {
        streetNumber: addrStreetNumber.trim(),
        streetName: addrStreetName.trim(),
        complement: addrComplement.trim() || undefined,
        neighborhood: addrNeighborhood.trim(),
        city: addrCity.trim(),
        state: addrState.trim(),
        reference: addrReference.trim() || undefined,
      }),
    onSuccess: () => {
      toast.success("Troca de endereço solicitada ao iFood.");
      setShowAddressChangeForm(false);
      setAddrStreetNumber("");
      setAddrStreetName("");
      setAddrComplement("");
      setAddrNeighborhood("");
      setAddrCity("");
      setAddrState("");
      setAddrReference("");
    },
    onError: () => toast.error("Não foi possível solicitar a troca de endereço no iFood."),
  });

  const acceptAddressChangeMutation = useMutation({
    mutationFn: () => acceptIFoodDeliveryAddressChange(order.id),
    onSuccess: () => toast.success("Troca de endereço aceita no iFood."),
    onError: () => toast.error("Não foi possível aceitar a troca de endereço no iFood."),
  });

  const denyAddressChangeMutation = useMutation({
    mutationFn: () => denyIFoodDeliveryAddressChange(order.id),
    onSuccess: () => toast.success("Troca de endereço recusada no iFood."),
    onError: () => toast.error("Não foi possível recusar a troca de endereço no iFood."),
  });

  const confirmUserAddressMutation = useMutation({
    mutationFn: () => confirmIFoodUserAddress(order.id),
    onSuccess: () => toast.success("Endereço do usuário confirmado no iFood."),
    onError: () => toast.error("Não foi possível confirmar o endereço do usuário no iFood."),
  });

  const bag = bagQuery.data;

  return (
    <Modal onClose={onClose} title={`Mais ações — #${order.displayId ?? order.ifoodOrderId}`}>
      <div style={{ display: "grid", gap: 16, minWidth: 340 }}>
        <div style={{ display: "grid", gap: 8 }}>
          <span style={{ fontWeight: 600, fontSize: "0.9rem" }}>Sacola virtual (Grocery)</span>
          <Button variant="ghost" size="sm" onClick={() => setShowBag(true)} loading={showBag && bagQuery.isLoading}>
            Consultar sacola
          </Button>
          {showBag && bagQuery.isError && <QueryError error={bagQuery.error} what="a sacola do pedido" />}
          {showBag && bag && (
            <div className="ticket" style={{ padding: 12, display: "grid", gap: 4, fontSize: "0.85rem" }}>
              <span>Status: {bag.status ?? "—"}</span>
              <span>Itens: {bag.items.length}</span>
              {bag.grossValueAmount && (
                <span>
                  Valor bruto: {bag.grossValueAmount} {bag.grossValueCurrency ?? ""}
                </span>
              )}
            </div>
          )}
        </div>

        <div style={{ display: "grid", gap: 8 }}>
          <span style={{ fontWeight: 600, fontSize: "0.9rem" }}>Entregador (endpoint próprio do módulo Order)</span>
          <div className="ui-row" style={{ gap: 8 }}>
            <Button variant="ghost" size="sm" loading={requestDriverMutation.isPending} onClick={() => requestDriverMutation.mutate()}>
              Solicitar
            </Button>
            <Button variant="ghost" size="sm" loading={cancelDriverMutation.isPending} onClick={() => cancelDriverMutation.mutate()}>
              Cancelar solicitação
            </Button>
          </div>
        </div>

        <div style={{ display: "grid", gap: 8 }}>
          <span style={{ fontWeight: 600, fontSize: "0.9rem" }}>Verificar código de entrega (módulo Order)</span>
          {!verifyingCode ? (
            <Button variant="ghost" size="sm" onClick={() => setVerifyingCode(true)}>
              Informar código
            </Button>
          ) : (
            <div className="ui-row" style={{ gap: 8 }}>
              <div style={{ flex: 1 }}>
                <TextField label="Código" value={code} onChange={(e) => setCode(e.target.value)} autoFocus />
              </div>
              <Button
                variant="primary"
                size="sm"
                disabled={!code.trim()}
                loading={verifyCodeMutation.isPending}
                onClick={() => verifyCodeMutation.mutate()}
              >
                Verificar
              </Button>
            </div>
          )}
        </div>

        <div style={{ display: "grid", gap: 8 }}>
          <span style={{ fontWeight: 600, fontSize: "0.9rem" }}>Troca de endereço de entrega (módulo Shipping)</span>
          <span style={{ color: "var(--ink-faint)", fontSize: "0.8rem" }}>
            fluxo bidirecional — solicite um novo endereço pra propor a troca, ou aceite/recuse/confirme
            quando é o cliente quem propõe pelo app dele
          </span>

          {!showAddressChangeForm ? (
            <Button variant="ghost" size="sm" onClick={() => setShowAddressChangeForm(true)}>
              Solicitar novo endereço
            </Button>
          ) : (
            <div style={{ display: "grid", gap: 8 }}>
              <div className="ui-row ui-row-wrap" style={{ gap: 8 }}>
                <div style={{ flex: 1, minWidth: 100 }}>
                  <TextField label="Número" value={addrStreetNumber} onChange={(e) => setAddrStreetNumber(e.target.value)} />
                </div>
                <div style={{ flex: 2, minWidth: 180 }}>
                  <TextField label="Rua" value={addrStreetName} onChange={(e) => setAddrStreetName(e.target.value)} />
                </div>
              </div>
              <div className="ui-row ui-row-wrap" style={{ gap: 8 }}>
                <div style={{ flex: 1, minWidth: 160 }}>
                  <TextField label="Complemento" value={addrComplement} onChange={(e) => setAddrComplement(e.target.value)} />
                </div>
                <div style={{ flex: 1, minWidth: 160 }}>
                  <TextField label="Bairro" value={addrNeighborhood} onChange={(e) => setAddrNeighborhood(e.target.value)} />
                </div>
              </div>
              <div className="ui-row ui-row-wrap" style={{ gap: 8 }}>
                <div style={{ flex: 2, minWidth: 160 }}>
                  <TextField label="Cidade" value={addrCity} onChange={(e) => setAddrCity(e.target.value)} />
                </div>
                <div style={{ flex: 1, minWidth: 100 }}>
                  <TextField label="Estado (UF)" value={addrState} onChange={(e) => setAddrState(e.target.value)} />
                </div>
              </div>
              <TextField label="Referência (opcional)" value={addrReference} onChange={(e) => setAddrReference(e.target.value)} />
              <div className="ui-row" style={{ gap: 8, justifyContent: "flex-end" }}>
                <Button variant="ghost" size="sm" onClick={() => setShowAddressChangeForm(false)}>
                  Cancelar
                </Button>
                <Button
                  variant="primary"
                  size="sm"
                  disabled={!addrStreetNumber.trim() || !addrStreetName.trim() || !addrNeighborhood.trim() || !addrCity.trim() || !addrState.trim()}
                  loading={requestAddressChangeMutation.isPending}
                  onClick={() => requestAddressChangeMutation.mutate()}
                >
                  Enviar solicitação
                </Button>
              </div>
            </div>
          )}

          <div className="ui-row ui-row-wrap" style={{ gap: 8 }}>
            <Button variant="ghost" size="sm" loading={acceptAddressChangeMutation.isPending} onClick={() => acceptAddressChangeMutation.mutate()}>
              Aceitar troca
            </Button>
            <Button variant="ghost" size="sm" loading={denyAddressChangeMutation.isPending} onClick={() => denyAddressChangeMutation.mutate()}>
              Recusar troca
            </Button>
            <Button variant="ghost" size="sm" loading={confirmUserAddressMutation.isPending} onClick={() => confirmUserAddressMutation.mutate()}>
              Confirmar endereço do usuário
            </Button>
          </div>
        </div>

        <div style={{ display: "flex", justifyContent: "flex-end" }}>
          <Button variant="ghost" onClick={onClose}>
            Fechar
          </Button>
        </div>
      </div>
    </Modal>
  );
}

// Fase 9b — disputas Handshake (aceitar/rejeitar). O SyncBar ainda não ingere os eventos de
// disputa automaticamente (ver ressalva no backend) — a equipe informa o DisputeId recebido no
// app/painel do iFood quando o cliente abre uma disputa pós-entrega.
function DisputesSection({ branchId }: { branchId: number }) {
  const toast = useToast();
  const [disputeId, setDisputeId] = useState("");
  const [reason, setReason] = useState("");
  const [lastResult, setLastResult] = useState<string | null>(null);

  // Fase 9c — proposta de alternativa (POST disputes/{id}/alternatives/{alternativeId}).
  const [alternativeId, setAlternativeId] = useState("");
  const [alternativeType, setAlternativeType] = useState("");
  const [alternativeAmount, setAlternativeAmount] = useState("");

  const acceptMutation = useMutation({
    mutationFn: () => acceptIFoodDispute(branchId, disputeId.trim()),
    onSuccess: (result) => {
      toast.success("Disputa aceita no iFood.");
      setLastResult(result.status ?? "aceita");
      setDisputeId("");
    },
    onError: () => toast.error("Não foi possível aceitar a disputa — confira o ID e tente de novo."),
  });

  const rejectMutation = useMutation({
    mutationFn: () => rejectIFoodDispute(branchId, disputeId.trim(), reason.trim()),
    onSuccess: (result) => {
      toast.success("Disputa rejeitada no iFood.");
      setLastResult(result.status ?? "rejeitada");
      setDisputeId("");
      setReason("");
    },
    onError: () => toast.error("Não foi possível rejeitar a disputa — confira o ID, o motivo e tente de novo."),
  });

  const alternativeMutation = useMutation({
    mutationFn: () =>
      requestIFoodDisputeAlternative(
        branchId,
        disputeId.trim(),
        alternativeId.trim(),
        alternativeType.trim(),
        alternativeAmount.trim() ? Number(alternativeAmount.trim().replace(",", ".")) : undefined,
        alternativeAmount.trim() ? "BRL" : undefined,
      ),
    onSuccess: (result) => {
      toast.success("Alternativa proposta no iFood.");
      setLastResult(result.status ?? "alternativa proposta");
      setAlternativeId("");
      setAlternativeType("");
      setAlternativeAmount("");
    },
    onError: () => toast.error("Não foi possível propor a alternativa — confira os dados e tente de novo."),
  });

  return (
    <section className="ticket" style={{ padding: 18, display: "grid", gap: 12, marginTop: 22 }}>
      <div style={{ display: "grid", gap: 2 }}>
        <span className="display" style={{ fontSize: "1.05rem" }}>
          Disputas (Handshake)
        </span>
        <span style={{ color: "var(--ink-faint)", fontSize: "0.85rem" }}>
          quando o cliente abre uma disputa pós-entrega, o iFood avisa a equipe fora do SyncBar —
          informe aqui o ID da disputa pra aceitar, rejeitar ou propor uma alternativa
        </span>
      </div>

      <div className="ui-row ui-row-wrap" style={{ gap: 10, alignItems: "end" }}>
        <div style={{ flex: 1, minWidth: 200 }}>
          <TextField label="ID da disputa" value={disputeId} onChange={(e) => setDisputeId(e.target.value)} />
        </div>
        <div style={{ flex: 2, minWidth: 220 }}>
          <TextField
            label="Motivo (obrigatório para rejeitar)"
            value={reason}
            onChange={(e) => setReason(e.target.value)}
            placeholder="ex.: cliente recebeu o pedido corretamente"
          />
        </div>
        <Button variant="ghost" disabled={!disputeId.trim()} loading={acceptMutation.isPending} onClick={() => acceptMutation.mutate()}>
          Aceitar
        </Button>
        <Button
          variant="danger"
          disabled={!disputeId.trim() || !reason.trim()}
          loading={rejectMutation.isPending}
          onClick={() => rejectMutation.mutate()}
        >
          Rejeitar
        </Button>
      </div>

      <div className="ui-row ui-row-wrap" style={{ gap: 10, alignItems: "end" }}>
        <div style={{ flex: 1, minWidth: 160 }}>
          <TextField label="ID da alternativa" value={alternativeId} onChange={(e) => setAlternativeId(e.target.value)} />
        </div>
        <div style={{ flex: 1, minWidth: 160 }}>
          <TextField
            label="Tipo da alternativa"
            value={alternativeType}
            onChange={(e) => setAlternativeType(e.target.value)}
            placeholder="ex.: REFUND_ITEMS"
          />
        </div>
        <div style={{ flex: 1, minWidth: 120 }}>
          <TextField
            label="Valor (opcional)"
            value={alternativeAmount}
            onChange={(e) => setAlternativeAmount(e.target.value)}
            placeholder="ex.: 10,00"
          />
        </div>
        <Button
          variant="ghost"
          disabled={!disputeId.trim() || !alternativeId.trim() || !alternativeType.trim()}
          loading={alternativeMutation.isPending}
          onClick={() => alternativeMutation.mutate()}
        >
          Propor alternativa
        </Button>
      </div>

      {lastResult && (
        <span style={{ color: "var(--ink-faint)", fontSize: "0.82rem" }}>Última ação registrada — status no iFood: {lastResult}</span>
      )}
    </section>
  );
}
