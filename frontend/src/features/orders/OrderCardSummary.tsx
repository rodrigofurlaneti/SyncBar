import { formatBRL, OrderItemStatus } from "../../lib/types";
import type { OrderResponse } from "../../lib/types";

export const countOrderItems = (order: OrderResponse) => order.items
    .filter(item => item.orderItemStatusId !== OrderItemStatus.Cancelado)
    .reduce((count, item) => count + item.quantity, 0);

export const itemsLabel = (count: number) => `${count.toLocaleString("pt-BR")} ${count === 1 ? "item" : "itens"}`;
export function formatOpenedAt(value?: string) {
    const date = value ? new Date(value) : null;
    return date && !Number.isNaN(date.getTime())
        ? `Aberta às ${date.toLocaleTimeString("pt-BR", { hour: "2-digit", minute: "2-digit" })}`
        : "Horário indisponível";
}

export function OrderCardIcon({ type }: { type: "items" | "total" | "time" }) {
    return <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.6"
        strokeLinecap="round" strokeLinejoin="round" aria-hidden="true" focusable="false">
        {type === "items" ? <><path d="m4 7 8-4 8 4-8 4-8-4Zm0 0v10l8 4 8-4V7M12 11v10M8 5l8 4" /></>
            : type === "total" ? <><path d="M6 3h12v18l-3-2-3 2-3-2-3 2V3Z" /><path d="M9 7h6M9 11h6M9 15h3" /></>
                : <><circle cx="12" cy="12" r="9" /><path d="M12 7v5l3 2" /></>}
    </svg>;
}

export function OrderCardSummary({ itemsCount, totalValue, openedAt }: { itemsCount: number; totalValue: number; openedAt?: string }) {
    return <span className="order-card-summary" aria-hidden="true">
        <span><OrderCardIcon type="items" /><span>{itemsLabel(itemsCount)}</span></span>
        <span><OrderCardIcon type="total" /><span className="mono-num">{formatBRL(totalValue)}</span></span>
        <span><OrderCardIcon type="time" /><span>{formatOpenedAt(openedAt)}</span></span>
    </span>;
}
