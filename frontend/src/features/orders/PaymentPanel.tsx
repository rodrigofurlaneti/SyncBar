import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import Swal from "sweetalert2"; // Adicionado SweetAlert2
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

// Configuração do Toast do SweetAlert2
const Toast = Swal.mixin({
    toast: true,
    position: "top-end",
    showConfirmButton: false,
    timer: 3000,
    timerProgressBar: true,
});

export function PaymentPanel({ order, onPaid }: Props) {
    const queryClient = useQueryClient();
    const { employeeId } = useAuthStore();
    const [rows, setRows] = useState<PaymentRow[]>([
        { paymentMethodId: PaymentMethod.Dinheiro, amount: "", authorizationCode: "" },
    ]);
    const [openingAmount, setOpeningAmount] = useState("");
    const [error, setError] = useState<string | null>(null);
    const [splitCount, setSplitCount] = useState("2");

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
        onSuccess: () => {
            Toast.fire({ icon: "success", title: "Caixa aberto com sucesso." });
            void queryClient.invalidateQueries({ queryKey: ["cash"] });
        },
        onError: (e) => {
            const msg = e instanceof ApiError ? e.message : "Falha ao abrir o caixa.";
            setError(msg);
            Swal.fire("Erro", msg, "error");
        },
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
            Toast.fire({ icon: "success", title: "Pagamento registrado." });
            onPaid();
        },
        onError: (e) => {
            const msg = e instanceof ApiError ? e.message : "Falha ao registrar pagamento.";
            setError(msg);
            Swal.fire("Erro", msg, "error");
        },
    });

    const setRow = (index: number, patch: Partial<PaymentRow>) =>
        setRows((current) => current.map((row, i) => (i === index ? { ...row, ...patch } : row)));

    const applySplit = () => {
        const people = Math.max(1, Math.trunc(Number(splitCount) || 1));
        const totalCents = Math.round(amountDue * 100);
        const baseCents = Math.floor(totalCents / people);
        const remainder = totalCents % people;

        setRows(
            Array.from({ length: people }, (_, i) => ({
                paymentMethodId: PaymentMethod.Dinheiro,
                amount: ((baseCents + (i < remainder ? 1 : 0)) / 100).toFixed(2).replace(".", ","),
                authorizationCode: "",
            })),
        );
    };

    if (sessionQuery.isLoading)
        return <p style={{ color: "var(--ink-dim)" }}>Verificando caixa…</p>;

    if (noCashAccess)
        return (
            <div className="ticket" style={{ padding: 18, display: "grid", gap: 6 }} data-testid="no-cash-access-msg">
                <strong>Conta fechada — aguardando pagamento.</strong>
                <span style={{ color: "var(--ink-dim)", fontSize: "0.9rem" }}>
                    Você não tem acesso ao caixa. Chame o operador de caixa ou o gerente para
                    registrar o pagamento.
                </span>
            </div>
        );

    if (noSession)
        return (
            <div className="ticket" style={{ padding: 18, display: "grid", gap: 12 }} data-testid="no-session-panel">
                <strong>O caixa está fechado.</strong>
                <span style={{ color: "var(--ink-dim)", fontSize: "0.9rem" }}>
                    Abra uma sessão de caixa para receber pagamentos.
                </span>
                <input
                    placeholder="Fundo de troco (R$)"
                    inputMode="decimal"
                    value={openingAmount}
                    onChange={(e) => setOpeningAmount(e.target.value)}
                    data-testid="input-opening-amount"
                />
                {error && <p className="error-text" data-testid="opening-error-msg">{error}</p>}
                <button
                    type="button"
                    className="btn-primary"
                    disabled={openSessionMutation.isPending}
                    onClick={() => openSessionMutation.mutate()}
                    data-testid="btn-open-session"
                >
                    Abrir caixa
                </button>
            </div>
        );

    return (
        <div style={{ display: "grid", gap: 12 }} data-testid="payment-panel">
            <div className="display" style={{ fontSize: "1.2rem" }}>
                Pagamento — {formatBRL(amountDue)}
            </div>
            {order.partialPaidAmount > 0 && (
                <p style={{ color: "var(--ink-dim)", fontSize: "0.85rem", margin: 0 }}>
                    Conta de {formatBRL(order.totalAmount)} com{" "}
                    <span style={{ color: "var(--ok)" }}>{formatBRL(order.partialPaidAmount)} já pagos parcialmente</span>.
                </p>
            )}

            <div style={{ display: "grid", gap: 8, gridTemplateColumns: "1fr auto", alignItems: "center" }}>
                <input
                    placeholder="Dividir entre quantas pessoas?"
                    inputMode="numeric"
                    value={splitCount}
                    onChange={(e) => setSplitCount(e.target.value)}
                    data-testid="input-split-count"
                />
                <button className="btn-ghost" type="button" onClick={applySplit} data-testid="btn-split-bill">
                    Dividir conta
                </button>
            </div>

            {rows.map((row, index) => (
                <div key={index} className="ui-row ui-row-wrap" style={{ alignItems: "center" }} data-testid={`payment-row-${index}`}>
                    <select
                        style={{ flex: 2, minWidth: 170 }}
                        value={row.paymentMethodId}
                        onChange={(e) => setRow(index, { paymentMethodId: Number(e.target.value) })}
                        data-testid={`select-payment-method-${index}`}
                    >
                        {Object.entries(paymentMethodLabel).map(([id, label]) => (
                            <option key={id} value={id}>
                                {label}
                            </option>
                        ))}
                    </select>
                    <input
                        style={{ flex: 1, minWidth: 110 }}
                        placeholder="Valor"
                        inputMode="decimal"
                        value={row.amount}
                        onChange={(e) => setRow(index, { amount: e.target.value })}
                        data-testid={`input-payment-amount-${index}`}
                    />
                    <button
                        type="button"
                        className="btn-ghost btn-icon"
                        aria-label="Remover forma de pagamento"
                        title="Remover forma de pagamento"
                        disabled={rows.length === 1}
                        onClick={() => setRows((current) => current.filter((_, i) => i !== index))}
                        data-testid={`btn-remove-payment-${index}`}
                    >
                        ✕
                    </button>
                    {needsReceipt(row.paymentMethodId) && (
                        <input
                            style={{ flex: "1 1 100%" }}
                            placeholder="Comprovante / autorização (ex.: AUT-123456)"
                            value={row.authorizationCode}
                            onChange={(e) => setRow(index, { authorizationCode: e.target.value })}
                            data-testid={`input-auth-code-${index}`}
                        />
                    )}
                </div>
            ))}

            <button
                type="button"
                className="btn-ghost"
                onClick={() =>
                    setRows((current) => [
                        ...current,
                        { paymentMethodId: PaymentMethod.CartaoCredito, amount: "", authorizationCode: "" },
                    ])
                }
                data-testid="btn-add-payment-method"
            >
                + Adicionar forma de pagamento
            </button>

            <div className="ticket" style={{ padding: "12px 16px", display: "grid", gap: 4 }}>
                <div style={{ display: "flex", justifyContent: "space-between", color: "var(--ink-dim)" }}>
                    <span>Pago</span>
                    <span className="mono-num" data-testid="summary-total-paid">{formatBRL(totalPaid)}</span>
                </div>
                <div style={{ display: "flex", justifyContent: "space-between", color: "var(--ink-dim)" }}>
                    <span>Restante</span>
                    <span className="mono-num" data-testid="summary-remaining">
                        {formatBRL(Math.max(0, amountDue - totalPaid))}
                    </span>
                </div>
                {change > 0 && (
                    <div style={{ display: "flex", justifyContent: "space-between", color: "var(--amber)" }}>
                        <span>Troco (dinheiro)</span>
                        <span className="mono-num" data-testid="summary-change">{formatBRL(change)}</span>
                    </div>
                )}
                {!changeValid && (
                    <p className="error-text" data-testid="change-invalid-error">O troco excede o valor pago em dinheiro.</p>
                )}
            </div>

            {error && <p className="error-text" data-testid="payment-error-msg">{error}</p>}

            <button
                type="button"
                className="btn-primary"
                disabled={!canConfirm || payMutation.isPending}
                onClick={() => payMutation.mutate()}
                data-testid="btn-confirm-payment"
            >
                {payMutation.isPending ? "Registrando…" : "Confirmar pagamento"}
            </button>
        </div>
    );
}