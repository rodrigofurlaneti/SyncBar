import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  closeCashSession,
  getCashSummary,
  getOpenSession,
  openCashSession,
  registerCashMovement,
} from "./api";
import { useAuthStore } from "../../stores/authStore";
import { getPrintSettings, printCashClosing } from "../printing/api";
import { ApiError } from "../../lib/apiClient";
import {
  CashMovementType,
  DEFAULT_CASH_REGISTER_ID,
  formatBRL,
  paymentMethodLabel,
} from "../../lib/types";
import type { CloseCashSessionResponse } from "../../lib/types";
import { Overlay } from "../orders/Overlay";

interface Props {
  onClose: () => void;
}

const parseAmount = (raw: string): number => {
  const value = Number(raw.replace(",", "."));
  return Number.isFinite(value) ? value : 0;
};

export function CashDrawer({ onClose }: Props) {
  const queryClient = useQueryClient();
  const { employeeId } = useAuthStore();
  const [openingAmount, setOpeningAmount] = useState("");
  const [movementType, setMovementType] = useState<number>(CashMovementType.Suprimento);
  const [movementAmount, setMovementAmount] = useState("");
  const [movementDescription, setMovementDescription] = useState("");
  const [countedAmount, setCountedAmount] = useState("");
  const [closeResult, setCloseResult] = useState<CloseCashSessionResponse | null>(null);
  const [error, setError] = useState<string | null>(null);

  const printSettingsQuery = useQuery({
    queryKey: ["printing", "settings", DEFAULT_CASH_REGISTER_ID],
    queryFn: () => getPrintSettings(useAuthStore.getState().branchId),
    staleTime: 60_000,
  });

  const printClosingMutation = useMutation({
    mutationFn: (sessionIdToPrint: number) => printCashClosing(sessionIdToPrint),
    onError: (e) => onApiError(e, "Falha ao imprimir o fechamento."),
  });

  const sessionQuery = useQuery({
    queryKey: ["cash", "open", DEFAULT_CASH_REGISTER_ID],
    queryFn: () => getOpenSession(DEFAULT_CASH_REGISTER_ID),
    retry: false,
  });

  const sessionId = sessionQuery.data?.id;

  const summaryQuery = useQuery({
    queryKey: ["cash", "summary", sessionId],
    queryFn: () => getCashSummary(sessionId!),
    enabled: sessionId !== undefined,
    refetchInterval: 20_000,
  });

  const noSession =
    sessionQuery.isError &&
    sessionQuery.error instanceof ApiError &&
    sessionQuery.error.status === 404;

  const invalidateCash = () => void queryClient.invalidateQueries({ queryKey: ["cash"] });

  const onApiError = (e: unknown, fallback: string) =>
    setError(e instanceof ApiError ? e.message : fallback);

  const openMutation = useMutation({
    mutationFn: () =>
      openCashSession(DEFAULT_CASH_REGISTER_ID, employeeId ?? 1, parseAmount(openingAmount)),
    onSuccess: () => {
      setError(null);
      invalidateCash();
    },
    onError: (e) => onApiError(e, "Falha ao abrir o caixa."),
  });

  const movementMutation = useMutation({
    mutationFn: () =>
      registerCashMovement(
        sessionId!,
        movementType,
        employeeId ?? 1,
        parseAmount(movementAmount),
        movementDescription.trim() === "" ? null : movementDescription.trim(),
      ),
    onSuccess: () => {
      setError(null);
      setMovementAmount("");
      setMovementDescription("");
      invalidateCash();
    },
    onError: (e) => onApiError(e, "Falha ao registrar movimento."),
  });

  const closeMutation = useMutation({
    mutationFn: () => closeCashSession(sessionId!, employeeId ?? 1, parseAmount(countedAmount)),
    onSuccess: (result) => {
      setError(null);
      setCloseResult(result);
      invalidateCash();
    },
    onError: (e) => onApiError(e, "Falha ao fechar o caixa."),
  });

  const summary = summaryQuery.data;

  return (
    <Overlay title="Caixa 01" onClose={onClose} wide>
      {sessionQuery.isLoading && <p style={{ color: "var(--ink-dim)" }}>Carregando…</p>}

      {closeResult && (
        <div className="ticket" style={{ padding: 18, display: "grid", gap: 8 }}>
          <div className="display" style={{ fontSize: "1.3rem" }}>Caixa fechado</div>
          <div style={{ display: "flex", justifyContent: "space-between", color: "var(--ink-dim)" }}>
            <span>Esperado em dinheiro</span>
            <span className="mono-num">{formatBRL(closeResult.expectedAmount)}</span>
          </div>
          <div style={{ display: "flex", justifyContent: "space-between", color: "var(--ink-dim)" }}>
            <span>Contado</span>
            <span className="mono-num">{formatBRL(closeResult.closingAmount)}</span>
          </div>
          <div
            style={{
              display: "flex",
              justifyContent: "space-between",
              fontWeight: 700,
              color:
                closeResult.differenceAmount === 0
                  ? "var(--ok)"
                  : closeResult.differenceAmount > 0
                    ? "var(--amber)"
                    : "var(--danger)",
            }}
          >
            <span>
              {closeResult.differenceAmount === 0
                ? "Conferido — sem diferença"
                : closeResult.differenceAmount > 0
                  ? "Sobra"
                  : "Falta"}
            </span>
            <span className="mono-num">{formatBRL(Math.abs(closeResult.differenceAmount))}</span>
          </div>
          {printSettingsQuery.data?.printBillsEnabled && (
            <button
              className="btn-primary"
              disabled={printClosingMutation.isPending}
              onClick={() => printClosingMutation.mutate(closeResult.cashSessionId)}
            >
              {printClosingMutation.isPending ? "Imprimindo…" : "🖨 Imprimir fechamento"}
            </button>
          )}
        </div>
      )}

      {!closeResult && noSession && (
        <div style={{ display: "grid", gap: 12 }}>
          <p style={{ color: "var(--ink-dim)" }}>Nenhuma sessão aberta neste caixa.</p>
          <input
            placeholder="Fundo de troco (R$)"
            inputMode="decimal"
            value={openingAmount}
            onChange={(e) => setOpeningAmount(e.target.value)}
          />
          {error && <p className="error-text">{error}</p>}
          <button
            className="btn-primary"
            disabled={openMutation.isPending}
            onClick={() => openMutation.mutate()}
          >
            Abrir caixa
          </button>
        </div>
      )}

      {!closeResult && sessionId !== undefined && (
        <>
          <div className="ticket">
            <div className="ticket-head">
              <span className="display" style={{ fontSize: "1.2rem" }}>Resumo da sessão</span>
              <span className="mono-num" style={{ color: "var(--ink-faint)", fontSize: "0.85rem" }}>
                #{sessionId}
              </span>
            </div>
            {summary && (
              <>
                <div className="ticket-row" style={{ color: "var(--ink-dim)" }}>
                  <span>Fundo de troco</span>
                  <span className="mono-num">{formatBRL(summary.openingAmount)}</span>
                </div>
                <div className="ticket-row" style={{ color: "var(--ink-dim)" }}>
                  <span>Vendas ({summary.salesCount})</span>
                  <span className="mono-num">{formatBRL(summary.salesTotal)}</span>
                </div>
                {summary.paymentTotals.map((total) => (
                  <div className="ticket-row" key={total.paymentMethodId}>
                    <span>{paymentMethodLabel[total.paymentMethodId] ?? "Outros"}</span>
                    <span className="mono-num">{formatBRL(total.totalAmount)}</span>
                  </div>
                ))}
                {summary.partialPaymentsTotal > 0 && (
                  <div className="ticket-row" style={{ color: "var(--ok)" }}>
                    <span>Pagamentos parciais (mesas abertas)</span>
                    <span className="mono-num">+ {formatBRL(summary.partialPaymentsTotal)}</span>
                  </div>
                )}
                {summary.suprimentoTotal > 0 && (
                  <div className="ticket-row" style={{ color: "var(--ok)" }}>
                    <span>Suprimentos</span>
                    <span className="mono-num">+ {formatBRL(summary.suprimentoTotal)}</span>
                  </div>
                )}
                {summary.sangriaTotal > 0 && (
                  <div className="ticket-row" style={{ color: "var(--closing)" }}>
                    <span>Sangrias</span>
                    <span className="mono-num">− {formatBRL(summary.sangriaTotal)}</span>
                  </div>
                )}
                {summary.despesaTotal > 0 && (
                  <div className="ticket-row" style={{ color: "var(--closing)" }}>
                    <span>Despesas</span>
                    <span className="mono-num">− {formatBRL(summary.despesaTotal)}</span>
                  </div>
                )}
                <div className="ticket-total">
                  <span>Esperado em dinheiro</span>
                  <span className="mono-num" style={{ color: "var(--amber)" }}>
                    {formatBRL(summary.expectedCashAmount)}
                  </span>
                </div>
              </>
            )}
          </div>

          <div style={{ display: "grid", gap: 8 }}>
            <div className="display" style={{ fontSize: "1.1rem" }}>Sangria / Suprimento</div>
            <div style={{ display: "grid", gap: 8, gridTemplateColumns: "1fr 1fr" }}>
              <select value={movementType} onChange={(e) => setMovementType(Number(e.target.value))}>
                <option value={CashMovementType.Suprimento}>Suprimento (entrada)</option>
                <option value={CashMovementType.Sangria}>Sangria (retirada)</option>
                <option value={CashMovementType.Despesa}>Despesa</option>
              </select>
              <input
                placeholder="Valor (R$)"
                inputMode="decimal"
                value={movementAmount}
                onChange={(e) => setMovementAmount(e.target.value)}
              />
            </div>
            <input
              placeholder="Descrição (opcional)"
              value={movementDescription}
              onChange={(e) => setMovementDescription(e.target.value)}
            />
            <button
              className="btn-ghost"
              disabled={parseAmount(movementAmount) <= 0 || movementMutation.isPending}
              onClick={() => movementMutation.mutate()}
            >
              Registrar movimento
            </button>
          </div>

          <div style={{ display: "grid", gap: 8 }}>
            <div className="display" style={{ fontSize: "1.1rem" }}>Fechar caixa</div>
            <input
              placeholder="Dinheiro contado na gaveta (R$)"
              inputMode="decimal"
              value={countedAmount}
              onChange={(e) => setCountedAmount(e.target.value)}
            />
            {error && <p className="error-text">{error}</p>}
            <button
              className="btn-danger"
              disabled={countedAmount.trim() === "" || closeMutation.isPending}
              onClick={() => {
                if (window.confirm("Fechar o caixa? Pedidos aguardando pagamento não poderão ser recebidos."))
                  closeMutation.mutate();
              }}
            >
              {closeMutation.isPending ? "Fechando…" : "Fechar caixa e conferir"}
            </button>
          </div>
        </>
      )}
    </Overlay>
  );
}
