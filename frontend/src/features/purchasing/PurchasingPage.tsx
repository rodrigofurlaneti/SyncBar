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
import { Modal } from "../../ui/Modal";
import { Button } from "../../ui/Button";
import { TextField, SelectField } from "../../ui/Field";
import { useToast } from "../../ui/Toast";
import { EmptyState } from "../../ui/EmptyState";
import { SkeletonList } from "../../ui/Skeleton";

interface PurchaseItemState {
    productId: number;
    quantity: string;
    unitCost: string;
}

const parseNum = (raw: string): number => {
    if (raw.trim() === "") return 0;
    const value = Number(raw.replace(",", "."));
    return Number.isFinite(value) ? value : 0;
};

export function PurchasingPage() {
    const queryClient = useQueryClient();
    const dialog = useDialog();
    const toast = useToast();
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
    const [items, setItems] = useState<PurchaseItemState[]>([{ productId: 0, quantity: "1", unitCost: "0" }]);

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
            toast.success("Fornecedor criado.");
        },
        onError: onApiError,
    });

    const deactivateSupplierMutation = useMutation({
        mutationFn: (id: number) => deactivateSupplier(id),
        onSuccess: () => {
            refreshSuppliers();
            toast.success("Fornecedor desativado.");
        },
        onError: onApiError,
    });

    const validItems: PurchaseItemPayload[] = items
        .filter((i) => i.productId > 0 && parseNum(i.quantity) > 0)
        .map((i) => ({
            productId: i.productId,
            quantity: parseNum(i.quantity),
            unitCost: parseNum(i.unitCost),
        }));

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
            setItems([{ productId: 0, quantity: "1", unitCost: "0" }]);
            refreshPurchases();
            toast.success("Compra registrada e estoque atualizado.");
        },
        onError: onApiError,
    });

    const setItem = (index: number, patch: Partial<PurchaseItemState>) =>
        setItems((current) => current.map((it, i) => (i === index ? { ...it, ...patch } : it)));

    return (
        <main style={{ padding: 22, maxWidth: 1100, margin: "0 auto", position: "relative" }}>
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
                {suppliersQuery.isLoading && <SkeletonList rows={3} rowHeight={48} />}
                {!suppliersQuery.isLoading && (suppliersQuery.data ?? []).length === 0 && (
                    <EmptyState
                        icon="🚚"
                        title="Nenhum fornecedor cadastrado"
                        description="Cadastre um fornecedor para poder registrar compras."
                        action={
                            <button className="btn-primary" onClick={() => { setError(null); setCreatingSupplier(true); }}>
                                + Fornecedor
                            </button>
                        }
                    />
                )}
                {!suppliersQuery.isLoading && (suppliersQuery.data ?? []).length > 0 && (
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
                    </div>
                )}
            </section>

            <section className="rise rise-2" style={{ marginTop: 26 }}>
                <h3 className="display" style={{ fontSize: "1.15rem", marginBottom: 8 }}>Compras registradas</h3>
                {purchasesQuery.isLoading && <SkeletonList rows={3} rowHeight={72} />}
                {!purchasesQuery.isLoading && (purchasesQuery.data ?? []).length === 0 && (
                    <EmptyState
                        icon="🧾"
                        title="Nenhuma compra registrada"
                        description="Registre uma compra para dar entrada no estoque."
                        action={
                            <button className="btn-primary" onClick={() => { setError(null); setRegisteringPurchase(true); }}>
                                + Registrar compra
                            </button>
                        }
                    />
                )}
                {!purchasesQuery.isLoading && (purchasesQuery.data ?? []).length > 0 && (
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
                    </div>
                )}
            </section>

            {creatingSupplier && (
                <Modal title="Novo fornecedor" onClose={() => setCreatingSupplier(false)} variant="center" wide>
                    <TextField
                        label="Razão social"
                        value={supplierForm.legalName}
                        onChange={(e) => setSupplierForm((f) => ({ ...f, legalName: e.target.value }))}
                        autoFocus
                    />
                    <TextField
                        label="Nome fantasia"
                        value={supplierForm.tradeName}
                        onChange={(e) => setSupplierForm((f) => ({ ...f, tradeName: e.target.value }))}
                    />
                    <div className="ui-row ui-row-wrap">
                        <div style={{ flex: 1, minWidth: 160 }}>
                            <TextField
                                label="CNPJ"
                                value={supplierForm.cnpj}
                                onChange={(e) => setSupplierForm((f) => ({ ...f, cnpj: e.target.value }))}
                                maxLength={14}
                            />
                        </div>
                        <div style={{ flex: 1, minWidth: 160 }}>
                            <TextField
                                label="Telefone"
                                value={supplierForm.phone}
                                onChange={(e) => setSupplierForm((f) => ({ ...f, phone: e.target.value }))}
                            />
                        </div>
                    </div>
                    <TextField
                        label="E-mail"
                        value={supplierForm.email}
                        onChange={(e) => setSupplierForm((f) => ({ ...f, email: e.target.value }))}
                    />
                    {error && <p className="error-text">{error}</p>}
                    <Button
                        variant="primary"
                        block
                        loading={createSupplierMutation.isPending}
                        disabled={supplierForm.legalName.trim() === ""}
                        onClick={() => createSupplierMutation.mutate()}
                    >
                        Criar fornecedor
                    </Button>
                </Modal>
            )}

            {registeringPurchase && (
                <Modal title="Registrar compra" onClose={() => setRegisteringPurchase(false)} variant="center" wide>
                    <SelectField label="Fornecedor" value={supplierId} onChange={(e) => setSupplierId(e.target.value)} autoFocus>
                        <option value="">Selecione…</option>
                        {(suppliersQuery.data ?? []).filter((s) => s.isActive).map((s) => (
                            <option key={s.id} value={s.id}>{s.tradeName ?? s.legalName}</option>
                        ))}
                    </SelectField>
                    <div className="ui-row ui-row-wrap">
                        <div style={{ flex: 1, minWidth: 160 }}>
                            <TextField label="Nº da nota" value={documentNumber} onChange={(e) => setDocumentNumber(e.target.value)} />
                        </div>
                        <div style={{ flex: 1, minWidth: 160 }}>
                            <TextField
                                label="Data"
                                type="date"
                                value={purchasedAt}
                                onChange={(e) => setPurchasedAt(e.target.value)}
                            />
                        </div>
                    </div>

                    <div className="ui-stack">
                        <span className="field-label">Itens</span>
                        {items.map((item, index) => (
                            <div key={index} className="ui-row ui-row-wrap" style={{ alignItems: "end" }}>
                                <div style={{ flex: 2, minWidth: 180 }}>
                                    <SelectField
                                        label="Produto"
                                        value={item.productId}
                                        onChange={(e) => setItem(index, { productId: Number(e.target.value) })}
                                    >
                                        <option value={0}>Produto…</option>
                                        {(menuQuery.data ?? []).map((p) => (
                                            <option key={p.id} value={p.id}>{p.name}</option>
                                        ))}
                                    </SelectField>
                                </div>
                                <div style={{ flex: 1, minWidth: 90 }}>
                                    <TextField
                                        label="Qtd"
                                        inputMode="decimal"
                                        value={item.quantity}
                                        onChange={(e) => setItem(index, { quantity: e.target.value })}
                                    />
                                </div>
                                <div style={{ flex: 1, minWidth: 110 }}>
                                    <TextField
                                        label="Custo unit."
                                        inputMode="decimal"
                                        value={item.unitCost}
                                        onChange={(e) => setItem(index, { unitCost: e.target.value })}
                                    />
                                </div>
                                <Button
                                    iconOnly
                                    aria-label="Remover item"
                                    disabled={items.length === 1}
                                    onClick={() => setItems((current) => current.filter((_, i) => i !== index))}
                                >
                                    ✕
                                </Button>
                            </div>
                        ))}
                        <Button
                            onClick={() => setItems((current) => [...current, { productId: 0, quantity: "1", unitCost: "0" }])}
                        >
                            + Adicionar item
                        </Button>
                    </div>

                    <div style={{ display: "flex", justifyContent: "space-between", color: "var(--ink-dim)" }}>
                        <span>Total</span>
                        <span className="mono-num">{formatBRL(purchaseTotal)}</span>
                    </div>

                    {error && <p className="error-text">{error}</p>}
                    <Button
                        variant="primary"
                        block
                        loading={registerPurchaseMutation.isPending}
                        disabled={supplierId === "" || validItems.length === 0}
                        onClick={() => registerPurchaseMutation.mutate()}
                    >
                        Registrar compra e dar entrada no estoque
                    </Button>
                </Modal>
            )}
        </main>
    );
}