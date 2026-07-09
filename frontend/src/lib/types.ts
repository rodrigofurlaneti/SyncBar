// Espelham os Responses da SyncBar.API — manter em sincronia com o backend.

export interface LoginResponse {
  accessToken: string;
  accessTokenExpiresAt: string;
  refreshToken: string;
  refreshTokenExpiresAt: string;
  userName: string;
  companyId: number;
  employeeId: number | null;
}

export interface OrderItemResponse {
  id: number;
  productId: number;
  orderItemStatusId: number;
  quantity: number;
  unitPrice: number;
  discountAmount: number;
  totalAmount: number;
  notes: string | null;
}

export interface OrderResponse {
  id: number;
  branchId: number;
  diningTableId: number | null;
  comandaId: number | null;
  employeeId: number;
  orderStatusId: number;
  guestCount: number | null;
  openedAt: string;
  closedAt: string | null;
  subtotalAmount: number;
  discountAmount: number;
  serviceFeeAmount: number;
  totalAmount: number;
  notes: string | null;
  items: OrderItemResponse[];
}

export interface MenuItemResponse {
  id: number;
  categoryId: number;
  name: string;
  description: string | null;
  salePrice: number;
  preparationTimeMinutes: number | null;
}

export interface TableResponse {
  id: number;
  branchId: number;
  tableStatusId: number;
  number: number;
  capacity: number | null;
}

export interface ApiProblem {
  title: string;
  detail: string;
}

// Ids fixos dos lookups (BarRestaurante_Seed.sql)
export const OrderStatus = {
  Aberto: 1,
  EmAndamento: 2,
  AguardandoPagamento: 3,
  Pago: 4,
  Cancelado: 5,
} as const;

export const OrderItemStatus = {
  Lancado: 1,
  EnviadoCozinha: 2,
  EmPreparo: 3,
  Pronto: 4,
  Entregue: 5,
  Cancelado: 6,
} as const;

export const TableStatus = {
  Livre: 1,
  Ocupada: 2,
  Reservada: 3,
  EmFechamento: 4,
  Interditada: 5,
} as const;

export const orderItemStatusLabel: Record<number, string> = {
  1: "Lançado",
  2: "Na cozinha",
  3: "Em preparo",
  4: "Pronto",
  5: "Entregue",
  6: "Cancelado",
};

export const formatBRL = (value: number): string =>
  value.toLocaleString("pt-BR", { style: "currency", currency: "BRL" });

export interface CashSessionResponse {
  id: number;
  cashRegisterId: number;
  cashSessionStatusId: number;
  openedByEmployeeId: number;
  openingAmount: number;
  openedAt: string;
}

export interface PaymentMethodTotalResponse {
  paymentMethodId: number;
  totalAmount: number;
}

export interface CashSummaryResponse {
  cashSessionId: number;
  openingAmount: number;
  salesCount: number;
  salesTotal: number;
  paymentTotals: PaymentMethodTotalResponse[];
  suprimentoTotal: number;
  sangriaTotal: number;
  despesaTotal: number;
  expectedCashAmount: number;
}

export interface CloseCashSessionResponse {
  cashSessionId: number;
  expectedAmount: number;
  closingAmount: number;
  differenceAmount: number;
}

export const PaymentMethod = {
  Dinheiro: 1,
  CartaoCredito: 2,
  CartaoDebito: 3,
  Pix: 4,
  ValeRefeicao: 5,
  ValeAlimentacao: 6,
  Cortesia: 7,
} as const;

export const paymentMethodLabel: Record<number, string> = {
  1: "Dinheiro",
  2: "Cartão de crédito",
  3: "Cartão de débito",
  4: "Pix",
  5: "Vale refeição",
  6: "Vale alimentação",
  7: "Cortesia",
};

export const CashMovementType = {
  Suprimento: 1,
  Sangria: 2,
  Despesa: 5,
} as const;

// Caixa físico padrão (seed: "Caixa 01")
export const DEFAULT_CASH_REGISTER_ID = 1;
