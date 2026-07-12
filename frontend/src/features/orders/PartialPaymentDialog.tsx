import { useState } from "react";
import { useMutation, useQuery } from "@tanstack/react-query";
import { getOpenSession } from "../cash/api";
import { registerPartialPayment } from "../billing/api";
import { useAuthStore } from "../../stores/authStore";
import { ApiError } from "../../lib/apiClient";
import {
  DEFAULT_CASH_REGISTER_ID,
  PaymentMethod,
  formatBRL,
  paymentMethodLabel,
} from "../../lib/types";
import type { OrderResponse } from "../../lib/types";
import { Overlay } from "./Overlay";

interface Props {
  order: OrderResponse;
  onClose: () => void;
  onRegistered: () => void;
}

const parseNum = (raw: string): number | null => {
  const value = Number(raw.replace(",", "."));
  return Number.isFinite(value) && value > 0 ? value : null;
};

const needsReceipt = (methodId: number): boolean =>
  methodId === PaymentMethod.CartaoCredito ||
  methodId === PaymentMethod.CartaoDebito ||
  methodId === PaymentMethod.Pix;

export function PartialPaymentDialog({ order, onClose, onRegistered }: Props) {
  const { employeeId } = useAuthStore();
  const [amount, setAmount] = useState("");
  const [methodId, setMethodId] = useState<number>(PaymentMethod.Dinheiro);
  const [authorizationCode, setAuthorizationCode] = useState("");
  const [payerName, setPayerName] = useState("");
  const [error, setError] = useState<string | null>(null);

  const remaining = order.totalAmount - order.partialPaidAmount;

  const sessionQuery = useQuery({
    queryKey: ["cash", "open", DEFAULT_CASH_REGISTER_ID],
    queryFn: () => getOpenSession(DEFAULT_CASH_REGISTER_ID),
    retry: false,
  });

  const noSession =
    sessionQuery.isError &&
    sessionQuery.error instanceof ApiError &&
    (sessionQuery.error.status === 404 || sessionQuery.error.status === 403);

  const value = parseNum(amount);

  const mutation = useMutation({
    mutationFn: () =>
      registerPartialPayment({
        customerOrderId: order.id,
        cashSessionId: sessionQuery.data!.id,
        employeeId: employeeId ?? 1,
        paymentMethodId: methodId,
        amount: value ?? 0,
        authorizationCode: authorizationCode.trim() === "" ? null : authorizationCode.trim(),
        payerName: payerName.trim() === "" ? null : payerName.trim(),
      }),
    onSuccess: onRegistered,
    onError: (e) => setError(e instanceof ApiError ? e.message : "Falha ao registrar o pagamento parcial."),
  });

  return (
    <Overlay title="Pagamento parcial" onClose={onClose}>
      <p style={{ color: "var(--ink-dim)", fontSize: "0.9rem", margin: 0 }}>
        Cliente saindo antes? Registre o valor pago — a mesa continua aberta e o
        restante é cobrado no fechamento. Restante atual:{" "}
        <strong className="mono-num" style={{ color: "var(--amber)" }}>{formatBRL(remaining)}</strong>
      </p>

      {noSession && (
        <p className="error-text">
          O caixa está fechado — abra uma sessão (botão Caixa no topo) para receber.
        </p>
      )}

      <label style={{ display: "grid", gap: 4 }}>
        <span style={{ color: "var(--ink-dim)", fontSize: "0.85rem" }}>Quem pagou (opcional)</span>
        <input placeholder="ex.: Carlos" value={payerName} onChange={(e) => setPayerName(e.target.value)} />
      </label>

      <div style={{ display: "grid", gap: 8, gridTemplateColumns: "1.3fr 1fr" }}>
        <label style={{ display: "grid", gap: 4 }}>
          <span style={{ color: "var(--ink-dim)", fontSize: "0.85rem" }}>Forma de pagamento</span>
          <select value={methodId} onChange={(e) => setMethodId(Number(e.target.value))}>
            {Object.entries(paymentMethodLabel).map(([id, label]) => (
              <option key={id} value={id}>{label}</option>
            ))}
          </select>
        </label>
        <label style={{ display: "grid", gap: 4 }}>
          <span style={{ color: "var(--ink-dim)", fontSize: "0.85rem" }}>Valor (R$)</span>
          <input inputMode="decimal" placeholder="65,55" value={amount} onChange={(e) => setAmount(e.target.value)} />
        </label>
      </div>

      {needsReceipt(methodId) && (
        <input
          placeholder="Comprovante / autorização"
          value={authorizationCode}
          onChange={(e) => setAuthorizationCode(e.target.value)}
        />
      )}

      {value !== null && value > remaining && (
        <p className="error-text">O valor excede o restante da conta ({formatBRL(remaining)}).</p>
      )}
      {error && <p className="error-text">{error}</p>}

      <button
        className="btn-primary"
        disabled={value === null || value > remaining || noSession || sessionQuery.isLoading || mutation.isPending}
        onClick={() => mutation.mutate()}
      >
        {mutation.isPending ? "Registrando…" : "Registrar pagamento parcial"}
      </button>
    </Overlay>
  );
}
