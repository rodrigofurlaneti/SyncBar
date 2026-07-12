import { useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
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
import { Overlay } from "./Overlay";
import { PaymentPanel } from "./PaymentPanel";
import { PartialPaymentDialog } from "./PartialPaymentDialog";
import { useMyFeatures } from "../access/hooks";

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

export function OrderDrawer({ orderId, onClose }: Props) {
  const queryClient = useQueryClient();
  const { companyId, employeeId } = useAuthStore();
  const [menuOpen, setMenuOpen] = useState(false);
  const [partialOpen, setPartialOpen] = useState(false);
  const [search, setSearch] = useState("");
  const [discount, setDiscount] = useState("");
  const [actionError, setActionError] = useState<string | null>(null);

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

  const order = orderQuery.data;

  const refetchOrder = () => void queryClient.invalidateQueries({ queryKey: ["order", orderId] });

  const onError = (error: unknown) =>
    setActionError(error instanceof ApiError ? error.message : "Operação falhou.");

  const run = { onSuccess: () => { setActionError(null); refetchOrder(); }, onError };

  const addItem = useMutation({
    mutationFn: (productId: number) => addOrderItem(orderId, productId, 1, null, employeeId),
    ...run,
  });

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

  const printBillMutation = useMutation({
    mutationFn: () => printBill(orderId),
    onError: (e) => setActionError(e instanceof ApiError ? e.message : "Falha ao imprimir a conta."),
  });

  const closeMutation = useMutation({
    mutationFn: () => closeOrder(orderId),
    onSuccess: () => {
      setActionError(null);
      refetchOrder();
      // "Deseja imprimir?" SO aparece com a impressao de contas ligada.
      if (printSettingsQuery.data?.printBillsEnabled && window.confirm("Deseja imprimir a conta?"))
        printBillMutation.mutate();
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
    <Overlay title={title} onClose={onClose} wide>
      {orderQuery.isLoading && <p style={{ color: "var(--ink-dim)" }}>Carregando pedido…</p>}
      {orderQuery.isError && <p className="error-text">Falha ao carregar o pedido.</p>}

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
                    color:
                      order.totalAmount >= order.creditLimitAmount
                        ? "var(--danger)"
                        : order.totalAmount >= order.creditLimitAmount * 0.8
                          ? "var(--busy)"
                          : "var(--ok)",
                  }}
                >
                  {formatBRL(order.totalAmount)} / {formatBRL(order.creditLimitAmount)}
                </strong>
              </span>
              {featuresQuery.data?.canManageAccess && (
                <button
                  className="btn-ghost"
                  style={{ minHeight: 36, padding: "0 12px", fontSize: "0.85rem" }}
                  disabled={raiseLimitMutation.isPending}
                  onClick={() => {
                    const answer = window.prompt(
                      "Novo limite da comanda (R$):",
                      String(order.creditLimitAmount! + 100),
                    );
                    if (answer === null) return;
                    const value = Number(answer.replace(",", "."));
                    if (Number.isFinite(value) && value > 0) raiseLimitMutation.mutate(value);
                  }}
                >
                  Liberar limite (gerente)
                </button>
              )}
            </div>
          )}

          <div className="ticket">
            <div className="ticket-head">
              <span className="display" style={{ fontSize: "1.2rem" }}>
                Itens
              </span>
              <span className="mono-num" style={{ color: "var(--ink-faint)", fontSize: "0.85rem" }}>
                aberto às {new Date(order.openedAt).toLocaleTimeString("pt-BR", { hour: "2-digit", minute: "2-digit" })}
              </span>
            </div>

            {order.items.length === 0 && (
              <div className="ticket-row" style={{ color: "var(--ink-faint)" }}>
                Nenhum item lançado ainda.
              </div>
            )}

            {order.items.map((item) => {
              const next = nextItemStatus[item.orderItemStatusId];
              const cancelled = item.orderItemStatusId === OrderItemStatus.Cancelado;
              return (
                <div className="ticket-row" key={item.id}>
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
                  </div>
                  {isOpen && next !== undefined && !cancelled && (
                    <div style={{ display: "flex", gap: 6 }}>
                      <button
                        className="btn-ghost"
                        style={{ minHeight: 38, padding: "0 10px", fontSize: "0.85rem" }}
                        onClick={() => advanceItem.mutate({ itemId: item.id, statusId: next })}
                      >
                        → {orderItemStatusLabel[next]}
                      </button>
                      <button
                        className="btn-danger"
                        style={{ minHeight: 38, padding: "0 10px", fontSize: "0.85rem" }}
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
              <span className="mono-num" style={{ color: "var(--amber)" }}>
                {formatBRL(order.totalAmount)}
              </span>
            </div>
          </div>

          {actionError && <p className="error-text">{actionError}</p>}

          {isOpen && order.diningTableId !== null && canUseCash &&
            order.totalAmount - order.partialPaidAmount > 0 && (
            <button className="btn-ghost" onClick={() => setPartialOpen(true)}>
              💸 Pagamento parcial (cliente saindo)
            </button>
          )}

          {awaitingPayment && (
            <button
              className="btn-ghost"
              disabled={reopenMutation.isPending}
              onClick={() => {
                if (window.confirm("Reabrir a conta para consumo? A taxa de serviço será recalculada no próximo fechamento."))
                  reopenMutation.mutate();
              }}
            >
              ↩ Reabrir consumo (fechou por engano)
            </button>
          )}

          {awaitingPayment && order.serviceFeeAmount > 0 && featuresQuery.data?.canManageAccess && (
            <button
              className="btn-ghost"
              disabled={removeFeeMutation.isPending}
              onClick={() => {
                if (window.confirm("Retirar a taxa de serviço (10%) desta conta?"))
                  removeFeeMutation.mutate();
              }}
            >
              {removeFeeMutation.isPending ? "Retirando…" : "Retirar 10% (gerente)"}
            </button>
          )}

          {awaitingPayment && printSettingsQuery.data?.printBillsEnabled && (
            <button
              className="btn-ghost"
              disabled={printBillMutation.isPending}
              onClick={() => printBillMutation.mutate()}
            >
              {printBillMutation.isPending ? "Imprimindo…" : "🖨 Imprimir conta"}
            </button>
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
              <button className="btn-primary" onClick={() => setMenuOpen((v) => !v)}>
                {menuOpen ? "Fechar cardápio" : "+ Lançar item"}
              </button>

              {menuOpen && (
                <div style={{ display: "grid", gap: 10 }}>
                  <input
                    placeholder="Buscar no cardápio…"
                    value={search}
                    onChange={(e) => setSearch(e.target.value)}
                    autoFocus
                  />
                  <div style={{ display: "grid", gap: 8, maxHeight: 260, overflowY: "auto" }}>
                    {menuQuery.isLoading && (
                      <p style={{ color: "var(--ink-dim)" }}>Carregando cardápio…</p>
                    )}
                    {filteredMenu.map((item) => (
                      <button
                        key={item.id}
                        className="btn-ghost"
                        style={{ display: "flex", justifyContent: "space-between", padding: "0 14px" }}
                        disabled={addItem.isPending}
                        onClick={() => addItem.mutate(item.id)}
                      >
                        <span>
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
                  placeholder="Desconto (R$)"
                  inputMode="decimal"
                  value={discount}
                  onChange={(e) => setDiscount(e.target.value)}
                  style={{ flex: 1 }}
                />
                <button
                  className="btn-ghost"
                  disabled={discount.trim() === "" || discountMutation.isPending}
                  onClick={() => discountMutation.mutate()}
                >
                  Aplicar
                </button>
              </div>

              <div style={{ display: "flex", gap: 10 }}>
                <button
                  className="btn-danger"
                  style={{ flex: 1 }}
                  disabled={cancelMutation.isPending}
                  onClick={() => {
                    if (window.confirm("Cancelar este pedido? A mesa/comanda será liberada."))
                      cancelMutation.mutate();
                  }}
                >
                  Cancelar pedido
                </button>
                <button
                  className="btn-primary"
                  style={{ flex: 2 }}
                  disabled={order.items.length === 0 || closeMutation.isPending}
                  onClick={() => closeMutation.mutate()}
                >
                  Fechar conta (+10%)
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
