import { useEffect, useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useDialog } from "../../ui/Dialog";
import {
    activateCategory,
    activateProduct,
    createCategory,
    createProduct,
    deactivateCategory,
    deactivateProduct,
    getCategories,
    getCategoriesForManagement,
    getMenuForManagement,
    updateCategory,
    updateProduct,
    uploadProductImage,
    type ProductPayload,
} from "./api";
import { getStockByBranch } from "../stock/api";
import { useAuthStore } from "../../stores/authStore";
import { ApiError } from "../../lib/apiClient";
import { formatBRL, unitOfMeasureLabel } from "../../lib/types";
import type { CategoryManagementResponse, ProductManagementResponse, StockItemResponse } from "../../lib/types";
import { QueryError } from "../../components/QueryError";
import { ProductComplementLinkPanel } from "./ProductComplementLinkPanel";
import { Modal } from "../../ui/Modal";
import { Button } from "../../ui/Button";
import { Field, TextField, SelectField } from "../../ui/Field";
import { Switch } from "../../ui/Switch";
import { StatusBadge } from "../../ui/StatusBadge";
import { useToast } from "../../ui/Toast";
import { EmptyState } from "../../ui/EmptyState";
import { SkeletonList } from "../../ui/Skeleton";

const PAGE_SIZE = 8;
const ALL_CATEGORIES = "all" as const;
type StatusFilter = "all" | "active" | "inactive";

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

/* Ícones inline (sem dependência externa) — traço fino, 1.6px, no estilo do resto da UI. */
const DragIcon = () => (
    <svg width="13" height="13" viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
        <circle cx="8" cy="6" r="1.6" /><circle cx="16" cy="6" r="1.6" />
        <circle cx="8" cy="12" r="1.6" /><circle cx="16" cy="12" r="1.6" />
        <circle cx="8" cy="18" r="1.6" /><circle cx="16" cy="18" r="1.6" />
    </svg>
);
const SearchIcon = () => (
    <svg width="15" height="15" viewBox="0 0 24 24" fill="none" aria-hidden="true">
        <circle cx="11" cy="11" r="7" stroke="currentColor" strokeWidth="1.8" />
        <path d="M21 21l-4.3-4.3" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" />
    </svg>
);
const ChevronLeft = () => (
    <svg width="12" height="12" viewBox="0 0 24 24" fill="none" aria-hidden="true">
        <path d="M15 5l-7 7 7 7" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" />
    </svg>
);
const ChevronRight = () => (
    <svg width="12" height="12" viewBox="0 0 24 24" fill="none" aria-hidden="true">
        <path d="M9 5l7 7-7 7" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" />
    </svg>
);

