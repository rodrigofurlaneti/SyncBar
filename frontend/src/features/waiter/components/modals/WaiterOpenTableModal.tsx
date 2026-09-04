import { useState } from "react";
import { useMutation } from "@tanstack/react-query";
import Swal from "sweetalert2"; // Adicionado SweetAlert2
import { openOrder } from "../../../orders/api";
import { useAuthStore } from "../../../../stores/authStore";
import { ApiError } from "../../../../lib/apiClient";
import type { TableResponse } from "../../../../lib/types";

interface WaiterOpenTableModalProps {
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

export function WaiterOpenTableModal({ table, onClose, onOpened }: WaiterOpenTableModalProps) {
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
            Toast.fire({ icon: "success", title: `Mesa ${table.number} aberta com sucesso!` });
            onOpened(orderId);
        },
        onError: (e) => {
            const msg = e instanceof ApiError ? e.message : "Falha ao abrir mesa.";
            Swal.fire("Erro", msg, "error");
        },
    });

    return (
        <div
            className="modal-backdrop is-center"
            onMouseDown={(e) => {
                if (e.target === e.currentTarget) onClose();
            }}
            style={{ position: "absolute" }}
            data-testid="waiter-open-table-backdrop"
        >
            <div className="modal-panel is-center" style={{ width: "90%", maxWidth: "360px", padding: "24px" }} data-testid="waiter-open-table-modal">
                <div className="modal-head" style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: "20px" }}>
                    <span className="display" style={{ fontSize: "1.25rem", fontWeight: "800", textTransform: "uppercase" }} data-testid="modal-table-title">
                        Abrir Mesa {table.number}
                    </span>
                    <button type="button" className="btn-ghost btn-icon" onClick={onClose} aria-label="Fechar" data-testid="btn-close-modal">
                        ✕
                    </button>
                </div>

                <div style={{ display: "grid", gap: "8px", marginBottom: "24px", textAlign: "left" }}>
                    <label style={{ fontSize: "0.9rem", fontWeight: "600", color: "var(--ink-dim)" }}>
                        Pessoas na mesa
                    </label>
                    <input
                        type="number"
                        min={1}
                        value={guestCount}
                        onChange={(e) => setGuestCount(Number(e.target.value))}
                        autoFocus
                        data-testid="input-guest-count"
                        style={{
                            padding: "12px",
                            borderRadius: "8px",
                            border: "1px solid var(--border)",
                            backgroundColor: "var(--bg-raise, #f3f4f6)",
                            color: "var(--ink)",
                            width: "100%",
                            fontSize: "1rem"
                        }}
                    />
                </div>

                <div style={{ display: "flex", gap: "12px", justifyContent: "flex-end" }}>
                    <button
                        type="button"
                        className="btn-ghost"
                        onClick={onClose}
                        style={{ padding: "10px 16px", borderRadius: "8px", fontWeight: "600" }}
                        data-testid="btn-cancel-modal"
                    >
                        Voltar
                    </button>
                    <button
                        type="button"
                        className="waiter-cta"
                        onClick={() => mutation.mutate()}
                        disabled={mutation.isPending}
                        style={{ margin: 0, padding: "10px 20px", borderRadius: "8px", fontWeight: "700" }}
                        data-testid="btn-submit-open-table"
                    >
                        {mutation.isPending ? "Abrindo…" : "Abrir mesa"}
                    </button>
                </div>
            </div>
        </div>
    );
}