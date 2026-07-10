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
  unitOfMeasureId: number;
  name: string;
  description: string | null;
  barcode: string | null;
  salePrice: number;
  costPrice: number | null;
  isStockControlled: boolean;
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

export interface EmployeeResponse {
  id: number;
  branchId: number;
  jobTitleId: number;
  name: string;
  cpf: string;
  email: string | null;
  phone: string | null;
  hiredAt: string;
  dismissedAt: string | null;
  salary: number | null;
  isActive: boolean;
}

export interface JobTitleResponse {
  id: number;
  name: string;
}

export interface UserResponse {
  id: number;
  userName: string;
  email: string;
  employeeId: number | null;
  isActive: boolean;
  roleIds: number[];
}

export interface RoleResponse {
  id: number;
  name: string;
  description: string | null;
}

export interface CategoryResponse {
  id: number;
  name: string;
  displayOrder: number;
}

export interface StockItemResponse {
  id: number;
  branchId: number;
  productId: number;
  currentQuantity: number;
  minimumQuantity: number;
  maximumQuantity: number | null;
  isBelowMinimum: boolean;
}

export interface StockMovementResponse {
  id: number;
  stockItemId: number;
  stockMovementTypeId: number;
  quantity: number;
  unitCost: number | null;
  totalCost: number | null;
  documentNumber: string | null;
  movedAt: string;
  notes: string | null;
}

export const unitOfMeasureLabel: Record<number, string> = {
  1: "Unidade",
  2: "Quilograma",
  3: "Grama",
  4: "Litro",
  5: "Mililitro",
  6: "Dose",
  7: "Porção",
  8: "Garrafa",
  9: "Lata",
  10: "Caixa",
};

export const stockMovementTypeLabel: Record<number, string> = {
  1: "Entrada (compra)",
  2: "Saída (venda)",
  3: "Ajuste — entrada",
  4: "Ajuste — saída",
  5: "Perda",
  6: "Quebra",
  7: "Transferência — entrada",
  8: "Transferência — saída",
  9: "Devolução a fornecedor",
  10: "Consumo interno",
};

// Tipos ofertados no lançamento manual (venda/estorno passam pelos fluxos próprios)
export const manualStockMovementTypes = [1, 3, 4, 5, 6, 10] as const;

export const stockMovementIsInflow: Record<number, boolean> = {
  1: true, 2: false, 3: true, 4: false, 5: false,
  6: false, 7: true, 8: false, 9: false, 10: false,
};

export interface FeatureResponse {
  id: number;
  code: string;
  name: string;
}

export interface MyFeaturesResponse {
  canManageAccess: boolean;
  features: string[];
}

export const featureLabel: Record<string, string> = {
  Salao: "Salão",
  Cardapio: "Cardápio",
  Estoque: "Estoque",
  Equipe: "Equipe",
  Usuarios: "Usuários",
  Caixa: "Caixa",
  Faturamento: "Faturamento",
  Preparo: "Preparo",
};

export interface OperatingCostResponse {
  id: number;
  costTypeId: number;
  description: string;
  amount: number;
}

export interface DailyRevenueResponse {
  day: number;
  amount: number;
}

export interface BillingSummaryResponse {
  referenceYear: number;
  referenceMonth: number;
  revenue: number;
  salesCount: number;
  costOfGoodsSold: number;
  fixedCosts: number;
  variableCosts: number;
  totalCosts: number;
  netResult: number;
  targetAmount: number | null;
  targetAttainmentRate: number | null;
  costs: OperatingCostResponse[];
  dailyRevenue: DailyRevenueResponse[];
}

export const CostType = { Fixo: 1, Variavel: 2 } as const;

export const costTypeLabel: Record<number, string> = {
  1: "Fixo",
  2: "Variável",
};

export interface StockPlanItemResponse {
  productId: number;
  productName: string;
  revenueShare: number;
  estimatedUnits: number;
  currentStock: number;
  unitsToBuy: number;
}

export interface ScenarioResponse {
  name: string;
  marginRate: number;
  breakEvenRevenue: number;
  targetRevenue: number;
  dailyTarget: number;
  estimatedSalesCount: number | null;
  stockPlan: StockPlanItemResponse[];
}

export interface ScenariosResponse {
  referenceYear: number;
  referenceMonth: number;
  daysInMonth: number;
  fixedCosts: number;
  desiredProfit: number;
  historicalRevenue: number | null;
  historicalMarginRate: number | null;
  averageTicket: number | null;
  scenarios: ScenarioResponse[];
}

export interface PreparationItemResponse {
  orderItemId: number;
  productId: number;
  productName: string;
  quantity: number;
  orderItemStatusId: number;
  notes: string | null;
  startedAt: string;
  limitMinutes: number;
  isBarItem: boolean;
  requestedBy: string | null;
}

export interface PreparationTicketResponse {
  customerOrderId: number;
  tableNumber: number | null;
  comandaCode: string | null;
  openedAt: string;
  items: PreparationItemResponse[];
}

export interface ComandaResponse {
  id: number;
  branchId: number;
  comandaStatusId: number;
  code: string;
}

export const ComandaStatus = {
  Disponivel: 1,
  EmUso: 2,
  Extraviada: 3,
  Bloqueada: 4,
} as const;

export interface CashSessionHistoryResponse {
  id: number;
  cashRegisterName: string;
  cashSessionStatusId: number;
  openedByName: string | null;
  closedByName: string | null;
  openedAt: string;
  closedAt: string | null;
  openingAmount: number;
  expectedAmount: number | null;
  closingAmount: number | null;
  differenceAmount: number | null;
  salesTotal: number;
  salesCount: number;
}

export const CashSessionStatus = {
  Aberto: 1,
  Fechado: 2,
  Conferido: 3,
} as const;
