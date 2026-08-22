import { useState } from "react";
import { Link } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  IFOOD_CATALOG_V1_OPERATIONS,
  batchUpdateIFoodProductPrices,
  batchUpdateIFoodProductStatuses,
  checkIFoodCatalogVersion,
  createIFoodCategory,
  createIFoodProduct,
  deleteIFoodCategory,
  deleteIFoodInventoryBatch,
  deleteIFoodItem,
  deleteIFoodOption,
  deleteIFoodOptionGroup,
  deleteIFoodProduct,
  disassociateIFoodOptionGroup,
  downgradeIFoodCatalogVersion,
  editIFoodCategory,
  editIFoodProduct,
  getIFoodBatchResult,
  getIFoodCatalogs,
  getIFoodInventory,
  getIFoodItemFlat,
  getIFoodProductById,
  invokeIFoodCatalogV1Operation,
  listIFoodCategories,
  listIFoodCategoryItems,
  listIFoodOptionGroups,
  listIFoodProducts,
  listIFoodProductsByExternalCode,
  listIFoodSellableItems,
  setIFoodItemExternalCode,
  setIFoodItemPrice,
  setIFoodOptionExternalCode,
  setIFoodOptionPrice,
  setIFoodOptionStatus,
  updateIFoodOptionGroup,
  updateIFoodOptionGroupStatus,
  uploadIFoodImage,
  upgradeIFoodCatalogVersion,
  type IFoodCatalogV1Operation,
  type IFoodCategoryResponse,
  type IFoodOptionGroupResponse,
  type IFoodProductResponse,
} from "./api";
import { useAuthStore } from "../../stores/authStore";
import { useToast } from "../../ui/Toast";
import { Button } from "../../ui/Button";
import { Modal } from "../../ui/Modal";
import { SelectField, TextField } from "../../ui/Field";
import { QueryError } from "../../components/QueryError";
import { EmptyState } from "../../ui/EmptyState";

// Fase 10 — módulo Catalog completo. Tier 1 (v2, viva, já usada pela sincronização automática
// desde a fase 3): telas dedicadas de categorias, produtos, itens e grupos de opções/opções, mais
// as operações administrativas (estoque, lote, versão do catálogo, imagem). Tier 2 (v1, legado):
// console genérico — todo merchant está em v1 OU v2, nunca nos dois; quem está em v1 usa a aba
// "Console v1" pra chamar qualquer um dos 56 endpoints legados sem tela dedicada.
// Ressalva importante, válida pra todas as abas: os nomes de campo foram confirmados contra a
// collection oficial do Postman, mas os VALORES de exemplo da doc são placeholders gerados pelo
// Postman (schema mock), não tráfego real capturado — trate como "estrutura confirmada, valores
// não confirmados" até testar contra o sandbox.

type TabType = "categorias" | "produtos" | "itens" | "complementos" | "admin" | "v1";

const TABS: { key: TabType; label: string }[] = [
  { key: "categorias", label: "Categorias" },
  { key: "produtos", label: "Produtos" },
  { key: "itens", label: "Itens" },
  { key: "complementos", label: "Complementos" },
  { key: "admin", label: "Administração" },
  { key: "v1", label: "Console v1 (legado)" },
];

function prettyJson(raw: string | null | undefined): string {
  if (!raw) return "";
  try {
    return JSON.stringify(JSON.parse(raw), null, 2);
  } catch {
    return raw;
  }
}

export function IFoodCatalogPage() {
  const [activeTab, setActiveTab] = useState<TabType>("categorias");

  return (
    <main style={{ padding: 22, maxWidth: 1100, margin: "0 auto" }}>
      <div className="rise" style={{ marginBottom: 18 }}>
        <Link to="/integracoes/ifood" style={{ color: "var(--ink-faint)", fontSize: "0.85rem" }}>
          ← Integração iFood
        </Link>
        <h2 className="display" style={{ fontSize: "1.7rem" }}>
          Cardápio iFood (Catalog)
        </h2>
        <span style={{ color: "var(--ink-faint)", fontSize: "0.9rem" }}>
          gerencie categorias, produtos, itens e complementos diretamente no iFood — a sincronização
          automática (fase 3) continua cobrindo o fluxo essencial; use estas telas pra ajustes finos
          e operações que ela não cobre.
        </span>
      </div>

      <div style={{ display: "flex", gap: 0, borderBottom: "1px solid var(--border)", marginBottom: 20, flexWrap: "wrap" }}>
        {TABS.map((tab) => (
          <button
            key={tab.key}
            onClick={() => setActiveTab(tab.key)}
            style={{
              padding: "12px 18px",
              border: "none",
              background: "none",
              cursor: "pointer",
              fontSize: "0.9rem",
              fontWeight: activeTab === tab.key ? 600 : 400,
              color: activeTab === tab.key ? "var(--ink)" : "var(--ink-faint)",
              borderBottom: activeTab === tab.key ? "2px solid var(--ink)" : "2px solid transparent",
              marginBottom: -1,
            }}
          >
            {tab.label}
          </button>
        ))}
      </div>

      {activeTab === "categorias" && <CategoriesTab />}
      {activeTab === "produtos" && <ProductsTab />}
      {activeTab === "itens" && <ItemsTab />}
      {activeTab === "complementos" && <OptionGroupsTab />}
      {activeTab === "admin" && <AdminTab />}
      {activeTab === "v1" && <V1ConsoleTab />}
    </main>
  );
}

// ================================================================================================
// Categorias
// ================================================================================================

