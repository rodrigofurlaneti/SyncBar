import { api } from "../../lib/apiClient";

// Integração Asaas (gateway de pagamentos) — 5 áreas: Configurações (credenciais por
// empresa/filial), Clientes (vínculo Customer local ↔ cliente no Asaas), Pagamentos (cobranças
// PIX/Boleto/Cartão), Cartões salvos (tokenização) e Webhooks (log de eventos recebidos do
// Asaas). Todas as rotas abaixo espelham exatamente os 5 controllers do backend
// (AsaasCustomerController, AsaasPaymentController, AsaasSavedCardController,
// AsaasSettingController, AsaasWebhookLogController).

// --- Configurações (Setting) --------------------------------------------------------------

export interface AsaasSettingResponse {
  id: number;
  companyId: number;
  branchId: number | null;
  environment: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string | null;
}

export const getAsaasSettingById = (id: number): Promise<AsaasSettingResponse> =>
  api<AsaasSettingResponse>(`/api/asaas/settings/${id}`);

export const getAsaasSettingByCompanyId = (companyId: number): Promise<AsaasSettingResponse> =>
  api<AsaasSettingResponse>(`/api/asaas/settings/company/${companyId}`);

export const getAsaasSettingByBranchId = (companyId: number, branchId: number): Promise<AsaasSettingResponse> =>
  api<AsaasSettingResponse>(`/api/asaas/settings/company/${companyId}/branch/${branchId}`);

export const resolveAsaasSetting = (companyId: number, branchId?: number | null): Promise<AsaasSettingResponse> =>
  api<AsaasSettingResponse>(
    `/api/asaas/settings/resolve?companyId=${companyId}${branchId ? `&branchId=${branchId}` : ""}`,
  );

export const getAllActiveAsaasSettings = (companyId: number): Promise<AsaasSettingResponse[]> =>
  api<AsaasSettingResponse[]>(`/api/asaas/settings/company/${companyId}/active`);

export const existsAsaasSettingForCompany = (companyId: number): Promise<boolean> =>
  api<boolean>(`/api/asaas/settings/exists/company/${companyId}`);

export const existsAsaasSettingForBranch = (companyId: number, branchId: number): Promise<boolean> =>
  api<boolean>(`/api/asaas/settings/exists/company/${companyId}/branch/${branchId}`);

export interface CreateAsaasSettingPayload {
  companyId: number;
  branchId: number | null;
  apiKey: string;
  webhookToken: string | null;
  environment: string | null;
  isActive: boolean;
}

export interface CreateAsaasSettingResponse {
  id: number;
  companyId: number;
  branchId: number | null;
  environment: string;
  isActive: boolean;
}

export const createAsaasSetting = (payload: CreateAsaasSettingPayload): Promise<CreateAsaasSettingResponse> =>
  api<CreateAsaasSettingResponse>("/api/asaas/settings", { method: "POST", body: JSON.stringify(payload) });

export interface UpdateAsaasSettingPayload {
  companyId: number;
  apiKey?: string | null;
  webhookToken?: string | null;
  environment?: string | null;
  isActive?: boolean | null;
}

export const updateAsaasSetting = (id: number, payload: UpdateAsaasSettingPayload): Promise<void> =>
  api<void>(`/api/asaas/settings/${id}`, { method: "PUT", body: JSON.stringify(payload) });

export const deleteAsaasSetting = (id: number, companyId: number): Promise<void> =>
  api<void>(`/api/asaas/settings/${id}?companyId=${companyId}`, { method: "DELETE" });

// --- Clientes (Customer binding) -----------------------------------------------------------

export interface AsaasCustomerBindingResponse {
  id: number;
  customerId: number;
  companyId: number;
  asaasCustomerId: string;
  createdAt: string;
  isActive: boolean;
}

export const getAsaasCustomerById = (id: number): Promise<AsaasCustomerBindingResponse> =>
  api<AsaasCustomerBindingResponse>(`/api/asaas/customers/${id}`);

