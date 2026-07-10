import { useState } from "react";
import { useMutation } from "@tanstack/react-query";
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
    onSuccess: (orderId) => onOpened(orderId),
  });

  return (
    <Overlay title={`Abrir comanda ${comanda.code}`} onClose={onClose}>
      <label style={{ display: "grid", gap: 6 }}>
        <span style={{ color: "var(--ink-dim)", fontSize: "0.9rem" }}>Nome do cliente (opcional)</span>
        <input
          value={customerName}
          onChange={(e) => setCustomerName(e.target.value)}
          autoFocus
          placeholder="ex.: João"
        />
      </label>

      {mutation.isError && (
        <p className="error-text">
          {mutation.error instanceof ApiError ? mutation.error.message : "Falha ao abrir comanda."}
        </p>
      )}

      <div style={{ display: "flex", gap: 10, justifyContent: "flex-end" }}>
        <button className="btn-ghost" onClick={onClose}>
          Voltar
        </button>
        <button className="btn-primary" onClick={() => mutation.mutate()} disabled={mutation.isPending}>
          {mutation.isPending ? "Abrindo…" : "Abrir comanda"}
        </button>
      </div>
    </Overlay>
  );
}
