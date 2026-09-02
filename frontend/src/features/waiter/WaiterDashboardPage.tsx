import { useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { api } from "../../lib/apiClient";
import { getTablesByBranch } from "../tables/api";
import { getOpenOrdersByBranch } from "../orders/api";
import { getComandasByBranch } from "../comandas/api";
import { getActiveAssignmentsByEmployee, getTablesByArea } from "../diningareas/api";
import { useAuthStore } from "../../stores/authStore";
import { useMyFeatures } from "../access/hooks";
import { OrderDrawer } from "../orders/OrderDrawer";
import { CashDrawer } from "../cash/CashDrawer";
import { QueryError } from "../../components/QueryError";
import { TableStatus, OrderItemStatus, parseApiDate } from "../../lib/types";
import type { ComandaResponse, OrderResponse, TableResponse } from "../../lib/types";
import { TabKey, QuickActionKey } from "./utils";
import { WaiterHeader } from "./components/WaiterHeader";
import { WaiterTabBar } from "./components/WaiterTabBar";
import { TabInicio } from "./components/tabs/TabInicio";
import { TabMesas } from "./components/tabs/TabMesas";
import { TabComandas } from "./components/tabs/TabComandas";
import { TabPedidos } from "./components/tabs/TabPedidos";
import { TabMensagens, WaiterMessageResponse } from "./components/tabs/TabMensagens";
import { CalculatorModal } from "./components/modals/CalculatorModal";
import { TransferItemModal } from "./components/modals/TransferItemModal";
import { WaiterProfileModal } from "./components/modals/WaiterProfileModal";
import { WaiterOpenTableModal } from "./components/modals/WaiterOpenTableModal";
import { WaiterOpenComandaModal } from "./components/modals/WaiterOpenComandaModal";

const getWaiterMessagesByBranch = (branchId: number, diningAreaId: number | null): Promise<WaiterMessageResponse[]> => {
    if (!diningAreaId) return Promise.resolve([]);
    return api<WaiterMessageResponse[]>(`/api/diningareas/messages/branch/${branchId}?diningAreaId=${diningAreaId}`);
};

export function WaiterDashboardPage() {
    const navigate = useNavigate();
    const queryClient = useQueryClient();
    const { branchId, userName, clear, employeeId } = useAuthStore();
    const featuresQuery = useMyFeatures();

    const canSeeCaixa = !!(featuresQuery.data?.canManageAccess || featuresQuery.data?.features?.includes("Caixa"));

    const [activeTab, setActiveTab] = useState<TabKey>("inicio");
    const [selectedOrderId, setSelectedOrderId] = useState<number | null>(null);
    const [tableToOpen, setTableToOpen] = useState<TableResponse | null>(null);
    const [comandaToOpen, setComandaToOpen] = useState<ComandaResponse | null>(null);

    // Unificado para um único estado de modal de transferência
    const [openModal, setOpenModal] = useState<"calculator" | "transfer" | "profile" | "cash" | null>(null);

    const [toast, setToast] = useState<string | null>(null);
    const showToast = (message: string) => {
        setToast(message);
        window.setTimeout(() => setToast((current) => (current === message ? null : current)), 2500);
    };
    const handleLogout = () => {
        queryClient.clear();
        clear();
        navigate("/login", { replace: true });
    };
    const assignmentQuery = useQuery({ queryKey: ["diningareaassignments", "active", employeeId], queryFn: () => getActiveAssignmentsByEmployee(employeeId ?? 0), enabled: !!employeeId });
    const activeAreaId = assignmentQuery.data?.[0]?.diningAreaId ?? null;
    const areaTablesQuery = useQuery({ queryKey: ["diningareatables", activeAreaId], queryFn: () => getTablesByArea(activeAreaId!), enabled: !!activeAreaId });
    const allowedTableIds = useMemo(() => {
        const set = new Set<number>();
        for (const t of areaTablesQuery.data ?? []) set.add(t.diningTableId);
        return set;
    }, [areaTablesQuery.data]);
    const tablesQuery = useQuery({ queryKey: ["tables", branchId], queryFn: () => getTablesByBranch(branchId), refetchInterval: 15_000 });
    const comandasQuery = useQuery({ queryKey: ["comandas", branchId], queryFn: () => getComandasByBranch(branchId), refetchInterval: 15_000 });
    const ordersQuery = useQuery({ queryKey: ["orders", "open", branchId], queryFn: () => getOpenOrdersByBranch(branchId), refetchInterval: 15_000 });
    const messagesQuery = useQuery({
        queryKey: ["waitermessages", branchId, activeAreaId],
        queryFn: () => getWaiterMessagesByBranch(branchId, activeAreaId),
        enabled: !!branchId && !!activeAreaId,
        refetchInterval: 10_000,
    });
    const sortedMessages = useMemo(() => {
        const msgs = messagesQuery.data ?? [];
        return [...msgs].sort((a, b) => parseApiDate(a.createdAt).getTime() - parseApiDate(b.createdAt).getTime());
    }, [messagesQuery.data]);

    const refresh = () => {
        void queryClient.invalidateQueries({ queryKey: ["tables"] });
        void queryClient.invalidateQueries({ queryKey: ["orders"] });
        void queryClient.invalidateQueries({ queryKey: ["comandas"] });
        void queryClient.invalidateQueries({ queryKey: ["waitermessages"] });
    };
    const allActiveOrders = ordersQuery.data ?? [];
    const myOrders = useMemo(() => {
        if (!activeAreaId) return [];
        return allActiveOrders.filter(order => (order.comandaId !== null) || (order.diningTableId !== null && allowedTableIds.has(order.diningTableId)));
    }, [allActiveOrders, activeAreaId, allowedTableIds]);
    const myTables = useMemo(() => (tablesQuery.data ?? []).filter((t) => allowedTableIds.has(t.id)), [tablesQuery.data, allowedTableIds]);
    const ordersByTableId = useMemo(() => {
        const map = new Map<number, OrderResponse>();
        for (const order of allActiveOrders) if (order.diningTableId !== null) map.set(order.diningTableId, order);
        return map;
    }, [allActiveOrders]);
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
    const myOpenTablesCount = myTables.filter((t) => t.tableStatusId === TableStatus.Ocupada || t.tableStatusId === TableStatus.EmFechamento).length;
    const tableOrders = useMemo(() => myOrders.filter((o) => o.diningTableId !== null), [myOrders]);
    const comandaOrders = useMemo(() => myOrders.filter((o) => o.comandaId !== null && o.diningTableId === null), [myOrders]);
    const totalTablesAmount = useMemo(() => tableOrders.reduce((sum, order) => sum + order.totalAmount, 0), [tableOrders]);
    const totalComandasAmount = useMemo(() => comandaOrders.reduce((sum, order) => sum + order.totalAmount, 0), [comandaOrders]);
    const readyItemsCount = useMemo(() => myOrders.reduce((sum, order) => sum + order.items.filter((i) => i.orderItemStatusId === OrderItemStatus.Pronto).length, 0), [myOrders]);
    const latestOrders = useMemo(() => [...myOrders].sort((a, b) => new Date(b.openedAt).getTime() - new Date(a.openedAt).getTime()).slice(0, 3), [myOrders]);

    const handleQuickAction = (key: QuickActionKey) => {
        if (key === "mesas" || key === "comandas") setActiveTab(key);
        else if (key === "turno") setOpenModal("cash");
        else if (key === "calculadora") setOpenModal("calculator");
        else if (key === "transferir") setOpenModal("transfer");
    };

    return (
        <div className="waiter-view">
            <div className="waiter-shell">
                <WaiterHeader userName={userName} readyItemsCount={readyItemsCount} onBellClick={() => setActiveTab("mensagens")} />
                <main className="waiter-body">
                    {(tablesQuery.isError || ordersQuery.isError || comandasQuery.isError) && (
                        <div className="waiter-card">
                            <QueryError error={tablesQuery.error || ordersQuery.error || comandasQuery.error} what="os dados" />
                        </div>
                    )}
                    {!assignmentQuery.isLoading && !activeAreaId && (
                        <div className="waiter-card" style={{ backgroundColor: "var(--w-warn)", padding: 12, borderRadius: 8, marginBottom: 16 }}>
                            <strong>Sem praça atribuída!</strong> Solicite ao gerente que inicie seu turno em uma praça.
                        </div>
                    )}
                    {activeTab === "inicio" && (
                        <TabInicio
                            myOpenTablesCount={myOpenTablesCount} myTotalTables={myTables.length} comandaOrders={comandaOrders}
                            myOrders={myOrders} totalTablesAmount={totalTablesAmount} totalComandasAmount={totalComandasAmount}
                            latestOrders={latestOrders} tablesById={tablesById} comandasById={comandasById} canSeeCaixa={canSeeCaixa}
                            onTabChange={setActiveTab} onQuickAction={handleQuickAction} onOrderClick={setSelectedOrderId}
                        />
                    )}
                    {activeTab === "mesas" && (
                        <TabMesas
                            activeAreaId={activeAreaId}
                            myTables={myTables}
                            ordersByTableId={ordersByTableId}
                            onTableClick={(tableId, status) => status === TableStatus.Livre ? setTableToOpen(myTables.find(t => t.id === tableId)!) : setSelectedOrderId(myOrders.find(o => o.diningTableId === tableId)?.id || null)}
                        />
                    )}
                    {activeTab === "comandas" && (
                        <TabComandas
                            isLoading={comandasQuery.isLoading}
                            comandas={comandasQuery.data}
                            comandaOrders={comandaOrders}
                            onComandaClick={(comanda, orderId) => orderId ? setSelectedOrderId(orderId) : setComandaToOpen(comanda)}
                        />
                    )}
                    {activeTab === "pedidos" && (
                        <TabPedidos
                            myOrders={myOrders}
                            tablesById={tablesById}
                            comandasById={comandasById}
                            onOrderClick={setSelectedOrderId}
                        />
                    )}
                    {activeTab === "mensagens" && (
                        <TabMensagens
                            activeAreaId={activeAreaId}
                            isLoading={messagesQuery.isLoading}
                            isError={messagesQuery.isError}
                            error={messagesQuery.error}
                            messages={sortedMessages}
                        />
                    )}
                </main>
                <WaiterTabBar
                    activeTab={activeTab}
                    onTabChange={(key) => key === "perfil" ? setOpenModal("profile") : setActiveTab(key)}
                />

                {/* Modais Globais */}
                {openModal === "calculator" && <CalculatorModal onClose={() => setOpenModal(null)} />}
                {openModal === "profile" && <WaiterProfileModal userName={userName} branchId={branchId} onClose={() => setOpenModal(null)} onLogout={handleLogout} />}
                {openModal === "cash" && <CashDrawer onClose={() => setOpenModal(null)} />}

                {/* Modal Unificado de Transferência (Mesas / Comandas) */}
                {openModal === "transfer" && (
                    <TransferItemModal
                        mode="table"
                        myTables={myTables}
                        comandas={comandasQuery.data ?? []}
                        comandaOrders={comandaOrders}
                        allActiveOrders={allActiveOrders}
                        employeeId={employeeId}
                        onClose={() => setOpenModal(null)}
                        onSuccess={showToast}
                        onError={showToast}
                    />
                )}

                {tableToOpen && <WaiterOpenTableModal table={tableToOpen} onClose={() => setTableToOpen(null)} onOpened={(id) => { setTableToOpen(null); refresh(); setSelectedOrderId(id); }} />}
                {comandaToOpen && <WaiterOpenComandaModal comanda={comandaToOpen} onClose={() => setComandaToOpen(null)} onOpened={(id) => { setComandaToOpen(null); refresh(); setSelectedOrderId(id); }} />}

                {selectedOrderId !== null && <OrderDrawer orderId={selectedOrderId} onClose={() => { setSelectedOrderId(null); refresh(); }} />}
            </div>

            {toast && <div className="waiter-toast" role="status">{toast}</div>}
        </div>
    );
}