function CategoriesTab() {
  const { branchId } = useAuthStore();
  const toast = useToast();
  const queryClient = useQueryClient();
  const [catalogId, setCatalogId] = useState("");
  const [creating, setCreating] = useState(false);
  const [editing, setEditing] = useState<IFoodCategoryResponse | null>(null);
  const [groupId, setGroupId] = useState("");
  const [submittedGroupId, setSubmittedGroupId] = useState("");

  const catalogsQuery = useQuery({
    queryKey: ["integrations", "ifood", "catalog", "catalogs", branchId],
    queryFn: () => getIFoodCatalogs(branchId),
  });

  const catalogs = catalogsQuery.data ?? [];
  const effectiveCatalogId = catalogId || catalogs[0]?.catalogId || "";

  const categoriesQuery = useQuery({
    queryKey: ["integrations", "ifood", "catalog", "categories", branchId, effectiveCatalogId],
    queryFn: () => listIFoodCategories(branchId, effectiveCatalogId),
    enabled: !!effectiveCatalogId,
  });

  const sellableItemsQuery = useQuery({
    queryKey: ["integrations", "ifood", "catalog", "sellable-items", branchId, submittedGroupId],
    queryFn: () => listIFoodSellableItems(branchId, submittedGroupId),
    enabled: !!submittedGroupId,
  });

  const invalidate = () =>
    void queryClient.invalidateQueries({ queryKey: ["integrations", "ifood", "catalog", "categories"] });

  const deleteMutation = useMutation({
    mutationFn: (categoryId: string) => deleteIFoodCategory(branchId, categoryId),
    onSuccess: () => {
      toast.success("Categoria excluída no iFood.");
      invalidate();
    },
    onError: () => toast.error("Não foi possível excluir a categoria."),
  });

  const categories = categoriesQuery.data ?? [];

  return (
    <div style={{ display: "grid", gap: 18 }}>
      <div className="card" style={{ padding: 16, display: "grid", gap: 12 }}>
        <div className="ui-row ui-row-wrap" style={{ gap: 12, alignItems: "flex-end", justifyContent: "space-between" }}>
          <SelectField
            label="Catálogo"
            value={effectiveCatalogId}
            onChange={(e) => setCatalogId(e.target.value)}
            disabled={catalogsQuery.isLoading}
            hint="grupo/canal (ex.: iFood, WhatsApp) — o iFood v2 pode ter mais de um"
          >
            {catalogs.map((c) => (
              <option key={c.catalogId ?? ""} value={c.catalogId ?? ""}>
                {c.catalogId} {c.status ? `(${c.status})` : ""}
              </option>
            ))}
          </SelectField>
          <Button variant="primary" disabled={!effectiveCatalogId} onClick={() => setCreating(true)}>
            + Nova categoria
          </Button>
        </div>
      </div>

      {catalogsQuery.isError && <QueryError error={catalogsQuery.error} what="os catálogos" />}
      {categoriesQuery.isError && <QueryError error={categoriesQuery.error} what="as categorias" />}

      {!categoriesQuery.isLoading && categories.length === 0 && !categoriesQuery.isError && effectiveCatalogId && (
        <EmptyState title="Nenhuma categoria" description="Este catálogo ainda não tem categorias no iFood." />
      )}

      <div style={{ display: "grid", gap: 10 }}>
        {categories.map((category) => (
          <section key={category.id} className="ticket rise" style={{ padding: 14 }}>
            <div className="ui-row ui-row-wrap" style={{ justifyContent: "space-between", alignItems: "center", gap: 10 }}>
              <div style={{ display: "grid", gap: 2 }}>
                <span style={{ fontWeight: 700 }}>{category.name}</span>
                <span style={{ color: "var(--ink-faint)", fontSize: "0.8rem" }}>
                  {category.id} · status: {category.status ?? "—"} · índice: {category.index ?? "—"}
                  {category.externalCode ? ` · código externo: ${category.externalCode}` : ""}
                </span>
              </div>
              <div style={{ display: "flex", gap: 8 }}>
                <Button variant="ghost" size="sm" onClick={() => setEditing(category)}>
                  Editar
                </Button>
                <Button
                  variant="danger"
                  size="sm"
                  loading={deleteMutation.isPending}
                  onClick={() => category.id && deleteMutation.mutate(category.id)}
                >
                  Excluir
                </Button>
              </div>
            </div>
          </section>
        ))}
      </div>

      <div className="card" style={{ padding: 16, display: "grid", gap: 12 }}>
        <strong>Itens vendáveis por grupo</strong>
        <span style={{ fontSize: "0.85rem", color: "var(--ink-faint)" }}>
          consulta os itens vendáveis associados a um groupId (obtido na resposta de categorias/itens).
        </span>
        <div className="ui-row ui-row-wrap" style={{ gap: 10, alignItems: "flex-end" }}>
          <TextField label="Group Id" value={groupId} onChange={(e) => setGroupId(e.target.value)} />
          <Button variant="ghost" onClick={() => setSubmittedGroupId(groupId)}>
            Consultar
          </Button>
        </div>
        {sellableItemsQuery.isError && <QueryError error={sellableItemsQuery.error} what="os itens vendáveis" />}
        {(sellableItemsQuery.data ?? []).map((item) => (
          <div key={item.itemId} style={{ fontSize: "0.85rem", borderTop: "1px solid var(--border)", paddingTop: 6 }}>
            {item.itemName} — {item.itemPriceValue?.toLocaleString("pt-BR", { style: "currency", currency: "BRL" }) ?? "—"}
            {item.itemExternalCode ? ` · código: ${item.itemExternalCode}` : ""}
          </div>
        ))}
      </div>

      {creating && (
        <CategoryFormModal
          branchId={branchId}
          catalogId={effectiveCatalogId}
          onClose={() => setCreating(false)}
          onSaved={() => {
            setCreating(false);
            invalidate();
          }}
        />
      )}

      {editing && (
        <CategoryFormModal
          branchId={branchId}
          catalogId={effectiveCatalogId}
          category={editing}
          onClose={() => setEditing(null)}
          onSaved={() => {
            setEditing(null);
            invalidate();
          }}
        />
      )}
    </div>
  );
}

function CategoryFormModal({
  branchId,
  catalogId,
  category,
  onClose,
  onSaved,
}: {
  branchId: number;
  catalogId: string;
  category?: IFoodCategoryResponse;
  onClose: () => void;
  onSaved: () => void;
}) {
  const toast = useToast();
  const [name, setName] = useState(category?.name ?? "");
  const [externalCode, setExternalCode] = useState(category?.externalCode ?? "");
  const [status, setStatus] = useState(category?.status ?? "AVAILABLE");
  const [index, setIndex] = useState(category?.index != null ? String(category.index) : "");

  const saveMutation = useMutation({
    mutationFn: () =>
      category?.id
        ? editIFoodCategory(branchId, catalogId, category.id, {
            name: name.trim() || undefined,
            externalCode: externalCode.trim() || undefined,
            status: status || undefined,
            index: index.trim() ? Number(index) : undefined,
          })
        : createIFoodCategory(branchId, catalogId, name.trim()),
    onSuccess: () => {
      toast.success(category ? "Categoria atualizada no iFood." : "Categoria criada no iFood.");
      onSaved();
    },
    onError: () => toast.error("Não foi possível salvar a categoria."),
  });

  return (
    <Modal onClose={onClose} title={category ? "Editar categoria" : "Nova categoria"}>
      <div style={{ display: "grid", gap: 14, minWidth: 340 }}>
        <TextField label="Nome" value={name} onChange={(e) => setName(e.target.value)} autoFocus />
        {category && (
          <>
            <TextField label="Código externo (opcional)" value={externalCode} onChange={(e) => setExternalCode(e.target.value)} />
            <SelectField label="Status" value={status} onChange={(e) => setStatus(e.target.value)}>
              <option value="AVAILABLE">Disponível</option>
              <option value="UNAVAILABLE">Indisponível</option>
            </SelectField>
            <TextField label="Índice (opcional)" inputMode="numeric" value={index} onChange={(e) => setIndex(e.target.value)} />
          </>
        )}
        <div style={{ display: "flex", gap: 10, justifyContent: "flex-end" }}>
          <Button variant="ghost" onClick={onClose}>
            Voltar
          </Button>
          <Button variant="primary" disabled={!name.trim()} loading={saveMutation.isPending} onClick={() => saveMutation.mutate()}>
            Salvar
          </Button>
        </div>
      </div>
    </Modal>
  );
}

