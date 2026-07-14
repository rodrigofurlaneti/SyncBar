import { useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useDialog } from "../../ui/Dialog";
import {
  createSupplier,
  deactivateSupplier,
  getPurchasesByBranch,
  getSuppliersByCompany,
  registerPurchase,
  type PurchaseItemPayload,
} from "./api";
import { getMenu } from "../catalog/api";
import { useAuthStore } from "../../stores/authStore";
import { ApiError } from "../../lib/apiClient";
import { formatBRL } from "../../lib/types";
import { QueryError } from "../../components/QueryError";
import { Overlay } from "../orders/Overlay";

export function PurchasingPage() {
  const queryClient = useQueryClient();
  const dialog = useDialog();
  const { branchId, companyId, employeeId } = useAuthStore();
  const [creatingSupplier, setCreatingSupplier] = useState(false);
  const [registeringPurchase, setRegisteringPurchase] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const [supplierForm, setSupplierForm] = useState({
    legalName: "",
    tradeName: "",
    cnpj: "",
    email: "",
    phone: "",
  });

  const [supplierId, setSupplierId] = useState("");
  const [documentNumber, setDocumentNumber] = useState("");
  const [purchasedAt, setPurchasedAt] = useState(() => new Date().toISOString().slice(0, 10));
  const [items, setItems] = useState<PurchaseItemPayload[]>([{ productId: 0, quantity: 1, unitCost: 0 }]);

  const suppliersQuery = useQuery({
    queryKey: ["suppliers", companyId],
    queryFn: () => getSuppliersByCompany(companyId ?? 1),
  });

  const purchasesQuery = useQuery({
    queryKey: ["purchases", branchId],
    queryFn: () => getPurchasesByBranch(branchId),
  });

  const menuQuery = useQuery({
    queryKey: ["menu", companyId],
    queryFn: () => getMenu(companyId ?? 1),
  });

  const productName = useMemo(() => {
    const map = new Map<number, string>();
    for (const p of menuQuery.data ?? []) map.set(p.id, p.name);
    return map;
  }, [menuQuery.data]);

  const supplierName = useMemo(() => {
    const map = new Map<number, string>();
    for (const s of suppliersQuery.data ?? []) map.set(s.id, s.tradeName ?? s.legalName);
    return map;
  }, [suppliersQuery.data]);

  const refreshSuppliers = () => void queryClient.invalidateQueries({ queryKey: ["suppliers"] });
  const refreshPurchases = () => void queryClient.invalidateQueries({ queryKey: ["purchases"] });
  const onApiError = (e: unknown) => setError(e instanceof ApiError ? e.message : "Operação falhou.");

  const createSupplierMutation = useMutation({
    mutationFn: () =>
      createSupplier({
        companyId: companyId ?? 1,
        legalName: supplierForm.legalName.trim(),
        tradeName: supplierForm.tradeName.trim() === "" ? null : supplierForm.tradeName.trim(),
        cnpj: supplierForm.cnpj.trim() === "" ? null : supplierForm.cnpj.trim(),
        email: supplierForm.email.trim() === "" ? null : supplierForm.email.trim(),
        phone: supplierForm.phone.trim() === "" ? null : supplierForm.phone.trim(),
      }),
    onSuccess: () => {
      setError(null);
      setCreatingSupplier(false);
      setSupplierForm({ legalName: "", tradeName: "", cnpj: "", email: "", phone: "" });
      refreshSuppliers();
    },
    onError: onApiError,
  });

  const deactivateSupplierMutation = useMutation({
    mutationFn: (id: number) => deactivateSupplier(id),
    onSuccess: refreshSuppliers,
    onError: onApiError,
  });

  const validItems = items.filter((i) => i.productId > 0 && i.quantity > 0);
  const purchaseTotal = validItems.reduce((sum, i) => sum + i.quantity * i.unitCost, 0);

  const registerPurchaseMutation = useMutation({
    mutationFn: () =>
      registerPurchase({
        branchId,
        supplierId: Number(supplierId),
        employeeId: employeeId ?? 1,
        documentNumber: documentNumber.trim() === "" ? null : documentNumber.trim(),
        purchasedAt: new Date(purchasedAt).toISOString(),
        notes: null,
        items: validItems,
      }),
    onSuccess: () => {
      setError(null);
      setRegisteringPurchase(false);
      setSupplierId("");
      setDocumentNumber("");
      setItems([{ productId: 0, quantity: 1, unitCost: 0 }]);
      refreshPurchases();
    },
    onError: onApiError,
  });

  const setItem = (index: number, patch: Partial<PurchaseItemPayload>) =>
    setItems((current) => current.map((it, i) => (i === index ? { ...it, ...patch } : it)));

  return (
    <main style={{ padding: 22, maxWidth: 1100, margin: "0 auto" }}>
      <div className="rise" style={{ display: "flex", alignItems: "baseline", gap: 14, marginBottom: 6, flexWrap: "wrap" }}>
        <h2 className="display" style={{ fontSize: "1.7rem" }}>Fornecedores e Compras</h2>
        <span style={{ flex: 1 }} />
        <button className="btn-ghost" onClick={() => { setError(null); setCreatingSupplier(true); }}>
          + Fornecedor
        </button>
        <button className="btn-primary" onClick={() => { setError(null); setRegisteringPurchase(true); }}>
          + Registrar compra
        </button>
      </div>

      {suppliersQuery.isError && <QueryError error={suppliersQuery.error} what="os fornecedores" />}
      {purchasesQuery.isError && <QueryError error={purchasesQuery.error} what="as compras" />}
      {error && !creatingSupplier && !registeringPurchase && <p className="error-text">{error}</p>}

      <section className="rise rise-1" style={{ marginTop: 18 }}>
        <h3 className="display" style={{ fontSize: "1.15rem", marginBottom: 8 }}>Fornecedores</h3>
        <div style={{ display: "grid", gap: 8 }}>
          {(suppliersQuery.data ?? []).map((s) => (
            <div key={s.id} className="ticket-row">
              <div style={{ display: "grid", gap: 2 }}>
                <span>{s.tradeName ?? s.legalName}</span>
                <span style={{ fontSize: "0.8rem", color: "var(--ink-faint)" }}>
                  {s.cnpj ?? "sem CNPJ"} {s.phone ? `· ${s.phone}` : ""}
                </span>
              </div>
              {s.isActive && (
                <button
                  className="btn-danger"
                  style={{ minHeight: 36, padding: "0 10px", fontSize: "0.85rem" }}
                  onClick={async () => {
                    if (await dialog.confirm({ title: "Desativar fornecedor", message: `Desativar "${s.legalName}"?`, confirmLabel: "Desativar", danger: true }))
                      deactivateSupplierMutation.mutate(s.id);
                  }}
                >
                  Desativar
                </button>
              )}
            </div>
          ))}
          {(suppliersQuery.data ?? []).length === 0 && !suppliersQuery.isLoading && (
            <p style={{ color: "var(--ink-faint)" }}>Nenhum fornecedor cadastrado.</p>
          )}
        </div>
      </section>

      <section className="rise rise-2" style={{ marginTop: 26 }}>
        <h3 className="display" style={{ fontSize: "1.15rem", marginBottom: 8 }}>Compras registradas</h3>
        <div style={{ display: "grid", gap: 8 }}>
          {(purchasesQuery.data ?? []).map((p) => (
            <div key={p.id} className="ticket">
              <div className="ticket-head">
                <span>{supplierName.get(p.supplierId) ?? `Fornecedor ${p.supplierId}`}</span>
                <span className="mono-num">{formatBRL(p.totalAmount)}</span>
              </div>
              <div style={{ padding: "6px 14px", color: "var(--ink-faint)", fontSize: "0.85rem" }}>
                {new Date(p.purchasedAt).toLocaleDateString("pt-BR")} {p.documentNumber ? `· NF ${p.documentNumber}` : ""}
                {" · "}
                {p.items.map((it) => `${productName.get(it.productId) ?? it.productId} (${it.quantity})`).join(", ")}
              </div>
            </div>
          ))}
          {(purchasesQuery.data ?? []).length === 0 && !purchasesQuery.isLoading && (
            <p style={{ color: "var(--ink-faint)" }}>Nenhuma compra registrada.</p>
          )}
        </div>
      </section>

      {creatingSupplier && (
        <Overlay title="Novo fornecedor" onClose={() => setCreatingSupplier(false)}>
          <label style={{ display: "grid", gap: 4 }}>
            <span style={{ color: "var(--ink-dim)", fontSize: "0.85rem" }}>Razão social</span>
            <input value={supplierForm.legalName} onChange={(e) => setSupplierForm((f) => ({ ...f, legalName: e.target.value }))} />
          </label>
          <label style={{ display: "grid", gap: 4 }}>
            <span style={{ color: "var(--ink-dim)", fontSize: "0.85rem" }}>Nome fantasia</span>
            <input value={supplierForm.tradeName} onChange={(e) => setSupplierForm((f) => ({ ...f, tradeName: e.target.value }))} />
          </label>
          <div style={{ display: "grid", gap: 8, gridTemplateColumns: "1fr 1fr" }}>
            <label style={{ display: "grid", gap: 4 }}>
              <span style={{ color: "var(--ink-dim)", fontSize: "0.85rem" }}>CNPJ</span>
              <input value={supplierForm.cnpj} onChange={(e) => setSupplierForm((f) => ({ ...f, cnpj: e.target.value }))} maxLength={14} />
            </label>
            <label style={{ display: "grid", gap: 4 }}>
              <span style={{ color: "var(--ink-dim)", fontSize: "0.85rem" }}>Telefone</span>
              <input value={supplierForm.phone} onChange={(e) => setSupplierForm((f) => ({ ...f, phone: e.target.value }))} />
            </label>
          </div>
          <label style={{ display: "grid", gap: 4 }}>
            <span style={{ color: "var(--ink-dim)", fontSize: "0.85rem" }}>E-mail</span>
            <input value={supplierForm.email} onChange={(e) => setSupplierForm((f) => ({ ...f, email: e.target.value }))} />
          </label>
          {error && <p className="error-text">{error}</p>}
          <button
            className="btn-primary"
            disabled={supplierForm.legalName.trim() === "" || createSupplierMutation.isPending}
            onClick={() => createSupplierMutation.mutate()}
          >
            {createSupplierMutation.isPending ? "Criando…" : "Criar fornecedor"}
          </button>
        </Overlay>
      )}

      {registeringPurchase && (
        <Overlay title="Registrar compra" onClose={() => setRegisteringPurchase(false)} wide>
          <label style={{ display: "grid", gap: 4 }}>
            <span style={{ color: "var(--ink-dim)", fontSize: "0.85rem" }}>Fornecedor</span>
            <select value={supplierId} onChange={(e) => setSupplierId(e.target.value)}>
              <option value="">Selecione…</option>
              {(suppliersQuery.data ?? []).filter((s) => s.isActive).map((s) => (
                <option key={s.id} value={s.id}>{s.tradeName ?? s.legalName}</option>
              ))}
            </select>
          </label>
          <div style={{ display: "grid", gap: 8, gridTemplateColumns: "1fr 1fr" }}>
            <label style={{ display: "grid", gap: 4 }}>
              <span style={{ color: "var(--ink-dim)", fontSize: "0.85rem" }}>Nº da nota</span>
              <input value={documentNumber} onChange={(e) => setDocumentNumber(e.target.value)} />
            </label>
            <label style={{ display: "grid", gap: 4 }}>
              <span style={{ color: "var(--ink-dim)", fontSize: "0.85rem" }}>Data</span>
              <input type="date" value={purchasedAt} onChange={(e) => setPurchasedAt(e.target.value)} />
            </label>
          </div>

          <div style={{ display: "grid", gap: 8 }}>
            <span style={{ color: "var(--ink-dim)", fontSize: "0.85rem" }}>Itens</span>
            {items.map((item, index) => (
              <div key={index} style={{ display: "grid", gap: 8, gridTemplateColumns: "1.6fr 0.7fr 0.8fr auto" }}>
                <select value={item.productId} onChange={(e) => setItem(index, { productId: Number(e.target.value) })}>
                  <option value={0}>Produto…</option>
                  {(menuQuery.data ?? []).map((p) => (
                    <option key={p.id} value={p.id}>{p.name}</option>
                  ))}
                </select>
                <input placeholder="Qtd" inputMode="decimal" value={item.quantity} onChange={(e) => setItem(index, { quantity: Number(e.target.value) || 0 })} />
                <input placeholder="Custo unit." inputMode="decimal" value={item.unitCost} onChange={(e) => setItem(index, { unitCost: Number(e.target.value) || 0 })} />
                <button
                  className="btn-ghost"
                  aria-label="Remover item"
                  disabled={items.length === 1}
                  onClick={() => setItems((current) => current.filter((_, i) => i !== index))}
                >
                  ✕
                </button>
              </div>
            ))}
            <button className="btn-ghost" onClick={() => setItems((current) => [...current, { productId: 0, quantity: 1, unitCost: 0 }])}>
              + Adicionar item
            </button>
          </div>

          <div style={{ display: "flex", justifyContent: "space-between", color: "var(--ink-dim)" }}>
            <span>Total</span>
            <span className="mono-num">{formatBRL(purchaseTotal)}</span>
          </div>

          {error && <p className="error-text">{error}</p>}
          <button
            className="btn-primary"
            disabled={supplierId === "" || validItems.length === 0 || registerPurchaseMutation.isPending}
            onClick={() => registerPurchaseMutation.mutate()}
          >
            {registerPurchaseMutation.isPending ? "Registrando…" : "Registrar compra e dar entrada no estoque"}
          </button>
        </Overlay>
      )}
    </main>
  );
}
