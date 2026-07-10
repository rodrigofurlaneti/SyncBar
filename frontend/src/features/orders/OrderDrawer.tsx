import { useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  addOrderItem,
  applyDiscount,
  cancelOrder,
  closeOrder,
  getOrder,
  updateItemStatus,
} from "./api";
import { getMenu } from "../catalog/api";
import { useAuthStore } from "../../stores/authStore";
import { ApiError } from "../../lib/apiClient";
import {
  OrderItemStatus,
  OrderStatus,
  formatBRL,
  orderItemStatusLabel,
} from "../../lib/types";
import { Overlay } from "./Overlay";
import { PaymentPanel } from "./PaymentPanel";

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
  const [search, setSearch] = useState("");
  const [discount, setDiscount] = useState("");
  const [actionError, setActionError] = useState<string | null>(null);

  const orderQuery = useQuery({
    queryKey: ["order", orderId],
    queryFn: () => getOrder(orderId),
  });

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
      updateItemStatus(orderId, itemId, statusId),
    ...run,
  });

  const discountMutation = useMutation({
    mutationFn: () => applyDiscount(orderId, Number(discount.replace(",", "."))),
    ...run,
  });

  const closeMutation = useMutation({ mutationFn: () => closeOrder(orderId), ...run });
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

      {order && (
        <>
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
            <div className="ticket-total">
              <span>Total</span>
              <span className="mono-num" style={{ color: "var(--amber)" }}>
                {formatBRL(order.totalAmount)}
              </span>
            </div>
          </div>

          {actionError && <p className="error-text">{actionError}</p>}

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
                        <span>{item.name}</span>
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
