import { useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { getStockByBranch, getStockLedger, registerStockMovement, setStockLimits } from "./api";
import { getMenu } from "../catalog/api";
import { useAuthStore } from "../../stores/authStore";
import { ApiError } from "../../lib/apiClient";
import {
    manualStockMovementTypes,
    stockMovementIsInflow,
    stockMovementTypeLabel,
} from "../../lib/types";
import type { StockItemResponse } from "../../lib/types";
import { InventoryOverlay } from "./InventoryOverlay";
import { QueryError } from "../../components/QueryError";

const parseNum = (raw: string): number | null => {
    if (raw.trim() === "") return null;
    const value = Number(raw.replace(",", "."));
    return Number.isFinite(value) ? value : null;
};

export function StockPage() {
    const queryClient = useQueryClient();
    const { branchId, companyId, employeeId } = useAuthStore();
    const [movementOpen, setMovementOpen] = useState(false);
    const [inventoryOpen, setInventoryOpen] = useState(false);
    const [ledgerItem, setLedgerItem] = useState<StockItemResponse | null>(null);
    const [limitsItem, setLimitsItem] = useState<StockItemResponse | null>(null);
    const [productId, setProductId] = useState("");
    const [typeId, setTypeId] = useState<number>(1);
    const [quantity, setQuantity] = useState("");
    const [unitCost, setUnitCost] = useState("");
    const [documentNumber, setDocumentNumber] = useState("");
    const [notes, setNotes] = useState("");
    const [minQ, setMinQ] = useState("");
    const [maxQ, setMaxQ] = useState("");
    const [error, setError] = useState<string | null>(null);

    const stockQuery = useQuery({
        queryKey: ["stock", branchId],
        queryFn: () => getStockByBranch(branchId),
    });

    const menuQuery = useQuery({
        queryKey: ["menu", companyId],
        queryFn: () => getMenu(companyId ?? 1),
    });

    const ledgerQuery = useQuery({
        queryKey: ["stock", "ledger", ledgerItem?.id],
        queryFn: () => getStockLedger(ledgerItem!.id),
        enabled: ledgerItem !== null,
    });

    const productName = useMemo(() => {
        const map = new Map<number, string>();
        for (const p of menuQuery.data ?? []) map.set(p.id, p.name);
        return map;
    }, [menuQuery.data]);

    const refresh = () => void queryClient.invalidateQueries({ queryKey: ["stock"] });

    const onApiError = (e: unknown) =>
        setError(e instanceof ApiError ? e.message : "Operação falhou.");

    const movementMutation = useMutation({
        mutationFn: () =>
            registerStockMovement({
                branchId,
                productId: Number(productId),
                stockMovementTypeId: typeId,
                employeeId: employeeId ?? 1,
                quantity: parseNum(quantity) ?? 0,
                unitCost: parseNum(unitCost),
                documentNumber: documentNumber.trim() === "" ? null : documentNumber.trim(),
                notes: notes.trim() === "" ? null : notes.trim(),
            }),
        onSuccess: () => {
            setError(null);
            setMovementOpen(false);
            setQuantity(""); setUnitCost(""); setDocumentNumber(""); setNotes("");
            refresh();
        },
        onError: onApiError,
    });

    const limitsMutation = useMutation({
        mutationFn: () => setStockLimits(limitsItem!.id, parseNum(minQ) ?? 0, parseNum(maxQ)),
        onSuccess: () => {
            setError(null);
            setLimitsItem(null);
            refresh();
        },
        onError: onApiError,
    });

    return (
        <main style={{ padding: 22, maxWidth: 1100, margin: "0 auto", position: "relative" }}>
            <div className="rise" style={{ display: "flex", alignItems: "baseline", gap: 14, marginBottom: 16 }}>
                <h2 className="display" style={{ fontSize: "1.7rem" }}>Estoque</h2>
                <span style={{ color: "var(--ink-faint)", fontSize: "0.9rem" }}>
                    saldo por produto · linhas em vermelho estão abaixo do mínimo
                </span>
                <span style={{ flex: 1 }} />
                <button className="btn-ghost" onClick={() => setInventoryOpen(true)}>
                    Inventário
                </button>
                <button className="btn-primary" onClick={() => { setError(null); setMovementOpen(true); }}>
                    + Lançar movimento
                </button>
            </div>

            {error && !movementOpen && limitsItem === null && <p className="error-text">{error}</p>}
            {stockQuery.isError && <QueryError error={stockQuery.error} what="o estoque" />}
            {menuQuery.isError && <QueryError error={menuQuery.error} what="os produtos" />}

            <div className="ticket rise rise-1">
                {(stockQuery.data ?? []).map((item) => (
                    <div className="ticket-row" key={item.id}>
                        <div style={{ display: "grid", gap: 2 }}>
                            <span style={{ color: item.isBelowMinimum ? "var(--danger)" : "var(--ink)" }}>
                                {productName.get(item.productId) ?? `Produto ${item.productId}`}
                            </span>
                            <span style={{ fontSize: "0.8rem", color: "var(--ink-faint)" }}>
                                mínimo {item.minimumQuantity}{item.maximumQuantity !== null ? ` · máximo ${item.maximumQuantity}` : ""}
                            </span>
                        </div>
                        <div style={{ display: "flex", gap: 8, alignItems: "center" }}>
                            <span
                                className="mono-num display"
                                style={{ fontSize: "1.5rem", color: item.isBelowMinimum ? "var(--danger)" : "var(--amber)" }}
                            >
                                {item.currentQuantity}
                            </span>
                            <button
                                className="btn-ghost"
                                style={{ minHeight: 38, padding: "0 12px", fontSize: "0.85rem" }}
                                onClick={() => setLedgerItem(item)}
                            >
                                Extrato
                            </button>
                            <button
                                className="btn-ghost"
                                style={{ minHeight: 38, padding: "0 12px", fontSize: "0.85rem" }}
                                onClick={() => {
                                    setError(null);
                                    setLimitsItem(item);
                                    setMinQ(String(item.minimumQuantity));
                                    setMaxQ(item.maximumQuantity === null ? "" : String(item.maximumQuantity));
                                }}
                            >
                                Limites
                            </button>
                        </div>
                    </div>
                ))}
                {stockQuery.data?.length === 0 && (
                    <div className="ticket-row" style={{ color: "var(--ink-faint)" }}>
                        Nenhum item de estoque — lance uma entrada para começar.
                    </div>
                )}
            </div>

            {inventoryOpen && (
                <InventoryOverlay
                    items={stockQuery.data ?? []}
                    productName={productName}
                    onClose={() => setInventoryOpen(false)}
                    onDone={refresh}
                />
            )}

            {movementOpen && (
                <div style={{
                    position: "fixed", top: 0, left: 0, right: 0, bottom: 0,
                    background: "rgba(0,0,0,0.6)", display: "flex", alignItems: "center", justifyContent: "center", zIndex: 1000
                }}>
                    <div style={{ background: "#18181b", padding: 24, borderRadius: 8, width: 480, maxWidth: "90%", display: "grid", gap: 16, border: "1px solid #27272a" }}>
                        <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
                            <h3 style={{ margin: 0, color: "#fff" }}>Lançar movimento</h3>
                            <button onClick={() => setMovementOpen(false)} style={{ background: "transparent", border: "none", color: "#fff", cursor: "pointer", fontSize: "1.2rem" }}>✕</button>
                        </div>

                        <label style={{ display: "grid", gap: 4 }}>
                            <span style={{ color: "var(--ink-dim)", fontSize: "0.85rem" }}>Produto</span>
                            <select value={productId} onChange={(e) => setProductId(e.target.value)} style={{ padding: "8px", borderRadius: "4px", border: "1px solid #3f3f46", background: "#27272a", color: "#fff" }}>
                                <option value="">Selecione…</option>
                                {(menuQuery.data ?? []).map((p) => (
                                    <option key={p.id} value={p.id}>{p.name}</option>
                                ))}
                            </select>
                        </label>

                        <div style={{ display: "grid", gap: 8, gridTemplateColumns: "1fr 1fr" }}>
                            <label style={{ display: "grid", gap: 4 }}>
                                <span style={{ color: "var(--ink-dim)", fontSize: "0.85rem" }}>Tipo</span>
                                <select value={typeId} onChange={(e) => setTypeId(Number(e.target.value))} style={{ padding: "8px", borderRadius: "4px", border: "1px solid #3f3f46", background: "#27272a", color: "#fff" }}>
                                    {manualStockMovementTypes.map((id) => (
                                        <option key={id} value={id}>
                                            {stockMovementTypeLabel[id]} {stockMovementIsInflow[id] ? "(+)" : "(−)"}
                                        </option>
                                    ))}
                                </select>
                            </label>
                            <label style={{ display: "grid", gap: 4 }}>
                                <span style={{ color: "var(--ink-dim)", fontSize: "0.85rem" }}>Quantidade</span>
                                <input
                                    inputMode="decimal"
                                    value={quantity}
                                    onChange={(e) => setQuantity(e.target.value)}
                                    autoFocus
                                    style={{ padding: "8px", borderRadius: "4px", border: "1px solid #3f3f46", background: "#27272a", color: "#fff" }}
                                />
                            </label>
                        </div>

                        <div style={{ display: "grid", gap: 8, gridTemplateColumns: "1fr 1fr" }}>
                            <label style={{ display: "grid", gap: 4 }}>
                                <span style={{ color: "var(--ink-dim)", fontSize: "0.85rem" }}>Custo unit. (R$)</span>
                                <input
                                    inputMode="decimal"
                                    value={unitCost}
                                    onChange={(e) => setUnitCost(e.target.value)}
                                    style={{ padding: "8px", borderRadius: "4px", border: "1px solid #3f3f46", background: "#27272a", color: "#fff" }}
                                />
                            </label>
                            <label style={{ display: "grid", gap: 4 }}>
                                <span style={{ color: "var(--ink-dim)", fontSize: "0.85rem" }}>Documento (NF)</span>
                                <input
                                    value={documentNumber}
                                    onChange={(e) => setDocumentNumber(e.target.value)}
                                    style={{ padding: "8px", borderRadius: "4px", border: "1px solid #3f3f46", background: "#27272a", color: "#fff" }}
                                />
                            </label>
                        </div>

                        <input
                            placeholder="Observações"
                            value={notes}
                            onChange={(e) => setNotes(e.target.value)}
                            style={{ padding: "8px", borderRadius: "4px", border: "1px solid #3f3f46", background: "#27272a", color: "#fff" }}
                        />

                        {error && <p className="error-text">{error}</p>}

                        <button
                            className="btn-primary"
                            disabled={productId === "" || (parseNum(quantity) ?? 0) <= 0 || movementMutation.isPending}
                            onClick={() => movementMutation.mutate()}
                        >
                            Registrar
                        </button>
                    </div>
                </div>
            )}

            {ledgerItem !== null && (
                <div style={{
                    position: "fixed", top: 0, left: 0, right: 0, bottom: 0,
                    background: "rgba(0,0,0,0.6)", display: "flex", alignItems: "center", justifyContent: "center", zIndex: 1000
                }}>
                    <div style={{ background: "#18181b", padding: 24, borderRadius: 8, width: 600, maxWidth: "90%", display: "grid", gap: 16, border: "1px solid #27272a", maxHeight: "85vh", overflowY: "auto" }}>
                        <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
                            <h3 style={{ margin: 0, color: "#fff" }}>
                                Extrato — {productName.get(ledgerItem.productId) ?? `produto ${ledgerItem.productId}`}
                            </h3>
                            <button onClick={() => setLedgerItem(null)} style={{ background: "transparent", border: "none", color: "#fff", cursor: "pointer", fontSize: "1.2rem" }}>✕</button>
                        </div>

                        <div className="ticket" style={{ display: "grid", gap: 8 }}>
                            {(ledgerQuery.data ?? []).map((movement) => {
                                const inflow = stockMovementIsInflow[movement.stockMovementTypeId];
                                return (
                                    <div className="ticket-row" key={movement.id}>
                                        <div style={{ display: "grid", gap: 2 }}>
                                            <span>{stockMovementTypeLabel[movement.stockMovementTypeId]}</span>
                                            <span style={{ fontSize: "0.8rem", color: "var(--ink-faint)" }}>
                                                {new Date(movement.movedAt).toLocaleString("pt-BR")}
                                                {movement.documentNumber ? ` · ${movement.documentNumber}` : ""}
                                                {movement.notes ? ` · ${movement.notes}` : ""}
                                            </span>
                                        </div>
                                        <span className="mono-num" style={{ color: inflow ? "var(--ok)" : "var(--danger)" }}>
                                            {inflow ? "+" : "−"}{movement.quantity}
                                        </span>
                                    </div>
                                );
                            })}
                            {ledgerQuery.data?.length === 0 && (
                                <div className="ticket-row" style={{ color: "var(--ink-faint)" }}>Sem movimentos.</div>
                            )}
                        </div>
                    </div>
                </div>
            )}

            {limitsItem !== null && (
                <div style={{
                    position: "fixed", top: 0, left: 0, right: 0, bottom: 0,
                    background: "rgba(0,0,0,0.6)", display: "flex", alignItems: "center", justifyContent: "center", zIndex: 1000
                }}>
                    <div style={{ background: "#18181b", padding: 24, borderRadius: 8, width: 400, maxWidth: "90%", display: "grid", gap: 16, border: "1px solid #27272a" }}>
                        <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
                            <h3 style={{ margin: 0, color: "#fff" }}>Limites de estoque</h3>
                            <button onClick={() => setLimitsItem(null)} style={{ background: "transparent", border: "none", color: "#fff", cursor: "pointer", fontSize: "1.2rem" }}>✕</button>
                        </div>

                        <label style={{ display: "grid", gap: 4 }}>
                            <span style={{ color: "var(--ink-dim)", fontSize: "0.85rem" }}>Quantidade mínima</span>
                            <input
                                inputMode="decimal"
                                value={minQ}
                                onChange={(e) => setMinQ(e.target.value)}
                                autoFocus
                                style={{ padding: "8px", borderRadius: "4px", border: "1px solid #3f3f46", background: "#27272a", color: "#fff" }}
                            />
                        </label>
                        <label style={{ display: "grid", gap: 4 }}>
                            <span style={{ color: "var(--ink-dim)", fontSize: "0.85rem" }}>Quantidade máxima (opcional)</span>
                            <input
                                inputMode="decimal"
                                value={maxQ}
                                onChange={(e) => setMaxQ(e.target.value)}
                                style={{ padding: "8px", borderRadius: "4px", border: "1px solid #3f3f46", background: "#27272a", color: "#fff" }}
                            />
                        </label>

                        {error && <p className="error-text">{error}</p>}

                        <button className="btn-primary" disabled={limitsMutation.isPending} onClick={() => limitsMutation.mutate()}>
                            Salvar
                        </button>
                    </div>
                </div>
            )}
        </main>
    );
}