export const getAllAsaasCustomersByCompany = (companyId: number): Promise<AsaasCustomerBindingResponse[]> =>
  api<AsaasCustomerBindingResponse[]>(`/api/asaas/customers/company/${companyId}`);

export const getAsaasCustomerByCustomerAndCompany = (
  companyId: number,
  customerId: number,
): Promise<AsaasCustomerBindingResponse> =>
  api<AsaasCustomerBindingResponse>(`/api/asaas/customers/company/${companyId}/customer/${customerId}`);

export const getAsaasCustomerByAsaasId = (asaasCustomerId: string): Promise<AsaasCustomerBindingResponse> =>
  api<AsaasCustomerBindingResponse>(`/api/asaas/customers/asaas-id/${encodeURIComponent(asaasCustomerId)}`);

export const existsAsaasCustomer = (customerId: number, companyId: number): Promise<boolean> =>
  api<boolean>(`/api/asaas/customers/exists?customerId=${customerId}&companyId=${companyId}`);

export interface CreateAsaasCustomerPayload {
  customerId: number;
  companyId: number;
  asaasCustomerId: string;
}

export const createAsaasCustomer = (payload: CreateAsaasCustomerPayload): Promise<number> =>
  api<number>("/api/asaas/customers", { method: "POST", body: JSON.stringify(payload) });

export const updateAsaasCustomer = (id: number, newAsaasCustomerId: string): Promise<void> =>
  api<void>(`/api/asaas/customers/${id}`, {
    method: "PUT",
    body: JSON.stringify({ newAsaasCustomerId }),
  });

export const deleteAsaasCustomer = (companyId: number, customerId: number): Promise<void> =>
  api<void>(`/api/asaas/customers/company/${companyId}/customer/${customerId}`, { method: "DELETE" });

// --- Pagamentos (Payment) --------------------------------------------------------------------

export interface AsaasPaymentResponse {
  id: number;
  branchId: number;
  customerOrderId: number;
  customerId: number | null;
  asaasPaymentId: string;
  billingType: string;
  status: string;
  value: number;
  netValue: number | null;
  dueDate: string;
  paymentDate: string | null;
  pixQrCodeBase64: string | null;
  pixPayload: string | null;
  invoiceUrl: string | null;
  bankSlipUrl: string | null;
  installmentCount: number;
  creditCardToken: string | null;
  createdAt: string;
  isActive: boolean;
}

export const getAsaasPaymentById = (id: number): Promise<AsaasPaymentResponse> =>
  api<AsaasPaymentResponse>(`/api/asaas/payments/${id}`);

export const getAsaasPaymentByAsaasId = (asaasPaymentId: string): Promise<AsaasPaymentResponse> =>
  api<AsaasPaymentResponse>(`/api/asaas/payments/asaas-id/${encodeURIComponent(asaasPaymentId)}`);

export const getAsaasPaymentByCustomerOrderId = (customerOrderId: number): Promise<AsaasPaymentResponse> =>
  api<AsaasPaymentResponse>(`/api/asaas/payments/order/${customerOrderId}`);

export const getAsaasPaymentsByBranch = (branchId: number): Promise<AsaasPaymentResponse[]> =>
  api<AsaasPaymentResponse[]>(`/api/asaas/payments/branch/${branchId}`);

export const getPendingAsaasPaymentsByBranch = (branchId: number): Promise<AsaasPaymentResponse[]> =>
  api<AsaasPaymentResponse[]>(`/api/asaas/payments/branch/${branchId}/pending`);

export const existsAsaasPayment = (asaasPaymentId: string): Promise<boolean> =>
  api<boolean>(`/api/asaas/payments/exists/${encodeURIComponent(asaasPaymentId)}`);

export const AsaasBillingType = {
  Pix: "PIX",
  Boleto: "BOLETO",
  CreditCard: "CREDIT_CARD",
} as const;

export type AsaasBillingTypeValue = (typeof AsaasBillingType)[keyof typeof AsaasBillingType];

export const asaasBillingTypeLabel: Record<string, string> = {
  PIX: "Pix",
  BOLETO: "Boleto",
  CREDIT_CARD: "Cartão de crédito",
};

