import { useState, useMemo, useCallback, useRef } from "react";
import { useParams } from "react-router-dom";
import { useMutation, useQuery } from "@tanstack/react-query";
import Swal from "sweetalert2";
import type { MenuItemResponse, OrderItemComplementSelection } from "../../lib/types";
import { ComplementSelectorModal } from "../orders/ComplementSelectorModal";
import { PublicOrderCard } from "../publicOrdering/PublicOrderCard";
import { StorefrontCartDrawer, CartItem, CustomerSessionData, PaymentMethod, NewCardData } from "./StorefrontCartDrawer";
import { submitStorefrontOrder, StorefrontOrderPayload } from "./storefrontApi";
import { StorefrontAuthModal } from "./StorefrontAuthModal";
import { StorefrontPaymentModal, StorefrontPaymentResult } from "./StorefrontPaymentModal";
import { payWithPix, payWithCreditCard, payWithBoleto, payWithDebitCard } from "./checkoutApi";

import logoImg from "../../image/logo.png";
import bgImg from "../../image/screenbackground_auth.jpeg";

const styles = `
  .storefront-main {
    min-height: 100vh;
    background-color: #09090b;
    padding-bottom: 7rem;
    font-family: system-ui, -apple-system, sans-serif;
    color: #f4f4f5;
  }
  .storefront-header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    border-bottom: 0.0625rem solid #27272a;
    background-image: linear-gradient(rgba(9, 9, 11, 0.85), rgba(9, 9, 11, 0.95)), url(${bgImg});
    background-size: cover;
    background-position: center;
    padding: 0 1rem;
    height: 5rem;
  }
  .storefront-logo {
    position: relative;
    z-index: 10;
    height: 3rem;
    object-fit: contain;
  }
  .storefront-title {
    position: relative;
    z-index: 10;
    font-size: 1.125rem;
    font-weight: 600;
    color: #fff;
    margin: 0;
  }
  .storefront-nav-container {
    position: sticky;
    top: 0;
    z-index: 40;
    border-bottom: 0.0625rem solid #27272a;
    background-color: rgba(9, 9, 11, 0.9);
    backdrop-filter: blur(0.75rem);
    padding: 1rem;
  }
  .storefront-nav-inner {
    margin: 0 auto;
    max-width: 80rem;
  }
  .category-nav {
    display: flex;
    gap: 1.5rem;
    overflow-x: auto;
    white-space: nowrap;
    padding-bottom: 0.5rem;
    scrollbar-width: none;
  }
  .category-nav::-webkit-scrollbar {
    display: none;
  }
  .category-btn {
    background: none;
    border: none;
    padding-bottom: 0.25rem;
    font-size: 0.875rem;
    font-weight: 500;
    cursor: pointer;
    transition: color 0.2s ease, border-color 0.2s ease;
  }
  .category-btn:focus-visible {
    outline: 0.125rem solid #f59e0b;
    outline-offset: 0.25rem;
    border-radius: 0.125rem;
  }
  .search-container {
    position: relative;
    margin-top: 1rem;
  }
  .search-input {
    width: 100%;
    border-radius: 0.75rem;
    border: 0.0625rem solid #3f3f46;
    background-color: #18181b;
    padding: 0.875rem 3rem 0.875rem 1rem;
    font-size: 0.875rem;
    color: #f4f4f5;
    transition: border-color 0.2s ease, box-shadow 0.2s ease;
    outline: none;
  }
  .search-input:focus {
    border-color: #f59e0b;
    box-shadow: 0 0 0 0.125rem rgba(245, 158, 11, 0.2);
  }
  .content-container {
    margin: 0 auto;
    max-width: 80rem;
    padding: 2rem 1rem 0;
  }
  .product-grid {
    display: grid;
    gap: 1rem;
    grid-template-columns: 1fr;
  }
  .fab-cart {
    position: fixed;
    bottom: 1.5rem;
    right: 1.5rem;
    z-index: 50;
    display: flex;
    height: 4rem;
    width: 4rem;
    align-items: center;
    justify-content: center;
    border-radius: 50%;
    border: none;
    background-color: #f59e0b;
    font-size: 1.5rem;
    box-shadow: 0 0.25rem 0.9375rem rgba(245,158,11,0.4);
    cursor: pointer;
    transition: transform 0.2s ease, background-color 0.2s ease;
  }
  .fab-cart:hover {
    transform: scale(1.05);
    background-color: #d97706;
  }
  .fab-cart:active {
    transform: scale(0.95);
  }
  .fab-cart:focus-visible {
    outline: 0.125rem solid #fff;
    outline-offset: 0.125rem;
  }
  .cart-badge {
    position: absolute;
    top: 0;
    right: 0;
    display: flex;
    height: 1.5rem;
    width: 1.5rem;
    align-items: center;
    justify-content: center;
    border-radius: 50%;
    background-color: #ef4444;
    font-size: 0.75rem;
    font-weight: bold;
    color: #fff;
    box-shadow: 0 0.25rem 0.375rem -0.0625rem rgba(0, 0, 0, 0.1);
  }

  /* Breakpoints: Tablet */
  @media (min-width: 40.0625rem) { /* 641px */
    .storefront-header { padding: 0 2.5rem; height: 7rem; }
    .storefront-logo { height: 4rem; }
    .storefront-title { font-size: 1.5rem; }
    .storefront-nav-container { padding: 1rem 2.5rem; }
    .category-btn { font-size: 1rem; }
    .search-input { font-size: 1rem; }
    .content-container { padding: 2rem 2.5rem 0; }
    .product-grid { grid-template-columns: repeat(2, 1fr); gap: 1.5rem; }
    .fab-cart { bottom: 2rem; right: 2rem; height: 5rem; width: 5rem; font-size: 1.875rem; }
    .cart-badge { height: 1.75rem; width: 1.75rem; font-size: 0.875rem; }
  }

  /* Breakpoints: Desktop */
  @media (min-width: 64rem) { /* 1024px */
    .product-grid { grid-template-columns: repeat(3, 1fr); }
  }
  @media (min-width: 90rem) { /* 1440px */
    .product-grid { grid-template-columns: repeat(4, 1fr); }
  }

  .spinner {
    border: 0.25rem solid rgba(255, 255, 255, 0.1);
    border-left-color: #f59e0b;
    border-radius: 50%;
    width: 2.5rem;
    height: 2.5rem;
    animation: spin 1s linear infinite;
  }
  @keyframes spin { 0% { transform: rotate(0deg); } 100% { transform: rotate(360deg); } }

  .payment-processing-overlay {
    position: fixed; inset: 0; z-index: 10001;
    display: flex; flex-direction: column; align-items: center; justify-content: center; gap: 1rem;
    background-color: rgba(9, 9, 11, 0.88);
    backdrop-filter: blur(0.25rem);
    color: #f4f4f5;
    text-align: center;
    padding: 1rem;
  }
  .payment-processing-overlay p { margin: 0; font-size: 0.95rem; color: #d4d4d8; max-width: 20rem; }
`;

