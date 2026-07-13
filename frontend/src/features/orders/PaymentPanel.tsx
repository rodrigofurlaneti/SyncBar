import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { getOpenSession, openCashSession } from "../cash/api";
import { registerSale, type SalePaymentInput } from "../billing/api";
import { useAuthStore } from "../../stores/authStore";
import { ApiError } from "../../lib/apiClient";
import {
  DEFAULT_CASH_REGISTER_ID,
  PaymentMethod,
  formatBRL,
  paymentMethodLabel,
} from "../../lib/types";
import type { OrderResponse } from "../../lib/types";

interface PaymentRow {
  paymentMethodId: number;
  amount: string;
  authorizationCode: string;
}

interface Props {
  order: OrderResponse;
  onPaid: () => void;
}

const needsReceipt = (methodId: number): boolean =>
  methodId === PaymentMethod.CartaoCredito ||
  methodId === PaymentMethod.CartaoDebito ||
  methodId === PaymentMethod.Pix;

const parseAmount = (raw: string): number => {
  const value = Number(raw.replace(",", "."));
  return Number.isFinite(value) ? value : 0;
};

export function PaymentPanel({ order, onPaid }: Props) {
  const queryClient = useQueryClient();
  const { employeeId } = useAuthStore();
  const [rows, setRows] = useState<PaymentRow[]>([
    { paymentMethodId: PaymentMethod.Dinheiro, amount: "", authorizationCode: "" },
  ]);
  const [openingAmount, setOpeningAmount] = useState("");
  const [error, setError] = useState<string | null>(null);

  const sessionQuery = useQuery({
    queryKey: ["cash", "open", DEFAULT_CASH_REGISTER_ID],
    queryFn: () => getOpenSession(DEFAULT_CASH_REGISTER_ID),
    retry: false,
  });

  const noSession =
    sessionQuery.isError &&
    sessionQuery.error instanceof ApiError &&
    sessionQuery.error.status === 404;

  const noCashAccess =
    sessionQuery.isError &&
    sessionQuery.error instanceof ApiError &&
    sessionQuery.error.status === 403;

  const openSessionMutation = useMutation({
    mutationFn: () =>
      openCashSession(DEFAULT_CASH_REGISTER_ID, employeeId ?? 1, parseAmount(openingAmount)),
    onSuccess: () => void queryClient.invalidateQueries({ queryKey: ["cash"] }),
    onError: (e) => setError(e instanceof ApiError ? e.message : "Falha ao abrir o caixa."),
  });

  const amountDue = order.totalAmount - order.partialPaidAmount;
  const totalPaid = rows.reduce((sum, row) => sum + parseAmount(row.amount), 0);
  const cashPaid = rows
    .filter((row) => row.paymentMethodId === PaymentMethod.Dinheiro)
    .reduce((sum, row) => sum + parseAmount(row.amount), 0);
  const change = Math.max(0, Number((totalPaid - amountDue).toFixed(2)));
  const changeValid = change === 0 || cashPaid >= change;
  const canConfirm = totalPaid >= amountDue && changeValid && rows.every((r) => parseAmount(r.amount) > 0);

  const payMutation = useMutation({
    mutationFn: () => {
      // Troco é abatido do (primeiro) pagamento em dinheiro.
      let changeLeft = change;
      const payments: SalePaymentInput[] = rows.map((row) => {
        const amount = parseAmount(row.amount);
        let changeAmount: number | null = null;
        if (row.paymentMethodId === PaymentMethod.Dinheiro && changeLeft > 0) {
          changeAmount = Math.min(changeLeft, amount);
          changeLeft = Number((changeLeft - changeAmount).toFixed(2));
        }
        return {
          paymentMethodId: row.paymentMethodId,
          amount,
          changeAmount,
          authorizationCode: row.authorizationCode.trim() === "" ? null : row.authorizationCode.trim(),
        };
      });
      return registerSale(order.id, sessionQuery.data!.id, employeeId ?? 1, payments);
    },
    onSuccess: () => {
      setError(null);
      onPaid();
    },
    onError: (e) => setError(e instanceof ApiError ? e.message : "Falha ao registrar pagamento."),
  });

  const setRow = (index: number, patch: Partial<PaymentRow>) =>
    setRows((current) => current.map((row, i) => (i === index ? { ...row, ...patch } : row)));

  if (sessionQuery.isLoading)
    return <p style={{ color: "var(--ink-dim)" }}>Verificando caixa…</p>;

  if (noCashAccess)
    return (
      <div className="ticket" style={{ padding: 18, display: "grid", gap: 6 }}>
        <strong>Conta fechada — aguardando pagamento.</strong>
        <span style={{ color: "var(--ink-dim)", fontSize: "0.9rem" }}>
          Você não tem acesso ao caixa. Chame o operador de caixa ou o gerente para
          registrar o pagamento.
        </span>
      </div>
    );

  if (noSession)
    return (
      <div className="ticket" style={{ padding: 18, display: "grid", gap: 12 }}>
        <strong>O caixa está fechado.</strong>
        <span style={{ color: "var(--ink-dim)", fontSize: "0.9rem" }}>
          Abra uma sessão de caixa para receber pagamentos.
        </span>
        <input
          placeholder="Fundo de troco (R$)"
          inputMode="decimal"
          value={openingAmount}
          onChange={(e) => setOpeningAmount(e.target.value)}
        />
        {error && <p className="error-text">{error}</p>}
        <button
          className="btn-primary"
          disabled={openSessionMutation.isPending}
          onClick={() => openSessionMutation.mutate()}
        >
          Abrir caixa
        </button>
      </div>
    );

  return (
    <div style={{ display: "grid", gap: 12 }}>
      <div className="display" style={{ fontSize: "1.2rem" }}>
        Pagamento — {formatBRL(amountDue)}
      </div>
      {order.partialPaidAmount > 0 && (
        <p style={{ color: "var(--ink-dim)", fontSize: "0.85rem", margin: 0 }}>
          Conta de {formatBRL(order.totalAmount)} com{" "}
          <span style={{ color: "var(--ok)" }}>{formatBRL(order.partialPaidAmount)} já pagos parcialmente</span>.
        </p>
      )}

      {rows.map((row, index) => (
        <div key={index} style={{ display: "grid", gap: 8, gridTemplateColumns: "1.3fr 0.8fr auto" }}>
          <select
            value={row.paymentMethodId}
            onChange={(e) => setRow(index, { paymentMethodId: Number(e.target.value) })}
          >
            {Object.entries(paymentMethodLabel).map(([id, label]) => (
              <option key={id} value={id}>
                {label}
              </option>
            ))}
          </select>
          <input
            placeholder="Valor"
            inputMode="decimal"
            value={row.amount}
            onChange={(e) => setRow(index, { amount: e.target.value })}
          />
          <button
            className="btn-ghost"
            aria-label="Remover forma de pagamento"
            title="Remover forma de pagamento"
            style={{ minHeight: 44, padding: "0 12px" }}
            disabled={rows.length === 1}
            onClick={() => setRows((current) => current.filter((_, i) => i !== index))}
          >
            ✕
          </button>
          {needsReceipt(row.paymentMethodId) && (
            <input
              style={{ gridColumn: "1 / -1" }}
              placeholder="Comprovante / autorização (ex.: AUT-123456)"
              value={row.authorizationCode}
              onChange={(e) => setRow(index, { authorizationCode: e.target.value })}
            />
          )}
        </div>
      ))}

      <button
        className="btn-ghost"
        onClick={() =>
          setRows((current) => [
            ...current,
            { paymentMethodId: PaymentMethod.CartaoCredito, amount: "", authorizationCode: "" },
          ])
        }
      >
        + Adicionar forma de pagamento
      </button>

      <div className="ticket" style={{ padding: "12px 16px", display: "grid", gap: 4 }}>
        <div style={{ display: "flex", justifyContent: "space-between", color: "var(--ink-dim)" }}>
          <span>Pago</span>
          <span className="mono-num">{formatBRL(totalPaid)}</span>
        </div>
        <div style={{ display: "flex", justifyContent: "space-between", color: "var(--ink-dim)" }}>
          <span>Restante</span>
          <span className="mono-num">
            {formatBRL(Math.max(0, amountDue - totalPaid))}
          </span>
        </div>
        {change > 0 && (
          <div style={{ display: "flex", justifyContent: "space-between", color: "var(--amber)" }}>
            <span>Troco (dinheiro)</span>
            <span className="mono-num">{formatBRL(change)}</span>
          </div>
        )}
        {!changeValid && (
          <p className="error-text">O troco excede o valor pago em dinheiro.</p>
        )}
      </div>

      {error && <p className="error-text">{error}</p>}

      <button
        className="btn-primary"
        disabled={!canConfirm || payMutation.isPending}
        onClick={() => payMutation.mutate()}
      >
        {payMutation.isPending ? "Registrando…" : "Confirmar pagamento"}
      </button>
    </div>
  );
}
