import { useState } from "react";
import { formatBRL } from "../../lib/types";

type PublicOrderModalProps = {
    isOpen: boolean;
    onClose: () => void;
    tableNumber: string;
    onFetchMesaBill: () => Promise<any>;
    onFetchComandaBill: (code: string) => Promise<any>;
};

export function PublicOrderModal({ isOpen, onClose, tableNumber, onFetchMesaBill, onFetchComandaBill }: PublicOrderModalProps) {
    const [step, setStep] = useState<"select" | "view">("select");
    const [destination, setDestination] = useState<"mesa" | "comanda">("mesa");
    const [commandNumber, setCommandNumber] = useState("");
    const [consultedCode, setConsultedCode] = useState("");

    const [loading, setLoading] = useState(false);
    const [error, setError] = useState(false);
    const [billData, setBillData] = useState<any>(null);

    if (!isOpen) return null;

    const handleConsult = async () => {
        setLoading(true);
        setError(false);
        try {
            let data;
            if (destination === "mesa") {
                data = await onFetchMesaBill();
            } else {
                if (!commandNumber) return;
                setConsultedCode(commandNumber);
                data = await onFetchComandaBill(commandNumber);
            }
            setBillData(data);
            setStep("view");
        } catch {
            setError(true);
        } finally {
            setLoading(false);
        }
    };

    return (
        <div style={{ position: "fixed", inset: 0, backgroundColor: "rgba(0,0,0,0.8)", zIndex: 9999, display: "flex", alignItems: step === "select" ? "center" : "flex-end", justifyContent: "center", padding: step === "select" ? 16 : 0 }}>
            {step === "select" ? (
                <div style={{ backgroundColor: "#1e1e24", padding: 24, borderRadius: 12, width: "100%", maxWidth: 400, border: "1px solid #323238", boxShadow: "0 10px 25px rgba(0,0,0,0.5)" }}>
                    <h3 style={{ marginTop: 0, marginBottom: 24, color: "#fff", fontSize: "1.2rem", textAlign: "center" }}>
                        Qual conta deseja consultar?
                    </h3>

                    <div style={{ display: "flex", gap: 12, marginBottom: destination === "comanda" ? 16 : 24 }}>
                        <button
                            onClick={() => setDestination("mesa")}
                            style={{ flex: 1, padding: "14px", borderRadius: 8, border: destination === "mesa" ? "2px solid #f59e0b" : "1px solid #323238", backgroundColor: destination === "mesa" ? "rgba(245, 158, 11, 0.1)" : "transparent", color: destination === "mesa" ? "#f59e0b" : "#a8a8b3", fontWeight: "bold", cursor: "pointer" }}
                        >
                            Da Mesa
                        </button>
                        <button
                            onClick={() => setDestination("comanda")}
                            style={{ flex: 1, padding: "14px", borderRadius: 8, border: destination === "comanda" ? "2px solid #f59e0b" : "1px solid #323238", backgroundColor: destination === "comanda" ? "rgba(245, 158, 11, 0.1)" : "transparent", color: destination === "comanda" ? "#f59e0b" : "#a8a8b3", fontWeight: "bold", cursor: "pointer" }}
                        >
                            Da Comanda
                        </button>
                    </div>

                    {destination === "comanda" && (
                        <div style={{ marginBottom: 24 }}>
                            <label style={{ display: "block", color: "#a8a8b3", marginBottom: 8, fontSize: "0.9rem" }}>Número da Comanda</label>
                            <input
                                type="text"
                                value={commandNumber}
                                onChange={(e) => setCommandNumber(e.target.value)}
                                placeholder="Ex: 001"
                                autoFocus
                                style={{ width: "100%", padding: "14px 16px", borderRadius: 8, border: "1px solid #323238", backgroundColor: "#121214", color: "#fff", fontSize: "1rem", outline: "none", boxSizing: "border-box" }}
                            />
                        </div>
                    )}

                    <div style={{ display: "flex", gap: 12 }}>
                        <button onClick={onClose} style={{ flex: 1, padding: "14px", borderRadius: 8, border: "none", backgroundColor: "#323238", color: "#fff", fontWeight: "bold", cursor: "pointer" }}>
                            Cancelar
                        </button>
                        <button
                            disabled={destination === "comanda" && !commandNumber || loading}
                            onClick={handleConsult}
                            style={{ flex: 1, padding: "14px", borderRadius: 8, border: "none", backgroundColor: "#f59e0b", color: "#121214", fontWeight: "bold", cursor: "pointer", opacity: loading ? 0.7 : 1 }}
                        >
                            {loading ? "Buscando..." : "Consultar"}
                        </button>
                    </div>
                </div>
            ) : (
                <div style={{ backgroundColor: "#1e1e24", borderTopLeftRadius: 24, borderTopRightRadius: 24, padding: 24, width: "100%", maxHeight: "85vh", display: "flex", flexDirection: "column", boxShadow: "0 -5px 25px rgba(0,0,0,0.5)" }}>
                    <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", borderBottom: "1px solid #323238", paddingBottom: 16, marginBottom: 16 }}>
                        <div style={{ display: "flex", alignItems: "center", gap: 12 }}>
                            <button onClick={() => setStep("select")} style={{ background: "none", border: "none", color: "#a8a8b3", fontSize: "1.2rem", cursor: "pointer", padding: 0 }}>←</button>
                            <h2 style={{ margin: 0, fontSize: "1.2rem", color: "#fff" }}>
                                {destination === "mesa" ? `Conta - Mesa ${tableNumber}` : `Conta - Comanda ${consultedCode}`}
                            </h2>
                        </div>
                        <button onClick={onClose} style={{ background: "none", border: "none", color: "#a8a8b3", fontSize: "1.5rem", cursor: "pointer" }}>✕</button>
                    </div>

                    <div style={{ flex: 1, overflowY: "auto", paddingRight: 4 }}>
                        {error ? (
                            <p style={{ textAlign: "center", color: "#ef4444", marginTop: 40 }}>Nenhum consumo encontrado ou comanda inválida.</p>
                        ) : !billData?.items?.length ? (
                            <p style={{ textAlign: "center", color: "#a8a8b3", marginTop: 40 }}>Nenhum pedido feito ainda nesta conta.</p>
                        ) : (
                            <div style={{ display: "grid", gap: 12 }}>
                                {billData.items.map((order: any) => {
                                    const statusText = order.statusId === 1 ? "Pendente" : order.statusId === 2 ? "Preparando" : order.statusId === 3 ? "Pronto" : "Entregue";
                                    return (
                                        <div key={order.itemId} style={{ backgroundColor: "#202024", padding: 16, borderRadius: 8, border: "1px solid #323238" }}>
                                            <div style={{ display: "flex", justifyContent: "space-between", marginBottom: 8 }}>
                                                <span style={{ color: "#fff", fontWeight: "bold" }}>{order.quantity}x {order.productName}</span>
                                                <span style={{ color: "#f59e0b", fontWeight: "bold" }}>{formatBRL(order.totalPrice)}</span>
                                            </div>
                                            <div style={{ display: "flex", justifyContent: "flex-end" }}>
                                                <span style={{ fontSize: "0.8rem", padding: "4px 8px", borderRadius: 4, fontWeight: "bold", backgroundColor: "rgba(245, 158, 11, 0.2)", color: "#f59e0b" }}>
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
                            <span style={{ color: "#a8a8b3", fontSize: "1.1rem" }}>Total Geral</span>
                            <span style={{ color: "#fff", fontSize: "1.4rem", fontWeight: "bold" }}>{formatBRL(billData?.totalAmount || 0)}</span>
                        </div>
                        <button onClick={onClose} style={{ width: "100%", padding: "16px", borderRadius: 8, border: "none", backgroundColor: "#f59e0b", color: "#121214", fontWeight: "bold", fontSize: "1.1rem", cursor: "pointer" }}>
                            Continuar Comprando
                        </button>
                    </div>
                </div>
            )}
        </div>
    );
}