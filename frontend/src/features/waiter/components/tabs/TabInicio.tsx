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
        <div data-testid="tab-inicio-container">
            <div className="waiter-stats-row" data-testid="waiter-stats-row" style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: '10px', marginBottom: '16px' }}>
                <button
                    type="button"
                    className="waiter-stat-card"
                    onClick={() => onTabChange("mesas")}
                    data-testid="stat-card-tables"
                    style={{ cursor: "pointer", margin: 0 }}
                >
                    <span className="waiter-stat-value mono-num" data-testid="stat-tables-value">
                        {myOpenTablesCount} <small>/{myTotalTables || "—"}</small>
                    </span>
                    <span className="waiter-stat-label">Mesas</span>
                </button>
                <button
                    type="button"
                    className="waiter-stat-card"
                    onClick={() => onTabChange("comandas")}
                    data-testid="stat-card-comandas"
                    style={{ cursor: "pointer", margin: 0 }}
                >
                    <span className="waiter-stat-value mono-num" data-testid="stat-comandas-value">{comandaOrders.length}</span>
                    <span className="waiter-stat-label">Comandas</span>
                </button>
                <button
                    type="button"
                    className="waiter-stat-card"
                    onClick={() => onTabChange("pedidos")}
                    data-testid="stat-card-orders"
                    style={{ cursor: "pointer", margin: 0 }}
                >
                    <span className="waiter-stat-value mono-num" data-testid="stat-orders-value">{myOrders.length}</span>
                    <span className="waiter-stat-label">Pedidos ativos</span>
                </button>
            </div>

            <div style={{ display: 'grid', gap: '10px', marginBottom: '16px' }} data-testid="waiter-highlights-row">
                <div className="waiter-highlight-card" data-testid="highlight-tables-amount" style={{ margin: 0 }}>
                    <span className="waiter-highlight-label">Mesas em aberto</span>
                    <span className="waiter-highlight-value mono-num">{formatBRL(totalTablesAmount)}</span>
                </div>
                <div className="waiter-highlight-card" data-testid="highlight-comandas-amount" style={{ margin: 0 }}>
                    <span className="waiter-highlight-label">Comandas em aberto</span>
                    <span className="waiter-highlight-value mono-num">{formatBRL(totalComandasAmount)}</span>
                </div>
            </div>

            <section id="waiter-latest-orders" className="waiter-section" data-testid="latest-orders-section">
                <div className="waiter-section-head">
                    <h2 className="waiter-section-title">Últimos pedidos</h2>
                    <button type="button" className="waiter-link-btn" onClick={() => onTabChange("pedidos")} data-testid="btn-view-all-orders">Ver todos</button>
                </div>
                {latestOrders.length === 0 ? (
                    <p className="waiter-empty" data-testid="empty-latest-orders">Nenhum pedido em aberto na sua praça.</p>
                ) : (
                    <div className="waiter-order-list" data-testid="latest-orders-list">
                        {latestOrders.map((order) => {
                            const badge = deriveOrderBadge(order);
                            return (
                                <button key={order.id} type="button" className="waiter-order-row" onClick={() => onOrderClick(order.id)} data-testid={`latest-order-row-${order.id}`}>
                                    <span className="waiter-order-info">
                                        <span className="waiter-order-title">{orderLabel(order, tablesById, comandasById)}</span>
                                        <span className="waiter-order-meta">{order.items.length} itens · {elapsedLabel(order.openedAt)}</span>
                                    </span>
                                    <span className="waiter-order-badge" data-testid={`latest-order-badge-${order.id}`} style={{ "--w-badge": badgeToneVar[badge.tone] } as CSSProperties}>{badge.label}</span>
                                </button>
                            );
                        })}
                    </div>
                )}
            </section>

            <section className="waiter-section" data-testid="quick-actions-section">
                <h2 className="waiter-section-title">Ações rápidas</h2>
                <div className="waiter-quick-grid" data-testid="quick-actions-grid">
                    {quickActions.filter((action) => action.key !== "turno" || canSeeCaixa).map((action, idx) => (
                        <button key={`${action.key}-${idx}`} type="button" className="waiter-quick-tile" onClick={() => onQuickAction(action.key)} data-testid={`quick-action-${action.key}`}>
                            <span className="waiter-quick-icon" aria-hidden="true">{action.icon}</span>
                            <span className="waiter-quick-label">{action.label}</span>
                        </button>
                    ))}
                </div>
            </section>
        </div>
    );
}