// ================================================================================================
// Produtos
// ================================================================================================

function ProductsTab() {
  const { branchId } = useAuthStore();
  const toast = useToast();
  const queryClient = useQueryClient();
  const [creating, setCreating] = useState(false);
  const [editing, setEditing] = useState<IFoodProductResponse | null>(null);
  const [externalCodeLookup, setExternalCodeLookup] = useState("");
  const [submittedExternalCode, setSubmittedExternalCode] = useState("");
  const [idLookup, setIdLookup] = useState("");
  const [lookedUpProduct, setLookedUpProduct] = useState<IFoodProductResponse | null>(null);
  const [batchStatusIds, setBatchStatusIds] = useState("");
  const [batchStatusValue, setBatchStatusValue] = useState("AVAILABLE");
  const [batchPriceIds, setBatchPriceIds] = useState("");
  const [batchPriceValue, setBatchPriceValue] = useState("");

  const productsQuery = useQuery({
    queryKey: ["integrations", "ifood", "catalog", "products", branchId],
    queryFn: () => listIFoodProducts(branchId),
  });

  const byExternalCodeQuery = useQuery({
    queryKey: ["integrations", "ifood", "catalog", "products-by-external-code", branchId, submittedExternalCode],
    queryFn: () => listIFoodProductsByExternalCode(branchId, submittedExternalCode),
    enabled: !!submittedExternalCode,
  });

  const invalidate = () =>
    void queryClient.invalidateQueries({ queryKey: ["integrations", "ifood", "catalog", "products"] });

  const deleteMutation = useMutation({
    mutationFn: (productId: string) => deleteIFoodProduct(branchId, productId),
    onSuccess: () => {
      toast.success("Produto excluído no iFood.");
      invalidate();
    },
    onError: () => toast.error("Não foi possível excluir o produto."),
  });

  const lookupByIdMutation = useMutation({
    mutationFn: () => getIFoodProductById(branchId, idLookup.trim()),
    onSuccess: (result) => setLookedUpProduct(result),
    onError: () => toast.error("Produto não encontrado."),
  });

  const batchStatusMutation = useMutation({
    mutationFn: () =>
      batchUpdateIFoodProductStatuses(
        branchId,
        batchStatusIds
          .split(",")
          .map((id) => id.trim())
          .filter(Boolean)
          .map((productId) => ({ productId, status: batchStatusValue })),
      ),
    onSuccess: () => toast.success("Status em lote enviado ao iFood."),
    onError: () => toast.error("Falha ao atualizar status em lote."),
  });

  const batchPriceMutation = useMutation({
    mutationFn: () =>
      batchUpdateIFoodProductPrices(
        branchId,
        batchPriceIds
          .split(",")
          .map((id) => id.trim())
          .filter(Boolean)
          .map((productId) => ({ productId, value: Number(batchPriceValue) || 0 })),
      ),
    onSuccess: (result) => toast.success(`Lote de preços enviado. BatchId: ${result.batchId ?? "(ver payload)"}`),
    onError: () => toast.error("Falha ao atualizar preços em lote."),
  });

  const products = productsQuery.data ?? [];

  return (
    <div style={{ display: "grid", gap: 18 }}>
      <div className="ui-row" style={{ justifyContent: "flex-end" }}>
        <Button variant="primary" onClick={() => setCreating(true)}>
          + Novo produto
        </Button>
      </div>

      {productsQuery.isError && <QueryError error={productsQuery.error} what="os produtos" />}
      {!productsQuery.isLoading && products.length === 0 && !productsQuery.isError && (
        <EmptyState title="Nenhum produto" description="Nenhum produto encontrado no catálogo do iFood." />
      )}

      <div style={{ display: "grid", gap: 10 }}>
        {products.map((product) => (
          <section key={product.id} className="ticket rise" style={{ padding: 14 }}>
            <div className="ui-row ui-row-wrap" style={{ justifyContent: "space-between", alignItems: "center", gap: 10 }}>
              <div style={{ display: "grid", gap: 2 }}>
                <span style={{ fontWeight: 700 }}>{product.name}</span>
                <span style={{ color: "var(--ink-faint)", fontSize: "0.8rem" }}>
                  {product.id}
                  {product.externalCode ? ` · código externo: ${product.externalCode}` : ""}
                  {product.ean ? ` · EAN: ${product.ean}` : ""}
                </span>
              </div>
              <div style={{ display: "flex", gap: 8 }}>
                <Button variant="ghost" size="sm" onClick={() => setEditing(product)}>
                  Editar
                </Button>
                <Button
                  variant="danger"
                  size="sm"
                  loading={deleteMutation.isPending}
                  onClick={() => product.id && deleteMutation.mutate(product.id)}
                >
                  Excluir
                </Button>
              </div>
            </div>
          </section>
        ))}
      </div>

      <div className="card" style={{ padding: 16, display: "grid", gap: 12 }}>
        <strong>Buscar produto</strong>
        <div className="ui-row ui-row-wrap" style={{ gap: 10, alignItems: "flex-end" }}>
          <TextField label="Código externo" value={externalCodeLookup} onChange={(e) => setExternalCodeLookup(e.target.value)} />
          <Button variant="ghost" onClick={() => setSubmittedExternalCode(externalCodeLookup)}>
            Buscar por código
          </Button>
          <TextField label="Product Id" value={idLookup} onChange={(e) => setIdLookup(e.target.value)} />
          <Button variant="ghost" loading={lookupByIdMutation.isPending} onClick={() => lookupByIdMutation.mutate()}>
            Buscar por Id
          </Button>
        </div>
        {byExternalCodeQuery.isError && <QueryError error={byExternalCodeQuery.error} what="a busca por código externo" />}
        {(byExternalCodeQuery.data ?? []).map((p) => (
          <div key={p.id} style={{ fontSize: "0.85rem" }}>
            {p.name} — {p.id}
          </div>
        ))}
        {lookedUpProduct && (
          <div style={{ fontSize: "0.85rem", borderTop: "1px solid var(--border)", paddingTop: 8 }}>
            {lookedUpProduct.name} — {lookedUpProduct.id}
          </div>
        )}
      </div>

      <div className="card" style={{ padding: 16, display: "grid", gap: 12 }}>
        <strong>Atualização em lote</strong>
        <span style={{ fontSize: "0.85rem", color: "var(--ink-faint)" }}>Ids separados por vírgula.</span>
        <div className="ui-row ui-row-wrap" style={{ gap: 10, alignItems: "flex-end" }}>
          <TextField label="Product Ids" value={batchStatusIds} onChange={(e) => setBatchStatusIds(e.target.value)} />
          <SelectField label="Status" value={batchStatusValue} onChange={(e) => setBatchStatusValue(e.target.value)}>
            <option value="AVAILABLE">Disponível</option>
            <option value="UNAVAILABLE">Indisponível</option>
          </SelectField>
          <Button variant="ghost" loading={batchStatusMutation.isPending} onClick={() => batchStatusMutation.mutate()}>
            Atualizar status em lote
          </Button>
        </div>
        <div className="ui-row ui-row-wrap" style={{ gap: 10, alignItems: "flex-end" }}>
          <TextField label="Product Ids" value={batchPriceIds} onChange={(e) => setBatchPriceIds(e.target.value)} />
          <TextField label="Novo preço" inputMode="decimal" value={batchPriceValue} onChange={(e) => setBatchPriceValue(e.target.value)} />
          <Button variant="ghost" loading={batchPriceMutation.isPending} onClick={() => batchPriceMutation.mutate()}>
            Atualizar preço em lote
          </Button>
        </div>
      </div>

      {creating && (
        <ProductFormModal
          branchId={branchId}
          onClose={() => setCreating(false)}
          onSaved={() => {
            setCreating(false);
            invalidate();
          }}
        />
      )}

      {editing && (
        <ProductFormModal
          branchId={branchId}
          product={editing}
          onClose={() => setEditing(null)}
          onSaved={() => {
            setEditing(null);
            invalidate();
          }}
        />
      )}
    </div>
  );
}