// Espelha os status brutos devolvidos pelo Asaas (não é um enum fechado nosso — o gateway pode
// mandar outros valores; os mais comuns estão listados aqui só para exibição amigável).
export const asaasPaymentStatusLabel: Record<string, string> = {
  PENDING: "Aguardando pagamento",
  RECEIVED: "Recebido",
  CONFIRMED: "Confirmado",
  OVERDUE: "Vencido",
  REFUNDED: "Estornado",
  RECEIVED_IN_CASH: "Recebido em dinheiro",
  CHARGEBACK_REQUESTED: "Chargeback solicitado",
  CHARGEBACK_DISPUTE: "Em disputa (chargeback)",
  AWAITING_CHARGEBACK_REVERSAL: "Aguardando reversão de chargeback",
  DUNNING_REQUESTED: "Em cobrança",
  DUNNING_RECEIVED: "Recebido via cobrança",
  AWAITING_RISK_ANALYSIS: "Em análise de risco",
};

export interface CreateAsaasPaymentPayload {
  branchId: number;
  customerOrderId: number;
  customerId: number | null;
  billingType: AsaasBillingTypeValue | string;
  value: number;
  dueDate: string; // yyyy-MM-dd
  installmentCount?: number;
  creditCardToken?: string | null;
}

export interface CreateAsaasPaymentResponse {
  paymentId: number;
  asaasPaymentId: string;
  status: string;
  pixQrCodeBase64: string | null;
  pixPayload: string | null;
  invoiceUrl: string | null;
  bankSlipUrl: string | null;
}

export const createAsaasPayment = (payload: CreateAsaasPaymentPayload): Promise<CreateAsaasPaymentResponse> =>
  api<CreateAsaasPaymentResponse>("/api/asaas/payments", { method: "POST", body: JSON.stringify(payload) });

export interface UpdateAsaasPaymentPayload {
  status: string;
  netValue?: number | null;
  paymentDate?: string | null;
  pixQrCodeBase64?: string | null;
  pixPayload?: string | null;
  invoiceUrl?: string | null;
  bankSlipUrl?: string | null;
}

export const updateAsaasPayment = (id: number, payload: UpdateAsaasPaymentPayload): Promise<void> =>
  api<void>(`/api/asaas/payments/${id}`, { method: "PUT", body: JSON.stringify(payload) });

export const deleteAsaasPayment = (id: number): Promise<void> =>
  api<void>(`/api/asaas/payments/${id}`, { method: "DELETE" });

// --- Cartões salvos (SavedCard) --------------------------------------------------------------

export interface AsaasSavedCardResponse {
  id: number;
  customerId: number;
  companyId: number;
  cardBrand: string;
  last4Digits: string;
  holderName: string;
  expiryMonth: string;
  expiryYear: string;
  isDefault: boolean;
  createdAt: string;
  isActive: boolean;
}

export const getAsaasSavedCardById = (id: number): Promise<AsaasSavedCardResponse> =>
  api<AsaasSavedCardResponse>(`/api/asaas/saved-cards/${id}`);

export const getAsaasSavedCardsByCustomerId = (customerId: number): Promise<AsaasSavedCardResponse[]> =>
  api<AsaasSavedCardResponse[]>(`/api/asaas/saved-cards/customer/${customerId}`);

export const getAsaasSavedCardByToken = (creditCardToken: string): Promise<AsaasSavedCardResponse> =>
  api<AsaasSavedCardResponse>(`/api/asaas/saved-cards/token/${encodeURIComponent(creditCardToken)}`);

export const existsAsaasSavedCardByToken = (creditCardToken: string): Promise<boolean> =>
  api<boolean>(`/api/asaas/saved-cards/exists/token/${encodeURIComponent(creditCardToken)}`);

export interface CreateAsaasSavedCardPayload {
  customerId: number;
  companyId: number;
  holderName: string;
  cardNumber: string;
  expiryMonth: string;
  expiryYear: string;
  ccv: string;
  setAsDefault?: boolean;
}

