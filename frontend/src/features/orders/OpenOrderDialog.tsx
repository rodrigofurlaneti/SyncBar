import { useState } from "react";
import { useMutation } from "@tanstack/react-query";
import Swal from "sweetalert2"; // Adicionado SweetAlert2
import { openOrder } from "./api";
import { useAuthStore } from "../../stores/authStore";
import { ApiError } from "../../lib/apiClient";
import type { TableResponse } from "../../lib/types";
import { Overlay } from "./Overlay";

interface Props {
    table: TableResponse;
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

export function OpenOrderDialog({ table, onClose, onOpened }: Props) {
    const { branchId, employeeId } = useAuthStore();
    const [guestCount, setGuestCount] = useState<number>(2);

    const mutation = useMutation({
        mutationFn: () =>
            openOrder({
                branchId,
                diningTableId: table.id,
                comandaId: null,
                employeeId: employeeId ?? 1,
                guestCount,
                notes: null,
            }),
        onSuccess: (orderId) => {
            Toast.fire({ icon: "success", title: `Mesa ${table.number} aberta!` });
            onOpened(orderId);
        },
        onError: (error) => {
            const msg = error instanceof ApiError ? error.message : "Falha ao abrir pedido.";
            Swal.fire("Erro", msg, "error");
        },
    });

    return (
        <Overlay onClose={onClose} title={`Abrir mesa ${table.number}`} data-testid="open-table-overlay">
            <label style={{ display: "grid", gap: 6 }}>
                <span style={{ color: "var(--ink-dim)", fontSize: "0.9rem" }}>Pessoas na mesa</span>
                <input
                    type="number"
                    min={1}
                    value={guestCount}
                    onChange={(e) => setGuestCount(Number(e.target.value))}
                    data-testid="input-guest-count"
                />
            </label>

            <div style={{ display: "flex", gap: 10, justifyContent: "flex-end", marginTop: 24 }}>
                <button type="button" className="btn-ghost" onClick={onClose} data-testid="btn-close-dialog">
                    Voltar
                </button>
                <button
                    type="button"
                    className="btn-primary"
                    onClick={() => mutation.mutate()}
                    disabled={mutation.isPending}
                    data-testid="btn-submit-order"
                >
                    {mutation.isPending ? "Abrindo…" : "Abrir pedido"}
                </button>
            </div>
        </Overlay>
    );
}