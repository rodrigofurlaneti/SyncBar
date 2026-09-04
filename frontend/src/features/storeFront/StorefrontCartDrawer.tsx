import { useState, useId, useEffect } from "react";
import { formatBRL } from "../../lib/types";
import { getCustomerAddressesByCustomer, CustomerAddressResponse } from "./storefrontApi";

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
    initialStep?: "review" | "delivery"; // Adicionado para receber o passo inicial vindo da página principal
    onUpdateQuantity: (productId: number, newQty: number) => void;
    onRemoveItem: (productId: number) => void;
    onCheckout: (
        notes: string,
        customerData?: CustomerSessionData,
        deliveryType?: "PICKUP" | "DELIVERY",
        addressId?: number | null,
        newAddress?: any,
        paymentMethod?: string
    ) => void;
    isSubmitting: boolean;
    customerData?: CustomerSessionData | null;
    onOpenAuthModal: () => void;
};

export function StorefrontCartDrawer({
    isOpen,
    onClose,
    items,
    initialStep = "review",
    onUpdateQuantity,
    onRemoveItem,
    onCheckout,
    isSubmitting,
    customerData,
    onOpenAuthModal,
}: StorefrontCartDrawerProps) {
    const [generalNotes, setGeneralNotes] = useState("");
    const notesId = useId();

    // Controle de Etapas: Sincronizado com o initialStep
    const [step, setStep] = useState<"review" | "delivery">(initialStep);

    // Estados do fluxo logístico
    const [deliveryType, setDeliveryType] = useState<"PICKUP" | "DELIVERY">("DELIVERY");
    const [addresses, setAddresses] = useState<CustomerAddressResponse[]>([]);
    const [selectedAddressId, setSelectedAddressId] = useState<number | null>(null);
    const [isEditingAddress, setIsEditingAddress] = useState(false);

    // Campos caso precise preencher um novo endereço de entrega
    const [newStreet, setNewStreet] = useState("");
    const [newNumber, setNewNumber] = useState("");
    const [newSupplement, setNewSupplement] = useState("");
    const [newZipCode, setNewZipCode] = useState("");

    const [paymentMethod, setPaymentMethod] = useState<"PIX" | "MAQUININHA">("PIX");
    const [isLoadingAddresses, setIsLoadingAddresses] = useState(false);

    // Sincroniza o passo sempre que o drawer abrir ou o initialStep mudar
    useEffect(() => {
        if (isOpen) {
            setStep(initialStep);
        } else {
            setIsEditingAddress(false);
        }
    }, [isOpen, initialStep]);

    // Busca os endereços cadastrados assim que o usuário vai para a etapa de entrega
    useEffect(() => {
        if (step === "delivery" && customerData?.customerId) {
            setIsLoadingAddresses(true);
            getCustomerAddressesByCustomer(customerData.customerId)
                .then((data) => {
                    setAddresses(data);
                    if (data && data.length > 0) {
                        setSelectedAddressId(data[0].id);
                        setIsEditingAddress(false);
                    } else {
                        setIsEditingAddress(true);
                    }
                })
                .catch(console.error)
                .finally(() => setIsLoadingAddresses(false));
        }
    }, [step, customerData]);

    if (!isOpen) return null;

    const subtotal = items.reduce((acc, item) => {
        const complementsTotal = item.complements?.reduce((cAcc, c) => cAcc + c.price, 0) || 0;
        return acc + (item.salePrice + complementsTotal) * item.quantity;
    }, 0);

    // Função central que controla se vai para a pergunta ou se envia o pedido
    const handleMainActionClick = () => {
        if (!customerData || !customerData.customerId) {
            onOpenAuthModal();
            return;
        }

        // Se está na revisão da cesta, NÃO envia o pedido: para na pergunta de Retirada vs Motoboy
        if (step === "review") {
            setStep("delivery");
            return;
        }

        // Se já está na etapa de entrega/balcão, finaliza e envia
        onCheckout(
            generalNotes,
            customerData!,
            deliveryType,
            deliveryType === "DELIVERY" && !isEditingAddress ? selectedAddressId : null,
            deliveryType === "DELIVERY" && isEditingAddress ? { street: newStreet, number: newNumber, supplement: newSupplement, zipCode: newZipCode } : null,
            paymentMethod
        );
    };

    return (
        <div
            data-testid="public-cart-drawer"
            style={{ position: "fixed", inset: 0, zIndex: 9999, display: "flex", justifyContent: "flex-end", fontFamily: "sans-serif" }}
            role="dialog"
            aria-modal="true"
        >
            {/* Overlay Escurecido */}
            <div
                style={{ position: "absolute", inset: 0, backgroundColor: "rgba(0,0,0,0.75)", backdropFilter: "blur(4px)" }}
                onClick={onClose}
                aria-hidden="true"
            />

            {/* Container do Drawer */}
            <div style={{ position: "relative", display: "flex", flexDirection: "column", height: "100%", width: "100%", maxWidth: "440px", backgroundColor: "#18181b", boxShadow: "-10px 0 30px rgba(0,0,0,0.7)" }}>

                {/* Cabeçalho */}
                <header style={{ display: "flex", alignItems: "center", justifyContent: "space-between", borderBottom: "1px solid #27272a", padding: "20px 24px" }}>
                    <div style={{ display: "flex", alignItems: "center", gap: "12px" }}>
                        {step === "delivery" && (
                            <button onClick={() => setStep("review")} style={{ background: "none", border: "none", color: "#f59e0b", cursor: "pointer", padding: "4px", fontSize: "1.2rem" }}>
                                ←
                            </button>
                        )}
                        <h2 style={{ fontSize: "1.25rem", fontWeight: "bold", color: "#f4f4f5", margin: 0, display: "flex", alignItems: "center", gap: "8px" }}>
                            {step === "review" ? (<span>🛒 Sua Cesta</span>) : (<span>🚚 Modalidade de Entrega</span>)}
                        </h2>
                    </div>
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
                        <span style={{ fontSize: "0.75rem", color: "#a1a1aa" }}>Cliente / Conta:</span>
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

                {/* Conteúdo Dinâmico */}
                <div style={{ flex: 1, overflowY: "auto", padding: "24px", display: "flex", flexDirection: "column", gap: "16px" }}>
                    {step === "review" ? (
                        items.length === 0 ? (
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
                        )
                    ) : (
                        /* ETAPA OBRIGATÓRIA DE PERGUNTA: RETIRADA VS MOTOBOY */
                        <div style={{ display: "flex", flexDirection: "column", gap: "20px" }}>

                            {/* Pergunta principal */}
                            <div style={{ display: "flex", flexDirection: "column", gap: "10px" }}>
                                <span style={{ fontSize: "0.95rem", fontWeight: 600, color: "#f4f4f5" }}>Como deseja receber o pedido?</span>
                                <div style={{ display: "flex", gap: "10px" }}>
                                    <button
                                        type="button"
                                        onClick={() => setDeliveryType("DELIVERY")}
                                        style={{ flex: 1, padding: "12px", borderRadius: "8px", border: deliveryType === "DELIVERY" ? "2px solid #f59e0b" : "1px solid #3f3f46", backgroundColor: deliveryType === "DELIVERY" ? "rgba(245, 158, 11, 0.1)" : "#09090b", color: deliveryType === "DELIVERY" ? "#f59e0b" : "#a1a1aa", fontWeight: "bold", cursor: "pointer", fontSize: "0.85rem" }}
                                    >
                                        🛵 Enviar com Motoboy
                                    </button>
                                    <button
                                        type="button"
                                        onClick={() => setDeliveryType("PICKUP")}
                                        style={{ flex: 1, padding: "12px", borderRadius: "8px", border: deliveryType === "PICKUP" ? "2px solid #f59e0b" : "1px solid #3f3f46", backgroundColor: deliveryType === "PICKUP" ? "rgba(245, 158, 11, 0.1)" : "#09090b", color: deliveryType === "PICKUP" ? "#f59e0b" : "#a1a1aa", fontWeight: "bold", cursor: "pointer", fontSize: "0.85rem" }}
                                    >
                                        🏬 Retirar no Balcão
                                    </button>
                                </div>
                            </div>

                            {/* Se for Motoboy, valida se mantém o endereço cadastrado ou preenche novo */}
                            {deliveryType === "DELIVERY" && (
                                <div style={{ display: "flex", flexDirection: "column", gap: "10px", borderTop: "1px solid #27272a", paddingTop: "16px" }}>
                                    <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
                                        <span style={{ fontSize: "0.95rem", fontWeight: 600, color: "#f4f4f5" }}>Endereço de Entrega</span>
                                        {!isEditingAddress && addresses.length > 0 && (
                                            <button
                                                type="button"
                                                onClick={() => setIsEditingAddress(true)}
                                                style={{ background: "none", border: "none", color: "#f59e0b", fontSize: "0.8rem", cursor: "pointer", textDecoration: "underline" }}
                                            >
                                                Cadastrar / Usar outro
                                            </button>
                                        )}
                                    </div>

                                    {isLoadingAddresses ? (
                                        <span style={{ color: "#a1a1aa", fontSize: "0.85rem" }}>Verificando endereço cadastrado...</span>
                                    ) : !isEditingAddress && addresses.length > 0 ? (
                                        <div style={{ padding: "14px", borderRadius: "10px", border: "1px solid #f59e0b", backgroundColor: "rgba(245, 158, 11, 0.05)" }}>
                                            <div style={{ fontSize: "0.75rem", color: "#a1a1aa" }}>Endereço atualmente cadastrado:</div>
                                            <div style={{ fontWeight: "bold", color: "#f4f4f5", marginTop: "4px" }}>📍 {addresses[0].street}, {addresses[0].number}</div>
                                            <div style={{ fontSize: "0.8rem", color: "#a1a1aa", marginTop: "2px" }}>CEP: {addresses[0].zipCode} {addresses[0].supplement ? `(${addresses[0].supplement})` : ""}</div>
                                        </div>
                                    ) : (
                                        <div style={{ display: "flex", flexDirection: "column", gap: "10px", backgroundColor: "#09090b", padding: "12px", borderRadius: "10px", border: "1px solid #3f3f46" }}>
                                            <div style={{ fontSize: "0.8rem", color: "#f59e0b", fontWeight: "bold" }}>Informe o endereço de entrega:</div>
                                            <input
                                                type="text"
                                                placeholder="CEP (somente números)"
                                                value={newZipCode}
                                                onChange={(e) => setNewZipCode(e.target.value.replace(/\D/g, ''))}
                                                style={{ padding: "10px", borderRadius: "6px", backgroundColor: "#18181b", border: "1px solid #3f3f46", color: "#fff", fontSize: "0.85rem" }}
                                            />
                                            <div style={{ display: "flex", gap: "8px" }}>
                                                <input
                                                    type="text"
                                                    placeholder="Rua / Avenida"
                                                    value={newStreet}
                                                    onChange={(e) => setNewStreet(e.target.value)}
                                                    style={{ flex: 3, padding: "10px", borderRadius: "6px", backgroundColor: "#18181b", border: "1px solid #3f3f46", color: "#fff", fontSize: "0.85rem" }}
                                                />
                                                <input
                                                    type="text"
                                                    placeholder="Número"
                                                    value={newNumber}
                                                    onChange={(e) => setNewNumber(e.target.value)}
                                                    style={{ flex: 1, padding: "10px", borderRadius: "6px", backgroundColor: "#18181b", border: "1px solid #3f3f46", color: "#fff", fontSize: "0.85rem" }}
                                                />
                                            </div>
                                            <input
                                                type="text"
                                                placeholder="Complemento / Bairro"
                                                value={newSupplement}
                                                onChange={(e) => setNewSupplement(e.target.value)}
                                                style={{ padding: "10px", borderRadius: "6px", backgroundColor: "#18181b", border: "1px solid #3f3f46", color: "#fff", fontSize: "0.85rem" }}
                                            />

                                            {addresses.length > 0 && (
                                                <button
                                                    type="button"
                                                    onClick={() => setIsEditingAddress(false)}
                                                    style={{ background: "none", border: "none", color: "#a1a1aa", fontSize: "0.75rem", cursor: "pointer", textAlign: "left", marginTop: "2px" }}
                                                >
                                                    ← Usar meu endereço cadastrado
                                                </button>
                                            )}
                                        </div>
                                    )}
                                </div>
                            )}

                            {/* Forma de Pagamento */}
                            <div style={{ display: "flex", flexDirection: "column", gap: "10px", borderTop: "1px solid #27272a", paddingTop: "16px" }}>
                                <span style={{ fontSize: "0.95rem", fontWeight: 600, color: "#f4f4f5" }}>Forma de Pagamento</span>
                                <div style={{ display: "flex", gap: "10px" }}>
                                    <button
                                        type="button"
                                        onClick={() => setPaymentMethod("PIX")}
                                        style={{ flex: 1, padding: "10px", borderRadius: "8px", border: paymentMethod === "PIX" ? "2px solid #10b981" : "1px solid #3f3f46", backgroundColor: paymentMethod === "PIX" ? "rgba(16, 185, 129, 0.1)" : "#09090b", color: paymentMethod === "PIX" ? "#10b981" : "#a1a1aa", fontWeight: "bold", cursor: "pointer", fontSize: "0.85rem" }}
                                    >
                                        PIX (Online)
                                    </button>
                                    <button
                                        type="button"
                                        onClick={() => setPaymentMethod("MAQUININHA")}
                                        style={{ flex: 1, padding: "10px", borderRadius: "8px", border: paymentMethod === "MAQUININHA" ? "2px solid #f59e0b" : "1px solid #3f3f46", backgroundColor: paymentMethod === "MAQUININHA" ? "rgba(245, 158, 11, 0.1)" : "#09090b", color: paymentMethod === "MAQUININHA" ? "#f59e0b" : "#a1a1aa", fontWeight: "bold", cursor: "pointer", fontSize: "0.85rem" }}
                                    >
                                        Maquininha
                                    </button>
                                </div>
                            </div>
                        </div>
                    )}
                </div>

                {/* Footer Fixo */}
                {items.length > 0 && (
                    <footer style={{ borderTop: "1px solid #27272a", backgroundColor: "#09090b", padding: "24px" }}>
                        <div style={{ display: "flex", flexDirection: "column", gap: "16px" }}>

                            {step === "review" && (
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
                            )}

                            <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between" }}>
                                <span style={{ fontSize: "0.95rem", fontWeight: 500, color: "#a1a1aa" }}>{step === "review" ? "Total do Pedido" : "Total a Pagar"}</span>
                                <span data-testid="cart-total-amount" style={{ fontSize: "1.35rem", fontWeight: "bold", color: "#f4f4f5" }}>
                                    {formatBRL(subtotal)}
                                </span>
                            </div>

                            <button
                                type="button"
                                onClick={handleMainActionClick}
                                disabled={isSubmitting}
                                data-testid="btn-submit-order"
                                style={{ width: "100%", borderRadius: "12px", backgroundColor: "#f59e0b", padding: "14px", fontSize: "1rem", fontWeight: "bold", color: "#18181b", border: "none", cursor: isSubmitting ? "not-allowed" : "pointer", opacity: isSubmitting ? 0.7 : 1 }}
                            >
                                {isSubmitting
                                    ? "Processando..."
                                    : (step === "review"
                                        ? (customerData?.customerId ? "Avançar para Opções de Entrega" : "Identificar-se para Continuar")
                                        : "Confirmar e Enviar Pedido")}
                            </button>
                        </div>
                    </footer>
                )}
            </div>
        </div>
    );
}