export function ProductsPage() {
    const queryClient = useQueryClient();
    const dialog = useDialog();
    const toast = useToast();
    const { companyId, branchId } = useAuthStore();
    const [editing, setEditing] = useState<ProductManagementResponse | "new" | null>(null);
    const [form, setForm] = useState<FormState>(emptyForm);
    const [newCategory, setNewCategory] = useState("");
    const [creatingCategory, setCreatingCategory] = useState(false);
    const [modalNewCategory, setModalNewCategory] = useState("");
    const [editingCategoryId, setEditingCategoryId] = useState<number | null>(null);
    const [categoryEditName, setCategoryEditName] = useState("");
    const [categoryEditOrder, setCategoryEditOrder] = useState("");
    const [dragCategoryId, setDragCategoryId] = useState<number | null>(null);
    const [imageFile, setImageFile] = useState<File | null>(null);
    const [error, setError] = useState<string | null>(null);

    // Painel de produtos: categoria selecionada, busca, filtro de status e paginação.
    const [selectedCategoryId, setSelectedCategoryId] = useState<number | typeof ALL_CATEGORIES>(ALL_CATEGORIES);
    const [search, setSearch] = useState("");
    const [statusFilter, setStatusFilter] = useState<StatusFilter>("all");
    const [page, setPage] = useState(1);

    const imagePreviewUrl = useMemo(
        () => (imageFile !== null ? URL.createObjectURL(imageFile) : null),
        [imageFile],
    );
    useEffect(() => {
        return () => {
            if (imagePreviewUrl !== null) URL.revokeObjectURL(imagePreviewUrl);
        };
    }, [imagePreviewUrl]);

    // Lista "para pedido" (só ativas) — alimenta o <select> de categoria do modal de produto;
    // nunca deve ser possível cadastrar um produto numa categoria já desativada.
    const categoriesQuery = useQuery({
        queryKey: ["categories", companyId],
        queryFn: () => getCategories(companyId ?? 1),
    });

    // Lista "de gerenciamento" (ativas + inativas, com contador de itens) — alimenta a coluna
    // de categorias do split view.
    const categoriesManagementQuery = useQuery({
        queryKey: ["categories", "management", companyId],
        queryFn: () => getCategoriesForManagement(companyId ?? 1),
    });

    const productsQuery = useQuery({
        queryKey: ["menu", "management", companyId],
        queryFn: () => getMenuForManagement(companyId ?? 1),
    });

    // Estoque real da filial atual — usado só para o selo "Em estoque/Baixo/Sem estoque".
    // Se falhar ou não tiver branchId, os produtos com controle de estoque caem no selo
    // neutro "Sem registro" em vez de quebrar a tela.
    const stockQuery = useQuery({
        queryKey: ["stock", "branch", branchId],
        queryFn: () => getStockByBranch(branchId ?? 1),
        enabled: branchId != null,
    });
    const stockByProduct = useMemo(() => {
        const map = new Map<number, StockItemResponse>();
        for (const s of stockQuery.data ?? []) map.set(s.productId, s);
        return map;
    }, [stockQuery.data]);

    const sortedCategories = useMemo(
        () => [...(categoriesManagementQuery.data ?? [])].sort((a, b) => a.displayOrder - b.displayOrder),
        [categoriesManagementQuery.data],
    );

    const refresh = () => {
        void queryClient.invalidateQueries({ queryKey: ["menu"] });
        void queryClient.invalidateQueries({ queryKey: ["categories"] });
    };

    const openEditor = (product: ProductManagementResponse | "new") => {
        setError(null);
        setImageFile(null);
        setCreatingCategory(false);
        setModalNewCategory("");
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
        mutationFn: async () => {
            const productId =
                editing === "new"
                    ? await createProduct(companyId ?? 1, buildPayload())
                    : (editing as ProductManagementResponse).id;
            if (editing !== "new") await updateProduct(productId, buildPayload());
            if (imageFile !== null) await uploadProductImage(productId, imageFile);
        },
        onSuccess: () => {
            toast.success(editing === "new" ? "Produto criado." : "Produto atualizado.");
            setEditing(null);
            refresh();
        },
        onError: onApiError,
    });

    // Um switch só, duas mutações por trás: liga chama activateProduct, desliga chama
    // deactivateProduct — mantém o histórico/])soft delete sem precisar de dois botões.
    const toggleProductMutation = useMutation({
        mutationFn: ({ id, activate }: { id: number; activate: boolean }) =>
            activate ? activateProduct(id) : deactivateProduct(id),
        onSuccess: (_data, vars) => {
            toast.success(vars.activate ? "Produto ativado." : "Produto desativado.");
            refresh();
        },
        onError: onApiError,
    });

    const toggleProduct = async (product: ProductManagementResponse) => {
        if (product.isActive) {
            const confirmed = await dialog.confirm({
                title: "Desativar produto",
                message: `Desativar "${product.name}"? Ele deixa de aparecer no cardápio para pedidos.`,
                confirmLabel: "Desativar",
                danger: true,
            });
            if (!confirmed) return;
        }
        toggleProductMutation.mutate({ id: product.id, activate: !product.isActive });
    };

    const categoryMutation = useMutation({
        mutationFn: () =>
            createCategory(companyId ?? 1, newCategory.trim(), (categoriesManagementQuery.data?.length ?? 0) + 1),
        onSuccess: () => {
            setNewCategory("");
            refresh();
        },
        onError: onApiError,
    });

    const modalCategoryMutation = useMutation({
        mutationFn: () =>
            createCategory(companyId ?? 1, modalNewCategory.trim(), (categoriesManagementQuery.data?.length ?? 0) + 1),
        onSuccess: (newCategoryId) => {
            toast.success("Categoria criada.");
            setForm((f) => ({ ...f, categoryId: String(newCategoryId) }));
            setModalNewCategory("");
            setCreatingCategory(false);
            setError(null);
            refresh();
        },
        onError: onApiError,
    });

    const startCategoryEdit = (category: CategoryManagementResponse) => {
        setEditingCategoryId(category.id);
        setCategoryEditName(category.name);
        setCategoryEditOrder(String(category.displayOrder));
    };

    const cancelCategoryEdit = () => {
        setEditingCategoryId(null);
        setCategoryEditName("");
        setCategoryEditOrder("");
    };

    const updateCategoryMutation = useMutation({
        mutationFn: () =>
            updateCategory(editingCategoryId!, categoryEditName.trim(), Number(categoryEditOrder) || 0),
        onSuccess: () => {
            toast.success("Categoria atualizada.");
            cancelCategoryEdit();
            refresh();
        },
        onError: onApiError,
    });

    const toggleCategoryMutation = useMutation({
        mutationFn: ({ id, activate }: { id: number; activate: boolean }) =>
            activate ? activateCategory(id) : deactivateCategory(id),
        onSuccess: (_data, vars) => {
            toast.success(vars.activate ? "Categoria ativada." : "Categoria desativada.");
            refresh();
        },
        onError: onApiError,
    });

    const toggleCategory = async (category: CategoryManagementResponse) => {
        if (category.isActive) {
            const confirmed = await dialog.confirm({
                title: "Desativar categoria",
                message: `Desativar "${category.name}"? Produtos já cadastrados nela continuam funcionando, mas ela deixa de aparecer como opção para novos cadastros.`,
                confirmLabel: "Desativar",
                danger: true,
            });
            if (!confirmed) return;
        }
        toggleCategoryMutation.mutate({ id: category.id, activate: !category.isActive });
    };

    // Reordenar arrastando: troca o DisplayOrder das duas categorias envolvidas via o
    // mesmo endpoint PUT /api/categories/{id} que a edição manual já usa.
    const reorderMutation = useMutation({
        mutationFn: async ({ from, to }: { from: CategoryManagementResponse; to: CategoryManagementResponse }) => {
            await Promise.all([
                updateCategory(from.id, from.name, to.displayOrder),
                updateCategory(to.id, to.name, from.displayOrder),
            ]);
        },
        onSuccess: () => refresh(),
        onError: onApiError,
    });

    const handleCategoryDrop = (target: CategoryManagementResponse) => {
        const source = sortedCategories.find((c) => c.id === dragCategoryId);
        setDragCategoryId(null);
        if (!source || source.id === target.id) return;
        reorderMutation.mutate({ from: source, to: target });
    };

    const selectCategory = (id: number | typeof ALL_CATEGORIES) => {
        setSelectedCategoryId(id);
        setPage(1);
    };

    const filteredProducts = useMemo(() => {
        let list = productsQuery.data ?? [];
        if (selectedCategoryId !== ALL_CATEGORIES) list = list.filter((p) => p.categoryId === selectedCategoryId);
        if (statusFilter === "active") list = list.filter((p) => p.isActive);
        if (statusFilter === "inactive") list = list.filter((p) => !p.isActive);
        if (search.trim() !== "") {
            const q = search.trim().toLowerCase();
            list = list.filter((p) => p.name.toLowerCase().includes(q));
        }
        return list;
    }, [productsQuery.data, selectedCategoryId, statusFilter, search]);

    const totalPages = Math.max(1, Math.ceil(filteredProducts.length / PAGE_SIZE));
    const currentPage = Math.min(page, totalPages);
    const pageStart = (currentPage - 1) * PAGE_SIZE;
    const pageItems = filteredProducts.slice(pageStart, pageStart + PAGE_SIZE);

    const stockBadge = (product: ProductManagementResponse) => {
        if (!product.isStockControlled) return <StatusBadge color="var(--ink-faint)">Sem controle</StatusBadge>;
        const stock = stockByProduct.get(product.id);
        if (!stock) return <StatusBadge color="var(--ink-faint)">Sem registro</StatusBadge>;
        if (stock.currentQuantity <= 0) return <StatusBadge color="var(--danger)">Sem estoque</StatusBadge>;
        if (stock.isBelowMinimum) return <StatusBadge color="var(--amber)">Baixo · {stock.currentQuantity}</StatusBadge>;
        return <StatusBadge color="var(--ok)">Em estoque · {stock.currentQuantity}</StatusBadge>;
    };

    return (
        <main style={{ padding: 22, maxWidth: 1280, margin: "0 auto", position: "relative" }}>
            <div className="rise" style={{ display: "flex", alignItems: "baseline", gap: 14, marginBottom: 18 }}>
                <h2 className="display" style={{ fontSize: "1.7rem" }}>Cardápio</h2>
                <span style={{ flex: 1 }} />
                <button type="button" className="btn-primary" onClick={() => openEditor("new")}>+ Novo produto</button>
            </div>

            {categoriesManagementQuery.isError && <QueryError error={categoriesManagementQuery.error} what="as categorias" />}
            {productsQuery.isError && <QueryError error={productsQuery.error} what="o cardápio" />}
            {error && !editing && (
                <p className="error-text" role="alert">
                    {error}
                </p>
            )}

            <div className="catalog-split rise rise-1">
                {/* --- Coluna esquerda: categorias --- */}
                <div className="catalog-panel">
                    <div className="catalog-panel-head">
                        <h3 style={{ fontSize: "0.95rem", color: "var(--ink-dim)" }}>Categorias</h3>
                        <span className="catalog-count">{sortedCategories.length}</span>
                    </div>

                    <div className="category-list">
                        <div
                            className={`category-row is-pinned ${selectedCategoryId === ALL_CATEGORIES ? "is-selected" : ""}`}
                            onClick={() => selectCategory(ALL_CATEGORIES)}
                        >
                            <span className="category-drag-handle" style={{ visibility: "hidden" }}><DragIcon /></span>
                            <span className="category-main">
                                <span className="category-name">Todos os produtos</span>
                                <span className="category-count-pill">{productsQuery.data?.length ?? 0}</span>
                            </span>
                        </div>

                        {sortedCategories.map((category, index) =>
                            editingCategoryId === category.id ? (
                                <div key={category.id} style={{ display: "flex", gap: 6, alignItems: "center", padding: "4px 8px" }}>
                                    <input
                                        autoFocus
                                        value={categoryEditName}
                                        onChange={(e) => setCategoryEditName(e.target.value)}
                                        style={{ flex: 1, minWidth: 0, minHeight: 36 }}
                                    />
                                    <input
                                        type="number"
                                        inputMode="numeric"
                                        value={categoryEditOrder}
                                        onChange={(e) => setCategoryEditOrder(e.target.value)}
                                        title="Ordem de exibição"
                                        style={{ width: 52, minHeight: 36 }}
                                    />
                                    <Button
                                        size="sm"
                                        loading={updateCategoryMutation.isPending}
                                        disabled={categoryEditName.trim() === ""}
                                        onClick={() => updateCategoryMutation.mutate()}
                                    >
                                        Salvar
                                    </Button>
                                    <Button size="sm" iconOnly aria-label="Cancelar edição" onClick={cancelCategoryEdit}>
                                        ✕
                                    </Button>
                                </div>
                            ) : (
                                <div
                                    key={category.id}
                                    className={`category-row ${selectedCategoryId === category.id ? "is-selected" : ""} ${!category.isActive ? "is-inactive" : ""} ${dragCategoryId === category.id ? "is-dragging" : ""}`}
                                    draggable
                                    onClick={() => selectCategory(category.id)}
                                    onDragStart={() => setDragCategoryId(category.id)}
                                    onDragEnd={() => setDragCategoryId(null)}
                                    onDragOver={(e) => e.preventDefault()}
                                    onDrop={(e) => { e.preventDefault(); handleCategoryDrop(category); }}
                                >
                                    <span className="category-drag-handle" title="Arraste para reordenar" onClick={(e) => e.stopPropagation()}>
                                        <DragIcon />
                                    </span>
                                    <span className="category-order">{String(index + 1).padStart(2, "0")}</span>
                                    <span className="category-main">
                                        <span className="category-name">{category.name}</span>
                                        <span className="category-count-pill">{category.productCount}</span>
                                        {!category.isActive && <span className="category-inactive-tag">Inativa</span>}
                                    </span>
                                    <Switch
                                        checked={category.isActive}
                                        onChange={() => toggleCategory(category)}
                                        label={category.isActive ? `Desativar categoria ${category.name}` : `Ativar categoria ${category.name}`}
                                    />
                                    <Button
                                        size="sm"
                                        iconOnly
                                        aria-label={`Editar categoria ${category.name}`}
                                        onClick={(e) => { e.stopPropagation(); startCategoryEdit(category); }}
                                    >
                                        ✎
                                    </Button>
                                </div>
                            ),
                        )}
                    </div>

                    <div className="catalog-new-cat">
                        <input
                            placeholder="Nova categoria…"
                            value={newCategory}
                            onChange={(e) => setNewCategory(e.target.value)}
                            onKeyDown={(e) => {
                                if (e.key === "Enter" && newCategory.trim() !== "") categoryMutation.mutate();
                            }}
                        />
                        <Button
                            size="sm"
                            iconOnly
                            aria-label="Criar categoria"
                            disabled={newCategory.trim() === "" || categoryMutation.isPending}
                            loading={categoryMutation.isPending}
                            onClick={() => categoryMutation.mutate()}
                        >
                            +
                        </Button>
                    </div>

                    <p className="catalog-hint">
                        Arraste para reordenar. Desativar uma categoria não afeta os produtos já vinculados a ela.
                    </p>
                </div>

                {/* --- Coluna direita: produtos da categoria selecionada --- */}
                <div className="catalog-panel">
                    <div className="catalog-toolbar">
                        <div className="catalog-search">
                            <SearchIcon />
                            <input
                                placeholder="Buscar produto por nome…"
                                value={search}
                                onChange={(e) => { setSearch(e.target.value); setPage(1); }}
                            />
                        </div>
                        <div className="segmented" role="group" aria-label="Filtrar por status">
                            {(["all", "active", "inactive"] as StatusFilter[]).map((s) => (
                                <button
                                    key={s}
                                    type="button"
                                    className={statusFilter === s ? "is-active" : ""}
                                    onClick={() => { setStatusFilter(s); setPage(1); }}
                                >
                                    {s === "all" ? "Todos" : s === "active" ? "Ativos" : "Inativos"}
                                </button>
                            ))}
                        </div>
                    </div>

                    {productsQuery.isLoading && <div style={{ padding: 16 }}><SkeletonList rows={5} rowHeight={62} /></div>}

                    {!productsQuery.isLoading && filteredProducts.length === 0 && (
                        <EmptyState
                            icon="🍽"
                            title="Nenhum produto encontrado"
                            description={
                                search.trim() !== ""
                                    ? `Nenhum resultado para "${search.trim()}". Ajuste a busca ou o filtro de status.`
                                    : (productsQuery.data?.length ?? 0) === 0
                                        ? "Adicione o primeiro item do cardápio para começar a montar pedidos."
                                        : "Nenhum produto nesta categoria com o filtro atual."
                            }
                            action={
                                <button type="button" className="btn-primary" onClick={() => openEditor("new")}>
                                    + Novo produto
                                </button>
                            }
                        />
                    )}

                    {!productsQuery.isLoading && filteredProducts.length > 0 && (
                        <>
                            <div className="ticket" style={{ border: "none", borderRadius: 0 }}>
                                {pageItems.map((product) => (
                                    <div className={`ticket-row product-row ${!product.isActive ? "is-inactive" : ""}`} key={product.id}>
                                        <div className="product-row-grid" style={{ flex: 1 }}>
                                            <div style={{ display: "flex", gap: 12, alignItems: "center", minWidth: 0 }}>
                                                {product.imageUrl ? (
                                                    <img
                                                        src={product.imageUrl}
                                                        alt={product.name}
                                                        width={44}
                                                        height={44}
                                                        loading="lazy"
                                                        style={{ width: 44, height: 44, objectFit: "cover", borderRadius: 8, border: "1px solid var(--line)", flexShrink: 0 }}
                                                    />
                                                ) : (
                                                    <div style={{ width: 44, height: 44, borderRadius: 8, background: "var(--bg-press)", display: "grid", placeItems: "center", color: "var(--ink-faint)", fontSize: "1.1rem", flexShrink: 0 }}>
                                                        🍽
                                                    </div>
                                                )}
                                                <div style={{ display: "grid", gap: 2, minWidth: 0 }}>
                                                    <span style={{ display: "flex", alignItems: "center", gap: 7, whiteSpace: "nowrap", overflow: "hidden", textOverflow: "ellipsis" }}>
                                                        {product.name}
                                                        {!product.isActive && <span className="category-inactive-tag">Inativo</span>}
                                                    </span>
                                                    <span style={{ fontSize: "0.8rem", color: "var(--ink-faint)", whiteSpace: "nowrap", overflow: "hidden", textOverflow: "ellipsis" }}>
                                                        {product.categoryName}
                                                        {product.description ? ` · ${product.description}` : ""}
                                                    </span>
                                                </div>
                                            </div>
                                            <span className="mono-num" style={{ color: "var(--amber)", textAlign: "right" }}>
                                                {formatBRL(product.salePrice)}
                                            </span>
                                            <span className="product-stock-cell">{stockBadge(product)}</span>
                                            <div style={{ display: "flex", gap: 8, alignItems: "center", justifyContent: "flex-end" }}>
                                                <Button size="sm" iconOnly aria-label={`Editar ${product.name}`} onClick={() => openEditor(product)}>
                                                    ✎
                                                </Button>
                                                <Switch
                                                    checked={product.isActive}
                                                    onChange={() => toggleProduct(product)}
                                                    label={product.isActive ? `Desativar ${product.name}` : `Ativar ${product.name}`}
                                                />
                                            </div>
                                        </div>
                                    </div>
                                ))}
                            </div>

                            <div className="catalog-footer">
                                <span>
                                    Mostrando {pageStart + 1}–{Math.min(pageStart + PAGE_SIZE, filteredProducts.length)} de {filteredProducts.length} produtos
                                </span>
                                <div className="pager">
                                    <button type="button" disabled={currentPage <= 1} onClick={() => setPage(currentPage - 1)} aria-label="Página anterior">
                                        <ChevronLeft />
                                    </button>
                                    {Array.from({ length: totalPages }, (_, i) => i + 1).map((n) => (
                                        <button
                                            key={n}
                                            type="button"
                                            className={n === currentPage ? "is-current" : ""}
                                            onClick={() => setPage(n)}
                                        >
                                            {n}
                                        </button>
                                    ))}
                                    <button type="button" disabled={currentPage >= totalPages} onClick={() => setPage(currentPage + 1)} aria-label="Próxima página">
                                        <ChevronRight />
                                    </button>
                                </div>
                            </div>
                        </>
                    )}
                </div>
            </div>

            {editing !== null && (
                <Modal
                    title={editing === "new" ? "Novo produto" : "Editar produto"}
                    onClose={() => setEditing(null)}
                    variant="center"
                    wide
                >
                    <TextField
                        label="Nome"
                        type="text"
                        value={form.name}
                        onChange={(e) => setForm({ ...form, name: e.target.value })}
                        autoFocus
                    />

                    {/* alignItems: "end" — o label "Categoria" pode quebrar em 2 linhas por causa
                        do link "+ nova categoria" embutido nele; alinhando pelo rodapé da linha,
                        os dois campos (selects) ficam sempre na mesma altura, mesmo quando um
                        label é mais alto que o outro. */}
                    <div className="ui-row ui-row-wrap" style={{ alignItems: "end" }}>
                        <div style={{ flex: 1, minWidth: 240 }}>
                            <Field
                                label={
                                    <span className="ui-row" style={{ justifyContent: "space-between", width: "100%" }}>
                                        Categoria
                                        {!creatingCategory && (
                                            <button
                                                type="button"
                                                onClick={() => setCreatingCategory(true)}
                                                style={{ background: "transparent", border: "none", color: "var(--amber)", cursor: "pointer", fontSize: "0.8rem", padding: 0 }}
                                            >
                                                + nova categoria
                                            </button>
                                        )}
                                    </span>
                                }
                            >
                                {(a11y) =>
                                    creatingCategory ? (
                                        <div className="ui-row" style={{ gap: 6 }}>
                                            <input
                                                {...a11y}
                                                type="text"
                                                autoFocus
                                                placeholder="Nome da categoria"
                                                value={modalNewCategory}
                                                onChange={(e) => setModalNewCategory(e.target.value)}
                                                onKeyDown={(e) => {
                                                    if (e.key === "Enter") {
                                                        e.preventDefault();
                                                        if (modalNewCategory.trim() !== "") modalCategoryMutation.mutate();
                                                    } else if (e.key === "Escape") {
                                                        setCreatingCategory(false);
                                                        setModalNewCategory("");
                                                    }
                                                }}
                                                style={{ flex: 1, minWidth: 0 }}
                                            />
                                            <Button
                                                size="sm"
                                                loading={modalCategoryMutation.isPending}
                                                disabled={modalNewCategory.trim() === ""}
                                                onClick={() => modalCategoryMutation.mutate()}
                                            >
                                                Criar
                                            </Button>
                                            <Button
                                                size="sm"
                                                iconOnly
                                                aria-label="Cancelar criação de categoria"
                                                onClick={() => { setCreatingCategory(false); setModalNewCategory(""); }}
                                            >
                                                ✕
                                            </Button>
                                        </div>
                                    ) : (
                                        <select
                                            {...a11y}
                                            value={form.categoryId}
                                            onChange={(e) => setForm({ ...form, categoryId: e.target.value })}
                                        >
                                            <option value="">Selecione a categoria…</option>
                                            {(categoriesQuery.data ?? []).map((c) => (
                                                <option key={c.id} value={c.id}>{c.name}</option>
                                            ))}
                                        </select>
                                    )
                                }
                            </Field>
                        </div>
                        <div style={{ flex: 1, minWidth: 160 }}>
                            <SelectField
                                label="Unidade"
                                value={form.unitOfMeasureId}
                                onChange={(e) => setForm({ ...form, unitOfMeasureId: e.target.value })}
                            >
                                {Object.entries(unitOfMeasureLabel).map(([id, label]) => (
                                    <option key={id} value={id}>{label}</option>
                                ))}
                            </SelectField>
                        </div>
                    </div>

                    <div className="ui-row ui-row-wrap">
                        <div style={{ flex: 1, minWidth: 160 }}>
                            <TextField
                                label="Preço de venda (R$)"
                                inputMode="decimal"
                                value={form.salePrice}
                                onChange={(e) => setForm({ ...form, salePrice: e.target.value })}
                            />
                        </div>
                        <div style={{ flex: 1, minWidth: 160 }}>
                            <TextField
                                label="Custo (R$, opcional)"
                                inputMode="decimal"
                                value={form.costPrice}
                                onChange={(e) => setForm({ ...form, costPrice: e.target.value })}
                            />
                        </div>
                    </div>

                    <TextField
                        label="Descrição"
                        type="text"
                        value={form.description}
                        onChange={(e) => setForm({ ...form, description: e.target.value })}
                    />

                    <div className="ui-row ui-row-wrap" style={{ alignItems: "end" }}>
                        <div style={{ flex: 1, minWidth: 160 }}>
                            <TextField
                                label="Preparo (min, opcional)"
                                inputMode="numeric"
                                value={form.preparationTimeMinutes}
                                onChange={(e) => setForm({ ...form, preparationTimeMinutes: e.target.value })}
                            />
                        </div>
                        <div className="ui-row" style={{ gap: 10, paddingBottom: 12 }}>
                            <Switch
                                checked={form.isStockControlled}
                                onChange={(next) => setForm({ ...form, isStockControlled: next })}
                                label="Controla estoque"
                            />
                            <span style={{ color: "var(--ink-dim)", fontSize: "0.9rem" }}>Controla estoque</span>
                        </div>
                    </div>

                    <div className="field">
                        <span className="field-label">Foto do produto (JPG/PNG/WebP, até 2 MB)</span>
                        <input
                            type="file"
                            accept="image/jpeg,image/png,image/webp"
                            onChange={(e) => setImageFile(e.target.files?.[0] ?? null)}
                        />
                        {(imagePreviewUrl !== null || (editing !== "new" && editing !== null && editing.imageUrl)) && (
                            <img
                                src={imagePreviewUrl ?? (editing as ProductManagementResponse).imageUrl!}
                                alt="Prévia"
                                width={90}
                                height={90}
                                style={{ width: 90, height: 90, objectFit: "cover", borderRadius: 10, border: "1px solid var(--line)" }}
                            />
                        )}
                    </div>

                    {editing !== "new" && editing !== null && (
                        <ProductComplementLinkPanel productId={editing.id} />
                    )}

                    {error && (
                        <p className="error-text" role="alert">
                            {error}
                        </p>
                    )}
                    {form.categoryId === "" && (
                        <p className="field-hint" style={{ margin: 0 }}>
                            Selecione uma categoria para habilitar o salvar.
                        </p>
                    )}

                    <Button
                        variant="primary"
                        block
                        loading={saveMutation.isPending}
                        disabled={form.name.trim() === "" || form.categoryId === ""}
                        onClick={() => saveMutation.mutate()}
                    >
                        Salvar
                    </Button>
                </Modal>
            )}
        </main>
    );
}
