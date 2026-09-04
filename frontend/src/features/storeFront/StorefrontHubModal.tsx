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
                title: 'Link copiado! Pronto para enviar.',
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
        <div
            data-testid="storefront-hub-modal"
            style={{ position: "fixed", inset: 0, backgroundColor: "rgba(0,0,0,0.85)", zIndex: 9999, display: "flex", alignItems: "center", justifyContent: "center", padding: 16, backdropFilter: "blur(4px)" }}
            onClick={onClose} // Fecha ao clicar no fundo escuro
        >
            <div
                onClick={(e) => e.stopPropagation()} // Previne que o clique dentro do modal o feche
                style={{ backgroundColor: "#18181b", padding: "28px", borderRadius: "16px", width: "100%", maxWidth: "420px", border: "1px solid #27272a", boxShadow: "0 25px 50px -12px rgba(0,0,0,0.6)", display: "flex", flexDirection: "column", gap: "20px" }}
            >
                {/* Header */}
                <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
                    <h3 style={{ margin: 0, color: "#fff", fontSize: "1.15rem", display: "flex", alignItems: "center", gap: "8px" }}>
                        <span>📱</span> Autoatendimento
                    </h3>
                    <button
                        onClick={onClose}
                        style={{ background: "none", border: "none", color: "#a1a1aa", fontSize: "1.5rem", cursor: "pointer", padding: "0 4px" }}
                        aria-label="Fechar"
                    >
                        ✕
                    </button>
                </div>

                {/* Abas */}
                <div style={{ display: "flex", gap: "4px", backgroundColor: "#09090b", padding: "4px", borderRadius: "8px" }}>
                    <button
                        onClick={() => setActiveTab("link")}
                        style={{ flex: 1, padding: "10px", borderRadius: "6px", border: "none", backgroundColor: activeTab === "link" ? "#f59e0b" : "transparent", color: activeTab === "link" ? "#000" : "#a1a1aa", fontWeight: "bold", cursor: "pointer", fontSize: "0.85rem", transition: "all 0.2s" }}
                    >
                        📲 Enviar Link
                    </button>
                    <button
                        onClick={() => setActiveTab("qrcode")}
                        style={{ flex: 1, padding: "10px", borderRadius: "6px", border: "none", backgroundColor: activeTab === "qrcode" ? "#f59e0b" : "transparent", color: activeTab === "qrcode" ? "#000" : "#a1a1aa", fontWeight: "bold", cursor: "pointer", fontSize: "0.85rem", transition: "all 0.2s" }}
                    >
                        🖨️ QR Code
                    </button>
                </div>

                {/* Conteúdo */}
                {activeTab === "link" ? (
                    <div style={{ display: "flex", flexDirection: "column", gap: "16px", textAlign: "center" }}>
                        <p style={{ margin: 0, color: "#a1a1aa", fontSize: "0.9rem", lineHeight: "1.4" }}>
                            Copie o link abaixo e mande para o cliente fazer o pedido direto do celular dele:
                        </p>
                        <input
                            type="text"
                            readOnly
                            value={storefrontUrl}
                            onFocus={(e) => e.target.select()}
                            style={{ width: "100%", padding: "14px", borderRadius: "8px", border: "1px solid #3f3f46", backgroundColor: "#09090b", color: "#fff", fontSize: "0.9rem", textAlign: "center", outline: "none", boxSizing: "border-box", cursor: "pointer" }}
                        />
                        <button
                            onClick={handleCopy}
                            style={{ width: "100%", padding: "14px", borderRadius: "8px", border: "none", backgroundColor: "#f59e0b", color: "#18181b", fontWeight: "bold", cursor: "pointer", fontSize: "1rem", boxShadow: "0 4px 12px rgba(245, 158, 11, 0.2)" }}
                        >
                            📋 Copiar Link do Cardápio
                        </button>
                    </div>
                ) : (
                    <div style={{ display: "flex", flexDirection: "column", gap: "16px", alignItems: "center", textAlign: "center" }}>
                        <p style={{ margin: 0, color: "#a1a1aa", fontSize: "0.9rem", lineHeight: "1.4" }}>
                            Aponte a câmera para o QR Code abaixo para acessar o cardápio:
                        </p>
                        <div style={{ backgroundColor: "#fff", padding: "12px", borderRadius: "12px", display: "inline-flex" }}>
                            <img
                                src={`https://api.qrserver.com/v1/create-qr-code/?size=180x180&data=${encodeURIComponent(storefrontUrl)}`}
                                alt="QR Code do Cardápio Digital"
                                width={160}
                                height={160}
                                style={{ display: "block" }}
                            />
                        </div>
                        <input
                            readOnly
                            value={storefrontUrl}
                            onFocus={(e) => e.target.select()}
                            style={{ width: "100%", padding: "10px", backgroundColor: "#09090b", color: "#71717a", border: "1px solid #27272a", borderRadius: "6px", fontSize: "0.8rem", textAlign: "center", boxSizing: "border-box", cursor: "pointer" }}
                        />
                    </div>
                )}
            </div>
        </div>
    );
}