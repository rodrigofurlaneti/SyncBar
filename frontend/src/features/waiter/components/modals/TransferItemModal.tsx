import { useState, useMemo } from "react";
import { useQuery } from "@tanstack/react-query";
import { api } from "../../../../lib/apiClient";
import type { TableResponse, OrderResponse } from "../../../../lib/types";

interface TransferItemModalProps {
    myTables: TableResponse[];
    ordersByTableId: Map<number, OrderResponse>;
    allActiveOrders: OrderResponse[];
    employeeId: number | null;
    onClose: () => void;
    onSuccess: (msg: string) => void;
    onError: (msg: string) => void;
}

// Interface auxiliar para tipar o produto da API
interface ProductDetails {
    id: number;
    name: string;
}

export function TransferItemModal({
    myTables,
    ordersByTableId,
    allActiveOrders,
    employeeId,
    onClose,
    onSuccess,
    onError
}: TransferItemModalProps) {
    const [sourceTableId, setSourceTableId] = useState<string>("");
    const [targetTableId, setTargetTableId] = useState<string>("");
    const [selectedItemId, setSelectedItemId] = useState<string>("");
    const [isTransferring, setIsTransferring] = useState(false);
    const sourceOrder = useMemo(() => {
        if (!sourceTableId) return null;
        return allActiveOrders.find(o => o.diningTableId === Number(sourceTableId)) || null;
    }, [allActiveOrders, sourceTableId]);
    const productIds = useMemo(() => {
        if (!sourceOrder) return [];
        return Array.from(new Set(sourceOrder.items.map(i => i.productId)));
    }, [sourceOrder]);
    const productQueries = useQuery({
        queryKey: ["products-details", productIds],
        queryFn: async () => {
            const promises = productIds.map(async (id) => {
                try {
                    const res = await api<ProductDetails>(`/api/products/${id}`);
                    return res;
                } catch {
                    return null;
                }
            });
            const results = await Promise.all(promises);
            const map = new Map<number, string>();
            results.forEach(prod => {
                if (prod) map.set(prod.id, prod.name);
            });
            return map;
        },
        enabled: productIds.length > 0,
    });

    const productsMap = productQueries.data ?? new Map<number, string>();
    const handleExecuteTransfer = async (e: React.FormEvent) => {
        e.preventDefault();
        if (!sourceTableId || !targetTableId || !selectedItemId || !sourceOrder) {
            return onError("Preencha todos os campos da transferência.");
        }
        const targetOrder = allActiveOrders.find(o => o.diningTableId === Number(targetTableId));
        if (!targetOrder) {
            return onError("A mesa de destino precisa ter um pedido aberto para receber o item.");
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
            onSuccess("Item transferido com sucesso!");
            onClose();
        } catch (err: any) {
            onError(err?.message || "Erro ao realizar transferência.");
        } finally {
            setIsTransferring(false);
        }
    };
    return (
        <div
            className="modal-backdrop is-center"
            onMouseDown={(e) => {
                if (e.target === e.currentTarget) onClose();
            }}
            style={{ position: "absolute" }}
        >
            <div className="modal-panel is-center" style={{ width: "90%", maxWidth: "420px" }}>
                <div className="modal-head" style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: "16px" }}>
                    <span className="display" style={{ fontSize: "1.2rem", fontWeight: "bold" }}>🔀 Transferir Item de Mesa</span>
                    <button type="button" className="btn-ghost btn-icon" aria-label="Fechar" onClick={onClose}>
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
                            <option value="">{productQueries.isLoading ? "Carregando produtos..." : "Selecione o item..."}</option>
                            {sourceOrder?.items
                                .filter((item: any) => item.orderItemStatusId !== 6)
                                .map((item: any) => {
                                    let statusLabel = item.orderItemStatusId.toString();
                                    if (item.orderItemStatusId === 1) statusLabel = "Lançado";
                                    if (item.orderItemStatusId === 2) statusLabel = "Enviado Cozinha";
                                    if (item.orderItemStatusId === 3) statusLabel = "Em Preparo";
                                    if (item.orderItemStatusId === 4) statusLabel = "Pronto";
                                    if (item.orderItemStatusId === 5) statusLabel = "Entregue";
                                    const pId = item.ProductId || item.productId;
                                    const productName = productsMap.get(pId) || `Produto #${pId}`;
                                    return (
                                        <option key={item.id} value={item.id}>
                                            {productName} — Qtd: {item.quantity} ({statusLabel})
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
                                <option key={t.id} value={t.id}>
                                    Mesa {t.number} ({t.tableStatusId === 1 ? "Livre" : "Ocupada"})
                                </option>
                            ))}
                        </select>
                    </div>
                    <div style={{ display: "flex", gap: "10px", marginTop: "10px" }}>
                        <button
                            type="button"
                            className="btn-ghost"
                            style={{ flex: 1, padding: "10px", borderRadius: "8px" }}
                            onClick={onClose}
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
    );
}