import { useState, useMemo } from "react";
import { useQuery } from "@tanstack/react-query";
import { api } from "../../../../lib/apiClient";
import type { TableResponse, ComandaResponse, OrderResponse } from "../../../../lib/types";

interface TransferItemModalProps {
    mode?: "table" | "comanda";
    myTables?: TableResponse[];
    comandas?: ComandaResponse[];
    comandaOrders?: OrderResponse[];
    ordersByTableId?: Map<number, OrderResponse>;
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

export function TransferItemModal({
    mode = "table",
    myTables = [],
    comandas = [],
    comandaOrders = [],
    ordersByTableId,
    allActiveOrders,
    employeeId,
    onClose,
    onSuccess,
    onError
}: TransferItemModalProps) {
    const [transferType, setTransferType] = useState<"table" | "comanda">(mode);

    const [sourceId, setSourceId] = useState<string>("");
    const [targetId, setTargetId] = useState<string>("");

    const [transferMode, setTransferMode] = useState<"single" | "batch">("single");
    const [selectedItemIds, setSelectedItemIds] = useState<number[]>([]);
    const [searchTerm, setSearchTerm] = useState("");
    const [isTransferring, setIsTransferring] = useState(false);

    // Mapeamento robusto de pedidos por ID de mesa
    const effectiveOrdersByTableId = useMemo(() => {
        if (ordersByTableId && ordersByTableId.size > 0) return ordersByTableId;
        const map = new Map<number, OrderResponse>();
        for (const order of allActiveOrders) {
            if (order.diningTableId !== null) map.set(order.diningTableId, order);
        }
        return map;
    }, [ordersByTableId, allActiveOrders]);

    const sourceOrder = useMemo(() => {
        if (!sourceId) return null;
        const idNum = Number(sourceId);
        if (transferType === "table") {
            return effectiveOrdersByTableId.get(idNum) || allActiveOrders.find(o => o.diningTableId === idNum) || null;
        } else {
            return allActiveOrders.find(o => o.comandaId === idNum) || null;
        }
    }, [allActiveOrders, effectiveOrdersByTableId, sourceId, transferType]);

    const productIds = useMemo(() => {
        if (!sourceOrder) return [];
        return Array.from(new Set(sourceOrder.items.map(i => i.productId)));
    }, [sourceOrder]);

    const productQueries = useQuery({
        queryKey: ["products-details", productIds],
        queryFn: async () => {
            const promises = productIds.map(async (id) => {
                try {
                    return await api<ProductDetails>(`/api/products/${id}`);
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

    const availableItems = useMemo(() => {
        if (!sourceOrder) return [];
        return sourceOrder.items.filter((item: any) => {
            if (item.orderItemStatusId === 6) return false;
            if (!searchTerm) return true;
            const pId = item.productId || item.ProductId;
            const pName = productsMap.get(pId) || "";
            return pName.toLowerCase().includes(searchTerm.toLowerCase());
        });
    }, [sourceOrder, productsMap, searchTerm]);

    const handleToggleItem = (itemId: number) => {
        if (isTransferring) return;
        if (transferMode === "single") {
            setSelectedItemIds([itemId]);
        } else {
            setSelectedItemIds(prev =>
                prev.includes(itemId) ? prev.filter(id => id !== itemId) : [...prev, itemId]
            );
        }
    };

    const handleSelectAll = () => {
        if (isTransferring) return;
        if (selectedItemIds.length === availableItems.length) {
            setSelectedItemIds([]);
        } else {
            setSelectedItemIds(availableItems.map((i: any) => i.id));
        }
    };

    const handleTypeChange = (newType: "table" | "comanda") => {
        if (isTransferring) return;
        setTransferType(newType);
        setSourceId("");
        setTargetId("");
        setSelectedItemIds([]);
        setSearchTerm("");
        setTransferMode("single");
    };

    const handleExecuteTransfer = async (e: React.FormEvent) => {
        e.preventDefault();
        if (isTransferring) return;

        if (!sourceId || !targetId || selectedItemIds.length === 0 || !sourceOrder) {
            return onError("Preencha a origem, o destino e selecione ao menos um item.");
        }

        const targetIdNum = Number(targetId);
        const targetOrder = transferType === "table"
            ? effectiveOrdersByTableId.get(targetIdNum) || allActiveOrders.find(o => o.diningTableId === targetIdNum)
            : allActiveOrders.find(o => o.comandaId === targetIdNum);

        if (!targetOrder) {
            const destinoTipo = transferType === "table" ? "mesa" : "comanda";
            return onError(`A ${destinoTipo} de destino precisa ter um pedido aberto para receber o(s) item(ns).`);
        }

        setIsTransferring(true);
        try {
            const endpoint = transferType === "table"
                ? "/api/orders/items/transfer-batch"
                : "/api/orders/comanda-items/transfer-batch";

            const payload = transferType === "table" ? {
                sourceCustomerOrderId: sourceOrder.id,
                targetCustomerOrderId: targetOrder.id,
                customerOrderItemIds: selectedItemIds,
                sourceDiningTableId: Number(sourceId),
                targetDiningTableId: targetIdNum,
                actorEmployeeId: employeeId ?? 1,
            } : {
                sourceCustomerOrderId: sourceOrder.id,
                targetCustomerOrderId: targetOrder.id,
                customerOrderItemIds: selectedItemIds,
                sourceComandaId: Number(sourceId),
                targetComandaId: targetIdNum,
                actorEmployeeId: employeeId ?? 1,
            };

            await api(endpoint, {
                method: "PUT",
                body: JSON.stringify(payload),
            });

            onSuccess(`${selectedItemIds.length} item(ns) transferido(s) com sucesso!`);
            onClose();
        } catch (err: any) {
            onError(err?.message || "Erro ao realizar transferência.");
            setIsTransferring(false);
        }
    };

    return (
        <div
            role="dialog"
            aria-modal="true"
            onMouseDown={(e) => {
                if (e.target === e.currentTarget && !isTransferring) onClose();
            }}
            style={{
                position: "fixed",
                inset: 0,
                backgroundColor: "rgba(0, 0, 0, 0.75)",
                backdropFilter: "blur(4px)",
                display: "flex",
                alignItems: "center",
                justifyContent: "center",
                zIndex: 99999,
                padding: "16px",
                boxSizing: "border-box"
            }}
        >
            <div
                style={{
                    width: "100%",
                    maxWidth: "480px",
                    maxHeight: "90vh",
                    backgroundColor: "#1e293b",
                    color: "#f8fafc",
                    borderRadius: "16px",
                    padding: "24px",
                    boxShadow: "0 25px 50px -12px rgba(0, 0, 0, 0.7)",
                    display: "flex",
                    flexDirection: "column",
                    boxSizing: "border-box",
                    overflowY: "auto",
                    border: "1px solid #334155"
                }}
            >
                {/* Header & Abas */}
                <div style={{ display: "flex", flexDirection: "column", gap: "12px", marginBottom: "16px", borderBottom: "1px solid #334155", paddingBottom: "12px" }}>
                    <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
                        <span style={{ fontSize: "1.1rem", fontWeight: "700" }}>🔀 Transferir Itens</span>
                        <button
                            type="button"
                            onClick={onClose}
                            disabled={isTransferring}
                            aria-label="Fechar"
                            style={{ background: "none", border: "none", cursor: isTransferring ? "not-allowed" : "pointer", color: "#94a3b8", fontSize: "1.2rem", padding: "4px" }}
                        >
                            ✕
                        </button>
                    </div>

                    <div role="tablist" style={{ display: "flex", background: "#0f172a", padding: "4px", borderRadius: "8px", border: "1px solid #334155" }}>
                        <button
                            type="button"
                            role="tab"
                            aria-selected={transferType === "table"}
                            disabled={isTransferring}
                            onClick={() => handleTypeChange("table")}
                            style={{
                                flex: 1,
                                padding: "8px",
                                borderRadius: "6px",
                                border: "none",
                                background: transferType === "table" ? "#3b82f6" : "transparent",
                                color: "#fff",
                                fontWeight: "600",
                                fontSize: "0.85rem",
                                cursor: isTransferring ? "not-allowed" : "pointer",
                                transition: "background 0.2s"
                            }}
                        >
                            🍽️ Mesas
                        </button>
                        <button
                            type="button"
                            role="tab"
                            aria-selected={transferType === "comanda"}
                            disabled={isTransferring}
                            onClick={() => handleTypeChange("comanda")}
                            style={{
                                flex: 1,
                                padding: "8px",
                                borderRadius: "6px",
                                border: "none",
                                background: transferType === "comanda" ? "#3b82f6" : "transparent",
                                color: "#fff",
                                fontWeight: "600",
                                fontSize: "0.85rem",
                                cursor: isTransferring ? "not-allowed" : "pointer",
                                transition: "background 0.2s"
                            }}
                        >
                            📋 Comandas
                        </button>
                    </div>
                </div>

                <form onSubmit={handleExecuteTransfer} style={{ display: "flex", flexDirection: "column", gap: "14px" }}>

                    {/* Origem */}
                    <div style={{ display: "flex", flexDirection: "column", gap: "6px" }}>
                        <label style={{ fontSize: "0.80rem", fontWeight: "600", color: "#94a3b8" }}>
                            {transferType === "table" ? "Mesa de Origem (Com pedido)" : "Comanda de Origem (Com pedido)"}
                        </label>
                        <select
                            value={sourceId}
                            disabled={isTransferring}
                            onChange={(e) => {
                                setSourceId(e.target.value);
                                setSelectedItemIds([]);
                            }}
                            style={{ padding: "10px", borderRadius: "8px", border: "1px solid #475569", backgroundColor: "#0f172a", color: "#f8fafc", fontSize: "0.9rem" }}
                            required
                        >
                            <option value="">Selecione a origem...</option>
                            {transferType === "table" ? (
                                myTables.filter(t => effectiveOrdersByTableId.has(t.id)).map(t => (
                                    <option key={t.id} value={t.id}>Mesa {t.number}</option>
                                ))
                            ) : (
                                comandas.filter(c => comandaOrders.some(o => o.comandaId === c.id)).map(c => (
                                    <option key={c.id} value={c.id}>Comanda {c.code}</option>
                                ))
                            )}
                        </select>
                    </div>

                    {/* Seletor de Modo e Atalho de Seleção Rápida */}
                    {sourceOrder && (
                        <div style={{ display: "flex", flexDirection: "column", gap: "8px", background: "rgba(255,255,255,0.03)", padding: "10px 12px", borderRadius: "8px", border: "1px solid #334155" }}>
                            <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
                                <span style={{ fontSize: "0.80rem", fontWeight: "600", color: "#94a3b8" }}>Modo:</span>
                                <div style={{ display: "flex", gap: "6px" }}>
                                    <button
                                        type="button"
                                        disabled={isTransferring}
                                        onClick={() => { setTransferMode("single"); setSelectedItemIds([]); }}
                                        style={{ padding: "4px 10px", fontSize: "0.75rem", borderRadius: "6px", border: "1px solid #475569", background: transferMode === "single" ? "#3b82f6" : "transparent", color: "#fff", cursor: isTransferring ? "not-allowed" : "pointer" }}
                                    >
                                        Item Único
                                    </button>
                                    <button
                                        type="button"
                                        disabled={isTransferring}
                                        onClick={() => setTransferMode("batch")}
                                        style={{ padding: "4px 10px", fontSize: "0.75rem", borderRadius: "6px", border: "1px solid #475569", background: transferMode === "batch" ? "#3b82f6" : "transparent", color: "#fff", cursor: isTransferring ? "not-allowed" : "pointer" }}
                                    >
                                        Múltiplos / Todos
                                    </button>
                                </div>
                            </div>

                            {transferMode === "batch" && (
                                <button
                                    type="button"
                                    disabled={isTransferring}
                                    onClick={handleSelectAll}
                                    style={{
                                        marginTop: "4px",
                                        padding: "6px",
                                        borderRadius: "6px",
                                        border: "1px dashed #60a5fa",
                                        background: "rgba(96, 165, 250, 0.1)",
                                        color: "#60a5fa",
                                        fontSize: "0.78rem",
                                        fontWeight: "600",
                                        cursor: "pointer",
                                        textAlign: "center"
                                    }}
                                >
                                    {selectedItemIds.length === availableItems.length ? "☑ Desmarcar Todos" : "✅ Selecionar Todos Automaticamente"}
                                </button>
                            )}
                        </div>
                    )}

                    {/* Lista de Itens */}
                    {sourceId && (
                        <div style={{ display: "flex", flexDirection: "column", gap: "6px" }}>
                            <label style={{ fontSize: "0.80rem", fontWeight: "600", color: "#94a3b8" }}>
                                Itens Disponíveis ({availableItems.length})
                            </label>

                            <input
                                type="text"
                                placeholder="🔍 Buscar item..."
                                value={searchTerm}
                                disabled={isTransferring}
                                onChange={(e) => setSearchTerm(e.target.value)}
                                style={{ padding: "8px 10px", borderRadius: "6px", border: "1px solid #475569", backgroundColor: "#0f172a", color: "#fff", fontSize: "0.85rem" }}
                            />

                            <div style={{ maxHeight: "130px", overflowY: "auto", border: "1px solid #334155", borderRadius: "8px", background: "#0f172a" }}>
                                {productQueries.isLoading ? (
                                    <div style={{ padding: "10px", textAlign: "center", color: "#94a3b8", fontSize: "0.85rem" }}>Carregando produtos...</div>
                                ) : availableItems.length === 0 ? (
                                    <div style={{ padding: "10px", textAlign: "center", color: "#94a3b8", fontSize: "0.85rem" }}>Nenhum item ativo encontrado.</div>
                                ) : (
                                    availableItems.map((item: any) => {
                                        const pId = item.productId || item.ProductId;
                                        const productName = productsMap.get(pId) || `Produto #${pId}`;
                                        const isSelected = selectedItemIds.includes(item.id);

                                        return (
                                            <div
                                                key={item.id}
                                                onClick={() => !isTransferring && handleToggleItem(item.id)}
                                                style={{
                                                    display: "flex",
                                                    alignItems: "center",
                                                    gap: "12px",
                                                    padding: "10px 12px",
                                                    borderBottom: "1px solid rgba(255,255,255,0.05)",
                                                    backgroundColor: isSelected ? "rgba(59, 130, 246, 0.25)" : "transparent",
                                                    cursor: isTransferring ? "not-allowed" : "pointer"
                                                }}
                                            >
                                                {transferMode === "batch" && (
                                                    <input
                                                        type="checkbox"
                                                        checked={isSelected}
                                                        disabled={isTransferring}
                                                        onChange={() => { }}
                                                        style={{
                                                            cursor: "pointer",
                                                            width: "16px",
                                                            height: "16px",
                                                            accentColor: "#3b82f6",
                                                            flexShrink: 0
                                                        }}
                                                    />
                                                )}
                                                <span style={{ fontSize: "0.85rem", fontWeight: "500", lineHeight: "1.2" }}>
                                                    {productName} <span style={{ color: "#94a3b8" }}>(Qtd: {item.quantity})</span>
                                                </span>
                                            </div>
                                        );
                                    })
                                )}
                            </div>
                        </div>
                    )}

                    {/* Destino */}
                    <div style={{ display: "flex", flexDirection: "column", gap: "6px" }}>
                        <label style={{ fontSize: "0.80rem", fontWeight: "600", color: "#94a3b8" }}>
                            {transferType === "table" ? "Mesa de Destino" : "Comanda de Destino"}
                        </label>
                        <select
                            value={targetId}
                            disabled={isTransferring}
                            onChange={(e) => setTargetId(e.target.value)}
                            style={{ padding: "10px", borderRadius: "8px", border: "1px solid #475569", backgroundColor: "#0f172a", color: "#f8fafc", fontSize: "0.9rem" }}
                            required
                        >
                            <option value="">Selecione o destino...</option>
                            {transferType === "table" ? (
                                myTables.filter(t => t.id.toString() !== sourceId).map(t => (
                                    <option key={t.id} value={t.id}>
                                        Mesa {t.number} ({t.tableStatusId === 1 ? "Livre" : "Ocupada"})
                                    </option>
                                ))
                            ) : (
                                comandas.filter(c => c.id.toString() !== sourceId).map(c => (
                                    <option key={c.id} value={c.id}>
                                        Comanda {c.code} {comandaOrders.some(o => o.comandaId === c.id) ? "(Em uso)" : "(Livre)"}
                                    </option>
                                ))
                            )}
                        </select>
                    </div>

                    {/* Ações */}
                    <div style={{ display: "flex", gap: "10px", marginTop: "8px" }}>
                        <button
                            type="button"
                            onClick={onClose}
                            disabled={isTransferring}
                            style={{ flex: 1, padding: "10px", borderRadius: "8px", border: "1px solid #475569", background: "transparent", color: "inherit", cursor: isTransferring ? "not-allowed" : "pointer", fontWeight: "600" }}
                        >
                            Cancelar
                        </button>
                        <button
                            type="submit"
                            disabled={isTransferring || selectedItemIds.length === 0}
                            style={{
                                flex: 1,
                                padding: "10px",
                                borderRadius: "8px",
                                border: "none",
                                background: isTransferring ? "#2563eb" : "#3b82f6",
                                color: "#fff",
                                cursor: isTransferring ? "wait" : "pointer",
                                fontWeight: "600",
                                opacity: (isTransferring || selectedItemIds.length === 0) ? 0.6 : 1,
                                transition: "background-color 0.2s ease"
                            }}
                        >
                            {isTransferring ? "Transferindo..." : `Confirmar (${selectedItemIds.length})`}
                        </button>
                    </div>

                </form>
            </div>
        </div>
    );
}