async function fetchMenu(branchId: number): Promise<any> {
    const res = await fetch(`/api/storefront/branches/${branchId}/menu`);
    if (!res.ok) throw new Error("Não foi possível carregar o cardápio da filial.");
    return res.json();
}

export function StorefrontOrderPage() {
    const { branchIdParam } = useParams<{ branchIdParam: string }>();
    const branchId = branchIdParam ? Number(branchIdParam) : 1;

    const [selectingItem, setSelectingItem] = useState<MenuItemResponse | null>(null);
    const [activeCategory, setActiveCategory] = useState<string>("Todas");
    const [searchQuery, setSearchQuery] = useState("");
    const [quantities, setQuantities] = useState<Record<number, number>>({});
    const [cartItems, setCartItems] = useState<CartItem[]>([]);

    const [isCartOpen, setIsCartOpen] = useState(false);
    const [customerData, setCustomerData] = useState<CustomerSessionData | null>(null);
    const [isAuthModalOpen, setIsAuthModalOpen] = useState(false);
    const [pendingCheckoutNotes, setPendingCheckoutNotes] = useState("");

    const menuQuery = useQuery({
        queryKey: ["storefront-menu", branchId],
        queryFn: () => fetchMenu(branchId),
        enabled: !!branchId,
        retry: false,
    });

    const [paymentResult, setPaymentResult] = useState<StorefrontPaymentResult | null>(null);
    const [paymentOrderId, setPaymentOrderId] = useState<number | null>(null);
    const [isChargingPayment, setIsChargingPayment] = useState(false);
    const pendingPaymentRef = useRef<{ method: PaymentMethod; cardData?: NewCardData } | null>(null);

    const chargeOnlinePayment = useCallback(async (orderId: number) => {
        const pending = pendingPaymentRef.current;
        if (!pending || pending.method === "MAQUININHA") return;

        setIsChargingPayment(true);
        try {
            switch (pending.method) {
                case "DEBITO": {
                    const data = await payWithDebitCard(orderId);
                    setPaymentOrderId(orderId);
                    setPaymentResult({ method: "DEBITO", data });
                    break;
                }
                case "PIX": {
                    const data = await payWithPix(orderId);
                    setPaymentOrderId(orderId);
                    setPaymentResult({ method: "PIX", data });
                    break;
                }
                case "CREDITO": {
                    const card = pending.cardData;
                    const data = await payWithCreditCard({
                        customerOrderId: orderId,
                        card: card
                            ? {
                                holderName: card.holderName,
                                number: card.number,
                                expiryMonth: card.expiryMonth,
                                expiryYear: card.expiryYear,
                                ccv: card.ccv,
                            }
                            : null,
                        saveCard: card?.saveCard ?? false,
                    });
                    setPaymentOrderId(orderId);
                    setPaymentResult({ method: "CREDITO", data });
                    break;
                }
                case "BOLETO": {
                    const data = await payWithBoleto(orderId);
                    setPaymentOrderId(orderId);
                    setPaymentResult({ method: "BOLETO", data });
                    break;
                }
            }
        } catch (e: unknown) {
            const msg = e instanceof Error ? e.message : "Falha ao processar o pagamento.";
            Swal.fire({
                title: "Pagamento não concluído",
                text: `${msg} Seu pedido já foi registrado — você pode tentar pagar novamente pela tela de acompanhamento.`,
                icon: "error",
                background: "#18181b",
                color: "#ffffff",
                confirmButtonColor: "#ef4444",
            });
        } finally {
            setIsChargingPayment(false);
        }
    }, []);

    const addBatchMutation = useMutation({
        mutationFn: (payload: StorefrontOrderPayload) => submitStorefrontOrder(branchId, payload),
        onSuccess: (data) => {
            setCartItems([]);
            setIsCartOpen(false);

            const pending = pendingPaymentRef.current;
            if (pending && pending.method !== "MAQUININHA") {
                void chargeOnlinePayment(data.orderId);
                return;
            }

            Swal.fire({
                title: "Pedido Solicitado!",
                text: "Seu pedido foi enviado com sucesso para a produção.",
                icon: "success",
                background: "#18181b",
                color: "#ffffff",
                confirmButtonColor: "#f59e0b",
            });
        },
        onError: (e: unknown) => {
            const msg = e instanceof Error ? e.message : "Falha ao enviar o pedido.";
            Swal.fire({
                title: "Ops!",
                text: msg,
                icon: "error",
                background: "#18181b",
                color: "#ffffff",
                confirmButtonColor: "#ef4444",
                confirmButtonText: "Voltar",
            });
        },
    });

    const getQty = useCallback((productId: number) => quantities[productId] || 1, [quantities]);

    const setQty = useCallback((productId: number, newQty: number) => {
        setQuantities(prev => ({ ...prev, [productId]: Math.max(1, newQty) }));
    }, []);

    const handleAddOrAddToCart = useCallback(({ productId, quantity, complements, item }: { productId: number; quantity: number; complements?: OrderItemComplementSelection[]; item?: MenuItemResponse }) => {
        const targetItem: MenuItemResponse | undefined = item || menuQuery.data?.items.find((i: MenuItemResponse) => i.id === productId);
        if (!targetItem) return;

        const chosen = (complements ?? []).flatMap(selection => {
            const group = targetItem.complementGroups.find(group => group.id === selection.complementGroupId);
            const option = group?.complements.find(option => option.id === selection.complementId);
            return option ? [{ ...selection, name: option.complementItemName, price: option.extraPrice }] : [];
        });
        const cartKey = JSON.stringify([productId, chosen.map(option =>
            `${option.complementGroupId}:${option.complementId}`).sort()]);

        setCartItems(prev => {
            const existingIndex = prev.findIndex(i => i.cartKey === cartKey);
            if (existingIndex > -1) {
                return prev.map((item, index) => index === existingIndex ? { ...item, quantity: item.quantity + quantity } : item);
            }
            return [...prev, {
                cartKey,
                productId: targetItem.id,
                productName: targetItem.name,
                salePrice: targetItem.salePrice,
                quantity,
                imageUrl: targetItem.imageUrl,
                complements: chosen,
            }];
        });

        setQuantities(prev => ({ ...prev, [productId]: 1 }));
        Swal.fire({
            toast: true,
            position: 'top-end',
            icon: 'success',
            title: 'Item adicionado à cesta!',
            showConfirmButton: false,
            timer: 1500,
            background: '#18181b',
            color: '#fff'
        });
    }, [menuQuery.data]);

    const handlePickItem = useCallback((item: MenuItemResponse) => {
        const currentQty = getQty(item.id);
        if (item.complementGroups && item.complementGroups.length > 0) {
            setSelectingItem(item);
            return;
        }
        handleAddOrAddToCart({ productId: item.id, quantity: currentQty, item });
    }, [getQty, handleAddOrAddToCart]);

    const executeSubmitOrder = useCallback((
        activeCustomer: CustomerSessionData,
        deliveryType: "PICKUP" | "DELIVERY" = "DELIVERY",
        addressId?: number | null,
        newAddress?: any
    ) => {
        const payloadItems = cartItems.map(cartItem => ({
            productId: cartItem.productId,
            quantity: cartItem.quantity,
            notes: cartItem.notes || null,
            complements: cartItem.complements?.map(c => ({
                complementGroupId: c.complementGroupId,
                complementId: c.complementId
            })) || []
        }));

        addBatchMutation.mutate({
            customerId: activeCustomer.customerId || null,
            customerName: activeCustomer.name,
            customerPhone: activeCustomer.phone || null,
            generalNotes: pendingCheckoutNotes || undefined,
            deliveryType: deliveryType,
            addressId: addressId || null,
            newAddress: newAddress || null,
            items: payloadItems
        });
    }, [cartItems, pendingCheckoutNotes, addBatchMutation]);

    const handleCheckoutCart = useCallback((
        generalNotes: string,
        activeCustomerData?: CustomerSessionData,
        deliveryType?: "PICKUP" | "DELIVERY",
        addressId?: number | null,
        newAddress?: any,
        paymentMethod?: PaymentMethod,
        cardData?: NewCardData
    ) => {
        if (cartItems.length === 0) return;
        setPendingCheckoutNotes(generalNotes);

        const currentCustomer = activeCustomerData || customerData;
        if (!currentCustomer || !currentCustomer.customerId) {
            setIsAuthModalOpen(true);
            return;
        }
        if (!deliveryType) return;

        pendingPaymentRef.current = paymentMethod ? { method: paymentMethod, cardData } : null;
        executeSubmitOrder(currentCustomer, deliveryType, addressId, newAddress);
    }, [cartItems.length, customerData, executeSubmitOrder]);

    const { categoryList, groupedItems, filteredItems } = useMemo(() => {
        if (!menuQuery.data) return { categoryList: [] as string[], groupedItems: {} as Record<string, MenuItemResponse[]>, filteredItems: [] as MenuItemResponse[] };
        const items: MenuItemResponse[] = menuQuery.data.items;

        const uniqueCategories = Array.from(new Set(items.map((i: any) => i.categoryName || i.category || "Geral")));
        const cats: string[] = ["Todas", ...uniqueCategories];
        let resultItems = items;

        if (activeCategory !== "Todas") {
            resultItems = resultItems.filter((i: any) => (i.categoryName || i.category || "Geral") === activeCategory);
        }
        if (searchQuery) {
            resultItems = resultItems.filter((i: MenuItemResponse) => i.name.toLowerCase().includes(searchQuery.toLowerCase()));
        }

        const grouped: Record<string, MenuItemResponse[]> = {};
        resultItems.forEach((item: any) => {
            const catName = item.categoryName || item.category || "Geral";
            if (!grouped[catName]) grouped[catName] = [];
            grouped[catName].push(item);
        });

        return { categoryList: cats, groupedItems: grouped, filteredItems: resultItems };
    }, [menuQuery.data, activeCategory, searchQuery]);

    if (menuQuery.isLoading) {
        return (
            <main data-testid="loading-menu" className="storefront-main" style={{ display: 'flex', alignItems: 'center', justifyContent: 'center' }} aria-busy="true" aria-live="polite">
                <style>{styles}</style>
                <div className="spinner" aria-label="Carregando cardápio"></div>
            </main>
        );
    }

    if (menuQuery.isError) {
        return (
            <main data-testid="error-menu" className="storefront-main" style={{ display: 'flex', alignItems: 'center', justifyContent: 'center' }} role="alert">
                <style>{styles}</style>
                <p style={{ color: "#ef4444", fontSize: "1.125rem" }}>Não foi possível carregar o cardápio. Tente novamente mais tarde.</p>
            </main>
        );
    }

    return (
        <main data-testid="storefront-order-page" className="storefront-main">
            <style>{styles}</style>

            <header className="storefront-header">
                <img src={logoImg} alt="Logotipo SyncBar" className="storefront-logo" />
                <h1 data-testid="header-store-title" className="storefront-title">
                    Cardápio <span style={{ color: "#f59e0b" }}>Digital</span>
                </h1>
            </header>

            <nav className="storefront-nav-container" aria-label="Navegação do Cardápio">
                <div className="storefront-nav-inner">
                    <div className="category-nav" role="tablist">
                        {categoryList.map((cat: string) => {
                            const isActive = activeCategory === cat;
                            return (
                                <button
                                    key={cat}
                                    role="tab"
                                    aria-selected={isActive}
                                    data-testid={`category-tab-${cat.replace(/\s+/g, '-')}`}
                                    onClick={() => setActiveCategory(cat)}
                                    className="category-btn"
                                    style={{
                                        borderBottom: isActive ? "0.125rem solid #f59e0b" : "0.125rem solid transparent",
                                        color: isActive ? "#f59e0b" : "#a1a1aa"
                                    }}
                                >
                                    {cat}
                                </button>
                            );
                        })}
                    </div>

                    <div className="search-container" role="search">
                        <input
                            type="text"
                            placeholder="Pesquisar um produto..."
                            value={searchQuery}
                            onChange={(e) => setSearchQuery(e.target.value)}
                            data-testid="input-menu-search"
                            className="search-input"
                            aria-label="Buscar produtos no cardápio"
                        />
                        <div style={{ position: "absolute", top: 0, bottom: 0, right: "1rem", display: "flex", alignItems: "center", pointerEvents: "none" }} aria-hidden="true">
                            <span style={{ color: "#71717a" }}>🔍</span>
                        </div>
                    </div>
                </div>
            </nav>

            <section className="content-container" aria-live="polite">
                {activeCategory === "Todas" && !searchQuery ? (
                    Object.entries(groupedItems).map(([categoryName, products]) => (
                        <article key={categoryName} data-testid={`category-section-${categoryName.replace(/\s+/g, '-')}`} style={{ marginBottom: "3rem" }}>
                            <h2 style={{ marginBottom: "1.5rem", display: "inline-block", borderBottom: "0.125rem solid #f59e0b", paddingBottom: "0.25rem", fontSize: "1.25rem", fontWeight: "bold", textTransform: "uppercase" }}>
                                {categoryName}
                            </h2>
                            <div className="product-grid">
                                {products.map((item: MenuItemResponse) => (
                                    <PublicOrderCard
                                        key={item.id}
                                        item={item}
                                        quantity={getQty(item.id)}
                                        isJustSent={false}
                                        isPending={addBatchMutation.isPending}
                                        onQuantityChange={(newQty) => setQty(item.id, newQty)}
                                        onAddItem={() => handlePickItem(item)}
                                    />
                                ))}
                            </div>
                        </article>
                    ))
                ) : filteredItems.length > 0 ? (
                    <div data-testid="filtered-items-grid" className="product-grid">
                        {filteredItems.map((item: MenuItemResponse) => (
                            <PublicOrderCard
                                key={item.id}
                                item={item}
                                quantity={getQty(item.id)}
                                isJustSent={false}
                                isPending={addBatchMutation.isPending}
                                onQuantityChange={(newQty) => setQty(item.id, newQty)}
                                onAddItem={() => handlePickItem(item)}
                            />
                        ))}
                    </div>
                ) : (
                    <div style={{ textAlign: "center", padding: "4rem 1rem", color: "#a1a1aa" }}>
                        <p style={{ fontSize: "1.25rem", marginBottom: "0.5rem" }}>Nenhum produto encontrado.</p>
                        <p style={{ fontSize: "0.875rem" }}>Tente ajustar os termos da sua pesquisa ou mude de categoria.</p>
                    </div>
                )}
            </section>

            <button
                onClick={() => setIsCartOpen(true)}
                data-testid="btn-open-cart"
                aria-label={`Ver Cesta de Compras com ${cartItems.reduce((acc, i) => acc + i.quantity, 0)} itens`}
                className="fab-cart"
            >
                🛒
                {cartItems.length > 0 && (
                    <span className="cart-badge" aria-hidden="true">
                        {cartItems.reduce((acc, i) => acc + i.quantity, 0)}
                    </span>
                )}
            </button>

            <StorefrontCartDrawer
                branchId={branchId}
                companyId={menuQuery.data?.companyId}
                isOpen={isCartOpen}
                onClose={() => setIsCartOpen(false)}
                items={cartItems}
                onUpdateQuantity={(cartKey, newQty) => {
                    if (newQty <= 0) {
                        setCartItems(prev => prev.filter(i => i.cartKey !== cartKey));
                    } else {
                        setCartItems(prev => prev.map(i => i.cartKey === cartKey ? { ...i, quantity: newQty } : i));
                    }
                }}
                onRemoveItem={(cartKey) => setCartItems(prev => prev.filter(i => i.cartKey !== cartKey))}
                onCheckout={handleCheckoutCart}
                isSubmitting={addBatchMutation.isPending}
                customerData={customerData}
                onOpenAuthModal={() => setIsAuthModalOpen(true)}
            />

            <StorefrontAuthModal
                isOpen={isAuthModalOpen}
                onClose={() => setIsAuthModalOpen(false)}
                branchId={branchId}
                onAuthenticated={(data) => {
                    setCustomerData(data);
                    setIsAuthModalOpen(false);
                }}
            />

            {isChargingPayment && (
                <div className="payment-processing-overlay" role="status" aria-live="polite" data-testid="payment-processing-overlay">
                    <div className="spinner" aria-hidden="true"></div>
                    <p>Comunicando com a instituição financeira, aguarde…</p>
                </div>
            )}

            {paymentResult && paymentOrderId && (
                <StorefrontPaymentModal
                    orderId={paymentOrderId}
                    result={paymentResult}
                    onClose={() => {
                        setPaymentResult(null);
                        setPaymentOrderId(null);
                    }}
                />
            )}

            {selectingItem && (
                <ComplementSelectorModal
                    productName={selectingItem.name}
                    imageUrl={selectingItem.imageUrl} description={selectingItem.description} basePrice={selectingItem.salePrice}
                    groups={selectingItem.complementGroups}
                    onCancel={() => setSelectingItem(null)}
                    submitting={false}
                    confirmLabel="ADICIONAR À CESTA"
                    onConfirm={(complements: OrderItemComplementSelection[]) => {
                        const productId = selectingItem.id;
                        setSelectingItem(null);
                        handleAddOrAddToCart({ productId, quantity: 1, complements });
                    }}
                />
            )}
        </main>
    );
}

