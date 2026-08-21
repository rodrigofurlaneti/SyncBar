import { useState } from "react";
import { Link } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  assignIFoodDriver,
  dispatchIFoodLogistics,
  getIFoodLogisticsDeliveries,
  getIFoodLogisticsOrderDetails,
  getIFoodOrders,
  markIFoodArrivedAtDestination,
  markIFoodArrivedAtOrigin,
  markIFoodGoingToOrigin,
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

// Fase 7 — Logistics: entrega, pela FROTA PRÓPRIA, de um pedido QUE VEIO do iFood (deliveredBy
// diferente de "IFOOD" no pedido — ver IFoodOrderResponse.deliveredBy). Diferente da tela
// "Entregas iFood (Shipping)" (fase 8, entregador do próprio iFood pra pedido de OUTRO canal),
// aqui é o inverso — o pedido existe no iFood, só quem entrega é a equipe do lojista, e o iFood
// só precisa ser avisado de cada passo (atribuir → saiu pra origem → chegou na origem →
// despachou → chegou no destino → verificar código de entrega).
const REFRESH_INTERVAL_MS = 20_000;

const STATUS_LABEL: Record<string, string> = {
  DRIVER_ASSIGNED: "Entregador atribuído",
  GOING_TO_ORIGIN: "A caminho da loja",
  ARRIVED_AT_ORIGIN: "Chegou na loja",
  DISPATCHED: "Despachado",
  ARRIVED_AT_DESTINATION: "Chegou no destino",
  DELIVERY_CODE_VERIFIED: "Entrega confirmada",
};

const STATUS_COLOR: Record<string, string> = {
  DRIVER_ASSIGNED: "var(--ink-faint)",
  GOING_TO_ORIGIN: "var(--warn, #d97706)",
  ARRIVED_AT_ORIGIN: "var(--warn, #d97706)",
  DISPATCHED: "var(--warn, #d97706)",
  ARRIVED_AT_DESTINATION: "var(--warn, #d97706)",
  DELIVERY_CODE_VERIFIED: "var(--ok)",
};

