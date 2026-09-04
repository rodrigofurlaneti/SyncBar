import { useState, useId } from "react";
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
    const notesId = useId();

    if (!isOpen) return null;

    const subtotal = items.reduce((acc, item) => {
        const complementsTotal = item.complements?.reduce((cAcc, c) => cAcc + c.price, 0) || 0;
        return acc + (item.salePrice + complementsTotal) * item.quantity;
    }, 0);

    const handleCheckoutClick = () => {
        if (!customerData || !customerData.customerId) {
            onOpenAuthModal();
            return;
        }
        onCheckout(generalNotes, customerData);
    };

    return (
        <div
            data-testid="public-cart-drawer"
            style={{ position: "fixed", inset: 0, zIndex: 9999, display: "flex", justifyContent: "flex-end", fontFamily: "sans-serif" }}
            role="dialog"
            aria-modal="true"
        >
            {/* Overlay Escurecido (clicável para fechar) */}
            <div
                style={{ position: "absolute", inset: 0, backgroundColor: "rgba(0,0,0,0.75)", backdropFilter: "blur(4px)" }}
                onClick={onClose}
                aria-hidden="true"
            />

            {/* Container do Drawer */}
            <div style={{ position: "relative", display: "flex", flexDirection: "column", height: "100%", width: "100%", maxWidth: "440px", backgroundColor: "#18181b", boxShadow: "-10px 0 30px rgba(0,0,0,0.7)" }}>

                {/* Cabeçalho */}
                <header style={{ display: "flex", alignItems: "center", justifyContent: "space-between", borderBottom: "1px solid #27272a", padding: "20px 24px" }}>
                    <h2 style={{ fontSize: "1.25rem", fontWeight: "bold", color: "#f4f4f5", margin: 0, display: "flex", alignItems: "center", gap: "8px" }}>
                        <span>🛒</span> Sua Cesta
                    </h2>
                    <button
                        onClick={onClose}
                        data-testid="btn-close-cart"
                        aria-label="Fechar carrinho"
                        style={{ background: "none", border: "none", padding: "8px", color: "#a1a1aa", cursor: "pointer", borderRadius: "6px" }}
                    >
                        <svg style={{ height: "24px", width: "24px" }} fill="none" viewBox="0 0 24 24" stroke="currentColor">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                        </svg>
                    </button>
                </header>

                {/* Banner de Identificação do Cliente */}
                <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", borderBottom: "1px solid #27272a", backgroundColor: "rgba(9, 9, 11, 0.5)", padding: "12px 24px" }}>
                    <div style={{ display: "flex", flexDirection: "column" }}>
                        <span style={{ fontSize: "0.75rem", color: "#a1a1aa" }}>Cliente / Entrega:</span>
                        <span style={{ fontSize: "0.875rem", fontWeight: "bold", color: customerData?.name ? "#f59e0b" : "#f4f4f5" }}>
                            {customerData?.name ? `👤 ${customerData.name}` : "⚠️ Não identificado"}
                        </span>
                    </div>
                    <button
                        type="button"
                        onClick={onOpenAuthModal}
                        style={{ borderRadius: "8px", border: "1px solid rgba(245, 158, 11, 0.5)", backgroundColor: "rgba(245, 158, 11, 0.1)", padding: "6px 12px", fontSize: "0.75rem", fontWeight: "bold", color: "#f59e0b", cursor: "pointer" }}
                    >
                        {customerData?.name ? "Trocar / Editar" : "Identificar-se"}
                    </button>
                </div>

                {/* Lista de Itens (Scrollable) */}
                <div style={{ flex: 1, overflowY: "auto", padding: "24px", display: "flex", flexDirection: "column", gap: "16px" }}>
                    {items.length === 0 ? (
                        <div data-testid="empty-cart-msg" style={{ display: "flex", height: "100%", flexDirection: "column", alignItems: "center", justifyContent: "center", gap: "16px", textAlign: "center", color: "#71717a" }}>
                            <svg style={{ height: "64px", width: "64px", opacity: 0.5 }} fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1} d="M16 11V7a4 4 0 00-8 0v4M5 9h14l1 12H4L5 9z" />
                            </svg>
                            <p style={{ fontSize: "1rem", fontWeight: 500, margin: 0 }}>Sua cesta está vazia.</p>
                        </div>
                    ) : (
                        items.map((item) => {
                            const itemTotal = (item.salePrice + (item.complements?.reduce((acc, c) => acc + c.price, 0) || 0)) * item.quantity;

                            return (
                                <div key={item.productId} data-testid={`cart-item-${item.productId}`} style={{ display: "flex", flexDirection: "column", gap: "12px", borderRadius: "12px", border: "1px solid #27272a", backgroundColor: "rgba(39, 39, 42, 0.3)", padding: "16px" }}>

                                    <div style={{ display: "flex", alignItems: "flex-start", justifyContent: "space-between", gap: "16px" }}>
                                        <div style={{ flex: 1 }}>
                                            <h4 style={{ margin: 0, fontSize: "1rem", fontWeight: 600, color: "#f4f4f5", lineHeight: 1.2 }}>{item.productName}</h4>
                                            <span style={{ marginTop: "4px", display: "block", fontSize: "0.875rem", fontWeight: "bold", color: "#f59e0b" }}>{formatBRL(item.salePrice)}</span>
                                        </div>
                                        <button
                                            onClick={() => onRemoveItem(item.productId)}
                                            data-testid={`btn-remove-${item.productId}`}
                                            aria-label={`Remover ${item.productName}`}
                                            style={{ background: "none", border: "none", fontSize: "0.75rem", fontWeight: 600, color: "#f87171", cursor: "pointer", padding: 0 }}
                                        >
                                            Remover
                                        </button>
                                    </div>

                                    {/* Complementos */}
                                    {item.complements && item.complements.length > 0 && (
                                        <div style={{ display: "flex", flexDirection: "column", gap: "4px", borderLeft: "2px solid #3f3f46", paddingLeft: "12px", fontSize: "0.875rem", color: "#a1a1aa" }}>
                                            {item.complements.map(c => (
                                                <div key={c.complementId} style={{ display: "flex", justifyContent: "space-between" }}>
                                                    <span>+ {c.name}</span>
                                                    <span>{formatBRL(c.price)}</span>
                                                </div>
                                            ))}
                                        </div>
                                    )}

                                    {/* Controles de Quantidade e Preço Final */}
                                    <div style={{ marginTop: "8px", display: "flex", alignItems: "center", justifyContent: "space-between" }}>
                                        <div style={{ display: "flex", height: "40px", alignItems: "center", overflow: "hidden", borderRadius: "8px", border: "1px solid #3f3f46", backgroundColor: "#18181b" }}>
                                            <button
                                                onClick={() => onUpdateQuantity(item.productId, item.quantity - 1)}
                                                aria-label="Diminuir quantidade"
                                                style={{ display: "flex", height: "100%", width: "40px", alignItems: "center", justifyContent: "center", border: "none", background: "none", color: "#a1a1aa", cursor: "pointer", fontSize: "1.2rem" }}
                                            >
                                                −
                                            </button>
                                            <span style={{ display: "flex", width: "32px", alignItems: "center", justifyContent: "center", fontSize: "0.875rem", fontWeight: 500, color: "#f4f4f5" }}>
                                                {item.quantity}
                                            </span>
                                            <button
                                                onClick={() => onUpdateQuantity(item.productId, item.quantity + 1)}
                                                aria-label="Aumentar quantidade"
                                                style={{ display: "flex", height: "100%", width: "40px", alignItems: "center", justifyContent: "center", border: "none", background: "none", color: "#a1a1aa", cursor: "pointer", fontSize: "1.2rem" }}
                                            >
                                                +
                                            </button>
                                        </div>
                                        <span style={{ fontSize: "1rem", fontWeight: "bold", color: "#f4f4f5" }}>
                                            {formatBRL(itemTotal)}
                                        </span>
                                    </div>
                                </div>
                            );
                        })
                    )}
                </div>

                {/* Footer Fixo: Observações e Checkout */}
                {items.length > 0 && (
                    <footer style={{ borderTop: "1px solid #27272a", backgroundColor: "#09090b", padding: "24px" }}>
                        <div style={{ display: "flex", flexDirection: "column", gap: "20px" }}>

                            <div style={{ display: "flex", flexDirection: "column", gap: "6px" }}>
                                <label htmlFor={notesId} style={{ fontSize: "0.875rem", fontWeight: 500, color: "#a1a1aa" }}>
                                    Observações gerais do pedido
                                </label>
                                <input
                                    id={notesId}
                                    type="text"
                                    value={generalNotes}
                                    onChange={(e) => setGeneralNotes(e.target.value)}
                                    placeholder="Ex: Sem cebola, caprichar no molho..."
                                    data-testid="input-general-notes"
                                    style={{ width: "100%", borderRadius: "8px", border: "1px solid #27272a", backgroundColor: "#18181b", padding: "12px 16px", fontSize: "0.875rem", color: "#f4f4f5", outline: "none", boxSizing: "border-box" }}
                                />
                            </div>

                            <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between" }}>
                                <span style={{ fontSize: "1rem", fontWeight: 500, color: "#a1a1aa" }}>Total do Pedido</span>
                                <span data-testid="cart-total-amount" style={{ fontSize: "1.5rem", fontWeight: "bold", color: "#f4f4f5" }}>
                                    {formatBRL(subtotal)}
                                </span>
                            </div>

                            <button
                                type="button"
                                onClick={handleCheckoutClick}
                                disabled={isSubmitting}
                                data-testid="btn-submit-order"
                                style={{ width: "100%", borderRadius: "12px", backgroundColor: "#f59e0b", padding: "16px", fontSize: "1rem", fontWeight: "bold", color: "#18181b", border: "none", cursor: isSubmitting ? "not-allowed" : "pointer", opacity: isSubmitting ? 0.7 : 1, boxShadow: "0 10px 15px -3px rgba(245, 158, 11, 0.2)" }}
                            >
                                {isSubmitting ? "Processando..." : customerData?.customerId ? "Finalizar Pedido" : "Identificar-se e Finalizar"}
                            </button>
                        </div>
                    </footer>
                )}
            </div>
        </div>
    );
}