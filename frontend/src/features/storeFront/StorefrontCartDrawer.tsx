import { useState, useId, useEffect, useMemo, useCallback } from "react";
import Swal from "sweetalert2";
import { formatBRL } from "../../lib/types";
import { getCustomerAddressesByCustomer, CustomerAddressResponse, registerCustomerAddress } from "./storefrontApi";

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

export type PaymentMethod = "PIX" | "MAQUININHA" | "CREDITO" | "BOLETO";

export type NewCardData = {
    holderName: string;
    number: string;
    expiryMonth: string;
    expiryYear: string;
    ccv: string;
    saveCard: boolean;
};

type StorefrontCartDrawerProps = {
    isOpen: boolean;
    onClose: () => void;
    items: CartItem[];
    initialStep?: "review" | "delivery";
    onUpdateQuantity: (productId: number, newQty: number) => void;
    onRemoveItem: (productId: number) => void;
    onCheckout: (
        notes: string,
        customerData?: CustomerSessionData,
        deliveryType?: "PICKUP" | "DELIVERY",
        addressId?: number | null,
        newAddress?: any,
        paymentMethod?: PaymentMethod,
        cardData?: NewCardData
    ) => void;
    isSubmitting: boolean;
    customerData?: CustomerSessionData | null;
    onOpenAuthModal: () => void;
};