export function IFoodLogisticsPage() {
  const queryClient = useQueryClient();
  const { branchId } = useAuthStore();
  const [assigning, setAssigning] = useState<IFoodOrderResponse | null>(null);
  const [verifying, setVerifying] = useState<IFoodLogisticsDeliveryResponse | null>(null);
  const [detailsFor, setDetailsFor] = useState<IFoodLogisticsDeliveryResponse | null>(null);

  const deliveriesQuery = useQuery({
    queryKey: ["integrations", "ifood", "logistics", branchId],
    queryFn: () => getIFoodLogisticsDeliveries(branchId),
    refetchInterval: REFRESH_INTERVAL_MS,
  });

  const ordersQuery = useQuery({
    queryKey: ["integrations", "ifood", "orders", branchId],
    queryFn: () => getIFoodOrders(branchId),
    refetchInterval: REFRESH_INTERVAL_MS,
  });

  const invalidate = () => {
    void queryClient.invalidateQueries({ queryKey: ["integrations", "ifood", "logistics"] });
    void queryClient.invalidateQueries({ queryKey: ["integrations", "ifood", "orders"] });
  };

  const deliveries = deliveriesQuery.data ?? [];
  const assignedIFoodOrderIds = new Set(deliveries.map((d) => d.ifoodOrderId));

  // Pedidos elegíveis pra frota própria: vieram do iFood, deliveredBy preenchido e diferente de
  // "IFOOD" (self-delivery), ainda não cancelados/entregues, e sem entrega já atribuída.
  const eligibleOrders = (ordersQuery.data ?? []).filter(
    (o) =>
      o.deliveredBy != null &&
      o.deliveredBy !== "IFOOD" &&
      o.status !== "CANCELLED" &&
      o.status !== "CONCLUDED" &&
      !assignedIFoodOrderIds.has(o.id),
  );

  return (
    <main style={{ padding: 22, maxWidth: 1000, margin: "0 auto" }}>
      <div className="rise" style={{ marginBottom: 18 }}>
        <Link to="/integracoes/ifood" style={{ color: "var(--ink-faint)", fontSize: "0.85rem" }}>
          ← Integração iFood
        </Link>
        <div style={{ display: "grid", gap: 4 }}>
          <h2 className="display" style={{ fontSize: "1.7rem" }}>
            Logística (frota própria)
          </h2>
          <span style={{ color: "var(--ink-faint)", fontSize: "0.9rem" }}>
            entregue pedidos do iFood com a equipe da própria casa — atribua o entregador e avise
            cada passo pro iFood
          </span>
        </div>
      </div>

      {(deliveriesQuery.isError || ordersQuery.isError) && (
        <QueryError error={deliveriesQuery.error ?? ordersQuery.error} what="a logística do iFood" />
      )}

      <section style={{ marginBottom: 22 }}>
        <span className="display" style={{ fontSize: "1.15rem", display: "block", marginBottom: 10 }}>
          Aguardando entregador
        </span>
        {!ordersQuery.isLoading && eligibleOrders.length === 0 && (
          <EmptyState
            title="Nenhum pedido esperando entregador"
            description="Pedidos do iFood marcados para entrega por frota própria aparecem aqui assim que confirmados."
          />
        )}
        <div style={{ display: "grid", gap: 10 }}>
          {eligibleOrders.map((order) => (
            <div
              key={order.id}
              className="ticket rise"
              style={{ padding: 16, display: "flex", justifyContent: "space-between", alignItems: "center", gap: 12, flexWrap: "wrap" }}
            >
              <div style={{ display: "grid", gap: 2 }}>
                <span style={{ fontWeight: 700 }}>
                  {order.displayId ?? `Pedido #${order.id}`} — {order.customerName}
                </span>
                <span style={{ color: "var(--ink-dim)", fontSize: "0.85rem" }}>{order.deliveryAddress}</span>
              </div>
              <Button variant="primary" onClick={() => setAssigning(order)}>
                Atribuir entregador
              </Button>
            </div>
          ))}
        </div>
      </section>

      <section>
        <span className="display" style={{ fontSize: "1.15rem", display: "block", marginBottom: 10 }}>
          Entregas em andamento
        </span>
        {!deliveriesQuery.isLoading && deliveries.length === 0 && (
          <EmptyState
            title="Nenhuma entrega em andamento"
            description="Assim que um entregador for atribuído a um pedido, o acompanhamento aparece aqui."
          />
        )}
        <div style={{ display: "grid", gap: 12 }}>
          {deliveries.map((delivery) => (
            <LogisticsDeliveryCard
              key={delivery.id}
              delivery={delivery}
              onAdvanced={invalidate}
              onVerifyCode={() => setVerifying(delivery)}
              onViewDetails={() => setDetailsFor(delivery)}
            />
          ))}
        </div>
      </section>

      {detailsFor && <LogisticsOrderDetailsModal delivery={detailsFor} onClose={() => setDetailsFor(null)} />}

      {assigning && (
        <AssignDriverModal
          order={assigning}
          onClose={() => setAssigning(null)}
          onAssigned={() => {
            setAssigning(null);
            invalidate();
          }}
        />
      )}

      {verifying && (
        <VerifyCodeModal
          delivery={verifying}
          onClose={() => setVerifying(null)}
          onVerified={() => {
            setVerifying(null);
            invalidate();
          }}
        />
      )}
    </main>
  );
}

