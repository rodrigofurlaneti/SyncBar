import { useMemo, useState, useRef, useEffect, type CSSProperties } from "react";
import { useNavigate } from "react-router-dom";
import { useQuery, useQueryClient, useMutation } from "@tanstack/react-query";
import { getTablesByBranch } from "../tables/api";
import { getOpenOrdersByBranch, openOrder } from "../orders/api";
import { getComandasByBranch } from "../comandas/api";
import { getActiveAssignmentsByEmployee, getTablesByArea } from "../diningareas/api";
import { api, ApiError } from "../../lib/apiClient";
import { useAuthStore } from "../../stores/authStore";
import { useThemeStore } from "../../stores/themeStore";
import { useMyFeatures } from "../access/hooks";
import { OrderDrawer } from "../orders/OrderDrawer";
import { CashDrawer } from "../cash/CashDrawer";
import { QueryError } from "../../components/QueryError";
import {
    OrderItemStatus,
    TableStatus,
    formatBRL,
    orderTypeLabel,
} from "../../lib/types";
import type { ComandaResponse, OrderResponse, TableResponse } from "../../lib/types";

// ============================================================================
// COMPONENTE: Modal para Abrir Comanda
// ============================================================================
interface WaiterOpenComandaModalProps {
    comanda: ComandaResponse;
    onClose: () => void;
    onOpened: (orderId: number) => void;
}

function WaiterOpenComandaModal({ comanda, onClose, onOpened }: WaiterOpenComandaModalProps) {
    const { branchId, employeeId } = useAuthStore();
    const [customerName, setCustomerName] = useState("");

    const mutation = useMutation({
        mutationFn: () =>
            openOrder({
                branchId,
                diningTableId: null,
                comandaId: comanda.id,
                employeeId: employeeId ?? 1,
                guestCount: 1,
                notes: customerName.trim() === "" ? null : `Cliente: ${customerName.trim()}`,
            }),
        onSuccess: (orderId) => onOpened(orderId),
    });

    return (
        <div className="modal-backdrop is-center" onClick={onClose} style={{ position: "absolute" }}>
            <div className="modal-panel is-center" onClick={(e) => e.stopPropagation()} style={{ width: "90%", maxWidth: "360px", padding: "24px" }}>

                <div className="modal-head" style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: "20px" }}>
                    <span className="display" style={{ fontSize: "1.25rem", fontWeight: "800", textTransform: "uppercase" }}>
                        Abrir Comanda {comanda.code || comanda.id}
                    </span>
                    <button type="button" className="btn-ghost btn-icon" onClick={onClose} aria-label="Fechar">
                        ✕
                    </button>
                </div>

                <div style={{ display: "grid", gap: "8px", marginBottom: "24px", textAlign: "left" }}>
                    <label style={{ fontSize: "0.9rem", fontWeight: "600", color: "var(--ink-dim)" }}>
                        Nome do cliente
                    </label>
                    <input
                        value={customerName}
                        onChange={(e) => setCustomerName(e.target.value)}
                        autoFocus
                        placeholder="ex.: João Furlaneti"
                        style={{
                            padding: "12px",
                            borderRadius: "8px",
                            border: "1px solid var(--border)",
                            backgroundColor: "var(--bg-raise, #f3f4f6)",
                            color: "var(--ink)",
                            width: "100%",
                            fontSize: "1rem"
                        }}
                    />
                </div>

                {mutation.isError && (
                    <p style={{ color: "var(--w-warn, #ef4444)", fontSize: "0.85rem", marginBottom: "16px", fontWeight: "500", textAlign: "left" }}>
                        {mutation.error instanceof ApiError ? mutation.error.message : "Falha ao abrir comanda."}
                    </p>
                )}

                <div style={{ display: "flex", gap: "12px", justifyContent: "flex-end" }}>
                    <button
                        type="button"
                        className="btn-ghost"
                        onClick={onClose}
                        style={{ padding: "10px 16px", borderRadius: "8px", fontWeight: "600" }}
                    >
                        Voltar
                    </button>
                    <button
                        type="button"
                        className="waiter-cta"
                        onClick={() => mutation.mutate()}
                        disabled={mutation.isPending}
                        style={{ margin: 0, padding: "10px 20px", borderRadius: "8px", fontWeight: "700" }}
                    >
                        {mutation.isPending ? "Abrindo…" : "Abrir comanda"}
                    </button>
                </div>
            </div>
        </div>
    );
}

// ============================================================================
// COMPONENTE: Modal para Abrir Mesa
// ============================================================================
interface WaiterOpenTableModalProps {
    table: TableResponse;
    onClose: () => void;
    onOpened: (orderId: number) => void;
}

function WaiterOpenTableModal({ table, onClose, onOpened }: WaiterOpenTableModalProps) {
    const { branchId, employeeId } = useAuthStore();
    const [guestCount, setGuestCount] = useState<number>(2);

    const mutation = useMutation({
        mutationFn: () =>
            openOrder({
                branchId,
                diningTableId: table.id,
                comandaId: null,
                employeeId: employeeId ?? 1,
                guestCount,
                notes: null,
            }),
        onSuccess: (orderId) => onOpened(orderId),
    });

    return (
        <div className="modal-backdrop is-center" onClick={onClose} style={{ position: "absolute" }}>
            <div className="modal-panel is-center" onClick={(e) => e.stopPropagation()} style={{ width: "90%", maxWidth: "360px", padding: "24px" }}>

                <div className="modal-head" style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: "20px" }}>
                    <span className="display" style={{ fontSize: "1.25rem", fontWeight: "800", textTransform: "uppercase" }}>
                        Abrir Mesa {table.number}
                    </span>
                    <button type="button" className="btn-ghost btn-icon" onClick={onClose} aria-label="Fechar">
                        ✕
                    </button>
                </div>

                <div style={{ display: "grid", gap: "8px", marginBottom: "24px", textAlign: "left" }}>
                    <label style={{ fontSize: "0.9rem", fontWeight: "600", color: "var(--ink-dim)" }}>
                        Pessoas na mesa
                    </label>
                    <input
                        type="number"
                        min={1}
                        value={guestCount}
                        onChange={(e) => setGuestCount(Number(e.target.value))}
                        autoFocus
                        style={{
                            padding: "12px",
                            borderRadius: "8px",
                            border: "1px solid var(--border)",
                            backgroundColor: "var(--bg-raise, #f3f4f6)",
                            color: "var(--ink)",
                            width: "100%",
                            fontSize: "1rem"
                        }}
                    />
                </div>

                {mutation.isError && (
                    <p style={{ color: "var(--w-warn, #ef4444)", fontSize: "0.85rem", marginBottom: "16px", fontWeight: "500", textAlign: "left" }}>
                        {mutation.error instanceof ApiError ? mutation.error.message : "Falha ao abrir mesa."}
                    </p>
                )}

                <div style={{ display: "flex", gap: "12px", justifyContent: "flex-end" }}>
                    <button
                        type="button"
                        className="btn-ghost"
                        onClick={onClose}
                        style={{ padding: "10px 16px", borderRadius: "8px", fontWeight: "600" }}
                    >
                        Voltar
                    </button>
                    <button
                        type="button"
                        className="waiter-cta"
                        onClick={() => mutation.mutate()}
                        disabled={mutation.isPending}
                        style={{ margin: 0, padding: "10px 20px", borderRadius: "8px", fontWeight: "700" }}
                    >
                        {mutation.isPending ? "Abrindo…" : "Abrir mesa"}
                    </button>
                </div>
            </div>
        </div>
    );
}

