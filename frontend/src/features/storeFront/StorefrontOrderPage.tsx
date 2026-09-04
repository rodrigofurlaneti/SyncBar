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

// Importando as imagens do projeto
import logoImg from "../../image/logo.png";
import bgImg from "../../image/screenbackground_auth.jpeg";

// Função para buscar o menu público da filial usando a rota correta do storefront
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

    // Estados de Sessão do Cliente Logado / Identificado
    const [customerData, setCustomerData] = useState<CustomerSessionData | null>(null);

    // Estados do Modal de Autenticação / Cadastro para o Checkout
    const [isAuthModalOpen, setIsAuthModalOpen] = useState(false);
    const [pendingCheckoutNotes, setPendingCheckoutNotes] = useState("");

    const [windowWidth, setWindowWidth] = useState(typeof window !== "undefined" ? window.innerWidth : 1200);
    useEffect(() => {
        const handleResize = () => setWindowWidth(window.innerWidth);
        window.addEventListener("resize", handleResize);
        return () => window.removeEventListener("resize", handleResize);
    }, []);

    const isMobile = windowWidth < 640;
    const isTvOrLarge = windowWidth > 1200;

    const menuQuery = useQuery({
        queryKey: ["storefront-menu", branchId],
        queryFn: () => fetchMenu(branchId),
        enabled: !!branchId,
        retry: false,
    });

    const addBatchMutation = useMutation({
        mutationFn: (payload: StorefrontOrderPayload) => {
            return submitStorefrontOrder(branchId, payload);
        },
        onSuccess: () => {
            setCartItems([]);
            setIsCartOpen(false);
            Swal.fire({
                title: "Pedido Solicitado!",
                text: "Seu pedido foi enviado com sucesso para a produção.",
                icon: "success",
                background: "#1e1e24",
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
                background: "#1e1e24",
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
            background: '#1e1e24',
            color: '#fff'
        });
    };

    const handleCheckoutCart = (generalNotes: string, activeCustomerData?: CustomerSessionData) => {
        if (cartItems.length === 0) return;
        setPendingCheckoutNotes(generalNotes);

        // Se o cliente ainda não estiver identificado, abre o modal de autenticação/cadastro
        const currentCustomer = activeCustomerData || customerData;
        if (!currentCustomer || !currentCustomer.customerId) {
            setIsAuthModalOpen(true);
            return;
        }

        // Caso já esteja autenticado, envia o pedido diretamente vinculando o customerId
        executeSubmitOrder(currentCustomer);
    };

    const handleAuthenticatedSuccess = (authenticatedData: CustomerSessionData) => {
        setCustomerData(authenticatedData);
        setIsAuthModalOpen(false);

        // Após autenticar/cadastrar com sucesso, prossegue automaticamente com o envio do pedido
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
            customerId: activeCustomer.customerId || null, // <-- CustomerId repassado corretamente
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

    if (menuQuery.isLoading)
        return <main data-testid="loading-menu" style={{ padding: 24, textAlign: "center", color: "#a8a8b3", backgroundColor: "#121214", minHeight: "100vh" }}>Carregando cardápio…</main>;
    if (menuQuery.isError)
        return <main data-testid="error-menu" style={{ padding: 24, textAlign: "center", backgroundColor: "#121214", minHeight: "100vh" }}><p style={{ color: "#ef4444" }}>Não foi possível carregar o cardápio.</p></main>;

    const gridColumns = isMobile
        ? "1fr"
        : isTvOrLarge
            ? "repeat(auto-fill, minmax(380px, 1fr))"
            : "repeat(auto-fill, minmax(300px, 1fr))";

    return (
        <main data-testid="storefront-order-page" style={{ backgroundColor: "#121214", minHeight: "100vh", paddingBottom: 100, color: "#e1e1e6", fontFamily: "sans-serif", position: "relative", fontSize: isTvOrLarge ? "1.1rem" : "1rem" }}>
            <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", padding: isMobile ? "8px 12px" : "12px 24px", backgroundImage: `linear-gradient(rgba(18, 18, 20, 0.85), rgba(18, 18, 20, 0.95)), url(${bgImg})`, backgroundSize: "cover", backgroundPosition: "center", borderBottom: "1px solid #29292e", height: isTvOrLarge ? "100px" : "80px", boxSizing: "border-box" }}>
                <img src={logoImg} alt="Logotipo SyncBar" style={{ height: isTvOrLarge ? 70 : 50, objectFit: "contain", position: "relative", zIndex: 2 }} />
                <div style={{ textAlign: "right", position: "relative", zIndex: 2 }}>
                    <div style={{ color: "#ffffff", fontSize: isTvOrLarge ? "1.5rem" : "1.15rem", fontWeight: "600" }} data-testid="header-store-title">
                        Cardápio <span style={{ color: "#f59e0b" }}>Digital</span>
                    </div>
                </div>
            </div>

            <div style={{ padding: isMobile ? "0 12px" : "0 24px", maxWidth: isTvOrLarge ? 1400 : 900, margin: "16px auto 0" }}>
                <div style={{ display: "flex", overflowX: "auto", gap: isTvOrLarge ? 32 : 24, paddingBottom: 6, borderBottom: "1px solid #323238" }}>
                    {categoryList.map((cat: string) => {
                        const isActive = activeCategory === cat;
                        return (
                            <button
                                key={cat}
                                data-testid={`category-tab-${cat.replace(/\s+/g, '-')}`}
                                onClick={() => setActiveCategory(cat)}
                                style={{ background: "none", border: "none", padding: "0 0 10px 0", whiteSpace: "nowrap", fontWeight: isActive ? "bold" : "normal", color: isActive ? "#f59e0b" : "#a8a8b3", borderBottom: isActive ? "2px solid #f59e0b" : "2px solid transparent", cursor: "pointer", fontSize: isTvOrLarge ? "1.2rem" : "0.95rem" }}
                            >
                                {cat}
                            </button>
                        );
                    })}
                </div>
                <div style={{ marginTop: 16, position: "relative" }}>
                    <input
                        type="text"
                        placeholder="Pesquisar um produto..."
                        value={searchQuery}
                        onChange={(e) => setSearchQuery(e.target.value)}
                        data-testid="input-menu-search"
                        style={{ width: "100%", padding: isTvOrLarge ? "18px 20px" : "14px 16px", borderRadius: 8, border: "1px solid #323238", backgroundColor: "#202024", color: "#e1e1e6", outline: "none", fontSize: isTvOrLarge ? "1.1rem" : "0.95rem", boxSizing: "border-box" }}
                    />
                    <span style={{ position: "absolute", right: 16, top: isTvOrLarge ? 18 : 14, color: "#a8a8b3" }}>🔍</span>
                </div>

                {activeCategory === "Todas" && !searchQuery ? (
                    Object.entries(groupedItems).map(([categoryName, products]) => (
                        <div key={categoryName} style={{ marginTop: 32 }} data-testid={`category-section-${categoryName.replace(/\s+/g, '-')}`}>
                            <div style={{ marginBottom: 16 }}>
                                <h2 style={{ fontSize: isTvOrLarge ? "1.3rem" : "1.05rem", textTransform: "uppercase", letterSpacing: 1, color: "#fff", margin: 0, paddingBottom: 6, borderBottom: "2px solid #f59e0b", display: "inline-block" }}>{categoryName}</h2>
                            </div>
                            <div style={{ display: "grid", gap: 16, gridTemplateColumns: gridColumns }}>
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
                        </div>
                    ))
                ) : (
                    <div style={{ marginTop: 24, display: "grid", gap: 16, gridTemplateColumns: gridColumns }} data-testid="filtered-items-grid">
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

            {/* Botão Flutuante do Carrinho / Cesta */}
            <button
                onClick={() => setIsCartOpen(true)}
                data-testid="btn-open-cart"
                style={{ position: "fixed", bottom: isTvOrLarge ? 36 : 24, right: isTvOrLarge ? 36 : 24, zIndex: 50, backgroundColor: "#f59e0b", color: "#121214", border: "none", borderRadius: "50%", width: isTvOrLarge ? 80 : 64, height: isTvOrLarge ? 80 : 64, boxShadow: "0 4px 15px rgba(245, 158, 11, 0.4)", fontSize: isTvOrLarge ? "2.2rem" : "1.8rem", display: "flex", alignItems: "center", justifyContent: "center", cursor: "pointer" }}
                aria-label="Ver Cesta"
            >
                🛒
                {cartItems.length > 0 && (
                    <span style={{ position: "absolute", top: 4, right: 4, backgroundColor: "#ef4444", color: "#fff", fontSize: "0.75rem", fontWeight: "bold", width: 22, height: 22, borderRadius: "50%", display: "flex", alignItems: "center", justifyContent: "center" }}>
                        {cartItems.reduce((acc, i) => acc + i.quantity, 0)}
                    </span>
                )}
            </button>

            {/* Gaveta do Carrinho de Autoatendimento */}
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

            {/* Modal de Autenticação / Cadastro para Finalizar Pedido */}
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