function LogisticsDeliveryCard({
  delivery,
  onAdvanced,
  onVerifyCode,
  onViewDetails,
}: {
  delivery: IFoodLogisticsDeliveryResponse;
  onAdvanced: () => void;
  onVerifyCode: () => void;
  onViewDetails: () => void;
}) {
  const toast = useToast();

  const goingToOriginMutation = useMutation({
    mutationFn: () => markIFoodGoingToOrigin(delivery.ifoodOrderId),
    onSuccess: onAdvanced,
    onError: () => toast.error("Não foi possível avançar — confira se o pedido ainda está ativo."),
  });
  const arrivedAtOriginMutation = useMutation({
    mutationFn: () => markIFoodArrivedAtOrigin(delivery.ifoodOrderId),
    onSuccess: onAdvanced,
    onError: () => toast.error("Não foi possível avançar — confira se o pedido ainda está ativo."),
  });
  const dispatchMutation = useMutation({
    mutationFn: () => dispatchIFoodLogistics(delivery.ifoodOrderId),
    onSuccess: onAdvanced,
    onError: () => toast.error("Não foi possível avançar — confira se o pedido ainda está ativo."),
  });
  const arrivedAtDestinationMutation = useMutation({
    mutationFn: () => markIFoodArrivedAtDestination(delivery.ifoodOrderId),
    onSuccess: onAdvanced,
    onError: () => toast.error("Não foi possível avançar — confira se o pedido ainda está ativo."),
  });

  const pending =
    goingToOriginMutation.isPending || arrivedAtOriginMutation.isPending || dispatchMutation.isPending || arrivedAtDestinationMutation.isPending;

  return (
    <section className="ticket rise" style={{ padding: 18, display: "grid", gap: 10 }}>
      <div className="ui-row ui-row-wrap" style={{ justifyContent: "space-between", gap: 12 }}>
        <div style={{ display: "grid", gap: 2 }}>
          <span style={{ fontWeight: 700, fontSize: "1.05rem" }}>
            {delivery.ifoodOrderDisplayId ?? `Pedido #${delivery.ifoodOrderId}`}
            {delivery.customerName ? ` — ${delivery.customerName}` : ""}
          </span>
          {delivery.deliveryAddress && <span style={{ color: "var(--ink-dim)", fontSize: "0.85rem" }}>{delivery.deliveryAddress}</span>}
          <span style={{ color: "var(--ink-faint)", fontSize: "0.82rem" }}>
            {delivery.driverName} · {delivery.driverPhone} · {delivery.driverVehicleType}
          </span>
        </div>
        <StatusBadge color={STATUS_COLOR[delivery.status] ?? "var(--ink-faint)"}>
          {STATUS_LABEL[delivery.status] ?? delivery.status}
        </StatusBadge>
      </div>

      <div style={{ display: "flex", gap: 10, justifyContent: "flex-end", flexWrap: "wrap" }}>
        <Button variant="ghost" onClick={onViewDetails}>
          Ver detalhes no iFood
        </Button>
        {delivery.status === "DRIVER_ASSIGNED" && (
          <Button variant="primary" loading={goingToOriginMutation.isPending} disabled={pending} onClick={() => goingToOriginMutation.mutate()}>
            Saiu para a loja
          </Button>
        )}
        {delivery.status === "GOING_TO_ORIGIN" && (
          <Button variant="primary" loading={arrivedAtOriginMutation.isPending} disabled={pending} onClick={() => arrivedAtOriginMutation.mutate()}>
            Chegou na loja
          </Button>
        )}
        {delivery.status === "ARRIVED_AT_ORIGIN" && (
          <Button variant="primary" loading={dispatchMutation.isPending} disabled={pending} onClick={() => dispatchMutation.mutate()}>
            Despachar
          </Button>
        )}
        {delivery.status === "DISPATCHED" && (
          <Button
            variant="primary"
            loading={arrivedAtDestinationMutation.isPending}
            disabled={pending}
            onClick={() => arrivedAtDestinationMutation.mutate()}
          >
            Chegou no destino
          </Button>
        )}
        {delivery.status === "ARRIVED_AT_DESTINATION" && (
          <Button variant="primary" onClick={onVerifyCode}>
            Verificar código de entrega
          </Button>
        )}
      </div>
    </section>
  );
}

