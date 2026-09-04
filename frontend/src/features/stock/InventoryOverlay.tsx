import { useMemo, useState } from "react";
import { useMutation } from "@tanstack/react-query";
import Swal from "sweetalert2";
import { adjustInventory, type InventoryAdjustment } from "./api";
import { useAuthStore } from "../../stores/authStore";
import { ApiError } from "../../lib/apiClient";
import type { StockItemResponse } from "../../lib/types";
import { Overlay } from "../orders/Overlay";

interface Props {
    items: StockItemResponse[];
    productName: Map<number, string>;
    onClose: () => void;
    onDone: () => void;
}

const parseNum = (raw: string): number | null => {
    if (raw.trim() === "") return null;
    const value = Number(raw.replace(",", "."));
    return Number.isFinite(value) && value >= 0 ? value : null;
};

export function InventoryOverlay({ items, productName, onClose, onDone }: Props) {
    const { branchId, employeeId } = useAuthStore();
    const [counts, setCounts] = useState<Record<number, string>>({});
    const [result, setResult] = useState<InventoryAdjustment[] | null>(null);

    const filled = useMemo(
        () => items.filter((item) => parseNum(counts[item.productId] ?? "") !== null),
        [items, counts],
    );

    const mutation = useMutation({
        mutationFn: () =>
            adjustInventory(
                branchId ?? 1,
                employeeId ?? 1,
                filled.map((item) => ({
                    productId: item.productId,
                    countedQuantity: parseNum(counts[item.productId] ?? "")!,
                })),
            ),
        onSuccess: (adjustments) => {
            setResult(adjustments);
            onDone();
            Swal.fire({
                title: "Inventário Registrado",
                text: "A contagem física foi processada com sucesso.",
                icon: "success",
                timer: 1500,
                showConfirmButton: false,
            });
        },
        onError: (e) => {
            const message = e instanceof ApiError ? e.message : "Falha ao registrar o inventário.";
            Swal.fire({
                title: "Erro",
                text: message,
                icon: "error",
                confirmButtonText: "Ok",
            });
        },
    });

    if (result !== null)
        return (
            <Overlay title="Inventário registrado" onClose={onClose}>
                {result.length === 0 ? (
                    <p style={{ color: "var(--ok)" }} data-testid="inventory-success-match">
                        Todas as contagens bateram com o sistema — nenhum ajuste necessário.
                    </p>
                ) : (
                    <div className="ticket" data-testid="inventory-results-list">
                        {result.map((adjustment) => (
                            <div className="ticket-row" key={adjustment.productId}>
                                <div style={{ display: "grid", gap: 2 }}>
                                    <span>{productName.get(adjustment.productId) ?? `Produto ${adjustment.productId}`}</span>
                                    <span style={{ fontSize: "0.8rem", color: "var(--ink-faint)" }}>
                                        sistema {adjustment.previousQuantity} → contado {adjustment.countedQuantity}
                                    </span>
                                </div>
                                <span
                                    className="mono-num"
                                    style={{ color: adjustment.difference > 0 ? "var(--ok)" : "var(--danger)" }}
                                >
                                    {adjustment.difference > 0 ? "+" : ""}{adjustment.difference}
                                </span>
                            </div>
                        ))}
                    </div>
                )}
                <button type="button" data-testid="btn-close-inventory" className="btn-primary" onClick={onClose}>Fechar</button>
            </Overlay>
        );

    return (
        <Overlay title="Inventário — contagem física" onClose={onClose} wide>
            <p style={{ color: "var(--ink-dim)", fontSize: "0.9rem", margin: 0 }}>
                Digite a quantidade CONTADA de cada produto. Itens em branco não são ajustados.
                As diferenças viram ajustes de entrada/saída no extrato.
            </p>

            <div className="ticket" data-testid="inventory-items-list">
                {items.map((item) => (
                    <div className="ticket-row" key={item.id}>
                        <div style={{ display: "grid", gap: 2 }}>
                            <span>{productName.get(item.productId) ?? `Produto ${item.productId}`}</span>
                            <span className="mono-num" style={{ fontSize: "0.8rem", color: "var(--ink-faint)" }}>
                                sistema: {item.currentQuantity}
                            </span>
                        </div>
                        <input
                            data-testid={`input-count-${item.productId}`}
                            inputMode="decimal"
                            placeholder="contagem"
                            style={{ width: 110, textAlign: "right" }}
                            value={counts[item.productId] ?? ""}
                            onChange={(e) =>
                                setCounts((current) => ({ ...current, [item.productId]: e.target.value }))
                            }
                        />
                    </div>
                ))}
                {items.length === 0 && (
                    <div className="ticket-row" style={{ color: "var(--ink-faint)" }}>
                        Nenhum item de estoque para contar.
                    </div>
                )}
            </div>

            <button
                type="button"
                data-testid="btn-submit-inventory"
                className="btn-primary"
                disabled={filled.length === 0 || mutation.isPending}
                onClick={() => mutation.mutate()}
            >
                {mutation.isPending ? "Registrando…" : `Registrar inventário (${filled.length} itens contados)`}
            </button>
        </Overlay>
    );
}