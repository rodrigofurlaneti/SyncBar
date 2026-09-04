import { useState } from "react";
import { useMutation } from "@tanstack/react-query";
import Swal from "sweetalert2"; // Adicionado SweetAlert2
import { openOrder } from "../orders/api";
import { useAuthStore } from "../../stores/authStore";
import { ApiError } from "../../lib/apiClient";
import type { ComandaResponse } from "../../lib/types";
import { Overlay } from "../orders/Overlay";

interface Props {
    comanda: ComandaResponse;
    onClose: () => void;
    onOpened: (orderId: number) => void;
}

export function OpenComandaDialog({ comanda, onClose, onOpened }: Props) {
    const { branchId, employeeId } = useAuthStore();
    const [customerName, setCustomerName] = useState("");

    const mutation = useMutation({
        mutationFn: () =>
            openOrder({
                branchId,
                diningTableId: null,
                comandaId: comanda.id,
                employeeId: employeeId ?? 1,
                guestCount: 1,
                notes: customerName.trim() === "" ? null : `Cliente: ${customerName.trim()}`,
            }),
        onSuccess: (orderId) => {
            Swal.fire({
                title: "Sucesso!",
                text: `Comanda ${comanda.code} aberta com sucesso.`,
                icon: "success",
                timer: 1500,
                showConfirmButton: false,
            });
            onOpened(orderId);
        },
        onError: (error) => {
            const msg = error instanceof ApiError ? error.message : "Falha ao abrir comanda.";
            Swal.fire("Erro", msg, "error");
        },
    });

    return (
        <Overlay title={`Abrir comanda ${comanda.code}`} onClose={onClose} data-testid="open-comanda-overlay">
            <label style={{ display: "grid", gap: 6 }}>
                <span style={{ color: "var(--ink-dim)", fontSize: "0.9rem" }}>Nome do cliente</span>
                <input
                    value={customerName}
                    onChange={(e) => setCustomerName(e.target.value)}
                    autoFocus
                    placeholder="ex.: João"
                    data-testid="customer-name-input"
                />
            </label>

            <div style={{ display: "flex", gap: 10, justifyContent: "flex-end", marginTop: 16 }}>
                <button type="button" className="btn-ghost" onClick={onClose} data-testid="close-comanda-btn">
                    Voltar
                </button>
                <button
                    type="button"
                    className="btn-primary"
                    onClick={() => mutation.mutate()}
                    disabled={mutation.isPending}
                    data-testid="submit-comanda-btn"
                >
                    {mutation.isPending ? "Abrindo…" : "Abrir comanda"}
                </button>
            </div>
        </Overlay>
    );
}