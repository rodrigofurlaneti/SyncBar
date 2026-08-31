import type { CSSProperties } from "react";
import { formatBRL, OrderResponse, TableResponse, ComandaResponse } from "../../../../lib/types";
import { QuickActionKey, quickActions, TabKey, deriveOrderBadge, badgeToneVar, orderLabel, elapsedLabel } from "../../utils";

interface TabInicioProps {
    myOpenTablesCount: number;
    myTotalTables: number;
    comandaOrders: OrderResponse[];
    myOrders: OrderResponse[];
    totalTablesAmount: number;
    totalComandasAmount: number;
    latestOrders: OrderResponse[];
    tablesById: Map<number, TableResponse>;
    comandasById: Map<number, ComandaResponse>;
    canSeeCaixa: boolean;
    onTabChange: (key: TabKey) => void;
    onQuickAction: (key: QuickActionKey) => void;
    onOrderClick: (orderId: number) => void;
}

export function TabInicio({ myOpenTablesCount, myTotalTables, comandaOrders, myOrders, totalTablesAmount, totalComandasAmount, latestOrders, tablesById, comandasById, canSeeCaixa, onTabChange, onQuickAction, onOrderClick }: TabInicioProps) {
    return (
        <>
            <div className="waiter-stats-row" style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: '10px', marginBottom: '16px' }}>
                <div
                    className="waiter-stat-card"
                    onClick={() => onTabChange("mesas")}
                    onKeyDown={(e) => {
                        if (e.key === "Enter" || e.key === " ") {
                            e.preventDefault();
                            onTabChange("mesas");
                        }
                    }}
                    role="button"
                    tabIndex={0}
                    style={{ cursor: "pointer", margin: 0 }}
                >
                    <span className="waiter-stat-value mono-num">
                        {myOpenTablesCount} <small>/{myTotalTables || "—"}</small>
                    </span>
                    <span className="waiter-stat-label">Mesas</span>
                </div>
                <div
                    className="waiter-stat-card"
                    onClick={() => onTabChange("comandas")}
                    onKeyDown={(e) => {
                        if (e.key === "Enter" || e.key === " ") {
                            e.preventDefault();
                            onTabChange("comandas");
                        }
                    }}
                    role="button"
                    tabIndex={0}
                    style={{ cursor: "pointer", margin: 0 }}
                >
                    <span className="waiter-stat-value mono-num">{comandaOrders.length}</span>
                    <span className="waiter-stat-label">Comandas</span>
                </div>
                <div
                    className="waiter-stat-card"
                    onClick={() => onTabChange("pedidos")}
                    onKeyDown={(e) => {
                        if (e.key === "Enter" || e.key === " ") {
                            e.preventDefault();
                            onTabChange("pedidos");
                        }
                    }}
                    role="button"
                    tabIndex={0}
                    style={{ cursor: "pointer", margin: 0 }}
                >
                    <span className="waiter-stat-value mono-num">{myOrders.length}</span>
                    <span className="waiter-stat-label">Pedidos ativos</span>
                </div>
            </div>

            <div style={{ display: 'grid', gap: '10px', marginBottom: '16px' }}>
                <div className="waiter-highlight-card" style={{ margin: 0 }}>
                    <span className="waiter-highlight-label">Mesas em aberto</span>
                    <span className="waiter-highlight-value mono-num">{formatBRL(totalTablesAmount)}</span>
                </div>
                <div className="waiter-highlight-card" style={{ margin: 0 }}>
                    <span className="waiter-highlight-label">Comandas em aberto</span>
                    <span className="waiter-highlight-value mono-num">{formatBRL(totalComandasAmount)}</span>
                </div>
            </div>

            <section id="waiter-latest-orders" className="waiter-section">
                <div className="waiter-section-head">
                    <h2 className="waiter-section-title">Últimos pedidos</h2>
                    <button type="button" className="waiter-link-btn" onClick={() => onTabChange("pedidos")}>Ver todos</button>
                </div>
                {latestOrders.length === 0 ? (
                    <p className="waiter-empty">Nenhum pedido em aberto na sua praça.</p>
                ) : (
                    <div className="waiter-order-list">
                        {latestOrders.map((order) => {
                            const badge = deriveOrderBadge(order);
                            return (
                                <button key={order.id} type="button" className="waiter-order-row" onClick={() => onOrderClick(order.id)}>
                                    <span className="waiter-order-info">
                                        <span className="waiter-order-title">{orderLabel(order, tablesById, comandasById)}</span>
                                        <span className="waiter-order-meta">{order.items.length} itens · {elapsedLabel(order.openedAt)}</span>
                                    </span>
                                    <span className="waiter-order-badge" style={{ "--w-badge": badgeToneVar[badge.tone] } as CSSProperties}>{badge.label}</span>
                                </button>
                            );
                        })}
                    </div>
                )}
            </section>

            <section className="waiter-section">
                <h2 className="waiter-section-title">Ações rápidas</h2>
                <div className="waiter-quick-grid">
                    {quickActions.filter((action) => action.key !== "turno" || canSeeCaixa).map((action, idx) => (
                        <button key={`${action.key}-${idx}`} type="button" className="waiter-quick-tile" onClick={() => onQuickAction(action.key)}>
                            <span className="waiter-quick-icon" aria-hidden="true">{action.icon}</span>
                            <span className="waiter-quick-label">{action.label}</span>
                        </button>
                    ))}
                </div>
            </section>
        </>
    );
}