function ProductFormModal({
  branchId,
  product,
  onClose,
  onSaved,
}: {
  branchId: number;
  product?: IFoodProductResponse;
  onClose: () => void;
  onSaved: () => void;
}) {
  const toast = useToast();
  const [name, setName] = useState(product?.name ?? "");
  const [description, setDescription] = useState(product?.description ?? "");
  const [externalCode, setExternalCode] = useState(product?.externalCode ?? "");
  const [ean, setEan] = useState(product?.ean ?? "");
  const [image, setImage] = useState(product?.imagePath ?? "");

  const saveMutation = useMutation({
    mutationFn: () => {
      const payload = {
        name: name.trim(),
        description: description.trim() || undefined,
        externalCode: externalCode.trim() || undefined,
        ean: ean.trim() || undefined,
        image: image.trim() || undefined,
      };
      return product?.id ? editIFoodProduct(branchId, product.id, payload) : createIFoodProduct(branchId, payload);
    },
    onSuccess: () => {
      toast.success(product ? "Produto atualizado no iFood." : "Produto criado no iFood.");
      onSaved();
    },
    onError: () => toast.error("Não foi possível salvar o produto."),
  });

  return (
    <Modal onClose={onClose} title={product ? "Editar produto" : "Novo produto"}>
      <div style={{ display: "grid", gap: 14, minWidth: 360 }}>
        <TextField label="Nome" value={name} onChange={(e) => setName(e.target.value)} autoFocus />
        <TextField label="Descrição (opcional)" value={description} onChange={(e) => setDescription(e.target.value)} />
        <TextField label="Código externo (opcional)" value={externalCode} onChange={(e) => setExternalCode(e.target.value)} />
        <TextField label="EAN (opcional)" value={ean} onChange={(e) => setEan(e.target.value)} />
        <TextField label="URL da imagem (opcional)" value={image} onChange={(e) => setImage(e.target.value)} />
        <span style={{ fontSize: "0.8rem", color: "var(--ink-faint)" }}>
          horários de disponibilidade (shifts) não estão nesta tela — use o console v1/v2 pra casos avançados.
        </span>
        <div style={{ display: "flex", gap: 10, justifyContent: "flex-end" }}>
          <Button variant="ghost" onClick={onClose}>
            Voltar
          </Button>
          <Button variant="primary" disabled={!name.trim()} loading={saveMutation.isPending} onClick={() => saveMutation.mutate()}>
            Salvar
          </Button>
        </div>
      </div>
    </Modal>
  );
}

// ================================================================================================
// Itens (v2 — flat)
// ================================================================================================

