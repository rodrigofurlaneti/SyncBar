import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useDialog } from "../../ui/Dialog";
import {
  closeCashSession,
  getCashSummary,
  getOpenSession,
  openCashSession,
  registerCashMovement,
} from "./api";
import { useAuthStore } from "../../stores/authStore";
import { getPrintSettings, printCashClosing } from "../printing/api";
import { getSalesBySession, refundSale } from "../billing/api";
import { useMyFeatures } from "../access/hooks";
import { ApiError } from "../../lib/apiClient";
import {
  CashMovementType,
  DEFAULT_CASH_REGISTER_ID,
  PaymentMethod,
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

// Conferência de fechamento por modalidade eletrônica — só exibição/comparação no front-end.
// O único valor de fato enviado ao backend em Fechar caixa continua sendo o Dinheiro contado
// (é o que a tabela CashSession grava hoje); Crédito/Débito/Pix aqui ajudam o operador a bater
// o que a maquininha/o extrato Pix mostrou contra o que o sistema já espera, sem exigir mudança
// de schema para isso.
const RECONCILE_METHODS = [PaymentMethod.CartaoCredito, PaymentMethod.CartaoDebito, PaymentMethod.Pix];

interface DifferenceState {
  label: string;
  color: string;
}

const getDifferenceState = (differenceAmount: number): DifferenceState => {
  if (differenceAmount === 0) return { label: "Conferido — sem diferença", color: "var(--ok)" };
  if (differenceAmount > 0) return { label: "Sobra", color: "var(--amber)" };
  return { label: "Falta", color: "var(--danger)" };
};

export function CashDrawer({ onClose }: Props) {
  const queryClient = useQueryClient();
  const dialog = useDialog();
  const { employeeId } = useAuthStore();
  const [openingAmount, setOpeningAmount] = useState("");
  const [movementType, setMovementType] = useState<number>(CashMovementType.Suprimento);
  const [movementAmount, setMovementAmount] = useState("");
  const [movementDescription, setMovementDescription] = useState("");
  const [countedAmount, setCountedAmount] = useState("");
  const [cardCounts, setCardCounts] = useState<Record<number, string>>({});
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
  const featuresQuery = useMyFeatures();

  const salesQuery = useQuery({
    queryKey: ["cash", "sales", sessionId],
    queryFn: () => getSalesBySession(sessionId!),
    enabled: sessionId !== undefined,
    refetchInterval: 30_000,
  });

  const refundMutation = useMutation({
    mutationFn: ({ saleId, reason }: { saleId: number; reason: string | null }) =>
      refundSale(saleId, useAuthStore.getState().employeeId ?? 1, reason),
    onSuccess: () => {
      setError(null);
      invalidateCash();
      void queryClient.invalidateQueries({ queryKey: ["orders"] });
      void queryClient.invalidateQueries({ queryKey: ["tables"] });
    },
    onError: (e) => onApiError(e, "Falha ao estornar a venda."),
  });

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
  const differenceState = getDifferenceState(closeResult?.differenceAmount ?? 0);

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
              color: differenceState.color,
            }}
          >
            <span>{differenceState.label}</span>
            <span className="mono-num">{formatBRL(Math.abs(closeResult.differenceAmount))}</span>
          </div>
          {printSettingsQuery.data?.printBillsEnabled && (
            <button
              type="button"
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
            type="button"
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

          {(salesQuery.data ?? []).length > 0 && (
            <div className="ticket">
              <div className="ticket-head">
                <span className="display" style={{ fontSize: "1.1rem" }}>Vendas da sessão</span>
              </div>
              {(salesQuery.data ?? []).map((sale) => (
                <div className="ticket-row" key={sale.id}>
                  <div style={{ display: "grid", gap: 2 }}>
                    <span className="mono-num">
                      Venda #{sale.saleNumber} · pedido #{sale.customerOrderId} · {formatBRL(sale.totalAmount)}
                    </span>
                    <span style={{ fontSize: "0.78rem", color: "var(--ink-faint)" }}>
                      {new Date(sale.soldAt).toLocaleTimeString("pt-BR", { hour: "2-digit", minute: "2-digit" })}
                    </span>
                  </div>
                  {featuresQuery.data?.canManageAccess && (
                    <button
                      type="button"
                      className="btn-danger"
                      style={{ minHeight: 44, padding: "0 10px", fontSize: "0.82rem" }}
                      disabled={refundMutation.isPending}
                      onClick={async () => {
                        const reason = await dialog.prompt({
                          title: "Estornar venda",
                          message: `Estornar a venda #${sale.saleNumber} (${formatBRL(sale.totalAmount)})?`,
                          label: "Motivo (opcional)",
                          confirmLabel: "Estornar",
                        });
                        if (reason !== null)
                          refundMutation.mutate({ saleId: sale.id, reason: reason.trim() === "" ? null : reason.trim() });
                      }}
                    >
                      Estornar
                    </button>
                  )}
                </div>
              ))}
            </div>
          )}

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
              type="button"
              className="btn-ghost"
              disabled={parseAmount(movementAmount) <= 0 || movementMutation.isPending}
              onClick={() => movementMutation.mutate()}
            >
              Registrar movimento
            </button>
          </div>

          <div style={{ display: "grid", gap: 8 }}>
            <div className="display" style={{ fontSize: "1.1rem" }}>Fechar caixa</div>

            {summary && (
              <div className="ticket">
                <div className="ticket-head">
                  <span style={{ fontSize: "0.85rem", color: "var(--ink-dim)" }}>
                    Conferência por forma de pagamento
                  </span>
                </div>
                {RECONCILE_METHODS.map((methodId) => {
                  const expected =
                    summary.paymentTotals.find((t) => t.paymentMethodId === methodId)?.totalAmount ?? 0;
                  const countedRaw = cardCounts[methodId] ?? "";
                  const hasCounted = countedRaw.trim() !== "";
                  const diff = parseAmount(countedRaw) - expected;
                  return (
                    <div
                      className="ticket-row"
                      key={methodId}
                      style={{ flexDirection: "column", alignItems: "stretch", gap: 6 }}
                    >
                      <div style={{ display: "flex", justifyContent: "space-between", width: "100%" }}>
                        <span>{paymentMethodLabel[methodId]}</span>
                        <span className="mono-num" style={{ color: "var(--ink-dim)" }}>
                          Esperado {formatBRL(expected)}
                        </span>
                      </div>
                      <div style={{ display: "flex", gap: 8, alignItems: "center" }}>
                        <input
                          placeholder="Conferido (R$)"
                          inputMode="decimal"
                          value={countedRaw}
                          onChange={(e) => setCardCounts((c) => ({ ...c, [methodId]: e.target.value }))}
                          style={{ flex: 1 }}
                        />
                        {hasCounted && (
                          <span
                            className="mono-num"
                            style={{
                              fontSize: "0.85rem",
                              minWidth: 96,
                              textAlign: "right",
                              color: diff === 0 ? "var(--ok)" : diff > 0 ? "var(--amber)" : "var(--danger)",
                            }}
                          >
                            {diff === 0 ? "Confere" : diff > 0 ? `+ ${formatBRL(diff)}` : `− ${formatBRL(Math.abs(diff))}`}
                          </span>
                        )}
                      </div>
                    </div>
                  );
                })}
                <p style={{ padding: "8px 18px 12px", margin: 0, fontSize: "0.78rem", color: "var(--ink-faint)" }}>
                  Só o Dinheiro abaixo é enviado ao fechar o caixa — Crédito/Débito/Pix aqui são conferência
                  visual (o valor esperado já vem das vendas da sessão) e não alteram o fechamento.
                </p>
              </div>
            )}

            <input
              placeholder="Dinheiro contado na gaveta (R$)"
              inputMode="decimal"
              value={countedAmount}
              onChange={(e) => setCountedAmount(e.target.value)}
            />
            {error && <p className="error-text">{error}</p>}
            <button
              type="button"
              className="btn-danger"
              disabled={countedAmount.trim() === "" || closeMutation.isPending}
              onClick={async () => {
                if (
                  await dialog.confirm({
                    title: "Fechar caixa",
                    message: "Fechar o caixa? Pedidos aguardando pagamento não poderão ser recebidos.",
                    confirmLabel: "Fechar caixa",
                    danger: true,
                  })
                )
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
