import { useEffect, useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useDialog } from "../../ui/Dialog";
import {
    createCategory,
    createProduct,
    deactivateProduct,
    getCategories,
    getMenu,
    updateProduct,
    uploadProductImage,
    type ProductPayload,
} from "./api";
import { useAuthStore } from "../../stores/authStore";
import { ApiError } from "../../lib/apiClient";
import { formatBRL, unitOfMeasureLabel } from "../../lib/types";
import type { MenuItemResponse } from "../../lib/types";
import { QueryError } from "../../components/QueryError";
import { ProductComplementLinkPanel } from "./ProductComplementLinkPanel";
import { Modal } from "../../ui/Modal";
import { Button } from "../../ui/Button";
import { Field, TextField, SelectField } from "../../ui/Field";
import { Switch } from "../../ui/Switch";
import { useToast } from "../../ui/Toast";
import { EmptyState } from "../../ui/EmptyState";
import { SkeletonList } from "../../ui/Skeleton";

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
    const dialog = useDialog();
    const toast = useToast();
    const { companyId } = useAuthStore();
    const [editing, setEditing] = useState<MenuItemResponse | "new" | null>(null);
    const [form, setForm] = useState<FormState>(emptyForm);
    const [newCategory, setNewCategory] = useState("");
    const [creatingCategory, setCreatingCategory] = useState(false);
    const [modalNewCategory, setModalNewCategory] = useState("");
    const [imageFile, setImageFile] = useState<File | null>(null);
    const [error, setError] = useState<string | null>(null);

    // Achado de revisão (web-design-guidelines / performance): antes, `URL.createObjectURL`
    // era chamado direto no JSX — a cada re-render do formulário (qualquer tecla digitada em
    // qualquer campo) uma blob URL nova era criada e a anterior nunca era liberada
    // (URL.revokeObjectURL), vazando memória enquanto o modal ficasse aberto. Agora a URL só é
    // recriada quando `imageFile` de fato muda, e a anterior é revogada na troca/no unmount.
    const imagePreviewUrl = useMemo(
        () => (imageFile !== null ? URL.createObjectURL(imageFile) : null),
        [imageFile],
    );
    useEffect(() => {
        return () => {
            if (imagePreviewUrl !== null) URL.revokeObjectURL(imagePreviewUrl);
        };
    }, [imagePreviewUrl]);

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
                    : (editing as MenuItemResponse).id;
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

    const deactivateMutation = useMutation({
        mutationFn: (id: number) => deactivateProduct(id),
        onSuccess: () => {
            toast.success("Produto desativado.");
            refresh();
        },
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

    // Criar categoria sem sair do formulário de produto — a nova categoria
    // já entra selecionada assim que criada.
    const modalCategoryMutation = useMutation({
        mutationFn: () =>
            createCategory(companyId ?? 1, modalNewCategory.trim(), (categoriesQuery.data?.length ?? 0) + 1),
        onSuccess: (newCategoryId) => {
            toast.success("Categoria criada.");
            setForm((f) => ({ ...f, categoryId: String(newCategoryId) }));
            setModalNewCategory("");
            setCreatingCategory(false);
            setError(null);
            void queryClient.invalidateQueries({ queryKey: ["categories"] });
        },
        onError: onApiError,
    });

    return (
        <main style={{ padding: 22, maxWidth: 1100, margin: "0 auto", position: "relative" }}>
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

            {error && !editing && (
                <p className="error-text" role="alert">
                    {error}
                </p>
            )}
            {menuQuery.isError && <QueryError error={menuQuery.error} what="o cardápio" />}
            {categoriesQuery.isError && <QueryError error={categoriesQuery.error} what="as categorias" />}

            {menuQuery.isLoading && <SkeletonList rows={5} rowHeight={62} />}

            {!menuQuery.isLoading && menuQuery.data?.length === 0 && (
                <EmptyState
                    icon="🍽"
                    title="Nenhum produto cadastrado"
                    description="Adicione o primeiro item do cardápio para começar a montar pedidos."
                    action={
                        <button className="btn-primary" onClick={() => openEditor("new")}>
                            + Novo produto
                        </button>
                    }
                />
            )}

            {!menuQuery.isLoading && (menuQuery.data?.length ?? 0) > 0 && (
            <div className="ticket rise rise-2">
                {(menuQuery.data ?? []).map((product) => (
                    <div className="ticket-row" key={product.id}>
                        <div style={{ display: "flex", gap: 12, alignItems: "center" }}>
                            {product.imageUrl ? (
                                <img
                                    src={product.imageUrl}
                                    alt={product.name}
                                    width={46}
                                    height={46}
                                    loading="lazy"
                                    style={{ width: 46, height: 46, objectFit: "cover", borderRadius: 8, border: "1px solid var(--line)" }}
                                />
                            ) : (
                                <div style={{ width: 46, height: 46, borderRadius: 8, background: "var(--bg-press)", display: "grid", placeItems: "center", color: "var(--ink-faint)", fontSize: "1.2rem" }}>
                                    🍽
                                </div>
                            )}
                            <div style={{ display: "grid", gap: 2 }}>
                                <span>{product.name}</span>
                                <span style={{ fontSize: "0.8rem", color: "var(--ink-faint)" }}>
                                    {categoryName.get(product.categoryId) ?? `Categoria ${product.categoryId}`}
                                    {product.description ? ` · ${product.description}` : ""}
                                    {product.isStockControlled ? " · controla estoque" : " · sem controle de estoque"}
                                </span>
                            </div>
                        </div>
                        <div style={{ display: "flex", gap: 8, alignItems: "center" }}>
                            <span className="mono-num" style={{ color: "var(--amber)" }}>
                                {formatBRL(product.salePrice)}
                            </span>
                            <button
                                className="btn-ghost"
                                style={{ minHeight: 44, padding: "0 12px", fontSize: "0.85rem" }}
                                onClick={() => openEditor(product)}
                            >
                                Editar
                            </button>
                            <button
                                className="btn-danger"
                                style={{ minHeight: 44, padding: "0 12px", fontSize: "0.85rem" }}
                                onClick={async () => {
                                    if (
                                        await dialog.confirm({
                                            title: "Desativar produto",
                                            message: `Desativar "${product.name}"?`,
                                            confirmLabel: "Desativar",
                                            danger: true,
                                        })
                                    )
                                        deactivateMutation.mutate(product.id);
                                }}
                            >
                                Desativar
                            </button>
                        </div>
                    </div>
                ))}
            </div>
            )}

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
                                src={imagePreviewUrl ?? (editing as MenuItemResponse).imageUrl!}
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