// ============================================================================
// LÓGICA E COMPONENTE PRINCIPAL
// ============================================================================

interface WaiterMessageResponse {
    id: number;
    branchId: number;
    senderEmployeeId: number;
    recipientEmployeeId: number | null;
    diningAreaId: number | null;
    message: string;
    isRead: boolean;
    createdAt: string;
}

const getWaiterMessagesByBranch = (branchId: number, diningAreaId: number | null): Promise<WaiterMessageResponse[]> => {
    if (!diningAreaId) return Promise.resolve([]);
    return api<WaiterMessageResponse[]>(`/api/diningareas/messages/branch/${branchId}?diningAreaId=${diningAreaId}`);
};

type BadgeTone = "ready" | "preparing" | "waiting";

interface OrderBadge {
    label: string;
    tone: BadgeTone;
}

function deriveOrderBadge(order: OrderResponse): OrderBadge {
    const statuses = order.items.map((item) => item.orderItemStatusId);
    if (statuses.length === 0) return { label: "Sem itens", tone: "waiting" };
    if (statuses.some((s) => s === OrderItemStatus.Pronto)) return { label: "Pronto", tone: "ready" };
    if (statuses.some((s) => s === OrderItemStatus.EmPreparo || s === OrderItemStatus.EnviadoCozinha))
        return { label: "Em preparo", tone: "preparing" };
    if (statuses.some((s) => s === OrderItemStatus.Lancado)) return { label: "Aguardando", tone: "waiting" };
    return { label: "Entregue", tone: "ready" };
}

function orderLabel(
    order: OrderResponse,
    tablesById: Map<number, TableResponse>,
    comandasById: Map<number, ComandaResponse>,
): string {
    if (order.diningTableId !== null) {
        const table = tablesById.get(order.diningTableId);
        return `Mesa ${table?.number ?? order.diningTableId}`;
    }
    if (order.comandaId !== null) {
        const comanda = comandasById.get(order.comandaId);
        return `Comanda ${comanda?.code ?? order.comandaId}`;
    }
    const type = order.orderTypeId ? orderTypeLabel[order.orderTypeId] : "Pedido";
    return order.customerName ? `${type} · ${order.customerName}` : type;
}

function elapsedLabel(openedAt: string): string {
    const minutes = Math.max(0, Math.floor((Date.now() - new Date(openedAt).getTime()) / 60_000));
    if (minutes < 60) return `há ${minutes} min`;
    const hours = Math.floor(minutes / 60);
    return `há ${hours} h`;
}

function firstNameFrom(userName: string | null): string {
    if (!userName) return "Garçom";
    const beforeAt = userName.split("@")[0];
    const firstWord = beforeAt.trim().split(/[\s._-]+/)[0];
    if (!firstWord) return "Garçom";
    return firstWord.charAt(0).toUpperCase() + firstWord.slice(1).toLowerCase();
}

function initialsFrom(userName: string | null): string {
    if (!userName) return "GC";
    const base = userName.split("@")[0].trim();
    const parts = base.split(/[\s._-]+/).filter(Boolean);
    if (parts.length >= 2) return (parts[0][0] + parts[1][0]).toUpperCase();
    return base.slice(0, 2).toUpperCase();
}

const badgeToneVar: Record<BadgeTone, string> = {
    ready: "var(--w-ok, #22c55e)",
    preparing: "var(--w-info, #3b82f6)",
    waiting: "var(--w-warn, #f59e0b)",
};

type QuickActionKey = "transferir" | "mesas" | "calculadora" | "turno" | "comandas" | "nova" | "conta";

const quickActions: { key: QuickActionKey; icon: string; label: string }[] = [
    { key: "transferir", icon: "🔀", label: "Transferir Mesa" },
    { key: "mesas", icon: "🍽️", label: "Mesas" },
    { key: "calculadora", icon: "🔢", label: "Calculadora" },
    { key: "transferir", icon: "🔀", label: "Transferir Comanda" },
    { key: "comandas", icon: "📋", label: "Comandas" }
];

type TabKey = "inicio" | "mesas" | "comandas" | "pedidos" | "mensagens" | "perfil";

const tabs: { key: TabKey; icon: string; label: string }[] = [
    { key: "inicio", icon: "🏠", label: "Início" },
    { key: "pedidos", icon: "🧾", label: "Pedidos" },
    { key: "mensagens", icon: "💬", label: "Mensagens" },
    { key: "perfil", icon: "👤", label: "Perfil" },
];

