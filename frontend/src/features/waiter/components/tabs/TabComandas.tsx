import { formatBRL } from "../../../../lib/types";
import type { ComandaResponse, OrderResponse } from "../../../../lib/types";

interface TabComandasProps {
    isLoading: boolean;
    comandas: ComandaResponse[] | undefined;
    comandaOrders: OrderResponse[];
    onComandaClick: (comanda: ComandaResponse, orderId?: number) => void;
}

export function TabComandas({ isLoading, comandas, comandaOrders, onComandaClick }: TabComandasProps) {
    return (
        <section className="waiter-section">
            <h2 className="waiter-section-title" style={{ marginBottom: 16 }}>Comandas</h2>

            {isLoading ? (
                <p className="waiter-empty">Carregando comandas...</p>
            ) : !comandas || comandas.length === 0 ? (
                <p className="waiter-empty">Nenhuma comanda registrada.</p>
            ) : (
                <div className="waiter-tables-grid" style={{ display: "grid", gridTemplateColumns: "repeat(auto-fill, minmax(150px, 1fr))", gap: "14px" }}>
                    {comandas.map((comanda) => {
                        const order = comandaOrders.find((o) => o.comandaId === comanda.id);

                        let leftBorderColor = "var(--w-ok)";
                        let statusBg = "color-mix(in srgb, var(--w-ok) 15%, transparent)";
                        let statusColor = "var(--w-ok)";
                        let statusText = "LIVRE";
                        let subText = "Toque para abrir";

                        if (order) {
                            leftBorderColor = "var(--w-warn)";
                            statusBg = "color-mix(in srgb, var(--w-warn) 15%, transparent)";
                            statusColor = "var(--w-warn)";
                            statusText = "EM USO";
                            subText = `${order.items.length} itens - ${formatBRL(order.totalAmount)}`;
                        }

                        return (
                            <button
                                key={comanda.id}
                                onClick={() => onComandaClick(comanda, order?.id)}
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
                                    {/* Aqui está a mágica: aplicando font-display para ficar igual à mesa */}
                                    <span style={{ fontFamily: "var(--font-display)", fontSize: "2rem", color: "var(--w-ink)", lineHeight: 1 }}>
                                        {comanda.code || comanda.id}
                                    </span>
                                    <span style={{ fontSize: "0.65rem", fontWeight: "700", backgroundColor: statusBg, color: statusColor, padding: "4px 8px", borderRadius: "20px", display: "flex", alignItems: "center", gap: "4px", textTransform: "uppercase" }}>
                                        <span style={{ width: "6px", height: "6px", borderRadius: "50%", backgroundColor: statusColor }} />
                                        {statusText}
                                    </span>
                                </div>
                                <span style={{ fontSize: "0.85rem", color: "var(--w-ink-dim)", fontWeight: 500 }}>
                                    {subText}
                                </span>
                            </button>
                        );
                    })}
                </div>
            )}
        </section>
    );
}