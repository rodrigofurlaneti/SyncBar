import { useState } from "react";
import Swal from "sweetalert2";

type StorefrontHubModalProps = {
    isOpen: boolean;
    onClose: () => void;
    branchId: number;
};

export function StorefrontHubModal({
    isOpen,
    onClose,
    branchId,
}: StorefrontHubModalProps) {
    const [activeTab, setActiveTab] = useState<"link" | "qrcode">("link");

    if (!isOpen) return null;

    const storefrontUrl = `${window.location.origin}/cardapio/${branchId}`;

    const handleCopy = async () => {
        try {
            await navigator.clipboard.writeText(storefrontUrl);
            Swal.fire({
                toast: true,
                position: 'top-end',
                icon: 'success',
                title: 'Link copiado! Pronto para colar no WhatsApp.',
                showConfirmButton: false,
                timer: 2500,
                background: '#1e1e24',
                color: '#fff'
            });
        } catch {
            Swal.fire("Erro", "Não foi possível copiar o link.", "error");
        }
    };

    return (
        <div data-testid="storefront-hub-modal" style={{ position: "fixed", inset: 0, backgroundColor: "rgba(0,0,0,0.85)", zIndex: 9999, display: "flex", alignItems: "center", justifyContent: "center", padding: 16 }}>
            <div style={{ backgroundColor: "#1e1e24", padding: 32, borderRadius: 16, width: "100%", maxWidth: 460, border: "1px solid #323238", boxShadow: "0 10px 30px rgba(0,0,0,0.6)", display: "grid", gap: 20 }}>

                <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
                    <h3 style={{ margin: 0, color: "#fff", fontSize: "1.2rem" }}>📱 Cardápio Digital (Autoatendimento)</h3>
                    <button onClick={onClose} style={{ background: "none", border: "none", color: "#a8a8b3", fontSize: "1.5rem", cursor: "pointer" }}>✕</button>
                </div>

                {/* Abas para alternar entre Copiar Link ou Exibir QR Code Geral do Link */}
                <div style={{ display: "flex", gap: 8, backgroundColor: "#121214", padding: 4, borderRadius: 8 }}>
                    <button
                        onClick={() => setActiveTab("link")}
                        style={{ flex: 1, padding: 10, borderRadius: 6, border: "none", backgroundColor: activeTab === "link" ? "#f59e0b" : "transparent", color: activeTab === "link" ? "#121214" : "#a8a8b3", fontWeight: "bold", cursor: "pointer", fontSize: "0.85rem" }}
                    >
                        📲 Enviar Link (WhatsApp)
                    </button>
                    <button
                        onClick={() => setActiveTab("qrcode")}
                        style={{ flex: 1, padding: 10, borderRadius: 6, border: "none", backgroundColor: activeTab === "qrcode" ? "#f59e0b" : "transparent", color: activeTab === "qrcode" ? "#121214" : "#a8a8b3", fontWeight: "bold", cursor: "pointer", fontSize: "0.85rem" }}
                    >
                        🖨️ QR Code na Tela
                    </button>
                </div>

                {activeTab === "link" ? (
                    <div style={{ display: "grid", gap: 12, textAlign: "center" }}>
                        <p style={{ margin: 0, color: "#a8a8b3", fontSize: "0.9rem", lineHeight: "1.4" }}>
                            Copie o link abaixo e mande para o cliente fazer o pedido direto do celular dele:
                        </p>
                        <input
                            type="text"
                            readOnly
                            value={storefrontUrl}
                            onFocus={(e) => e.target.select()}
                            style={{ width: "100%", padding: "14px", borderRadius: 8, border: "1px solid #323238", backgroundColor: "#121214", color: "#fff", fontSize: "0.9rem", textAlign: "center", outline: "none", boxSizing: "border-box" }}
                        />
                        <button
                            onClick={handleCopy}
                            data-testid="btn-copy-link"
                            style={{ width: "100%", padding: 14, borderRadius: 8, border: "none", backgroundColor: "#f59e0b", color: "#121214", fontWeight: "bold", cursor: "pointer", fontSize: "1rem" }}
                        >
                            📋 Copiar Link do Cardápio
                        </button>
                    </div>
                ) : (
                    <div style={{ display: "grid", gap: 12, textAlign: "center" }}>
                        <p style={{ margin: 0, color: "#a8a8b3", fontSize: "0.9rem", lineHeight: "1.4" }}>
                            Aponte a câmera do celular para o QR Code abaixo para acessar o cardápio:
                        </p>

                        <div style={{ display: "grid", gap: 8, justifyItems: "center" }}>
                            <img
                                src={`https://api.qrserver.com/v1/create-qr-code/?size=180x180&data=${encodeURIComponent(storefrontUrl)}`}
                                alt="QR Code do Cardápio Digital"
                                width={160}
                                height={160}
                                style={{ borderRadius: 8, background: "#fff", padding: 8 }}
                            />
                            <input
                                readOnly
                                value={storefrontUrl}
                                onFocus={(e) => e.target.select()}
                                style={{ width: "100%", padding: "8px", backgroundColor: "#121214", color: "#fff", border: "1px solid #323238", borderRadius: 6, fontSize: "0.8rem", textAlign: "center" }}
                            />
                        </div>
                    </div>
                )}

                <button
                    onClick={onClose}
                    style={{ width: "100%", padding: 12, borderRadius: 8, border: "none", backgroundColor: "#323238", color: "#fff", fontWeight: "bold", cursor: "pointer" }}
                >
                    Fechar
                </button>
            </div>
        </div>
    );
}