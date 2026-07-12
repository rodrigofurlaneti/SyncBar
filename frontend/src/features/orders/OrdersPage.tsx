import { useMemo, useState, type CSSProperties } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { getTablesByBranch } from "../tables/api";
import { getOpenOrdersByBranch } from "./api";
import { getComandaSetting, getComandasByBranch, setComandaDefaultLimit } from "../comandas/api";
import { useMyFeatures } from "../access/hooks";
import { OpenComandaDialog } from "../comandas/OpenComandaDialog";
import { useAuthStore } from "../../stores/authStore";
import { ComandaStatus, TableStatus, formatBRL } from "../../lib/types";
import type { ComandaResponse, OrderResponse, TableResponse } from "../../lib/types";
import { OrderDrawer } from "./OrderDrawer";
import { OpenOrderDialog } from "./OpenOrderDialog";
import { QueryError } from "../../components/QueryError";

const statusColor: Record<number, string> = {
  [TableStatus.Livre]: "var(--free)",
  [TableStatus.Ocupada]: "var(--busy)",
  [TableStatus.Reservada]: "var(--reserved)",
  [TableStatus.EmFechamento]: "var(--closing)",
  [TableStatus.Interditada]: "var(--blocked)",
};

const comandaColor: Record<number, string> = {
  [ComandaStatus.Disponivel]: "var(--free)",
  [ComandaStatus.EmUso]: "var(--busy)",
  [ComandaStatus.Extraviada]: "var(--closing)",
  [ComandaStatus.Bloqueada]: "var(--blocked)",
};

