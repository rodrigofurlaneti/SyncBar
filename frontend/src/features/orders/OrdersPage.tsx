import { useMemo, useState, type CSSProperties } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { generateTableQrToken, getTablesByBranch } from "../tables/api";
import { getOpenOrdersByBranch } from "./api";
import { getComandaSetting, getComandasByBranch } from "../comandas/api";
import { OpenComandaDialog } from "../comandas/OpenComandaDialog";
import { useAuthStore } from "../../stores/authStore";
import { ComandaStatus, TableStatus, formatBRL } from "../../lib/types";
import type { ComandaResponse, OrderResponse, TableResponse } from "../../lib/types";
import { OrderDrawer } from "./OrderDrawer";
import { OpenOrderDialog } from "./OpenOrderDialog";
import { OpenDeliveryOrderDialog } from "./OpenDeliveryOrderDialog";
import { QueryError } from "../../components/QueryError";
import { Overlay } from "./Overlay";
import { StorefrontHubModal } from "../storeFront/StorefrontHubModal";
import { TableCard, TableCardSkeleton } from "./TableCard";
import { ComandaCard, ComandaCardSkeleton } from "./ComandaCard";

export function OrdersPage() {
    const queryClient = useQueryClient();
    const { branchId } = useAuthStore();
    const [selectedOrderId, setSelectedOrderId] = useState<number | null>(null);
    const [openingTable, setOpeningTable] = useState<TableResponse | null>(null);
    const [openingComanda, setOpeningComanda] = useState<ComandaResponse | null>(null);
    const [comandaSearch, setComandaSearch] = useState("");
    const [qrTable, setQrTable] = useState<TableResponse | null>(null);
    const [qrUrl, setQrUrl] = useState<string | null>(null);
    const [openingDelivery, setOpeningDelivery] = useState(false);

    // Estado para controlar a abertura do modal unificado de autoatendimento da filial
    const [isLinkModalOpen, setIsLinkModalOpen] = useState(false);

    const qrMutation = useMutation({
        mutationFn: (tableId: number) => generateTableQrToken(tableId),
        onSuccess: (result) => setQrUrl(`${window.location.origin}/pedido/${result.token}`),
    });

    const tablesQuery = useQuery({
        queryKey: ["tables", branchId],
        queryFn: () => getTablesByBranch(branchId),
        refetchInterval: 15_000,
    });

    const comandaSettingQuery = useQuery({
        queryKey: ["comandas", "setting", branchId],
        queryFn: () => getComandaSetting(branchId),
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
            <main style={{ padding: "22px", maxWidth: 1240, margin: "0 auto" }} data-testid="orders-page-main">
                <section className="rise">
                    <div className="ui-row ui-row-wrap" style={{ alignItems: "baseline", gap: 14, marginBottom: 14 }}>
                        <h2 className="display" style={{ fontSize: "1.7rem" }}>
                            Mesas
                        </h2>
                        <span style={{ color: "var(--ink-faint)", fontSize: "0.9rem" }}>
                            toque numa mesa livre para abrir um pedido
                        </span>
                        <span className="ui-spacer" />
                        <button
                            className="btn-ghost"
                            type="button"
                            onClick={() => setOpeningDelivery(true)}
                            data-testid="btn-new-delivery"
                        >
                            + Retirada / Delivery
                        </button>
                        <button
                            className="btn-ghost"
                            type="button"
                            disabled={(tablesQuery.data ?? []).length === 0}
                            data-testid="btn-generate-qr-modal"
                            onClick={() => { setQrUrl(null); setQrTable(tablesQuery.data?.[0] ?? null); }}
                        >
                            Gerar QR de autoatendimento
                        </button>

                        {/* Botão para abrir o modal unificado de link e QR Code da filial */}
                        <button
                            className="btn-ghost"
                            type="button"
                            onClick={() => setIsLinkModalOpen(true)}
                            data-testid="btn-open-storefront-modal"
                        >
                            🔗 Gerar link de autoatendimento
                        </button>
                    </div>

                    {tablesQuery.isError && <QueryError error={tablesQuery.error} what="as mesas" />}
                    {ordersQuery.isError && <QueryError error={ordersQuery.error} what="os pedidos abertos" />}

                    <div className="table-grid" data-testid="tables-grid" aria-busy={tablesQuery.isLoading}>
                        {tablesQuery.isLoading
                            ? Array.from({ length: 8 }, (_, i) => <TableCardSkeleton key={i} />)
                            : (tablesQuery.data ?? []).map((table) => {
                                  const order = orderByTable.get(table.id);
                                  const isFree = table.tableStatusId === TableStatus.Livre;
                                  return (
                                      <TableCard
                                          key={table.id}
                                          id={table.id}
                                          number={table.number}
                                          statusId={table.tableStatusId}
                                          capacity={table.capacity}
                                          itemsCount={order?.items.length}
                                          totalValue={order?.totalAmount}
                                          openedAt={order?.openedAt}
                                          disabled={!order && !isFree}
                                          onOpen={() => {
                                              if (order) setSelectedOrderId(order.id);
                                              else if (isFree) setOpeningTable(table);
                                          }}
                                      />
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
                        <input
                            placeholder="nº…"
                            inputMode="numeric"
                            value={comandaSearch}
                            onChange={(e) => setComandaSearch(e.target.value)}
                            style={{ width: 110 }}
                            data-testid="input-comanda-search"
                        />
                    </div>

                    {comandasQuery.isError && (
                        <QueryError error={comandasQuery.error} what="as comandas" />
                    )}

                    <div className="comanda-grid" data-testid="comandas-grid" aria-busy={comandasQuery.isLoading}>
                        {comandasQuery.isLoading
                            ? Array.from({ length: 10 }, (_, i) => <ComandaCardSkeleton key={i} />)
                            : filteredComandas.map((comanda) => {
                                  const order = orderByComanda.get(comanda.id);
                                  const isAvailable = comanda.comandaStatusId === ComandaStatus.Disponivel;
                                  return (
                                      <ComandaCard
                                          key={comanda.id}
                                          id={comanda.id}
                                          code={comanda.code}
                                          statusId={comanda.comandaStatusId}
                                          totalValue={order?.totalAmount}
                                          disabled={!order && !isAvailable}
                                          onOpen={() => {
                                              if (order) setSelectedOrderId(order.id);
                                              else if (isAvailable) setOpeningComanda(comanda);
                                          }}
                                      />
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

            {openingDelivery && (
                <OpenDeliveryOrderDialog
                    onClose={() => setOpeningDelivery(false)}
                    onOpened={(orderId) => {
                        setOpeningDelivery(false);
                        refresh();
                        setSelectedOrderId(orderId);
                    }}
                />
            )}

            {/* Modal limpo da filial (Link Geral + QR Code Geral da Filial) */}
            <StorefrontHubModal
                isOpen={isLinkModalOpen}
                onClose={() => setIsLinkModalOpen(false)}
                branchId={branchId}
            />

            {qrTable && (
                <Overlay title="QR Code de autoatendimento" onClose={() => setQrTable(null)} data-testid="qr-overlay">
                    <label style={{ display: "grid", gap: 4 }}>
                        <span style={{ color: "var(--ink-dim)", fontSize: "0.85rem" }}>Mesa</span>
                        <select
                            value={qrTable.id}
                            data-testid="select-qr-table"
                            onChange={(e) => {
                                const table = (tablesQuery.data ?? []).find((t) => t.id === Number(e.target.value)) ?? null;
                                setQrTable(table);
                                setQrUrl(null);
                            }}
                        >
                            {(tablesQuery.data ?? []).map((t) => (
                                <option key={t.id} value={t.id}>Mesa {t.number}</option>
                            ))}
                        </select>
                    </label>

                    {qrMutation.isError && (
                        <p className="error-text" data-testid="qr-error-msg">
                            Falha ao gerar o QR Code — confirme que existe um funcionário configurado
                            para autoatendimento em Config. → Filial.
                        </p>
                    )}

                    {qrUrl ? (
                        <div style={{ display: "grid", gap: 10, justifyItems: "center" }}>
                            <img
                                src={`https://api.qrserver.com/v1/create-qr-code/?size=220x220&data=${encodeURIComponent(qrUrl)}`}
                                alt={`QR Code de autoatendimento da mesa ${qrTable.number}`}
                                width={220}
                                height={220}
                                style={{ borderRadius: 8, background: "#fff" }}
                                data-testid="qr-code-img"
                            />
                            <input readOnly value={qrUrl} onFocus={(e) => e.target.select()} style={{ width: "100%" }} />
                        </div>
                    ) : (
                        <button
                            className="btn-primary"
                            type="button"
                            disabled={qrMutation.isPending}
                            onClick={() => qrMutation.mutate(qrTable.id)}
                            data-testid="btn-submit-qr"
                        >
                            {qrMutation.isPending ? "Gerando…" : "Gerar QR Code"}
                        </button>
                    )}
                </Overlay>
            )}
        </>
    );
}