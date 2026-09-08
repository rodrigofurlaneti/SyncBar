import { useEffect, useRef, useState } from "react";
import Swal from "sweetalert2";
import { formatBRL } from "../../lib/types";
import {
    getPaymentStatusByOrder,
    type BoletoPaymentResult,
    type CreditCardPaymentResult,
    type PixPaymentResult,
} from "./checkoutApi";

export type StorefrontPaymentMethod = "PIX" | "CREDITO" | "BOLETO" | "DEBITO";

export type StorefrontPaymentResult =
    | { method: "DEBITO"; data: import("./checkoutApi").HostedCardPaymentResult }
    | { method: "PIX"; data: PixPaymentResult }
    | { method: "CREDITO"; data: CreditCardPaymentResult }
    | { method: "BOLETO"; data: BoletoPaymentResult };

type StorefrontPaymentModalProps = {
    orderId: number;
    result: StorefrontPaymentResult;
    onClose: () => void;
};

const PAID_STATUSES = ["RECEIVED", "CONFIRMED", "RECEIVED_IN_CASH"];
const POLL_INTERVAL_MS = 4000;
const POLL_TIMEOUT_MS = 3 * 60 * 1000;

const styles = `
  .payment-modal-wrapper { position: fixed; inset: 0; z-index: 10000; display: flex; align-items: center; justify-content: center; font-family: system-ui, -apple-system, sans-serif; animation: pmFadeIn 0.2s ease-out; padding: 1rem; }
  .payment-modal-backdrop { position: absolute; inset: 0; background-color: rgba(0, 0, 0, 0.8); backdrop-filter: blur(0.25rem); }
  .payment-modal-panel { position: relative; width: 100%; max-width: 26rem; max-height: 90vh; overflow-y: auto; background-color: #18181b; border-radius: 1rem; box-shadow: 0 1.25rem 3rem rgba(0, 0, 0, 0.6); border: 0.0625rem solid #27272a; animation: pmRise 0.25s cubic-bezier(0.16, 1, 0.3, 1); }
  .payment-modal-header { display: flex; align-items: center; justify-content: space-between; padding: 1.25rem 1.5rem; border-bottom: 0.0625rem solid #27272a; }
  .payment-modal-title { font-size: 1.125rem; font-weight: bold; color: #f4f4f5; margin: 0; }
  .payment-modal-body { padding: 1.5rem; display: flex; flex-direction: column; gap: 1rem; align-items: center; text-align: center; }
  .payment-modal-close { background: none; border: none; color: #a1a1aa; cursor: pointer; padding: 0.5rem; border-radius: 0.375rem; }
  .payment-modal-close:hover { color: #fff; background-color: #27272a; }
  .payment-qr-img { width: 12rem; height: 12rem; border-radius: 0.75rem; background: #fff; padding: 0.5rem; }
  .payment-code-box { width: 100%; display: flex; flex-direction: column; gap: 0.375rem; text-align: left; }
  .payment-code-input { width: 100%; padding: 0.75rem; border-radius: 0.5rem; background-color: #09090b; border: 0.0625rem solid #3f3f46; color: #d4d4d8; font-size: 0.75rem; font-family: monospace; box-sizing: border-box; word-break: break-all; }
  .payment-btn { width: 100%; border-radius: 0.625rem; padding: 0.75rem; font-size: 0.9rem; font-weight: bold; cursor: pointer; border: 0.0625rem solid transparent; transition: all 0.2s ease; }
  .payment-btn-primary { background-color: #f59e0b; color: #18181b; }
  .payment-btn-primary:hover { background-color: #d97706; }
  .payment-btn-outline { background: transparent; border-color: #3f3f46; color: #f4f4f5; }
  .payment-btn-outline:hover { border-color: #f59e0b; color: #f59e0b; }
  .payment-status-badge { display: inline-flex; align-items: center; gap: 0.375rem; font-size: 0.8rem; font-weight: 600; padding: 0.375rem 0.75rem; border-radius: 999px; }
  .payment-status-waiting { background: rgba(245, 158, 11, 0.12); color: #f59e0b; }
  .payment-status-confirmed { background: rgba(16, 185, 129, 0.12); color: #10b981; }
  @keyframes pmFadeIn { from { opacity: 0; } to { opacity: 1; } }
  @keyframes pmRise { from { opacity: 0; transform: translateY(0.75rem) scale(0.98); } to { opacity: 1; transform: translateY(0) scale(1); } }
  @keyframes pmSpin { to { transform: rotate(360deg); } }
  .payment-spinner { width: 0.9rem; height: 0.9rem; border-radius: 50%; border: 0.15rem solid rgba(245, 158, 11, 0.35); border-top-color: #f59e0b; animation: pmSpin 0.8s linear infinite; }
`;