export interface CreateAsaasSavedCardResponse {
  id: number;
  customerId: number;
  companyId: number;
  cardBrand: string;
  last4Digits: string;
  isDefault: boolean;
}

export const createAsaasSavedCard = (payload: CreateAsaasSavedCardPayload): Promise<CreateAsaasSavedCardResponse> =>
  api<CreateAsaasSavedCardResponse>("/api/asaas/saved-cards", { method: "POST", body: JSON.stringify(payload) });

export interface UpdateAsaasSavedCardPayload {
  customerId: number;
  companyId: number;
  holderName?: string | null;
  expiryMonth?: string | null;
  expiryYear?: string | null;
  setAsDefault?: boolean | null;
}

export const updateAsaasSavedCard = (id: number, payload: UpdateAsaasSavedCardPayload): Promise<void> =>
  api<void>(`/api/asaas/saved-cards/${id}`, { method: "PUT", body: JSON.stringify(payload) });

export const setDefaultAsaasSavedCard = (id: number, customerId: number, companyId: number): Promise<void> =>
  api<void>(`/api/asaas/saved-cards/${id}/default`, {
    method: "PATCH",
    body: JSON.stringify({ customerId, companyId }),
  });

export const deleteAsaasSavedCard = (id: number, customerId: number, companyId: number): Promise<void> =>
  api<void>(`/api/asaas/saved-cards/${id}?customerId=${customerId}&companyId=${companyId}`, { method: "DELETE" });

// --- Webhooks (WebhookLog) -------------------------------------------------------------------

// A API não registra JsonStringEnumConverter — o enum trafega como o inteiro ordinal
// (Pending=0, Processed=1, Failed=2), não como string.
export const WebhookLogStatus = {
  Pending: 0,
  Processed: 1,
  Failed: 2,
} as const;

export const webhookLogStatusLabel: Record<number, string> = {
  0: "Pendente",
  1: "Processado",
  2: "Falhou",
};

export interface AsaasWebhookLogResponse {
  id: number;
  companyId: number;
  branchId: number | null;
  event: string;
  asaasEventId: string | null;
  paymentId: string | null;
  payload: string;
  requestHeaders: string | null;
  ipAddress: string | null;
  status: number;
  errorMessage: string | null;
  createdAt: string;
  processedAt: string | null;
}

export const getAsaasWebhookLogById = (id: number, companyId: number): Promise<AsaasWebhookLogResponse> =>
  api<AsaasWebhookLogResponse>(`/api/asaas/webhook-logs/${id}?companyId=${companyId}`);

export const getAsaasWebhookLogsByPaymentId = (
  companyId: number,
  paymentId: string,
): Promise<AsaasWebhookLogResponse[]> =>
  api<AsaasWebhookLogResponse[]>(
    `/api/asaas/webhook-logs/payment/${encodeURIComponent(paymentId)}?companyId=${companyId}`,
  );

export const getUnprocessedAsaasWebhookLogs = (companyId: number, limit = 50): Promise<AsaasWebhookLogResponse[]> =>
  api<AsaasWebhookLogResponse[]>(`/api/asaas/webhook-logs/unprocessed?companyId=${companyId}&limit=${limit}`);

export const hasAlreadyProcessedAsaasEvent = (asaasEventId: string): Promise<boolean> =>
  api<boolean>(`/api/asaas/webhook-logs/events/${encodeURIComponent(asaasEventId)}/processed`);

export const updateAsaasWebhookLogStatus = (
  id: number,
  companyId: number,
  status: number,
  errorMessage?: string | null,
): Promise<void> =>
  api<void>(`/api/asaas/webhook-logs/${id}/status`, {
    method: "PATCH",
    body: JSON.stringify({ companyId, status, errorMessage: errorMessage ?? null }),
  });

export const deleteAsaasWebhookLog = (id: number, companyId: number): Promise<void> =>
  api<void>(`/api/asaas/webhook-logs/${id}?companyId=${companyId}`, { method: "DELETE" });
