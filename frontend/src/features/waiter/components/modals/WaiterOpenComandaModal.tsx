import { useState } from "react";
import { useMutation } from "@tanstack/react-query";
import { openOrder } from "../../../orders/api";
import { useAuthStore } from "../../../../stores/authStore";
import { ApiError } from "../../../../lib/apiClient";
import type { ComandaResponse } from "../../../../lib/types";

interface WaiterOpenComandaModalProps {
    comanda: ComandaResponse;
    onClose: () => void;
    onOpened: (orderId: number) => void;
}

export function WaiterOpenComandaModal({ comanda, onClose, onOpened }: WaiterOpenComandaModalProps) {
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
        onSuccess: (orderId) => onOpened(orderId),
    });

    return (
        <div className="modal-backdrop is-center" onClick={onClose} style={{ position: "absolute" }}>
            <div className="modal-panel is-center" onClick={(e) => e.stopPropagation()} style={{ width: "90%", maxWidth: "360px", padding: "24px" }}>
                <div className="modal-head" style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: "20px" }}>
                    <span className="display" style={{ fontSize: "1.25rem", fontWeight: "800", textTransform: "uppercase" }}>
                        Abrir Comanda {comanda.code || comanda.id}
                    </span>
                    <button type="button" className="btn-ghost btn-icon" onClick={onClose} aria-label="Fechar">
                        ✕
                    </button>
                </div>

                <div style={{ display: "grid", gap: "8px", marginBottom: "24px", textAlign: "left" }}>
                    <label style={{ fontSize: "0.9rem", fontWeight: "600", color: "var(--ink-dim)" }}>
                        Nome do cliente
                    </label>
                    <input
                        value={customerName}
                        onChange={(e) => setCustomerName(e.target.value)}
                        autoFocus
                        placeholder="ex.: João Furlaneti"
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

                {mutation.isError && (
                    <p style={{ color: "var(--w-warn, #ef4444)", fontSize: "0.85rem", marginBottom: "16px", fontWeight: "500", textAlign: "left" }}>
                        {mutation.error instanceof ApiError ? mutation.error.message : "Falha ao abrir comanda."}
                    </p>
                )}

                <div style={{ display: "flex", gap: "12px", justifyContent: "flex-end" }}>
                    <button
                        type="button"
                        className="btn-ghost"
                        onClick={onClose}
                        style={{ padding: "10px 16px", borderRadius: "8px", fontWeight: "600" }}
                    >
                        Voltar
                    </button>
                    <button
                        type="button"
                        className="waiter-cta"
                        onClick={() => mutation.mutate()}
                        disabled={mutation.isPending}
                        style={{ margin: 0, padding: "10px 20px", borderRadius: "8px", fontWeight: "700" }}
                    >
                        {mutation.isPending ? "Abrindo…" : "Abrir comanda"}
                    </button>
                </div>
            </div>
        </div>
    );
}