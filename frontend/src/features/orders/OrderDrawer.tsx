import { useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import Swal from "sweetalert2"; // Adicionado SweetAlert2
import {
    addOrderItem,
    applyDiscount,
    cancelOrder,
    closeOrder,
    getOrder,
    raiseCreditLimit,
    removeServiceFee,
    reopenOrder,
    updateItemStatus,
} from "./api";
import { getMenu } from "../catalog/api";
import { getComplementGroups } from "../catalog/complementsApi";
import { getActivePromotions } from "../promotions/api";
import { getPrintSettings, printBill } from "../printing/api";
import { useAuthStore } from "../../stores/authStore";
import { ApiError } from "../../lib/apiClient";
import {
    OrderItemStatus,
    OrderStatus,
    formatBRL,
    orderItemStatusLabel,
    promotionBadge,
} from "../../lib/types";
import type { MenuItemResponse, OrderItemComplementSelection } from "../../lib/types";
import { Overlay } from "./Overlay";
import { PaymentPanel } from "./PaymentPanel";
import { PartialPaymentDialog } from "./PartialPaymentDialog";
import { ComplementSelectorModal } from "./ComplementSelectorModal";
import { useMyFeatures } from "../access/hooks";
import { getServiceFeeSetting } from "../settings/api";

interface Props {
    orderId: number;
    onClose: () => void;
}

const nextItemStatus: Record<number, number> = {
    [OrderItemStatus.Lancado]: OrderItemStatus.EnviadoCozinha,
    [OrderItemStatus.EnviadoCozinha]: OrderItemStatus.EmPreparo,
    [OrderItemStatus.EmPreparo]: OrderItemStatus.Pronto,
    [OrderItemStatus.Pronto]: OrderItemStatus.Entregue,
};

function creditLimitColor(totalAmount: number, creditLimitAmount: number): string {
    if (totalAmount >= creditLimitAmount) return "var(--danger)";
    if (totalAmount >= creditLimitAmount * 0.8) return "var(--busy)";
    return "var(--ok)";
}

export function OrderDrawer({ orderId, onClose }: Props) {
    const queryClient = useQueryClient();
    const { companyId, employeeId } = useAuthStore();
    const [menuOpen, setMenuOpen] = useState(false);
    const [partialOpen, setPartialOpen] = useState(false);
    const [search, setSearch] = useState("");
    const [discount, setDiscount] = useState("");
    const [actionError, setActionError] = useState<string | null>(null);
    const [selectingItem, setSelectingItem] = useState<MenuItemResponse | null>(null);

    const orderQuery = useQuery({
        queryKey: ["order", orderId],
        queryFn: () => getOrder(orderId),
    });

    const featuresQuery = useMyFeatures();
    const canUseCash =
        featuresQuery.data?.canManageAccess || featuresQuery.data?.features.includes("Caixa");

    const activePromosQuery = useQuery({
        queryKey: ["promotions", "active"],
        queryFn: () => getActivePromotions(useAuthStore.getState().branchId),
        refetchInterval: 60_000,
    });

    const promoByProduct = useMemo(() => {
        const map = new Map<number, string>();
        for (const p of activePromosQuery.data ?? []) map.set(p.productId, promotionBadge(p));
        return map;
    }, [activePromosQuery.data]);

    const menuQuery = useQuery({
        queryKey: ["menu", companyId],
        queryFn: () => getMenu(companyId ?? 1),
        staleTime: 5 * 60_000,
    });

    const productNameById = useMemo(() => {
        const map = new Map<number, string>();
        for (const item of menuQuery.data ?? []) map.set(item.id, item.name);
        return map;
    }, [menuQuery.data]);

    const complementGroupsQuery = useQuery({
        queryKey: ["complement-groups", companyId],
        queryFn: () => getComplementGroups(companyId ?? 1),
        staleTime: 5 * 60_000,
    });

    const complementNameById = useMemo(() => {
        const map = new Map<number, string>();
        for (const group of complementGroupsQuery.data ?? [])
            for (const c of group.complements) map.set(c.id, c.complementItemName);
        return map;
    }, [complementGroupsQuery.data]);

    const order = orderQuery.data;

    const refetchOrder = () => void queryClient.invalidateQueries({ queryKey: ["order", orderId] });

    const onError = (error: unknown) =>
        setActionError(error instanceof ApiError ? error.message : "Operação falhou.");

    const run = { onSuccess: () => { setActionError(null); refetchOrder(); }, onError };

    const addItem = useMutation({
        mutationFn: ({
            productId,
            complements,
        }: {
            productId: number;
            complements?: OrderItemComplementSelection[];
        }) => addOrderItem(orderId, productId, 1, null, employeeId, complements),
        onSuccess: () => {
            setActionError(null);
            setSelectingItem(null);
            refetchOrder();
        },
        onError,
    });

    const handlePickItem = (item: MenuItemResponse) => {
        if (item.complementGroups.length > 0) setSelectingItem(item);
        else addItem.mutate({ productId: item.id });
    };

    const advanceItem = useMutation({
        mutationFn: ({ itemId, statusId }: { itemId: number; statusId: number }) =>
            updateItemStatus(orderId, itemId, statusId, employeeId),
        ...run,
    });

    const discountMutation = useMutation({
        mutationFn: () => applyDiscount(orderId, Number(discount.replace(",", "."))),
        ...run,
    });

    const printSettingsQuery = useQuery({
        queryKey: ["printing", "settings", useAuthStore.getState().branchId],
        queryFn: () => getPrintSettings(useAuthStore.getState().branchId),
        staleTime: 60_000,
    });

    const serviceFeeSettingQuery = useQuery({
        queryKey: ["orders", "service-fee-setting", useAuthStore.getState().branchId],
        queryFn: () => getServiceFeeSetting(useAuthStore.getState().branchId),
        staleTime: 60_000,
    });
    const serviceFeeOn = serviceFeeSettingQuery.data?.enabled ?? true;

    const printBillMutation = useMutation({
        mutationFn: () => printBill(orderId),
        onError: (e) => setActionError(e instanceof ApiError ? e.message : "Falha ao imprimir a conta."),
    });

    const closeMutation = useMutation({
        mutationFn: () => closeOrder(orderId),
        onSuccess: async () => {
            setActionError(null);
            refetchOrder();

            if (printSettingsQuery.data?.printBillsEnabled) {
                const { isConfirmed } = await Swal.fire({
                    title: "Imprimir conta",
                    text: "Deseja imprimir a conta?",
                    icon: "question",
                    showCancelButton: true,
                    confirmButtonText: "Imprimir",
                    cancelButtonText: "Não"
                });

                if (isConfirmed) printBillMutation.mutate();
            }
        },
        onError,
    });

    const removeFeeMutation = useMutation({ mutationFn: () => removeServiceFee(orderId), ...run });
    const reopenMutation = useMutation({ mutationFn: () => reopenOrder(orderId), ...run });

    const raiseLimitMutation = useMutation({
        mutationFn: (newLimit: number) => raiseCreditLimit(orderId, newLimit),
        ...run,
    });

    const cancelMutation = useMutation({
        mutationFn: () => cancelOrder(orderId),
        onSuccess: onClose,
        onError,
    });

    const filteredMenu = useMemo(
        () =>
            (menuQuery.data ?? []).filter((item) =>
                item.name.toLowerCase().includes(search.toLowerCase()),
            ),
        [menuQuery.data, search],
    );

    const isOpen =
        order !== undefined &&
        (order.orderStatusId === OrderStatus.Aberto ||
            order.orderStatusId === OrderStatus.EmAndamento ||
            order.orderStatusId === OrderStatus.AguardandoPagamento);

    const isEditable =
        order !== undefined &&
        (order.orderStatusId === OrderStatus.Aberto ||
            order.orderStatusId === OrderStatus.EmAndamento);

    const awaitingPayment =
        order !== undefined && order.orderStatusId === OrderStatus.AguardandoPagamento;

    const title = order?.diningTableId
        ? `Mesa · pedido #${orderId}`
        : `Comanda · pedido #${orderId}`;

    return (
        <Overlay title={title} onClose={onClose} wide data-testid="order-drawer-overlay">
            {orderQuery.isLoading && <p style={{ color: "var(--ink-dim)" }}>Carregando pedido…</p>}
            {orderQuery.isError && <p className="error-text" data-testid="error-loading-order">Falha ao carregar o pedido.</p>}

            {order && partialOpen && (
                <PartialPaymentDialog
                    order={order}
                    onClose={() => setPartialOpen(false)}
                    onRegistered={() => {
                        setPartialOpen(false);
                        setActionError(null);
                        refetchOrder();
                    }}
                />
            )}

            {selectingItem && (
                <ComplementSelectorModal
                    productName={selectingItem.name}
                    groups={selectingItem.complementGroups}
                    onCancel={() => setSelectingItem(null)}
                    submitting={addItem.isPending}
                    onConfirm={(complements) =>
                        addItem.mutate({ productId: selectingItem.id, complements })
                    }
                />
            )}

            {order && (
                <>
                    {order.comandaId !== null && order.creditLimitAmount !== null && (
                        <div
                            className="ticket"
                            style={{
                                padding: "10px 16px",
                                display: "flex",
                                justifyContent: "space-between",
                                alignItems: "center",
                                gap: 10,
                                flexWrap: "wrap",
                                borderColor:
                                    order.totalAmount >= order.creditLimitAmount ? "var(--danger)" : "var(--line)",
                            }}
                        >
                            <span style={{ fontSize: "0.88rem", color: "var(--ink-dim)" }}>
                                Limite da comanda:{" "}
                                <strong
                                    className="mono-num"
                                    style={{
                                        color: creditLimitColor(order.totalAmount, order.creditLimitAmount),
                                    }}
                                    data-testid="comanda-credit-limit"
                                >
                                    {formatBRL(order.totalAmount)} / {formatBRL(order.creditLimitAmount)}
                                </strong>
                            </span>
                            {featuresQuery.data?.canManageAccess && (
                                <button
                                    className="btn-ghost"
                                    type="button"
                                    style={{ minHeight: 44, padding: "0 12px", fontSize: "0.85rem" }}
                                    disabled={raiseLimitMutation.isPending}
                                    data-testid="btn-raise-limit"
                                    onClick={async () => {
                                        const { value: answer } = await Swal.fire({
                                            title: "Liberar limite",
                                            input: "text",
                                            inputLabel: "Novo limite da comanda (R$)",
                                            inputValue: String(order.creditLimitAmount! + 100),
                                            showCancelButton: true,
                                            confirmButtonText: "Salvar",
                                            cancelButtonText: "Cancelar"
                                        });

                                        if (answer) {
                                            const value = Number(answer.replace(",", "."));
                                            if (Number.isFinite(value) && value > 0) raiseLimitMutation.mutate(value);
                                        }
                                    }}
                                >
                                    Liberar limite (gerente)
                                </button>
                            )}
                        </div>
                    )}

                    <div className="ticket" data-testid="order-items-list">
                        <div className="ticket-head">
                            <span className="display" style={{ fontSize: "1.2rem" }}>
                                Itens
                            </span>
                            <span className="mono-num" style={{ color: "var(--ink-faint)", fontSize: "0.85rem" }}>
                                aberto às {new Date(order.openedAt).toLocaleTimeString("pt-BR", { hour: "2-digit", minute: "2-digit" })}
                            </span>
                        </div>

                        {order.items.length === 0 && (
                            <div className="ticket-row" style={{ color: "var(--ink-faint)" }} data-testid="empty-items-msg">
                                Nenhum item lançado ainda.
                            </div>
                        )}

                        {order.items.map((item) => {
                            const next = nextItemStatus[item.orderItemStatusId];
                            const cancelled = item.orderItemStatusId === OrderItemStatus.Cancelado;
                            return (
                                <div className="ticket-row" key={item.id} data-testid={`order-item-row-${item.id}`}>
                                    <div style={{ display: "grid", gap: 2 }}>
                                        <span
                                            className="mono-num"
                                            style={{
                                                textDecoration: cancelled ? "line-through" : "none",
                                                color: cancelled ? "var(--ink-faint)" : "var(--ink)",
                                            }}
                                        >
                                            {item.quantity} × {productNameById.get(item.productId) ?? `produto #${item.productId}`} — {formatBRL(item.totalAmount)}
                                        </span>
                                        <span style={{ fontSize: "0.8rem", color: "var(--ink-faint)" }}>
                                            {orderItemStatusLabel[item.orderItemStatusId]}
                                            {item.notes ? ` · ${item.notes}` : ""}
                                        </span>
                                        {item.complements.length > 0 && (
                                            <span style={{ fontSize: "0.8rem", color: "var(--ink-faint)" }}>
                                                + {item.complements
                                                    .map((c) => complementNameById.get(c.complementId) ?? `complemento #${c.complementId}`)
                                                    .join(", ")}
                                            </span>
                                        )}
                                    </div>
                                    {isOpen && next !== undefined && !cancelled && (
                                        <div style={{ display: "flex", gap: 6 }}>
                                            <button
                                                className="btn-ghost"
                                                type="button"
                                                style={{ minHeight: 44, padding: "0 10px", fontSize: "0.85rem" }}
                                                data-testid={`btn-advance-item-${item.id}`}
                                                onClick={() => advanceItem.mutate({ itemId: item.id, statusId: next })}
                                            >
                                                → {orderItemStatusLabel[next]}
                                            </button>
                                            <button
                                                className="btn-danger"
                                                type="button"
                                                aria-label={`Cancelar item ${productNameById.get(item.productId) ?? ""}`.trim()}
                                                title="Cancelar item"
                                                style={{ minHeight: 44, padding: "0 10px", fontSize: "0.85rem" }}
                                                data-testid={`btn-cancel-item-${item.id}`}
                                                onClick={() =>
                                                    advanceItem.mutate({ itemId: item.id, statusId: OrderItemStatus.Cancelado })
                                                }
                                            >
                                                ✕
                                            </button>
                                        </div>
                                    )}
                                </div>
                            );
                        })}

                        <div className="ticket-row" style={{ color: "var(--ink-dim)" }}>
                            <span>Subtotal</span>
                            <span className="mono-num">{formatBRL(order.subtotalAmount)}</span>
                        </div>
                        {order.discountAmount > 0 && (
                            <div className="ticket-row" style={{ color: "var(--ok)" }}>
                                <span>Desconto</span>
                                <span className="mono-num">− {formatBRL(order.discountAmount)}</span>
                            </div>
                        )}
                        {order.serviceFeeAmount > 0 && (
                            <div className="ticket-row" style={{ color: "var(--ink-dim)" }}>
                                <span>Serviço (10%)</span>
                                <span className="mono-num">{formatBRL(order.serviceFeeAmount)}</span>
                            </div>
                        )}
                        {order.partialPaidAmount > 0 && (
                            <>
                                <div className="ticket-row" style={{ color: "var(--ok)" }}>
                                    <span>Pago parcial</span>
                                    <span className="mono-num">− {formatBRL(order.partialPaidAmount)}</span>
                                </div>
                                <div className="ticket-row" style={{ color: "var(--amber)" }}>
                                    <span>Restante</span>
                                    <span className="mono-num">{formatBRL(order.totalAmount - order.partialPaidAmount)}</span>
                                </div>
                            </>
                        )}
                        <div className="ticket-total">
                            <span>Total</span>
                            <span className="mono-num" style={{ color: "var(--amber)" }} data-testid="order-total-amount">
                                {formatBRL(order.totalAmount)}
                            </span>
                        </div>
                    </div>

                    {actionError && (
                        <p className="error-text" role="alert" data-testid="drawer-error-message">
                            {actionError}
                        </p>
                    )}

                    {(
                        (isOpen && order.diningTableId !== null && canUseCash &&
                            order.totalAmount - order.partialPaidAmount > 0) ||
                        awaitingPayment
                    ) && (
                            <div className="ui-row ui-row-wrap" style={{ gap: 8 }}>
                                {isOpen && order.diningTableId !== null && canUseCash &&
                                    order.totalAmount - order.partialPaidAmount > 0 && (
                                        <button className="btn-ghost" type="button" onClick={() => setPartialOpen(true)} data-testid="btn-partial-payment">
                                            💸 Pagamento parcial (cliente saindo)
                                        </button>
                                    )}

                                {awaitingPayment && (
                                    <button
                                        className="btn-ghost"
                                        type="button"
                                        disabled={reopenMutation.isPending}
                                        data-testid="btn-reopen-order"
                                        onClick={async () => {
                                            const { isConfirmed } = await Swal.fire({
                                                title: "Reabrir consumo",
                                                text: "Reabrir a conta para consumo? A taxa de serviço será recalculada no próximo fechamento.",
                                                icon: "question",
                                                showCancelButton: true,
                                                confirmButtonText: "Reabrir",
                                                cancelButtonText: "Cancelar"
                                            });

                                            if (isConfirmed) reopenMutation.mutate();
                                        }}
                                    >
                                        ↩ Reabrir consumo (fechou por engano)
                                    </button>
                                )}

                                {awaitingPayment && order.serviceFeeAmount > 0 && featuresQuery.data?.canManageAccess && (
                                    <button
                                        className="btn-ghost"
                                        type="button"
                                        disabled={removeFeeMutation.isPending}
                                        data-testid="btn-remove-fee"
                                        onClick={async () => {
                                            const { isConfirmed } = await Swal.fire({
                                                title: "Retirar taxa de serviço",
                                                text: "Retirar a taxa de serviço (10%) desta conta?",
                                                icon: "warning",
                                                showCancelButton: true,
                                                confirmButtonColor: "#d33",
                                                confirmButtonText: "Retirar 10%",
                                                cancelButtonText: "Cancelar"
                                            });

                                            if (isConfirmed) removeFeeMutation.mutate();
                                        }}
                                    >
                                        {removeFeeMutation.isPending ? "Retirando…" : "Retirar 10% (gerente)"}
                                    </button>
                                )}

                                {awaitingPayment && printSettingsQuery.data?.printBillsEnabled && (
                                    <button
                                        className="btn-ghost"
                                        type="button"
                                        disabled={printBillMutation.isPending}
                                        data-testid="btn-print-bill"
                                        onClick={() => printBillMutation.mutate()}
                                    >
                                        {printBillMutation.isPending ? "Imprimindo…" : "🖨 Imprimir conta"}
                                    </button>
                                )}
                            </div>
                        )}

                    {awaitingPayment && (
                        <PaymentPanel
                            order={order}
                            onPaid={() => {
                                setActionError(null);
                                refetchOrder();
                            }}
                        />
                    )}

                    {isEditable && (
                        <>
                            <button
                                className="btn-primary"
                                type="button"
                                aria-expanded={menuOpen}
                                aria-controls="order-drawer-menu-panel"
                                data-testid="btn-toggle-menu"
                                onClick={() => setMenuOpen((v) => !v)}
                            >
                                {menuOpen ? "Fechar cardápio" : "+ Lançar item"}
                            </button>

                            {menuOpen && (
                                <div id="order-drawer-menu-panel" style={{ display: "grid", gap: 10 }}>
                                    <input
                                        placeholder="Buscar no cardápio…"
                                        value={search}
                                        onChange={(e) => setSearch(e.target.value)}
                                        autoFocus
                                        data-testid="input-menu-search"
                                    />
                                    <div style={{ display: "grid", gap: 8, maxHeight: 260, overflowY: "auto" }}>
                                        {menuQuery.isLoading && (
                                            <p style={{ color: "var(--ink-dim)" }}>Carregando cardápio…</p>
                                        )}
                                        {filteredMenu.map((item) => (
                                            <button
                                                key={item.id}
                                                className="btn-ghost"
                                                type="button"
                                                style={{ display: "flex", justifyContent: "space-between", alignItems: "center", padding: "6px 14px", minHeight: 56 }}
                                                disabled={addItem.isPending}
                                                data-testid={`btn-add-menu-item-${item.id}`}
                                                onClick={() => handlePickItem(item)}
                                            >
                                                <span style={{ display: "flex", alignItems: "center", gap: 10 }}>
                                                    {item.imageUrl && (
                                                        <img
                                                            src={item.imageUrl}
                                                            alt=""
                                                            style={{ width: 40, height: 40, objectFit: "cover", borderRadius: 8 }}
                                                        />
                                                    )}
                                                    {item.name}
                                                    {promoByProduct.has(item.id) && (
                                                        <span
                                                            style={{
                                                                marginLeft: 8,
                                                                fontFamily: "var(--font-cond)",
                                                                fontSize: "0.68rem",
                                                                letterSpacing: "0.1em",
                                                                color: "var(--amber-ink)",
                                                                background: "var(--amber)",
                                                                borderRadius: 4,
                                                                padding: "2px 6px",
                                                                fontWeight: 700,
                                                            }}
                                                        >
                                                            {promoByProduct.get(item.id)}
                                                        </span>
                                                    )}
                                                </span>
                                                <span className="mono-num" style={{ color: "var(--amber)" }}>
                                                    {formatBRL(item.salePrice)}
                                                </span>
                                            </button>
                                        ))}
                                    </div>
                                </div>
                            )}

                            <div style={{ display: "flex", gap: 10 }}>
                                <input
                                    placeholder="Desconto (R$)…"
                                    aria-label="Desconto em reais"
                                    inputMode="decimal"
                                    value={discount}
                                    onChange={(e) => setDiscount(e.target.value)}
                                    style={{ flex: 1 }}
                                    data-testid="input-discount"
                                />
                                <button
                                    className="btn-ghost"
                                    type="button"
                                    disabled={discount.trim() === "" || discountMutation.isPending}
                                    data-testid="btn-apply-discount"
                                    onClick={() => discountMutation.mutate()}
                                >
                                    Aplicar
                                </button>
                            </div>

                            <div className="drawer-actionbar">
                                <button
                                    className="btn-danger"
                                    type="button"
                                    style={{ flex: 1 }}
                                    disabled={cancelMutation.isPending}
                                    data-testid="btn-cancel-order"
                                    onClick={async () => {
                                        const { isConfirmed } = await Swal.fire({
                                            title: "Cancelar pedido",
                                            text: "Cancelar este pedido? A mesa/comanda será liberada.",
                                            icon: "warning",
                                            showCancelButton: true,
                                            confirmButtonColor: "#d33",
                                            confirmButtonText: "Cancelar pedido",
                                            cancelButtonText: "Voltar"
                                        });

                                        if (isConfirmed) cancelMutation.mutate();
                                    }}
                                >
                                    Cancelar pedido
                                </button>
                                <button
                                    className="btn-primary"
                                    type="button"
                                    style={{ flex: 2 }}
                                    disabled={order.items.length === 0 || closeMutation.isPending}
                                    data-testid="btn-close-order"
                                    onClick={() => closeMutation.mutate()}
                                >
                                    {serviceFeeOn ? "Fechar conta (+10%)" : "Fechar conta (sem 10%)"}
                                </button>
                            </div>
                        </>
                    )}

                    {!isOpen && (
                        <p style={{ color: "var(--ink-dim)" }}>
                            Pedido encerrado — status {order.orderStatusId === OrderStatus.Pago ? "Pago" : "Cancelado"}.
                        </p>
                    )}
                </>
            )}
        </Overlay>
    );
}