function ItemsTab() {
  const { branchId } = useAuthStore();
  const toast = useToast();
  const [itemId, setItemId] = useState("");
  const [submittedItemId, setSubmittedItemId] = useState("");
  const [priceValue, setPriceValue] = useState("");
  const [externalCode, setExternalCode] = useState("");
  const [categoryId, setCategoryId] = useState("");
  const [productId, setProductId] = useState("");
  const [categoryItemsId, setCategoryItemsId] = useState("");
  const [submittedCategoryItemsId, setSubmittedCategoryItemsId] = useState("");

  const itemQuery = useQuery({
    queryKey: ["integrations", "ifood", "catalog", "item", branchId, submittedItemId],
    queryFn: () => getIFoodItemFlat(branchId, submittedItemId),
    enabled: !!submittedItemId,
  });

  const categoryItemsQuery = useQuery({
    queryKey: ["integrations", "ifood", "catalog", "category-items", branchId, submittedCategoryItemsId],
    queryFn: () => listIFoodCategoryItems(branchId, submittedCategoryItemsId),
    enabled: !!submittedCategoryItemsId,
  });

  const setPriceMutation = useMutation({
    mutationFn: () => setIFoodItemPrice(branchId, submittedItemId, Number(priceValue) || 0),
    onSuccess: () => {
      toast.success("Preço do item atualizado no iFood.");
      void itemQuery.refetch();
    },
    onError: () => toast.error("Falha ao atualizar o preço do item."),
  });

  const setExternalCodeMutation = useMutation({
    mutationFn: () => setIFoodItemExternalCode(branchId, submittedItemId, externalCode.trim() || undefined),
    onSuccess: () => {
      toast.success("Código externo do item atualizado no iFood.");
      void itemQuery.refetch();
    },
    onError: () => toast.error("Falha ao atualizar o código externo do item."),
  });

  const deleteItemMutation = useMutation({
    mutationFn: () => deleteIFoodItem(branchId, categoryId.trim(), productId.trim()),
    onSuccess: () => toast.success("Item excluído no iFood."),
    onError: () => toast.error("Falha ao excluir o item."),
  });

  const item = itemQuery.data;

  return (
    <div style={{ display: "grid", gap: 18 }}>
      <div className="card" style={{ padding: 16, display: "grid", gap: 12 }}>
        <strong>Consultar item (formato flat v2)</strong>
        <div className="ui-row ui-row-wrap" style={{ gap: 10, alignItems: "flex-end" }}>
          <TextField label="Item Id" value={itemId} onChange={(e) => setItemId(e.target.value)} />
          <Button variant="ghost" onClick={() => setSubmittedItemId(itemId)}>
            Consultar
          </Button>
        </div>
        {itemQuery.isError && <QueryError error={itemQuery.error} what="o item" />}
        {item && (
          <pre className="ticket" style={{ padding: 12, fontSize: "0.8rem", overflowX: "auto", whiteSpace: "pre-wrap" }}>
            {prettyJson(item.rawPayload) || `${item.itemId} · status ${item.status} · preço ${item.priceValue}`}
          </pre>
        )}
        {submittedItemId && (
          <div style={{ display: "grid", gap: 10 }}>
            <div className="ui-row ui-row-wrap" style={{ gap: 10, alignItems: "flex-end" }}>
              <TextField label="Novo preço" inputMode="decimal" value={priceValue} onChange={(e) => setPriceValue(e.target.value)} />
              <Button variant="ghost" loading={setPriceMutation.isPending} onClick={() => setPriceMutation.mutate()}>
                Atualizar preço
              </Button>
            </div>
            <div className="ui-row ui-row-wrap" style={{ gap: 10, alignItems: "flex-end" }}>
              <TextField label="Novo código externo" value={externalCode} onChange={(e) => setExternalCode(e.target.value)} />
              <Button variant="ghost" loading={setExternalCodeMutation.isPending} onClick={() => setExternalCodeMutation.mutate()}>
                Atualizar código externo
              </Button>
            </div>
          </div>
        )}
      </div>

      <div className="card" style={{ padding: 16, display: "grid", gap: 12 }}>
        <strong>Excluir item de uma categoria</strong>
        <div className="ui-row ui-row-wrap" style={{ gap: 10, alignItems: "flex-end" }}>
          <TextField label="Category Id" value={categoryId} onChange={(e) => setCategoryId(e.target.value)} />
          <TextField label="Product Id" value={productId} onChange={(e) => setProductId(e.target.value)} />
          <Button
            variant="danger"
            disabled={!categoryId.trim() || !productId.trim()}
            loading={deleteItemMutation.isPending}
            onClick={() => deleteItemMutation.mutate()}
          >
            Excluir item
          </Button>
        </div>
      </div>

      <div className="card" style={{ padding: 16, display: "grid", gap: 12 }}>
        <strong>Itens de uma categoria</strong>
        <div className="ui-row ui-row-wrap" style={{ gap: 10, alignItems: "flex-end" }}>
          <TextField label="Category Id" value={categoryItemsId} onChange={(e) => setCategoryItemsId(e.target.value)} />
          <Button variant="ghost" onClick={() => setSubmittedCategoryItemsId(categoryItemsId)}>
            Consultar
          </Button>
        </div>
        {categoryItemsQuery.isError && <QueryError error={categoryItemsQuery.error} what="os itens da categoria" />}
        {categoryItemsQuery.data?.rawPayload && (
          <pre className="ticket" style={{ padding: 12, fontSize: "0.8rem", overflowX: "auto", whiteSpace: "pre-wrap" }}>
            {prettyJson(categoryItemsQuery.data.rawPayload)}
          </pre>
        )}
      </div>
    </div>
  );
}

// ================================================================================================
// Complementos (grupos de opções / opções)
// ================================================================================================

