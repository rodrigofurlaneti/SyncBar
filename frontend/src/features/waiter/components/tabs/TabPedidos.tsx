import type { CSSProperties } from "react";
import { formatBRL } from "../../../../lib/types";
import type { OrderResponse, TableResponse, ComandaResponse } from "../../../../lib/types";
import { deriveOrderBadge, badgeToneVar, orderLabel, elapsedLabel } from "../../utils";

interface TabPedidosProps {
    myOrders: OrderResponse[];
    tablesById: Map<number, TableResponse>;
    comandasById: Map<number, ComandaResponse>;
    onOrderClick: (orderId: number) => void;
}

export function TabPedidos({ myOrders, tablesById, comandasById, onOrderClick }: TabPedidosProps) {
    return (
        <section className="waiter-section" data-testid="tab-pedidos-section">
            <h2 className="waiter-section-title" style={{ marginBottom: 16 }}>Histórico de Pedidos</h2>

            {myOrders.length === 0 ? (
                <p className="waiter-empty" data-testid="empty-orders-msg">Nenhum pedido registrado na sua praça no momento.</p>
            ) : (
                <div className="waiter-order-list" data-testid="waiter-order-list">
                    {myOrders.map((order) => {
                        const badge = deriveOrderBadge(order);
                        return (
                            <button
                                key={order.id}
                                type="button"
                                className="waiter-order-row"
                                onClick={() => onOrderClick(order.id)}
                                data-testid={`order-row-${order.id}`}
                                style={{
                                    backgroundColor: "var(--bg-elevated, var(--surface, #ffffff))",
                                    borderRadius: "10px",
                                    padding: "14px",
                                    marginBottom: "10px",
                                    border: "1px solid var(--line, var(--border, #e5e7eb))",
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
                                    data-testid={`order-badge-${order.id}`}
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
    );
}