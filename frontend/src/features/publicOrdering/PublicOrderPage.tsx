import { useState, useMemo } from "react";
import { useParams } from "react-router-dom";
import { useMutation, useQuery } from "@tanstack/react-query";
import Swal from "sweetalert2";
import { addPublicOrderItem, getPublicMenu } from "./api";
import { formatBRL } from "../../lib/types";
import type { MenuItemResponse, OrderItemComplementSelection } from "../../lib/types";
import { ComplementSelectorModal } from "../orders/ComplementSelectorModal";

// Importando as imagens
import logoImg from "../../image/logo.png";
import bgImg from "../../image/screenbackground_auth.jpeg";

type PendingOrder = {
    productId: number;
    quantity: number;
    complements?: OrderItemComplementSelection[];
};

type TableOrderView = {
    id: number;
    productName: string;
    quantity: number;
    totalPrice: number;
    status: "Pendente" | "Preparando" | "Pronto" | "Entregue";
};

export function PublicOrderPage() {
    const { token } = useParams<{ token: string }>();
    const [sentIds, setSentIds] = useState<number[]>([]);
    const [error, setError] = useState<string | null>(null);
    const [selectingItem, setSelectingItem] = useState<MenuItemResponse | null>(null);
    const [activeCategory, setActiveCategory] = useState<string>("Todas");
    const [searchQuery, setSearchQuery] = useState("");
    const [quantities, setQuantities] = useState<Record<number, number>>({});

    const [pendingOrder, setPendingOrder] = useState<PendingOrder | null>(null);
    const [destination, setDestination] = useState<"mesa" | "comanda">("mesa");
    const [commandNumber, setCommandNumber] = useState("");

    const [showMyOrders, setShowMyOrders] = useState(false);
    const [myOrdersStep, setMyOrdersStep] = useState<"select" | "view">("select");
    const [myOrdersDestination, setMyOrdersDestination] = useState<"mesa" | "comanda">("mesa");
    const [myOrdersCommandNumber, setMyOrdersCommandNumber] = useState("");

    const menuQuery = useQuery({
        queryKey: ["public-menu", token],
        queryFn: () => getPublicMenu(token!),
        enabled: !!token,
        retry: false,
    });

    const myOrdersQuery = useQuery({
        queryKey: ["public-my-orders", token, myOrdersDestination, myOrdersCommandNumber],
        queryFn: async (): Promise<TableOrderView[]> => {
            await new Promise(resolve => setTimeout(resolve, 800));
            return [
                { id: 1, productName: "Cerveja Heiniken Garrafa 600ml", quantity: 2, totalPrice: 49.98, status: "Entregue" },
                { id: 2, productName: "Mini contra filé", quantity: 1, totalPrice: 39.99, status: "Preparando" }
            ];
        },
        enabled: showMyOrders && myOrdersStep === "view",
    });

    const addMutation = useMutation({
        mutationFn: ({
            productId,
            complements,
            quantity = 1,
            command,
        }: {
            productId: number;
            complements?: OrderItemComplementSelection[];
            quantity?: number;
            command?: string;
        }) => {
            return addPublicOrderItem(token!, productId, quantity, command || null, complements);
        },
        onSuccess: (_result, { productId }) => {
            setError(null);
            setSentIds((current) => [...current, productId]);
            setSelectingItem(null);
            setPendingOrder(null);
            setCommandNumber("");
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
        onError: (e) => setError(e instanceof Error ? e.message : "Falha ao enviar o pedido."),
    });

    const getQty = (productId: number) => quantities[productId] || 1;

    const setQty = (productId: number, newQty: number) => {
        setQuantities(prev => ({ ...prev, [productId]: Math.max(1, newQty) }));
    };

    const handlePickItem = (item: MenuItemResponse) => {
        const currentQty = getQty(item.id);
        if (item.complementGroups && item.complementGroups.length > 0) {
            setSelectingItem(item);
        } else {
            setPendingOrder({ productId: item.id, quantity: currentQty });
            setDestination("mesa");
        }
    };

    // Extrai categorias dinâmicas e agrupa os itens para exibição condicional
    const { categoryList, groupedItems, filteredItems } = useMemo(() => {
        if (!menuQuery.data) return { categoryList: [], groupedItems: {}, filteredItems: [] };

        const items = menuQuery.data.items;

        // Extrai nomes únicos das categorias dos produtos
        const uniqueCategories = Array.from(new Set(items.map((i: any) => i.categoryName || "Geral")));
        const cats = ["Todas", ...uniqueCategories];

        let resultItems = items;

        if (activeCategory !== "Todas") {
            resultItems = resultItems.filter((i: any) => (i.categoryName || "Geral") === activeCategory);
        }

        if (searchQuery) {
            resultItems = resultItems.filter(i => i.name.toLowerCase().includes(searchQuery.toLowerCase()));
        }

        // Agrupa os itens por categoria para quando estiver em "Todas"
        const grouped: Record<string, MenuItemResponse[]> = {};
        resultItems.forEach((item: any) => {
            const catName = item.categoryName || "Geral";
            if (!grouped[catName]) grouped[catName] = [];
            grouped[catName].push(item);
        });

        return { categoryList: cats, groupedItems: grouped, filteredItems: resultItems };
    }, [menuQuery.data, activeCategory, searchQuery]);

    const openMyOrders = () => {
        setMyOrdersStep("select");
        setShowMyOrders(true);
    };

    if (!token) return null;

    if (menuQuery.isLoading)
        return (
            <main style={{ padding: 24, textAlign: "center", color: "#a8a8b3", backgroundColor: "#121214", minHeight: "100vh" }}>
                Carregando cardápio…
            </main>
        );

    if (menuQuery.isError)
        return (
            <main style={{ padding: 24, textAlign: "center", backgroundColor: "#121214", minHeight: "100vh" }}>
                <p style={{ color: "#ef4444" }}>
                    Não foi possível carregar o cardápio. Peça a um garçom para gerar um novo QR Code para esta mesa.
                </p>
            </main>
        );

    const menu = menuQuery.data!;
    const totalConta = (myOrdersQuery.data || []).reduce((acc, order) => acc + order.totalPrice, 0);

    return (
        <>
            <style>{`
                @keyframes fadeInAlpha {
                    0% { opacity: 0; transform: translateY(-15px); }
                    100% { opacity: 1; transform: translateY(0); }
                }
                .alpha-load {
                    animation: fadeInAlpha 0.6s cubic-bezier(0.2, 0.8, 0.2, 1) forwards;
                }
            `}</style>

            <main className="alpha-load" style={{ backgroundColor: "#121214", minHeight: "100vh", paddingBottom: 100, color: "#e1e1e6", fontFamily: "sans-serif", position: "relative" }}>

                {/* Cabeçalho */}
                <div style={{
                    display: "flex",
                    justifyContent: "space-between",
                    alignItems: "center",
                    padding: "8px 20px",
                    backgroundImage: `linear-gradient(rgba(18, 18, 20, 0.85), rgba(18, 18, 20, 0.95)), url(${bgImg})`,
                    backgroundSize: "cover",
                    backgroundPosition: "center",
                    borderBottom: "1px solid #29292e",
                    height: "80px",
                    boxSizing: "border-box"
                }}>
                    <img
                        src={logoImg}
                        alt="Logotipo SyncBar"
                        style={{ height: 50, objectFit: "contain", position: "relative", zIndex: 2 }}
                    />

                    <div style={{ textAlign: "right", position: "relative", zIndex: 2 }}>
                        <div style={{ color: "#ffffff", fontSize: "1.15rem", fontWeight: "600" }}>
                            Mesa <span style={{ color: "#f59e0b" }}>{menu.tableNumber}</span>
                        </div>
                    </div>
                </div>

                <div style={{ padding: "0 16px", maxWidth: 900, margin: "12px auto 0" }}>

                    {/* Abas de Categoria */}
                    <div style={{ display: "flex", overflowX: "auto", gap: 24, paddingBottom: 4, borderBottom: "1px solid #323238", WebkitOverflowScrolling: "touch" }}>
                        {categoryList.map((cat: string) => {
                            const isActive = activeCategory === cat;
                            return (
                                <button
                                    key={cat}
                                    onClick={() => setActiveCategory(cat)}
                                    style={{
                                        background: "none", border: "none",
                                        padding: "0 0 8px 0",
                                        whiteSpace: "nowrap",
                                        fontWeight: isActive ? "bold" : "normal",
                                        color: isActive ? "#f59e0b" : "#a8a8b3",
                                        borderBottom: isActive ? "2px solid #f59e0b" : "2px solid transparent",
                                        cursor: "pointer", fontSize: "0.95rem", transition: "color 0.2s"
                                    }}
                                >
                                    {cat}
                                </button>
                            );
                        })}
                    </div>

                    <div style={{ marginTop: 12, position: "relative" }}>
                        <input
                            type="text"
                            placeholder="Pesquisar um produto..."
                            value={searchQuery}
                            onChange={(e) => setSearchQuery(e.target.value)}
                            style={{
                                width: "100%", padding: "14px 16px", borderRadius: 8,
                                border: "1px solid #323238", backgroundColor: "#202024", color: "#e1e1e6",
                                outline: "none", fontSize: "0.95rem", boxSizing: "border-box"
                            }}
                        />
                        <span style={{ position: "absolute", right: 16, top: 14, color: "#a8a8b3" }}>🔍</span>
                    </div>

                    {error && <p style={{ marginTop: 16, textAlign: "center", color: "#ef4444" }}>{error}</p>}

                    {/* RENDERIZAÇÃO CONDICIONAL DAS CATEGORIAS */}
                    {activeCategory === "Todas" && !searchQuery ? (
                        // Se estiver em "Todas" e sem pesquisa, agrupa e exibe os títulos de categoria
                        Object.entries(groupedItems).map(([categoryName, products]) => (
                            <div key={categoryName} style={{ marginTop: 28 }}>
                                {/* Título da Categoria Estilizado com Linha (Igual ao protótipo) */}
                                <div style={{ marginBottom: 16 }}>
                                    <h2 style={{ fontSize: "1.05rem", textTransform: "uppercase", letterSpacing: 1, color: "#fff", margin: 0, paddingBottom: 6, borderBottom: "2px solid #f59e0b", display: "inline-block" }}>
                                        {categoryName}
                                    </h2>
                                </div>

                                <div style={{
                                    display: "grid",
                                    gap: 16,
                                    gridTemplateColumns: "repeat(auto-fill, minmax(320px, 1fr))"
                                }}>
                                    {products.map((item) => renderProductCard(item))}
                                </div>
                            </div>
                        ))
                    ) : (
                        // Se estiver em uma categoria específica ou pesquisando, exibe direto sem os títulos repetidos
                        <div style={{
                            marginTop: 20,
                            display: "grid",
                            gap: 16,
                            gridTemplateColumns: "repeat(auto-fill, minmax(320px, 1fr))"
                        }}>
                            {filteredItems.map((item) => renderProductCard(item))}
                        </div>
                    )}

                    {filteredItems.length === 0 && (
                        <p style={{ textAlign: "center", color: "#a8a8b3", marginTop: 40 }}>Nenhum produto encontrado.</p>
                    )}
                </div>

                {/* Botão Flutuante de Conta */}
                <button
                    onClick={openMyOrders}
                    style={{
                        position: "fixed",
                        bottom: 24,
                        right: 24,
                        zIndex: 50,
                        backgroundColor: "#f59e0b",
                        color: "#121214",
                        border: "none",
                        borderRadius: "50%",
                        width: 64,
                        height: 64,
                        boxShadow: "0 4px 15px rgba(245, 158, 11, 0.4)",
                        fontSize: "1.8rem",
                        display: "flex",
                        alignItems: "center",
                        justifyContent: "center",
                        cursor: "pointer",
                        transition: "transform 0.2s"
                    }}
                    aria-label="Ver minha conta"
                >
                    🧾
                </button>

                {/* Modais de Conta e Complementos */}
                {showMyOrders && (
                    <div style={{ position: "fixed", inset: 0, backgroundColor: "rgba(0,0,0,0.8)", zIndex: 9999, display: "flex", alignItems: myOrdersStep === "select" ? "center" : "flex-end", justifyContent: "center", padding: myOrdersStep === "select" ? 16 : 0 }}>
                        {myOrdersStep === "select" && (
                            <div style={{ backgroundColor: "#1e1e24", padding: 24, borderRadius: 12, width: "100%", maxWidth: 400, border: "1px solid #323238", boxShadow: "0 10px 25px rgba(0,0,0,0.5)", animation: "fadeInAlpha 0.2s" }}>
                                <h3 style={{ marginTop: 0, marginBottom: 24, color: "#fff", fontSize: "1.2rem", textAlign: "center" }}>
                                    Qual conta deseja consultar?
                                </h3>

                                <div style={{ display: "flex", gap: 12, marginBottom: myOrdersDestination === "comanda" ? 16 : 24 }}>
                                    <button
                                        onClick={() => setMyOrdersDestination("mesa")}
                                        style={{ flex: 1, padding: "14px", borderRadius: 8, border: myOrdersDestination === "mesa" ? "2px solid #f59e0b" : "1px solid #323238", backgroundColor: myOrdersDestination === "mesa" ? "rgba(245, 158, 11, 0.1)" : "transparent", color: myOrdersDestination === "mesa" ? "#f59e0b" : "#a8a8b3", fontWeight: "bold", cursor: "pointer", transition: "all 0.2s" }}
                                    >
                                        Da Mesa
                                    </button>
                                    <button
                                        onClick={() => setMyOrdersDestination("comanda")}
                                        style={{ flex: 1, padding: "14px", borderRadius: 8, border: myOrdersDestination === "comanda" ? "2px solid #f59e0b" : "1px solid #323238", backgroundColor: myOrdersDestination === "comanda" ? "rgba(245, 158, 11, 0.1)" : "transparent", color: myOrdersDestination === "comanda" ? "#f59e0b" : "#a8a8b3", fontWeight: "bold", cursor: "pointer", transition: "all 0.2s" }}
                                    >
                                        Da Comanda
                                    </button>
                                </div>

                                {myOrdersDestination === "comanda" && (
                                    <div style={{ marginBottom: 24, animation: "fadeIn 0.2s" }}>
                                        <label style={{ display: "block", color: "#a8a8b3", marginBottom: 8, fontSize: "0.9rem" }}>Número da Comanda</label>
                                        <input
                                            type="number"
                                            value={myOrdersCommandNumber}
                                            onChange={(e) => setMyOrdersCommandNumber(e.target.value)}
                                            placeholder="Ex: 15"
                                            autoFocus
                                            style={{ width: "100%", padding: "14px 16px", borderRadius: 8, border: "1px solid #323238", backgroundColor: "#121214", color: "#fff", fontSize: "1rem", boxSizing: "border-box", outline: "none" }}
                                        />
                                    </div>
                                )}

                                <div style={{ display: "flex", gap: 12 }}>
                                    <button
                                        onClick={() => setShowMyOrders(false)}
                                        style={{ flex: 1, padding: "14px", borderRadius: 8, border: "none", backgroundColor: "#323238", color: "#fff", fontWeight: "bold", cursor: "pointer" }}
                                    >
                                        Cancelar
                                    </button>
                                    <button
                                        disabled={myOrdersDestination === "comanda" && !myOrdersCommandNumber}
                                        onClick={() => setMyOrdersStep("view")}
                                        style={{
                                            flex: 1, padding: "14px", borderRadius: 8, border: "none",
                                            backgroundColor: "#f59e0b", color: "#121214", fontWeight: "bold",
                                            cursor: (myOrdersDestination === "comanda" && !myOrdersCommandNumber) ? "not-allowed" : "pointer",
                                            opacity: (myOrdersDestination === "comanda" && !myOrdersCommandNumber) ? 0.5 : 1
                                        }}
                                    >
                                        Consultar
                                    </button>
                                </div>
                            </div>
                        )}

                        {myOrdersStep === "view" && (
                            <div style={{ backgroundColor: "#1e1e24", borderTopLeftRadius: 24, borderTopRightRadius: 24, padding: 24, width: "100%", maxHeight: "85vh", display: "flex", flexDirection: "column", boxShadow: "0 -5px 25px rgba(0,0,0,0.5)", animation: "fadeInAlpha 0.2s" }}>
                                <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", borderBottom: "1px solid #323238", paddingBottom: 16, marginBottom: 16 }}>
                                    <div style={{ display: "flex", alignItems: "center", gap: 12 }}>
                                        <button onClick={() => setMyOrdersStep("select")} style={{ background: "none", border: "none", color: "#a8a8b3", fontSize: "1.2rem", cursor: "pointer", display: "flex", alignItems: "center", padding: 0 }}>
                                            ←
                                        </button>
                                        <h2 style={{ margin: 0, fontSize: "1.2rem", color: "#fff" }}>
                                            {myOrdersDestination === "mesa" ? `Conta - Mesa ${menu.tableNumber}` : `Conta - Comanda ${myOrdersCommandNumber}`}
                                        </h2>
                                    </div>
                                    <button onClick={() => setShowMyOrders(false)} style={{ background: "none", border: "none", color: "#a8a8b3", fontSize: "1.5rem", cursor: "pointer" }}>✕</button>
                                </div>

                                <div style={{ flex: 1, overflowY: "auto", paddingRight: 4 }}>
                                    {myOrdersQuery.isLoading ? (
                                        <p style={{ textAlign: "center", color: "#a8a8b3", marginTop: 40 }}>Buscando pedidos...</p>
                                    ) : myOrdersQuery.isError ? (
                                        <p style={{ textAlign: "center", color: "#ef4444", marginTop: 40 }}>Erro ao carregar conta.</p>
                                    ) : myOrdersQuery.data?.length === 0 ? (
                                        <p style={{ textAlign: "center", color: "#a8a8b3", marginTop: 40 }}>Nenhum pedido feito ainda nesta conta.</p>
                                    ) : (
                                        <div style={{ display: "grid", gap: 12 }}>
                                            {myOrdersQuery.data?.map(order => (
                                                <div key={order.id} style={{ backgroundColor: "#202024", padding: 16, borderRadius: 8, border: "1px solid #323238" }}>
                                                    <div style={{ display: "flex", justifyContent: "space-between", marginBottom: 8 }}>
                                                        <span style={{ color: "#fff", fontWeight: "bold" }}>{order.quantity}x {order.productName}</span>
                                                        <span style={{ color: "#f59e0b", fontWeight: "bold" }}>{formatBRL(order.totalPrice)}</span>
                                                    </div>
                                                    <div style={{ display: "flex", justifyContent: "flex-end" }}>
                                                        <span style={{
                                                            fontSize: "0.8rem", padding: "4px 8px", borderRadius: 4, fontWeight: "bold",
                                                            backgroundColor: order.status === "Entregue" ? "rgba(34, 197, 94, 0.2)" : "rgba(245, 158, 11, 0.2)",
                                                            color: order.status === "Entregue" ? "#22c55e" : "#f59e0b"
                                                        }}>
                                                            {order.status}
                                                        </span>
                                                    </div>
                                                </div>
                                            ))}
                                        </div>
                                    )}
                                </div>

                                <div style={{ borderTop: "1px solid #323238", paddingTop: 16, marginTop: 16 }}>
                                    <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: 16 }}>
                                        <span style={{ color: "#a8a8b3", fontSize: "1.1rem" }}>Total Parcial</span>
                                        <span style={{ color: "#fff", fontSize: "1.4rem", fontWeight: "bold" }}>{formatBRL(totalConta)}</span>
                                    </div>
                                    <button
                                        onClick={() => setShowMyOrders(false)}
                                        style={{ width: "100%", padding: "16px", borderRadius: 8, border: "none", backgroundColor: "#f59e0b", color: "#121214", fontWeight: "bold", fontSize: "1.1rem", cursor: "pointer" }}
                                    >
                                        Continuar Comprando
                                    </button>
                                </div>
                            </div>
                        )}
                    </div>
                )}

                {selectingItem && (
                    <ComplementSelectorModal
                        productName={selectingItem.name}
                        groups={selectingItem.complementGroups}
                        onCancel={() => setSelectingItem(null)}
                        submitting={addMutation.isPending}
                        confirmLabel="ADICIONAR"
                        onConfirm={(complements: OrderItemComplementSelection[]) => {
                            setPendingOrder({ productId: selectingItem.id, quantity: 1, complements });
                            setSelectingItem(null);
                            setDestination("mesa");
                        }}
                    />
                )}

                {pendingOrder && (
                    <div style={{ position: "fixed", inset: 0, backgroundColor: "rgba(0,0,0,0.8)", zIndex: 9999, display: "flex", alignItems: "center", justifyContent: "center", padding: 16 }}>
                        <div style={{ backgroundColor: "#1e1e24", padding: 24, borderRadius: 12, width: "100%", maxWidth: 400, border: "1px solid #323238", boxShadow: "0 10px 25px rgba(0,0,0,0.5)" }}>
                            <h3 style={{ marginTop: 0, marginBottom: 24, color: "#fff", fontSize: "1.2rem", textAlign: "center" }}>
                                Onde deseja anotar este pedido?
                            </h3>

                            <div style={{ display: "flex", gap: 12, marginBottom: destination === "comanda" ? 16 : 24 }}>
                                <button
                                    onClick={() => setDestination("mesa")}
                                    style={{ flex: 1, padding: "14px", borderRadius: 8, border: destination === "mesa" ? "2px solid #f59e0b" : "1px solid #323238", backgroundColor: destination === "mesa" ? "rgba(245, 158, 11, 0.1)" : "transparent", color: destination === "mesa" ? "#f59e0b" : "#a8a8b3", fontWeight: "bold", cursor: "pointer", transition: "all 0.2s" }}
                                >
                                    Na Mesa
                                </button>
                                <button
                                    onClick={() => setDestination("comanda")}
                                    style={{ flex: 1, padding: "14px", borderRadius: 8, border: destination === "comanda" ? "2px solid #f59e0b" : "1px solid #323238", backgroundColor: destination === "comanda" ? "rgba(245, 158, 11, 0.1)" : "transparent", color: destination === "comanda" ? "#f59e0b" : "#a8a8b3", fontWeight: "bold", cursor: "pointer", transition: "all 0.2s" }}
                                >
                                    Na Comanda
                                </button>
                            </div>

                            {destination === "comanda" && (
                                <div style={{ marginBottom: 24, animation: "fadeIn 0.2s" }}>
                                    <label style={{ display: "block", color: "#a8a8b3", marginBottom: 8, fontSize: "0.9rem" }}>Número da Comanda</label>
                                    <input
                                        type="number"
                                        value={commandNumber}
                                        onChange={(e) => setCommandNumber(e.target.value)}
                                        placeholder="Ex: 15"
                                        autoFocus
                                        style={{ width: "100%", padding: "14px 16px", borderRadius: 8, border: "1px solid #323238", backgroundColor: "#121214", color: "#fff", fontSize: "1rem", boxSizing: "border-box", outline: "none" }}
                                    />
                                </div>
                            )}

                            <div style={{ display: "flex", gap: 12 }}>
                                <button
                                    onClick={() => { setPendingOrder(null); setCommandNumber(""); }}
                                    style={{ flex: 1, padding: "14px", borderRadius: 8, border: "none", backgroundColor: "#323238", color: "#fff", fontWeight: "bold", cursor: "pointer" }}
                                >
                                    Cancelar
                                </button>
                                <button
                                    disabled={destination === "comanda" && !commandNumber}
                                    onClick={() => {
                                        addMutation.mutate({
                                            productId: pendingOrder.productId,
                                            quantity: pendingOrder.quantity,
                                            complements: pendingOrder.complements,
                                            command: destination === "comanda" ? `Comanda ${commandNumber}` : undefined
                                        });
                                    }}
                                    style={{
                                        flex: 1, padding: "14px", borderRadius: 8, border: "none",
                                        backgroundColor: "#f59e0b", color: "#121214", fontWeight: "bold",
                                        cursor: (destination === "comanda" && !commandNumber) ? "not-allowed" : "pointer",
                                        opacity: (destination === "comanda" && !commandNumber) ? 0.5 : 1
                                    }}
                                >
                                    {addMutation.isPending ? "Enviando..." : "Confirmar"}
                                </button>
                            </div>
                        </div>
                    </div>
                )}
            </main>
        </>
    );

    // Função auxiliar para renderizar o card do produto de forma limpa
    function renderProductCard(item: MenuItemResponse) {
        const justSent = sentIds.includes(item.id);

        return (
            <div key={item.id} style={{
                display: "flex",
                backgroundColor: "#1e1e24",
                borderRadius: 12,
                padding: 16,
                border: "1px solid #29292e",
                boxShadow: "0 4px 6px rgba(0,0,0,0.3)"
            }}>
                <div style={{ width: 80, height: 80, borderRadius: 8, backgroundColor: "#323238", flexShrink: 0, overflow: "hidden" }}>
                    {item.imageUrl ? (
                        <img src={item.imageUrl} alt={item.name} style={{ width: "100%", height: "100%", objectFit: "cover" }} />
                    ) : (
                        <div style={{ width: "100%", height: "100%", display: "flex", alignItems: "center", justifyContent: "center", color: "#555" }}>📷</div>
                    )}
                </div>

                <div style={{ marginLeft: 16, flex: 1, display: "flex", flexDirection: "column", justifyContent: "space-between" }}>
                    <div>
                        <h3 style={{ margin: 0, fontSize: "1rem", color: "#ffffff", fontWeight: "600" }}>{item.name}</h3>
                        {item.description && (
                            <p style={{ margin: "4px 0 0", fontSize: "0.85rem", color: "#8d8d99", lineHeight: "1.3" }}>
                                {item.description}
                            </p>
                        )}
                    </div>

                    <span style={{ marginTop: 12, fontWeight: "bold", color: "#f59e0b", fontSize: "1.1rem" }}>
                        {item.complementGroups?.length ? "A partir de " : ""}{formatBRL(item.salePrice)}
                    </span>

                    <div style={{ display: "flex", justifyContent: "flex-end", alignItems: "center", gap: 12, marginTop: 12 }}>
                        <div style={{
                            display: "flex", alignItems: "center",
                            border: "1px solid #323238", borderRadius: 8,
                            overflow: "hidden", height: 36, backgroundColor: "#121214"
                        }}>
                            <button
                                type="button"
                                onClick={() => setQty(item.id, getQty(item.id) - 1)}
                                style={{ width: 36, height: "100%", background: "none", border: "none", color: "#a8a8b3", fontSize: "1.2rem", cursor: "pointer" }}
                            >−</button>
                            <span style={{ width: 28, textAlign: "center", color: "#ffffff", fontWeight: "500", fontSize: "0.95rem" }}>
                                {getQty(item.id)}
                            </span>
                            <button
                                type="button"
                                onClick={() => setQty(item.id, getQty(item.id) + 1)}
                                style={{ width: 36, height: "100%", background: "none", border: "none", color: "#a8a8b3", fontSize: "1.2rem", cursor: "pointer" }}
                            >+</button>
                        </div>

                        <button
                            type="button"
                            onClick={() => handlePickItem(item)}
                            disabled={addMutation.isPending}
                            style={{
                                backgroundColor: "#f59e0b",
                                color: "#121214",
                                border: "none",
                                borderRadius: 8,
                                padding: "0 20px",
                                height: 36,
                                fontWeight: "bold",
                                fontSize: "0.95rem",
                                cursor: addMutation.isPending ? "not-allowed" : "pointer",
                                opacity: addMutation.isPending ? 0.7 : 1
                            }}
                        >
                            {justSent ? "Pedir de novo" : "Pedir"}
                        </button>
                    </div>
                </div>
            </div>
        );
    }
}