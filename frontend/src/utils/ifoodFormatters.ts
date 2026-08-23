// Formatadores e tipos para dados do iFood

export function formatCurrency(value: number): string {
  return new Intl.NumberFormat("pt-BR", {
    style: "currency",
    currency: "BRL",
  }).format(value);
}

export function formatPercentage(value: number, decimals = 2): string {
  return `${(value * 100).toFixed(decimals)}%`;
}

export function formatDate(date: string | Date): string {
  const d = typeof date === "string" ? new Date(date) : date;
  return new Intl.DateTimeFormat("pt-BR", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
  }).format(d);
}

export function formatDateTime(date: string | Date): string {
  const d = typeof date === "string" ? new Date(date) : date;
  return new Intl.DateTimeFormat("pt-BR", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  }).format(d);
}

export function formatTime(date: string | Date): string {
  const d = typeof date === "string" ? new Date(date) : date;
  return new Intl.DateTimeFormat("pt-BR", {
    hour: "2-digit",
    minute: "2-digit",
  }).format(d);
}

// Status do merchant com cor e rótulo
export function getMerchantStatusDisplay(available: boolean, operationState?: string) {
  if (available) {
    return {
      label: "Aberto",
      color: "#4caf50",
      bgColor: "#e8f5e9",
      icon: "●",
    };
  }

  return {
    label: operationState || "Fechado",
    color: "#f44336",
    bgColor: "#ffebee",
    icon: "●",
  };
}

// Scores de avaliação com cores
export function getReviewScoreDisplay(score: number) {
  if (score >= 4.5) return { label: "Excelente", color: "#4caf50", icon: "⭐" };
  if (score >= 4) return { label: "Muito bom", color: "#8bc34a", icon: "★" };
  if (score >= 3) return { label: "Bom", color: "#ffc107", icon: "★" };
  if (score >= 2) return { label: "Regular", color: "#ff9800", icon: "★" };
  return { label: "Ruim", color: "#f44336", icon: "★" };
}

// Status do pedido iFood
export const IFoodOrderStatuses: Record<string, { label: string; color: string; bgColor: string }> = {
  PENDING_CONFIRMATION: { label: "Aguardando confirmação", color: "#1976d2", bgColor: "#e3f2fd" },
  CONFIRMED: { label: "Confirmado", color: "#2196f3", bgColor: "#e3f2fd" },
  PREPARING: { label: "Em preparo", color: "#ff9800", bgColor: "#fff3e0" },
  READY_FOR_PICKUP: { label: "Pronto para retirada", color: "#8bc34a", bgColor: "#e8f5e9" },
  READY_FOR_DELIVERY: { label: "Pronto para entrega", color: "#8bc34a", bgColor: "#e8f5e9" },
  IN_DELIVERY: { label: "Em entrega", color: "#2196f3", bgColor: "#e3f2fd" },
  DELIVERED: { label: "Entregue", color: "#4caf50", bgColor: "#e8f5e9" },
  CANCELLED_BY_MERCHANT: { label: "Cancelado pelo lojista", color: "#f44336", bgColor: "#ffebee" },
  CANCELLED_BY_CUSTOMER: { label: "Cancelado pelo cliente", color: "#f44336", bgColor: "#ffebee" },
  CANCELLED_BY_PLATFORM: { label: "Cancelado pela plataforma", color: "#f44336", bgColor: "#ffebee" },
};

// Tipos de entrega
export function getDeliveryTypeLabel(deliveryType?: string): string {
  if (!deliveryType) return "–";
  return {
    TAKEOUT: "Retirada",
    DINE_IN: "No local",
    DELIVERY: "Entrega",
  }[deliveryType] || deliveryType;
}

// Status de validação do merchant
export function getValidationStatusDisplay(state: string) {
  const statusMap: Record<string, { label: string; severity: "error" | "warning" | "info" }> = {
    VALID: { label: "✓ Válido", severity: "info" },
    INVALID: { label: "✕ Inválido", severity: "error" },
    WARNING: { label: "⚠ Atenção", severity: "warning" },
  };

  return statusMap[state] || { label: state, severity: "info" };
}