const METHOD_LABEL: Record<StorefrontPaymentMethod, string> = {
    PIX: "Pix",
    CREDITO: "Cartão de Crédito",
    DEBITO: "Cartão de Débito",
    BOLETO: "Boleto",
};

function copyToClipboard(text: string, message: string) {
    navigator.clipboard.writeText(text).then(() => {
        Swal.fire({
            toast: true,
            position: "top-end",
            icon: "success",
            title: message,
            showConfirmButton: false,
            timer: 1500,
            background: "#18181b",
            color: "#fff",
        });
    });
}

export function StorefrontPaymentModal({ orderId, result, onClose }: StorefrontPaymentModalProps) {
    const [status, setStatus] = useState<string>(result.data.status);
    const [isPolling, setIsPolling] = useState(result.method !== "CREDITO");
    const [timedOut, setTimedOut] = useState(false);
    const startedAtRef = useRef(Date.now());

    const isConfirmed = PAID_STATUSES.includes(status.toUpperCase());

    useEffect(() => {
        const handleKeyDown = (e: KeyboardEvent) => {
            if (e.key === "Escape") onClose();
        };
        document.addEventListener("keydown", handleKeyDown);
        document.body.style.overflow = "hidden";
        return () => {
            document.removeEventListener("keydown", handleKeyDown);
            document.body.style.overflow = "unset";
        };
    }, [onClose]);

    useEffect(() => {
        if (!isPolling || isConfirmed) return;

        let cancelled = false;
        const interval = setInterval(async () => {
            if (Date.now() - startedAtRef.current > POLL_TIMEOUT_MS) {
                if (!cancelled) {
                    setIsPolling(false);
                    setTimedOut(true);
                }
                return;
            }
            try {
                const current = await getPaymentStatusByOrder(orderId);
                if (!cancelled && current?.status) {
                    setStatus(current.status);
                }
            } catch {
                // Falha pontual de rede não interrompe o polling — tenta de novo no próximo tick.
            }
        }, POLL_INTERVAL_MS);

        return () => {
            cancelled = true;
            clearInterval(interval);
        };
    }, [isPolling, isConfirmed, orderId]);

    const handleCheckAgain = () => {
        setTimedOut(false);
        startedAtRef.current = Date.now();
        setIsPolling(true);
    };

    const renderMethodBody = () => {
        switch (result.method) {
            case "DEBITO":
                return <><p>Conclua o pagamento de {formatBRL(result.data.value)} na fatura do Asaas.</p><a className="payment-btn payment-btn-primary" href={result.data.invoiceUrl} target="_blank" rel="noopener noreferrer">Abrir fatura para pagar</a></>;
            case "PIX": {
                const { pixQrCodeBase64, pixPayload, value } = result.data;
                return (
                    <>
                        <p style={{ margin: 0, color: "#a1a1aa", fontSize: "0.875rem" }}>
                            Escaneie o QR Code ou copie o código Pix — valor de <strong style={{ color: "#f4f4f5" }}>{formatBRL(value)}</strong>.
                        </p>
                        {pixQrCodeBase64 && (
                            <img className="payment-qr-img" src={`data:image/png;base64,${pixQrCodeBase64}`} alt="QR Code Pix" />
                        )}
                        {pixPayload && (
                            <div className="payment-code-box">
                                <label style={{ fontSize: "0.75rem", color: "#a1a1aa", fontWeight: 500 }}>Pix copia e cola</label>
                                <textarea className="payment-code-input" readOnly rows={3} value={pixPayload} />
                                <button type="button" className="payment-btn payment-btn-outline" onClick={() => copyToClipboard(pixPayload, "Código Pix copiado!")}>
                                    📋 Copiar código
                                </button>
                            </div>
                        )}
                    </>
                );
            }
            case "BOLETO": {
                const { identificationField, bankSlipUrl, value, dueDate } = result.data;
                return (
                    <>
                        <p style={{ margin: 0, color: "#a1a1aa", fontSize: "0.875rem" }}>
                            Boleto de <strong style={{ color: "#f4f4f5" }}>{formatBRL(value)}</strong>, vencimento em{" "}
                            {new Date(dueDate).toLocaleDateString("pt-BR")}.
                        </p>
                        {identificationField && (
                            <div className="payment-code-box">
                                <label style={{ fontSize: "0.75rem", color: "#a1a1aa", fontWeight: 500 }}>Linha digitável</label>
                                <textarea className="payment-code-input" readOnly rows={2} value={identificationField} />
                                <button type="button" className="payment-btn payment-btn-outline" onClick={() => copyToClipboard(identificationField, "Linha digitável copiada!")}>
                                    📋 Copiar linha digitável
                                </button>
                            </div>
                        )}
                        {bankSlipUrl && (
                            <a href={bankSlipUrl} target="_blank" rel="noreferrer" className="payment-btn payment-btn-primary" style={{ display: "block", textDecoration: "none", boxSizing: "border-box" }}>
                                📄 Abrir boleto em PDF
                            </a>
                        )}
                    </>
                );
            }
            case "CREDITO": {
                const { cardBrand, last4Digits, value } = result.data;
                return (
                    <p style={{ margin: 0, color: "#a1a1aa", fontSize: "0.875rem" }}>
                        Cobrança de <strong style={{ color: "#f4f4f5" }}>{formatBRL(value)}</strong> enviada
                        {cardBrand ? ` no cartão ${cardBrand}` : ""}
                        {last4Digits ? ` final ${last4Digits}` : ""}. Estamos aguardando a confirmação do banco.
                    </p>
                );
            }
        }
    };

    return (
        <div className="payment-modal-wrapper" role="dialog" aria-modal="true" aria-labelledby="payment-modal-title">
            <style>{styles}</style>
            <div className="payment-modal-backdrop" aria-hidden="true" onClick={onClose} />
            <div className="payment-modal-panel">
                <header className="payment-modal-header">
                    <h2 id="payment-modal-title" className="payment-modal-title">
                        {isConfirmed ? "🎉 Pagamento confirmado!" : `Pagamento via ${METHOD_LABEL[result.method]}`}
                    </h2>
                    <button type="button" className="payment-modal-close" onClick={onClose} aria-label="Fechar">
                        ✕
                    </button>
                </header>
                <div className="payment-modal-body">
                    {isConfirmed ? (
                        <>
                            <span className="payment-status-badge payment-status-confirmed">✓ Pago com sucesso</span>
                            <p style={{ margin: 0, color: "#a1a1aa", fontSize: "0.875rem" }}>
                                Seu pedido já está sendo preparado. Obrigado!
                            </p>
                            <button type="button" className="payment-btn payment-btn-primary" onClick={onClose}>
                                Concluir
                            </button>
                        </>
                    ) : (
                        <>
                            {renderMethodBody()}
                            {result.method !== "CREDITO" && (
                                <>
                                    {isPolling ? (
                                        <span className="payment-status-badge payment-status-waiting">
                                            <span className="payment-spinner" aria-hidden="true" /> Aguardando confirmação...
                                        </span>
                                    ) : timedOut ? (
                                        <button type="button" className="payment-btn payment-btn-outline" onClick={handleCheckAgain}>
                                            🔄 Verificar novamente
                                        </button>
                                    ) : null}
                                </>
                            )}
                        </>
                    )}
                </div>
            </div>
        </div>
    );
}

