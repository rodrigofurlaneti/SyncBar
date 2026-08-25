export function formatOrderStatus(status: string): { label: string; color: string; icon: string } {
  const statusMap: Record<
    string,
    { label: string; color: string; icon: string }
  > = {
    PLACED: { label: "Recebido", color: "#3b82f6", icon: "📥" },
    CONFIRMED: { label: "Confirmado", color: "#0ea5e9", icon: "✓" },
    PREPARATION_STARTED: { label: "Em preparo", color: "#f59e0b", icon: "👨‍🍳" },
    READY_TO_PICKUP: { label: "Pronto", color: "#10b981", icon: "📦" },
    DISPATCHED: { label: "Saiu pra entrega", color: "#06b6d4", icon: "🚗" },
    CONCLUDED: { label: "Entregue", color: "#6b7280", icon: "✓" },
    CANCELLED: { label: "Cancelado", color: "#ef4444", icon: "✕" },
  };

  return statusMap[status] || { label: status, color: "#6b7280", icon: "?" };
}

export function formatOrderType(type: string): string {
  const typeMap: Record<string, string> = {
    DELIVERY: "🚗 Delivery",
    TAKEOUT: "🛍️ Retirada",
    DINE_IN: "🍽️ Consumo no local",
  };
  return typeMap[type] || type;
}

export function formatMerchantAvailability(available: boolean, state?: string | null): {
  label: string;
  color: string;
  bg: string;
  icon: string;
} {
  if (available) {
    return {
      label: "Disponível",
      color: "#1b5e20",
      bg: "#e8f5e9",
      icon: "✓",
    };
  }
  return {
    label: state || "Indisponível",
    color: "#b71c1c",
    bg: "#ffebee",
    icon: "✕",
  };
}

export function formatValidationState(state: string): { severity: "error" | "warning" | "info"; icon: string; label: string } {
  const states: Record<string, { severity: "error" | "warning" | "info"; icon: string; label: string }> = {
    ERROR: { severity: "error", icon: "✕", label: "Erro" },
    INVALID: { severity: "error", icon: "✕", label: "Inválido" },
    WARNING: { severity: "warning", icon: "⚠", label: "Aviso" },
    INFO: { severity: "info", icon: "ℹ", label: "Informação" },
    VALID: { severity: "info", icon: "✓", label: "Válido" },
    OK: { severity: "info", icon: "✓", label: "Válido" },
  };

  return states[state.toUpperCase()] || { severity: "info", icon: "?", label: state };
}

export function formatDeliveredBy(deliveredBy?: string | null): string {
  if (!deliveredBy) return "—";
  if (deliveredBy === "IFOOD") return "🍔 iFood Logística";
  return `🏪 ${deliveredBy}`;
}

export function formatFinancialStatus(status: string): { label: string; icon: string; color: string } {
  const statusMap: Record<string, { label: string; icon: string; color: string }> = {
    PENDING: { label: "Pendente", icon: "⏳", color: "#f59e0b" },
    AVAILABLE: { label: "Disponível", icon: "✓", color: "#10b981" },
    FAILED: { label: "Falha", icon: "✕", color: "#ef4444" },
    CANCELLED: { label: "Cancelado", icon: "✕", color: "#6b7280" },
  };
  return statusMap[status] || { label: status, icon: "?", color: "#6b7280" };
}

export function formatReconciliationStatus(status: string): { label: string; icon: string; color: string } {
  const statusMap: Record<string, { label: string; icon: string; color: string }> = {
    PROCESSING: { label: "Processando", icon: "⏳", color: "#f59e0b" },
    COMPLETED: { label: "Concluído", icon: "✓", color: "#10b981" },
    FAILED: { label: "Falha", icon: "✕", color: "#ef4444" },
  };
  return statusMap[status] || { label: status, icon: "?", color: "#6b7280" };
}