function OptionGroupsTab() {
  const { branchId } = useAuthStore();
  const toast = useToast();
  const queryClient = useQueryClient();
  const [editing, setEditing] = useState<IFoodOptionGroupResponse | null>(null);
  const [optionId, setOptionId] = useState("");
  const [optionPrice, setOptionPrice] = useState("");
  const [optionExternalCode, setOptionExternalCode] = useState("");

  const groupsQuery = useQuery({
    queryKey: ["integrations", "ifood", "catalog", "option-groups", branchId],
    queryFn: () => listIFoodOptionGroups(branchId, true),
  });

  const invalidate = () =>
    void queryClient.invalidateQueries({ queryKey: ["integrations", "ifood", "catalog", "option-groups"] });

  const deleteGroupMutation = useMutation({
    mutationFn: (id: string) => deleteIFoodOptionGroup(branchId, id),
    onSuccess: () => {
      toast.success("Grupo de opções excluído no iFood.");
      invalidate();
    },
    onError: () => toast.error("Falha ao excluir o grupo de opções."),
  });

  const toggleStatusMutation = useMutation({
    mutationFn: ({ id, available }: { id: string; available: boolean }) => updateIFoodOptionGroupStatus(branchId, id, available),
    onSuccess: () => {
      toast.success("Status do grupo atualizado no iFood.");
      invalidate();
    },
    onError: () => toast.error("Falha ao atualizar o status do grupo."),
  });

  const setOptionPriceMutation = useMutation({
    mutationFn: () => setIFoodOptionPrice(branchId, optionId.trim(), Number(optionPrice) || 0),
    onSuccess: () => toast.success("Preço da opção atualizado no iFood."),
    onError: () => toast.error("Falha ao atualizar o preço da opção."),
  });

  const setOptionExternalCodeMutation = useMutation({
    mutationFn: () => setIFoodOptionExternalCode(branchId, optionId.trim(), optionExternalCode.trim()),
    onSuccess: () => toast.success("Código externo da opção atualizado no iFood."),
    onError: () => toast.error("Falha ao atualizar o código externo da opção."),
  });

  const toggleOptionStatusMutation = useMutation({
    mutationFn: (available: boolean) => setIFoodOptionStatus(branchId, optionId.trim(), available),
    onSuccess: () => toast.success("Status da opção atualizado no iFood."),
    onError: () => toast.error("Falha ao atualizar o status da opção."),
  });

  const deleteOptionMutation = useMutation({
    mutationFn: () => deleteIFoodOption(branchId, editing?.id ?? "", optionId.trim()),
    onSuccess: () => toast.success("Opção excluída no iFood."),
    onError: () => toast.error("Falha ao excluir a opção."),
  });

  const disassociateMutation = useMutation({
    mutationFn: ({ groupId, productId }: { groupId: string; productId: string }) =>
      disassociateIFoodOptionGroup(branchId, groupId, productId),
    onSuccess: () => toast.success("Grupo desassociado do produto no iFood."),
    onError: () => toast.error("Falha ao desassociar o grupo do produto."),
  });

  const renameGroupMutation = useMutation({
    mutationFn: (name: string) => updateIFoodOptionGroup(branchId, editing?.id ?? "", name),
    onSuccess: () => {
      toast.success("Nome do grupo atualizado no iFood.");
      invalidate();
    },
    onError: () => toast.error("Falha ao renomear o grupo."),
  });

  const [disassociateProductId, setDisassociateProductId] = useState("");
  const [renameValue, setRenameValue] = useState("");

  const groups = groupsQuery.data ?? [];

  return (
    <div style={{ display: "grid", gap: 18 }}>
      {groupsQuery.isError && <QueryError error={groupsQuery.error} what="os grupos de opções" />}
      {!groupsQuery.isLoading && groups.length === 0 && !groupsQuery.isError && (
        <EmptyState title="Nenhum grupo de opções" description="Nenhum grupo de complementos encontrado no iFood." />
      )}

      <div style={{ display: "grid", gap: 10 }}>
        {groups.map((group) => (
          <section key={group.id} className="ticket rise" style={{ padding: 14 }}>
            <div className="ui-row ui-row-wrap" style={{ justifyContent: "space-between", alignItems: "center", gap: 10 }}>
              <div style={{ display: "grid", gap: 2 }}>
                <span style={{ fontWeight: 700 }}>{group.name}</span>
                <span style={{ color: "var(--ink-faint)", fontSize: "0.8rem" }}>
                  {group.id} · status: {group.status ?? "—"}
                </span>
              </div>
              <div style={{ display: "flex", gap: 8 }}>
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => {
                    setEditing(group);
                    setRenameValue(group.name ?? "");
                  }}
                >
                  Gerenciar opções
                </Button>
                <Button
                  variant="ghost"
                  size="sm"
                  loading={toggleStatusMutation.isPending}
                  onClick={() => group.id && toggleStatusMutation.mutate({ id: group.id, available: group.status !== "AVAILABLE" })}
                >
                  {group.status === "AVAILABLE" ? "Pausar" : "Reativar"}
                </Button>
                <Button
                  variant="danger"
                  size="sm"
                  loading={deleteGroupMutation.isPending}
                  onClick={() => group.id && deleteGroupMutation.mutate(group.id)}
                >
                  Excluir
                </Button>
              </div>
            </div>
          </section>
        ))}
      </div>

      {editing && (
        <Modal onClose={() => setEditing(null)} title={`Opções de "${editing.name}"`}>
          <div style={{ display: "grid", gap: 14, minWidth: 380 }}>
            <div className="ui-row ui-row-wrap" style={{ gap: 10, alignItems: "flex-end" }}>
              <TextField label="Nome do grupo" value={renameValue} onChange={(e) => setRenameValue(e.target.value)} autoFocus />
              <Button
                variant="ghost"
                disabled={!renameValue.trim() || renameValue.trim() === editing.name}
                loading={renameGroupMutation.isPending}
                onClick={() => renameGroupMutation.mutate(renameValue.trim())}
              >
                Renomear
              </Button>
            </div>

            <hr style={{ border: "none", borderTop: "1px solid var(--border)", margin: "2px 0" }} />

            <TextField label="Option Id" value={optionId} onChange={(e) => setOptionId(e.target.value)} />
            <div className="ui-row ui-row-wrap" style={{ gap: 10, alignItems: "flex-end" }}>
              <TextField label="Novo preço" inputMode="decimal" value={optionPrice} onChange={(e) => setOptionPrice(e.target.value)} />
              <Button
                variant="ghost"
                disabled={!optionId.trim()}
                loading={setOptionPriceMutation.isPending}
                onClick={() => setOptionPriceMutation.mutate()}
              >
                Atualizar preço
              </Button>
            </div>
            <div className="ui-row ui-row-wrap" style={{ gap: 10, alignItems: "flex-end" }}>
              <TextField label="Novo código externo" value={optionExternalCode} onChange={(e) => setOptionExternalCode(e.target.value)} />
              <Button
                variant="ghost"
                disabled={!optionId.trim() || !optionExternalCode.trim()}
                loading={setOptionExternalCodeMutation.isPending}
                onClick={() => setOptionExternalCodeMutation.mutate()}
              >
                Atualizar código
              </Button>
            </div>
            <div className="ui-row ui-row-wrap" style={{ gap: 10 }}>
              <Button
                variant="ghost"
                disabled={!optionId.trim()}
                loading={toggleOptionStatusMutation.isPending}
                onClick={() => toggleOptionStatusMutation.mutate(true)}
              >
                Ativar opção
              </Button>
              <Button
                variant="ghost"
                disabled={!optionId.trim()}
                loading={toggleOptionStatusMutation.isPending}
                onClick={() => toggleOptionStatusMutation.mutate(false)}
              >
                Pausar opção
              </Button>
              <Button
                variant="danger"
                disabled={!optionId.trim()}
                loading={deleteOptionMutation.isPending}
                onClick={() => deleteOptionMutation.mutate()}
              >
                Excluir opção
              </Button>
            </div>

            <hr style={{ border: "none", borderTop: "1px solid var(--border)", margin: "6px 0" }} />

            <strong style={{ fontSize: "0.9rem" }}>Desassociar este grupo de um produto</strong>
            <div className="ui-row ui-row-wrap" style={{ gap: 10, alignItems: "flex-end" }}>
              <TextField label="Product Id" value={disassociateProductId} onChange={(e) => setDisassociateProductId(e.target.value)} />
              <Button
                variant="ghost"
                disabled={!disassociateProductId.trim()}
                loading={disassociateMutation.isPending}
                onClick={() =>
                  editing.id && disassociateMutation.mutate({ groupId: editing.id, productId: disassociateProductId.trim() })
                }
              >
                Desassociar
              </Button>
            </div>

            <div style={{ display: "flex", justifyContent: "flex-end", marginTop: 6 }}>
              <Button variant="ghost" onClick={() => setEditing(null)}>
                Fechar
              </Button>
            </div>
          </div>
        </Modal>
      )}
    </div>
  );
}

// ================================================================================================
// Administração (estoque, lote, versão do catálogo, imagem)
// ================================================================================================

