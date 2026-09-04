import { useState, useEffect } from "react";
import { formatBRL } from "../../lib/types";
import { ComandaReadingValidation, needsReadingValidation, type ReadingValidationRequirement } from "./ComandaReadingValidation";

type PublicOrderModalProps = {
    isOpen: boolean;
    onClose: () => void;
    tableNumber: string;
    token: string;
    isQrViewEnabled: boolean;
    linkedComandaCode: string | null;
    readingValidation: ReadingValidationRequirement;
    isComandaValidated: (code: string) => boolean;
    onComandaValidated: (code: string) => void;
    onFetchMesaBill: () => Promise<any>;
    onFetchComandaBill: (code: string) => Promise<any>;
};

export function PublicOrderModal({
    isOpen, onClose, tableNumber, token, isQrViewEnabled, linkedComandaCode, readingValidation, isComandaValidated, onComandaValidated,
    onFetchMesaBill, onFetchComandaBill,
}: PublicOrderModalProps) {
    const [step, setStep] = useState<"select" | "validate" | "view">("select");
    const [destination, setDestination] = useState<"mesa" | "comanda">("mesa");
    const [commandNumber, setCommandNumber] = useState("");
    const [consultedCode, setConsultedCode] = useState("");
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState(false);
    const [billData, setBillData] = useState<any>(null);

    const [windowWidth, setWindowWidth] = useState(typeof window !== "undefined" ? window.innerWidth : 1200);
    useEffect(() => {
        const handleResize = () => setWindowWidth(window.innerWidth);
        window.addEventListener("resize", handleResize);
        return () => window.removeEventListener("resize", handleResize);
    }, []);
    const isTvOrLarge = windowWidth > 1200;

    useEffect(() => {
        if (!isOpen || isQrViewEnabled) return;
        let cancelled = false;
        setLoading(true);
        setError(false);

        let fetchBill: Promise<any>;
        if (linkedComandaCode) {
            setDestination("comanda");
            setConsultedCode(linkedComandaCode);
            fetchBill = onFetchComandaBill(linkedComandaCode);
        } else {
            setDestination("mesa");
            fetchBill = onFetchMesaBill();
        }

        fetchBill
            .then((data) => {
                if (cancelled) return;
                setBillData(data);
                setStep("view");
            })
            .catch(() => {
                if (!cancelled) setError(true);
            })
            .finally(() => {
                if (!cancelled) setLoading(false);
            });
        return () => {
            cancelled = true;
        };
    }, [isOpen, isQrViewEnabled, linkedComandaCode]);

    if (!isOpen) return null;

    const consultComanda = async (code: string) => {
        setLoading(true);
        setError(false);
        try {
            setConsultedCode(code);
            const data = await onFetchComandaBill(code);
            setBillData(data);
            setStep("view");
        } catch {
            setError(true);
            setStep("select");
        } finally {
            setLoading(false);
        }
    };

    const handleConsult = async () => {
        if (destination === "mesa") {
            setLoading(true);
            setError(false);
            try {
                const data = await onFetchMesaBill();
                setBillData(data);
                setStep("view");
            } catch {
                setError(true);
            } finally {
                setLoading(false);
            }
            return;
        }
        if (!commandNumber) return;

        if (needsReadingValidation(readingValidation) && !isComandaValidated(commandNumber)) {
            setStep("validate");
            return;
        }
        await consultComanda(commandNumber);
    };

    return (
        <div data-testid="public-order-modal-container" style={{ position: "fixed", inset: 0, backgroundColor: "rgba(0,0,0,0.85)", zIndex: 9999, display: "flex", alignItems: step === "view" ? "flex-end" : "center", justifyContent: "center", padding: step === "view" ? 0 : 16 }}>
            {step === "validate" ? (
                <div data-testid="modal-step-validate" style={{ backgroundColor: "#1e1e24", padding: isTvOrLarge ? 36 : 24, borderRadius: 16, width: "100%", maxWidth: isTvOrLarge ? 550 : 420, border: "1px solid #323238", boxShadow: "0 10px 30px rgba(0,0,0,0.6)" }}>
                    <ComandaReadingValidation
                        token={token}
                        comandaCode={commandNumber}
                        requirement={readingValidation}
                        onValidated={() => {
                            onComandaValidated(commandNumber);
                            void consultComanda(commandNumber);
                        }}
                        onCancel={() => setStep("select")}
                    />
                </div>
            ) : step === "select" && isQrViewEnabled ? (
                <div data-testid="modal-step-select" style={{ backgroundColor: "#1e1e24", padding: isTvOrLarge ? 36 : 24, borderRadius: 16, width: "100%", maxWidth: isTvOrLarge ? 550 : 420, border: "1px solid #323238", boxShadow: "0 10px 30px rgba(0,0,0,0.6)", fontSize: isTvOrLarge ? "1.15rem" : "1rem" }}>
                    <h3 style={{ marginTop: 0, marginBottom: isTvOrLarge ? 32 : 24, color: "#fff", fontSize: isTvOrLarge ? "1.5rem" : "1.2rem", textAlign: "center" }}>
                        Qual conta deseja consultar?
                    </h3>
                    <div style={{ display: "flex", gap: 12, marginBottom: destination === "comanda" ? 20 : 28 }}>
                        <button
                            data-testid="btn-select-mesa"
                            onClick={() => setDestination("mesa")}
                            style={{ flex: 1, padding: isTvOrLarge ? "18px" : "14px", borderRadius: 8, border: destination === "mesa" ? "2px solid #f59e0b" : "1px solid #323238", backgroundColor: destination === "mesa" ? "rgba(245, 158, 11, 0.1)" : "transparent", color: destination === "mesa" ? "#f59e0b" : "#a8a8b3", fontWeight: "bold", cursor: "pointer", fontSize: isTvOrLarge ? "1.1rem" : "1rem" }}
                        >
                            Da Mesa
                        </button>
                        <button
                            data-testid="btn-select-comanda"
                            onClick={() => setDestination("comanda")}
                            style={{ flex: 1, padding: isTvOrLarge ? "18px" : "14px", borderRadius: 8, border: destination === "comanda" ? "2px solid #f59e0b" : "1px solid #323238", backgroundColor: destination === "comanda" ? "rgba(245, 158, 11, 0.1)" : "transparent", color: destination === "comanda" ? "#f59e0b" : "#a8a8b3", fontWeight: "bold", cursor: "pointer", fontSize: isTvOrLarge ? "1.1rem" : "1rem" }}
                        >
                            Da Comanda
                        </button>
                    </div>
                    {destination === "comanda" && (
                        <div style={{ marginBottom: 28 }}>
                            <label style={{ display: "block", color: "#a8a8b3", marginBottom: 8, fontSize: isTvOrLarge ? "1.05rem" : "0.9rem" }}>Número da Comanda</label>
                            <input
                                type="text"
                                data-testid="input-command-number"
                                value={commandNumber}
                                onChange={(e) => setCommandNumber(e.target.value)}
                                placeholder="Ex: 001"
                                autoFocus
                                style={{ width: "100%", padding: isTvOrLarge ? "18px 20px" : "14px 16px", borderRadius: 8, border: "1px solid #323238", backgroundColor: "#121214", color: "#fff", fontSize: isTvOrLarge ? "1.2rem" : "1rem", outline: "none", boxSizing: "border-box" }}
                            />
                        </div>
                    )}
                    {error && destination === "comanda" && (
                        <p data-testid="error-select-msg" style={{ color: "#ef4444", fontSize: "0.9rem", textAlign: "center", marginBottom: 16 }}>Comanda inválida ou não encontrada.</p>
                    )}
                    <div style={{ display: "flex", gap: 12 }}>
                        <button data-testid="btn-cancel-select" onClick={onClose} style={{ flex: 1, padding: isTvOrLarge ? "18px" : "14px", borderRadius: 8, border: "none", backgroundColor: "#323238", color: "#fff", fontWeight: "bold", cursor: "pointer", fontSize: isTvOrLarge ? "1.1rem" : "1rem" }}>
                            Cancelar
                        </button>
                        <button
                            data-testid="btn-submit-consult"
                            disabled={destination === "comanda" && !commandNumber || loading}
                            onClick={handleConsult}
                            style={{ flex: 1, padding: isTvOrLarge ? "18px" : "14px", borderRadius: 8, border: "none", backgroundColor: "#f59e0b", color: "#121214", fontWeight: "bold", cursor: "pointer", opacity: loading ? 0.7 : 1, fontSize: isTvOrLarge ? "1.1rem" : "1rem" }}
                        >
                            {loading ? "Buscando..." : "Consultar"}
                        </button>
                    </div>
                </div>
            ) : step === "view" ? (
                <div data-testid="modal-step-view" style={{ backgroundColor: "#1e1e24", borderTopLeftRadius: 24, borderTopRightRadius: 24, padding: isTvOrLarge ? 36 : 24, width: "100%", maxWidth: isTvOrLarge ? 800 : "100%", maxHeight: "85vh", display: "flex", flexDirection: "column", boxShadow: "0 -5px 30px rgba(0,0,0,0.6)", fontSize: isTvOrLarge ? "1.1rem" : "1rem" }}>
                    <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", borderBottom: "1px solid #323238", paddingBottom: 16, marginBottom: 16 }}>
                        <div style={{ display: "flex", alignItems: "center", gap: 12 }}>
                            {isQrViewEnabled && !linkedComandaCode && (
                                <button data-testid="btn-back-to-select" onClick={() => setStep("select")} style={{ background: "none", border: "none", color: "#a8a8b3", fontSize: isTvOrLarge ? "1.5rem" : "1.2rem", cursor: "pointer", padding: 0 }}>←</button>
                            )}
                            <h2 data-testid="view-title" style={{ margin: 0, fontSize: isTvOrLarge ? "1.5rem" : "1.2rem", color: "#fff" }}>
                                {destination === "mesa" ? `Conta - Mesa ${tableNumber}` : `Conta - Comanda ${consultedCode}`}
                            </h2>
                        </div>
                        <button data-testid="btn-close-view" onClick={onClose} style={{ background: "none", border: "none", color: "#a8a8b3", fontSize: isTvOrLarge ? "2rem" : "1.5rem", cursor: "pointer" }}>✕</button>
                    </div>
                    <div style={{ flex: 1, overflowY: "auto", paddingRight: 4 }}>
                        {error ? (
                            <p data-testid="error-view-msg" style={{ textAlign: "center", color: "#ef4444", marginTop: 40, fontSize: isTvOrLarge ? "1.2rem" : "1rem" }}>Nenhum consumo encontrado ou erro na busca.</p>
                        ) : !billData?.items?.length ? (
                            <p data-testid="empty-view-msg" style={{ textAlign: "center", color: "#a8a8b3", marginTop: 40, fontSize: isTvOrLarge ? "1.2rem" : "1rem" }}>Nenhum pedido feito ainda nesta conta.</p>
                        ) : (
                            <div style={{ display: "grid", gap: 12 }} data-testid="bill-items-list">
                                {billData.items.map((order: any, index: number) => {
                                    const statusText = order.statusId === 1 ? "Pendente" : order.statusId === 2 ? "Preparando" : order.statusId === 3 ? "Pronto" : "Entregue";
                                    return (
                                        <div key={order.itemId || index} data-testid={`bill-item-row-${order.itemId || index}`} style={{ backgroundColor: "#202024", padding: isTvOrLarge ? 20 : 16, borderRadius: 8, border: "1px solid #323238" }}>
                                            <div style={{ display: "flex", justifyContent: "space-between", marginBottom: 8 }}>
                                                <span style={{ color: "#fff", fontWeight: "bold", fontSize: isTvOrLarge ? "1.2rem" : "1rem" }}>{order.quantity}x {order.productName}</span>
                                                <span style={{ color: "#f59e0b", fontWeight: "bold", fontSize: isTvOrLarge ? "1.2rem" : "1rem" }}>{formatBRL(order.totalPrice)}</span>
                                            </div>
                                            <div style={{ display: "flex", justifyContent: "flex-end" }}>
                                                <span style={{ fontSize: isTvOrLarge ? "0.95rem" : "0.8rem", padding: "4px 8px", borderRadius: 4, fontWeight: "bold", backgroundColor: "rgba(245, 158, 11, 0.2)", color: "#f59e0b" }}>
                                                    {statusText}
                                                </span>
                                            </div>
                                        </div>
                                    );
                                })}
                            </div>
                        )}
                    </div>
                    <div style={{ borderTop: "1px solid #323238", paddingTop: 16, marginTop: 16 }}>
                        <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: 16 }}>
                            <span style={{ color: "#a8a8b3", fontSize: isTvOrLarge ? "1.3rem" : "1.1rem" }}>Total Geral</span>
                            <span data-testid="bill-total-amount" style={{ color: "#fff", fontSize: isTvOrLarge ? "1.8rem" : "1.4rem", fontWeight: "bold" }}>{formatBRL(billData?.totalAmount || 0)}</span>
                        </div>
                        <button data-testid="btn-continue-shopping" onClick={onClose} style={{ width: "100%", padding: isTvOrLarge ? "20px" : "16px", borderRadius: 8, border: "none", backgroundColor: "#f59e0b", color: "#121214", fontWeight: "bold", fontSize: isTvOrLarge ? "1.2rem" : "1.1rem", cursor: "pointer" }}>
                            Continuar Comprando
                        </button>
                    </div>
                </div>
            ) : (
                <div data-testid="modal-step-loading" style={{ backgroundColor: "#1e1e24", padding: isTvOrLarge ? 36 : 24, borderRadius: 16, width: "100%", maxWidth: isTvOrLarge ? 550 : 420, border: "1px solid #323238", boxShadow: "0 10px 30px rgba(0,0,0,0.6)", textAlign: "center", color: "#a8a8b3" }}>
                    Carregando…
                </div>
            )}
        </div>
    );
}