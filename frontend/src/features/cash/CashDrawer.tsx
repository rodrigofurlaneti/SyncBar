import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import Swal from "sweetalert2"; // Adicionado SweetAlert2
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
import { api, ApiError } from "../../lib/apiClient";
import {
    CashMovementType,
    DEFAULT_CASH_REGISTER_ID,
    PaymentMethod,
    formatBRL,
    paymentMethodLabel,
} from "../../lib/types";
import type { CloseCashSessionResponse, PaymentMethodReconciliationResponse } from "../../lib/types";
import { Overlay } from "../orders/Overlay";
import { useCashRegister } from "./useCashRegister";

interface Props {
    onClose: () => void;
}

const parseAmount = (raw: string): number => {
    return Number(raw.replace(",", "."));
};
const validAmount = (raw: string) => /^\d+(?:[,.]\d{1,2})?$/.test(raw.trim()) && Number.isFinite(parseAmount(raw));

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
    const cashRegister = useCashRegister();
    const registerId = cashRegister.registerId;
    const queryClient = useQueryClient();
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
        queryKey: ["cash", "open", registerId],
        queryFn: () => getOpenSession(registerId!),
        enabled: !!registerId,
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
        setError(e instanceof Error ? e.message : fallback);

    const openMutation = useMutation({
        mutationFn: () => {
            if (!registerId || !validAmount(openingAmount)) throw new Error("Informe um valor de abertura válido.");
            return openCashSession(registerId, employeeId ?? 1, parseAmount(openingAmount));
        },
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
        mutationFn: () => {
            if (!canClose) throw new Error("Confira os valores de todas as modalidades antes de fechar.");
            // Só envia a modalidade se o operador de fato digitou algo — um campo em branco não
            // vira "conferido = 0" (que soaria como "recebemos zero", quando é só "não conferimos ainda").
            const paymentMethodCounts = RECONCILE_METHODS
                .filter((methodId) => (cardCounts[methodId] ?? "").trim() !== "")
                .map((methodId) => ({ paymentMethodId: methodId, countedAmount: parseAmount(cardCounts[methodId]) }));

            return closeCashSession(sessionId!, employeeId ?? 1, parseAmount(countedAmount), paymentMethodCounts);
        },
        onSuccess: (result) => {
            setError(null);
            setCloseResult(result);
            invalidateCash();
        },
        onError: (e) => onApiError(e, "Falha ao fechar o caixa."),
    });

    const summary = summaryQuery.data;
    const canClose = !!summary && !summaryQuery.isError && !salesQuery.isError && validAmount(countedAmount)
        && RECONCILE_METHODS.every(id => {
            const raw = cardCounts[id] ?? "";
            const expected = summary.paymentTotals.find(p => p.paymentMethodId === id)?.totalAmount ?? 0;
            return raw.trim() === "" ? expected === 0 : validAmount(raw);
        });
    const expectedTotal = (summary?.expectedCashAmount ?? 0) + RECONCILE_METHODS.reduce((sum, id) => sum + (summary?.paymentTotals.find(p => p.paymentMethodId === id)?.totalAmount ?? 0), 0);
    const countedTotal = parseAmount(countedAmount || "0") + RECONCILE_METHODS.reduce((sum, id) => sum + parseAmount(cardCounts[id] || "0"), 0);
    const differenceState = getDifferenceState(closeResult?.differenceAmount ?? 0);

    return (
        <Overlay title={cashRegister.registerName ?? "Caixa"} onClose={onClose} wide data-testid="cash-drawer-overlay">
            <label>Terminal
                <select value={registerId ?? ""} onChange={e => { cashRegister.selectRegister(Number(e.target.value)); setCloseResult(null); setCardCounts({}); setCountedAmount(""); }} disabled={openMutation.isPending || closeMutation.isPending || movementMutation.isPending}>
                    {cashRegister.registers.map(r => <option key={r.id} value={r.id}>{r.name}</option>)}
                </select>
            </label>
            {featuresQuery.data?.canManageAccess && <button type="button" className="btn-ghost" onClick={async () => {
                const answer = await Swal.fire({ title: "Novo terminal", input: "text", inputLabel: "Nome do caixa", showCancelButton: true, confirmButtonText: "Cadastrar", cancelButtonText: "Cancelar", inputValidator: value => !value.trim() ? "Informe o nome do terminal." : undefined });
                if (!answer.isConfirmed) return;
                try {
                    const id = await api<number>("/api/cash/registers", { method: "POST", body: JSON.stringify({ branchId: useAuthStore.getState().branchId, name: answer.value }) });
                    cashRegister.selectRegister(id);
                    await cashRegister.refetch();
                } catch (e) { onApiError(e, "Não foi possível cadastrar o terminal."); }
            }}>Cadastrar terminal</button>}
            {(cashRegister.isError || (sessionQuery.isError && !noSession) || summaryQuery.isError || salesQuery.isError) && <p role="alert" className="error-text">Não foi possível carregar os dados do caixa. <button type="button" onClick={() => { void cashRegister.refetch(); invalidateCash(); }}>Tentar novamente</button></p>}
            {cashRegister.isSuccess && !registerId && <p>Nenhum terminal cadastrado nesta filial.</p>}
            {summary?.movements && <section aria-label="Extrato da sessão" className="ticket">
                <h3>Movimentações da sessão</h3>
                {summary.movements.length === 0 ? <p>Nenhuma movimentação registrada.</p> : <div style={{ overflowX: "auto" }}><table><thead><tr><th>Data</th><th>Tipo</th><th>Descrição</th><th>Valor</th></tr></thead><tbody>{summary.movements.map(m => <tr key={m.id}><td>{new Date(m.createdAt).toLocaleString("pt-BR")}</td><td>{({1:"Suprimento",2:"Sangria",3:"Venda",4:"Estorno",5:"Despesa"} as Record<number,string>)[m.cashMovementTypeId] ?? "Movimento"}</td><td>{m.description ?? "—"}</td><td>{formatBRL(m.amount)}</td></tr>)}</tbody></table></div>}
            </section>}
            {sessionQuery.isLoading && <p style={{ color: "var(--ink-dim)" }} data-testid="loading-text">Carregando…</p>}

            {closeResult && (
                <div className="ticket" style={{ padding: 18, display: "grid", gap: 8 }} data-testid="close-result-view">
                    <div className="display" style={{ fontSize: "1.3rem" }}>Caixa fechado</div>
                    <div style={{ display: "flex", justifyContent: "space-between", color: "var(--ink-dim)" }}>
                        <span>Dinheiro — esperado</span>
                        <span className="mono-num">{formatBRL(closeResult.expectedAmount)}</span>
                    </div>
                    <div style={{ display: "flex", justifyContent: "space-between", color: "var(--ink-dim)" }}>
                        <span>Dinheiro — contado</span>
                        <span className="mono-num">{formatBRL(closeResult.closingAmount)}</span>
                    </div>
                    <div style={{ display: "flex", justifyContent: "space-between", fontWeight: 700, color: differenceState.color }}>
                        <span>{differenceState.label} (dinheiro)</span>
                        <span className="mono-num">{formatBRL(Math.abs(closeResult.differenceAmount))}</span>
                    </div>

                    {closeResult.paymentReconciliations.length > 0 && (
                        <>
                            <div style={{ borderTop: "1px solid var(--line-soft)", margin: "6px 0" }} />
                            {closeResult.paymentReconciliations.map((reconciliation: PaymentMethodReconciliationResponse) => {
                                const state = getDifferenceState(reconciliation.differenceAmount);
                                return (
                                    <div
                                        key={reconciliation.paymentMethodId}
                                        style={{ display: "flex", justifyContent: "space-between", color: state.color }}
                                        data-testid={`close-reconciliation-${reconciliation.paymentMethodId}`}
                                    >
                                        <span>
                                            {paymentMethodLabel[reconciliation.paymentMethodId] ?? "Outros"} — {state.label} (esperado{" "}
                                            {formatBRL(reconciliation.expectedAmount)}, conferido {formatBRL(reconciliation.countedAmount)})
                                        </span>
                                        <span className="mono-num">{formatBRL(Math.abs(reconciliation.differenceAmount))}</span>
                                    </div>
                                );
                            })}
                        </>
                    )}

                    <div className="ticket-total" data-testid="close-total-difference">
                        <span>Quebra de caixa — total geral</span>
                        <span className="mono-num" style={{ color: getDifferenceState(closeResult.totalDifferenceAmount).color }}>
                            {formatBRL(Math.abs(closeResult.totalDifferenceAmount))}
                        </span>
                    </div>

                    {printSettingsQuery.data?.printBillsEnabled && (
                        <button
                            type="button"
                            className="btn-primary"
                            disabled={printClosingMutation.isPending}
                            onClick={() => printClosingMutation.mutate(closeResult.cashSessionId)}
                            data-testid="print-closing-btn"
                        >
                            {printClosingMutation.isPending ? "Imprimindo…" : "🖨 Imprimir fechamento"}
                        </button>
                    )}
                </div>
            )}

            {!closeResult && noSession && (
                <div style={{ display: "grid", gap: 12 }} data-testid="open-session-view">
                    <p style={{ color: "var(--ink-dim)" }}>Nenhuma sessão aberta neste caixa.</p>
                    <input
                        placeholder="Fundo de troco (R$)"
                        inputMode="decimal"
                        value={openingAmount}
                        onChange={(e) => setOpeningAmount(e.target.value)}
                        data-testid="opening-amount-input"
                    />
                    {error && <p className="error-text" data-testid="error-message">{error}</p>}
                    <button
                        type="button"
                        className="btn-primary"
                        disabled={openMutation.isPending || !registerId || !validAmount(openingAmount)}
                        onClick={() => openMutation.mutate()}
                        data-testid="open-cash-btn"
                    >
                        Abrir caixa
                    </button>
                </div>
            )}

            {!closeResult && sessionId !== undefined && (
                <>
                    <div className="ticket" data-testid="session-summary-view">
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
                        <div className="ticket" data-testid="session-sales-view">
                            <div className="ticket-head">
                                <span className="display" style={{ fontSize: "1.1rem" }}>Vendas da sessão</span>
                            </div>
                            {(salesQuery.data ?? []).map((sale) => (
                                <div className="ticket-row" key={sale.id} data-testid={`sale-row-${sale.id}`}>
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
                                            data-testid={`refund-btn-${sale.id}`}
                                            onClick={async () => {
                                                const { value: reason, isConfirmed } = await Swal.fire({
                                                    title: "Estornar venda",
                                                    text: `Estornar a venda #${sale.saleNumber} (${formatBRL(sale.totalAmount)})?`,
                                                    input: "text",
                                                    inputLabel: "Motivo (opcional)",
                                                    showCancelButton: true,
                                                    confirmButtonText: "Estornar",
                                                    cancelButtonText: "Cancelar"
                                                });

                                                if (isConfirmed) {
                                                    refundMutation.mutate({
                                                        saleId: sale.id,
                                                        reason: reason?.trim() === "" ? null : reason?.trim()
                                                    });
                                                }
                                            }}
                                        >
                                            Estornar
                                        </button>
                                    )}
                                </div>
                            ))}
                        </div>
                    )}

                    <div style={{ display: "grid", gap: 8 }} data-testid="movement-view">
                        <div className="display" style={{ fontSize: "1.1rem" }}>Sangria / Suprimento</div>
                        <div style={{ display: "grid", gap: 8, gridTemplateColumns: "1fr 1fr" }}>
                            <select
                                value={movementType}
                                onChange={(e) => setMovementType(Number(e.target.value))}
                                data-testid="movement-type-select"
                            >
                                <option value={CashMovementType.Suprimento}>Suprimento (entrada)</option>
                                <option value={CashMovementType.Sangria}>Sangria (retirada)</option>
                                <option value={CashMovementType.Despesa}>Despesa</option>
                            </select>
                            <input
                                placeholder="Valor (R$)"
                                inputMode="decimal"
                                value={movementAmount}
                                onChange={(e) => setMovementAmount(e.target.value)}
                                data-testid="movement-amount-input"
                            />
                        </div>
                        <input
                            placeholder="Descrição (opcional)"
                            value={movementDescription}
                            onChange={(e) => setMovementDescription(e.target.value)}
                            data-testid="movement-description-input"
                        />
                        <button
                            type="button"
                            className="btn-ghost"
                            disabled={!validAmount(movementAmount) || parseAmount(movementAmount) <= 0 || movementDescription.trim() === "" || movementMutation.isPending}
                            onClick={() => movementMutation.mutate()}
                            data-testid="register-movement-btn"
                        >
                            Registrar movimento
                        </button>
                    </div>

                    <div style={{ display: "grid", gap: 8 }} data-testid="close-session-view">
                        <div className="display" style={{ fontSize: "1.1rem" }}>Fechar caixa</div><p>Confira dinheiro e todas as modalidades com recebimentos.</p><p data-testid="conference-total">Total esperado: {formatBRL(expectedTotal)} · Conferido: {Number.isFinite(countedTotal) ? formatBRL(countedTotal) : "Valor inválido"} · Diferença: {Number.isFinite(countedTotal) ? formatBRL(Math.round((countedTotal - expectedTotal) * 100) / 100) : "—"}</p>

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
                                                    style={{ flex: 1, minWidth: 0 }}
                                                    data-testid={`conference-input-${methodId}`}
                                                />
                                                {hasCounted && (
                                                    <span
                                                        className="mono-num"
                                                        data-testid={`conference-diff-${methodId}`}
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
                            </div>
                        )}

                        <input
                            placeholder="Dinheiro contado na gaveta (R$)"
                            inputMode="decimal"
                            value={countedAmount}
                            onChange={(e) => setCountedAmount(e.target.value)}
                            data-testid="counted-amount-input"
                        />
                        {error && <p className="error-text" data-testid="error-message">{error}</p>}
                        <button
                            type="button"
                            className="btn-danger"
                            disabled={!canClose || closeMutation.isPending}
                            data-testid="close-cash-btn"
                            onClick={async () => {
                                const { isConfirmed } = await Swal.fire({
                                    title: "Fechar caixa",
                                    text: "Fechar o caixa? Pedidos aguardando pagamento não poderão ser recebidos.",
                                    icon: "warning",
                                    showCancelButton: true,
                                    confirmButtonColor: "#d33",
                                    confirmButtonText: "Fechar caixa",
                                    cancelButtonText: "Cancelar"
                                });

                                if (isConfirmed) {
                                    closeMutation.mutate();
                                }
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

