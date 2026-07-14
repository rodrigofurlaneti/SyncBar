import { useState } from "react";
import { useMutation } from "@tanstack/react-query";
import { openOrder } from "./api";
import { useAuthStore } from "../../stores/authStore";
import { ApiError } from "../../lib/apiClient";
import { OrderType } from "../../lib/types";
import { Overlay } from "./Overlay";

interface Props {
  onClose: () => void;
  onOpened: (orderId: number) => void;
}

// Pedido sem mesa/comanda — retirada no balcão ou entrega no endereço do cliente.
// Diferente do fluxo de mesa (OpenOrderDialog), aqui o nome do cliente é obrigatório
// (é o único jeito de identificar o pedido depois) e o endereço só entra em Delivery.
export function OpenDeliveryOrderDialog({ onClose, onOpened }: Props) {
  const { branchId, employeeId } = useAuthStore();
  const [orderTypeId, setOrderTypeId] = useState<number>(OrderType.Retirada);
  const [customerName, setCustomerName] = useState("");
  const [customerPhone, setCustomerPhone] = useState("");
  const [deliveryAddress, setDeliveryAddress] = useState("");

  const mutation = useMutation({
    mutationFn: () =>
      openOrder({
        branchId,
        diningTableId: null,
        comandaId: null,
        employeeId: employeeId ?? 1,
        guestCount: null,
        notes: null,
        orderTypeId,
        customerName: customerName.trim(),
        customerPhone: customerPhone.trim() === "" ? null : customerPhone.trim(),
        deliveryAddress: orderTypeId === OrderType.Delivery ? deliveryAddress.trim() : null,
      }),
    onSuccess: (orderId) => onOpened(orderId),
  });

  const isDelivery = orderTypeId === OrderType.Delivery;
  const canSubmit =
    customerName.trim() !== "" && (!isDelivery || deliveryAddress.trim() !== "");

  return (
    <Overlay onClose={onClose} title="Novo pedido — retirada / delivery">
      <div style={{ display: "flex", gap: 8 }}>
        <button
          className={orderTypeId === OrderType.Retirada ? "btn-primary" : "btn-ghost"}
          style={{ flex: 1 }}
          onClick={() => setOrderTypeId(OrderType.Retirada)}
        >
          Retirada
        </button>
        <button
          className={isDelivery ? "btn-primary" : "btn-ghost"}
          style={{ flex: 1 }}
          onClick={() => setOrderTypeId(OrderType.Delivery)}
        >
          Delivery
        </button>
      </div>

      <label style={{ display: "grid", gap: 6 }}>
        <span style={{ color: "var(--ink-dim)", fontSize: "0.9rem" }}>Nome do cliente</span>
        <input value={customerName} onChange={(e) => setCustomerName(e.target.value)} autoFocus />
      </label>

      <label style={{ display: "grid", gap: 6 }}>
        <span style={{ color: "var(--ink-dim)", fontSize: "0.9rem" }}>Telefone</span>
        <input value={customerPhone} onChange={(e) => setCustomerPhone(e.target.value)} />
      </label>

      {isDelivery && (
        <label style={{ display: "grid", gap: 6 }}>
          <span style={{ color: "var(--ink-dim)", fontSize: "0.9rem" }}>Endereço de entrega</span>
          <input value={deliveryAddress} onChange={(e) => setDeliveryAddress(e.target.value)} />
        </label>
      )}

      {mutation.isError && (
        <p className="error-text">
          {mutation.error instanceof ApiError ? mutation.error.message : "Falha ao abrir pedido."}
        </p>
      )}

      <div style={{ display: "flex", gap: 10, justifyContent: "flex-end" }}>
        <button className="btn-ghost" onClick={onClose}>
          Voltar
        </button>
        <button className="btn-primary" disabled={!canSubmit || mutation.isPending} onClick={() => mutation.mutate()}>
          {mutation.isPending ? "Abrindo…" : "Abrir pedido"}
        </button>
      </div>
    </Overlay>
  );
}
