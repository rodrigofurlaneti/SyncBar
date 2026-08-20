import { useState } from "react";
import { Link } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  cancelIFoodShippingDelivery,
  getIFoodSafeDeliveryScore,
  getIFoodShippingCancellationReasons,
  getIFoodShippingDeliveries,
  getIFoodShippingQuote,
  getIFoodShippingTracking,
  requestIFoodShippingDriver,
  type IFoodShippingDeliveryResponse,
  type IFoodShippingItemInput,
  type IFoodShippingQuoteResponse,
} from "./api";
import { useAuthStore } from "../../stores/authStore";
import { useToast } from "../../ui/Toast";
import { Button } from "../../ui/Button";
import { StatusBadge } from "../../ui/StatusBadge";
import { Modal } from "../../ui/Modal";
import { SelectField, TextField } from "../../ui/Field";
import { QueryError } from "../../components/QueryError";
import { EmptyState } from "../../ui/EmptyState";

// Fase 8 — Shipping: pede um entregador do iFood pra um pedido de OUTRO canal (telefone,
// WhatsApp, balcão). Diferente da tela "Pedidos iFood" (fase 7, frota própria entregando pedido
// QUE VEIO do iFood), aqui é o inverso — o pedido nunca existiu no iFood, só a entrega é feita
// pelos entregadores dele. O iFood não devolve um "status" de entrega neste módulo (só o id +
// trackingUrl na criação, e lat/long em /tracking), então o status aqui só reflete se a entrega
// ainda está ativa ou foi cancelada pelo SyncBar.
const REFRESH_INTERVAL_MS = 20_000;

const STATUS_LABEL: Record<string, string> = {
  DRIVER_REQUESTED: "Entregador solicitado",
  CANCELLED: "Cancelada",
};

const STATUS_COLOR: Record<string, string> = {
  DRIVER_REQUESTED: "var(--ok)",
  CANCELLED: "var(--danger)",
};

export function IFoodShippingPage() {
  const queryClient = useQueryClient();
  const { branchId } = useAuthStore();
  const [requesting, setRequesting] = useState(false);
  const [tracking, setTracking] = useState<IFoodShippingDeliveryResponse | null>(null);
  const [cancelling, setCancelling] = useState<IFoodShippingDeliveryResponse | null>(null);

  const deliveriesQuery = useQuery({
    queryKey: ["integrations", "ifood", "shipping", branchId],
    queryFn: () => getIFoodShippingDeliveries(branchId),
    refetchInterval: REFRESH_INTERVAL_MS,
  });

  const invalidate = () => void queryClient.invalidateQueries({ queryKey: ["integrations", "ifood", "shipping"] });

  const deliveries = deliveriesQuery.data ?? [];

  return (
    <main style={{ padding: 22, maxWidth: 1000, margin: "0 auto" }}>
      <div className="rise" style={{ marginBottom: 18 }}>
        <Link to="/integracoes/ifood" style={{ color: "var(--ink-faint)", fontSize: "0.85rem" }}>
          ← Integração iFood
        </Link>
        <div className="ui-row ui-row-wrap" style={{ justifyContent: "space-between", alignItems: "flex-end", gap: 12 }}>
          <div style={{ display: "grid", gap: 4 }}>
            <h2 className="display" style={{ fontSize: "1.7rem" }}>
              Entregas iFood (Shipping)
            </h2>
            <span style={{ color: "var(--ink-faint)", fontSize: "0.9rem" }}>
              peça um entregador do iFood pra um pedido de outro canal (telefone, WhatsApp, balcão)
            </span>
          </div>
          <Button variant="primary" onClick={() => setRequesting(true)}>
            + Nova entrega
          </Button>
        </div>
      </div>

      {deliveriesQuery.isError && <QueryError error={deliveriesQuery.error} what="as entregas do iFood" />}

      {!deliveriesQuery.isLoading && deliveries.length === 0 && !deliveriesQuery.isError && (
        <EmptyState
          title="Nenhuma entrega em aberto"
          description="Peça um entregador do iFood pra um pedido de telefone, WhatsApp ou balcão que precise ser entregue."
        />
      )}

      <div style={{ display: "grid", gap: 12 }}>
        {deliveries.map((delivery) => (
          <ShippingDeliveryCard
            key={delivery.id}
            delivery={delivery}
            onTrack={() => setTracking(delivery)}
            onCancel={() => setCancelling(delivery)}
          />
        ))}
      </div>

      {requesting && (
        <RequestDriverModal
          branchId={branchId}
          onClose={() => setRequesting(false)}
          onRequested={() => {
            setRequesting(false);
            invalidate();
          }}
        />
      )}

      {tracking && <TrackingModal delivery={tracking} onClose={() => setTracking(null)} />}

      {cancelling && (
        <CancelDeliveryModal
          delivery={cancelling}
          onClose={() => setCancelling(null)}
          onCancelled={() => {
            setCancelling(null);
            invalidate();
          }}
        />
      )}
    </main>
  );
}

