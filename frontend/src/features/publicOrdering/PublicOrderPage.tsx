import { useState, useMemo, useEffect } from "react";
import { useParams } from "react-router-dom";
import { useMutation, useQuery } from "@tanstack/react-query";
import Swal from "sweetalert2";
import { addPublicOrderItem, getPublicMenu, getPublicBill, getPublicComandaBill } from "./api";
import type { MenuItemResponse, OrderItemComplementSelection } from "../../lib/types";
import { ComplementSelectorModal } from "../orders/ComplementSelectorModal";
import { PublicOrderModal } from "./PublicOrderModal";
import { PublicOrderCard } from "./PublicOrderCard";
import { ComandaReadingValidation, LinkComandaValidation, needsReadingValidation } from "./ComandaReadingValidation";
// Importando as imagens
import logoImg from "../../image/logo.png";
import bgImg from "../../image/screenbackground_auth.jpeg";

type PendingOrder = {
    productId: number;
    quantity: number;
    complements?: OrderItemComplementSelection[];
};

export function PublicOrderPage() {
    const { token } = useParams<{ token: string }>();
    const [sentIds, setSentIds] = useState<number[]>([]);
    const [selectingItem, setSelectingItem] = useState<MenuItemResponse | null>(null);
    const [activeCategory, setActiveCategory] = useState<string>("Todas");
    const [searchQuery, setSearchQuery] = useState("");
    const [quantities, setQuantities] = useState<Record<number, number>>({});
    const [pendingOrder, setPendingOrder] = useState<PendingOrder | null>(null);
    const [destination, setDestination] = useState<"mesa" | "comanda">("mesa");
    const [commandNumber, setCommandNumber] = useState("");
    const [showMyOrders, setShowMyOrders] = useState(false);

    const [validatedComandaCodes, setValidatedComandaCodes] = useState<Set<string>>(new Set());
    const [linkedComandaCode, setLinkedComandaCode] = useState<string | null>(null);
    const [orderValidationStep, setOrderValidationStep] = useState<"select" | "validateComanda" | "linkComanda" | "submitting">("select");

    const [windowWidth, setWindowWidth] = useState(typeof window !== "undefined" ? window.innerWidth : 1200);
    useEffect(() => {
        const handleResize = () => setWindowWidth(window.innerWidth);
        window.addEventListener("resize", handleResize);
        return () => window.removeEventListener("resize", handleResize);
    }, []);

    const isMobile = windowWidth < 640;
    const isTvOrLarge = windowWidth > 1200;

    const menuQuery = useQuery({
        queryKey: ["public-menu", token],
        queryFn: () => getPublicMenu(token!),
        enabled: !!token,
        retry: false,
    });

    const addMutation = useMutation({
        mutationFn: ({ productId, complements, quantity = 1, comandaCode }: { productId: number; complements?: OrderItemComplementSelection[]; quantity?: number; comandaCode?: string }) => {
            return addPublicOrderItem(token!, productId, quantity, null, complements, comandaCode);
        },
        onSuccess: (_result, { productId }) => {
            setSentIds((current) => [...current, productId]);
            setSelectingItem(null);
            setPendingOrder(null);
            setCommandNumber("");
            setOrderValidationStep("select");
            setQuantities(prev => ({ ...prev, [productId]: 1 }));
            Swal.fire({
                title: "Sucesso!",
                text: "Pedido enviado com sucesso! Só aguardar.",
                icon: "success",
                background: "#1e1e24",
                color: "#ffffff",
                confirmButtonColor: "#f59e0b",
                confirmButtonText: "OK",
            });
        },
        onError: (e) => {
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
            setOrderValidationStep((step) => (step === "submitting" ? "linkComanda" : step));
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
        submitOrGateDirectOrder({ productId: item.id, quantity: currentQty });
    };

    const { categoryList, groupedItems, filteredItems } = useMemo(() => {
        if (!menuQuery.data) return { categoryList: [], groupedItems: {}, filteredItems: [] };
        const items = menuQuery.data.items;
        const uniqueCategories = Array.from(new Set(items.map((i: any) => i.categoryName || "Geral")));
        const cats = ["Todas", ...uniqueCategories];
        let resultItems = items;
        if (activeCategory !== "Todas") {
            resultItems = resultItems.filter((i: any) => (i.categoryName || "Geral") === activeCategory);
        }
        if (searchQuery) {
            resultItems = resultItems.filter((i: any) => i.name.toLowerCase().includes(searchQuery.toLowerCase()));
        }
        const grouped: Record<string, MenuItemResponse[]> = {};
        resultItems.forEach((item: any) => {
            const catName = item.categoryName || "Geral";
            if (!grouped[catName]) grouped[catName] = [];
            grouped[catName].push(item);
        });
        return { categoryList: cats, groupedItems: grouped, filteredItems: resultItems };
    }, [menuQuery.data, activeCategory, searchQuery]);

    if (!token) return null;
    if (menuQuery.isLoading)
        return <main data-testid="loading-menu" style={{ padding: 24, textAlign: "center", color: "#a8a8b3", backgroundColor: "#121214", minHeight: "100vh" }}>Carregando cardápio…</main>;
    if (menuQuery.isError)
        return <main data-testid="error-menu" style={{ padding: 24, textAlign: "center", backgroundColor: "#121214", minHeight: "100vh" }}><p style={{ color: "#ef4444" }}>Não foi possível carregar o cardápio.</p></main>;

    const menu = menuQuery.data!;
    const readingValidation = {
        isCameraInputEnabled: menu.isCameraInputEnabled,
        isBarcodeEnabled: menu.isBarcodeEnabled,
        isQrCodeEnabled: menu.isQrCodeEnabled,
    };

    const gridColumns = isMobile
        ? "1fr"
        : isTvOrLarge
            ? "repeat(auto-fill, minmax(380px, 1fr))"
            : "repeat(auto-fill, minmax(300px, 1fr))";

    const submitPendingOrder = () => {
        if (!pendingOrder) return;
        addMutation.mutate({
            productId: pendingOrder.productId,
            quantity: pendingOrder.quantity,
            complements: pendingOrder.complements,
            comandaCode: destination === "comanda" ? commandNumber : undefined,
        });
    };

    const handleConfirmPendingOrder = () => {
        if (destination === "comanda" && needsReadingValidation(readingValidation) && !validatedComandaCodes.has(commandNumber)) {
            setOrderValidationStep("validateComanda");
            return;
        }
        submitPendingOrder();
    };

    const submitOrGateDirectOrder = (order: PendingOrder) => {
        if (menu.isQrViewEnabled) {
            setPendingOrder(order);
            setDestination("mesa");
            setOrderValidationStep("select");
            return;
        }
        if (readingValidation.isCameraInputEnabled) {
            if (linkedComandaCode) {
                addMutation.mutate({ ...order, comandaCode: linkedComandaCode });
                return;
            }
            setPendingOrder(order);
            setOrderValidationStep("linkComanda");
            return;
        }
        addMutation.mutate(order);
    };

    return (
        <main data-testid="public-order-page" style={{ backgroundColor: "#121214", minHeight: "100vh", paddingBottom: 100, color: "#e1e1e6", fontFamily: "sans-serif", position: "relative", fontSize: isTvOrLarge ? "1.1rem" : "1rem" }}>
            <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", padding: isMobile ? "8px 12px" : "12px 24px", backgroundImage: `linear-gradient(rgba(18, 18, 20, 0.85), rgba(18, 18, 20, 0.95)), url(${bgImg})`, backgroundSize: "cover", backgroundPosition: "center", borderBottom: "1px solid #29292e", height: isTvOrLarge ? "100px" : "80px", boxSizing: "border-box" }}>
                <img src={logoImg} alt="Logotipo SyncBar" style={{ height: isTvOrLarge ? 70 : 50, objectFit: "contain", position: "relative", zIndex: 2 }} />
                <div style={{ textAlign: "right", position: "relative", zIndex: 2 }}>
                    <div style={{ color: "#ffffff", fontSize: isTvOrLarge ? "1.5rem" : "1.15rem", fontWeight: "600" }} data-testid="header-table-number">
                        Mesa <span style={{ color: "#f59e0b" }}>{menu.tableNumber}</span>
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
                                {products.map((item) => (
                                    <PublicOrderCard
                                        key={item.id}
                                        item={item}
                                        quantity={getQty(item.id)}
                                        isJustSent={sentIds.includes(item.id)}
                                        isPending={addMutation.isPending}
                                        onQuantityChange={(newQty) => setQty(item.id, newQty)}
                                        onAddItem={() => handlePickItem(item)}
                                    />
                                ))}
                            </div>
                        </div>
                    ))
                ) : (
                    <div style={{ marginTop: 24, display: "grid", gap: 16, gridTemplateColumns: gridColumns }} data-testid="filtered-items-grid">
                        {filteredItems.map((item) => (
                            <PublicOrderCard
                                key={item.id}
                                item={item as MenuItemResponse}
                                quantity={getQty(item.id)}
                                isJustSent={sentIds.includes(item.id)}
                                isPending={addMutation.isPending}
                                onQuantityChange={(newQty) => setQty(item.id, newQty)}
                                onAddItem={() => handlePickItem(item as MenuItemResponse)}
                            />
                        ))}
                    </div>
                )}
            </div>

            <button
                onClick={() => setShowMyOrders(true)}
                data-testid="btn-my-orders"
                style={{ position: "fixed", bottom: isTvOrLarge ? 36 : 24, right: isTvOrLarge ? 36 : 24, zIndex: 50, backgroundColor: "#f59e0b", color: "#121214", border: "none", borderRadius: "50%", width: isTvOrLarge ? 80 : 64, height: isTvOrLarge ? 80 : 64, boxShadow: "0 4px 15px rgba(245, 158, 11, 0.4)", fontSize: isTvOrLarge ? "2.2rem" : "1.8rem", display: "flex", alignItems: "center", justifyContent: "center", cursor: "pointer" }}
                aria-label="Ver minha conta"
            >
                🧾
            </button>

            <PublicOrderModal
                isOpen={showMyOrders}
                onClose={() => setShowMyOrders(false)}
                tableNumber={menu.tableNumber.toString()}
                token={token}
                isQrViewEnabled={menu.isQrViewEnabled}
                linkedComandaCode={linkedComandaCode}
                readingValidation={readingValidation}
                isComandaValidated={(code) => validatedComandaCodes.has(code)}
                onComandaValidated={(code) => setValidatedComandaCodes((prev) => new Set(prev).add(code))}
                onFetchMesaBill={() => getPublicBill(token!)}
                onFetchComandaBill={(code) => getPublicComandaBill(token!, code)}
            />

            {selectingItem && (
                <ComplementSelectorModal
                    productName={selectingItem.name}
                    groups={selectingItem.complementGroups}
                    onCancel={() => setSelectingItem(null)}
                    submitting={addMutation.isPending}
                    confirmLabel="ADICIONAR"
                    onConfirm={(complements: OrderItemComplementSelection[]) => {
                        const productId = selectingItem.id;
                        setSelectingItem(null);
                        submitOrGateDirectOrder({ productId, quantity: 1, complements });
                    }}
                />
            )}

            {pendingOrder && (
                <div data-testid="modal-pending-order" style={{ position: "fixed", inset: 0, backgroundColor: "rgba(0,0,0,0.8)", zIndex: 9999, display: "flex", alignItems: "center", justifyContent: "center", padding: 16 }}>
                    <div style={{ backgroundColor: "#1e1e24", padding: 24, borderRadius: 12, width: "100%", maxWidth: 400, border: "1px solid #323238" }}>
                        {orderValidationStep === "validateComanda" ? (
                            <ComandaReadingValidation
                                token={token}
                                comandaCode={commandNumber}
                                requirement={readingValidation}
                                onValidated={() => {
                                    setValidatedComandaCodes((prev) => new Set(prev).add(commandNumber));
                                    setOrderValidationStep("select");
                                    submitPendingOrder();
                                }}
                                onCancel={() => setOrderValidationStep("select")}
                            />
                        ) : orderValidationStep === "linkComanda" ? (
                            <LinkComandaValidation
                                token={token}
                                requirement={readingValidation}
                                onLinked={(code) => {
                                    setLinkedComandaCode(code);
                                    if (pendingOrder) {
                                        setOrderValidationStep("submitting");
                                        addMutation.mutate({ ...pendingOrder, comandaCode: code });
                                    } else {
                                        setOrderValidationStep("select");
                                    }
                                }}
                                onCancel={() => {
                                    setPendingOrder(null);
                                    setOrderValidationStep("select");
                                }}
                            />
                        ) : orderValidationStep === "submitting" ? (
                            <p data-testid="msg-submitting-order" style={{ textAlign: "center", color: "#a8a8b3", margin: "24px 0" }}>Enviando pedido…</p>
                        ) : (
                            <>
                                <h3 style={{ marginTop: 0, marginBottom: 24, color: "#fff", fontSize: "1.2rem", textAlign: "center" }}>Onde deseja anotar este pedido?</h3>
                                <div style={{ display: "flex", gap: 12, marginBottom: destination === "comanda" ? 16 : 24 }}>
                                    <button data-testid="btn-select-mesa" onClick={() => setDestination("mesa")} style={{ flex: 1, padding: "14px", borderRadius: 8, border: destination === "mesa" ? "2px solid #f59e0b" : "1px solid #323238", backgroundColor: destination === "mesa" ? "rgba(245, 158, 11, 0.1)" : "transparent", color: destination === "mesa" ? "#f59e0b" : "#a8a8b3", fontWeight: "bold", cursor: "pointer" }}>Na Mesa</button>
                                    <button data-testid="btn-select-comanda" onClick={() => setDestination("comanda")} style={{ flex: 1, padding: "14px", borderRadius: 8, border: destination === "comanda" ? "2px solid #f59e0b" : "1px solid #323238", backgroundColor: destination === "comanda" ? "rgba(245, 158, 11, 0.1)" : "transparent", color: destination === "comanda" ? "#f59e0b" : "#a8a8b3", fontWeight: "bold", cursor: "pointer" }}>Na Comanda</button>
                                </div>
                                {destination === "comanda" && (
                                    <div style={{ marginBottom: 24 }}>
                                        <label style={{ display: "block", color: "#a8a8b3", marginBottom: 8, fontSize: "0.9rem" }}>Número da Comanda</label>
                                        <input data-testid="input-command-number" type="text" value={commandNumber} onChange={(e) => setCommandNumber(e.target.value)} placeholder="Ex: 001" autoFocus style={{ width: "100%", padding: "14px 16px", borderRadius: 8, border: "1px solid #323238", backgroundColor: "#121214", color: "#fff", fontSize: "1rem", outline: "none", boxSizing: "border-box" }} />
                                    </div>
                                )}
                                <div style={{ display: "flex", gap: 12 }}>
                                    <button data-testid="btn-cancel-pending" onClick={() => { setPendingOrder(null); setCommandNumber(""); setOrderValidationStep("select"); }} style={{ flex: 1, padding: "14px", borderRadius: 8, border: "none", backgroundColor: "#323238", color: "#fff", fontWeight: "bold", cursor: "pointer" }}>Cancelar</button>
                                    <button data-testid="btn-confirm-pending" disabled={(destination === "comanda" && !commandNumber) || addMutation.isPending} onClick={handleConfirmPendingOrder} style={{ flex: 1, padding: "14px", borderRadius: 8, border: "none", backgroundColor: "#f59e0b", color: "#121214", fontWeight: "bold", cursor: "pointer" }}>{addMutation.isPending ? "Enviando..." : "Confirmar"}</button>
                                </div>
                            </>
                        )}
                    </div>
                </div>
            )}
        </main>
    );
}