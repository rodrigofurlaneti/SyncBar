import { useState, useMemo, useEffect } from "react";
import { useParams } from "react-router-dom";
import { useMutation, useQuery } from "@tanstack/react-query";
import Swal from "sweetalert2";
import type { MenuItemResponse, OrderItemComplementSelection } from "../../lib/types";
import { ComplementSelectorModal } from "../orders/ComplementSelectorModal";
import { PublicOrderCard } from "../publicOrdering/PublicOrderCard";
import { StorefrontCartDrawer, CartItem, CustomerSessionData } from "./StorefrontCartDrawer";
import { submitStorefrontOrder, StorefrontOrderPayload } from "./storefrontApi";
import { StorefrontAuthModal } from "./StorefrontAuthModal";

import logoImg from "../../image/logo.png";
import bgImg from "../../image/screenbackground_auth.jpeg";

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

    // Controle de responsividade para estilos inline
    const [windowWidth, setWindowWidth] = useState(typeof window !== "undefined" ? window.innerWidth : 1200);
    useEffect(() => {
        const handleResize = () => setWindowWidth(window.innerWidth);
        window.addEventListener("resize", handleResize);
        return () => window.removeEventListener("resize", handleResize);
    }, []);

    const isMobile = windowWidth < 640;

    const menuQuery = useQuery({
        queryKey: ["storefront-menu", branchId],
        queryFn: () => fetchMenu(branchId),
        enabled: !!branchId,
        retry: false,
    });

    const addBatchMutation = useMutation({
        mutationFn: (payload: StorefrontOrderPayload) => submitStorefrontOrder(branchId, payload),
        onSuccess: () => {
            setCartItems([]);
            setIsCartOpen(false);
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

    const getQty = (productId: number) => quantities[productId] || 1;
    const setQty = (productId: number, newQty: number) => {
        setQuantities(prev => ({ ...prev, [productId]: Math.max(1, newQty) }));
    };

    const handlePickItem = (item: MenuItemResponse) => {
        const currentQty = getQty(item.id);
        if (item.complementGroups && item.complementGroups.length > 0) {
            setSelectingItem(item);
            return;
        }
        handleAddOrAddToCart({ productId: item.id, quantity: currentQty, item });
    };

    const handleAddOrAddToCart = ({ productId, quantity, complements, item }: { productId: number; quantity: number; complements?: OrderItemComplementSelection[]; item?: MenuItemResponse }) => {
        const targetItem = item || menuQuery.data?.items.find((i: MenuItemResponse) => i.id === productId);
        if (!targetItem) return;

        setCartItems(prev => {
            const existingIndex = prev.findIndex(i => i.productId === productId);
            if (existingIndex > -1) {
                const copy = [...prev];
                copy[existingIndex].quantity += quantity;
                return copy;
            }
            return [...prev, {
                productId: targetItem.id,
                productName: targetItem.name,
                salePrice: targetItem.salePrice,
                quantity,
                imageUrl: targetItem.imageUrl,
                complements: complements?.map(c => ({ complementId: c.complementId, name: "", price: 0 }))
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
    };

    const handleCheckoutCart = (generalNotes: string, activeCustomerData?: CustomerSessionData) => {
        if (cartItems.length === 0) return;
        setPendingCheckoutNotes(generalNotes);

        const currentCustomer = activeCustomerData || customerData;
        if (!currentCustomer || !currentCustomer.customerId) {
            setIsAuthModalOpen(true);
            return;
        }

        executeSubmitOrder(currentCustomer);
    };

    const handleAuthenticatedSuccess = (authenticatedData: CustomerSessionData) => {
        setCustomerData(authenticatedData);
        setIsAuthModalOpen(false);
        executeSubmitOrder(authenticatedData);
    };

    const executeSubmitOrder = (activeCustomer: CustomerSessionData) => {
        const payloadItems = cartItems.map(cartItem => ({
            productId: cartItem.productId,
            quantity: cartItem.quantity,
            notes: cartItem.notes || null,
            complements: cartItem.complements?.map(c => ({
                complementGroupId: 0,
                complementId: c.complementId
            })) || []
        }));

        addBatchMutation.mutate({
            customerId: activeCustomer.customerId || null,
            customerName: activeCustomer.name,
            customerPhone: activeCustomer.phone || null,
            generalNotes: pendingCheckoutNotes || undefined,
            items: payloadItems
        });
    };

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
            <main data-testid="loading-menu" style={{ display: "flex", minHeight: "100vh", alignItems: "center", justifyContent: "center", backgroundColor: "#09090b", color: "#a1a1aa" }}>
                <span>Carregando cardápio…</span>
            </main>
        );
    }

    if (menuQuery.isError) {
        return (
            <main data-testid="error-menu" style={{ display: "flex", minHeight: "100vh", alignItems: "center", justifyContent: "center", backgroundColor: "#09090b" }}>
                <p style={{ color: "#ef4444" }}>Não foi possível carregar o cardápio.</p>
            </main>
        );
    }

    const gridColumns = isMobile ? "1fr" : "repeat(auto-fill, minmax(300px, 1fr))";

    return (
        <main data-testid="storefront-order-page" style={{ minHeight: "100vh", backgroundColor: "#09090b", paddingBottom: "112px", fontFamily: "sans-serif", color: "#f4f4f5" }}>

            {/* Cabeçalho Hero com Imagem */}
            <header style={{ display: "flex", height: isMobile ? "80px" : "112px", alignItems: "center", justifyContent: "space-between", borderBottom: "1px solid #27272a", padding: isMobile ? "0 16px" : "0 40px", backgroundImage: `linear-gradient(rgba(9, 9, 11, 0.85), rgba(9, 9, 11, 0.95)), url(${bgImg})`, backgroundSize: "cover", backgroundPosition: "center" }}>
                <img src={logoImg} alt="Logotipo SyncBar" style={{ position: "relative", zIndex: 10, height: isMobile ? "48px" : "64px", objectFit: "contain" }} />
                <h1 data-testid="header-store-title" style={{ position: "relative", zIndex: 10, fontSize: isMobile ? "1.125rem" : "1.5rem", fontWeight: 600, color: "#fff", margin: 0 }}>
                    Cardápio <span style={{ color: "#f59e0b" }}>Digital</span>
                </h1>
            </header>

            {/* Navegação Sticky (Categorias + Busca) */}
            <div style={{ position: "sticky", top: 0, zIndex: 40, borderBottom: "1px solid #27272a", backgroundColor: "rgba(9, 9, 11, 0.9)", backdropFilter: "blur(12px)", padding: isMobile ? "16px" : "16px 40px" }}>
                <div style={{ margin: "0 auto", maxWidth: "1280px" }}>

                    {/* Lista de Categorias */}
                    <nav style={{ display: "flex", gap: "24px", overflowX: "auto", whiteSpace: "nowrap", paddingBottom: "8px" }}>
                        {categoryList.map((cat: string) => {
                            const isActive = activeCategory === cat;
                            return (
                                <button
                                    key={cat}
                                    data-testid={`category-tab-${cat.replace(/\s+/g, '-')}`}
                                    onClick={() => setActiveCategory(cat)}
                                    style={{ background: "none", border: "none", borderBottom: isActive ? "2px solid #f59e0b" : "2px solid transparent", paddingBottom: "4px", fontSize: isMobile ? "0.875rem" : "1rem", fontWeight: 500, color: isActive ? "#f59e0b" : "#a1a1aa", cursor: "pointer", transition: "color 0.2s" }}
                                >
                                    {cat}
                                </button>
                            );
                        })}
                    </nav>

                    {/* Barra de Pesquisa */}
                    <div style={{ position: "relative", marginTop: "16px" }}>
                        <input
                            type="text"
                            placeholder="Pesquisar um produto..."
                            value={searchQuery}
                            onChange={(e) => setSearchQuery(e.target.value)}
                            data-testid="input-menu-search"
                            style={{ width: "100%", borderRadius: "12px", border: "1px solid #3f3f46", backgroundColor: "#18181b", padding: "14px 48px 14px 16px", fontSize: isMobile ? "0.875rem" : "1rem", color: "#f4f4f5", boxSizing: "border-box", outline: "none" }}
                        />
                        <div style={{ position: "absolute", top: 0, bottom: 0, right: "16px", display: "flex", alignItems: "center", pointerEvents: "none" }}>
                            <span style={{ color: "#71717a" }}>🔍</span>
                        </div>
                    </div>
                </div>
            </div>

            {/* Container Principal de Produtos */}
            <div style={{ margin: "0 auto", maxWidth: "1280px", padding: isMobile ? "32px 16px 0" : "32px 40px 0" }}>
                {activeCategory === "Todas" && !searchQuery ? (
                    Object.entries(groupedItems).map(([categoryName, products]) => (
                        <section key={categoryName} data-testid={`category-section-${categoryName.replace(/\s+/g, '-')}`} style={{ marginBottom: "48px" }}>
                            <h2 style={{ marginBottom: "24px", display: "inline-block", borderBottom: "2px solid #f59e0b", paddingBottom: "4px", fontSize: isMobile ? "1.125rem" : "1.25rem", fontWeight: "bold", letterSpacing: "0.025em", color: "#f4f4f5", textTransform: "uppercase" }}>
                                {categoryName}
                            </h2>
                            <div style={{ display: "grid", gap: "16px", gridTemplateColumns: gridColumns }}>
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
                        </section>
                    ))
                ) : (
                    <div data-testid="filtered-items-grid" style={{ display: "grid", gap: "16px", gridTemplateColumns: gridColumns }}>
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
                )}
            </div>

            {/* Botão Flutuante (FAB) do Carrinho */}
            <button
                onClick={() => setIsCartOpen(true)}
                data-testid="btn-open-cart"
                aria-label="Ver Cesta de Compras"
                style={{ position: "fixed", bottom: isMobile ? "24px" : "32px", right: isMobile ? "24px" : "32px", zIndex: 50, display: "flex", height: isMobile ? "64px" : "80px", width: isMobile ? "64px" : "80px", alignItems: "center", justifyContent: "center", borderRadius: "50%", border: "none", backgroundColor: "#f59e0b", fontSize: isMobile ? "1.5rem" : "1.875rem", boxShadow: "0 4px 15px rgba(245,158,11,0.4)", cursor: "pointer" }}
            >
                🛒
                {cartItems.length > 0 && (
                    <span style={{ position: "absolute", top: 0, right: 0, display: "flex", height: isMobile ? "24px" : "28px", width: isMobile ? "24px" : "28px", alignItems: "center", justifyContent: "center", borderRadius: "50%", backgroundColor: "#ef4444", fontSize: isMobile ? "0.75rem" : "0.875rem", fontWeight: "bold", color: "#fff", boxShadow: "0 4px 6px -1px rgba(0, 0, 0, 0.1)" }}>
                        {cartItems.reduce((acc, i) => acc + i.quantity, 0)}
                    </span>
                )}
            </button>

            {/* Modais */}
            <StorefrontCartDrawer
                isOpen={isCartOpen}
                onClose={() => setIsCartOpen(false)}
                items={cartItems}
                onUpdateQuantity={(productId, newQty) => {
                    if (newQty <= 0) {
                        setCartItems(prev => prev.filter(i => i.productId !== productId));
                    } else {
                        setCartItems(prev => prev.map(i => i.productId === productId ? { ...i, quantity: newQty } : i));
                    }
                }}
                onRemoveItem={(productId) => setCartItems(prev => prev.filter(i => i.productId !== productId))}
                onCheckout={handleCheckoutCart}
                isSubmitting={addBatchMutation.isPending}
                customerData={customerData}
                onOpenAuthModal={() => setIsAuthModalOpen(true)}
            />

            <StorefrontAuthModal
                isOpen={isAuthModalOpen}
                onClose={() => setIsAuthModalOpen(false)}
                branchId={branchId}
                onAuthenticated={handleAuthenticatedSuccess}
            />

            {selectingItem && (
                <ComplementSelectorModal
                    productName={selectingItem.name}
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