function ShippingDeliveryCard({
  delivery,
  onTrack,
  onCancel,
}: {
  delivery: IFoodShippingDeliveryResponse;
  onTrack: () => void;
  onCancel: () => void;
}) {
  const canCancel = delivery.status !== "CANCELLED";

  return (
    <section className="ticket rise" style={{ padding: 18, display: "grid", gap: 10 }}>
      <div className="ui-row ui-row-wrap" style={{ justifyContent: "space-between", gap: 12 }}>
        <div style={{ display: "grid", gap: 2 }}>
          <span style={{ fontWeight: 700, fontSize: "1.05rem" }}>
            {delivery.orderReference ?? `Entrega #${delivery.id}`} — {delivery.customerName}
          </span>
          <span style={{ color: "var(--ink-dim)", fontSize: "0.85rem" }}>{delivery.deliveryAddress}</span>
        </div>
        <div style={{ display: "grid", gap: 6, justifyItems: "end" }}>
          <StatusBadge color={STATUS_COLOR[delivery.status] ?? "var(--ink-faint)"}>
            {STATUS_LABEL[delivery.status] ?? delivery.status}
          </StatusBadge>
          <span style={{ fontWeight: 600 }}>
            {delivery.merchantFee.toLocaleString("pt-BR", { style: "currency", currency: "BRL" })}
          </span>
        </div>
      </div>

      {canCancel && (
        <div style={{ display: "flex", gap: 10, justifyContent: "flex-end" }}>
          <Button variant="ghost" onClick={onTrack}>
            Rastrear
          </Button>
          <Button variant="ghost" onClick={onCancel}>
            Cancelar entrega
          </Button>
        </div>
      )}
    </section>
  );
}