const styles = `
  .drawer-wrapper { position: fixed; inset: 0; z-index: 9999; display: flex; justify-content: flex-end; font-family: system-ui, -apple-system, sans-serif; animation: fadeIn 0.2s ease-out; }
  .drawer-backdrop { position: absolute; inset: 0; background-color: rgba(0, 0, 0, 0.75); backdrop-filter: blur(0.25rem); }
  .drawer-container { position: relative; width: 100%; max-width: 27.5rem; height: 100%; background-color: #18181b; box-shadow: -0.625rem 0 1.875rem rgba(0, 0, 0, 0.7); display: flex; flex-direction: column; animation: slideInRight 0.3s cubic-bezier(0.16, 1, 0.3, 1); }
  .drawer-header { display: flex; align-items: center; justify-content: space-between; border-bottom: 0.0625rem solid #27272a; padding: 1.25rem 1.5rem; }
  .drawer-title { font-size: 1.25rem; font-weight: bold; color: #f4f4f5; margin: 0; display: flex; align-items: center; gap: 0.5rem; }
  .btn-icon { background: none; border: none; color: #a1a1aa; cursor: pointer; padding: 0.5rem; border-radius: 0.375rem; display: flex; align-items: center; justify-content: center; transition: all 0.2s ease; }
  .btn-icon:hover { color: #fff; background-color: #27272a; }
  .btn-icon:focus-visible { outline: 0.125rem solid #f59e0b; outline-offset: 0.125rem; }
  .customer-banner { display: flex; align-items: center; justify-content: space-between; border-bottom: 0.0625rem solid #27272a; background-color: rgba(9, 9, 11, 0.5); padding: 0.75rem 1.5rem; }
  .drawer-body { flex: 1; overflow-y: auto; padding: 1.5rem; display: flex; flex-direction: column; gap: 1rem; }
  
  .cart-item-card { display: flex; flex-direction: column; gap: 0.75rem; border-radius: 0.75rem; border: 0.0625rem solid #27272a; background-color: rgba(39, 39, 42, 0.3); padding: 1rem; transition: border-color 0.2s ease; }
  .cart-item-card:hover { border-color: #3f3f46; }
  
  .qty-control { display: flex; height: 2.75rem; align-items: center; overflow: hidden; border-radius: 0.5rem; border: 0.0625rem solid #3f3f46; background-color: #18181b; }
  .qty-btn { display: flex; height: 100%; width: 2.75rem; align-items: center; justify-content: center; border: none; background: none; color: #a1a1aa; cursor: pointer; font-size: 1.2rem; transition: background-color 0.2s ease, color 0.2s ease; }
  .qty-btn:hover:not(:disabled) { background-color: #27272a; color: #fff; }
  .qty-btn:focus-visible { outline: 0.125rem solid #f59e0b; outline-offset: -0.125rem; }
  
  .selection-btn { width: 100%; padding: 0.75rem 0.5rem; border-radius: 0.5rem; font-weight: bold; cursor: pointer; font-size: 0.85rem; transition: all 0.2s ease; border: 0.0625rem solid #3f3f46; background-color: #09090b; color: #a1a1aa; display: flex; align-items: center; justify-content: center; text-align: center; }
  .selection-btn[aria-pressed="true"] { border-color: #f59e0b; background-color: rgba(245, 158, 11, 0.1); color: #f59e0b; }
  .selection-btn:focus-visible { outline: 0.125rem solid #f59e0b; outline-offset: 0.125rem; }
  
  .address-card { width: 100%; text-align: left; padding: 0.875rem; border-radius: 0.625rem; border: 0.0625rem solid #3f3f46; background-color: #18181b; cursor: pointer; transition: all 0.2s ease; display: flex; flex-direction: column; gap: 0.25rem; }
  .address-card:hover { border-color: #f59e0b; }
  .address-card[aria-checked="true"] { border: 0.125rem solid #f59e0b; background-color: rgba(245, 158, 11, 0.05); padding: 0.8125rem; /* Ajuste para não dar salto visual devido à borda maior */ }
  .address-card:focus-visible { outline: 0.125rem solid #f59e0b; outline-offset: 0.125rem; }

  .input-group { display: flex; flex-direction: column; gap: 0.375rem; width: 100%; }
  .input-label { font-size: 0.875rem; font-weight: 500; color: #a1a1aa; }
  .form-input { width: 100%; padding: 0.75rem; border-radius: 0.375rem; background-color: #18181b; border: 0.0625rem solid #3f3f46; color: #fff; font-size: 0.875rem; transition: border-color 0.2s ease; box-sizing: border-box; outline: none; }
  .form-input:focus:not(:disabled) { border-color: #f59e0b; }
  .form-input:disabled { opacity: 0.5; cursor: not-allowed; }
  .form-input:read-only { background-color: #27272a; color: #a1a1aa; border-color: #27272a; cursor: not-allowed; opacity: 0.8; }
  .form-input:read-only:focus { border-color: #27272a; outline: none; }

  .btn-primary { width: 100%; border-radius: 0.75rem; background-color: #f59e0b; padding: 0.875rem; font-size: 1rem; font-weight: bold; color: #18181b; border: none; cursor: pointer; transition: all 0.2s ease; }
  .btn-primary:hover:not(:disabled) { background-color: #d97706; transform: translateY(-0.0625rem); }
  .btn-primary:active:not(:disabled) { transform: translateY(0); }
  .btn-primary:disabled { cursor: not-allowed; opacity: 0.7; filter: grayscale(0.5); }
  .btn-primary:focus-visible { outline: 0.125rem solid #fff; outline-offset: 0.125rem; }
  .btn-secondary { margin-top: 0.5rem; width: 100%; border-radius: 0.5rem; background-color: rgba(245, 158, 11, 0.1); border: 0.0625rem solid #f59e0b; color: #f59e0b; font-weight: bold; padding: 0.75rem; font-size: 0.875rem; cursor: pointer; transition: all 0.2s ease; }
  .btn-secondary:hover:not(:disabled) { background-color: rgba(245, 158, 11, 0.2); }
  .btn-secondary:disabled { opacity: 0.6; cursor: not-allowed; }

  .address-skeleton { display: flex; flex-direction: column; gap: 0.5rem; padding: 0.875rem; border-radius: 0.625rem; background-color: rgba(39, 39, 42, 0.3); }
  .skeleton-line { height: 0.75rem; background: linear-gradient(90deg, #27272a 25%, #3f3f46 50%, #27272a 75%); background-size: 200% 100%; animation: shimmer 1.5s infinite; border-radius: 0.25rem; }
  .drawer-footer { border-top: 0.0625rem solid #27272a; background-color: #09090b; padding: 1.5rem; }
  
  @keyframes fadeIn { from { opacity: 0; } to { opacity: 1; } }
  @keyframes slideInRight { from { transform: translateX(100%); } to { transform: translateX(0); } }
  @keyframes shimmer { 0% { background-position: 200% 0; } 100% { background-position: -200% 0; } }
`;

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
    const zipCodeId = useId();
    const streetId = useId();
    const numberId = useId();
    const supplementId = useId();

    const [step, setStep] = useState<"review" | "delivery">(initialStep);
    const [deliveryType, setDeliveryType] = useState<"PICKUP" | "DELIVERY">("DELIVERY");

    const [addresses, setAddresses] = useState<CustomerAddressResponse[]>([]);
    const [selectedAddressId, setSelectedAddressId] = useState<number | null>(null);
    const [isEditingAddress, setIsEditingAddress] = useState(false);

    const [newStreet, setNewStreet] = useState("");
    const [newNumber, setNewNumber] = useState("");
    const [newSupplement, setNewSupplement] = useState("");
    const [newNeighborhood, setNewNeighborhood] = useState("");
    const [newZipCode, setNewZipCode] = useState("");

    const [paymentMethod, setPaymentMethod] = useState<PaymentMethod>("PIX");
    const [isLoadingAddresses, setIsLoadingAddresses] = useState(false);
    const [isProcessingAddress, setIsProcessingAddress] = useState(false);
    const [isFetchingCep, setIsFetchingCep] = useState(false);

    const [cardHolderName, setCardHolderName] = useState("");
    const [cardNumber, setCardNumber] = useState("");
    const [cardExpiryMonth, setCardExpiryMonth] = useState("");
    const [cardExpiryYear, setCardExpiryYear] = useState("");
    const [cardCcv, setCardCcv] = useState("");
    const [saveCard, setSaveCard] = useState(false);
    const cardHolderId = useId();
    const cardNumberId = useId();
    const cardExpiryMonthId = useId();
    const cardExpiryYearId = useId();
    const cardCcvId = useId();

    useEffect(() => {
        const handleKeyDown = (e: KeyboardEvent) => {
            if (e.key === "Escape" && isOpen) onClose();
        };

        if (isOpen) {
            window.addEventListener("keydown", handleKeyDown);
            document.body.style.overflow = "hidden";
            setStep(initialStep);
        }

        return () => {
            window.removeEventListener("keydown", handleKeyDown);
            document.body.style.overflow = "unset";
            if (!isOpen) setIsEditingAddress(false);
        };
    }, [isOpen, initialStep, onClose]);

    useEffect(() => {
        if (step === "delivery" && customerData?.customerId) {
            setIsLoadingAddresses(true);
            getCustomerAddressesByCustomer(customerData.customerId)
                .then((data) => {
                    // Opcional: ordenar para mostrar os mais recentes primeiro
                    const sortedData = data.sort((a, b) => b.id - a.id);
                    setAddresses(sortedData);

                    if (sortedData.length > 0) {
                        setSelectedAddressId(sortedData[0].id);
                        setIsEditingAddress(false);
                    } else {
                        setIsEditingAddress(true);
                    }
                })
                .catch(console.error)
                .finally(() => setIsLoadingAddresses(false));
        }
    }, [step, customerData]);

    const subtotal = useMemo(() => {
        return items.reduce((acc, item) => {
            const complementsTotal = item.complements?.reduce((cAcc, c) => cAcc + c.price, 0) || 0;
            return acc + (item.salePrice + complementsTotal) * item.quantity;
        }, 0);
    }, [items]);

    const handleCepChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
        const cep = e.target.value.replace(/\D/g, '').slice(0, 8);
        setNewZipCode(cep);

        if (cep.length === 8) {
            setIsFetchingCep(true);
            try {
                const response = await fetch(`https://viacep.com.br/ws/${cep}/json/`);
                const data = await response.json();

                if (!data.erro) {
                    setNewStreet(data.logradouro || "");
                    setNewNeighborhood(data.bairro ? `${data.bairro}, ${data.localidade} - ${data.uf}` : "");
                    document.getElementById(numberId)?.focus();
                } else {
                    Swal.fire({ toast: true, position: 'top-end', icon: 'error', title: 'CEP não encontrado', showConfirmButton: false, timer: 2000, background: '#18181b', color: '#fff' });
                    setNewStreet("");
                    setNewNeighborhood("");
                }
            } catch (error) {
                console.error("Erro na busca do CEP:", error);
            } finally {
                setIsFetchingCep(false);
            }
        }
    };

    const executeSaveAddress = async (): Promise<number> => {
        const fullSupplementInfo = newSupplement
            ? `${newSupplement} - ${newNeighborhood}`
            : newNeighborhood;

        const payload = {
            companyId: 1,
            customerId: customerData!.customerId!,
            street: newStreet,
            number: newNumber,
            supplement: fullSupplementInfo,
            zipCode: newZipCode,
        };
        const result = await registerCustomerAddress(payload);
        const updatedAddresses = await getCustomerAddressesByCustomer(customerData!.customerId!);
        const sortedData = updatedAddresses.sort((a, b) => b.id - a.id);

        setAddresses(sortedData);
        setSelectedAddressId(result.id);
        setIsEditingAddress(false);

        // Limpar formulário de novo endereço
        setNewZipCode(""); setNewStreet(""); setNewNumber(""); setNewSupplement(""); setNewNeighborhood("");
        return result.id;
    };

    const handleMainActionClick = useCallback(async () => {
        if (!customerData || !customerData.customerId) {
            onOpenAuthModal();
            return;
        }

        if (step === "review") {
            setStep("delivery");
            return;
        }

        let currentAddressIdToSubmit = selectedAddressId;

        if (deliveryType === "DELIVERY" && isEditingAddress) {
            if (!newZipCode || !newStreet || !newNumber) {
                Swal.fire({ title: "Atenção", text: "Preencha o CEP, Rua e Número para entrega.", icon: "warning", background: '#18181b', color: '#fff' });
                return;
            }

            setIsProcessingAddress(true);
            try {
                currentAddressIdToSubmit = await executeSaveAddress();
            } catch (e) {
                Swal.fire({ title: "Erro", text: "Falha ao registrar o novo endereço.", icon: "error", background: '#18181b', color: '#fff' });
                setIsProcessingAddress(false);
                return;
            }
            setIsProcessingAddress(false);
        }

        if (paymentMethod === "CREDITO") {
            if (!cardHolderName || !cardNumber || !cardExpiryMonth || !cardExpiryYear || !cardCcv) {
                Swal.fire({ title: "Atenção", text: "Preencha todos os dados do cartão.", icon: "warning", background: '#18181b', color: '#fff' });
                return;
            }
        }

        onCheckout(
            generalNotes,
            customerData,
            deliveryType,
            deliveryType === "DELIVERY" ? currentAddressIdToSubmit : null,
            null,
            paymentMethod,
            paymentMethod === "CREDITO"
                ? {
                    holderName: cardHolderName,
                    number: cardNumber,
                    expiryMonth: cardExpiryMonth,
                    expiryYear: cardExpiryYear,
                    ccv: cardCcv,
                    saveCard,
                }
                : undefined
        );
    }, [customerData, step, onCheckout, generalNotes, deliveryType, isEditingAddress, selectedAddressId, paymentMethod, newZipCode, newStreet, newNumber, newSupplement, newNeighborhood, onOpenAuthModal, cardHolderName, cardNumber, cardExpiryMonth, cardExpiryYear, cardCcv, saveCard]);

    if (!isOpen) return null;

    return (
        <div
            data-testid="public-cart-drawer"
            className="drawer-wrapper"
            role="dialog"
            aria-modal="true"
            aria-labelledby="drawer-title"
        >
            <style>{styles}</style>

            <div className="drawer-backdrop" aria-hidden="true" onClick={onClose} />

            <aside className="drawer-container">
                <header className="drawer-header">
                    <div style={{ display: "flex", alignItems: "center", gap: "0.75rem" }}>
                        {step === "delivery" && (
                            <button
                                onClick={() => setStep("review")}
                                className="btn-icon"
                                aria-label="Voltar para a cesta"
                                style={{ color: "#f59e0b", padding: "0.25rem" }}
                            >
                                <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M19 12H5M12 19l-7-7 7-7" /></svg>
                            </button>
                        )}
                        <h2 id="drawer-title" className="drawer-title">
                            {step === "review" ? (
                                <><span aria-hidden="true">🛒</span> Sua Cesta</>
                            ) : (
                                <><span aria-hidden="true">🚚</span> Modalidade de Entrega</>
                            )}
                        </h2>
                    </div>
                    <button
                        onClick={onClose}
                        className="btn-icon"
                        data-testid="btn-close-cart"
                        aria-label="Fechar carrinho"
                    >
                        <svg width="24" height="24" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                        </svg>
                    </button>
                </header>

                <div className="customer-banner">
                    <div style={{ display: "flex", flexDirection: "column" }}>
                        <span style={{ fontSize: "0.75rem", color: "#a1a1aa" }}>Cliente / Conta:</span>
                        <span style={{ fontSize: "0.875rem", fontWeight: "bold", color: customerData?.name ? "#f59e0b" : "#f4f4f5" }}>
                            {customerData?.name ? `👤 ${customerData.name}` : "⚠️ Não identificado"}
                        </span>
                    </div>
                    <button
                        type="button"
                        onClick={onOpenAuthModal}
                        style={{ borderRadius: "0.5rem", border: "0.0625rem solid rgba(245, 158, 11, 0.5)", background: "rgba(245, 158, 11, 0.1)", padding: "0.375rem 0.75rem", fontSize: "0.75rem", fontWeight: "bold", color: "#f59e0b", cursor: "pointer", transition: "all 0.2s ease" }}
                        aria-label={customerData?.name ? "Trocar conta ou editar identificação" : "Identificar-se no sistema"}
                    >
                        {customerData?.name ? "Trocar / Editar" : "Identificar-se"}
                    </button>
                </div>

                <section className="drawer-body" aria-live="polite">
                    {step === "review" ? (
                        items.length === 0 ? (
                            <div data-testid="empty-cart-msg" style={{ display: "flex", height: "100%", flexDirection: "column", alignItems: "center", justifyContent: "center", gap: "1rem", textAlign: "center", color: "#71717a" }}>
                                <svg style={{ height: "4rem", width: "4rem", opacity: 0.5 }} fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true">
                                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1} d="M16 11V7a4 4 0 00-8 0v4M5 9h14l1 12H4L5 9z" />
                                </svg>
                                <p style={{ fontSize: "1rem", fontWeight: 500, margin: 0 }}>Sua cesta está vazia.</p>
                                <p style={{ fontSize: "0.875rem", margin: 0 }}>Adicione itens do cardápio para continuar.</p>
                            </div>
                        ) : (
                            items.map((item) => {
                                const itemTotal = (item.salePrice + (item.complements?.reduce((acc, c) => acc + c.price, 0) || 0)) * item.quantity;
                                return (
                                    <article key={item.productId} className="cart-item-card" data-testid={`cart-item-${item.productId}`}>
                                        <div style={{ display: "flex", alignItems: "flex-start", justifyContent: "space-between", gap: "1rem" }}>
                                            <div style={{ flex: 1 }}>
                                                <h4 style={{ margin: 0, fontSize: "1rem", fontWeight: 600, color: "#f4f4f5", lineHeight: 1.2 }}>{item.productName}</h4>
                                                <span style={{ marginTop: "0.25rem", display: "block", fontSize: "0.875rem", fontWeight: "bold", color: "#f59e0b" }}>{formatBRL(item.salePrice)}</span>
                                            </div>
                                            <button
                                                onClick={() => onRemoveItem(item.productId)}
                                                data-testid={`btn-remove-${item.productId}`}
                                                aria-label={`Remover ${item.productName} da cesta`}
                                                style={{ background: "none", border: "none", fontSize: "0.75rem", fontWeight: 600, color: "#f87171", cursor: "pointer", padding: "0.25rem", borderRadius: "0.25rem" }}
                                            >
                                                Remover
                                            </button>
                                        </div>

                                        {item.complements && item.complements.length > 0 && (
                                            <div style={{ display: "flex", flexDirection: "column", gap: "0.25rem", borderLeft: "0.125rem solid #3f3f46", paddingLeft: "0.75rem", fontSize: "0.875rem", color: "#a1a1aa" }}>
                                                {item.complements.map(c => (
                                                    <div key={c.complementId} style={{ display: "flex", justifyContent: "space-between" }}>
                                                        <span>+ {c.name}</span>
                                                        <span>{formatBRL(c.price)}</span>
                                                    </div>
                                                ))}
                                            </div>
                                        )}

                                        <div style={{ marginTop: "0.5rem", display: "flex", alignItems: "center", justifyContent: "space-between" }}>
                                            <div className="qty-control" role="group" aria-label={`Quantidade de ${item.productName}`}>
                                                <button
                                                    onClick={() => onUpdateQuantity(item.productId, item.quantity - 1)}
                                                    className="qty-btn"
                                                    aria-label="Diminuir quantidade"
                                                >
                                                    −
                                                </button>
                                                <span style={{ display: "flex", width: "2rem", alignItems: "center", justifyContent: "center", fontSize: "0.875rem", fontWeight: 500, color: "#f4f4f5" }} aria-live="polite">
                                                    {item.quantity}
                                                </span>
                                                <button
                                                    onClick={() => onUpdateQuantity(item.productId, item.quantity + 1)}
                                                    className="qty-btn"
                                                    aria-label="Aumentar quantidade"
                                                >
                                                    +
                                                </button>
                                            </div>
                                            <span style={{ fontSize: "1rem", fontWeight: "bold", color: "#f4f4f5" }}>
                                                {formatBRL(itemTotal)}
                                            </span>
                                        </div>
                                    </article>
                                );
                            })
                        )
                    ) : (
                        <div style={{ display: "flex", flexDirection: "column", gap: "1.25rem" }}>

                            <fieldset style={{ border: "none", padding: 0, margin: 0, display: "flex", flexDirection: "column", gap: "0.625rem" }}>
                                <legend style={{ fontSize: "0.95rem", fontWeight: 600, color: "#f4f4f5", marginBottom: "0.625rem" }}>Como deseja receber o pedido?</legend>
                                <div style={{ display: "grid", gridTemplateColumns: "repeat(2, 1fr)", gap: "0.625rem" }}>
                                    <button
                                        type="button"
                                        onClick={() => setDeliveryType("DELIVERY")}
                                        className="selection-btn"
                                        aria-pressed={deliveryType === "DELIVERY"}
                                    >
                                        <span aria-hidden="true">🛵</span> Enviar com Motoboy
                                    </button>
                                    <button
                                        type="button"
                                        onClick={() => setDeliveryType("PICKUP")}
                                        className="selection-btn"
                                        aria-pressed={deliveryType === "PICKUP"}
                                    >
                                        <span aria-hidden="true">🏬</span> Retirar no Balcão
                                    </button>
                                </div>
                            </fieldset>

                            {deliveryType === "DELIVERY" && (
                                <section style={{ display: "flex", flexDirection: "column", gap: "0.625rem", borderTop: "0.0625rem solid #27272a", paddingTop: "1rem" }}>
                                    <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
                                        <h3 style={{ fontSize: "0.95rem", fontWeight: 600, color: "#f4f4f5", margin: 0 }}>Endereço de Entrega</h3>
                                        {!isEditingAddress && (
                                            <button
                                                type="button"
                                                onClick={() => setIsEditingAddress(true)}
                                                style={{ background: "none", border: "none", color: "#f59e0b", fontSize: "0.8rem", cursor: "pointer", textDecoration: "underline" }}
                                            >
                                                + Novo Endereço
                                            </button>
                                        )}
                                    </div>

                                    {isLoadingAddresses ? (
                                        <div className="address-skeleton" aria-label="Carregando endereços...">
                                            <div className="skeleton-line" style={{ width: "40%" }}></div>
                                            <div className="skeleton-line" style={{ width: "80%" }}></div>
                                            <div className="skeleton-line" style={{ width: "60%" }}></div>
                                        </div>
                                    ) : !isEditingAddress && addresses.length > 0 ? (
                                        <div role="radiogroup" aria-label="Selecione o endereço de entrega" style={{ display: "flex", flexDirection: "column", gap: "0.75rem" }}>
                                            {addresses.map(addr => (
                                                <button
                                                    key={addr.id}
                                                    type="button"
                                                    onClick={() => setSelectedAddressId(addr.id)}
                                                    className="address-card"
                                                    role="radio"
                                                    aria-checked={selectedAddressId === addr.id}
                                                >
                                                    <div style={{ fontSize: "0.75rem", color: selectedAddressId === addr.id ? "#f59e0b" : "#a1a1aa", fontWeight: "bold" }}>
                                                        {selectedAddressId === addr.id ? "✓ Selecionado para entrega" : "Endereço cadastrado"}
                                                    </div>
                                                    <div style={{ fontWeight: "bold", color: "#f4f4f5", marginTop: "0.25rem", fontSize: "0.9rem" }}>
                                                        📍 {addr.street}, {addr.number}
                                                    </div>
                                                    <div style={{ fontSize: "0.8rem", color: "#a1a1aa", marginTop: "0.125rem", lineHeight: "1.4" }}>
                                                        CEP: {addr.zipCode} {addr.supplement ? `— ${addr.supplement}` : ""}
                                                    </div>
                                                </button>
                                            ))}
                                        </div>
                                    ) : (
                                        <fieldset style={{ display: "flex", flexDirection: "column", gap: "1rem", background: "#09090b", padding: "1rem", borderRadius: "0.625rem", border: "0.0625rem solid #3f3f46", margin: 0 }}>
                                            <legend className="visually-hidden">Informe o endereço de entrega:</legend>

                                            <div className="input-group">
                                                <label htmlFor={zipCodeId} className="input-label">
                                                    CEP {isFetchingCep && <span style={{ color: "#f59e0b", marginLeft: "4px", fontSize: "0.75rem" }}>(Buscando...)</span>}
                                                </label>
                                                <input
                                                    id={zipCodeId}
                                                    type="text"
                                                    placeholder="00000000"
                                                    value={newZipCode}
                                                    onChange={handleCepChange}
                                                    maxLength={8}
                                                    disabled={isFetchingCep}
                                                    className="form-input"
                                                />
                                            </div>

                                            <div className="input-group">
                                                <label htmlFor={streetId} className="input-label">Rua / Avenida</label>
                                                <input
                                                    id={streetId}
                                                    type="text"
                                                    placeholder="Preenchido pelo CEP"
                                                    value={newStreet}
                                                    readOnly
                                                    tabIndex={-1}
                                                    className="form-input"
                                                />
                                            </div>

                                            <div style={{ display: "flex", gap: "1rem" }}>
                                                <div className="input-group" style={{ flex: 1 }}>
                                                    <label htmlFor={numberId} className="input-label">Número</label>
                                                    <input
                                                        id={numberId}
                                                        type="text"
                                                        placeholder="Ex: 123"
                                                        value={newNumber}
                                                        onChange={(e) => setNewNumber(e.target.value)}
                                                        disabled={isFetchingCep}
                                                        className="form-input"
                                                    />
                                                </div>
                                                <div className="input-group" style={{ flex: 2 }}>
                                                    <label htmlFor={supplementId} className="input-label">Complemento</label>
                                                    <input
                                                        id={supplementId}
                                                        type="text"
                                                        placeholder="Apto, Bloco..."
                                                        value={newSupplement}
                                                        onChange={(e) => setNewSupplement(e.target.value)}
                                                        disabled={isFetchingCep}
                                                        className="form-input"
                                                    />
                                                </div>
                                            </div>

                                            <div className="input-group">
                                                <label htmlFor={`${supplementId}-neighborhood`} className="input-label">Bairro / Cidade</label>
                                                <input
                                                    id={`${supplementId}-neighborhood`}
                                                    type="text"
                                                    placeholder="Preenchido pelo CEP"
                                                    value={newNeighborhood}
                                                    readOnly
                                                    tabIndex={-1}
                                                    className="form-input"
                                                />
                                            </div>

                                            <button
                                                type="button"
                                                onClick={async () => {
                                                    if (!newZipCode || !newStreet || !newNumber) {
                                                        Swal.fire({ title: "Atenção", text: "Preencha o CEP, Rua e Número.", icon: "warning", background: '#18181b', color: '#fff' });
                                                        return;
                                                    }
                                                    setIsProcessingAddress(true);
                                                    try {
                                                        await executeSaveAddress();
                                                        Swal.fire({ toast: true, position: 'top-end', icon: 'success', title: 'Endereço salvo com sucesso!', showConfirmButton: false, timer: 1500, background: '#18181b', color: '#fff' });
                                                    } catch (e) {
                                                        Swal.fire({ title: "Erro", text: "Falha ao registrar endereço.", icon: "error", background: '#18181b', color: '#fff' });
                                                    } finally {
                                                        setIsProcessingAddress(false);
                                                    }
                                                }}
                                                disabled={isProcessingAddress || isFetchingCep}
                                                className="btn-secondary"
                                            >
                                                {isProcessingAddress ? "Salvando..." : "Salvar novo endereço"}
                                            </button>

                                            {addresses.length > 0 && (
                                                <button
                                                    type="button"
                                                    onClick={() => setIsEditingAddress(false)}
                                                    style={{ background: "none", border: "none", color: "#a1a1aa", fontSize: "0.75rem", cursor: "pointer", textAlign: "center", padding: "0.5rem 0", marginTop: "0.25rem", width: "100%" }}
                                                >
                                                    ← Cancelar e escolher endereço existente
                                                </button>
                                            )}
                                        </fieldset>
                                    )}
                                </section>
                            )}

                            <fieldset style={{ border: "none", padding: 0, margin: 0, display: "flex", flexDirection: "column", gap: "0.625rem", borderTop: "0.0625rem solid #27272a", paddingTop: "1rem" }}>
                                <legend style={{ fontSize: "0.95rem", fontWeight: 600, color: "#f4f4f5", marginBottom: "0.625rem" }}>Forma de Pagamento</legend>
                                <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fit, minmax(100px, 1fr))", gap: "0.625rem" }}>
                                    <button
                                        type="button"
                                        onClick={() => setPaymentMethod("PIX")}
                                        className="selection-btn"
                                        style={paymentMethod === "PIX" ? { borderColor: "#10b981", background: "rgba(16, 185, 129, 0.1)", color: "#10b981" } : {}}
                                        aria-pressed={paymentMethod === "PIX"}
                                    >
                                        PIX (Online)
                                    </button>
                                    <button
                                        type="button"
                                        onClick={() => setPaymentMethod("CREDITO")}
                                        className="selection-btn"
                                        style={paymentMethod === "CREDITO" ? { borderColor: "#3b82f6", background: "rgba(59, 130, 246, 0.1)", color: "#3b82f6" } : {}}
                                        aria-pressed={paymentMethod === "CREDITO"}
                                    >
                                        Cartão (Crédito/Débito)
                                    </button>
                                    <button
                                        type="button"
                                        onClick={() => setPaymentMethod("BOLETO")}
                                        className="selection-btn"
                                        style={paymentMethod === "BOLETO" ? { borderColor: "#a78bfa", background: "rgba(167, 139, 250, 0.1)", color: "#a78bfa" } : {}}
                                        aria-pressed={paymentMethod === "BOLETO"}
                                    >
                                        Boleto
                                    </button>
                                    <button
                                        type="button"
                                        onClick={() => setPaymentMethod("MAQUININHA")}
                                        className="selection-btn"
                                        aria-pressed={paymentMethod === "MAQUININHA"}
                                    >
                                        Maquininha
                                    </button>
                                </div>

                                {paymentMethod === "CREDITO" && (
                                    <fieldset style={{ display: "flex", flexDirection: "column", gap: "0.75rem", background: "#09090b", padding: "1rem", borderRadius: "0.625rem", border: "0.0625rem solid #3f3f46", margin: 0 }}>
                                        <legend className="visually-hidden">Dados do cartão de crédito</legend>

                                        <div className="input-group">
                                            <label htmlFor={cardHolderId} className="input-label">Nome no cartão</label>
                                            <input
                                                id={cardHolderId}
                                                type="text"
                                                placeholder="Como está impresso no cartão"
                                                value={cardHolderName}
                                                onChange={(e) => setCardHolderName(e.target.value)}
                                                className="form-input"
                                                autoComplete="cc-name"
                                            />
                                        </div>

                                        <div className="input-group">
                                            <label htmlFor={cardNumberId} className="input-label">Número do cartão</label>
                                            <input
                                                id={cardNumberId}
                                                type="text"
                                                inputMode="numeric"
                                                placeholder="0000 0000 0000 0000"
                                                value={cardNumber}
                                                onChange={(e) => setCardNumber(e.target.value.replace(/\D/g, "").slice(0, 19))}
                                                className="form-input"
                                                autoComplete="cc-number"
                                            />
                                        </div>

                                        <div style={{ display: "flex", gap: "0.75rem" }}>
                                            <div className="input-group" style={{ flex: 1 }}>
                                                <label htmlFor={cardExpiryMonthId} className="input-label">Mês (MM)</label>
                                                <input
                                                    id={cardExpiryMonthId}
                                                    type="text"
                                                    inputMode="numeric"
                                                    placeholder="MM"
                                                    maxLength={2}
                                                    value={cardExpiryMonth}
                                                    onChange={(e) => setCardExpiryMonth(e.target.value.replace(/\D/g, "").slice(0, 2))}
                                                    className="form-input"
                                                    autoComplete="cc-exp-month"
                                                />
                                            </div>
                                            <div className="input-group" style={{ flex: 1 }}>
                                                <label htmlFor={cardExpiryYearId} className="input-label">Ano (AAAA)</label>
                                                <input
                                                    id={cardExpiryYearId}
                                                    type="text"
                                                    inputMode="numeric"
                                                    placeholder="AAAA"
                                                    maxLength={4}
                                                    value={cardExpiryYear}
                                                    onChange={(e) => setCardExpiryYear(e.target.value.replace(/\D/g, "").slice(0, 4))}
                                                    className="form-input"
                                                    autoComplete="cc-exp-year"
                                                />
                                            </div>
                                            <div className="input-group" style={{ flex: 1 }}>
                                                <label htmlFor={cardCcvId} className="input-label">CVV</label>
                                                <input
                                                    id={cardCcvId}
                                                    type="text"
                                                    inputMode="numeric"
                                                    placeholder="123"
                                                    maxLength={4}
                                                    value={cardCcv}
                                                    onChange={(e) => setCardCcv(e.target.value.replace(/\D/g, "").slice(0, 4))}
                                                    className="form-input"
                                                    autoComplete="cc-csc"
                                                />
                                            </div>
                                        </div>

                                        <label style={{ display: "flex", alignItems: "center", gap: "0.5rem", fontSize: "0.8rem", color: "#a1a1aa", cursor: "pointer" }}>
                                            <input type="checkbox" checked={saveCard} onChange={(e) => setSaveCard(e.target.checked)} />
                                            Salvar cartão para próximas compras
                                        </label>
                                    </fieldset>
                                )}
                            </fieldset>
                        </div>
                    )}
                </section>

                {items.length > 0 && (
                    <footer className="drawer-footer">
                        <div style={{ display: "flex", flexDirection: "column", gap: "1rem" }}>
                            {step === "review" && (
                                <div style={{ display: "flex", flexDirection: "column", gap: "0.375rem" }}>
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
                                        className="form-input"
                                    />
                                </div>
                            )}

                            <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between" }}>
                                <span style={{ fontSize: "0.95rem", fontWeight: 500, color: "#a1a1aa" }}>{step === "review" ? "Total do Pedido" : "Total a Pagar"}</span>
                                <strong data-testid="cart-total-amount" style={{ fontSize: "1.35rem", color: "#f4f4f5" }}>
                                    {formatBRL(subtotal)}
                                </strong>
                            </div>

                            <button
                                type="button"
                                onClick={handleMainActionClick}
                                disabled={isSubmitting || isProcessingAddress || isFetchingCep}
                                data-testid="btn-submit-order"
                                className="btn-primary"
                                aria-live="polite"
                            >
                                {isSubmitting || isProcessingAddress
                                    ? "Processando..."
                                    : (step === "review"
                                        ? (customerData?.customerId ? "Avançar para Opções de Entrega" : "Identificar-se para Continuar")
                                        : "Confirmar e Enviar Pedido")}
                            </button>
                        </div>
                    </footer>
                )}
            </aside>
        </div>
    );
}