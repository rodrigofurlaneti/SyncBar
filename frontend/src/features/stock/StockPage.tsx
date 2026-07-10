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
import { Overlay } from "../orders/Overlay";
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
    <main style={{ padding: 22, maxWidth: 1100, margin: "0 auto" }}>
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
        <Overlay title="Lançar movimento" onClose={() => setMovementOpen(false)}>
          <label style={{ display: "grid", gap: 4 }}>
            <span style={{ color: "var(--ink-dim)", fontSize: "0.85rem" }}>Produto</span>
            <select value={productId} onChange={(e) => setProductId(e.target.value)}>
              <option value="">Selecione…</option>
              {(menuQuery.data ?? []).map((p) => (
                <option key={p.id} value={p.id}>{p.name}</option>
              ))}
            </select>
          </label>
          <div style={{ display: "grid", gap: 8, gridTemplateColumns: "1fr 1fr" }}>
            <label style={{ display: "grid", gap: 4 }}>
              <span style={{ color: "var(--ink-dim)", fontSize: "0.85rem" }}>Tipo</span>
              <select value={typeId} onChange={(e) => setTypeId(Number(e.target.value))}>
                {manualStockMovementTypes.map((id) => (
                  <option key={id} value={id}>
                    {stockMovementTypeLabel[id]} {stockMovementIsInflow[id] ? "(+)" : "(−)"}
                  </option>
                ))}
              </select>
            </label>
            <label style={{ display: "grid", gap: 4 }}>
              <span style={{ color: "var(--ink-dim)", fontSize: "0.85rem" }}>Quantidade</span>
              <input inputMode="decimal" value={quantity} onChange={(e) => setQuantity(e.target.value)} />
            </label>
          </div>
          <div style={{ display: "grid", gap: 8, gridTemplateColumns: "1fr 1fr" }}>
            <label style={{ display: "grid", gap: 4 }}>
              <span style={{ color: "var(--ink-dim)", fontSize: "0.85rem" }}>Custo unit. (R$)</span>
              <input inputMode="decimal" value={unitCost} onChange={(e) => setUnitCost(e.target.value)} />
            </label>
            <label style={{ display: "grid", gap: 4 }}>
              <span style={{ color: "var(--ink-dim)", fontSize: "0.85rem" }}>Documento (NF)</span>
              <input value={documentNumber} onChange={(e) => setDocumentNumber(e.target.value)} />
            </label>
          </div>
          <input placeholder="Observações" value={notes} onChange={(e) => setNotes(e.target.value)} />
          {error && <p className="error-text">{error}</p>}
          <button
            className="btn-primary"
            disabled={productId === "" || (parseNum(quantity) ?? 0) <= 0 || movementMutation.isPending}
            onClick={() => movementMutation.mutate()}
          >
            Registrar
          </button>
        </Overlay>
      )}

      {ledgerItem !== null && (
        <Overlay
          title={`Extrato — ${productName.get(ledgerItem.productId) ?? `produto ${ledgerItem.productId}`}`}
          onClose={() => setLedgerItem(null)}
          wide
        >
          <div className="ticket">
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
        </Overlay>
      )}

      {limitsItem !== null && (
        <Overlay title="Limites de estoque" onClose={() => setLimitsItem(null)}>
          <label style={{ display: "grid", gap: 4 }}>
            <span style={{ color: "var(--ink-dim)", fontSize: "0.85rem" }}>Quantidade mínima</span>
            <input inputMode="decimal" value={minQ} onChange={(e) => setMinQ(e.target.value)} />
          </label>
          <label style={{ display: "grid", gap: 4 }}>
            <span style={{ color: "var(--ink-dim)", fontSize: "0.85rem" }}>Quantidade máxima (opcional)</span>
            <input inputMode="decimal" value={maxQ} onChange={(e) => setMaxQ(e.target.value)} />
          </label>
          {error && <p className="error-text">{error}</p>}
          <button className="btn-primary" disabled={limitsMutation.isPending} onClick={() => limitsMutation.mutate()}>
            Salvar
          </button>
        </Overlay>
      )}
    </main>
  );
}
