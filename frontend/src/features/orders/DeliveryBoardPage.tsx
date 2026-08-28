import React, { useEffect, useMemo, useRef, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { getOpenOrdersByBranch, getOrder, updateItemStatus } from "./api";
import { useAuthStore } from "../../stores/authStore";
import { useToast } from "../../ui/Toast";
import { ApiError } from "../../lib/apiClient";
import { OrderDrawer } from "./OrderDrawer";
import { OpenDeliveryOrderDialog } from "./OpenDeliveryOrderDialog";
import { OrderItemStatus, OrderStatus, OrderType, formatBRL } from "../../lib/types";
import type { OrderResponse } from "../../lib/types";

// --- IMPORTAÇÃO DAS IMAGENS ---
// Ajuste o caminho "../../image/" conforme a localização exata deste arquivo dentro de "src"
import bagImg from "../../image/bag.png";
import doorbellImg from "../../image/doorbell.png";
import packageImg from "../../image/package.png";
import positionImg from "../../image/position.png";
import bagcheckImg from "../../image/bagcheck.png";
import calendarImg from "../../image/calendar.png";
import motorcycleImg from "../../image/motorcycle.jpeg";
import screenBgImg from "../../image/screenbackground.jpeg";

// --- TIPAGENS E CONSTANTES ---
type Stage = "novo" | "cozinha" | "aguardando" | "rota" | "entregue" | "cancelado";
type ViewMode = "simples" | "completo";
type ChannelFilter = "todos" | "delivery" | "retirada";

const VIEW_MODE_KEY = "syncbar:delivery-view-mode";
const ON_ROUTE_KEY = "syncbar:delivery-on-route";
const GHOST_LIMIT = 40;

const CHANNEL_FILTER_LABELS: Record<ChannelFilter, string> = {
    todos: "Todos",
    delivery: "Delivery",
    retirada: "Retirada",
};

// --- FUNÇÕES UTILITÁRIAS ---
function loadViewMode(): ViewMode {
    try { return localStorage.getItem(VIEW_MODE_KEY) === "completo" ? "completo" : "simples"; }
    catch { return "simples"; }
}

function loadOnRoute(): Set<number> {
    try {
        const stored = localStorage.getItem(ON_ROUTE_KEY);
        return stored ? new Set(JSON.parse(stored) as number[]) : new Set();
    } catch { return new Set(); }
}

function persistOnRoute(ids: Set<number>) {
    try { localStorage.setItem(ON_ROUTE_KEY, JSON.stringify([...ids])); } catch { }
}

const isDeliveryBoardOrder = (order: OrderResponse) => order.diningTableId === null && order.comandaId === null;
const getChannel = (order: OrderResponse): "delivery" | "retirada" => order.orderTypeId === OrderType.Delivery ? "delivery" : "retirada";
const READY_ITEM_STATUSES = new Set<number>([OrderItemStatus.Pronto, OrderItemStatus.Entregue]);

function deriveStage(order: OrderResponse, onRoute: Set<number>): Stage {
    if (order.orderStatusId === OrderStatus.Cancelado) return "cancelado";
    if (order.orderStatusId === OrderStatus.Pago) return "entregue";

    const activeItems = order.items.filter((i) => i.orderItemStatusId !== OrderItemStatus.Cancelado);
    const allReady = activeItems.length > 0 && activeItems.every((i) => READY_ITEM_STATUSES.has(i.orderItemStatusId));
    const anyStarted = activeItems.some((i) => i.orderItemStatusId >= OrderItemStatus.EnviadoCozinha);

    if (order.orderStatusId === OrderStatus.AguardandoPagamento || allReady)
        return onRoute.has(order.id) ? "rota" : "aguardando";
    if (anyStarted) return "cozinha";
    return "novo";
}

function elapsedLabel(openedAt: string): string {
    const opened = new Date(openedAt).getTime();
    const minutes = Math.max(0, Math.round((Date.now() - opened) / 60_000));
    if (minutes < 60) return `${minutes} min`;
    const hours = Math.floor(minutes / 60);
    return `${hours}h${String(minutes % 60).padStart(2, "0")}`;
}

// --- DEFINIÇÃO DE COLUNAS ---
interface ColumnDef {
    id: Stage | "agendamento";
    label: string;
    hint: string;
    icon: React.ReactNode;
    emptyIllustration: React.ReactNode;
    themeColor: string;
    placeholder?: boolean;
}

const illustrationStyle = { width: 140, height: 140, objectFit: "contain" as const };

const FULL_COLUMNS: ColumnDef[] = [
    {
        id: "novo", label: "NOVOS PEDIDOS", hint: "Aguardando aceite", themeColor: "#FF6B00",
        icon: <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" style={{ width: 20, height: 20 }}><path d="M6 2L3 6v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2V6l-3-4z"></path><line x1="3" y1="6" x2="21" y2="6"></line><path d="M16 10a4 4 0 0 1-8 0"></path></svg>,
        emptyIllustration: <img src={bagImg} alt="Novos Pedidos" style={illustrationStyle} />
    },
    {
        id: "cozinha", label: "COZINHA", hint: "Aguardando preparo", themeColor: "#FF8A00",
        icon: <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" style={{ width: 20, height: 20 }}><path d="M12 2v20M17 5H9.5a3.5 3.5 0 0 0 0 7h5a3.5 3.5 0 0 1 0 7H6"></path></svg>,
        emptyIllustration: <img src={doorbellImg} alt="Cozinha" style={illustrationStyle} />
    },
    {
        id: "aguardando", label: "AGUARDANDO ENTREGA", hint: "Separação e conferência", themeColor: "#FF5500",
        icon: <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" style={{ width: 20, height: 20 }}><line x1="16.5" y1="9.4" x2="7.5" y2="4.21"></line><path d="M21 16V8a2 2 0 0 0-1-1.73l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.73l7 4a2 2 0 0 0 2 0l7-4A2 2 0 0 0 21 16z"></path><polyline points="3.27 6.96 12 12.01 20.73 6.96"></polyline><line x1="12" y1="22.08" x2="12" y2="12"></line></svg>,
        emptyIllustration: <img src={packageImg} alt="Aguardando Entrega" style={illustrationStyle} />
    },
    {
        id: "rota", label: "SAIU PARA ENTREGA", hint: "Pedidos em rota", themeColor: "#FFB800",
        icon: <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" style={{ width: 20, height: 20 }}><path d="M21 10c0 7-9 13-9 13s-9-6-9-13a9 9 0 0 1 18 0z"></path><circle cx="12" cy="10" r="3"></circle></svg>,
        emptyIllustration: <img src={positionImg} alt="Em Rota" style={illustrationStyle} />
    },
    {
        id: "entregue", label: "ENTREGUE", hint: "Última hora", themeColor: "#F5C344",
        icon: <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" style={{ width: 20, height: 20 }}><polyline points="20 6 9 17 4 12"></polyline></svg>,
        emptyIllustration: <img src={bagcheckImg} alt="Entregue" style={illustrationStyle} />
    },
    {
        id: "agendamento", label: "AGENDAMENTO", hint: "Em breve", themeColor: "#B0B0B0", placeholder: true,
        icon: <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" style={{ width: 20, height: 20 }}><rect x="3" y="4" width="18" height="18" rx="2" ry="2"></rect><line x1="16" y1="2" x2="16" y2="6"></line><line x1="8" y1="2" x2="8" y2="6"></line><line x1="3" y1="10" x2="21" y2="10"></line></svg>,
        emptyIllustration: <img src={calendarImg} alt="Agendamento" style={illustrationStyle} />
    },
];

const SIMPLE_COLUMNS = FULL_COLUMNS.filter(c => c.id !== "agendamento" && c.id !== "cancelado");

// --- COMPONENTES VISUAIS ---
function OrderCard({ order, stage, dense, onOpen, onSendToKitchen, onMarkReady, onMarkOnRoute, busy }: any) {
    const customerName = order.customerName?.trim() || `Pedido #${order.id}`;
    const channelLabel = getChannel(order) === "delivery" ? "DELIVERY" : "RETIRADA";

    const stop = (fn: () => void) => (e: React.MouseEvent) => { e.stopPropagation(); fn(); };

    return (
        <button type="button" onClick={onOpen} style={{
            background: "#fff", border: "1px solid #EFEFEF", borderRadius: 12, padding: 16, width: "100%",
            textAlign: "left", cursor: "pointer", display: "flex", flexDirection: "column", gap: 12,
            boxShadow: "0 2px 8px rgba(0,0,0,0.04)", transition: "transform 0.1s"
        }}>
            <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
                <span style={{ fontWeight: 800, fontSize: "1.1rem", color: "#1A1A1A" }}>#{order.id}</span>
                <span style={{ fontSize: "0.75rem", fontWeight: 700, padding: "4px 8px", borderRadius: 6, background: "#F5F5F5", color: "#555", display: "flex", alignItems: "center", gap: 4 }}>
                    {channelLabel === "DELIVERY" ? "🛵" : "🏬"} {channelLabel}
                </span>
            </div>

            <div style={{ display: "flex", flexDirection: "column", gap: 4 }}>
                <span style={{ fontWeight: 700, color: "#1A1A1A", fontSize: "0.95rem" }}>{customerName}</span>
                {!dense && order.deliveryAddress && (
                    <span style={{ fontSize: "0.8rem", color: "#777", lineHeight: 1.3 }}>{order.deliveryAddress}</span>
                )}
            </div>

            <div style={{ display: "flex", justifyContent: "space-between", fontSize: "0.85rem", color: "#555", borderTop: "1px dashed #EEE", paddingTop: 12 }}>
                <span style={{ fontWeight: 600 }}>R$ {formatBRL(order.totalAmount).replace('R$', '').trim()}</span>
                <span>{order.items.length} {order.items.length === 1 ? "item" : "itens"}</span>
            </div>

            <div style={{ fontSize: "0.75rem", color: "#888", display: "flex", justifyContent: "space-between" }}>
                <span>aberto há {elapsedLabel(order.openedAt)}</span>
            </div>

            {stage === "novo" && <button onClick={stop(onSendToKitchen)} disabled={busy} style={btnActionStyle}>{busy ? "Enviando…" : "Enviar p/ cozinha"}</button>}
            {stage === "cozinha" && <button onClick={stop(onMarkReady)} disabled={busy} style={btnActionStyle}>{busy ? "Atualizando…" : "Pronto p/ saída"}</button>}
            {stage === "aguardando" && <button onClick={stop(onMarkOnRoute)} disabled={busy} style={btnActionStyle}>Saiu para entrega</button>}
            {stage === "rota" && <button onClick={stop(onOpen)} disabled={busy} style={btnActionStyle}>Confirmar entrega</button>}
        </button>
    );
}

const btnActionStyle: React.CSSProperties = {
    width: "100%", padding: "10px", borderRadius: 8, border: "none", background: "linear-gradient(90deg, #FF7B00 0%, #FF5500 100%)",
    color: "#fff", fontWeight: 700, cursor: "pointer", marginTop: 4, fontSize: "0.9rem"
};

// --- PÁGINA PRINCIPAL ---
export function DeliveryBoardPage() {
    const queryClient = useQueryClient();
    const toast = useToast();
    const { branchId, employeeId } = useAuthStore();

    const [viewMode, setViewMode] = useState<ViewMode>(loadViewMode);
    const [channelFilter, setChannelFilter] = useState<ChannelFilter>("todos");
    const [search, setSearch] = useState("");
    const [onRoute, setOnRoute] = useState<Set<number>>(loadOnRoute);
    const [selectedOrderId, setSelectedOrderId] = useState<number | null>(null);
    const [openingNew, setOpeningNew] = useState(false);
    const [pendingOrderId, setPendingOrderId] = useState<number | null>(null);
    const [ghosts, setGhosts] = useState<Map<number, OrderResponse>>(new Map());
    const knownRef = useRef<Map<number, OrderResponse>>(new Map());
    const fetchingRef = useRef<Set<number>>(new Set());

    useEffect(() => { localStorage.setItem(VIEW_MODE_KEY, viewMode); }, [viewMode]);

    const ordersQuery = useQuery({
        queryKey: ["orders", "open", branchId],
        queryFn: () => getOpenOrdersByBranch(branchId),
        refetchInterval: 15_000,
    });

    useEffect(() => {
        if (!ordersQuery.data) return;
        const boardOrders = ordersQuery.data.filter(isDeliveryBoardOrder);
        const currentIds = new Set(boardOrders.map((o) => o.id));
        const vanished: number[] = [];

        for (const id of knownRef.current.keys()) {
            if (!currentIds.has(id)) vanished.push(id);
        }
        for (const order of boardOrders) knownRef.current.set(order.id, order);

        vanished.forEach((id) => {
            if (fetchingRef.current.has(id)) return;
            fetchingRef.current.add(id);
            knownRef.current.delete(id);
            getOrder(id).then((finalOrder) => {
                setGhosts((prev) => {
                    const next = new Map(prev);
                    next.set(id, finalOrder);
                    while (next.size > GHOST_LIMIT) {
                        const oldest = next.keys().next().value;
                        if (oldest === undefined) break;
                        next.delete(oldest);
                    }
                    return next;
                });
                setOnRoute((prevRoute) => {
                    if (!prevRoute.has(id)) return prevRoute;
                    const next = new Set(prevRoute);
                    next.delete(id);
                    persistOnRoute(next);
                    return next;
                });
            }).catch(() => { }).finally(() => fetchingRef.current.delete(id));
        });
    }, [ordersQuery.data]);

    const boardOrders = useMemo(() => {
        const map = new Map<number, OrderResponse>();
        for (const order of ghosts.values()) if (isDeliveryBoardOrder(order)) map.set(order.id, order);
        for (const order of ordersQuery.data ?? []) if (isDeliveryBoardOrder(order)) map.set(order.id, order);
        return [...map.values()];
    }, [ordersQuery.data, ghosts]);

    const filteredOrders = useMemo(() => {
        const term = search.trim().toLowerCase();
        return boardOrders.filter((order) => {
            if (channelFilter !== "todos" && getChannel(order) !== channelFilter) return false;
            if (term === "") return true;
            const haystack = [String(order.id), order.customerName ?? "", order.deliveryAddress ?? "", order.customerPhone ?? ""].join(" ").toLowerCase();
            return haystack.includes(term);
        });
    }, [boardOrders, channelFilter, search]);

    const ordersByStage = useMemo(() => {
        const map: Record<Stage, OrderResponse[]> = { novo: [], cozinha: [], aguardando: [], rota: [], entregue: [], cancelado: [] };
        for (const order of filteredOrders) map[deriveStage(order, onRoute)].push(order);
        for (const list of Object.values(map)) list.sort((a, b) => a.id - b.id);
        return map;
    }, [filteredOrders, onRoute]);

    const dashboardMetrics = useMemo(() => {
        const activeOrders = boardOrders.filter(o => o.orderStatusId !== OrderStatus.Cancelado);
        const total = activeOrders.length;
        const entregues = ordersByStage.entregue.length;
        const andamento = total - entregues;

        let sumMinutes = 0;
        activeOrders.forEach(o => {
            const opened = new Date(o.openedAt).getTime();
            sumMinutes += Math.max(0, (Date.now() - opened) / 60000);
        });
        const avgTime = total > 0 ? Math.round(sumMinutes / total) : 0;

        return { total, avgTime, entregues, andamento, agendados: 0 };
    }, [boardOrders, ordersByStage]);

    const refresh = () => void queryClient.invalidateQueries({ queryKey: ["orders"] });
    const onErr = (fallback: string) => (error: unknown) => toast.error(error instanceof ApiError ? error.message : fallback);

    const sendToKitchen = useMutation({
        mutationFn: async (order: OrderResponse) => {
            const pending = order.items.filter((i) => i.orderItemStatusId === OrderItemStatus.Lancado);
            await Promise.all(pending.map((i) => updateItemStatus(order.id, i.id, OrderItemStatus.EnviadoCozinha, employeeId)));
        },
        onSuccess: (_data, order) => { toast.success(`Pedido #${order.id} p/ cozinha.`); setPendingOrderId(null); refresh(); },
        onError: (e) => { onErr("Falha ao enviar.")(e); setPendingOrderId(null); },
    });

    const markReady = useMutation({
        mutationFn: async (order: OrderResponse) => {
            const pending = order.items.filter((i) => i.orderItemStatusId !== OrderItemStatus.Cancelado && !READY_ITEM_STATUSES.has(i.orderItemStatusId));
            await Promise.all(pending.map((i) => updateItemStatus(order.id, i.id, OrderItemStatus.Pronto, employeeId)));
        },
        onSuccess: (_data, order) => { toast.success(`Pedido #${order.id} pronto.`); setPendingOrderId(null); refresh(); },
        onError: (e) => { onErr("Falha ao marcar.")(e); setPendingOrderId(null); },
    });

    const handleSendToKitchen = (o: OrderResponse) => { setPendingOrderId(o.id); sendToKitchen.mutate(o); };
    const handleMarkReady = (o: OrderResponse) => { setPendingOrderId(o.id); markReady.mutate(o); };
    const handleMarkOnRoute = (o: OrderResponse) => { setOnRoute((prev) => { const next = new Set(prev).add(o.id); persistOnRoute(next); return next; }); };

    const columns = viewMode === "completo" ? FULL_COLUMNS : SIMPLE_COLUMNS;
    const dense = viewMode === "simples";

    return (
        <div style={{
            backgroundImage: `url(${screenBgImg})`,
            backgroundSize: "cover",
            backgroundPosition: "center",
            backgroundColor: "#FDF8F4",
            minHeight: "100vh",
            padding: "32px 40px",
            fontFamily: "system-ui, -apple-system, sans-serif",
            display: "flex",
            flexDirection: "column"
        }}>

            {/* HEADER TOP */}
            <header style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: 32 }}>
                <div style={{ display: "flex", alignItems: "center", gap: 16 }}>
                    <div>
                        <img src={motorcycleImg} alt="Moto de Delivery" style={{ width: 64, height: 64, objectFit: "contain", mixBlendMode: "multiply" }} />
                    </div>
                    <div>
                        <h1 style={{ fontSize: "2.4rem", fontWeight: 900, color: "#1A1A1A", margin: 0, textTransform: "uppercase", letterSpacing: "-1px" }}>Delivery</h1>
                        <span style={{ color: "#777", fontSize: "0.95rem" }}>Movimente cada pedido pelas etapas até a entrega</span>
                    </div>
                </div>

                <div style={{ display: "flex", gap: 24, alignItems: "center" }}>
                    <div style={{ display: "flex", background: "#fff", borderRadius: 8, padding: 4, border: "1px solid #EAEAEA", boxShadow: "0 2px 4px rgba(0,0,0,0.02)" }}>
                        <button
                            onClick={() => setViewMode("simples")}
                            style={{ padding: "8px 16px", borderRadius: 6, border: "none", fontWeight: 600, cursor: "pointer", background: viewMode === "simples" ? "#fff" : "transparent", color: viewMode === "simples" ? "#1A1A1A" : "#888", boxShadow: viewMode === "simples" ? "0 2px 8px rgba(0,0,0,0.1)" : "none" }}
                        >Simples</button>
                        <button
                            onClick={() => setViewMode("completo")}
                            style={{ padding: "8px 16px", borderRadius: 6, border: "none", fontWeight: 600, cursor: "pointer", background: viewMode === "completo" ? "#FF6B00" : "transparent", color: viewMode === "completo" ? "#fff" : "#888", boxShadow: viewMode === "completo" ? "0 2px 8px rgba(255,107,0,0.4)" : "none" }}
                        >Completo</button>
                    </div>

                    <button type="button" onClick={() => setOpeningNew(true)} style={{ background: "linear-gradient(90deg, #FF7B00 0%, #FF5500 100%)", color: "#fff", border: "none", padding: "12px 24px", borderRadius: 8, fontWeight: 700, fontSize: "1rem", cursor: "pointer", boxShadow: "0 4px 12px rgba(255, 85, 0, 0.3)" }}>
                        + Novo pedido
                    </button>
                </div>
            </header>

            {/* FILTROS E BUSCA */}
            <div style={{ display: "flex", gap: 16, marginBottom: 24, alignItems: "center" }}>
                <div style={{ display: "flex", gap: 8 }}>
                    {(["todos", "delivery", "retirada"] as ChannelFilter[]).map((c) => (
                        <button key={c} onClick={() => setChannelFilter(c)} style={{
                            padding: "10px 20px", borderRadius: 8, border: c === channelFilter ? "none" : "1px solid #EAEAEA",
                            background: c === channelFilter ? "#FF6B00" : "#fff", color: c === channelFilter ? "#fff" : "#555",
                            fontWeight: 700, cursor: "pointer", boxShadow: c === channelFilter ? "0 4px 12px rgba(255,107,0,0.2)" : "0 2px 4px rgba(0,0,0,0.02)"
                        }}>
                            {CHANNEL_FILTER_LABELS[c]}
                        </button>
                    ))}
                </div>

                <div style={{ position: "relative", flex: 1, maxWidth: 400 }}>
                    <input
                        placeholder="Buscar pedido, cliente ou endereço..."
                        value={search} onChange={(e) => setSearch(e.target.value)}
                        style={{ width: "100%", padding: "12px 16px 12px 40px", borderRadius: 8, border: "1px solid #EAEAEA", background: "#fff", fontSize: "0.95rem", boxShadow: "0 2px 4px rgba(0,0,0,0.02)", outline: "none" }}
                    />
                    <svg style={{ position: "absolute", left: 14, top: 12, width: 18, color: "#999" }} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><circle cx="11" cy="11" r="8"></circle><line x1="21" y1="21" x2="16.65" y2="16.65"></line></svg>
                </div>

                <div style={{ marginLeft: "auto", display: "flex", alignItems: "center", gap: 12, background: "#fff", padding: "8px 16px", borderRadius: 8, border: "1px solid #EAEAEA", fontSize: "0.85rem", color: "#666" }}>
                    <span style={{ display: "flex", alignItems: "center", gap: 6 }}><svg style={{ width: 14 }} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><path d="M21.5 2v6h-6M21.34 15.57a10 10 0 1 1-.59-9.21l-5.45-5.45" /></svg> Atualizar em 15s</span>
                    <span style={{ width: 1, height: 12, background: "#DDD" }}></span>
                    <span style={{ display: "flex", alignItems: "center", gap: 6 }}>Atualizado agora há 15s <span style={{ width: 8, height: 8, background: "#4CAF50", borderRadius: "50%" }}></span></span>
                </div>
            </div>

            {/* KANBAN BOARD */}
            <div style={{ display: "flex", gap: 16, flex: 1, overflowX: "auto", paddingBottom: 24, minHeight: 400 }}>
                {columns.map((col) => {
                    const items = col.id === "agendamento" ? [] : ordersByStage[col.id as Stage] || [];
                    return (
                        <div key={col.id} style={{ minWidth: 260, width: "16.6%", background: "#fff", borderRadius: 16, padding: "16px", display: "flex", flexDirection: "column", border: "1px solid #EFEFEF", boxShadow: "0 4px 12px rgba(0,0,0,0.02)" }}>

                            {/* Header da Coluna */}
                            <div style={{ display: "flex", gap: 12, marginBottom: 20 }}>
                                <div style={{ background: col.themeColor, color: "#fff", width: 42, height: 42, borderRadius: 10, display: "flex", alignItems: "center", justifyContent: "center", boxShadow: `0 4px 10px ${col.themeColor}40` }}>
                                    {col.icon}
                                </div>
                                <div style={{ flex: 1 }}>
                                    <div style={{ display: "flex", justifyContent: "space-between", alignItems: "flex-start" }}>
                                        <span style={{ fontWeight: 800, fontSize: "0.85rem", color: "#1A1A1A", lineHeight: 1.2 }}>{col.label}</span>
                                        <span style={{ background: `${col.themeColor}15`, color: col.themeColor, fontWeight: 800, padding: "2px 8px", borderRadius: 12, fontSize: "0.8rem" }}>
                                            {items.length}
                                        </span>
                                    </div>
                                    <span style={{ fontSize: "0.75rem", color: "#888", display: "block", marginTop: 2 }}>{col.hint}</span>
                                </div>
                            </div>

                            {/* Corpo da Coluna */}
                            <div style={{ display: "flex", flexDirection: "column", gap: 12, flex: 1, overflowY: "auto", position: "relative" }}>
                                {col.placeholder ? (
                                    <div style={{ display: "flex", flexDirection: "column", alignItems: "center", justifyContent: "center", height: "100%", padding: 20 }}>
                                        {col.emptyIllustration}
                                        <div style={{ textAlign: "center", color: "#999", fontSize: "0.85rem", marginTop: 24 }}>
                                            Agendamento de pedidos existe no SyncBar Delivery, reservado para quando essa funcionalidade for liberada.
                                        </div>
                                        <span style={{ background: "#F5F5F5", padding: "6px 12px", borderRadius: 12, fontWeight: 600, color: "#888", marginTop: 16, fontSize: "0.8rem" }}>Em breve</span>
                                    </div>
                                ) : items.length === 0 ? (
                                    <div style={{ display: "flex", flexDirection: "column", alignItems: "center", justifyContent: "center", height: "100%", opacity: 0.8, marginTop: 40 }}>
                                        <div style={{ marginBottom: 24 }}>
                                            {col.emptyIllustration}
                                        </div>
                                        <div style={{ color: "#BBB", fontSize: "1rem", fontWeight: 600 }}>Sem pedidos</div>
                                    </div>
                                ) : (
                                    items.map((order) => (
                                        <OrderCard
                                            key={order.id} order={order} stage={col.id as Stage} dense={dense} busy={pendingOrderId === order.id}
                                            onOpen={() => setSelectedOrderId(order.id)} onSendToKitchen={() => handleSendToKitchen(order)}
                                            onMarkReady={() => handleMarkReady(order)} onMarkOnRoute={() => handleMarkOnRoute(order)}
                                        />
                                    ))
                                )}
                            </div>
                        </div>
                    );
                })}
            </div>

            {/* DASHBOARD INFERIOR */}
            <div style={{ background: "#111", borderRadius: 16, padding: "24px 40px", display: "flex", justifyContent: "space-between", alignItems: "center", marginTop: "auto", boxShadow: "0 10px 40px rgba(255, 107, 0, 0.15)", borderBottom: "4px solid #FF6B00" }}>
                <DashboardMetric icon={<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" style={{ width: 24 }}><path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"></path><circle cx="12" cy="7" r="4"></circle></svg>} label="Total de pedidos" value={dashboardMetrics.total} sub="hoje" />
                <div style={{ width: 1, height: 40, background: "#333" }}></div>
                <DashboardMetric icon={<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" style={{ width: 24 }}><circle cx="12" cy="12" r="10"></circle><polyline points="12 6 12 12 16 14"></polyline></svg>} label="Tempo médio" value={dashboardMetrics.avgTime} sub="min" />
                <div style={{ width: 1, height: 40, background: "#333" }}></div>
                <DashboardMetric icon={<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" style={{ width: 24 }}><path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"></path><polyline points="22 4 12 14.01 9 11.01"></polyline></svg>} label="Entregues" value={dashboardMetrics.entregues} sub="hoje" />
                <div style={{ width: 1, height: 40, background: "#333" }}></div>
                <DashboardMetric icon={<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" style={{ width: 24 }}><polyline points="23 4 23 10 17 10"></polyline><polyline points="1 20 1 14 7 14"></polyline><path d="M3.51 9a9 9 0 0 1 14.85-3.36L23 10M1 14l4.64 4.36A9 9 0 0 0 20.49 15"></path></svg>} label="Em andamento" value={dashboardMetrics.andamento} sub="agora" />
                <div style={{ width: 1, height: 40, background: "#333" }}></div>
                <DashboardMetric icon={<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" style={{ width: 24 }}><rect x="3" y="4" width="18" height="18" rx="2" ry="2"></rect><line x1="16" y1="2" x2="16" y2="6"></line><line x1="8" y1="2" x2="8" y2="6"></line><line x1="3" y1="10" x2="21" y2="10"></line></svg>} label="Agendados" value={dashboardMetrics.agendados} sub="próximos dias" />
            </div>

            {selectedOrderId !== null && <OrderDrawer orderId={selectedOrderId} onClose={() => { setSelectedOrderId(null); refresh(); }} />}
            {openingNew && <OpenDeliveryOrderDialog onClose={() => setOpeningNew(false)} onOpened={(orderId) => { setOpeningNew(false); refresh(); setSelectedOrderId(orderId); }} />}
        </div>
    );
}

function DashboardMetric({ icon, label, value, sub }: { icon: React.ReactNode, label: string, value: number, sub: string }) {
    return (
        <div style={{ display: "flex", alignItems: "center", gap: 16 }}>
            <div style={{ color: "#FF6B00", border: "1.5px solid #FF6B00", borderRadius: 12, width: 48, height: 48, display: "flex", alignItems: "center", justifyContent: "center", background: "rgba(255, 107, 0, 0.1)" }}>{icon}</div>
            <div style={{ display: "flex", flexDirection: "column" }}>
                <span style={{ fontSize: "0.85rem", color: "#888", fontWeight: 500 }}>{label}</span>
                <div style={{ display: "flex", alignItems: "baseline", gap: 6 }}>
                    <span style={{ fontSize: "1.8rem", fontWeight: 800, color: "#fff", lineHeight: 1.1 }}>{value > 0 ? value : "--"}</span>
                    <span style={{ fontSize: "0.8rem", color: "#888" }}>{sub}</span>
                </div>
            </div>
        </div>
    );
}