// Fase 9c — fecha o gap restante do módulo Logistics da auditoria de 2026-08-20/21: detalhes da
// entrega direto no iFood. A doc oficial não documenta o schema da resposta (só "<object>"), então
// o JSON bruto é exibido pré-formatado — não há como saber que campos mostrar de forma amigável.
function LogisticsOrderDetailsModal({ delivery, onClose }: { delivery: IFoodLogisticsDeliveryResponse; onClose: () => void }) {
  const detailsQuery = useQuery({
    queryKey: ["integrations", "ifood", "logistics-order-details", delivery.ifoodOrderId],
    queryFn: () => getIFoodLogisticsOrderDetails(delivery.ifoodOrderId),
  });

  let formatted = detailsQuery.data?.rawPayload ?? null;
  if (formatted) {
    try {
      formatted = JSON.stringify(JSON.parse(formatted), null, 2);
    } catch {
      // mantém o texto cru se não for JSON válido
    }
  }

  return (
    <Modal onClose={onClose} title={`Detalhes no iFood — ${delivery.ifoodOrderDisplayId ?? `Pedido #${delivery.ifoodOrderId}`}`}>
      <div style={{ display: "grid", gap: 12, minWidth: 360, maxWidth: 520 }}>
        <span style={{ color: "var(--ink-faint)", fontSize: "0.82rem" }}>
          A doc oficial do iFood não documenta os campos desta resposta — o JSON é mostrado como o
          iFood devolveu.
        </span>
        {detailsQuery.isError && <QueryError error={detailsQuery.error} what="os detalhes da entrega" />}
        {detailsQuery.isLoading && <span style={{ color: "var(--ink-faint)" }}>Carregando…</span>}
        {formatted && (
          <pre
            style={{
              maxHeight: 320,
              overflow: "auto",
              fontSize: "0.78rem",
              padding: 10,
              borderRadius: 8,
              background: "var(--surface-2, rgba(0,0,0,0.04))",
            }}
          >
            {formatted}
          </pre>
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

function AssignDriverModal({
  order,
  onClose,
  onAssigned,
}: {
  order: IFoodOrderResponse;
  onClose: () => void;
  onAssigned: () => void;
}) {
  const toast = useToast();
  const [driverName, setDriverName] = useState("");
  const [driverPhone, setDriverPhone] = useState("");
  const [driverVehicleType, setDriverVehicleType] = useState("MOTORCYCLE");

  const mutation = useMutation({
    mutationFn: () =>
      assignIFoodDriver(order.id, {
        driverName: driverName.trim(),
        driverPhone: driverPhone.trim(),
        driverVehicleType,
      }),
    onSuccess: () => {
      toast.success("Entregador atribuído — o iFood já foi avisado.");
      onAssigned();
    },
    onError: () => toast.error("Não foi possível atribuir o entregador."),
  });

  return (
    <Modal onClose={onClose} title={`Atribuir entregador — ${order.displayId ?? `Pedido #${order.id}`}`}>
      <div style={{ display: "grid", gap: 14, minWidth: 320 }}>
        <TextField label="Nome do entregador" value={driverName} onChange={(e) => setDriverName(e.target.value)} autoFocus />
        <TextField label="Telefone do entregador" value={driverPhone} onChange={(e) => setDriverPhone(e.target.value)} placeholder="(11) 98765-4321" />
        <SelectField label="Veículo" value={driverVehicleType} onChange={(e) => setDriverVehicleType(e.target.value)}>
          <option value="MOTORCYCLE">Moto</option>
          <option value="BICYCLE">Bicicleta</option>
          <option value="CAR">Carro</option>
          <option value="ON_FOOT">A pé</option>
        </SelectField>

        <div style={{ display: "flex", gap: 10, justifyContent: "flex-end" }}>
          <Button variant="ghost" onClick={onClose}>
            Voltar
          </Button>
          <Button
            variant="primary"
            disabled={!driverName.trim() || !driverPhone.trim()}
            loading={mutation.isPending}
            onClick={() => mutation.mutate()}
          >
            Atribuir
          </Button>
        </div>
      </div>
    </Modal>
  );
}

function VerifyCodeModal({
  delivery,
  onClose,
  onVerified,
}: {
  delivery: IFoodLogisticsDeliveryResponse;
  onClose: () => void;
  onVerified: () => void;
}) {
  const toast = useToast();
  const [code, setCode] = useState("");

  const mutation = useMutation({
    mutationFn: () => verifyIFoodDeliveryCode(delivery.ifoodOrderId, code.trim()),
    onSuccess: (result) => {
      if (result.codeMatched) {
        toast.success("Entrega confirmada.");
        onVerified();
      } else {
        toast.error("Código incorreto — confira com o cliente e tente de novo.");
      }
    },
    onError: () => toast.error("Não foi possível verificar o código."),
  });

  return (
    <Modal onClose={onClose} title="Verificar código de entrega">
      <div style={{ display: "grid", gap: 14, minWidth: 280 }}>
        <TextField label="Código informado pelo cliente" value={code} onChange={(e) => setCode(e.target.value)} autoFocus />
        <div style={{ display: "flex", gap: 10, justifyContent: "flex-end" }}>
          <Button variant="ghost" onClick={onClose}>
            Voltar
          </Button>
          <Button variant="primary" disabled={!code.trim()} loading={mutation.isPending} onClick={() => mutation.mutate()}>
            Verificar
          </Button>
        </div>
      </div>
    </Modal>
  );
}
