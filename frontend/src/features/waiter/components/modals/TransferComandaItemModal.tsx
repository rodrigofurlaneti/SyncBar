import { useState, useMemo } from "react";
import { useQuery } from "@tanstack/react-query";
import { api } from "../../../../lib/apiClient";
import type { ComandaResponse, OrderResponse } from "../../../../lib/types";

interface TransferComandaItemModalProps {
    comandas: ComandaResponse[];
    comandaOrders: OrderResponse[];
    allActiveOrders: OrderResponse[];
    employeeId: number | null;
    onClose: () => void;
    onSuccess: (msg: string) => void;
    onError: (msg: string) => void;
}

interface ProductDetails {
    id: number;
    name: string;
}

export function TransferComandaItemModal({
    comandas,
    comandaOrders,
    allActiveOrders,
    employeeId,
    onClose,
    onSuccess,
    onError
}: TransferComandaItemModalProps) {
    const [sourceComandaId, setSourceComandaId] = useState<string>("");
    const [targetComandaId, setTargetComandaId] = useState<string>("");
    const [selectedItemId, setSelectedItemId] = useState<string>("");
    const [isTransferring, setIsTransferring] = useState(false);
    const sourceOrder = useMemo(() => {
        if (!sourceComandaId) return null;
        return allActiveOrders.find(o => o.comandaId === Number(sourceComandaId)) || null;
    }, [allActiveOrders, sourceComandaId]);
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
        if (!sourceComandaId || !targetComandaId || !selectedItemId || !sourceOrder) {
            return onError("Preencha todos os campos da transferência de comanda.");
        }
        const targetOrder = allActiveOrders.find(o => o.comandaId === Number(targetComandaId));
        if (!targetOrder) {
            return onError("A comanda de destino precisa ter um pedido aberto para receber o item.");
        }
        setIsTransferring(true);
        try {
            await api("/api/orders/comanda-items/transfer", {
                method: "PUT",
                body: JSON.stringify({
                    sourceCustomerOrderId: sourceOrder.id,
                    targetCustomerOrderId: targetOrder.id,
                    customerOrderItemId: Number(selectedItemId),
                    sourceComandaId: Number(sourceComandaId),
                    targetComandaId: Number(targetComandaId),
                    actorEmployeeId: employeeId ?? 1,
                }),
            });
            onSuccess("Item transferido entre comandas com sucesso!");
            onClose();
        } catch (err: any) {
            onError(err?.message || "Erro ao realizar transferência de comanda.");
        } finally {
            setIsTransferring(false);
        }
    };
    return (
        <div className="modal-backdrop is-center" onClick={onClose} style={{ position: "absolute" }}>
            <div className="modal-panel is-center" onClick={(e) => e.stopPropagation()} style={{ width: "90%", maxWidth: "420px" }}>
                <div className="modal-head" style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: "16px" }}>
                    <span className="display" style={{ fontSize: "1.2rem", fontWeight: "bold" }}>🔀 Transferir Item de Comanda</span>
                    <button type="button" className="btn-ghost btn-icon" aria-label="Fechar" onClick={onClose}>
                        ✕
                    </button>
                </div>
                <form onSubmit={handleExecuteTransfer} style={{ display: "grid", gap: "14px" }}>
                    {/* Comanda de Origem */}
                    <div style={{ display: "grid", gap: "6px" }}>
                        <label style={{ fontSize: "0.85rem", fontWeight: "600", color: "var(--ink-dim)" }}>Comanda de Origem (Com pedido)</label>
                        <select
                            value={sourceComandaId}
                            onChange={(e) => {
                                setSourceComandaId(e.target.value);
                                setSelectedItemId("");
                            }}
                            style={{ padding: "10px", borderRadius: "8px", border: "1px solid var(--border)", backgroundColor: "var(--surface)", color: "var(--ink)" }}
                            required
                        >
                            <option value="">Selecione a comanda de origem...</option>
                            {comandas
                                .filter(c => comandaOrders.some(o => o.comandaId === c.id))
                                .map(c => (
                                    <option key={c.id} value={c.id}>
                                        Comanda {c.code}
                                    </option>
                                ))}
                        </select>
                    </div>

                    {/* Item a ser transferido */}
                    <div style={{ display: "grid", gap: "6px" }}>
                        <label style={{ fontSize: "0.85rem", fontWeight: "600", color: "var(--ink-dim)" }}>Item a ser transferido</label>
                        <select
                            value={selectedItemId}
                            onChange={(e) => setSelectedItemId(e.target.value)}
                            style={{ padding: "10px", borderRadius: "8px", border: "1px solid var(--border)", backgroundColor: "var(--surface)", color: "var(--ink)" }}
                            required
                            disabled={!sourceComandaId || !sourceOrder}
                        >
                            <option value="">{productQueries.isLoading ? "Carregando produtos..." : "Selecione o item..."}</option>
                            {sourceOrder?.items
                                .filter((item: any) => item.orderItemStatusId !== 6) // Exclui cancelados
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

                    {/* Comanda de Destino */}
                    <div style={{ display: "grid", gap: "6px" }}>
                        <label style={{ fontSize: "0.85rem", fontWeight: "600", color: "var(--ink-dim)" }}>Comanda de Destino</label>
                        <select
                            value={targetComandaId}
                            onChange={(e) => setTargetComandaId(e.target.value)}
                            style={{ padding: "10px", borderRadius: "8px", border: "1px solid var(--border)", backgroundColor: "var(--surface)", color: "var(--ink)" }}
                            required
                        >
                            <option value="">Selecione a comanda de destino...</option>
                            {comandas
                                .filter(c => c.id.toString() !== sourceComandaId)
                                .map(c => (
                                    <option key={c.id} value={c.id}>
                                        Comanda {c.code} {comandaOrders.some(o => o.comandaId === c.id) ? "(Em uso)" : "(Livre)"}
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