const comandaStatusLabel: Record<number, string> = {
  [ComandaStatus.Disponivel]: "Livre",
  [ComandaStatus.EmUso]: "Em uso",
  [ComandaStatus.Extraviada]: "Extraviada",
  [ComandaStatus.Bloqueada]: "Bloqueada",
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
  const [openingComanda, setOpeningComanda] = useState<ComandaResponse | null>(null);
  const [comandaSearch, setComandaSearch] = useState("");
  const [limitInput, setLimitInput] = useState("");

  const tablesQuery = useQuery({
    queryKey: ["tables", branchId],
    queryFn: () => getTablesByBranch(branchId),
    refetchInterval: 15_000,
  });

  const featuresQuery = useMyFeatures();

  const comandaSettingQuery = useQuery({
    queryKey: ["comandas", "setting", branchId],
    queryFn: () => getComandaSetting(branchId),
  });

  const limitMutation = useMutation({
    mutationFn: (value: number) => setComandaDefaultLimit(branchId, value),
    onSuccess: () => {
      setLimitInput("");
      void queryClient.invalidateQueries({ queryKey: ["comandas", "setting"] });
    },
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

  const orderByTable = useMemo(() => {
    const map = new Map<number, OrderResponse>();
    for (const order of ordersQuery.data ?? [])
      if (order.diningTableId !== null) map.set(order.diningTableId, order);
    return map;
  }, [ordersQuery.data]);

  const orderByComanda = useMemo(() => {
    const map = new Map<number, OrderResponse>();
    for (const order of ordersQuery.data ?? [])
      if (order.comandaId !== null) map.set(order.comandaId, order);
    return map;
  }, [ordersQuery.data]);

  const filteredComandas = useMemo(
    () =>
      (comandasQuery.data ?? []).filter((c) =>
        comandaSearch.trim() === "" ? true : c.code.includes(comandaSearch.trim()),
      ),
    [comandasQuery.data, comandaSearch],
  );

  const refresh = () => {
    void queryClient.invalidateQueries({ queryKey: ["tables"] });
    void queryClient.invalidateQueries({ queryKey: ["orders"] });
    void queryClient.invalidateQueries({ queryKey: ["comandas"] });
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
          {tablesQuery.isError && <QueryError error={tablesQuery.error} what="as mesas" />}
          {ordersQuery.isError && <QueryError error={ordersQuery.error} what="os pedidos abertos" />}

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

        <section className="rise rise-2" style={{ marginTop: 34 }}>
          <div style={{ display: "flex", alignItems: "baseline", gap: 14, marginBottom: 14, flexWrap: "wrap" }}>
            <h2 className="display" style={{ fontSize: "1.7rem" }}>Comandas</h2>
            <span style={{ color: "var(--ink-faint)", fontSize: "0.9rem" }}>
              toque numa comanda livre para abrir uma conta individual
            </span>
            <span style={{ flex: 1 }} />
            {comandaSettingQuery.data && (
              <span className="chip" style={{ "--dot": "var(--busy)" } as CSSProperties}>
                limite {formatBRL(comandaSettingQuery.data.defaultLimitAmount)}
              </span>
            )}
            {featuresQuery.data?.canManageAccess && (
              <span style={{ display: "flex", gap: 6 }}>
                <input
                  placeholder="novo limite"
                  inputMode="decimal"
                  value={limitInput}
                  onChange={(e) => setLimitInput(e.target.value)}
                  style={{ width: 120 }}
                />
                <button
                  className="btn-ghost"
                  style={{ minHeight: 44 }}
                  disabled={limitMutation.isPending || limitInput.trim() === ""}
                  onClick={() => {
                    const value = Number(limitInput.replace(",", "."));
                    if (Number.isFinite(value) && value > 0) limitMutation.mutate(value);
                  }}
                >
                  Salvar
                </button>
              </span>
            )}
            <input
              placeholder="nº…"
              inputMode="numeric"
              value={comandaSearch}
              onChange={(e) => setComandaSearch(e.target.value)}
              style={{ width: 110 }}
            />
          </div>

          {comandasQuery.isError && (
            <QueryError error={comandasQuery.error} what="as comandas" />
          )}

          <div
            style={{
              display: "grid",
              gridTemplateColumns: "repeat(auto-fill, minmax(76px, 1fr))",
              gap: 8,
            }}
          >
            {filteredComandas.map((comanda) => {
              const order = orderByComanda.get(comanda.id);
              const color = comandaColor[comanda.comandaStatusId] ?? "var(--ink-faint)";
              const busy = comanda.comandaStatusId === ComandaStatus.EmUso;
              return (
                <button
                  key={comanda.id}
                  onClick={() => {
                    if (order) setSelectedOrderId(order.id);
                    else if (comanda.comandaStatusId === ComandaStatus.Disponivel)
                      setOpeningComanda(comanda);
                  }}
                  title={order ? `${order.items.length} itens · ${formatBRL(order.totalAmount)}` : undefined}
                  style={{
                    background: busy ? "var(--bg-press)" : "var(--bg-raise)",
                    border: `1px solid ${busy ? color : "var(--line)"}`,
                    borderRadius: 10,
                    minHeight: 64,
                    display: "grid",
                    gap: 2,
                    placeItems: "center",
                    padding: "8px 4px",
                  }}
                >
                  <span className="display mono-num" style={{ fontSize: "1.5rem", color: busy ? color : "var(--ink)" }}>
                    {comanda.code}
                  </span>
                  {order ? (
                    <span className="mono-num" style={{ fontSize: "0.68rem", color: "var(--ink-dim)" }}>
                      {formatBRL(order.totalAmount)}
                    </span>
                  ) : (
                    <span style={{ fontSize: "0.62rem", color, textTransform: "uppercase", letterSpacing: "0.06em" }}>
                      {comandaStatusLabel[comanda.comandaStatusId] ?? ""}
                    </span>
                  )}
                </button>
              );
            })}
          </div>
        </section>
      </main>

      {openingComanda && (
        <OpenComandaDialog
          comanda={openingComanda}
          onClose={() => setOpeningComanda(null)}
          onOpened={(orderId) => {
            setOpeningComanda(null);
            refresh();
            setSelectedOrderId(orderId);
          }}
        />
      )}

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