function AdminTab() {
  const { branchId } = useAuthStore();
  const toast = useToast();
  const [inventoryProductId, setInventoryProductId] = useState("");
  const [submittedInventoryId, setSubmittedInventoryId] = useState("");
  const [batchDeleteIds, setBatchDeleteIds] = useState("");
  const [batchResultId, setBatchResultId] = useState("");
  const [submittedBatchResultId, setSubmittedBatchResultId] = useState("");
  const [imageJsonBody, setImageJsonBody] = useState("");
  const [confirmingUpgrade, setConfirmingUpgrade] = useState(false);
  const [confirmingDowngrade, setConfirmingDowngrade] = useState(false);

  const versionQuery = useQuery({
    queryKey: ["integrations", "ifood", "catalog", "version", branchId],
    queryFn: () => checkIFoodCatalogVersion(branchId),
  });

  const inventoryQuery = useQuery({
    queryKey: ["integrations", "ifood", "catalog", "inventory", branchId, submittedInventoryId],
    queryFn: () => getIFoodInventory(branchId, submittedInventoryId),
    enabled: !!submittedInventoryId,
  });

  const batchResultQuery = useQuery({
    queryKey: ["integrations", "ifood", "catalog", "batch-result", branchId, submittedBatchResultId],
    queryFn: () => getIFoodBatchResult(branchId, submittedBatchResultId),
    enabled: !!submittedBatchResultId,
  });

  const deleteInventoryBatchMutation = useMutation({
    mutationFn: () =>
      deleteIFoodInventoryBatch(
        branchId,
        batchDeleteIds.split(",").map((id) => id.trim()).filter(Boolean),
      ),
    onSuccess: () => toast.success("Estoque removido em lote no iFood."),
    onError: () => toast.error("Falha ao remover estoque em lote."),
  });

  const upgradeMutation = useMutation({
    mutationFn: () => upgradeIFoodCatalogVersion(branchId),
    onSuccess: () => {
      toast.success("Catálogo migrado para v2 no iFood.");
      setConfirmingUpgrade(false);
      void versionQuery.refetch();
    },
    onError: () => toast.error("Falha ao migrar o catálogo para v2."),
  });

  const downgradeMutation = useMutation({
    mutationFn: () => downgradeIFoodCatalogVersion(branchId),
    onSuccess: () => {
      toast.success("Catálogo revertido para v1 no iFood.");
      setConfirmingDowngrade(false);
      void versionQuery.refetch();
    },
    onError: () => toast.error("Falha ao reverter o catálogo para v1."),
  });

  const uploadImageMutation = useMutation({
    mutationFn: () => uploadIFoodImage(branchId, imageJsonBody),
    onSuccess: (result) => toast.success(`Imagem enviada. Resposta: ${prettyJson(result.rawPayload).slice(0, 120) || "(vazia)"}`),
    onError: () => toast.error("Falha ao enviar a imagem — confira o JSON."),
  });

  const inventory = inventoryQuery.data;

  return (
    <div style={{ display: "grid", gap: 18 }}>
      <div className="card" style={{ padding: 16, display: "grid", gap: 12 }}>
        <strong>Versão do catálogo</strong>
        {versionQuery.isError && <QueryError error={versionQuery.error} what="a versão do catálogo" />}
        <span>Versão atual: {versionQuery.data?.version ?? "—"}</span>
        <span style={{ fontSize: "0.8rem", color: "var(--ink-faint)" }}>
          ⚠️ upgrade/downgrade são operações destrutivas e irreversíveis contra o catálogo real do merchant no
          iFood — confirme com atenção.
        </span>
        <div className="ui-row ui-row-wrap" style={{ gap: 10 }}>
          <Button variant="ghost" onClick={() => setConfirmingUpgrade(true)}>
            Migrar para v2 (upgrade)
          </Button>
          <Button variant="ghost" onClick={() => setConfirmingDowngrade(true)}>
            Reverter para v1 (downgrade)
          </Button>
        </div>
      </div>

      <div className="card" style={{ padding: 16, display: "grid", gap: 12 }}>
        <strong>Estoque</strong>
        <div className="ui-row ui-row-wrap" style={{ gap: 10, alignItems: "flex-end" }}>
          <TextField label="Product Id" value={inventoryProductId} onChange={(e) => setInventoryProductId(e.target.value)} />
          <Button variant="ghost" onClick={() => setSubmittedInventoryId(inventoryProductId)}>
            Consultar
          </Button>
        </div>
        {inventoryQuery.isError && <QueryError error={inventoryQuery.error} what="o estoque" />}
        {inventory && (
          <span style={{ fontSize: "0.85rem" }}>
            Quantidade: {inventory.amount ?? "—"} · em estoque: {inventory.inStock ? "sim" : "não"}
          </span>
        )}
        <div className="ui-row ui-row-wrap" style={{ gap: 10, alignItems: "flex-end" }}>
          <TextField
            label="Remover estoque em lote (Ids separados por vírgula)"
            value={batchDeleteIds}
            onChange={(e) => setBatchDeleteIds(e.target.value)}
          />
          <Button
            variant="danger"
            disabled={!batchDeleteIds.trim()}
            loading={deleteInventoryBatchMutation.isPending}
            onClick={() => deleteInventoryBatchMutation.mutate()}
          >
            Remover
          </Button>
        </div>
      </div>

      <div className="card" style={{ padding: 16, display: "grid", gap: 12 }}>
        <strong>Resultado de processamento em lote</strong>
        <span style={{ fontSize: "0.85rem", color: "var(--ink-faint)" }}>
          use o BatchId devolvido por uma atualização de preços em lote (aba Produtos).
        </span>
        <div className="ui-row ui-row-wrap" style={{ gap: 10, alignItems: "flex-end" }}>
          <TextField label="Batch Id" value={batchResultId} onChange={(e) => setBatchResultId(e.target.value)} />
          <Button variant="ghost" onClick={() => setSubmittedBatchResultId(batchResultId)}>
            Consultar
          </Button>
        </div>
        {batchResultQuery.isError && <QueryError error={batchResultQuery.error} what="o resultado do lote" />}
        {batchResultQuery.data && (
          <div style={{ display: "grid", gap: 4 }}>
            <span>Status: {batchResultQuery.data.batchStatus ?? "—"}</span>
            {batchResultQuery.data.results.map((r, idx) => (
              <span key={idx} style={{ fontSize: "0.8rem", color: "var(--ink-faint)" }}>
                {r.resourceId}: {r.result} {r.failureReason ? `(${r.failureReason})` : ""}
              </span>
            ))}
          </div>
        )}
      </div>

      <div className="card" style={{ padding: 16, display: "grid", gap: 12 }}>
        <strong>Upload de imagem</strong>
        <span style={{ fontSize: "0.8rem", color: "var(--ink-faint)" }}>
          ⚠️ a doc oficial não documenta o schema deste endpoint — cole o JSON pronto que o iFood espera; a
          resposta crua aparece após o envio.
        </span>
        <textarea
          value={imageJsonBody}
          onChange={(e) => setImageJsonBody(e.target.value)}
          rows={5}
          placeholder='{"image": "..."}'
          style={{ fontFamily: "monospace", fontSize: "0.8rem", padding: 8 }}
        />
        <div style={{ display: "flex", justifyContent: "flex-end" }}>
          <Button
            variant="ghost"
            disabled={!imageJsonBody.trim()}
            loading={uploadImageMutation.isPending}
            onClick={() => uploadImageMutation.mutate()}
          >
            Enviar imagem
          </Button>
        </div>
      </div>

      {confirmingUpgrade && (
        <Modal onClose={() => setConfirmingUpgrade(false)} title="Confirmar migração para v2">
          <div style={{ display: "grid", gap: 14, minWidth: 320 }}>
            <span>
              Esta ação é <strong>destrutiva e irreversível</strong> — o iFood vai reorganizar todo o catálogo real
              do merchant desta filial de v1 para v2. Confirma?
            </span>
            <div style={{ display: "flex", gap: 10, justifyContent: "flex-end" }}>
              <Button variant="ghost" onClick={() => setConfirmingUpgrade(false)}>
                Cancelar
              </Button>
              <Button variant="danger" loading={upgradeMutation.isPending} onClick={() => upgradeMutation.mutate()}>
                Confirmar migração
              </Button>
            </div>
          </div>
        </Modal>
      )}

      {confirmingDowngrade && (
        <Modal onClose={() => setConfirmingDowngrade(false)} title="Confirmar reversão para v1">
          <div style={{ display: "grid", gap: 14, minWidth: 320 }}>
            <span>
              Esta ação é <strong>destrutiva e irreversível</strong> — o iFood vai reorganizar todo o catálogo real
              do merchant desta filial de v2 para v1. Confirma?
            </span>
            <div style={{ display: "flex", gap: 10, justifyContent: "flex-end" }}>
              <Button variant="ghost" onClick={() => setConfirmingDowngrade(false)}>
                Cancelar
              </Button>
              <Button variant="danger" loading={downgradeMutation.isPending} onClick={() => downgradeMutation.mutate()}>
                Confirmar reversão
              </Button>
            </div>
          </div>
        </Modal>
      )}
    </div>
  );
}

