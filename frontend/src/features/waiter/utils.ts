import { OrderItemStatus, orderTypeLabel } from "../../lib/types";
import type { OrderResponse, TableResponse, ComandaResponse } from "../../lib/types";

export type BadgeTone = "ready" | "preparing" | "waiting";

export interface OrderBadge {
    label: string;
    tone: BadgeTone;
}

export const badgeToneVar: Record<BadgeTone, string> = {
    ready: "var(--w-ok, #22c55e)",
    preparing: "var(--w-info, #3b82f6)",
    waiting: "var(--w-warn, #f59e0b)",
};

export type QuickActionKey = "transferir" | "mesas" | "comandas" | "turno" | "calculadora";

export const quickActions: { key: QuickActionKey; icon: string; label: string }[] = [
    { key: "transferir", icon: "🔀", label: "Transferir" },
    { key: "mesas", icon: "🍽️", label: "Mesas" },
    { key: "comandas", icon: "📋", label: "Comandas" }
];

export type TabKey = "inicio" | "mesas" | "comandas" | "pedidos" | "mensagens" | "calculadora" | "perfil";

export const tabs: { key: TabKey; icon: string; label: string }[] = [
    { key: "inicio", icon: "🏠", label: "Início" },
    { key: "pedidos", icon: "🧾", label: "Pedidos" },
    { key: "mensagens", icon: "💬", label: "Mensagens" },
    { key: "calculadora", icon: "🔢", label: "Calculadora" },
    { key: "perfil", icon: "👤", label: "Perfil" },
];

export function deriveOrderBadge(order: OrderResponse): OrderBadge {
    const statuses = order.items.map((item) => item.orderItemStatusId);
    if (statuses.length === 0) return { label: "Sem itens", tone: "waiting" };
    if (statuses.some((s) => s === OrderItemStatus.Pronto)) return { label: "Pronto", tone: "ready" };
    if (statuses.some((s) => s === OrderItemStatus.EmPreparo || s === OrderItemStatus.EnviadoCozinha))
        return { label: "Em preparo", tone: "preparing" };
    if (statuses.some((s) => s === OrderItemStatus.Lancado)) return { label: "Aguardando", tone: "waiting" };
    return { label: "Entregue", tone: "ready" };
}

export function orderLabel(
    order: OrderResponse,
    tablesById: Map<number, TableResponse>,
    comandasById: Map<number, ComandaResponse>,
): string {
    if (order.diningTableId !== null) {
        const table = tablesById.get(order.diningTableId);
        return `Mesa ${table?.number ?? order.diningTableId}`;
    }
    if (order.comandaId !== null) {
        const comanda = comandasById.get(order.comandaId);
        return `Comanda ${comanda?.code ?? order.comandaId}`;
    }
    const type = order.orderTypeId ? orderTypeLabel[order.orderTypeId] : "Pedido";
    return order.customerName ? `${type} · ${order.customerName}` : type;
}

export function elapsedLabel(openedAt: string): string {
    const minutes = Math.max(0, Math.floor((Date.now() - new Date(openedAt).getTime()) / 60_000));
    if (minutes < 60) return `há ${minutes} min`;
    const hours = Math.floor(minutes / 60);
    return `há ${hours} h`;
}

export function firstNameFrom(userName: string | null): string {
    if (!userName) return "Garçom";
    const beforeAt = userName.split("@")[0];
    const firstWord = beforeAt.trim().split(/[\s._-]+/)[0];
    if (!firstWord) return "Garçom";
    return firstWord.charAt(0).toUpperCase() + firstWord.slice(1).toLowerCase();
}

export function initialsFrom(userName: string | null): string {
    if (!userName) return "GC";
    const base = userName.split("@")[0].trim();
    const parts = base.split(/[\s._-]+/).filter(Boolean);
    if (parts.length >= 2) return (parts[0][0] + parts[1][0]).toUpperCase();
    return base.slice(0, 2).toUpperCase();
}