function RequestDriverModal({
  branchId,
  onClose,
  onRequested,
}: {
  branchId: number;
  onClose: () => void;
  onRequested: () => void;
}) {
  const toast = useToast();
  const [step, setStep] = useState<"form" | "confirm">("form");
  const [quote, setQuote] = useState<IFoodShippingQuoteResponse | null>(null);

  const [orderReference, setOrderReference] = useState("");
  const [customerName, setCustomerName] = useState("");
  const [phoneAreaCode, setPhoneAreaCode] = useState("");
  const [phoneNumber, setPhoneNumber] = useState("");
  const [merchantFee, setMerchantFee] = useState("0");
  const [postalCode, setPostalCode] = useState("");
  const [streetName, setStreetName] = useState("");
  const [streetNumber, setStreetNumber] = useState("");
  const [complement, setComplement] = useState("");
  const [neighborhood, setNeighborhood] = useState("");
  const [city, setCity] = useState("");
  const [state, setState] = useState("");
  const [latitude, setLatitude] = useState("");
  const [longitude, setLongitude] = useState("");
  const [itemName, setItemName] = useState("");
  const [itemQuantity, setItemQuantity] = useState("1");
  const [itemUnitPrice, setItemUnitPrice] = useState("");
  const [items, setItems] = useState<IFoodShippingItemInput[]>([]);

  const hasCoordinates = latitude.trim() !== "" && longitude.trim() !== "";

  const quoteMutation = useMutation({
    mutationFn: () => {
      if (!hasCoordinates) throw new Error("missing-coordinates");
      return getIFoodShippingQuote(branchId, Number(latitude), Number(longitude));
    },
    onSuccess: (result) => {
      setQuote(result);
      setStep("confirm");
    },
    onError: () => toast.error("Não foi possível cotar a entrega — confira as coordenadas e tente de novo."),
  });

  const requestMutation = useMutation({
    mutationFn: () => {
      if (!quote) throw new Error("missing-quote");
      return requestIFoodShippingDriver({
        branchId,
        orderReference: orderReference.trim() || undefined,
        customerName: customerName.trim(),
        customerPhoneAreaCode: phoneAreaCode.trim(),
        customerPhoneNumber: phoneNumber.trim(),
        merchantFee: Number(merchantFee) || 0,
        quoteId: quote.quoteId,
        postalCode: postalCode.trim(),
        streetNumber: streetNumber.trim(),
        streetName: streetName.trim(),
        complement: complement.trim() || undefined,
        neighborhood: neighborhood.trim(),
        city: city.trim(),
        state: state.trim(),
        latitude: hasCoordinates ? Number(latitude) : undefined,
        longitude: hasCoordinates ? Number(longitude) : undefined,
        items,
      });
    },
    onSuccess: () => {
      toast.success("Entregador solicitado ao iFood.");
      onRequested();
    },
    onError: () => toast.error("Não foi possível solicitar o entregador — a cotação pode ter expirado, tente cotar de novo."),
  });

  const addItem = () => {
    if (!itemName.trim() || Number(itemUnitPrice) <= 0) return;
    setItems([...items, { name: itemName.trim(), quantity: Number(itemQuantity) || 1, unitPrice: Number(itemUnitPrice) }]);
    setItemName("");
    setItemQuantity("1");
    setItemUnitPrice("");
  };

  const canQuote =
    customerName.trim() && phoneAreaCode.trim() && phoneNumber.trim() && postalCode.trim() &&
    streetName.trim() && streetNumber.trim() && neighborhood.trim() && city.trim() && state.trim() &&
    hasCoordinates && items.length > 0;

  if (step === "confirm" && quote) {
    return (
      <Modal onClose={onClose} title="Confirmar entrega">
        <div style={{ display: "grid", gap: 14, minWidth: 340 }}>
          <div className="ticket" style={{ padding: 14, display: "grid", gap: 6 }}>
            <span style={{ fontWeight: 600 }}>Cotação do iFood</span>
            <span>Valor: {quote.netValue.toLocaleString("pt-BR", { style: "currency", currency: "BRL" })}</span>
            <span>
              Prazo estimado: {Math.round(quote.deliveryTimeMinMinutes)}–{Math.round(quote.deliveryTimeMaxMinutes)} min
            </span>
            <span style={{ color: "var(--ink-faint)", fontSize: "0.85rem" }}>Distância: {(quote.distanceMeters / 1000).toFixed(1)} km</span>
            {quote.expirationAt && (
              <span style={{ color: "var(--warn, #d97706)", fontSize: "0.8rem" }}>
                Cotação válida até {new Date(quote.expirationAt).toLocaleTimeString("pt-BR")} — confirme logo.
              </span>
            )}
          </div>

          <TextField
            label="Taxa do estabelecimento (opcional)"
            inputMode="decimal"
            value={merchantFee}
            onChange={(e) => setMerchantFee(e.target.value)}
          />

          <div style={{ display: "flex", gap: 10, justifyContent: "flex-end" }}>
            <Button variant="ghost" onClick={() => setStep("form")}>
              Voltar
            </Button>
            <Button variant="primary" loading={requestMutation.isPending} onClick={() => requestMutation.mutate()}>
              Confirmar entregador
            </Button>
          </div>
        </div>
      </Modal>
    );
  }

  return (
    <Modal onClose={onClose} title="Nova entrega via iFood">
      <div style={{ display: "grid", gap: 14, minWidth: 380 }}>
        <TextField
          label="Referência do pedido (opcional)"
          placeholder="ex.: Balcão #45, Telefone (11) 98765-4321"
          value={orderReference}
          onChange={(e) => setOrderReference(e.target.value)}
        />
        <TextField label="Nome do cliente" value={customerName} onChange={(e) => setCustomerName(e.target.value)} autoFocus />
        <div className="ui-row ui-row-wrap" style={{ gap: 10 }}>
          <div style={{ width: 90 }}>
            <TextField label="DDD" inputMode="numeric" value={phoneAreaCode} onChange={(e) => setPhoneAreaCode(e.target.value)} />
          </div>
          <div style={{ flex: 1, minWidth: 160 }}>
            <TextField label="Telefone" inputMode="numeric" value={phoneNumber} onChange={(e) => setPhoneNumber(e.target.value)} />
          </div>
        </div>

        <span style={{ fontWeight: 600, marginTop: 4 }}>Endereço de entrega</span>
        <div className="ui-row ui-row-wrap" style={{ gap: 10 }}>
          <div style={{ flex: 1, minWidth: 140 }}>
            <TextField label="CEP" value={postalCode} onChange={(e) => setPostalCode(e.target.value)} />
          </div>
          <div style={{ width: 100 }}>
            <TextField label="Número" value={streetNumber} onChange={(e) => setStreetNumber(e.target.value)} />
          </div>
        </div>
        <TextField label="Rua" value={streetName} onChange={(e) => setStreetName(e.target.value)} />
        <TextField label="Complemento (opcional)" value={complement} onChange={(e) => setComplement(e.target.value)} />
        <div className="ui-row ui-row-wrap" style={{ gap: 10 }}>
          <div style={{ flex: 1, minWidth: 160 }}>
            <TextField label="Bairro" value={neighborhood} onChange={(e) => setNeighborhood(e.target.value)} />
          </div>
          <div style={{ flex: 1, minWidth: 140 }}>
            <TextField label="Cidade" value={city} onChange={(e) => setCity(e.target.value)} />
          </div>
          <div style={{ width: 70 }}>
            <TextField label="UF" value={state} onChange={(e) => setState(e.target.value.toUpperCase())} />
          </div>
        </div>
        <div className="ui-row ui-row-wrap" style={{ gap: 10 }}>
          <div style={{ flex: 1, minWidth: 140 }}>
            <TextField
              label="Latitude"
              inputMode="decimal"
              value={latitude}
              onChange={(e) => setLatitude(e.target.value)}
              hint="obrigatória para cotar"
            />
          </div>
          <div style={{ flex: 1, minWidth: 140 }}>
            <TextField label="Longitude" inputMode="decimal" value={longitude} onChange={(e) => setLongitude(e.target.value)} />
          </div>
        </div>

        <span style={{ fontWeight: 600, marginTop: 4 }}>Itens do pedido</span>
        {items.length > 0 && (
          <ul style={{ margin: 0, paddingLeft: 18, display: "grid", gap: 4 }}>
            {items.map((item, idx) => (
              <li key={idx} style={{ fontSize: "0.9rem" }}>
                {item.quantity}x {item.name} —{" "}
                {(item.unitPrice * item.quantity).toLocaleString("pt-BR", { style: "currency", currency: "BRL" })}
              </li>
            ))}
          </ul>
        )}
        <div className="ui-row ui-row-wrap" style={{ gap: 10, alignItems: "flex-end" }}>
          <div style={{ flex: 1, minWidth: 140 }}>
            <TextField label="Item" value={itemName} onChange={(e) => setItemName(e.target.value)} />
          </div>
          <div style={{ width: 70 }}>
            <TextField label="Qtd" inputMode="numeric" value={itemQuantity} onChange={(e) => setItemQuantity(e.target.value)} />
          </div>
          <div style={{ width: 110 }}>
            <TextField label="Preço unit." inputMode="decimal" value={itemUnitPrice} onChange={(e) => setItemUnitPrice(e.target.value)} />
          </div>
          <Button variant="ghost" onClick={addItem}>
            + Item
          </Button>
        </div>

        <div style={{ display: "flex", gap: 10, justifyContent: "flex-end", marginTop: 6 }}>
          <Button variant="ghost" onClick={onClose}>
            Voltar
          </Button>
          <Button variant="primary" disabled={!canQuote} loading={quoteMutation.isPending} onClick={() => quoteMutation.mutate()}>
            Cotar entrega
          </Button>
        </div>
      </div>
    </Modal>
  );
}