// ================================================================================================
// Console v1 (legado)
// ================================================================================================

function V1ConsoleTab() {
  const { branchId } = useAuthStore();
  const toast = useToast();
  const [operation, setOperation] = useState<IFoodCatalogV1Operation>(IFOOD_CATALOG_V1_OPERATIONS[0]);
  const [routeParamsText, setRouteParamsText] = useState("");
  const [queryParamsText, setQueryParamsText] = useState("");
  const [jsonBody, setJsonBody] = useState("");

  const invokeMutation = useMutation({
    mutationFn: () => {
      let routeParams: Record<string, string> | undefined;
      let queryParams: Record<string, string> | undefined;
      try {
        routeParams = routeParamsText.trim() ? JSON.parse(routeParamsText) : undefined;
        queryParams = queryParamsText.trim() ? JSON.parse(queryParamsText) : undefined;
      } catch {
        throw new Error("invalid-json");
      }
      return invokeIFoodCatalogV1Operation(branchId, operation, {
        routeParams,
        queryParams,
        jsonBody: jsonBody.trim() || undefined,
      });
    },
    onSuccess: (result) => {
      if (result.success) toast.success(`Chamada v1 concluída (HTTP ${result.statusCode}).`);
      else toast.error(`iFood respondeu com erro (HTTP ${result.statusCode}) — veja o payload abaixo.`);
    },
    onError: (error: unknown) =>
      toast.error(error instanceof Error && error.message === "invalid-json" ? "routeParams/queryParams precisam ser JSON válido." : "Falha ao chamar o endpoint v1."),
  });

  return (
    <div style={{ display: "grid", gap: 18 }}>
      <div className="card" style={{ padding: 16, display: "grid", gap: 12 }}>
        <strong>Console genérico — módulo Catalog v1 (legado)</strong>
        <span style={{ fontSize: "0.85rem", color: "var(--ink-faint)" }}>
          use esta tela apenas se o merchant desta filial ainda estiver na versão v1 do catálogo (ver aba
          Administração → "Versão do catálogo"). Cobre os 56 endpoints legados sem tela dedicada — escolha a
          operação, informe os parâmetros de rota/query (como JSON) e o corpo (quando aplicável). A resposta,
          inclusive erros do iFood, é sempre mostrada crua abaixo.
        </span>

        <SelectField
          label="Operação"
          value={operation}
          onChange={(e) => setOperation(e.target.value as IFoodCatalogV1Operation)}
        >
          {IFOOD_CATALOG_V1_OPERATIONS.map((op) => (
            <option key={op} value={op}>
              {op}
            </option>
          ))}
        </SelectField>

        <div style={{ display: "grid", gap: 4 }}>
          <label style={{ fontSize: "0.85rem", fontWeight: 600 }}>Route params (JSON, opcional)</label>
          <textarea
            value={routeParamsText}
            onChange={(e) => setRouteParamsText(e.target.value)}
            rows={2}
            placeholder='{"categoryId": "..."}'
            style={{ fontFamily: "monospace", fontSize: "0.8rem", padding: 8 }}
          />
        </div>

        <div style={{ display: "grid", gap: 4 }}>
          <label style={{ fontSize: "0.85rem", fontWeight: 600 }}>Query params (JSON, opcional)</label>
          <textarea
            value={queryParamsText}
            onChange={(e) => setQueryParamsText(e.target.value)}
            rows={2}
            placeholder='{"page": "1"}'
            style={{ fontFamily: "monospace", fontSize: "0.8rem", padding: 8 }}
          />
        </div>

        <div style={{ display: "grid", gap: 4 }}>
          <label style={{ fontSize: "0.85rem", fontWeight: 600 }}>Corpo JSON (opcional — POST/PUT/PATCH)</label>
          <textarea
            value={jsonBody}
            onChange={(e) => setJsonBody(e.target.value)}
            rows={6}
            placeholder='{"name": "..."}'
            style={{ fontFamily: "monospace", fontSize: "0.8rem", padding: 8 }}
          />
        </div>

        <div style={{ display: "flex", justifyContent: "flex-end" }}>
          <Button variant="primary" loading={invokeMutation.isPending} onClick={() => invokeMutation.mutate()}>
            Chamar iFood
          </Button>
        </div>

        {invokeMutation.data && (
          <pre className="ticket" style={{ padding: 12, fontSize: "0.8rem", overflowX: "auto", whiteSpace: "pre-wrap" }}>
            {`HTTP ${invokeMutation.data.statusCode}${invokeMutation.data.errorMessage ? ` — ${invokeMutation.data.errorMessage}` : ""}\n\n${prettyJson(invokeMutation.data.responseBody)}`}
          </pre>
        )}
      </div>
    </div>
  );
}
