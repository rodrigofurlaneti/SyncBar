import { useState } from "react";
import { formatBRL } from "../../lib/types";

export type CartItem = {
    productId: number;
    productName: string;
    salePrice: number;
    quantity: number;
    notes?: string | null;
    imageUrl?: string | null;
    complements?: Array<{ complementId: number; name: string; price: number }>;
};

export type CustomerSessionData = {
    name: string;
    phone?: string;
    customerId?: number;
};

type StorefrontCartDrawerProps = {
    isOpen: boolean;
    onClose: () => void;
    items: CartItem[];
    onUpdateQuantity: (productId: number, newQty: number) => void;
    onRemoveItem: (productId: number) => void;
    onCheckout: (notes: string, customerData?: CustomerSessionData) => void;
    isSubmitting: boolean;
    customerData?: CustomerSessionData | null;
    onOpenAuthModal: () => void;
};

export function StorefrontCartDrawer({
    isOpen,
    onClose,
    items,
    onUpdateQuantity,
    onRemoveItem,
    onCheckout,
    isSubmitting,
    customerData,
    onOpenAuthModal,
}: StorefrontCartDrawerProps) {
    const [generalNotes, setGeneralNotes] = useState("");

    if (!isOpen) return null;

    const subtotal = items.reduce((acc, item) => {
        const complementsTotal = item.complements?.reduce((cAcc, c) => cAcc + c.price, 0) || 0;
        return acc + (item.salePrice + complementsTotal) * item.quantity;
    }, 0);

    const handleCheckoutClick = () => {
        // Se o cliente não estiver logado/identificado, abre o modal de autenticação antes de prosseguir
        if (!customerData || !customerData.customerId) {
            onOpenAuthModal();
            return;
        }
        onCheckout(generalNotes, customerData);
    };

    return (
        <div data-testid="public-cart-drawer" style={{ position: "fixed", inset: 0, backgroundColor: "rgba(0,0,0,0.8)", zIndex: 9999, display: "flex", justifyContent: "flex-end" }}>
            <div style={{ backgroundColor: "#1e1e24", width: "100%", maxWidth: 480, height: "100%", display: "flex", flexDirection: "column", boxShadow: "-5px 0 30px rgba(0,0,0,0.6)", boxSizing: "border-box" }}>

                {/* Header */}
                <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", padding: 20, borderBottom: "1px solid #323238" }}>
                    <h2 style={{ margin: 0, color: "#fff", fontSize: "1.25rem" }}>🛒 Sua Cesta / Carrinho</h2>
                    <button onClick={onClose} data-testid="btn-close-cart" style={{ background: "none", border: "none", color: "#a8a8b3", fontSize: "1.5rem", cursor: "pointer" }}>✕</button>
                </div>

                {/* Customer Identification Banner */}
                <div style={{ padding: "12px 20px", backgroundColor: "#121214", borderBottom: "1px solid #323238", display: "flex", justifyContent: "space-between", alignItems: "center" }}>
                    <div>
                        <span style={{ fontSize: "0.8rem", color: "#a8a8b3", display: "block" }}>Cliente / Entregas:</span>
                        <span style={{ fontSize: "0.9rem", color: customerData?.name ? "#f59e0b" : "#fff", fontWeight: "bold" }}>
                            {customerData?.name ? `👤 ${customerData.name}` : "⚠️ Não identificado"}
                        </span>
                    </div>
                    <button
                        type="button"
                        onClick={onOpenAuthModal}
                        style={{ background: "transparent", border: "1px solid #f59e0b", color: "#f59e0b", padding: "6px 12px", borderRadius: 6, fontSize: "0.8rem", cursor: "pointer", fontWeight: "bold" }}
                    >
                        {customerData?.name ? "Trocar / Editar" : "Identificar-se"}
                    </button>
                </div>

                {/* Items List */}
                <div style={{ flex: 1, overflowY: "auto", padding: 20, display: "grid", gap: 16 }}>
                    {items.length === 0 ? (
                        <p data-testid="empty-cart-msg" style={{ textAlign: "center", color: "#a8a8b3", marginTop: 40 }}>Sua cesta está vazia.</p>
                    ) : (
                        items.map((item) => (
                            <div key={item.productId} data-testid={`cart-item-${item.productId}`} style={{ backgroundColor: "#202024", padding: 16, borderRadius: 8, border: "1px solid #323238", display: "grid", gap: 8 }}>
                                <div style={{ display: "flex", justifyContent: "space-between", alignItems: "flex-start" }}>
                                    <div>
                                        <h4 style={{ margin: 0, color: "#fff", fontSize: "1rem" }}>{item.productName}</h4>
                                        <span style={{ color: "#f59e0b", fontWeight: "bold", fontSize: "0.95rem" }}>{formatBRL(item.salePrice)}</span>
                                    </div>
                                    <button onClick={() => onRemoveItem(item.productId)} data-testid={`btn-remove-${item.productId}`} style={{ background: "none", border: "none", color: "#ef4444", cursor: "pointer", fontSize: "0.9rem" }}>Remover</button>
                                </div>

                                {item.complements && item.complements.length > 0 && (
                                    <div style={{ fontSize: "0.85rem", color: "#8d8d99" }}>
                                        {item.complements.map(c => <div key={c.complementId}>+ {c.name} ({formatBRL(c.price)})</div>)}
                                    </div>
                                )}

                                <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginTop: 8 }}>
                                    <div style={{ display: "flex", alignItems: "center", border: "1px solid #323238", borderRadius: 6, overflow: "hidden", backgroundColor: "#121214" }}>
                                        <button onClick={() => onUpdateQuantity(item.productId, item.quantity - 1)} style={{ width: 32, height: 32, background: "none", border: "none", color: "#a8a8b3", cursor: "pointer" }}>−</button>
                                        <span style={{ width: 28, textAlign: "center", color: "#fff", fontSize: "0.9rem" }}>{item.quantity}</span>
                                        <button onClick={() => onUpdateQuantity(item.productId, item.quantity + 1)} style={{ width: 32, height: 32, background: "none", border: "none", color: "#a8a8b3", cursor: "pointer" }}>+</button>
                                    </div>
                                    <span style={{ color: "#fff", fontWeight: "bold" }}>
                                        {formatBRL((item.salePrice + (item.complements?.reduce((acc, c) => acc + c.price, 0) || 0)) * item.quantity)}
                                    </span>
                                </div>
                            </div>
                        ))
                    )}
                </div>

                {/* Footer & Checkout */}
                {items.length > 0 && (
                    <div style={{ borderTop: "1px solid #323238", padding: 20, backgroundColor: "#18181b", display: "grid", gap: 12 }}>
                        <div>
                            <label style={{ display: "block", color: "#a8a8b3", marginBottom: 6, fontSize: "0.85rem" }}>Observações gerais do pedido</label>
                            <input
                                type="text"
                                value={generalNotes}
                                onChange={(e) => setGeneralNotes(e.target.value)}
                                placeholder="Ex: Sem cebola, caprichar no molho..."
                                data-testid="input-general-notes"
                                style={{ width: "100%", padding: "12px", borderRadius: 8, border: "1px solid #323238", backgroundColor: "#121214", color: "#fff", fontSize: "0.9rem", outline: "none", boxSizing: "border-box" }}
                            />
                        </div>

                        <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
                            <span style={{ color: "#a8a8b3", fontSize: "1.1rem" }}>Total do Pedido</span>
                            <span data-testid="cart-total-amount" style={{ color: "#fff", fontSize: "1.3rem", fontWeight: "bold" }}>{formatBRL(subtotal)}</span>
                        </div>

                        <button
                            type="button"
                            onClick={handleCheckoutClick}
                            disabled={isSubmitting}
                            data-testid="btn-submit-order"
                            style={{ width: "100%", padding: 16, borderRadius: 8, border: "none", backgroundColor: "#f59e0b", color: "#121214", fontWeight: "bold", fontSize: "1rem", cursor: isSubmitting ? "not-allowed" : "pointer", opacity: isSubmitting ? 0.7 : 1 }}
                        >
                            {isSubmitting ? "Enviando Pedido..." : customerData?.customerId ? "Finalizar Pedido" : "Identificar-se e Finalizar"}
                        </button>
                    </div>
                )}
            </div>
        </div>
    );
}