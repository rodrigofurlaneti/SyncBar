import { useState } from "react";
import { useMutation } from "@tanstack/react-query";
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
    onSuccess: (orderId) => onOpened(orderId),
  });

  return (
    <Overlay onClose={onClose} title={`Abrir mesa ${table.number}`}>
      <label style={{ display: "grid", gap: 6 }}>
        <span style={{ color: "var(--ink-dim)", fontSize: "0.9rem" }}>Pessoas na mesa</span>
        <input
          type="number"
          min={1}
          value={guestCount}
          onChange={(e) => setGuestCount(Number(e.target.value))}
        />
      </label>

      {mutation.isError && (
        <p className="error-text">
          {mutation.error instanceof ApiError ? mutation.error.message : "Falha ao abrir pedido."}
        </p>
      )}

      <div style={{ display: "flex", gap: 10, justifyContent: "flex-end" }}>
        <button className="btn-ghost" onClick={onClose}>
          Voltar
        </button>
        <button className="btn-primary" onClick={() => mutation.mutate()} disabled={mutation.isPending}>
          {mutation.isPending ? "Abrindo…" : "Abrir pedido"}
        </button>
      </div>
    </Overlay>
  );
}
