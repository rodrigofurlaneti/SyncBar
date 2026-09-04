import { useState } from "react";
import { useMutation } from "@tanstack/react-query";
import Swal from "sweetalert2"; // Adicionado SweetAlert2
import { openOrder } from "./api";
import { useAuthStore } from "../../stores/authStore";
import { ApiError } from "../../lib/apiClient";
import { OrderType } from "../../lib/types";
import { Overlay } from "./Overlay";

interface Props {
    onClose: () => void;
    onOpened: (orderId: number) => void;
}

// Configuração do Toast do SweetAlert2
const Toast = Swal.mixin({
    toast: true,
    position: "top-end",
    showConfirmButton: false,
    timer: 3000,
    timerProgressBar: true,
});

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
        onSuccess: (orderId) => {
            Toast.fire({ icon: "success", title: "Pedido aberto com sucesso!" });
            onOpened(orderId);
        },
        onError: (error) => {
            const msg = error instanceof ApiError ? error.message : "Falha ao abrir pedido.";
            Swal.fire("Erro", msg, "error");
        },
    });

    const isDelivery = orderTypeId === OrderType.Delivery;
    const canSubmit =
        customerName.trim() !== "" && (!isDelivery || deliveryAddress.trim() !== "");

    return (
        <Overlay onClose={onClose} title="Novo pedido — retirada / delivery" data-testid="open-delivery-overlay">
            <div style={{ display: "flex", gap: 8, marginBottom: 16 }}>
                <button
                    type="button"
                    className={orderTypeId === OrderType.Retirada ? "btn-primary" : "btn-ghost"}
                    style={{ flex: 1 }}
                    onClick={() => setOrderTypeId(OrderType.Retirada)}
                    data-testid="btn-type-retirada"
                >
                    Retirada
                </button>
                <button
                    type="button"
                    className={isDelivery ? "btn-primary" : "btn-ghost"}
                    style={{ flex: 1 }}
                    onClick={() => setOrderTypeId(OrderType.Delivery)}
                    data-testid="btn-type-delivery"
                >
                    Delivery
                </button>
            </div>

            <label style={{ display: "grid", gap: 6, marginBottom: 12 }}>
                <span style={{ color: "var(--ink-dim)", fontSize: "0.9rem" }}>Nome do cliente</span>
                <input
                    value={customerName}
                    onChange={(e) => setCustomerName(e.target.value)}
                    autoFocus
                    data-testid="input-customer-name"
                />
            </label>

            <label style={{ display: "grid", gap: 6, marginBottom: 12 }}>
                <span style={{ color: "var(--ink-dim)", fontSize: "0.9rem" }}>Telefone</span>
                <input
                    value={customerPhone}
                    onChange={(e) => setCustomerPhone(e.target.value)}
                    data-testid="input-customer-phone"
                />
            </label>

            {isDelivery && (
                <label style={{ display: "grid", gap: 6, marginBottom: 12 }}>
                    <span style={{ color: "var(--ink-dim)", fontSize: "0.9rem" }}>Endereço de entrega</span>
                    <input
                        value={deliveryAddress}
                        onChange={(e) => setDeliveryAddress(e.target.value)}
                        data-testid="input-delivery-address"
                    />
                </label>
            )}

            <div style={{ display: "flex", gap: 10, justifyContent: "flex-end", marginTop: 24 }}>
                <button type="button" className="btn-ghost" onClick={onClose} data-testid="btn-close-dialog">
                    Voltar
                </button>
                <button
                    type="button"
                    className="btn-primary"
                    disabled={!canSubmit || mutation.isPending}
                    onClick={() => mutation.mutate()}
                    data-testid="btn-submit-order"
                >
                    {mutation.isPending ? "Abrindo…" : "Abrir pedido"}
                </button>
            </div>
        </Overlay>
    );
}