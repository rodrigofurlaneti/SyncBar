import { useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  createCategory,
  createProduct,
  deactivateProduct,
  getCategories,
  getMenu,
  updateProduct,
  type ProductPayload,
} from "./api";
import { useAuthStore } from "../../stores/authStore";
import { ApiError } from "../../lib/apiClient";
import { formatBRL, unitOfMeasureLabel } from "../../lib/types";
import type { MenuItemResponse } from "../../lib/types";
import { Overlay } from "../orders/Overlay";
import { QueryError } from "../../components/QueryError";

const emptyForm = {
  categoryId: "",
  unitOfMeasureId: "1",
  name: "",
  description: "",
  barcode: "",
  salePrice: "",
  costPrice: "",
  isStockControlled: true,
  preparationTimeMinutes: "",
};

type FormState = typeof emptyForm;

const parseNum = (raw: string): number | null => {
  if (raw.trim() === "") return null;
  const value = Number(raw.replace(",", "."));
  return Number.isFinite(value) ? value : null;
};

export function ProductsPage() {
  const queryClient = useQueryClient();
  const { companyId } = useAuthStore();
  const [editing, setEditing] = useState<MenuItemResponse | "new" | null>(null);
  const [form, setForm] = useState<FormState>(emptyForm);
  const [newCategory, setNewCategory] = useState("");
  const [error, setError] = useState<string | null>(null);

  const menuQuery = useQuery({
    queryKey: ["menu", companyId],
    queryFn: () => getMenu(companyId ?? 1),
  });

  const categoriesQuery = useQuery({
    queryKey: ["categories", companyId],
    queryFn: () => getCategories(companyId ?? 1),
  });

  const categoryName = useMemo(() => {
    const map = new Map<number, string>();
    for (const c of categoriesQuery.data ?? []) map.set(c.id, c.name);
    return map;
  }, [categoriesQuery.data]);

  const refresh = () => {
    void queryClient.invalidateQueries({ queryKey: ["menu"] });
    void queryClient.invalidateQueries({ queryKey: ["categories"] });
  };

  const openEditor = (product: MenuItemResponse | "new") => {
    setError(null);
    setEditing(product);
    if (product === "new") setForm({ ...emptyForm, categoryId: String(categoriesQuery.data?.[0]?.id ?? "") });
    else
      setForm({
        categoryId: String(product.categoryId),
        unitOfMeasureId: String(product.unitOfMeasureId),
        name: product.name,
        description: product.description ?? "",
        barcode: product.barcode ?? "",
        salePrice: String(product.salePrice),
        costPrice: product.costPrice === null ? "" : String(product.costPrice),
        isStockControlled: product.isStockControlled,
        preparationTimeMinutes: product.preparationTimeMinutes === null ? "" : String(product.preparationTimeMinutes),
      });
  };

  const buildPayload = (): ProductPayload => ({
    categoryId: Number(form.categoryId),
    unitOfMeasureId: Number(form.unitOfMeasureId),
    name: form.name.trim(),
    description: form.description.trim() === "" ? null : form.description.trim(),
    barcode: form.barcode.trim() === "" ? null : form.barcode.trim(),
    salePrice: parseNum(form.salePrice) ?? 0,
    costPrice: parseNum(form.costPrice),
    isStockControlled: form.isStockControlled,
    preparationTimeMinutes: form.preparationTimeMinutes.trim() === "" ? null : Number(form.preparationTimeMinutes),
  });

  const onApiError = (e: unknown) =>
    setError(e instanceof ApiError ? e.message : "Operação falhou.");

  const saveMutation = useMutation({
    mutationFn: () =>
      editing === "new"
        ? createProduct(companyId ?? 1, buildPayload()).then(() => undefined)
        : updateProduct((editing as MenuItemResponse).id, buildPayload()),
    onSuccess: () => {
      setEditing(null);
      refresh();
    },
    onError: onApiError,
  });

  const deactivateMutation = useMutation({
    mutationFn: (id: number) => deactivateProduct(id),
    onSuccess: refresh,
    onError: onApiError,
  });

  const categoryMutation = useMutation({
    mutationFn: () =>
      createCategory(companyId ?? 1, newCategory.trim(), (categoriesQuery.data?.length ?? 0) + 1),
    onSuccess: () => {
      setNewCategory("");
      refresh();
    },
    onError: onApiError,
  });

  return (
    <main style={{ padding: 22, maxWidth: 1100, margin: "0 auto" }}>
      <div className="rise" style={{ display: "flex", alignItems: "baseline", gap: 14, marginBottom: 16 }}>
        <h2 className="display" style={{ fontSize: "1.7rem" }}>Cardápio</h2>
        <span style={{ flex: 1 }} />
        <button className="btn-primary" onClick={() => openEditor("new")}>+ Novo produto</button>
      </div>

      <div className="rise rise-1" style={{ display: "flex", gap: 8, marginBottom: 18, maxWidth: 460 }}>
        <input
          placeholder="Nova categoria…"
          value={newCategory}
          onChange={(e) => setNewCategory(e.target.value)}
        />
        <button
          className="btn-ghost"
          disabled={newCategory.trim() === "" || categoryMutation.isPending}
          onClick={() => categoryMutation.mutate()}
        >
          Criar
        </button>
      </div>

      {error && !editing && <p className="error-text">{error}</p>}
      {menuQuery.isError && <QueryError error={menuQuery.error} what="o cardápio" />}
      {categoriesQuery.isError && <QueryError error={categoriesQuery.error} what="as categorias" />}

      <div className="ticket rise rise-2">
        {(menuQuery.data ?? []).map((product) => (
          <div className="ticket-row" key={product.id}>
            <div style={{ display: "grid", gap: 2 }}>
              <span>{product.name}</span>
              <span style={{ fontSize: "0.8rem", color: "var(--ink-faint)" }}>
                {categoryName.get(product.categoryId) ?? `Categoria ${product.categoryId}`}
                {product.description ? ` · ${product.description}` : ""}
                {product.isStockControlled ? " · controla estoque" : " · sem controle de estoque"}
              </span>
            </div>
            <div style={{ display: "flex", gap: 8, alignItems: "center" }}>
              <span className="mono-num" style={{ color: "var(--amber)" }}>
                {formatBRL(product.salePrice)}
              </span>
              <button
                className="btn-ghost"
                style={{ minHeight: 38, padding: "0 12px", fontSize: "0.85rem" }}
                onClick={() => openEditor(product)}
              >
                Editar
              </button>
              <button
                className="btn-danger"
                style={{ minHeight: 38, padding: "0 12px", fontSize: "0.85rem" }}
                onClick={() => {
                  if (window.confirm(`Desativar "${product.name}"?`)) deactivateMutation.mutate(product.id);
                }}
              >
                Desativar
              </button>
            </div>
          </div>
        ))}
        {menuQuery.data?.length === 0 && (
          <div className="ticket-row" style={{ color: "var(--ink-faint)" }}>Nenhum produto cadastrado.</div>
        )}
      </div>

      {editing !== null && (
        <Overlay title={editing === "new" ? "Novo produto" : "Editar produto"} onClose={() => setEditing(null)}>
          <label style={{ display: "grid", gap: 4 }}>
            <span style={{ color: "var(--ink-dim)", fontSize: "0.85rem" }}>Nome</span>
            <input value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} />
          </label>
          <div style={{ display: "grid", gap: 8, gridTemplateColumns: "1fr 1fr" }}>
            <label style={{ display: "grid", gap: 4 }}>
              <span style={{ color: "var(--ink-dim)", fontSize: "0.85rem" }}>Categoria</span>
              <select value={form.categoryId} onChange={(e) => setForm({ ...form, categoryId: e.target.value })}>
                <option value="">Selecione a categoria…</option>
                {(categoriesQuery.data ?? []).map((c) => (
                  <option key={c.id} value={c.id}>{c.name}</option>
                ))}
              </select>
            </label>
            <label style={{ display: "grid", gap: 4 }}>
              <span style={{ color: "var(--ink-dim)", fontSize: "0.85rem" }}>Unidade</span>
              <select
                value={form.unitOfMeasureId}
                onChange={(e) => setForm({ ...form, unitOfMeasureId: e.target.value })}
              >
                {Object.entries(unitOfMeasureLabel).map(([id, label]) => (
                  <option key={id} value={id}>{label}</option>
                ))}
              </select>
            </label>
          </div>
          <div style={{ display: "grid", gap: 8, gridTemplateColumns: "1fr 1fr" }}>
            <label style={{ display: "grid", gap: 4 }}>
              <span style={{ color: "var(--ink-dim)", fontSize: "0.85rem" }}>Preço de venda (R$)</span>
              <input inputMode="decimal" value={form.salePrice} onChange={(e) => setForm({ ...form, salePrice: e.target.value })} />
            </label>
            <label style={{ display: "grid", gap: 4 }}>
              <span style={{ color: "var(--ink-dim)", fontSize: "0.85rem" }}>Custo (R$, opcional)</span>
              <input inputMode="decimal" value={form.costPrice} onChange={(e) => setForm({ ...form, costPrice: e.target.value })} />
            </label>
          </div>
          <label style={{ display: "grid", gap: 4 }}>
            <span style={{ color: "var(--ink-dim)", fontSize: "0.85rem" }}>Descrição</span>
            <input value={form.description} onChange={(e) => setForm({ ...form, description: e.target.value })} />
          </label>
          <div style={{ display: "grid", gap: 8, gridTemplateColumns: "1fr 1fr" }}>
            <label style={{ display: "grid", gap: 4 }}>
              <span style={{ color: "var(--ink-dim)", fontSize: "0.85rem" }}>Preparo (min, opcional)</span>
              <input inputMode="numeric" value={form.preparationTimeMinutes} onChange={(e) => setForm({ ...form, preparationTimeMinutes: e.target.value })} />
            </label>
            <label style={{ display: "flex", gap: 8, alignItems: "center", marginTop: 20 }}>
              <input
                type="checkbox"
                style={{ width: 20, minHeight: 20 }}
                checked={form.isStockControlled}
                onChange={(e) => setForm({ ...form, isStockControlled: e.target.checked })}
              />
              <span style={{ color: "var(--ink-dim)", fontSize: "0.9rem" }}>Controla estoque</span>
            </label>
          </div>
          {error && <p className="error-text">{error}</p>}
          {form.categoryId === "" && (
            <p style={{ color: "var(--ink-faint)", fontSize: "0.85rem", margin: 0 }}>
              Selecione uma categoria para habilitar o salvar.
            </p>
          )}
          <button
            className="btn-primary"
            disabled={form.name.trim() === "" || form.categoryId === "" || saveMutation.isPending}
            onClick={() => saveMutation.mutate()}
          >
            {saveMutation.isPending ? "Salvando…" : "Salvar"}
          </button>
        </Overlay>
      )}
    </main>
  );
}
