import { useState } from "react";
import { useMutation, useQuery } from "@tanstack/react-query";
import Swal from "sweetalert2"; // Adicionado SweetAlert2
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

// Configuração do Toast do SweetAlert2
const Toast = Swal.mixin({
    toast: true,
    position: "top-end",
    showConfirmButton: false,
    timer: 3000,
    timerProgressBar: true,
});

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
        onSuccess: () => {
            Toast.fire({ icon: "success", title: "Pagamento parcial registrado." });
            onRegistered();
        },
        onError: (e) => {
            const msg = e instanceof ApiError ? e.message : "Falha ao registrar o pagamento parcial.";
            setError(msg);
            Swal.fire("Erro", msg, "error");
        },
    });

    return (
        <Overlay title="Pagamento parcial" onClose={onClose} data-testid="partial-payment-overlay">
            <p style={{ color: "var(--ink-dim)", fontSize: "0.9rem", margin: 0 }}>
                Cliente saindo antes? Registre o valor pago — a mesa continua aberta e o
                restante é cobrado no fechamento. Restante atual:{" "}
                <strong className="mono-num" style={{ color: "var(--amber)" }} data-testid="remaining-amount">
                    {formatBRL(remaining)}
                </strong>
            </p>

            {noSession && (
                <p className="error-text" data-testid="no-session-error">
                    O caixa está fechado — abra uma sessão (botão Caixa no topo) para receber.
                </p>
            )}

            <label style={{ display: "grid", gap: 4, marginTop: 12 }}>
                <span style={{ color: "var(--ink-dim)", fontSize: "0.85rem" }}>Quem pagou (opcional)</span>
                <input
                    placeholder="ex.: Carlos"
                    value={payerName}
                    onChange={(e) => setPayerName(e.target.value)}
                    data-testid="input-payer-name"
                />
            </label>

            <div style={{ display: "grid", gap: 8, gridTemplateColumns: "1.3fr 1fr", marginTop: 12 }}>
                <label style={{ display: "grid", gap: 4 }}>
                    <span style={{ color: "var(--ink-dim)", fontSize: "0.85rem" }}>Forma de pagamento</span>
                    <select
                        value={methodId}
                        onChange={(e) => setMethodId(Number(e.target.value))}
                        data-testid="select-payment-method"
                    >
                        {Object.entries(paymentMethodLabel).map(([id, label]) => (
                            <option key={id} value={id}>{label}</option>
                        ))}
                    </select>
                </label>
                <label style={{ display: "grid", gap: 4 }}>
                    <span style={{ color: "var(--ink-dim)", fontSize: "0.85rem" }}>Valor (R$)</span>
                    <input
                        inputMode="decimal"
                        placeholder="65,55"
                        value={amount}
                        onChange={(e) => setAmount(e.target.value)}
                        data-testid="input-payment-amount"
                    />
                </label>
            </div>

            {needsReceipt(methodId) && (
                <input
                    placeholder="Comprovante / autorização"
                    value={authorizationCode}
                    onChange={(e) => setAuthorizationCode(e.target.value)}
                    style={{ marginTop: 12, width: "100%" }}
                    data-testid="input-auth-code"
                />
            )}

            {value !== null && value > remaining && (
                <p className="error-text" data-testid="exceeds-remaining-error">
                    O valor excede o restante da conta ({formatBRL(remaining)}).
                </p>
            )}
            {error && <p className="error-text" data-testid="dialog-error-message">{error}</p>}

            <div style={{ display: "flex", justifyContent: "flex-end", marginTop: 24 }}>
                <button
                    type="button"
                    className="btn-primary"
                    disabled={value === null || value > remaining || noSession || sessionQuery.isLoading || mutation.isPending}
                    onClick={() => mutation.mutate()}
                    data-testid="btn-submit-partial-payment"
                >
                    {mutation.isPending ? "Registrando…" : "Registrar pagamento parcial"}
                </button>
            </div>
        </Overlay>
    );
}