export function formatReviewState(state: string): { label: string; icon: string; color: string } {
  const stateMap: Record<string, { label: string; icon: string; color: string }> = {
    OPEN: { label: "Aberta", icon: "📝", color: "#f59e0b" },
    CLOSED: { label: "Respondida", icon: "✓", color: "#10b981" },
    REJECTED: { label: "Rejeitada", icon: "✕", color: "#6b7280" },
  };
  return stateMap[state] || { label: state, icon: "?", color: "#6b7280" };
}

export function formatShippingStatus(status: string): { label: string; icon: string; color: string } {
  const statusMap: Record<string, { label: string; icon: string; color: string }> = {
    DRIVER_ASSIGNED: { label: "Entregador atribuído", icon: "👤", color: "#3b82f6" },
    GOING_TO_ORIGIN: { label: "Indo para a loja", icon: "🚗", color: "#0ea5e9" },
    ARRIVED_AT_ORIGIN: { label: "Na loja", icon: "📍", color: "#06b6d4" },
    DISPATCHED: { label: "A caminho", icon: "🚗", color: "#f59e0b" },
    ARRIVED_AT_DESTINATION: { label: "Entregando", icon: "🏠", color: "#10b981" },
    DELIVERY_CODE_VERIFIED: { label: "Entregue", icon: "✓", color: "#059669" },
  };
  return statusMap[status] || { label: status, icon: "?", color: "#6b7280" };
}

export function formatDisputeStatus(status: string): { label: string; icon: string; color: string } {
  const statusMap: Record<string, { label: string; icon: string; color: string }> = {
    OPEN: { label: "Aberta", icon: "⚠", color: "#ef4444" },
    ACCEPTED: { label: "Aceita", icon: "✓", color: "#10b981" },
    REJECTED: { label: "Rejeitada", icon: "✕", color: "#6b7280" },
    IN_INVESTIGATION: { label: "Em investigação", icon: "🔍", color: "#f59e0b" },
  };
  return statusMap[status] || { label: status, icon: "?", color: "#6b7280" };
}

export function formatOrderTiming(timing: string, prepStartTime?: string | null): string {
  if (timing === "SCHEDULED" && prepStartTime) {
    const time = new Date(prepStartTime).toLocaleTimeString("pt-BR", {
      hour: "2-digit",
      minute: "2-digit",
    });
    return `📅 Agendado para ${time}`;
  }
  return timing === "IMMEDIATE" ? "⚡ Imediato" : timing;
}

export function formatCurrency(value: number, locale = "pt-BR", currency = "BRL"): string {
  return new Intl.NumberFormat(locale, {
    style: "currency",
    currency,
  }).format(value);
}

export function formatPercentage(value: number, decimals = 2): string {
  return `${value.toFixed(decimals)}%`;
}

export function formatDate(date: string | Date, format = "pt-BR"): string {
  const d = typeof date === "string" ? new Date(date) : date;
  return d.toLocaleDateString(format, {
    weekday: "short",
    year: "numeric",
    month: "short",
    day: "numeric",
  });
}

export function formatTime(date: string | Date): string {
  const d = typeof date === "string" ? new Date(date) : date;
  return d.toLocaleTimeString("pt-BR", {
    hour: "2-digit",
    minute: "2-digit",
  });
}

export function formatDateTimeShort(date: string | Date): string {
  return `${formatDate(date)} ${formatTime(date)}`;
}

export function calculateOrderMetrics(
  orders: Array<{ status: string; ifoodOrderType: string; totalAmount?: number }>,
) {
  return {
    total: orders.length,
    delivered: orders.filter((o) => o.status === "CONCLUDED").length,
    cancelled: orders.filter((o) => o.status === "CANCELLED").length,
    inProgress: orders.filter((o) => ["CONFIRMED", "PREPARATION_STARTED", "READY_TO_PICKUP", "DISPATCHED"].includes(o.status)).length,
    totalValue: orders.reduce((sum, o) => sum + (o.totalAmount ?? 0), 0),
    deliveryOrders: orders.filter((o) => o.ifoodOrderType === "DELIVERY").length,
    takeoutOrders: orders.filter((o) => o.ifoodOrderType === "TAKEOUT").length,
    dineInOrders: orders.filter((o) => o.ifoodOrderType === "DINE_IN").length,
  };
}
