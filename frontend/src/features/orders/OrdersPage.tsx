import { useMemo, useState, type CSSProperties } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { getTablesByBranch } from "../tables/api";
import { getOpenOrdersByBranch } from "./api";
import { useAuthStore } from "../../stores/authStore";
import { TableStatus, formatBRL } from "../../lib/types";
import type { OrderResponse, TableResponse } from "../../lib/types";
import { OrderDrawer } from "./OrderDrawer";
import { OpenOrderDialog } from "./OpenOrderDialog";

const statusColor: Record<number, string> = {
  [TableStatus.Livre]: "var(--free)",
  [TableStatus.Ocupada]: "var(--busy)",
  [TableStatus.Reservada]: "var(--reserved)",
  [TableStatus.EmFechamento]: "var(--closing)",
  [TableStatus.Interditada]: "var(--blocked)",
};

const statusLabel: Record<number, string> = {
  [TableStatus.Livre]: "Livre",
  [TableStatus.Ocupada]: "Ocupada",
  [TableStatus.Reservada]: "Reservada",
  [TableStatus.EmFechamento]: "Fechando",
  [TableStatus.Interditada]: "Interditada",
};

export function OrdersPage() {
  const queryClient = useQueryClient();
  const { branchId } = useAuthStore();
  const [selectedOrderId, setSelectedOrderId] = useState<number | null>(null);
  const [openingTable, setOpeningTable] = useState<TableResponse | null>(null);

  const tablesQuery = useQuery({
    queryKey: ["tables", branchId],
    queryFn: () => getTablesByBranch(branchId),
    refetchInterval: 15_000,
  });

  const ordersQuery = useQuery({
    queryKey: ["orders", "open", branchId],
    queryFn: () => getOpenOrdersByBranch(branchId),
    refetchInterval: 15_000,
  });

  const orderByTable = useMemo(() => {
    const map = new Map<number, OrderResponse>();
    for (const order of ordersQuery.data ?? [])
      if (order.diningTableId !== null) map.set(order.diningTableId, order);
    return map;
  }, [ordersQuery.data]);

  const comandaOrders = useMemo(
    () => (ordersQuery.data ?? []).filter((o) => o.diningTableId === null),
    [ordersQuery.data],
  );

  const refresh = () => {
    void queryClient.invalidateQueries({ queryKey: ["tables"] });
    void queryClient.invalidateQueries({ queryKey: ["orders"] });
  };

  return (
    <>

      <main style={{ padding: "22px", maxWidth: 1240, margin: "0 auto" }}>
        <section className="rise">
          <div style={{ display: "flex", alignItems: "baseline", gap: 14, marginBottom: 14 }}>
            <h2 className="display" style={{ fontSize: "1.7rem" }}>
              Mesas
            </h2>
            <span style={{ color: "var(--ink-faint)", fontSize: "0.9rem" }}>
              toque numa mesa livre para abrir um pedido
            </span>
          </div>

          {tablesQuery.isLoading && <p style={{ color: "var(--ink-dim)" }}>Carregando mesas…</p>}
          {tablesQuery.isError && (
            <p className="error-text">Falha ao carregar mesas. A API está de pé?</p>
          )}

          <div className="table-grid">
            {(tablesQuery.data ?? []).map((table) => {
              const order = orderByTable.get(table.id);
              const color = statusColor[table.tableStatusId] ?? "var(--ink-faint)";
              return (
                <button
                  key={table.id}
                  className="table-tile"
                  style={{ "--status": color } as CSSProperties}
                  onClick={() => {
                    if (order) setSelectedOrderId(order.id);
                    else if (table.tableStatusId === TableStatus.Livre) setOpeningTable(table);
                  }}
                >
                  <div style={{ display: "flex", justifyContent: "space-between" }}>
                    <span className="num mono-num">{table.number}</span>
                    <span className="chip" style={{ "--dot": color } as CSSProperties}>
                      {statusLabel[table.tableStatusId] ?? "—"}
                    </span>
                  </div>
                  <div style={{ color: "var(--ink-dim)", fontSize: "0.88rem" }}>
                    {order ? (
                      <span className="mono-num">
                        {order.items.length} itens · {formatBRL(order.totalAmount)}
                      </span>
                    ) : (
                      <span>{table.capacity ?? "—"} lugares</span>
                    )}
                  </div>
                </button>
              );
            })}
          </div>
        </section>

        {comandaOrders.length > 0 && (
          <section className="rise rise-2" style={{ marginTop: 34 }}>
            <h2 className="display" style={{ fontSize: "1.7rem", marginBottom: 14 }}>
              Comandas abertas
            </h2>
            <div className="table-grid">
              {comandaOrders.map((order) => (
                <button
                  key={order.id}
                  className="table-tile"
                  style={{ "--status": "var(--busy)" } as CSSProperties}
                  onClick={() => setSelectedOrderId(order.id)}
                >
                  <span className="num mono-num">#{order.comandaId}</span>
                  <span className="mono-num" style={{ color: "var(--ink-dim)", fontSize: "0.88rem" }}>
                    {order.items.length} itens · {formatBRL(order.totalAmount)}
                  </span>
                </button>
              ))}
            </div>
          </section>
        )}
      </main>

      {openingTable && (
        <OpenOrderDialog
          table={openingTable}
          onClose={() => setOpeningTable(null)}
          onOpened={(orderId) => {
            setOpeningTable(null);
            refresh();
            setSelectedOrderId(orderId);
          }}
        />
      )}

      {selectedOrderId !== null && (
        <OrderDrawer
          orderId={selectedOrderId}
          onClose={() => {
            setSelectedOrderId(null);
            refresh();
          }}
        />
      )}
    </>
  );
}