function TrackingModal({ delivery, onClose }: { delivery: IFoodShippingDeliveryResponse; onClose: () => void }) {
  const trackingQuery = useQuery({
    queryKey: ["integrations", "ifood", "shipping-tracking", delivery.id],
    queryFn: () => getIFoodShippingTracking(delivery.id),
    refetchInterval: 15_000,
  });

  const scoreQuery = useQuery({
    queryKey: ["integrations", "ifood", "shipping-safe-score", delivery.id],
    queryFn: () => getIFoodSafeDeliveryScore(delivery.id),
  });

  const tracking = trackingQuery.data;

  return (
    <Modal onClose={onClose} title={`Rastrear ${delivery.orderReference ?? `entrega #${delivery.id}`}`}>
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
        {scoreQuery.data?.score && (
          <span style={{ color: "var(--ink-faint)", fontSize: "0.85rem" }}>Índice de segurança da entrega: {scoreQuery.data.score}</span>
        )}
        {delivery.trackingUrl && (
          <a href={delivery.trackingUrl} target="_blank" rel="noreferrer" style={{ fontSize: "0.9rem" }}>
            Abrir rastreamento completo do iFood ↗
          </a>
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

function CancelDeliveryModal({
  delivery,
  onClose,
  onCancelled,
}: {
  delivery: IFoodShippingDeliveryResponse;
  onClose: () => void;
  onCancelled: () => void;
}) {
  const toast = useToast();
  const [cancelCodeId, setCancelCodeId] = useState("");

  const reasonsQuery = useQuery({
    queryKey: ["integrations", "ifood", "shipping-cancellation-reasons", delivery.id],
    queryFn: () => getIFoodShippingCancellationReasons(delivery.id),
  });

  const cancelMutation = useMutation({
    mutationFn: () => {
      const reason = reasonsQuery.data?.find((r) => r.cancelCodeId === cancelCodeId);
      return cancelIFoodShippingDelivery(delivery.id, reason?.description ?? "Cancelado pelo lojista", Number(cancelCodeId) || 0);
    },
    onSuccess: () => {
      toast.success("Entrega cancelada no iFood.");
      onCancelled();
    },
    onError: () => toast.error("Não foi possível cancelar a entrega."),
  });

  const reasons = reasonsQuery.data ?? [];

  return (
    <Modal onClose={onClose} title={`Cancelar ${delivery.orderReference ?? `entrega #${delivery.id}`}`}>
      <div style={{ display: "grid", gap: 14, minWidth: 320 }}>
        {reasonsQuery.isError && <QueryError error={reasonsQuery.error} what="os motivos de cancelamento" />}

        <SelectField
          label="Motivo do cancelamento"
          value={cancelCodeId}
          onChange={(e) => setCancelCodeId(e.target.value)}
          disabled={reasonsQuery.isLoading}
        >
          <option value="">Selecione um motivo…</option>
          {reasons.map((reason) => (
            <option key={reason.cancelCodeId} value={reason.cancelCodeId}>
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
            disabled={!cancelCodeId}
            loading={cancelMutation.isPending}
            onClick={() => cancelMutation.mutate()}
          >
            Cancelar entrega
          </Button>
        </div>
      </div>
    </Modal>
  );
}
