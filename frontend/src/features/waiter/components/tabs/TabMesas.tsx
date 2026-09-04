import { TableStatus, formatBRL } from "../../../../lib/types";
import type { TableResponse, OrderResponse } from "../../../../lib/types";

interface TabMesasProps {
    activeAreaId: number | null;
    myTables: TableResponse[];
    ordersByTableId: Map<number, OrderResponse>;
    onTableClick: (tableId: number, statusId: number) => void;
}

export function TabMesas({ activeAreaId, myTables, ordersByTableId, onTableClick }: TabMesasProps) {
    if (!activeAreaId) return <p className="waiter-empty" data-testid="no-area-msg">Nenhuma praça vinculada a você no momento.</p>;
    if (myTables.length === 0) return <p className="waiter-empty" data-testid="no-tables-msg">Nenhuma mesa foi configurada nesta praça.</p>;

    return (
        <section className="waiter-section" data-testid="tab-mesas-section">
            <h2 className="waiter-section-title" style={{ marginBottom: 16 }}>Minhas Mesas</h2>
            <div className="waiter-tables-grid" data-testid="waiter-tables-grid" style={{ display: "grid", gridTemplateColumns: "repeat(auto-fill, minmax(150px, 1fr))", gap: "14px" }}>
                {myTables.map((table) => {
                    const order = ordersByTableId.get(table.id);

                    let leftBorderColor = "var(--w-ok)";
                    let statusBg = "color-mix(in srgb, var(--w-ok) 15%, transparent)";
                    let statusColor = "var(--w-ok)";
                    let statusText = "LIVRE";
                    let subText = `${table.capacity ?? 4} lugares`;

                    if (table.tableStatusId === TableStatus.Ocupada) {
                        leftBorderColor = "var(--w-warn)";
                        statusBg = "color-mix(in srgb, var(--w-warn) 15%, transparent)";
                        statusColor = "var(--w-warn)";
                        statusText = "OCUPADA";
                        if (order) subText = `${order.items.length} itens - ${formatBRL(order.totalAmount)}`;
                    } else if (table.tableStatusId === TableStatus.EmFechamento) {
                        leftBorderColor = "var(--w-info)";
                        statusBg = "color-mix(in srgb, var(--w-info) 15%, transparent)";
                        statusColor = "var(--w-info)";
                        statusText = "FECHANDO";
                    }

                    return (
                        <button
                            key={table.id}
                            onClick={() => onTableClick(table.id, table.tableStatusId)}
                            data-testid={`table-tile-${table.id}`}
                            style={{
                                display: "flex",
                                flexDirection: "column",
                                alignItems: "stretch",
                                backgroundColor: "var(--w-bg-card)",
                                borderRadius: "12px",
                                border: "1px solid var(--w-line)",
                                borderLeft: `6px solid ${leftBorderColor}`,
                                padding: "14px 16px",
                                cursor: "pointer",
                                textAlign: "left",
                                minHeight: "88px",
                                justifyContent: "center"
                            }}
                        >
                            <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", width: "100%", marginBottom: "6px" }}>
                                <span style={{ fontFamily: "var(--font-display)", fontSize: "2rem", color: "var(--w-ink)", lineHeight: 1 }}>
                                    {table.number}
                                </span>
                                <span data-testid={`table-status-${table.id}`} style={{ fontSize: "0.65rem", fontWeight: "700", backgroundColor: statusBg, color: statusColor, padding: "4px 8px", borderRadius: "20px", display: "flex", alignItems: "center", gap: "4px", textTransform: "uppercase" }}>
                                    <span style={{ width: "6px", height: "6px", borderRadius: "50%", backgroundColor: statusColor }} />
                                    {statusText}
                                </span>
                            </div>
                            <span data-testid={`table-subtext-${table.id}`} style={{ fontSize: "0.85rem", color: "var(--w-ink-dim)", fontWeight: 500 }}>
                                {subText}
                            </span>
                        </button>
                    );
                })}
            </div>
        </section>
    );
}