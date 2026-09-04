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
import { Modal } from "../../ui/Modal";
import { Button } from "../../ui/Button";
import { TextField, SelectField } from "../../ui/Field";
import { useToast } from "../../ui/Toast";
import { EmptyState } from "../../ui/EmptyState";
import { SkeletonList } from "../../ui/Skeleton";

const parseNum = (raw: string): number | null => {
    if (raw.trim() === "") return null;
    const value = Number(raw.replace(",", "."));
    return Number.isFinite(value) ? value : null;
};

export function StockPage() {
    const queryClient = useQueryClient();
    const toast = useToast();
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
            toast.success("Movimento registrado.");
        },
        onError: onApiError,
    });

    const limitsMutation = useMutation({
        mutationFn: () => setStockLimits(limitsItem!.id, parseNum(minQ) ?? 0, parseNum(maxQ)),
        onSuccess: () => {
            setError(null);
            setLimitsItem(null);
            refresh();
            toast.success("Limites atualizados.");
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
                <button type="button" data-testid="btn-open-inventory" className="btn-ghost" onClick={() => setInventoryOpen(true)}>
                    Inventário
                </button>
                <button type="button" data-testid="btn-open-movement" className="btn-primary" onClick={() => { setError(null); setMovementOpen(true); }}>
                    + Lançar movimento
                </button>
            </div>

            {error && !movementOpen && limitsItem === null && <p className="error-text" data-testid="stock-error">{error}</p>}
            {stockQuery.isError && <QueryError error={stockQuery.error} what="o estoque" />}
            {menuQuery.isError && <QueryError error={menuQuery.error} what="os produtos" />}

            {stockQuery.isLoading && <SkeletonList rows={6} rowHeight={58} />}

            {!stockQuery.isLoading && stockQuery.data?.length === 0 && (
                <EmptyState
                    icon="📦"
                    title="Nenhum item de estoque"
                    description="Lance uma entrada (compra, ajuste ou inventário) para começar a controlar o saldo."
                    action={
                        <button type="button" data-testid="btn-empty-movement" className="btn-primary" onClick={() => { setError(null); setMovementOpen(true); }}>
                            + Lançar movimento
                        </button>
                    }
                />
            )}

            {!stockQuery.isLoading && (stockQuery.data?.length ?? 0) > 0 && (
                <div className="ticket rise rise-1" data-testid="stock-list">
                    {(stockQuery.data ?? []).map((item) => (
                        <div className="ticket-row" key={item.id} data-testid={`stock-item-${item.productId}`}>
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
                                    data-testid={`stock-qty-${item.productId}`}
                                >
                                    {item.currentQuantity}
                                </span>
                                <button
                                    type="button"
                                    data-testid={`btn-ledger-${item.productId}`}
                                    className="btn-ghost"
                                    style={{ minHeight: 44, padding: "0 12px", fontSize: "0.85rem" }}
                                    onClick={() => setLedgerItem(item)}
                                >
                                    Extrato
                                </button>
                                <button
                                    type="button"
                                    data-testid={`btn-limits-${item.productId}`}
                                    className="btn-ghost"
                                    style={{ minHeight: 44, padding: "0 12px", fontSize: "0.85rem" }}
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
                </div>
            )}

            {inventoryOpen && (
                <InventoryOverlay
                    items={stockQuery.data ?? []}
                    productName={productName}
                    onClose={() => setInventoryOpen(false)}
                    onDone={refresh}
                />
            )}

            {movementOpen && (
                <Modal title="Lançar movimento" onClose={() => setMovementOpen(false)} variant="center" wide>
                    <SelectField data-testid="select-movement-product" label="Produto" value={productId} onChange={(e) => setProductId(e.target.value)} autoFocus>
                        <option value="">Selecione…</option>
                        {(menuQuery.data ?? []).map((p) => (
                            <option key={p.id} value={p.id}>{p.name}</option>
                        ))}
                    </SelectField>

                    <div className="ui-row ui-row-wrap">
                        <div style={{ flex: 1, minWidth: 200 }}>
                            <SelectField data-testid="select-movement-type" label="Tipo" value={typeId} onChange={(e) => setTypeId(Number(e.target.value))}>
                                {manualStockMovementTypes.map((id) => (
                                    <option key={id} value={id}>
                                        {stockMovementTypeLabel[id]} {stockMovementIsInflow[id] ? "(+)" : "(−)"}
                                    </option>
                                ))}
                            </SelectField>
                        </div>
                        <div style={{ flex: 1, minWidth: 140 }}>
                            <TextField
                                data-testid="input-movement-quantity"
                                label="Quantidade"
                                inputMode="decimal"
                                value={quantity}
                                onChange={(e) => setQuantity(e.target.value)}
                            />
                        </div>
                    </div>

                    <div className="ui-row ui-row-wrap">
                        <div style={{ flex: 1, minWidth: 160 }}>
                            <TextField
                                data-testid="input-movement-cost"
                                label="Custo unit. (R$)"
                                inputMode="decimal"
                                value={unitCost}
                                onChange={(e) => setUnitCost(e.target.value)}
                            />
                        </div>
                        <div style={{ flex: 1, minWidth: 160 }}>
                            <TextField
                                data-testid="input-movement-doc"
                                label="Documento (NF)"
                                value={documentNumber}
                                onChange={(e) => setDocumentNumber(e.target.value)}
                            />
                        </div>
                    </div>

                    <TextField data-testid="input-movement-notes" label="Observações" value={notes} onChange={(e) => setNotes(e.target.value)} />

                    {error && <p className="error-text" data-testid="movement-error">{error}</p>}

                    <Button
                        variant="primary"
                        block
                        loading={movementMutation.isPending}
                        disabled={productId === "" || (parseNum(quantity) ?? 0) <= 0}
                        onClick={() => movementMutation.mutate()}
                        data-testid="btn-submit-movement"
                    >
                        Registrar
                    </Button>
                </Modal>
            )}

            {ledgerItem !== null && (
                <Modal
                    title={`Extrato — ${productName.get(ledgerItem.productId) ?? `produto ${ledgerItem.productId}`}`}
                    onClose={() => setLedgerItem(null)}
                    variant="center"
                    wide
                >
                    {ledgerQuery.isLoading && <SkeletonList rows={4} rowHeight={48} />}
                    {!ledgerQuery.isLoading && (ledgerQuery.data?.length ?? 0) === 0 && (
                        <EmptyState icon="🧾" title="Sem movimentos" description="Este produto ainda não teve entradas ou saídas registradas." />
                    )}
                    {!ledgerQuery.isLoading && (ledgerQuery.data?.length ?? 0) > 0 && (
                        <div className="ticket" style={{ display: "grid", gap: 8 }} data-testid="ledger-list">
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
                        </div>
                    )}
                </Modal>
            )}

            {limitsItem !== null && (
                <Modal title="Limites de estoque" onClose={() => setLimitsItem(null)} variant="center">
                    <TextField
                        data-testid="input-limit-min"
                        label="Quantidade mínima"
                        inputMode="decimal"
                        value={minQ}
                        onChange={(e) => setMinQ(e.target.value)}
                        autoFocus
                    />
                    <TextField
                        data-testid="input-limit-max"
                        label="Quantidade máxima (opcional)"
                        inputMode="decimal"
                        value={maxQ}
                        onChange={(e) => setMaxQ(e.target.value)}
                    />

                    {error && <p className="error-text" data-testid="limits-error">{error}</p>}

                    <Button variant="primary" block loading={limitsMutation.isPending} onClick={() => limitsMutation.mutate()} data-testid="btn-submit-limits">
                        Salvar
                    </Button>
                </Modal>
            )}
        </main>
    );
}