export function WaiterDashboardPage() {
    const navigate = useNavigate();
    const queryClient = useQueryClient();
    const { branchId, userName, clear, employeeId } = useAuthStore();
    const { theme, toggleTheme } = useThemeStore();
    const featuresQuery = useMyFeatures();

    const canSeeCaixa = featuresQuery.data?.canManageAccess || featuresQuery.data?.features.includes("Caixa");

    const [activeTab, setActiveTab] = useState<TabKey>("inicio");
    const [selectedOrderId, setSelectedOrderId] = useState<number | null>(null);
    const [comandaToOpen, setComandaToOpen] = useState<ComandaResponse | null>(null);
    const [tableToOpen, setTableToOpen] = useState<TableResponse | null>(null);
    const [cashOpen, setCashOpen] = useState(false);
    const [profileOpen, setProfileOpen] = useState(false);
    const [toast, setToast] = useState<string | null>(null);

    // Estados para a Calculadora
    const [calculatorOpen, setCalculatorOpen] = useState(false);
    const [calcInput, setCalcInput] = useState("0");
    const [calcPrevValue, setCalcPrevValue] = useState<number | null>(null);
    const [calcOperator, setCalcOperator] = useState<string | null>(null);
    const [calcNewNumber, setCalcNewNumber] = useState(false);

    // Estados para o Modal de Transferência de Mesa/Item
    const [transferOpen, setTransferOpen] = useState(false);
    const [sourceTableId, setSourceTableId] = useState<string>("");
    const [targetTableId, setTargetTableId] = useState<string>("");
    const [selectedItemId, setSelectedItemId] = useState<string>("");
    const [isTransferring, setIsTransferring] = useState(false);

    // Referência para rolar o chat de mensagens para o final automaticamente
    const messagesEndRef = useRef<HTMLDivElement>(null);

    const showToast = (message: string) => {
        setToast(message);
        window.setTimeout(() => setToast((current) => (current === message ? null : current)), 2500);
    };

    // -------------------------------------------------------------
    // Funções da Calculadora
    // -------------------------------------------------------------
    const handleCalcNum = (num: string) => {
        if (calcNewNumber) {
            setCalcInput(num);
            setCalcNewNumber(false);
        } else {
            setCalcInput(calcInput === "0" && num !== "." ? num : calcInput + num);
        }
    };

    const handleCalcOp = (op: string) => {
        if (calcOperator && !calcNewNumber) {
            handleCalcEqual();
        } else {
            setCalcPrevValue(parseFloat(calcInput));
        }
        setCalcOperator(op);
        setCalcNewNumber(true);
    };

    const handleCalcEqual = () => {
        if (calcOperator && calcPrevValue !== null) {
            const current = parseFloat(calcInput);
            let result = 0;
            if (calcOperator === "+") result = calcPrevValue + current;
            if (calcOperator === "-") result = calcPrevValue - current;
            if (calcOperator === "*") result = calcPrevValue * current;
            if (calcOperator === "/") result = calcPrevValue / current;

            result = parseFloat(result.toFixed(4));

            setCalcInput(result.toString());
            setCalcPrevValue(null);
            setCalcOperator(null);
            setCalcNewNumber(true);
        }
    };

    const handleCalcClear = () => {
        setCalcInput("0");
        setCalcPrevValue(null);
        setCalcOperator(null);
        setCalcNewNumber(false);
    };
    // -------------------------------------------------------------

    const assignmentQuery = useQuery({
        queryKey: ["diningareaassignments", "active", employeeId],
        queryFn: () => getActiveAssignmentsByEmployee(employeeId ?? 0),
        enabled: !!employeeId,
    });

    const activeAreaId = assignmentQuery.data?.[0]?.diningAreaId ?? null;

    const areaTablesQuery = useQuery({
        queryKey: ["diningareatables", activeAreaId],
        queryFn: () => getTablesByArea(activeAreaId!),
        enabled: !!activeAreaId,
    });

    const allowedTableIds = useMemo(() => {
        const set = new Set<number>();
        for (const t of areaTablesQuery.data ?? []) {
            set.add(t.diningTableId);
        }
        return set;
    }, [areaTablesQuery.data]);

    const tablesQuery = useQuery({
        queryKey: ["tables", branchId],
        queryFn: () => getTablesByBranch(branchId),
        refetchInterval: 15_000,
    });

    const comandasQuery = useQuery({
        queryKey: ["comandas", branchId],
        queryFn: () => getComandasByBranch(branchId),
        refetchInterval: 15_000,
    });

    const ordersQuery = useQuery({
        queryKey: ["orders", "open", branchId],
        queryFn: () => getOpenOrdersByBranch(branchId),
        refetchInterval: 15_000,
    });

    const messagesQuery = useQuery({
        queryKey: ["waitermessages", branchId, activeAreaId],
        queryFn: () => getWaiterMessagesByBranch(branchId, activeAreaId),
        enabled: !!branchId && !!activeAreaId,
        refetchInterval: 10_000,
    });

    const sortedMessages = useMemo(() => {
        const msgs = messagesQuery.data ?? [];
        return [...msgs].sort((a, b) => new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime());
    }, [messagesQuery.data]);

    useEffect(() => {
        if (activeTab === "mensagens") {
            messagesEndRef.current?.scrollIntoView({ behavior: "smooth" });
        }
    }, [activeTab, sortedMessages.length]);

    const tablesById = useMemo(() => {
        const map = new Map<number, TableResponse>();
        for (const table of tablesQuery.data ?? []) map.set(table.id, table);
        return map;
    }, [tablesQuery.data]);

    const comandasById = useMemo(() => {
        const map = new Map<number, ComandaResponse>();
        for (const comanda of comandasQuery.data ?? []) map.set(comanda.id, comanda);
        return map;
    }, [comandasQuery.data]);

    const allActiveOrders = ordersQuery.data ?? [];

    const myOrders = useMemo(() => {
        if (!activeAreaId) return [];
        return allActiveOrders.filter(order =>
            (order.comandaId !== null) ||
            (order.diningTableId !== null && allowedTableIds.has(order.diningTableId))
        );
    }, [allActiveOrders, activeAreaId, allowedTableIds]);

    const myTables = useMemo(() => {
        return (tablesQuery.data ?? []).filter((t) => allowedTableIds.has(t.id));
    }, [tablesQuery.data, allowedTableIds]);

    const ordersByTableId = useMemo(() => {
        const map = new Map<number, OrderResponse>();
        for (const order of allActiveOrders) {
            if (order.diningTableId !== null) {
                map.set(order.diningTableId, order);
            }
        }
        return map;
    }, [allActiveOrders]);

    const sourceOrder = useMemo(() => {
        if (!sourceTableId) return null;
        return allActiveOrders.find(o => o.diningTableId === Number(sourceTableId)) || null;
    }, [allActiveOrders, sourceTableId]);

    const myOpenTablesCount = myTables.filter(
        (t) => t.tableStatusId === TableStatus.Ocupada || t.tableStatusId === TableStatus.EmFechamento
    ).length;

    const myTotalTables = myTables.length;

    // --- NOVA SEPARAÇÃO MESAS VS COMANDAS ---
    const tableOrders = useMemo(() => myOrders.filter((o) => o.diningTableId !== null), [myOrders]);
    const comandaOrders = useMemo(() => myOrders.filter((o) => o.comandaId !== null && o.diningTableId === null), [myOrders]);

    const totalTablesAmount = useMemo(() => tableOrders.reduce((sum, order) => sum + order.totalAmount, 0), [tableOrders]);
    const totalComandasAmount = useMemo(() => comandaOrders.reduce((sum, order) => sum + order.totalAmount, 0), [comandaOrders]);
    // ----------------------------------------

    const readyItemsCount = useMemo(() => myOrders.reduce((sum, order) => sum + order.items.filter((i) => i.orderItemStatusId === OrderItemStatus.Pronto).length, 0), [myOrders]);

    const latestOrders = useMemo(
        () => [...myOrders].sort((a, b) => new Date(b.openedAt).getTime() - new Date(a.openedAt).getTime()).slice(0, 3),
        [myOrders],
    );

    const refresh = () => {
        void queryClient.invalidateQueries({ queryKey: ["tables"] });
        void queryClient.invalidateQueries({ queryKey: ["orders"] });
        void queryClient.invalidateQueries({ queryKey: ["comandas"] });
        void queryClient.invalidateQueries({ queryKey: ["waitermessages"] });
    };

    const handleQuickAction = (key: QuickActionKey) => {
        switch (key) {
            case "mesas":
                setActiveTab("mesas");
                break;
            case "comandas":
                setActiveTab("comandas");
                break;
            case "calculadora":
                setCalculatorOpen(true);
                break;
            case "transferir":
                setTransferOpen(true);
                break;
            case "turno":
                setCashOpen(true);
                break;
        }
    };

    const handleTab = (key: TabKey) => {
        switch (key) {
            case "inicio":
            case "mesas":
            case "comandas":
            case "pedidos":
            case "mensagens":
                setActiveTab(key);
                break;
            case "perfil":
                setProfileOpen(true);
                break;
        }
    };

    const handleTableClick = (tableId: number, statusId: number) => {
        if (statusId === TableStatus.Livre) {
            const tableObj = myTables.find(t => t.id === tableId);
            if (tableObj) {
                setTableToOpen(tableObj);
            }
            return;
        }
        const order = myOrders.find((o) => o.diningTableId === tableId);
        if (order) {
            setSelectedOrderId(order.id);
        }
    };

    const handleExecuteTransfer = async (e: React.FormEvent) => {
        e.preventDefault();
        if (!sourceTableId || !targetTableId || !selectedItemId || !sourceOrder) {
            showToast("Preencha todos os campos da transferência.");
            return;
        }
        const targetOrder = allActiveOrders.find(o => o.diningTableId === Number(targetTableId));
        if (!targetOrder) {
            showToast("A mesa de destino precisa ter um pedido aberto para receber o item.");
            return;
        }
        setIsTransferring(true);
        try {
            await api("/api/orders/items/transfer", {
                method: "PUT",
                body: JSON.stringify({
                    sourceCustomerOrderId: sourceOrder.id,
                    targetCustomerOrderId: targetOrder.id,
                    customerOrderItemId: Number(selectedItemId),
                    sourceDiningTableId: Number(sourceTableId),
                    targetDiningTableId: Number(targetTableId),
                    actorEmployeeId: employeeId ?? 1,
                }),
            });
            showToast("Item transferido com sucesso!");
            setTransferOpen(false);
            setSourceTableId("");
            setTargetTableId("");
            setSelectedItemId("");
            refresh();
        } catch (err: any) {
            showToast(err?.message || "Erro ao realizar transferência.");
        } finally {
            setIsTransferring(false);
        }
    };

    return (
        <div className="waiter-view">
            <div className="waiter-shell">
                <header className="waiter-header">
                    <div className="waiter-header-top">
                        <div className="waiter-avatar" aria-hidden="true">
                            {initialsFrom(userName)}
                        </div>
                        <div className="waiter-greeting">
                            <span className="waiter-greeting-hello">Olá, {firstNameFrom(userName)} 👋</span>
                            <span className="waiter-online-chip">
                                <span className="waiter-online-dot" /> Garçom Online
                            </span>
                        </div>
                        <span className="waiter-spacer" />
                        <button
                            type="button"
                            className="waiter-icon-btn"
                            onClick={toggleTheme}
                        >
                            {theme === "dark" ? "☀" : "🌙"}
                        </button>
                        <button
                            type="button"
                            className="waiter-icon-btn waiter-bell"
                            onClick={() => {
                                setActiveTab("inicio");
                                setTimeout(() => document.getElementById("waiter-latest-orders")?.scrollIntoView({ behavior: "smooth" }), 100);
                            }}
                        >
                            🔔
                            {readyItemsCount > 0 && <span className="waiter-bell-badge">{readyItemsCount}</span>}
                        </button>
                    </div>
                </header>

                <main className="waiter-body">
                    {(tablesQuery.isError || ordersQuery.isError || comandasQuery.isError) && (
                        <div className="waiter-card">
                            {tablesQuery.isError && <QueryError error={tablesQuery.error} what="as mesas" />}
                            {ordersQuery.isError && <QueryError error={ordersQuery.error} what="os pedidos abertos" />}
                            {comandasQuery.isError && <QueryError error={comandasQuery.error} what="as comandas" />}
                        </div>
                    )}

                    {!assignmentQuery.isLoading && !activeAreaId && (
                        <div className="waiter-card" style={{ backgroundColor: "var(--w-warn)", padding: 12, borderRadius: 8, marginBottom: 16 }}>
                            <strong>Sem praça atribuída!</strong> Solicite ao gerente que inicie seu turno em uma praça.
                        </div>
                    )}

                    {/* ========================================================== */}
                    {/* ABA: INÍCIO                                                */}
                    {/* ========================================================== */}
                    {activeTab === "inicio" && (
                        <>
                            <div className="waiter-stats-row" style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: '10px', marginBottom: '16px' }}>
                                <div className="waiter-stat-card" onClick={() => setActiveTab("mesas")} style={{ cursor: "pointer", margin: 0 }}>
                                    <span className="waiter-stat-value mono-num">
                                        {myOpenTablesCount}
                                        <small>/{myTotalTables || "—"}</small>
                                    </span>
                                    <span className="waiter-stat-label">Mesas</span>
                                </div>
                                <div className="waiter-stat-card" onClick={() => setActiveTab("comandas")} style={{ cursor: "pointer", margin: 0 }}>
                                    <span className="waiter-stat-value mono-num">{comandaOrders.length}</span>
                                    <span className="waiter-stat-label">Comandas</span>
                                </div>
                                <div className="waiter-stat-card" onClick={() => setActiveTab("pedidos")} style={{ cursor: "pointer", margin: 0 }}>
                                    <span className="waiter-stat-value mono-num">{myOrders.length}</span>
                                    <span className="waiter-stat-label">Pedidos ativos</span>
                                </div>
                            </div>

                            <div style={{ display: 'grid', gap: '10px', marginBottom: '16px' }}>
                                <div className="waiter-highlight-card" style={{ margin: 0 }}>
                                    <span className="waiter-highlight-label">Mesas em aberto</span>
                                    <span className="waiter-highlight-value mono-num">{formatBRL(totalTablesAmount)}</span>
                                    <span className="waiter-highlight-sub">
                                        {tableOrders.length} pedido{tableOrders.length === 1 ? "" : "s"} em aberto agora
                                    </span>
                                </div>

                                <div className="waiter-highlight-card" style={{ margin: 0 }}>
                                    <span className="waiter-highlight-label">Comandas em aberto</span>
                                    <span className="waiter-highlight-value mono-num">{formatBRL(totalComandasAmount)}</span>
                                    <span className="waiter-highlight-sub">
                                        {comandaOrders.length} pedido{comandaOrders.length === 1 ? "" : "s"} em aberto agora
                                    </span>
                                </div>
                            </div>

                            <section id="waiter-latest-orders" className="waiter-section">
                                <div className="waiter-section-head">
                                    <h2 className="waiter-section-title">Últimos pedidos</h2>
                                    <button type="button" className="waiter-link-btn" onClick={() => setActiveTab("pedidos")}>
                                        Ver todos
                                    </button>
                                </div>

                                {latestOrders.length === 0 ? (
                                    <p className="waiter-empty">Nenhum pedido em aberto na sua praça.</p>
                                ) : (
                                    <div className="waiter-order-list">
                                        {latestOrders.map((order) => {
                                            const badge = deriveOrderBadge(order);
                                            return (
                                                <button
                                                    key={order.id}
                                                    type="button"
                                                    className="waiter-order-row"
                                                    onClick={() => setSelectedOrderId(order.id)}
                                                >
                                                    <span className="waiter-order-info">
                                                        <span className="waiter-order-title">{orderLabel(order, tablesById, comandasById)}</span>
                                                        <span className="waiter-order-meta">
                                                            {order.items.length} {order.items.length === 1 ? "item" : "itens"} · {elapsedLabel(order.openedAt)}
                                                        </span>
                                                    </span>
                                                    <span
                                                        className="waiter-order-badge"
                                                        style={{ "--w-badge": badgeToneVar[badge.tone] } as CSSProperties}
                                                    >
                                                        {badge.label}
                                                    </span>
                                                </button>
                                            );
                                        })}
                                    </div>
                                )}
                            </section>

                            <section className="waiter-section">
                                <h2 className="waiter-section-title">Ações rápidas</h2>
                                <div className="waiter-quick-grid">
                                    {quickActions
                                        .filter((action) => action.key !== "turno" || canSeeCaixa)
                                        .map((action, idx) => (
                                            <button
                                                key={`${action.key}-${idx}`}
                                                type="button"
                                                className="waiter-quick-tile"
                                                onClick={() => handleQuickAction(action.key)}
                                            >
                                                <span className="waiter-quick-icon" aria-hidden="true">{action.icon}</span>
                                                <span className="waiter-quick-label">{action.label}</span>
                                            </button>
                                        ))}
                                </div>
                            </section>
                        </>
                    )}

                    {/* ========================================================== */}
                    {/* ABA: MESAS                                                 */}
                    {/* ========================================================== */}
                    {activeTab === "mesas" && (
                        <section className="waiter-section">
                            <h2 className="waiter-section-title" style={{ marginBottom: 16 }}>Minhas Mesas</h2>

                            {!activeAreaId ? (
                                <p className="waiter-empty">Nenhuma praça vinculada a você no momento.</p>
                            ) : myTables.length === 0 ? (
                                <p className="waiter-empty">Nenhuma mesa foi configurada nesta praça.</p>
                            ) : (
                                <div
                                    className="waiter-tables-grid"
                                    style={{
                                        display: "grid",
                                        gridTemplateColumns: "repeat(auto-fill, minmax(160px, 1fr))",
                                        gap: "14px"
                                    }}
                                >
                                    {myTables.map((table) => {
                                        const order = ordersByTableId.get(table.id);

                                        let leftBorderColor = "#22c55e";
                                        let statusBg = "#dcfce7";
                                        let statusColor = "#15803d";
                                        let statusText = "LIVRE";
                                        let subText = `${table.capacity ?? 4} lugares`;

                                        if (table.tableStatusId === TableStatus.Ocupada) {
                                            leftBorderColor = "#f59e0b";
                                            statusBg = "#fef3c7";
                                            statusColor = "#b45309";
                                            statusText = "OCUPADA";

                                            if (order) {
                                                const totalItems = order.items.length;
                                                subText = `${totalItems} ${totalItems === 1 ? "item" : "itens"} · ${formatBRL(order.totalAmount)}`;
                                            }
                                        } else if (table.tableStatusId === TableStatus.EmFechamento) {
                                            leftBorderColor = "#3b82f6";
                                            statusBg = "#dbeafe";
                                            statusColor = "#1d4ed8";
                                            statusText = "FECHANDO";
                                        }

                                        return (
                                            <button
                                                key={table.id}
                                                onClick={() => handleTableClick(table.id, table.tableStatusId)}
                                                style={{
                                                    display: "flex",
                                                    flexDirection: "column",
                                                    alignItems: "stretch",
                                                    backgroundColor: "var(--surface, #ffffff)",
                                                    borderRadius: "10px",
                                                    border: "1px solid var(--border, #e5e7eb)",
                                                    borderLeft: `6px solid ${leftBorderColor}`,
                                                    padding: "12px 14px",
                                                    cursor: "pointer",
                                                    textAlign: "left",
                                                    boxShadow: "0 1px 3px rgba(0,0,0,0.05)",
                                                    transition: "transform 0.1s ease",
                                                }}
                                            >
                                                <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", width: "100%", marginBottom: "6px" }}>
                                                    <span style={{ fontSize: "1.6rem", fontWeight: "800", color: "var(--ink, #111827)", lineHeight: 1 }}>
                                                        {table.number}
                                                    </span>
                                                    <span
                                                        style={{
                                                            fontSize: "0.65rem",
                                                            fontWeight: "700",
                                                            backgroundColor: statusBg,
                                                            color: statusColor,
                                                            padding: "3px 8px",
                                                            borderRadius: "20px",
                                                            letterSpacing: "0.5px",
                                                            display: "flex",
                                                            alignItems: "center",
                                                            gap: "4px"
                                                        }}
                                                    >
                                                        <span style={{ width: "6px", height: "6px", borderRadius: "50%", backgroundColor: statusColor }} />
                                                        {statusText}
                                                    </span>
                                                </div>
                                                <span style={{ fontSize: "0.8rem", color: "var(--ink-dim, #6b7280)", fontWeight: 500 }}>
                                                    {subText}
                                                </span>
                                            </button>
                                        );
                                    })}
                                </div>
                            )}
                        </section>
                    )}

                    {/* ========================================================== */}
                    {/* ABA: COMANDAS                                              */}
                    {/* ========================================================== */}
                    {activeTab === "comandas" && (
                        <section className="waiter-section">
                            <h2 className="waiter-section-title" style={{ marginBottom: 16 }}>Comandas</h2>

                            {comandasQuery.isLoading ? (
                                <p className="waiter-empty">Carregando comandas...</p>
                            ) : !comandasQuery.data || comandasQuery.data.length === 0 ? (
                                <p className="waiter-empty">Nenhuma comanda registrada.</p>
                            ) : (
                                <div
                                    className="waiter-tables-grid"
                                    style={{
                                        display: "grid",
                                        gridTemplateColumns: "repeat(auto-fill, minmax(160px, 1fr))",
                                        gap: "14px"
                                    }}
                                >
                                    {comandasQuery.data.map((comanda) => {
                                        const order = comandaOrders.find((o) => o.comandaId === comanda.id);

                                        let leftBorderColor = "#22c55e"; // Cor Livre (Verde)
                                        let statusBg = "#dcfce7";
                                        let statusColor = "#15803d";
                                        let statusText = "LIVRE";
                                        let subText = "Toque para abrir";

                                        if (order) {
                                            leftBorderColor = "#f59e0b"; // Cor Ocupada (Laranja/Amarelo)
                                            statusBg = "#fef3c7";
                                            statusColor = "#b45309";
                                            statusText = "EM USO";

                                            const totalItems = order.items.length;
                                            subText = `${totalItems} ${totalItems === 1 ? "item" : "itens"} · ${formatBRL(order.totalAmount)}`;
                                        }

                                        return (
                                            <button
                                                key={comanda.id}
                                                onClick={() => {
                                                    if (order) {
                                                        setSelectedOrderId(order.id);
                                                    } else {
                                                        setComandaToOpen(comanda);
                                                    }
                                                }}
                                                style={{
                                                    display: "flex",
                                                    flexDirection: "column",
                                                    alignItems: "stretch",
                                                    backgroundColor: "var(--surface, #ffffff)",
                                                    borderRadius: "10px",
                                                    border: "1px solid var(--border, #e5e7eb)",
                                                    borderLeft: `6px solid ${leftBorderColor}`,
                                                    padding: "12px 14px",
                                                    cursor: "pointer",
                                                    textAlign: "left",
                                                    boxShadow: "0 1px 3px rgba(0,0,0,0.05)",
                                                    transition: "transform 0.1s ease",
                                                }}
                                            >
                                                <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", width: "100%", marginBottom: "6px" }}>
                                                    <span style={{ fontSize: "1.6rem", fontWeight: "800", color: "var(--ink, #111827)", lineHeight: 1 }}>
                                                        {comanda.code || comanda.id}
                                                    </span>
                                                    <span
                                                        style={{
                                                            fontSize: "0.65rem",
                                                            fontWeight: "700",
                                                            backgroundColor: statusBg,
                                                            color: statusColor,
                                                            padding: "3px 8px",
                                                            borderRadius: "20px",
                                                            letterSpacing: "0.5px",
                                                            display: "flex",
                                                            alignItems: "center",
                                                            gap: "4px"
                                                        }}
                                                    >
                                                        <span style={{ width: "6px", height: "6px", borderRadius: "50%", backgroundColor: statusColor }} />
                                                        {statusText}
                                                    </span>
                                                </div>
                                                <span style={{ fontSize: "0.8rem", color: "var(--ink-dim, #6b7280)", fontWeight: 500 }}>
                                                    {subText}
                                                </span>
                                            </button>
                                        );
                                    })}
                                </div>
                            )}
                        </section>
                    )}

                    {/* ========================================================== */}
                    {/* ABA: PEDIDOS                                               */}
                    {/* ========================================================== */}
                    {activeTab === "pedidos" && (
                        <section className="waiter-section">
                            <h2 className="waiter-section-title" style={{ marginBottom: 16 }}>Histórico de Pedidos</h2>

                            {myOrders.length === 0 ? (
                                <p className="waiter-empty">Nenhum pedido registrado na sua praça no momento.</p>
                            ) : (
                                <div className="waiter-order-list">
                                    {myOrders.map((order) => {
                                        const badge = deriveOrderBadge(order);
                                        return (
                                            <button
                                                key={order.id}
                                                type="button"
                                                className="waiter-order-row"
                                                onClick={() => setSelectedOrderId(order.id)}
                                                style={{
                                                    backgroundColor: "var(--surface, #ffffff)",
                                                    borderRadius: "10px",
                                                    padding: "14px",
                                                    marginBottom: "10px",
                                                    border: "1px solid var(--border, #e5e7eb)",
                                                    width: "100%",
                                                    textAlign: "left",
                                                    cursor: "pointer",
                                                    display: "flex",
                                                    justifyContent: "space-between",
                                                    alignItems: "center"
                                                }}
                                            >
                                                <span className="waiter-order-info" style={{ display: "grid", gap: "4px" }}>
                                                    <span className="waiter-order-title" style={{ fontWeight: "700", fontSize: "1.05rem", color: "var(--ink)" }}>
                                                        {orderLabel(order, tablesById, comandasById)}
                                                    </span>
                                                    <span className="waiter-order-meta" style={{ fontSize: "0.85rem", color: "var(--ink-dim)" }}>
                                                        {order.items.length} {order.items.length === 1 ? "item" : "itens"} · Total: <strong>{formatBRL(order.totalAmount)}</strong>
                                                    </span>
                                                    <span style={{ fontSize: "0.75rem", color: "var(--ink-faint)" }}>
                                                        Aberto {elapsedLabel(order.openedAt)}
                                                    </span>
                                                </span>

                                                <span
                                                    className="waiter-order-badge"
                                                    style={{
                                                        "--w-badge": badgeToneVar[badge.tone],
                                                        backgroundColor: badge.tone === "ready" ? "#dcfce7" : badge.tone === "preparing" ? "#dbeafe" : "#fef3c7",
                                                        color: badge.tone === "ready" ? "#15803d" : badge.tone === "preparing" ? "#1d4ed8" : "#b45309",
                                                        padding: "4px 10px",
                                                        borderRadius: "20px",
                                                        fontSize: "0.75rem",
                                                        fontWeight: "700"
                                                    } as CSSProperties}
                                                >
                                                    {badge.label}
                                                </span>
                                            </button>
                                        );
                                    })}
                                </div>
                            )}
                        </section>
                    )}

                    {/* ========================================================== */}
                    {/* ABA: MENSAGENS (EXCLUSIVAS DA PRAÇA ATIVA DO GARÇOM)       */}
                    {/* ========================================================== */}
                    {activeTab === "mensagens" && (
                        <section className="waiter-section">
                            <h2 className="waiter-section-title" style={{ marginBottom: 16 }}>Mensagens e Avisos</h2>

                            {!activeAreaId ? (
                                <p className="waiter-empty">Atribua-se a uma praça para visualizar as mensagens locais.</p>
                            ) : messagesQuery.isLoading ? (
                                <p className="waiter-empty">Carregando mensagens...</p>
                            ) : messagesQuery.isError ? (
                                <QueryError error={messagesQuery.error} what="as mensagens" />
                            ) : sortedMessages.length === 0 ? (
                                <p className="waiter-empty">Nenhuma mensagem registrada na sua praça no momento.</p>
                            ) : (
                                <div className="waiter-order-list" style={{ display: "grid", gap: "10px" }}>
                                    {sortedMessages.map((msg) => (
                                        <div
                                            key={msg.id}
                                            style={{
                                                backgroundColor: "var(--surface, #ffffff)",
                                                borderRadius: "10px",
                                                padding: "14px",
                                                border: "1px solid var(--border, #e5e7eb)",
                                                borderLeft: `6px solid ${msg.isRead ? "#9ca3af" : "#3b82f6"}`,
                                                display: "grid",
                                                gap: "6px"
                                            }}
                                        >
                                            <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
                                                <span style={{ fontSize: "0.75rem", fontWeight: "700", color: "var(--ink-dim)" }}>
                                                    Aviso Operacional
                                                </span>
                                                <span style={{ fontSize: "0.75rem", color: "var(--ink-faint)" }}>
                                                    {new Date(msg.createdAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })} - {new Date(msg.createdAt).toLocaleDateString()}
                                                </span>
                                            </div>
                                            <p style={{ fontSize: "0.95rem", color: "var(--ink)", margin: 0, fontWeight: 500 }}>
                                                {msg.message}
                                            </p>
                                        </div>
                                    ))}
                                    <div ref={messagesEndRef} />
                                </div>
                            )}
                        </section>
                    )}
                </main>

                <nav className="waiter-tabbar" aria-label="Navegação do Modo Garçom">
                    {tabs.map((tab) => (
                        <button
                            key={tab.key}
                            type="button"
                            className={`waiter-tab${tab.key === activeTab ? " is-active" : ""}`}
                            onClick={() => handleTab(tab.key)}
                        >
                            <span aria-hidden="true">{tab.icon}</span>
                            <span>{tab.label}</span>
                        </button>
                    ))}
                </nav>

                {/* MODAL CALCULADORA: Movido para dentro do waiter-shell */}
                {calculatorOpen && (
                    <div className="modal-backdrop is-center" onClick={() => setCalculatorOpen(false)} style={{ position: "absolute" }}>
                        <div className="modal-panel is-center" onClick={(e) => e.stopPropagation()} style={{ width: "90%", maxWidth: "320px", padding: "20px" }}>
                            <div className="modal-head" style={{ marginBottom: "16px", display: "flex", justifyContent: "space-between", alignItems: "center" }}>
                                <span className="display" style={{ fontSize: "1.2rem", fontWeight: "bold" }}>➗ Calculadora</span>
                                <button type="button" className="btn-ghost btn-icon" onClick={() => setCalculatorOpen(false)}>✕</button>
                            </div>

                            <div style={{
                                backgroundColor: "var(--bg-body)",
                                border: "1px solid var(--border)",
                                borderRadius: "8px",
                                padding: "16px",
                                fontSize: "2rem",
                                textAlign: "right",
                                marginBottom: "16px",
                                overflow: "hidden",
                                color: "var(--ink)",
                                fontWeight: "bold"
                            }}>
                                {calcInput}
                            </div>

                            <div style={{ display: "grid", gridTemplateColumns: "repeat(4, 1fr)", gap: "8px" }}>
                                <button type="button" className="btn-ghost" style={{ gridColumn: "span 3", backgroundColor: "#fee2e2", color: "#b91c1c", fontWeight: "bold", fontSize: "1.2rem" }} onClick={handleCalcClear}>C</button>
                                <button type="button" className="btn-ghost" style={{ backgroundColor: "#e0e7ff", color: "#4338ca", fontWeight: "bold", fontSize: "1.2rem" }} onClick={() => handleCalcOp("/")}>÷</button>

                                <button type="button" className="btn-ghost" style={{ backgroundColor: "var(--bg-raise)", fontSize: "1.2rem" }} onClick={() => handleCalcNum("7")}>7</button>
                                <button type="button" className="btn-ghost" style={{ backgroundColor: "var(--bg-raise)", fontSize: "1.2rem" }} onClick={() => handleCalcNum("8")}>8</button>
                                <button type="button" className="btn-ghost" style={{ backgroundColor: "var(--bg-raise)", fontSize: "1.2rem" }} onClick={() => handleCalcNum("9")}>9</button>
                                <button type="button" className="btn-ghost" style={{ backgroundColor: "#e0e7ff", color: "#4338ca", fontWeight: "bold", fontSize: "1.2rem" }} onClick={() => handleCalcOp("*")}>×</button>

                                <button type="button" className="btn-ghost" style={{ backgroundColor: "var(--bg-raise)", fontSize: "1.2rem" }} onClick={() => handleCalcNum("4")}>4</button>
                                <button type="button" className="btn-ghost" style={{ backgroundColor: "var(--bg-raise)", fontSize: "1.2rem" }} onClick={() => handleCalcNum("5")}>5</button>
                                <button type="button" className="btn-ghost" style={{ backgroundColor: "var(--bg-raise)", fontSize: "1.2rem" }} onClick={() => handleCalcNum("6")}>6</button>
                                <button type="button" className="btn-ghost" style={{ backgroundColor: "#e0e7ff", color: "#4338ca", fontWeight: "bold", fontSize: "1.5rem" }} onClick={() => handleCalcOp("-")}>-</button>

                                <button type="button" className="btn-ghost" style={{ backgroundColor: "var(--bg-raise)", fontSize: "1.2rem" }} onClick={() => handleCalcNum("1")}>1</button>
                                <button type="button" className="btn-ghost" style={{ backgroundColor: "var(--bg-raise)", fontSize: "1.2rem" }} onClick={() => handleCalcNum("2")}>2</button>
                                <button type="button" className="btn-ghost" style={{ backgroundColor: "var(--bg-raise)", fontSize: "1.2rem" }} onClick={() => handleCalcNum("3")}>3</button>
                                <button type="button" className="btn-ghost" style={{ backgroundColor: "#e0e7ff", color: "#4338ca", fontWeight: "bold", fontSize: "1.2rem" }} onClick={() => handleCalcOp("+")}>+</button>

                                <button type="button" className="btn-ghost" style={{ gridColumn: "span 2", backgroundColor: "var(--bg-raise)", fontSize: "1.2rem" }} onClick={() => handleCalcNum("0")}>0</button>
                                <button type="button" className="btn-ghost" style={{ backgroundColor: "var(--bg-raise)", fontWeight: "bold", fontSize: "1.2rem" }} onClick={() => handleCalcNum(".")}>.</button>
                                <button type="button" className="waiter-cta" style={{ margin: 0, fontSize: "1.2rem" }} onClick={handleCalcEqual}>=</button>
                            </div>
                        </div>
                    </div>
                )}

                {/* MODAL DE TRANSFERÊNCIA: Movido para dentro do waiter-shell */}
                {transferOpen && (
                    <div className="modal-backdrop is-center" onClick={() => setTransferOpen(false)} style={{ position: "absolute" }}>
                        <div className="modal-panel is-center" onClick={(e) => e.stopPropagation()} style={{ width: "90%", maxWidth: "420px" }}>
                            <div className="modal-head" style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: "16px" }}>
                                <span className="display" style={{ fontSize: "1.2rem", fontWeight: "bold" }}>🔀 Transferir Item</span>
                                <button type="button" className="btn-ghost btn-icon" aria-label="Fechar" onClick={() => setTransferOpen(false)}>
                                    ✕
                                </button>
                            </div>

                            <form onSubmit={handleExecuteTransfer} style={{ display: "grid", gap: "14px" }}>
                                <div style={{ display: "grid", gap: "6px" }}>
                                    <label style={{ fontSize: "0.85rem", fontWeight: "600", color: "var(--ink-dim)" }}>Mesa de Origem (Com pedido)</label>
                                    <select
                                        value={sourceTableId}
                                        onChange={(e) => {
                                            setSourceTableId(e.target.value);
                                            setSelectedItemId("");
                                        }}
                                        style={{ padding: "10px", borderRadius: "8px", border: "1px solid var(--border)", backgroundColor: "var(--surface)", color: "var(--ink)" }}
                                        required
                                    >
                                        <option value="">Selecione a mesa de origem...</option>
                                        {myTables.filter(t => ordersByTableId.has(t.id)).map(t => (
                                            <option key={t.id} value={t.id}>Mesa {t.number}</option>
                                        ))}
                                    </select>
                                </div>

                                <div style={{ display: "grid", gap: "6px" }}>
                                    <label style={{ fontSize: "0.85rem", fontWeight: "600", color: "var(--ink-dim)" }}>Item a ser transferido</label>
                                    <select
                                        value={selectedItemId}
                                        onChange={(e) => setSelectedItemId(e.target.value)}
                                        style={{ padding: "10px", borderRadius: "8px", border: "1px solid var(--border)", backgroundColor: "var(--surface)", color: "var(--ink)" }}
                                        required
                                        disabled={!sourceTableId || !sourceOrder}
                                    >
                                        <option value="">Selecione o item...</option>
                                        {sourceOrder?.items
                                            .filter((item) => item.orderItemStatusId !== 6)
                                            .map((item) => {
                                                let statusLabel = item.orderItemStatusId.toString();
                                                if (item.orderItemStatusId === 1) statusLabel = "Lançado";
                                                if (item.orderItemStatusId === 2) statusLabel = "Enviado Cozinha";
                                                if (item.orderItemStatusId === 3) statusLabel = "Em Preparo";
                                                if (item.orderItemStatusId === 4) statusLabel = "Pronto";
                                                if (item.orderItemStatusId === 5) statusLabel = "Entregue";

                                                const productName = (item as any).productName || (item as any).name || `Produto #${item.productId}`;

                                                return (
                                                    <option key={item.id} value={item.id}>
                                                        {productName} - Qtd: {item.quantity} (Status: {statusLabel})
                                                    </option>
                                                );
                                            })}
                                    </select>
                                </div>

                                <div style={{ display: "grid", gap: "6px" }}>
                                    <label style={{ fontSize: "0.85rem", fontWeight: "600", color: "var(--ink-dim)" }}>Mesa de Destino</label>
                                    <select
                                        value={targetTableId}
                                        onChange={(e) => setTargetTableId(e.target.value)}
                                        style={{ padding: "10px", borderRadius: "8px", border: "1px solid var(--border)", backgroundColor: "var(--surface)", color: "var(--ink)" }}
                                        required
                                    >
                                        <option value="">Selecione a mesa de destino...</option>
                                        {myTables.filter(t => t.id.toString() !== sourceTableId).map(t => (
                                            <option key={t.id} value={t.id}>Mesa {t.number} ({t.tableStatusId === TableStatus.Livre ? "Livre" : "Ocupada"})</option>
                                        ))}
                                    </select>
                                </div>

                                <div style={{ display: "flex", gap: "10px", marginTop: "10px" }}>
                                    <button
                                        type="button"
                                        className="btn-ghost"
                                        style={{ flex: 1, padding: "10px", borderRadius: "8px" }}
                                        onClick={() => setTransferOpen(false)}
                                    >
                                        Cancelar
                                    </button>
                                    <button
                                        type="submit"
                                        className="waiter-cta"
                                        style={{ flex: 1, margin: 0, padding: "10px" }}
                                        disabled={isTransferring}
                                    >
                                        {isTransferring ? "Transferindo..." : "Confirmar"}
                                    </button>
                                </div>
                            </form>
                        </div>
                    </div>
                )}

                {/* MODAL DE ABRIR COMANDA: Movido para dentro do waiter-shell */}
                {comandaToOpen && (
                    <WaiterOpenComandaModal
                        comanda={comandaToOpen}
                        onClose={() => setComandaToOpen(null)}
                        onOpened={(orderId) => {
                            setComandaToOpen(null);
                            refresh();
                            setSelectedOrderId(orderId);
                        }}
                    />
                )}

                {/* MODAL DE ABRIR MESA: Movido para dentro do waiter-shell */}
                {tableToOpen && (
                    <WaiterOpenTableModal
                        table={tableToOpen}
                        onClose={() => setTableToOpen(null)}
                        onOpened={(orderId) => {
                            setTableToOpen(null);
                            refresh();
                            setSelectedOrderId(orderId);
                        }}
                    />
                )}

                {/* ORDER DRAWER: Movido para dentro do waiter-shell com isWaiterMode={true} */}
                {selectedOrderId !== null && (
                    <OrderDrawer
                        orderId={selectedOrderId}
                        isWaiterMode={true}
                        onClose={() => {
                            setSelectedOrderId(null);
                            refresh();
                        }}
                    />
                )}

            </div> {/* <---- AQUI FECHA A WAITER-SHELL (CELULAR) */}

            {/* ITENS GLOBAIS FORA DA WAITER-SHELL (Ficam na tela cheia real do PC) */}
            {toast && (
                <div className="waiter-toast" role="status">
                    {toast}
                </div>
            )}

            {cashOpen && <CashDrawer onClose={() => setCashOpen(false)} />}

            {profileOpen && (
                <div className="modal-backdrop is-center" onClick={() => setProfileOpen(false)}>
                    <div className="modal-panel is-center" onClick={(e) => e.stopPropagation()}>
                        <div className="modal-head">
                            <span className="display" style={{ fontSize: "1.3rem" }}>Perfil</span>
                            <button type="button" className="btn-ghost btn-icon" aria-label="Fechar" onClick={() => setProfileOpen(false)}>
                                ✕
                            </button>
                        </div>
                        <div style={{ display: "grid", gap: 4 }}>
                            <span style={{ color: "var(--ink-dim)", fontSize: "0.85rem" }}>Usuário</span>
                            <span>{userName ?? "—"}</span>
                        </div>
                        <div style={{ display: "grid", gap: 4 }}>
                            <span style={{ color: "var(--ink-dim)", fontSize: "0.85rem" }}>Filial</span>
                            <span>Filial {branchId}</span>
                        </div>
                        <button
                            type="button"
                            className="btn-danger btn-block"
                            onClick={() => {
                                queryClient.clear();
                                clear();
                                navigate("/login", { replace: true });
                            }}
                        >
                            Sair
                        </button>
                    </div>
                </div>
            )}
        </div>
    );
}